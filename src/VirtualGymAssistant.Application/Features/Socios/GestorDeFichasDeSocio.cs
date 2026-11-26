using Microsoft.EntityFrameworkCore;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Domain.Constants;
using VirtualGymAssistant.Domain.Entities;

namespace VirtualGymAssistant.Application.Features.Socios;

/// <summary>
/// Implementa la regla automática del sistema: "crear la ficha de socio automáticamente".
///
/// Cuando un usuario se registra, paga una cuota o marca asistencia en una sucursal en la
/// que todavía no es socio, el sistema le crea la ficha en el acto. Sin esto habría que
/// pedirle a un recepcionista que lo dé de alta antes de poder operar.
///
/// Está en una clase aparte porque la usan varios casos de uso (registro, pagos,
/// asistencias, rutinas y dietas) y la regla tiene que ser exactamente la misma en todos.
/// </summary>
public class GestorDeFichasDeSocio
{
    private readonly IVirtualGymDbContext _db;
    private readonly IProveedorDeFechaHora _reloj;

    public GestorDeFichasDeSocio(IVirtualGymDbContext db, IProveedorDeFechaHora reloj)
    {
        _db = db;
        _reloj = reloj;
    }

    /// <summary>
    /// Devuelve el Id de la ficha de socio del usuario en esa sucursal, creándola si no existe.
    /// Los cambios quedan confirmados antes de salir, así quien la llame puede usar el Id enseguida.
    /// </summary>
    public async Task<int> ObtenerOCrearAsync(int idUsuario, int idGimnasio, CancellationToken ct = default)
    {
        var existente = await BuscarIdAsync(idUsuario, idGimnasio, ct);
        if (existente is { } id) return id;

        var idEstadoActivo = await _db.SocioEstados
            .Where(e => e.Nombre == EstadosDeSocio.Activo)
            .Select(e => e.Id)
            .FirstAsync(ct);

        // El número de socio es correlativo dentro de cada sucursal.
        var ultimoNumero = await _db.Socios
            .Where(s => s.IdGimnasio == idGimnasio && s.NumeroSocio != null)
            .MaxAsync(s => (int?)s.NumeroSocio, ct) ?? 0;

        var ficha = new Socio
        {
            IdUsuario = idUsuario,
            IdGimnasio = idGimnasio,
            IdEstado = idEstadoActivo,
            NumeroSocio = ultimoNumero + 1,
            FechaAlta = _reloj.Hoy
        };

        _db.Socios.Add(ficha);
        await _db.SaveChangesAsync(ct);

        return ficha.Id;
    }

    /// <summary>Id de la ficha del usuario en esa sucursal, o null si todavía no es socio.</summary>
    public async Task<int?> BuscarIdAsync(int idUsuario, int idGimnasio, CancellationToken ct = default) =>
        await _db.Socios
            .Where(s => s.IdUsuario == idUsuario && s.IdGimnasio == idGimnasio)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync(ct);

    /// <summary>
    /// Asigna el rol Socio en esa sucursal si todavía no lo tiene. Misma lógica que usa el
    /// registro (CU-01); vive acá para que el alta automática y el alta al sumarse a otra
    /// sucursal (CU "Sumarse a otra sucursal") no la dupliquen cada una por su lado.
    /// </summary>
    public async Task AsignarRolDeSocioAsync(int idUsuario, int idGimnasio, CancellationToken ct = default)
    {
        var idRol = await _db.Roles
            .Where(r => r.Nombre == RolesDelSistema.Socio)
            .Select(r => r.Id)
            .FirstAsync(ct);

        var yaAsignado = await _db.UsuarioGimnasioRoles
            .AnyAsync(u => u.IdUsuario == idUsuario && u.IdGimnasio == idGimnasio && u.IdRol == idRol, ct);

        if (yaAsignado) return;

        _db.UsuarioGimnasioRoles.Add(new UsuarioGimnasioRol
        {
            IdUsuario = idUsuario,
            IdGimnasio = idGimnasio,
            IdRol = idRol,
            FechaDesde = _reloj.Hoy,
            Activo = true
        });

        await _db.SaveChangesAsync(ct);
    }
}

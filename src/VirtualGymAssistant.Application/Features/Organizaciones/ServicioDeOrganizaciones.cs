using Microsoft.EntityFrameworkCore;
using VirtualGymAssistant.Application.Common.Extensions;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Application.Features.Organizaciones.Dtos;
using VirtualGymAssistant.Domain.Entities;
using VirtualGymAssistant.Domain.Exceptions;

namespace VirtualGymAssistant.Application.Features.Organizaciones;

/// <summary>
/// Casos de uso de la organización (la cadena de gimnasios).
/// Todas las operaciones de escritura exigen ser el titular: la organización es el
/// tope de la jerarquía y no hay rol por encima del dueño.
/// </summary>
public class ServicioDeOrganizaciones
{
    private readonly IVirtualGymDbContext _db;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IProveedorDeFechaHora _reloj;

    public ServicioDeOrganizaciones(
        IVirtualGymDbContext db,
        IUsuarioActual usuarioActual,
        IProveedorDeFechaHora reloj)
    {
        _db = db;
        _usuarioActual = usuarioActual;
        _reloj = reloj;
    }

    /// <summary>Organizaciones de las que el usuario autenticado es titular.</summary>
    public async Task<IReadOnlyList<OrganizacionDto>> ListarMiasAsync(CancellationToken ct = default)
    {
        var idUsuario = _usuarioActual.IdRequerido;

        return await Proyectar(_db.Organizaciones.Where(o => o.IdDuenio == idUsuario))
            .OrderBy(o => o.Nombre)
            .ToListAsync(ct);
    }

    /// <summary>Detalle de una organización propia.</summary>
    public async Task<OrganizacionDto> ObtenerAsync(int id, CancellationToken ct = default)
    {
        await ExigirSerTitularAsync(id, ct);

        return await Proyectar(_db.Organizaciones.Where(o => o.Id == id)).FirstAsync(ct);
    }

    /// <summary>
    /// Caso de uso "Crear una organización".
    /// El usuario que la crea queda registrado como titular y, a partir de ahí, puede
    /// registrar sucursales.
    /// </summary>
    public async Task<OrganizacionDto> CrearAsync(CrearOrganizacionRequest request, CancellationToken ct = default)
    {
        var nombre = request.Nombre.Trim();

        // El nombre de la organización es único en todo el sistema (restricción de la base):
        // se valida acá para devolver un 409 claro en lugar de un error de SQL.
        if (await _db.Organizaciones.AnyAsync(o => o.Nombre == nombre, ct))
            throw new ConflictoException($"Ya existe una organización llamada \"{nombre}\".");

        var organizacion = new Organizacion
        {
            Nombre = nombre,
            RazonSocial = request.RazonSocial.NormalizarONull(),
            Cuit = request.Cuit.NormalizarONull(),
            IdDuenio = _usuarioActual.IdRequerido,
            Activo = true,
            FechaAlta = _reloj.Ahora
        };

        _db.Organizaciones.Add(organizacion);
        await _db.SaveChangesAsync(ct);

        return await ObtenerAsync(organizacion.Id, ct);
    }

    /// <summary>Caso de uso "Editar la organización".</summary>
    public async Task<OrganizacionDto> ActualizarAsync(
        int id, ActualizarOrganizacionRequest request, CancellationToken ct = default)
    {
        var organizacion = await ExigirSerTitularAsync(id, ct);
        var nombre = request.Nombre.Trim();

        if (await _db.Organizaciones.AnyAsync(o => o.Nombre == nombre && o.Id != id, ct))
            throw new ConflictoException($"Ya existe otra organización llamada \"{nombre}\".");

        organizacion.Nombre = nombre;
        organizacion.RazonSocial = request.RazonSocial.NormalizarONull();
        organizacion.Cuit = request.Cuit.NormalizarONull();
        organizacion.Activo = request.Activo;

        await _db.SaveChangesAsync(ct);

        return await ObtenerAsync(id, ct);
    }

    /// <summary>
    /// Baja lógica de la organización: deja de operar pero conserva todo su historial.
    /// Se bloquea si todavía tiene sucursales activas, para no dejar sedes huérfanas.
    /// </summary>
    public async Task DarDeBajaAsync(int id, CancellationToken ct = default)
    {
        var organizacion = await ExigirSerTitularAsync(id, ct);

        var sucursalesActivas = await _db.Gimnasios
            .CountAsync(g => g.IdOrganizacion == id && g.Activo, ct);

        if (sucursalesActivas > 0)
            throw new ReglaDeNegocioException(
                $"No se puede dar de baja la organización porque todavía tiene {sucursalesActivas} " +
                "sucursal(es) activa(s). Dá de baja primero las sucursales.");

        organizacion.Activo = false;
        await _db.SaveChangesAsync(ct);
    }

    // ------------------------------------------------------------------

    /// <summary>
    /// Trae la organización verificando que el usuario autenticado sea su titular.
    /// Se usa como guarda en todas las operaciones de escritura.
    /// </summary>
    private async Task<Organizacion> ExigirSerTitularAsync(int id, CancellationToken ct)
    {
        var organizacion = await _db.Organizaciones.FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NoEncontradoException("la organización", id);

        if (organizacion.IdDuenio != _usuarioActual.IdRequerido)
            throw new AccesoDenegadoException("No sos el titular de esta organización.");

        return organizacion;
    }

    /// <summary>Proyección común a DTO, para no repetir el Select en cada consulta.</summary>
    private static IQueryable<OrganizacionDto> Proyectar(IQueryable<Organizacion> consulta) =>
        consulta.Select(o => new OrganizacionDto
        {
            Id = o.Id,
            Nombre = o.Nombre,
            RazonSocial = o.RazonSocial,
            Cuit = o.Cuit,
            Activo = o.Activo,
            FechaAlta = o.FechaAlta,
            IdDuenio = o.IdDuenio,
            Duenio = o.Duenio.Nombre + " " + o.Duenio.Apellido,
            CantidadSucursales = o.Gimnasios.Count,
            CantidadSocios = o.Gimnasios.Sum(g => g.Socios.Count)
        });
}

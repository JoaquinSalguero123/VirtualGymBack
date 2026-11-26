using Microsoft.EntityFrameworkCore;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Application.Features.Membresias.Dtos;
using VirtualGymAssistant.Domain.Constants;
using VirtualGymAssistant.Domain.Entities;
using VirtualGymAssistant.Domain.Exceptions;

namespace VirtualGymAssistant.Application.Features.Membresias;

/// <summary>
/// Gestiona el vínculo socio-plan.
///
/// Regla central: un socio tiene como máximo UNA membresía activa. Cambiar de plan
/// significa cerrar la anterior y abrir una nueva, nunca editar la existente, para que
/// el historial quede completo.
/// </summary>
public class ServicioDeMembresias
{
    private readonly IVirtualGymDbContext _db;
    private readonly IServicioDeContexto _contexto;
    private readonly IProveedorDeFechaHora _reloj;

    public ServicioDeMembresias(
        IVirtualGymDbContext db,
        IServicioDeContexto contexto,
        IProveedorDeFechaHora reloj)
    {
        _db = db;
        _contexto = contexto;
        _reloj = reloj;
    }

    /// <summary>Historial de membresías de un socio de la sucursal activa.</summary>
    public async Task<IReadOnlyList<MembresiaDto>> ListarDeSocioAsync(
        int idSocio, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        var idGimnasio = contexto.GimnasioActivoRequerido;

        var socio = await _db.Socios.FirstOrDefaultAsync(s => s.Id == idSocio && s.IdGimnasio == idGimnasio, ct)
            ?? throw new NoEncontradoException("el socio", idSocio);

        // El socio ve las suyas; para ver las de otro hay que ser personal de mostrador.
        if (socio.IdUsuario != contexto.IdUsuario)
            contexto.ExigirRol(RolesDelSistema.Recepcionista, RolesDelSistema.Administrador);

        return await Proyectar(_db.Membresias.Where(m => m.IdSocio == idSocio))
            .OrderByDescending(m => m.Activa)
            .ThenByDescending(m => m.FechaInicio)
            .ToListAsync(ct);
    }

    /// <summary>Membresía vigente del socio, o null si no tiene ninguna.</summary>
    public async Task<MembresiaDto?> ObtenerActivaAsync(int idSocio, CancellationToken ct = default) =>
        await Proyectar(_db.Membresias.Where(m => m.IdSocio == idSocio && m.Activa))
            .FirstOrDefaultAsync(ct);

    /// <summary>
    /// Asigna un plan a un socio (alta o cambio de plan). Lo usa el personal de mostrador.
    /// </summary>
    public async Task<MembresiaDto> AsignarAsync(
        AsignarMembresiaRequest request, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Recepcionista, RolesDelSistema.Administrador);

        var idGimnasio = contexto.GimnasioActivoRequerido;

        var socio = await _db.Socios
            .FirstOrDefaultAsync(s => s.Id == request.IdSocio && s.IdGimnasio == idGimnasio, ct)
            ?? throw new NoEncontradoException("el socio", request.IdSocio);

        var plan = await ObtenerPlanDeLaOrganizacionAsync(request.IdPlan, contexto.OrganizacionActivaRequerida, ct);

        var membresia = await AsegurarMembresiaActivaAsync(
            socio.Id, idGimnasio, plan, request.PrecioAcordado, ct);

        await _db.SaveChangesAsync(ct);

        return await Proyectar(_db.Membresias.Where(m => m.Id == membresia.Id)).FirstAsync(ct);
    }

    /// <summary>Da de baja la membresía vigente del socio (por ejemplo, cuando se va del gimnasio).</summary>
    public async Task DarDeBajaActivaAsync(int idSocio, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Recepcionista, RolesDelSistema.Administrador);

        var idGimnasio = contexto.GimnasioActivoRequerido;

        if (!await _db.Socios.AnyAsync(s => s.Id == idSocio && s.IdGimnasio == idGimnasio, ct))
            throw new NoEncontradoException("el socio", idSocio);

        var activa = await _db.Membresias.FirstOrDefaultAsync(m => m.IdSocio == idSocio && m.Activa, ct)
            ?? throw new NoEncontradoException("El socio no tiene una membresía activa.");

        activa.Activa = false;
        activa.FechaFin = _reloj.Hoy;

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Deja al socio con una membresía activa del plan indicado.
    ///
    /// - Si ya tenía una del mismo plan, la reutiliza.
    /// - Si tenía una de otro plan, la cierra con fecha de hoy y crea la nueva.
    /// - Si no tenía ninguna, crea la primera.
    ///
    /// No llama a SaveChanges: la confirmación queda en manos de quien lo invoca, para
    /// que el alta de membresía y la aprobación del pago se guarden como una sola operación.
    /// </summary>
    public async Task<Membresia> AsegurarMembresiaActivaAsync(
        int idSocio,
        int idGimnasio,
        Plan plan,
        decimal? precioAcordado,
        CancellationToken ct = default)
    {
        var activa = await _db.Membresias.FirstOrDefaultAsync(m => m.IdSocio == idSocio && m.Activa, ct);

        if (activa is not null && activa.IdPlan == plan.Id)
            return activa;

        if (activa is not null)
        {
            activa.Activa = false;
            activa.FechaFin = _reloj.Hoy;
        }

        var nueva = new Membresia
        {
            IdSocio = idSocio,
            IdPlan = plan.Id,
            IdGimnasio = idGimnasio,
            FechaInicio = _reloj.Hoy,
            PrecioAcordado = precioAcordado ?? plan.Precio,
            Activa = true
        };

        _db.Membresias.Add(nueva);

        return nueva;
    }

    /// <summary>Trae un plan verificando que pertenezca a la organización en la que se opera.</summary>
    public async Task<Plan> ObtenerPlanDeLaOrganizacionAsync(
        int idPlan, int idOrganizacion, CancellationToken ct = default) =>
        await _db.Planes.FirstOrDefaultAsync(
            p => p.Id == idPlan && p.IdOrganizacion == idOrganizacion && p.Activo, ct)
        ?? throw new NoEncontradoException(
            "El plan no existe, no está vigente o no pertenece a esta organización.");

    // ------------------------------------------------------------------

    private static IQueryable<MembresiaDto> Proyectar(IQueryable<Membresia> consulta) =>
        consulta.Select(m => new MembresiaDto
        {
            Id = m.Id,
            IdSocio = m.IdSocio,
            Socio = (m.Socio.Usuario.Nombre + " " + m.Socio.Usuario.Apellido).Trim(),
            IdPlan = m.IdPlan,
            Plan = m.Plan.Nombre,
            IdGimnasio = m.IdGimnasio,
            Sucursal = m.Gimnasio.Nombre,
            FechaInicio = m.FechaInicio,
            FechaFin = m.FechaFin,
            PrecioAcordado = m.PrecioAcordado,
            Activa = m.Activa,
            AccesoMultiSede = m.Plan.AccesoMultiSede
        });
}

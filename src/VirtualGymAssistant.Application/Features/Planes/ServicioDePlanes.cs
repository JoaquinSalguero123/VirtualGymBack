using Microsoft.EntityFrameworkCore;
using VirtualGymAssistant.Application.Common.Extensions;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Application.Features.Planes.Dtos;
using VirtualGymAssistant.Domain.Constants;
using VirtualGymAssistant.Domain.Entities;
using VirtualGymAssistant.Domain.Exceptions;

namespace VirtualGymAssistant.Application.Features.Planes;

/// <summary>
/// Caso de uso "Administrar planes de la organización".
/// Los planes son de la organización (no de la sucursal), así que todas las sedes de
/// una misma cadena comparten la misma oferta comercial.
/// </summary>
public class ServicioDePlanes
{
    private readonly IVirtualGymDbContext _db;
    private readonly IServicioDeContexto _contexto;

    public ServicioDePlanes(IVirtualGymDbContext db, IServicioDeContexto contexto)
    {
        _db = db;
        _contexto = contexto;
    }

    /// <summary>
    /// Planes vigentes para el sitio público, sin necesidad de iniciar sesión.
    ///
    /// Un visitante tiene que poder comparar precios antes de crear la cuenta, así que
    /// este listado es anónimo. Devuelve sólo los planes activos: los dados de baja son
    /// información interna.
    /// </summary>
    /// <param name="idGimnasio">
    /// Sucursal desde la que se consulta. Si no se indica, se usa la organización de la
    /// casa matriz, igual que hace el resto del contenido público.
    /// </param>
    /// <param name="ct">Token de cancelación.</param>
    public async Task<IReadOnlyList<PlanDto>> ListarPublicosAsync(
        int? idGimnasio = null, CancellationToken ct = default)
    {
        var idOrganizacion = await ResolverOrganizacionPublicaAsync(idGimnasio, ct);

        if (idOrganizacion is null) return [];

        return await Proyectar(_db.Planes.Where(p => p.IdOrganizacion == idOrganizacion && p.Activo))
            .OrderBy(p => p.Precio)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Planes de la organización activa.
    /// Los socios sólo ven los planes vigentes; el personal puede pedir también los dados de baja.
    /// </summary>
    public async Task<IReadOnlyList<PlanDto>> ListarAsync(
        bool incluirInactivos = false, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        var idOrganizacion = contexto.OrganizacionActivaRequerida;

        // Sólo el personal puede ver los planes que ya no se ofrecen.
        var mostrarInactivos = incluirInactivos && contexto.EsStaff;

        var consulta = _db.Planes.Where(p => p.IdOrganizacion == idOrganizacion);

        if (!mostrarInactivos)
            consulta = consulta.Where(p => p.Activo);

        return await Proyectar(consulta).OrderBy(p => p.Precio).ToListAsync(ct);
    }

    /// <summary>Detalle de un plan de la organización activa.</summary>
    public async Task<PlanDto> ObtenerAsync(int id, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        var idOrganizacion = contexto.OrganizacionActivaRequerida;

        return await Proyectar(_db.Planes.Where(p => p.Id == id && p.IdOrganizacion == idOrganizacion))
            .FirstOrDefaultAsync(ct)
            ?? throw new NoEncontradoException("el plan", id);
    }

    /// <summary>Crea un plan en la organización activa.</summary>
    public async Task<PlanDto> CrearAsync(CrearPlanRequest request, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Administrador);

        var idOrganizacion = contexto.OrganizacionActivaRequerida;
        var nombre = request.Nombre.Trim();

        if (await _db.Planes.AnyAsync(p => p.IdOrganizacion == idOrganizacion && p.Nombre == nombre, ct))
            throw new ConflictoException($"Ya existe un plan llamado \"{nombre}\" en esta organización.");

        var plan = new Plan
        {
            IdOrganizacion = idOrganizacion,
            Nombre = nombre,
            Descripcion = request.Descripcion.NormalizarONull(),
            Precio = request.Precio,
            DuracionDias = request.DuracionDias,
            Nivel = request.Nivel.NormalizarONull(),
            AccesoMultiSede = request.AccesoMultiSede,
            Activo = true
        };

        _db.Planes.Add(plan);
        await _db.SaveChangesAsync(ct);

        return await ObtenerAsync(plan.Id, ct);
    }

    /// <summary>
    /// Modifica un plan. Cambiar el precio no altera lo que ya se le cobró a los socios:
    /// cada membresía guarda su propio precio acordado.
    /// </summary>
    public async Task<PlanDto> ActualizarAsync(
        int id, ActualizarPlanRequest request, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Administrador);

        var idOrganizacion = contexto.OrganizacionActivaRequerida;

        var plan = await _db.Planes
            .FirstOrDefaultAsync(p => p.Id == id && p.IdOrganizacion == idOrganizacion, ct)
            ?? throw new NoEncontradoException("el plan", id);

        var nombre = request.Nombre.Trim();

        if (await _db.Planes.AnyAsync(
                p => p.IdOrganizacion == idOrganizacion && p.Nombre == nombre && p.Id != id, ct))
            throw new ConflictoException($"Ya existe otro plan llamado \"{nombre}\" en esta organización.");

        plan.Nombre = nombre;
        plan.Descripcion = request.Descripcion.NormalizarONull();
        plan.Precio = request.Precio;
        plan.DuracionDias = request.DuracionDias;
        plan.Nivel = request.Nivel.NormalizarONull();
        plan.AccesoMultiSede = request.AccesoMultiSede;
        plan.Activo = request.Activo;

        await _db.SaveChangesAsync(ct);

        return await ObtenerAsync(id, ct);
    }

    /// <summary>
    /// Baja lógica del plan: deja de ofrecerse a socios nuevos.
    /// No se borra físicamente porque los pagos históricos lo referencian.
    /// </summary>
    public async Task DarDeBajaAsync(int id, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Administrador);

        var idOrganizacion = contexto.OrganizacionActivaRequerida;

        var plan = await _db.Planes
            .FirstOrDefaultAsync(p => p.Id == id && p.IdOrganizacion == idOrganizacion, ct)
            ?? throw new NoEncontradoException("el plan", id);

        plan.Activo = false;
        await _db.SaveChangesAsync(ct);
    }

    // ------------------------------------------------------------------

    /// <summary>
    /// Resuelve la organización cuando no hay usuario autenticado: sale de la sucursal
    /// indicada o, si no se indica ninguna, de la casa matriz.
    /// </summary>
    private async Task<int?> ResolverOrganizacionPublicaAsync(int? idGimnasio, CancellationToken ct)
    {
        if (idGimnasio is { } id)
        {
            return await _db.Gimnasios
                .Where(g => g.Id == id && g.Activo)
                .Select(g => (int?)g.IdOrganizacion)
                .FirstOrDefaultAsync(ct);
        }

        return await _db.Gimnasios
            .Where(g => g.Activo && g.Organizacion.Activo)
            .OrderByDescending(g => g.EsCasaMatriz)
            .ThenBy(g => g.Id)
            .Select(g => (int?)g.IdOrganizacion)
            .FirstOrDefaultAsync(ct);
    }

    private static IQueryable<PlanDto> Proyectar(IQueryable<Plan> consulta) =>
        consulta.Select(p => new PlanDto
        {
            Id = p.Id,
            IdOrganizacion = p.IdOrganizacion,
            Nombre = p.Nombre,
            Descripcion = p.Descripcion,
            Precio = p.Precio,
            DuracionDias = p.DuracionDias,
            Nivel = p.Nivel,
            AccesoMultiSede = p.AccesoMultiSede,
            Activo = p.Activo,
            SociosConEstePlan = p.Membresias.Count(m => m.Activa)
        });
}

using Microsoft.EntityFrameworkCore;
using VirtualGymAssistant.Application.Common.Extensions;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Application.Features.Ejercicios.Dtos;
using VirtualGymAssistant.Domain.Constants;
using VirtualGymAssistant.Domain.Entities;
using VirtualGymAssistant.Domain.Exceptions;

namespace VirtualGymAssistant.Application.Features.Ejercicios;

/// <summary>
/// Caso de uso "Administrar ejercicios y grupos musculares".
/// El catálogo de ejercicios es global: lo comparten todas las organizaciones, por eso
/// su edición está limitada a entrenadores y administradores.
/// </summary>
public class ServicioDeEjercicios
{
    private readonly IVirtualGymDbContext _db;
    private readonly IServicioDeContexto _contexto;

    public ServicioDeEjercicios(IVirtualGymDbContext db, IServicioDeContexto contexto)
    {
        _db = db;
        _contexto = contexto;
    }

    /// <summary>Lista el catálogo, con filtro opcional por grupo muscular y por texto.</summary>
    public async Task<IReadOnlyList<EjercicioDto>> ListarAsync(
        int? idGrupo = null, string? texto = null, CancellationToken ct = default)
    {
        var consulta = _db.Ejercicios.AsQueryable();

        if (idGrupo is { } grupo)
            consulta = consulta.Where(e => e.IdGrupo == grupo);

        if (texto.NormalizarONull() is { } filtro)
            consulta = consulta.Where(e => e.Nombre.Contains(filtro));

        return await Proyectar(consulta)
            .OrderBy(e => e.Grupo)
            .ThenBy(e => e.Nombre)
            .ToListAsync(ct);
    }

    public async Task<EjercicioDto> ObtenerAsync(int id, CancellationToken ct = default) =>
        await Proyectar(_db.Ejercicios.Where(e => e.Id == id)).FirstOrDefaultAsync(ct)
        ?? throw new NoEncontradoException("el ejercicio", id);

    public async Task<EjercicioDto> CrearAsync(
        GuardarEjercicioRequest request, CancellationToken ct = default)
    {
        await ExigirPermisoAsync(ct);

        var nombre = request.Nombre.Trim();

        if (await _db.Ejercicios.AnyAsync(e => e.Nombre == nombre, ct))
            throw new ConflictoException($"Ya existe un ejercicio llamado \"{nombre}\".");

        await ExigirGrupoExistenteAsync(request.IdGrupo, ct);

        var ejercicio = new Ejercicio
        {
            Nombre = nombre,
            Descripcion = request.Descripcion.NormalizarONull(),
            IdGrupo = request.IdGrupo,
            VideoUrl = request.VideoUrl.NormalizarONull()
        };

        _db.Ejercicios.Add(ejercicio);
        await _db.SaveChangesAsync(ct);

        return await ObtenerAsync(ejercicio.Id, ct);
    }

    public async Task<EjercicioDto> ActualizarAsync(
        int id, GuardarEjercicioRequest request, CancellationToken ct = default)
    {
        await ExigirPermisoAsync(ct);

        var ejercicio = await _db.Ejercicios.FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new NoEncontradoException("el ejercicio", id);

        var nombre = request.Nombre.Trim();

        if (await _db.Ejercicios.AnyAsync(e => e.Nombre == nombre && e.Id != id, ct))
            throw new ConflictoException($"Ya existe otro ejercicio llamado \"{nombre}\".");

        await ExigirGrupoExistenteAsync(request.IdGrupo, ct);

        ejercicio.Nombre = nombre;
        ejercicio.Descripcion = request.Descripcion.NormalizarONull();
        ejercicio.IdGrupo = request.IdGrupo;
        ejercicio.VideoUrl = request.VideoUrl.NormalizarONull();

        await _db.SaveChangesAsync(ct);

        return await ObtenerAsync(id, ct);
    }

    /// <summary>
    /// Elimina un ejercicio. Se bloquea si ya forma parte de alguna rutina, porque
    /// borrarlo dejaría rutinas incompletas.
    /// </summary>
    public async Task EliminarAsync(int id, CancellationToken ct = default)
    {
        await ExigirPermisoAsync(ct);

        var ejercicio = await _db.Ejercicios.FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new NoEncontradoException("el ejercicio", id);

        var usos = await _db.RutinaDetalles.CountAsync(d => d.IdEjercicio == id, ct)
                 + await _db.SocioRutinaDetalles.CountAsync(d => d.IdEjercicio == id, ct);

        if (usos > 0)
            throw new ReglaDeNegocioException(
                $"No se puede eliminar el ejercicio porque forma parte de {usos} rutina(s).");

        _db.Ejercicios.Remove(ejercicio);
        await _db.SaveChangesAsync(ct);
    }

    // ------------------------------------------------------------------

    private async Task ExigirPermisoAsync(CancellationToken ct)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Entrenador, RolesDelSistema.Administrador);
    }

    private async Task ExigirGrupoExistenteAsync(int idGrupo, CancellationToken ct)
    {
        if (!await _db.GruposMusculares.AnyAsync(g => g.Id == idGrupo, ct))
            throw new NoEncontradoException("el grupo muscular", idGrupo);
    }

    private static IQueryable<EjercicioDto> Proyectar(IQueryable<Ejercicio> consulta) =>
        consulta.Select(e => new EjercicioDto
        {
            Id = e.Id,
            Nombre = e.Nombre,
            Descripcion = e.Descripcion,
            IdGrupo = e.IdGrupo,
            Grupo = e.Grupo.Nombre,
            VideoUrl = e.VideoUrl,
            Usos = e.EnPlantillas.Count + e.EnRutinasDeSocios.Count
        });
}

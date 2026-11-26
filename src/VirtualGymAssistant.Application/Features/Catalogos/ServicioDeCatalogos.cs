using Microsoft.EntityFrameworkCore;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Application.Features.Catalogos.Dtos;
using VirtualGymAssistant.Domain.Constants;
using VirtualGymAssistant.Domain.Entities;
using VirtualGymAssistant.Domain.Exceptions;

namespace VirtualGymAssistant.Application.Features.Catalogos;

/// <summary>
/// ABM de los tres catálogos simples del sistema: objetivos, grupos musculares y
/// categorías de alimentos.
///
/// Están juntos porque se comportan igual (id + nombre único, baja sólo si nadie los usa)
/// y cambian por los mismos motivos. Lo único que los diferencia es quién puede editarlos
/// y qué tablas los referencian, y eso queda explícito en cada método.
/// </summary>
public class ServicioDeCatalogos
{
    private readonly IVirtualGymDbContext _db;
    private readonly IServicioDeContexto _contexto;

    public ServicioDeCatalogos(IVirtualGymDbContext db, IServicioDeContexto contexto)
    {
        _db = db;
        _contexto = contexto;
    }

    // ================= OBJETIVOS =================

    /// <summary>Objetivos de entrenamiento y alimentación.</summary>
    public async Task<IReadOnlyList<ItemDeCatalogoDto>> ListarObjetivosAsync(CancellationToken ct = default) =>
        await _db.Objetivos
            .OrderBy(o => o.Nombre)
            .Select(o => new ItemDeCatalogoDto
            {
                Id = o.Id,
                Nombre = o.Nombre,
                Usos = o.Alimentos.Count + o.Socios.Count + o.Rutinas.Count
            })
            .ToListAsync(ct);

    public async Task<ItemDeCatalogoDto> CrearObjetivoAsync(
        GuardarItemDeCatalogoRequest request, CancellationToken ct = default)
    {
        await ExigirRolAsync(ct, RolesDelSistema.Entrenador, RolesDelSistema.Nutricionista,
                             RolesDelSistema.Administrador);

        var nombre = await ExigirNombreLibreAsync(request.Nombre,
            n => _db.Objetivos.AnyAsync(o => o.Nombre == n, ct), "objetivo");

        var objetivo = new Objetivo { Nombre = nombre };
        _db.Objetivos.Add(objetivo);
        await _db.SaveChangesAsync(ct);

        return new ItemDeCatalogoDto { Id = objetivo.Id, Nombre = objetivo.Nombre, Usos = 0 };
    }

    public async Task<ItemDeCatalogoDto> ActualizarObjetivoAsync(
        int id, GuardarItemDeCatalogoRequest request, CancellationToken ct = default)
    {
        await ExigirRolAsync(ct, RolesDelSistema.Entrenador, RolesDelSistema.Nutricionista,
                             RolesDelSistema.Administrador);

        var objetivo = await _db.Objetivos.FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NoEncontradoException("el objetivo", id);

        objetivo.Nombre = await ExigirNombreLibreAsync(request.Nombre,
            n => _db.Objetivos.AnyAsync(o => o.Nombre == n && o.Id != id, ct), "objetivo");

        await _db.SaveChangesAsync(ct);

        return (await ListarObjetivosAsync(ct)).First(o => o.Id == id);
    }

    public async Task EliminarObjetivoAsync(int id, CancellationToken ct = default)
    {
        await ExigirRolAsync(ct, RolesDelSistema.Entrenador, RolesDelSistema.Nutricionista,
                             RolesDelSistema.Administrador);

        var objetivo = await _db.Objetivos.FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NoEncontradoException("el objetivo", id);

        var usos = await _db.Alimentos.CountAsync(a => a.IdObjetivo == id, ct)
                 + await _db.Socios.CountAsync(s => s.IdObjetivo == id, ct)
                 + await _db.Rutinas.CountAsync(r => r.IdObjetivo == id, ct);

        ExigirSinUsos(usos, "el objetivo");

        _db.Objetivos.Remove(objetivo);
        await _db.SaveChangesAsync(ct);
    }

    // ============= GRUPOS MUSCULARES =============

    /// <summary>Grupos musculares que clasifican los ejercicios.</summary>
    public async Task<IReadOnlyList<ItemDeCatalogoDto>> ListarGruposMuscularesAsync(
        CancellationToken ct = default) =>
        await _db.GruposMusculares
            .OrderBy(g => g.Nombre)
            .Select(g => new ItemDeCatalogoDto
            {
                Id = g.Id,
                Nombre = g.Nombre,
                Usos = g.Ejercicios.Count
            })
            .ToListAsync(ct);

    public async Task<ItemDeCatalogoDto> CrearGrupoMuscularAsync(
        GuardarItemDeCatalogoRequest request, CancellationToken ct = default)
    {
        await ExigirRolAsync(ct, RolesDelSistema.Entrenador, RolesDelSistema.Administrador);

        var nombre = await ExigirNombreLibreAsync(request.Nombre,
            n => _db.GruposMusculares.AnyAsync(g => g.Nombre == n, ct), "grupo muscular");

        var grupo = new GrupoMuscular { Nombre = nombre };
        _db.GruposMusculares.Add(grupo);
        await _db.SaveChangesAsync(ct);

        return new ItemDeCatalogoDto { Id = grupo.Id, Nombre = grupo.Nombre, Usos = 0 };
    }

    public async Task<ItemDeCatalogoDto> ActualizarGrupoMuscularAsync(
        int id, GuardarItemDeCatalogoRequest request, CancellationToken ct = default)
    {
        await ExigirRolAsync(ct, RolesDelSistema.Entrenador, RolesDelSistema.Administrador);

        var grupo = await _db.GruposMusculares.FirstOrDefaultAsync(g => g.Id == id, ct)
            ?? throw new NoEncontradoException("el grupo muscular", id);

        grupo.Nombre = await ExigirNombreLibreAsync(request.Nombre,
            n => _db.GruposMusculares.AnyAsync(g => g.Nombre == n && g.Id != id, ct), "grupo muscular");

        await _db.SaveChangesAsync(ct);

        return (await ListarGruposMuscularesAsync(ct)).First(g => g.Id == id);
    }

    public async Task EliminarGrupoMuscularAsync(int id, CancellationToken ct = default)
    {
        await ExigirRolAsync(ct, RolesDelSistema.Entrenador, RolesDelSistema.Administrador);

        var grupo = await _db.GruposMusculares.FirstOrDefaultAsync(g => g.Id == id, ct)
            ?? throw new NoEncontradoException("el grupo muscular", id);

        var usos = await _db.Ejercicios.CountAsync(e => e.IdGrupo == id, ct);
        ExigirSinUsos(usos, "el grupo muscular");

        _db.GruposMusculares.Remove(grupo);
        await _db.SaveChangesAsync(ct);
    }

    // ========= CATEGORÍAS DE ALIMENTOS =========

    /// <summary>Categorías nutricionales de los alimentos.</summary>
    public async Task<IReadOnlyList<ItemDeCatalogoDto>> ListarCategoriasDeAlimentosAsync(
        CancellationToken ct = default) =>
        await _db.AlimentosCategorias
            .OrderBy(c => c.Nombre)
            .Select(c => new ItemDeCatalogoDto
            {
                Id = c.Id,
                Nombre = c.Nombre,
                Usos = c.Alimentos.Count
            })
            .ToListAsync(ct);

    public async Task<ItemDeCatalogoDto> CrearCategoriaDeAlimentoAsync(
        GuardarItemDeCatalogoRequest request, CancellationToken ct = default)
    {
        await ExigirRolAsync(ct, RolesDelSistema.Nutricionista, RolesDelSistema.Administrador);

        var nombre = await ExigirNombreLibreAsync(request.Nombre,
            n => _db.AlimentosCategorias.AnyAsync(c => c.Nombre == n, ct), "categoría");

        var categoria = new AlimentoCategoria { Nombre = nombre };
        _db.AlimentosCategorias.Add(categoria);
        await _db.SaveChangesAsync(ct);

        return new ItemDeCatalogoDto { Id = categoria.Id, Nombre = categoria.Nombre, Usos = 0 };
    }

    public async Task<ItemDeCatalogoDto> ActualizarCategoriaDeAlimentoAsync(
        int id, GuardarItemDeCatalogoRequest request, CancellationToken ct = default)
    {
        await ExigirRolAsync(ct, RolesDelSistema.Nutricionista, RolesDelSistema.Administrador);

        var categoria = await _db.AlimentosCategorias.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NoEncontradoException("la categoría", id);

        categoria.Nombre = await ExigirNombreLibreAsync(request.Nombre,
            n => _db.AlimentosCategorias.AnyAsync(c => c.Nombre == n && c.Id != id, ct), "categoría");

        await _db.SaveChangesAsync(ct);

        return (await ListarCategoriasDeAlimentosAsync(ct)).First(c => c.Id == id);
    }

    public async Task EliminarCategoriaDeAlimentoAsync(int id, CancellationToken ct = default)
    {
        await ExigirRolAsync(ct, RolesDelSistema.Nutricionista, RolesDelSistema.Administrador);

        var categoria = await _db.AlimentosCategorias.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NoEncontradoException("la categoría", id);

        var usos = await _db.Alimentos.CountAsync(a => a.IdCategoria == id, ct);
        ExigirSinUsos(usos, "la categoría");

        _db.AlimentosCategorias.Remove(categoria);
        await _db.SaveChangesAsync(ct);
    }

    // ------------------------------------------------------------------

    private async Task ExigirRolAsync(CancellationToken ct, params string[] roles)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(roles);
    }

    /// <summary>Normaliza el nombre y verifica que no exista otro igual en el catálogo.</summary>
    private static async Task<string> ExigirNombreLibreAsync(
        string nombreCrudo, Func<string, Task<bool>> yaExiste, string queEs)
    {
        var nombre = nombreCrudo.Trim();

        if (await yaExiste(nombre))
            throw new ConflictoException($"Ya existe un/a {queEs} con el nombre \"{nombre}\".");

        return nombre;
    }

    /// <summary>Impide borrar un item que todavía referencian otras tablas.</summary>
    private static void ExigirSinUsos(int usos, string queEs)
    {
        if (usos > 0)
            throw new ReglaDeNegocioException(
                $"No se puede eliminar {queEs} porque está en uso en {usos} registro(s). " +
                "Reasigná esos registros antes de borrarlo.");
    }
}

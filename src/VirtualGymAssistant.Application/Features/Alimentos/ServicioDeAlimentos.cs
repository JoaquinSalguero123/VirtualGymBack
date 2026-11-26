using Microsoft.EntityFrameworkCore;
using VirtualGymAssistant.Application.Common.Extensions;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Application.Features.Alimentos.Dtos;
using VirtualGymAssistant.Domain.Constants;
using VirtualGymAssistant.Domain.Entities;
using VirtualGymAssistant.Domain.Exceptions;

namespace VirtualGymAssistant.Application.Features.Alimentos;

/// <summary>
/// Caso de uso "Administrar alimentos y categorías".
/// El catálogo alimenta el armado de dietas, tanto las automáticas como las manuales.
/// </summary>
public class ServicioDeAlimentos
{
    private readonly IVirtualGymDbContext _db;
    private readonly IServicioDeContexto _contexto;

    public ServicioDeAlimentos(IVirtualGymDbContext db, IServicioDeContexto contexto)
    {
        _db = db;
        _contexto = contexto;
    }

    /// <summary>Lista el catálogo con filtros por categoría, objetivo y texto.</summary>
    public async Task<IReadOnlyList<AlimentoDto>> ListarAsync(
        int? idCategoria = null,
        int? idObjetivo = null,
        string? texto = null,
        CancellationToken ct = default)
    {
        var consulta = _db.Alimentos.AsQueryable();

        if (idCategoria is { } categoria)
            consulta = consulta.Where(a => a.IdCategoria == categoria);

        if (idObjetivo is { } objetivo)
            consulta = consulta.Where(a => a.IdObjetivo == objetivo);

        if (texto.NormalizarONull() is { } filtro)
            consulta = consulta.Where(a => a.Nombre.Contains(filtro));

        return await Proyectar(consulta).OrderBy(a => a.Nombre).ToListAsync(ct);
    }

    public async Task<AlimentoDto> ObtenerAsync(int id, CancellationToken ct = default) =>
        await Proyectar(_db.Alimentos.Where(a => a.Id == id)).FirstOrDefaultAsync(ct)
        ?? throw new NoEncontradoException("el alimento", id);

    public async Task<AlimentoDto> CrearAsync(
        GuardarAlimentoRequest request, CancellationToken ct = default)
    {
        await ExigirPermisoAsync(ct);

        var nombre = request.Nombre.Trim();

        if (await _db.Alimentos.AnyAsync(a => a.Nombre == nombre, ct))
            throw new ConflictoException($"Ya existe un alimento llamado \"{nombre}\".");

        await ExigirReferenciasValidasAsync(request.IdCategoria, request.IdObjetivo, ct);

        var alimento = new Alimento
        {
            Nombre = nombre,
            Calorias = request.Calorias,
            Proteinas = request.Proteinas,
            Carbohidratos = request.Carbohidratos,
            Grasas = request.Grasas,
            Descripcion = request.Descripcion.NormalizarONull(),
            IdCategoria = request.IdCategoria,
            IdObjetivo = request.IdObjetivo
        };

        _db.Alimentos.Add(alimento);
        await _db.SaveChangesAsync(ct);

        return await ObtenerAsync(alimento.Id, ct);
    }

    public async Task<AlimentoDto> ActualizarAsync(
        int id, GuardarAlimentoRequest request, CancellationToken ct = default)
    {
        await ExigirPermisoAsync(ct);

        var alimento = await _db.Alimentos.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NoEncontradoException("el alimento", id);

        var nombre = request.Nombre.Trim();

        if (await _db.Alimentos.AnyAsync(a => a.Nombre == nombre && a.Id != id, ct))
            throw new ConflictoException($"Ya existe otro alimento llamado \"{nombre}\".");

        await ExigirReferenciasValidasAsync(request.IdCategoria, request.IdObjetivo, ct);

        alimento.Nombre = nombre;
        alimento.Calorias = request.Calorias;
        alimento.Proteinas = request.Proteinas;
        alimento.Carbohidratos = request.Carbohidratos;
        alimento.Grasas = request.Grasas;
        alimento.Descripcion = request.Descripcion.NormalizarONull();
        alimento.IdCategoria = request.IdCategoria;
        alimento.IdObjetivo = request.IdObjetivo;

        await _db.SaveChangesAsync(ct);

        return await ObtenerAsync(id, ct);
    }

    /// <summary>
    /// Elimina un alimento. Si ya figura en la dieta de algún socio se corta la operación,
    /// para no dejar planes de alimentación con referencias rotas.
    /// </summary>
    public async Task EliminarAsync(int id, CancellationToken ct = default)
    {
        await ExigirPermisoAsync(ct);

        var alimento = await _db.Alimentos.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NoEncontradoException("el alimento", id);

        var usos = await _db.SocioDietaDetalles.CountAsync(d => d.IdAlimento == id, ct);

        if (usos > 0)
            throw new ReglaDeNegocioException(
                $"No se puede eliminar el alimento porque forma parte de {usos} dieta(s).");

        _db.Alimentos.Remove(alimento);
        await _db.SaveChangesAsync(ct);
    }

    // ------------------------------------------------------------------

    private async Task ExigirPermisoAsync(CancellationToken ct)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Nutricionista, RolesDelSistema.Administrador);
    }

    private async Task ExigirReferenciasValidasAsync(int? idCategoria, int? idObjetivo, CancellationToken ct)
    {
        if (idCategoria is { } categoria &&
            !await _db.AlimentosCategorias.AnyAsync(c => c.Id == categoria, ct))
            throw new NoEncontradoException("la categoría de alimento", categoria);

        if (idObjetivo is { } objetivo &&
            !await _db.Objetivos.AnyAsync(o => o.Id == objetivo, ct))
            throw new NoEncontradoException("el objetivo", objetivo);
    }

    private static IQueryable<AlimentoDto> Proyectar(IQueryable<Alimento> consulta) =>
        consulta.Select(a => new AlimentoDto
        {
            Id = a.Id,
            Nombre = a.Nombre,
            Calorias = a.Calorias,
            Proteinas = a.Proteinas,
            Carbohidratos = a.Carbohidratos,
            Grasas = a.Grasas,
            Descripcion = a.Descripcion,
            IdCategoria = a.IdCategoria,
            Categoria = a.Categoria != null ? a.Categoria.Nombre : null,
            IdObjetivo = a.IdObjetivo,
            Objetivo = a.Objetivo != null ? a.Objetivo.Nombre : null
        });
}

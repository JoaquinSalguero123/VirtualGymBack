using Microsoft.EntityFrameworkCore;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Application.Features.FormasDePago.Dtos;
using VirtualGymAssistant.Domain.Constants;
using VirtualGymAssistant.Domain.Entities;
using VirtualGymAssistant.Domain.Exceptions;

namespace VirtualGymAssistant.Application.Features.FormasDePago;

/// <summary>
/// Caso de uso "Administrar formas de pago".
/// Es un catálogo compartido por todo el sistema, así que su ABM queda reservado al
/// Administrador.
/// </summary>
public class ServicioDeFormasDePago
{
    private readonly IVirtualGymDbContext _db;
    private readonly IServicioDeContexto _contexto;

    public ServicioDeFormasDePago(IVirtualGymDbContext db, IServicioDeContexto contexto)
    {
        _db = db;
        _contexto = contexto;
    }

    /// <summary>
    /// Lista las formas de pago. Por defecto sólo las vigentes, que son las que puede
    /// elegir un socio al pagar.
    /// </summary>
    public async Task<IReadOnlyList<FormaDePagoDto>> ListarAsync(
        bool incluirInactivas = false, CancellationToken ct = default)
    {
        var consulta = _db.FormasPago.AsQueryable();

        if (!incluirInactivas)
            consulta = consulta.Where(f => f.Activo);

        return await Proyectar(consulta).OrderBy(f => f.Nombre).ToListAsync(ct);
    }

    public async Task<FormaDePagoDto> ObtenerAsync(int id, CancellationToken ct = default) =>
        await Proyectar(_db.FormasPago.Where(f => f.Id == id)).FirstOrDefaultAsync(ct)
        ?? throw new NoEncontradoException("la forma de pago", id);

    /// <summary>Crea una forma de pago.</summary>
    public async Task<FormaDePagoDto> CrearAsync(
        GuardarFormaDePagoRequest request, CancellationToken ct = default)
    {
        await ExigirAdministradorAsync(ct);

        var nombre = request.Nombre.Trim();

        if (await _db.FormasPago.AnyAsync(f => f.Nombre == nombre, ct))
            throw new ConflictoException($"Ya existe una forma de pago llamada \"{nombre}\".");

        var forma = new FormaPago { Nombre = nombre, Activo = request.Activo };

        _db.FormasPago.Add(forma);
        await _db.SaveChangesAsync(ct);

        return await ObtenerAsync(forma.Id, ct);
    }

    /// <summary>Modifica el nombre o la vigencia de una forma de pago.</summary>
    public async Task<FormaDePagoDto> ActualizarAsync(
        int id, GuardarFormaDePagoRequest request, CancellationToken ct = default)
    {
        await ExigirAdministradorAsync(ct);

        var forma = await _db.FormasPago.FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new NoEncontradoException("la forma de pago", id);

        var nombre = request.Nombre.Trim();

        if (await _db.FormasPago.AnyAsync(f => f.Nombre == nombre && f.Id != id, ct))
            throw new ConflictoException($"Ya existe otra forma de pago llamada \"{nombre}\".");

        forma.Nombre = nombre;
        forma.Activo = request.Activo;

        await _db.SaveChangesAsync(ct);

        return await ObtenerAsync(id, ct);
    }

    /// <summary>
    /// Elimina la forma de pago. Si ya fue usada en algún pago se la da de baja en vez
    /// de borrarla, para no romper el historial de cobranzas.
    /// </summary>
    public async Task EliminarAsync(int id, CancellationToken ct = default)
    {
        await ExigirAdministradorAsync(ct);

        var forma = await _db.FormasPago.FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new NoEncontradoException("la forma de pago", id);

        var estaEnUso = await _db.Pagos.AnyAsync(p => p.IdFormaPago == id, ct);

        if (estaEnUso)
            forma.Activo = false;
        else
            _db.FormasPago.Remove(forma);

        await _db.SaveChangesAsync(ct);
    }

    // ------------------------------------------------------------------

    private async Task ExigirAdministradorAsync(CancellationToken ct)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Administrador);
    }

    private static IQueryable<FormaDePagoDto> Proyectar(IQueryable<FormaPago> consulta) =>
        consulta.Select(f => new FormaDePagoDto
        {
            Id = f.Id,
            Nombre = f.Nombre,
            Activo = f.Activo,
            PagosRegistrados = f.Pagos.Count
        });
}

using Microsoft.EntityFrameworkCore;
using VirtualGymAssistant.Application.Common.Extensions;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Application.Features.SitioWeb.Dtos;
using VirtualGymAssistant.Domain.Constants;
using VirtualGymAssistant.Domain.Entities;
using VirtualGymAssistant.Domain.Exceptions;

namespace VirtualGymAssistant.Application.Features.SitioWeb;

/// <summary>
/// Casos de uso "Configurar el carrusel del sitio" y "Configurar el pie de página".
///
/// El contenido es de la organización, así que lo comparten todas sus sucursales, salvo
/// que un item del carrusel se marque para una sede puntual.
/// Las consultas son públicas: el sitio las muestra sin pedir login.
/// </summary>
public class ServicioDeSitioWeb
{
    private readonly IVirtualGymDbContext _db;
    private readonly IServicioDeContexto _contexto;
    private readonly IAlmacenDeArchivos _archivos;

    public ServicioDeSitioWeb(
        IVirtualGymDbContext db,
        IServicioDeContexto contexto,
        IAlmacenDeArchivos archivos)
    {
        _db = db;
        _contexto = contexto;
        _archivos = archivos;
    }

    // ================= CARRUSEL =================

    /// <summary>
    /// Items del carrusel para el sitio público. Se resuelve la organización a partir
    /// de la sucursal, o se usa la casa matriz si no se indica ninguna.
    /// </summary>
    public async Task<IReadOnlyList<CarouselItemDto>> ListarCarouselPublicoAsync(
        string? tipo = null, int? idGimnasio = null, CancellationToken ct = default)
    {
        var idOrganizacion = await ResolverOrganizacionPublicaAsync(idGimnasio, ct);
        if (idOrganizacion is null) return [];

        var consulta = _db.CarouselItems
            .Where(c => c.IdOrganizacion == idOrganizacion && c.Activo);

        // Se muestran los items de la organización más los específicos de esa sucursal.
        if (idGimnasio is { } gimnasio)
            consulta = consulta.Where(c => c.IdGimnasio == null || c.IdGimnasio == gimnasio);

        if (tipo.NormalizarONull() is { } filtro)
            consulta = consulta.Where(c => c.Tipo == filtro);

        return await Proyectar(consulta).ToListAsync(ct);
    }

    /// <summary>Items del carrusel de la organización activa, incluidos los desactivados.</summary>
    public async Task<IReadOnlyList<CarouselItemDto>> ListarCarouselAsync(
        string? tipo = null, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        var idOrganizacion = contexto.OrganizacionActivaRequerida;

        var consulta = _db.CarouselItems.Where(c => c.IdOrganizacion == idOrganizacion);

        if (tipo.NormalizarONull() is { } filtro)
            consulta = consulta.Where(c => c.Tipo == filtro);

        return await Proyectar(consulta).ToListAsync(ct);
    }

    /// <summary>Crea un item del carrusel, con imagen opcional.</summary>
    public async Task<CarouselItemDto> CrearCarouselItemAsync(
        GuardarCarouselItemRequest request,
        Stream? imagen = null,
        string? nombreDeImagen = null,
        CancellationToken ct = default)
    {
        var contexto = await ExigirPermisoDeCarouselAsync(ct);
        var idOrganizacion = contexto.OrganizacionActivaRequerida;

        if (request.IdGimnasio is { } idGimnasio)
            contexto.ExigirAccesoA(idGimnasio);

        var item = new CarouselItem
        {
            IdOrganizacion = idOrganizacion,
            IdGimnasio = request.IdGimnasio,
            Tipo = request.Tipo.NormalizarONull(),
            Titulo = request.Titulo.NormalizarONull(),
            Descripcion = request.Descripcion.NormalizarONull(),
            Orden = request.Orden,
            Activo = request.Activo,
            Imagen = imagen is not null && nombreDeImagen is not null
                ? await _archivos.GuardarAsync(imagen, nombreDeImagen, ct)
                : null
        };

        _db.CarouselItems.Add(item);
        await _db.SaveChangesAsync(ct);

        return await Proyectar(_db.CarouselItems.Where(c => c.Id == item.Id)).FirstAsync(ct);
    }

    /// <summary>
    /// Modifica un item. Si se envía una imagen nueva, se reemplaza la anterior y se
    /// borra el archivo viejo para no dejar basura en el disco.
    /// </summary>
    public async Task<CarouselItemDto> ActualizarCarouselItemAsync(
        int id,
        GuardarCarouselItemRequest request,
        Stream? imagen = null,
        string? nombreDeImagen = null,
        CancellationToken ct = default)
    {
        var contexto = await ExigirPermisoDeCarouselAsync(ct);
        var idOrganizacion = contexto.OrganizacionActivaRequerida;

        var item = await _db.CarouselItems
            .FirstOrDefaultAsync(c => c.Id == id && c.IdOrganizacion == idOrganizacion, ct)
            ?? throw new NoEncontradoException("el item del carrusel", id);

        if (request.IdGimnasio is { } idGimnasio)
            contexto.ExigirAccesoA(idGimnasio);

        item.IdGimnasio = request.IdGimnasio;
        item.Tipo = request.Tipo.NormalizarONull();
        item.Titulo = request.Titulo.NormalizarONull();
        item.Descripcion = request.Descripcion.NormalizarONull();
        item.Orden = request.Orden;
        item.Activo = request.Activo;

        if (imagen is not null && nombreDeImagen is not null)
        {
            var anterior = item.Imagen;
            item.Imagen = await _archivos.GuardarAsync(imagen, nombreDeImagen, ct);
            _archivos.Eliminar(anterior);
        }

        await _db.SaveChangesAsync(ct);

        return await Proyectar(_db.CarouselItems.Where(c => c.Id == id)).FirstAsync(ct);
    }

    /// <summary>Elimina el item y su imagen asociada.</summary>
    public async Task EliminarCarouselItemAsync(int id, CancellationToken ct = default)
    {
        var contexto = await ExigirPermisoDeCarouselAsync(ct);
        var idOrganizacion = contexto.OrganizacionActivaRequerida;

        var item = await _db.CarouselItems
            .FirstOrDefaultAsync(c => c.Id == id && c.IdOrganizacion == idOrganizacion, ct)
            ?? throw new NoEncontradoException("el item del carrusel", id);

        var imagen = item.Imagen;

        _db.CarouselItems.Remove(item);
        await _db.SaveChangesAsync(ct);

        _archivos.Eliminar(imagen);
    }

    // ================= PIE DE PÁGINA =================

    /// <summary>
    /// Datos del pie agrupados por sección, listos para dibujar el footer del sitio.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, List<FooterConfigDto>>> ObtenerFooterPublicoAsync(
        int? idGimnasio = null, CancellationToken ct = default)
    {
        var idOrganizacion = await ResolverOrganizacionPublicaAsync(idGimnasio, ct);

        if (idOrganizacion is null)
            return new Dictionary<string, List<FooterConfigDto>>();

        var items = await _db.FooterConfigs
            .Where(f => f.IdOrganizacion == idOrganizacion)
            .OrderBy(f => f.Seccion)
            .ThenBy(f => f.Orden ?? 999)
            .Select(f => new FooterConfigDto
            {
                Id = f.Id,
                IdOrganizacion = f.IdOrganizacion,
                Seccion = f.Seccion,
                Clave = f.Clave,
                Valor = f.Valor,
                Orden = f.Orden
            })
            .ToListAsync(ct);

        return items
            .GroupBy(f => f.Seccion)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>Agrega un dato al pie de la organización activa.</summary>
    public async Task<FooterConfigDto> CrearFooterConfigAsync(
        GuardarFooterConfigRequest request, CancellationToken ct = default)
    {
        var idOrganizacion = await ExigirPermisoDeFooterAsync(ct);

        var seccion = request.Seccion.Trim();
        var clave = request.Clave.Trim();

        if (await _db.FooterConfigs.AnyAsync(
                f => f.IdOrganizacion == idOrganizacion && f.Seccion == seccion && f.Clave == clave, ct))
            throw new ConflictoException(
                $"Ya existe un dato con la clave \"{clave}\" en la sección \"{seccion}\".");

        var item = new FooterConfig
        {
            IdOrganizacion = idOrganizacion,
            Seccion = seccion,
            Clave = clave,
            Valor = request.Valor.NormalizarONull(),
            Orden = request.Orden
        };

        _db.FooterConfigs.Add(item);
        await _db.SaveChangesAsync(ct);

        return ProyectarFooter(item);
    }

    /// <summary>Modifica un dato del pie.</summary>
    public async Task<FooterConfigDto> ActualizarFooterConfigAsync(
        int id, GuardarFooterConfigRequest request, CancellationToken ct = default)
    {
        var idOrganizacion = await ExigirPermisoDeFooterAsync(ct);

        var item = await _db.FooterConfigs
            .FirstOrDefaultAsync(f => f.Id == id && f.IdOrganizacion == idOrganizacion, ct)
            ?? throw new NoEncontradoException("el dato del pie de página", id);

        item.Seccion = request.Seccion.Trim();
        item.Clave = request.Clave.Trim();
        item.Valor = request.Valor.NormalizarONull();
        item.Orden = request.Orden;

        await _db.SaveChangesAsync(ct);

        return ProyectarFooter(item);
    }

    /// <summary>Elimina un dato del pie.</summary>
    public async Task EliminarFooterConfigAsync(int id, CancellationToken ct = default)
    {
        var idOrganizacion = await ExigirPermisoDeFooterAsync(ct);

        var item = await _db.FooterConfigs
            .FirstOrDefaultAsync(f => f.Id == id && f.IdOrganizacion == idOrganizacion, ct)
            ?? throw new NoEncontradoException("el dato del pie de página", id);

        _db.FooterConfigs.Remove(item);
        await _db.SaveChangesAsync(ct);
    }

    // ------------------------------------------------------------------

    /// <summary>
    /// Para el sitio público no hay usuario logueado: la organización sale de la sucursal
    /// indicada o, si no se indica ninguna, de la casa matriz.
    /// </summary>
    private async Task<int?> ResolverOrganizacionPublicaAsync(int? idGimnasio, CancellationToken ct)
    {
        if (idGimnasio is { } id)
        {
            return await _db.Gimnasios
                .Where(g => g.Id == id)
                .Select(g => (int?)g.IdOrganizacion)
                .FirstOrDefaultAsync(ct);
        }

        return await _db.Gimnasios
            .Where(g => g.Activo)
            .OrderByDescending(g => g.EsCasaMatriz)
            .ThenBy(g => g.Id)
            .Select(g => (int?)g.IdOrganizacion)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<Common.Models.ContextoDeUsuario> ExigirPermisoDeCarouselAsync(CancellationToken ct)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Administrador, RolesDelSistema.Entrenador);

        return contexto;
    }

    private async Task<int> ExigirPermisoDeFooterAsync(CancellationToken ct)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Administrador);

        return contexto.OrganizacionActivaRequerida;
    }

    private static IQueryable<CarouselItemDto> Proyectar(IQueryable<CarouselItem> consulta) =>
        consulta
            .OrderBy(c => c.Orden ?? 999)
            .ThenBy(c => c.Id)
            .Select(c => new CarouselItemDto
            {
                Id = c.Id,
                IdOrganizacion = c.IdOrganizacion,
                IdGimnasio = c.IdGimnasio,
                Sucursal = c.Gimnasio != null ? c.Gimnasio.Nombre : null,
                Tipo = c.Tipo,
                Titulo = c.Titulo,
                Descripcion = c.Descripcion,
                Imagen = c.Imagen,
                Orden = c.Orden,
                Activo = c.Activo
            });

    private static FooterConfigDto ProyectarFooter(FooterConfig f) => new()
    {
        Id = f.Id,
        IdOrganizacion = f.IdOrganizacion,
        Seccion = f.Seccion,
        Clave = f.Clave,
        Valor = f.Valor,
        Orden = f.Orden
    };
}

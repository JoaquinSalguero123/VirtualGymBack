using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using VirtualGymAssistant.Application.Common.Extensions;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Application.Common.Models;
using VirtualGymAssistant.Application.Features.Foro.Dtos;
using VirtualGymAssistant.Domain.Constants;
using VirtualGymAssistant.Domain.Entities;
using VirtualGymAssistant.Domain.Exceptions;

namespace VirtualGymAssistant.Application.Features.Foro;

/// <summary>
/// Caso de uso "Participar del foro".
///
/// Se ven las publicaciones de la sucursal activa más las que son de toda la organización.
/// Cada uno borra las suyas; el personal de mostrador para arriba puede moderar cualquiera.
/// </summary>
public class ServicioDeForo
{
    private readonly IVirtualGymDbContext _db;
    private readonly IServicioDeContexto _contexto;
    private readonly IProveedorDeFechaHora _reloj;
    private readonly IAlmacenDeArchivos _archivos;

    public ServicioDeForo(
        IVirtualGymDbContext db,
        IServicioDeContexto contexto,
        IProveedorDeFechaHora reloj,
        IAlmacenDeArchivos archivos)
    {
        _db = db;
        _contexto = contexto;
        _reloj = reloj;
        _archivos = archivos;
    }

    /// <summary>
    /// Publicaciones visibles, de la más nueva a la más vieja, con sus respuestas anidadas.
    /// </summary>
    public async Task<ResultadoPaginado<PublicacionDto>> ListarAsync(
        ParametrosDePaginacion paginacion, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        var idGimnasio = contexto.GimnasioActivoRequerido;
        var idOrganizacion = contexto.OrganizacionActivaRequerida;
        var puedeModerar = PuedeModerar(contexto);

        // Sólo se paginan las publicaciones raíz; las respuestas viajan con su padre.
        var raices = await _db.ForoPosts
            .Where(p => p.IdPostPadre == null
                        && (p.IdGimnasio == idGimnasio
                            || (p.IdGimnasio == null && p.IdOrganizacion == idOrganizacion)))
            .OrderByDescending(p => p.Fecha)
            .Skip(paginacion.Salteo)
            .Take(paginacion.TamanioPagina)
            .Select(ProyeccionCruda(idGimnasio))
            .ToListAsync(ct);

        var total = await _db.ForoPosts
            .CountAsync(p => p.IdPostPadre == null
                             && (p.IdGimnasio == idGimnasio
                                 || (p.IdGimnasio == null && p.IdOrganizacion == idOrganizacion)), ct);

        var idsRaices = raices.Select(r => r.Id).ToList();

        var respuestas = await _db.ForoPosts
            .Where(p => p.IdPostPadre != null && idsRaices.Contains(p.IdPostPadre!.Value)
                        && (p.IdGimnasio == idGimnasio
                            || (p.IdGimnasio == null && p.IdOrganizacion == idOrganizacion)))
            .OrderBy(p => p.Fecha)
            .Select(ProyeccionCruda(idGimnasio))
            .ToListAsync(ct);

        var respuestasPorPadre = respuestas.ToLookup(r => r.IdPostPadre);

        var items = raices
            .Select(raiz => Convertir(raiz, contexto.IdUsuario, puedeModerar,
                respuestasPorPadre[raiz.Id]
                    .Select(r => Convertir(r, contexto.IdUsuario, puedeModerar, []))
                    .ToList()))
            .ToList();

        return ResultadoPaginado<PublicacionDto>.Crear(
            items, total, paginacion.Pagina, paginacion.TamanioPagina);
    }

    /// <summary>Publica un mensaje en el foro de la sucursal activa.</summary>
    public async Task<PublicacionDto> PublicarAsync(
        CrearPublicacionRequest request,
        Stream? archivo = null,
        string? nombreDeArchivo = null,
        CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        var idGimnasio = contexto.GimnasioActivoRequerido;
        var idOrganizacion = contexto.OrganizacionActivaRequerida;

        var contenido = request.Contenido.NormalizarONull();
        var tieneArchivo = archivo is not null && nombreDeArchivo is not null;

        if (contenido is null && !tieneArchivo)
            throw new ReglaDeNegocioException("La publicación no puede estar vacía.");

        if (request.IdPostPadre is { } idPadre)
        {
            var padreExiste = await _db.ForoPosts
                .AnyAsync(p => p.Id == idPadre && p.IdPostPadre == null
                               && (p.IdGimnasio == idGimnasio
                                   || (p.IdGimnasio == null && p.IdOrganizacion == idOrganizacion)), ct);

            if (!padreExiste)
                throw new NoEncontradoException("la publicación a la que querés responder", idPadre);
        }

        var post = new ForoPost
        {
            IdOrganizacion = contexto.OrganizacionActivaRequerida,
            IdGimnasio = idGimnasio,
            IdUsuario = contexto.IdUsuario,
            IdPostPadre = request.IdPostPadre,
            Contenido = contenido,
            Fecha = _reloj.Ahora,
            Archivo = tieneArchivo
                ? await _archivos.GuardarAsync(archivo!, nombreDeArchivo!, ct)
                : null
        };

        _db.ForoPosts.Add(post);
        await _db.SaveChangesAsync(ct);

        var crudo = await _db.ForoPosts
            .Where(p => p.Id == post.Id)
            .Select(ProyeccionCruda(idGimnasio))
            .FirstAsync(ct);

        return Convertir(crudo, contexto.IdUsuario, PuedeModerar(contexto), []);
    }

    /// <summary>
    /// Elimina una publicación. El autor puede borrar la suya; el personal puede moderar
    /// cualquiera de su sucursal. Al borrar una publicación raíz se van sus respuestas.
    /// </summary>
    public async Task EliminarAsync(int id, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        var idGimnasio = contexto.GimnasioActivoRequerido;
        var idOrganizacion = contexto.OrganizacionActivaRequerida;

        var post = await _db.ForoPosts
            .FirstOrDefaultAsync(p => p.Id == id
                                      && (p.IdGimnasio == idGimnasio
                                          || (p.IdGimnasio == null && p.IdOrganizacion == idOrganizacion)), ct)
            ?? throw new NoEncontradoException("la publicación", id);

        if (post.IdUsuario != contexto.IdUsuario && !PuedeModerar(contexto))
            throw new AccesoDenegadoException("Sólo podés eliminar tus propias publicaciones.");

        var respuestas = await _db.ForoPosts.Where(p => p.IdPostPadre == id).ToListAsync(ct);

        var archivos = respuestas.Select(r => r.Archivo).Append(post.Archivo).ToList();

        _db.ForoPosts.RemoveRange(respuestas);
        _db.ForoPosts.Remove(post);
        await _db.SaveChangesAsync(ct);

        foreach (var archivo in archivos)
            _archivos.Eliminar(archivo);
    }

    // ------------------------------------------------------------------

    /// <summary>El personal de mostrador para arriba puede moderar el foro.</summary>
    private static bool PuedeModerar(ContextoDeUsuario contexto) =>
        contexto.AlcanzaNivel(RolesDelSistema.Recepcionista);

    /// <summary>
    /// Proyección que se puede traducir a SQL. Los campos que dependen de quién consulta
    /// se completan después, en <see cref="Convertir"/>.
    ///
    /// Tiene que devolver una expresión (no llamar a un método común desde el lambda de
    /// Select) para que EF la traduzca en un JOIN: llamar a un método normal desde adentro
    /// de un Select no es traducible, así que EF terminaba trayendo las filas de
    /// ForoPosts sin nada más y recién ahí explotaba al tocar p.Usuario en el cliente
    /// (siempre null, porque nunca se había cargado).
    /// </summary>
    private static Expression<Func<ForoPost, PublicacionCruda>> ProyeccionCruda(int idGimnasio) => p => new PublicacionCruda
    {
        Id = p.Id,
        IdUsuario = p.IdUsuario,
        Usuario = (p.Usuario.Nombre + " " + p.Usuario.Apellido).Trim(),
        RolDelAutor = p.Usuario.RolesPorSucursal
            .Where(r => r.IdGimnasio == idGimnasio && r.Activo)
            .Select(r => r.Rol.Nombre)
            .FirstOrDefault(),
        IdGimnasio = p.IdGimnasio,
        Sucursal = p.Gimnasio != null ? p.Gimnasio.Nombre : null,
        IdPostPadre = p.IdPostPadre,
        Contenido = p.Contenido,
        Archivo = p.Archivo,
        Fecha = p.Fecha
    };

    private static PublicacionDto Convertir(
        PublicacionCruda cruda, int idUsuarioActual, bool puedeModerar, List<PublicacionDto> respuestas)
    {
        var esMia = cruda.IdUsuario == idUsuarioActual;

        return new PublicacionDto
        {
            Id = cruda.Id,
            IdUsuario = cruda.IdUsuario,
            Usuario = cruda.Usuario,
            RolDelAutor = cruda.RolDelAutor,
            IdGimnasio = cruda.IdGimnasio,
            Sucursal = cruda.Sucursal,
            IdPostPadre = cruda.IdPostPadre,
            Contenido = cruda.Contenido,
            Archivo = cruda.Archivo,
            Fecha = cruda.Fecha,
            EsMia = esMia,
            PuedeEliminar = esMia || puedeModerar,
            Respuestas = respuestas
        };
    }

    /// <summary>Fila tal como sale de la base, antes de aplicarle el punto de vista del usuario.</summary>
    private class PublicacionCruda
    {
        public int Id { get; init; }
        public int IdUsuario { get; init; }
        public string Usuario { get; init; } = string.Empty;
        public string? RolDelAutor { get; init; }
        public int? IdGimnasio { get; init; }
        public string? Sucursal { get; init; }
        public int? IdPostPadre { get; init; }
        public string? Contenido { get; init; }
        public string? Archivo { get; init; }
        public DateTime Fecha { get; init; }
    }
}

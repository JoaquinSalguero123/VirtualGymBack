using Microsoft.EntityFrameworkCore;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Application.Common.Models;
using VirtualGymAssistant.Domain.Constants;
using VirtualGymAssistant.Domain.Exceptions;

namespace VirtualGymAssistant.Application.Features.Contexto;

/// <summary>
/// Arma el contexto multi-sucursal del usuario.
///
/// Un usuario puede llegar a una sucursal por tres caminos distintos:
///   1. Es dueño de la organización a la que pertenece la sucursal.
///   2. Tiene un rol de personal asignado en esa sucursal.
///   3. Es socio de esa sucursal.
///
/// Como puede darse más de un camino a la vez (por ejemplo, el dueño que además entrena
/// en su propio gimnasio), se queda con el rol de mayor jerarquía para cada sucursal.
/// </summary>
public class ServicioDeContexto : IServicioDeContexto
{
    private readonly IVirtualGymDbContext _db;
    private readonly IUsuarioActual _usuarioActual;

    /// <summary>
    /// Caché por petición. El servicio se registra con ciclo de vida Scoped, así que
    /// este campo vive lo que dura el request y evita repetir la consulta.
    /// </summary>
    private ContextoDeUsuario? _contextoCacheado;

    public ServicioDeContexto(IVirtualGymDbContext db, IUsuarioActual usuarioActual)
    {
        _db = db;
        _usuarioActual = usuarioActual;
    }

    public async Task<ContextoDeUsuario> ObtenerAsync(CancellationToken ct = default)
    {
        if (_contextoCacheado is not null) return _contextoCacheado;

        if (!_usuarioActual.EstaAutenticado || _usuarioActual.Id is not { } idUsuario)
            throw new NoAutenticadoException("La petición requiere un usuario autenticado.");

        var sucursales = await ObtenerSucursalesAsync(idUsuario, ct);

        var esDuenio = await _db.Organizaciones
            .AnyAsync(o => o.IdDuenio == idUsuario, ct);

        // Resolución de la sucursal activa:
        //   - Si el front pidió una sucursal concreta, tiene que ser una a la que el
        //     usuario realmente pueda acceder. Si no lo es se corta con 403 en vez de
        //     caer en otra sede en silencio: de lo contrario, un recepcionista podría
        //     creer que está cobrando en una sucursal y estar cobrando en otra.
        //   - Si no pidió ninguna, se usa la primera de su lista (la casa matriz si la tiene).
        SucursalDelUsuario? activa;

        if (_usuarioActual.IdGimnasioSolicitado is { } idPedido)
        {
            activa = sucursales.FirstOrDefault(s => s.IdGimnasio == idPedido)
                ?? throw new AccesoDenegadoException(
                    $"No tenés acceso a la sucursal {idPedido}, o la sucursal no existe.");
        }
        else
        {
            activa = sucursales.FirstOrDefault();
        }

        _contextoCacheado = new ContextoDeUsuario
        {
            IdUsuario = idUsuario,
            Sucursales = sucursales,
            EsDuenio = esDuenio,
            SucursalActiva = activa
        };

        return _contextoCacheado;
    }

    public async Task<IReadOnlyList<SucursalDelUsuario>> ObtenerSucursalesAsync(
        int idUsuario, CancellationToken ct = default)
    {
        // Caminos 1 y 3: acá "Rol" es una constante de C# (RolesDelSistema.Duenio /
        // .Socio), no una columna. Si se la asigna dentro de un Select traducido a
        // SQL, EF Core la manda como literal de la consulta, y ese literal volvía
        // roto ("DueÃ±o" en vez de "Dueño") -- un problema puntual de esa traducción,
        // no del archivo fuente ni de la base, que están bien. Por eso acá se trae
        // primero lo que sí sale de la base y el Rol se asigna después, en memoria
        // managed, fuera de cualquier traducción a SQL.

        // Camino 1: dueño de la organización.
        var comoDuenio = (await _db.Gimnasios
            .Where(g => g.Activo && g.Organizacion.IdDuenio == idUsuario)
            .Select(g => new
            {
                g.Id,
                GimnasioNombre = g.Nombre,
                g.IdOrganizacion,
                OrganizacionNombre = g.Organizacion.Nombre,
                g.EsCasaMatriz
            })
            .ToListAsync(ct))
            .Select(g => new SucursalDelUsuario
            {
                IdGimnasio = g.Id,
                Gimnasio = g.GimnasioNombre,
                IdOrganizacion = g.IdOrganizacion,
                Organizacion = g.OrganizacionNombre,
                EsCasaMatriz = g.EsCasaMatriz,
                Rol = RolesDelSistema.Duenio
            });

        // Camino 2: personal con rol asignado en la sucursal. Acá "Rol" sí sale de
        // la base (ugr.Rol.Nombre), así que no tiene este problema.
        var comoPersonal = await _db.UsuarioGimnasioRoles
            .Where(ugr => ugr.IdUsuario == idUsuario && ugr.Activo && ugr.Gimnasio.Activo)
            .Select(ugr => new SucursalDelUsuario
            {
                IdGimnasio = ugr.IdGimnasio,
                Gimnasio = ugr.Gimnasio.Nombre,
                IdOrganizacion = ugr.Gimnasio.IdOrganizacion,
                Organizacion = ugr.Gimnasio.Organizacion.Nombre,
                EsCasaMatriz = ugr.Gimnasio.EsCasaMatriz,
                Rol = ugr.Rol.Nombre
            })
            .ToListAsync(ct);

        // Camino 3: socio de la sucursal.
        var comoSocio = (await _db.Socios
            .Where(s => s.IdUsuario == idUsuario && s.Gimnasio.Activo)
            .Select(s => new
            {
                s.IdGimnasio,
                GimnasioNombre = s.Gimnasio.Nombre,
                s.Gimnasio.IdOrganizacion,
                OrganizacionNombre = s.Gimnasio.Organizacion.Nombre,
                s.Gimnasio.EsCasaMatriz
            })
            .ToListAsync(ct))
            .Select(s => new SucursalDelUsuario
            {
                IdGimnasio = s.IdGimnasio,
                Gimnasio = s.GimnasioNombre,
                IdOrganizacion = s.IdOrganizacion,
                Organizacion = s.OrganizacionNombre,
                EsCasaMatriz = s.EsCasaMatriz,
                Rol = RolesDelSistema.Socio
            });

        var filas = comoDuenio
            .Concat(comoPersonal)
            .Concat(comoSocio)
            .ToList();

        // Una sucursal puede aparecer varias veces (distintos roles): nos quedamos con el más alto.
        return filas
            .GroupBy(f => f.IdGimnasio)
            .Select(grupo => grupo
                .OrderByDescending(f => RolesDelSistema.Nivel(f.Rol))
                .First())
            .OrderByDescending(s => s.EsCasaMatriz)
            .ThenBy(s => s.Gimnasio)
            .ToList();
    }
}

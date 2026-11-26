using Microsoft.EntityFrameworkCore;
using VirtualGymAssistant.Application.Common.Extensions;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Application.Features.Sucursales.Dtos;
using VirtualGymAssistant.Domain.Constants;
using VirtualGymAssistant.Domain.Entities;
using VirtualGymAssistant.Domain.Exceptions;

namespace VirtualGymAssistant.Application.Features.Sucursales;

/// <summary>
/// Casos de uso del módulo "Organización y sucursales".
///
/// Quién puede hacer qué:
///   - Registrar, marcar casa matriz y dar de baja: sólo el Dueño de la organización.
///   - Editar datos de contacto: Dueño o Administrador de esa sucursal.
///   - Consultar: cualquier usuario, limitado a las sucursales de su contexto.
/// </summary>
public class ServicioDeSucursales
{
    private readonly IVirtualGymDbContext _db;
    private readonly IServicioDeContexto _contexto;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IProveedorDeFechaHora _reloj;

    public ServicioDeSucursales(
        IVirtualGymDbContext db,
        IServicioDeContexto contexto,
        IUsuarioActual usuarioActual,
        IProveedorDeFechaHora reloj)
    {
        _db = db;
        _contexto = contexto;
        _usuarioActual = usuarioActual;
        _reloj = reloj;
    }

    /// <summary>
    /// Sucursales activas del sistema, sin datos sensibles. Es el único endpoint de
    /// sucursales que no requiere token: lo consume el formulario de registro.
    /// </summary>
    public async Task<IReadOnlyList<SucursalPublicaDto>> ListarPublicasAsync(CancellationToken ct = default) =>
        await _db.Gimnasios
            .Where(g => g.Activo && g.Organizacion.Activo)
            .OrderByDescending(g => g.EsCasaMatriz)
            .ThenBy(g => g.Nombre)
            .Select(g => new SucursalPublicaDto
            {
                Id = g.Id,
                Nombre = g.Nombre,
                Organizacion = g.Organizacion.Nombre,
                Direccion = g.Direccion,
                Latitud = g.Latitud,
                Longitud = g.Longitud,
                EsCasaMatriz = g.EsCasaMatriz
            })
            .ToListAsync(ct);

    /// <summary>
    /// Sucursales que ve el usuario autenticado.
    /// El dueño y el administrador ven todas las de la organización activa (incluso las
    /// dadas de baja, para poder reactivarlas); el resto ve sólo aquellas en las que opera.
    /// </summary>
    public async Task<IReadOnlyList<SucursalDto>> ListarVisiblesAsync(CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);

        if (contexto.SucursalActiva is null)
            return [];

        if (contexto.TieneRol(RolesDelSistema.Administrador))
        {
            var idOrganizacion = contexto.OrganizacionActivaRequerida;

            return await Proyectar(_db.Gimnasios.Where(g => g.IdOrganizacion == idOrganizacion))
                .OrderByDescending(g => g.EsCasaMatriz)
                .ThenBy(g => g.Nombre)
                .ToListAsync(ct);
        }

        var idsVisibles = contexto.Sucursales.Select(s => s.IdGimnasio).ToList();

        return await Proyectar(_db.Gimnasios.Where(g => idsVisibles.Contains(g.Id)))
            .OrderByDescending(g => g.EsCasaMatriz)
            .ThenBy(g => g.Nombre)
            .ToListAsync(ct);
    }

    /// <summary>Detalle de una sucursal a la que el usuario tiene acceso.</summary>
    public async Task<SucursalDto> ObtenerAsync(int id, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirAccesoA(id);

        return await Proyectar(_db.Gimnasios.Where(g => g.Id == id)).FirstOrDefaultAsync(ct)
            ?? throw new NoEncontradoException("la sucursal", id);
    }

    /// <summary>
    /// Caso de uso "Registrar una sucursal".
    /// Verifica que quien la crea sea el titular de la organización y que el nombre no
    /// se repita dentro de ella.
    /// </summary>
    public async Task<SucursalDto> CrearAsync(CrearSucursalRequest request, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);

        // Si no se indica organización, se toma la de la sucursal en la que está parado.
        var idOrganizacion = request.IdOrganizacion ?? contexto.IdOrganizacionActiva
            ?? throw new ReglaDeNegocioException(
                "Indicá la organización en la que querés crear la sucursal.");

        await ExigirSerTitularAsync(idOrganizacion, ct);

        var nombre = request.Nombre.Trim();

        if (await _db.Gimnasios.AnyAsync(g => g.IdOrganizacion == idOrganizacion && g.Nombre == nombre, ct))
            throw new ConflictoException(
                $"Ya existe una sucursal llamada \"{nombre}\" en esta organización.");

        var sucursal = new Gimnasio
        {
            IdOrganizacion = idOrganizacion,
            Nombre = nombre,
            Direccion = request.Direccion.Trim(),
            Latitud = RedondearCoordenada(request.Latitud),
            Longitud = RedondearCoordenada(request.Longitud),
            IdZona = request.IdZona,
            Telefono = request.Telefono.NormalizarONull(),
            Email = request.Email.NormalizarONull()?.ToLowerInvariant(),
            EsCasaMatriz = false,
            Activo = true,
            FechaAlta = _reloj.Ahora
        };

        _db.Gimnasios.Add(sucursal);
        await _db.SaveChangesAsync(ct);

        // La primera sucursal de una organización es siempre la casa matriz, aunque no
        // se haya pedido: una organización no puede quedar sin sede principal.
        var esLaPrimera = await _db.Gimnasios.CountAsync(g => g.IdOrganizacion == idOrganizacion, ct) == 1;

        if (request.EsCasaMatriz || esLaPrimera)
            await MarcarCasaMatrizAsync(sucursal.Id, ct);

        return await Proyectar(_db.Gimnasios.Where(g => g.Id == sucursal.Id)).FirstAsync(ct);
    }

    /// <summary>Caso de uso "Editar una sucursal" (datos de contacto y ubicación).</summary>
    public async Task<SucursalDto> ActualizarAsync(
        int id, ActualizarSucursalRequest request, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirAccesoA(id);
        contexto.ExigirRol(RolesDelSistema.Administrador);

        var sucursal = await _db.Gimnasios.FirstOrDefaultAsync(g => g.Id == id, ct)
            ?? throw new NoEncontradoException("la sucursal", id);

        var nombre = request.Nombre.Trim();

        if (await _db.Gimnasios.AnyAsync(
                g => g.IdOrganizacion == sucursal.IdOrganizacion && g.Nombre == nombre && g.Id != id, ct))
            throw new ConflictoException(
                $"Ya existe otra sucursal llamada \"{nombre}\" en esta organización.");

        sucursal.Nombre = nombre;
        sucursal.Direccion = request.Direccion.Trim();
        sucursal.Latitud = RedondearCoordenada(request.Latitud);
        sucursal.Longitud = RedondearCoordenada(request.Longitud);
        sucursal.IdZona = request.IdZona;
        sucursal.Telefono = request.Telefono.NormalizarONull();
        sucursal.Email = request.Email.NormalizarONull()?.ToLowerInvariant();

        await _db.SaveChangesAsync(ct);

        return await Proyectar(_db.Gimnasios.Where(g => g.Id == id)).FirstAsync(ct);
    }

    /// <summary>
    /// Caso de uso "Marcar la Casa Central".
    /// Sólo puede haber una por organización, así que se limpia la marca de las demás.
    /// </summary>
    public async Task MarcarCasaMatrizAsync(int id, CancellationToken ct = default)
    {
        var sucursal = await _db.Gimnasios.FirstOrDefaultAsync(g => g.Id == id, ct)
            ?? throw new NoEncontradoException("la sucursal", id);

        await ExigirSerTitularAsync(sucursal.IdOrganizacion, ct);

        if (!sucursal.Activo)
            throw new ReglaDeNegocioException(
                "Una sucursal dada de baja no puede ser la casa matriz.");

        var hermanas = await _db.Gimnasios
            .Where(g => g.IdOrganizacion == sucursal.IdOrganizacion)
            .ToListAsync(ct);

        foreach (var hermana in hermanas)
            hermana.EsCasaMatriz = hermana.Id == id;

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Caso de uso "Dar de baja una sucursal".
    /// Es una baja lógica: la sede deja de aparecer y de recibir operaciones, pero se
    /// conservan sus socios, pagos y asistencias históricas.
    /// </summary>
    public async Task DarDeBajaAsync(int id, CancellationToken ct = default)
    {
        var sucursal = await _db.Gimnasios.FirstOrDefaultAsync(g => g.Id == id, ct)
            ?? throw new NoEncontradoException("la sucursal", id);

        await ExigirSerTitularAsync(sucursal.IdOrganizacion, ct);

        if (!sucursal.Activo) return;

        // La organización no puede quedarse sin ninguna sucursal operativa.
        var otrasActivas = await _db.Gimnasios
            .CountAsync(g => g.IdOrganizacion == sucursal.IdOrganizacion && g.Activo && g.Id != id, ct);

        if (otrasActivas == 0)
            throw new ReglaDeNegocioException(
                "No se puede dar de baja la única sucursal activa de la organización.");

        sucursal.Activo = false;

        // Si era la casa matriz, el rol pasa a otra sucursal activa para no dejar la
        // organización sin sede principal.
        if (sucursal.EsCasaMatriz)
        {
            sucursal.EsCasaMatriz = false;

            var reemplazo = await _db.Gimnasios
                .Where(g => g.IdOrganizacion == sucursal.IdOrganizacion && g.Activo && g.Id != id)
                .OrderBy(g => g.Id)
                .FirstAsync(ct);

            reemplazo.EsCasaMatriz = true;
        }

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Reactiva una sucursal dada de baja.</summary>
    public async Task ReactivarAsync(int id, CancellationToken ct = default)
    {
        var sucursal = await _db.Gimnasios.FirstOrDefaultAsync(g => g.Id == id, ct)
            ?? throw new NoEncontradoException("la sucursal", id);

        await ExigirSerTitularAsync(sucursal.IdOrganizacion, ct);

        sucursal.Activo = true;
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Catálogo de zonas geográficas para el selector del formulario.</summary>
    public async Task<IReadOnlyList<ZonaDto>> ListarZonasAsync(CancellationToken ct = default) =>
        await _db.Zonas
            .OrderBy(z => z.Nombre)
            .Select(z => new ZonaDto
            {
                Id = z.Id,
                Nombre = z.Nombre,
                Localidad = z.Localidad,
                Provincia = z.Provincia
            })
            .ToListAsync(ct);

    // ------------------------------------------------------------------

    /// <summary>Verifica que el usuario autenticado sea el titular de la organización.</summary>
    private async Task ExigirSerTitularAsync(int idOrganizacion, CancellationToken ct)
    {
        var esTitular = await _db.Organizaciones
            .AnyAsync(o => o.Id == idOrganizacion && o.IdDuenio == _usuarioActual.IdRequerido, ct);

        if (!esTitular)
            throw new AccesoDenegadoException(
                "Sólo el titular de la organización puede realizar esta operación sobre sus sucursales.");
    }

    /// <summary>
    /// La columna es DECIMAL(9,6): seis decimales son unos 11 centímetros, de sobra para ubicar
    /// un local. Se redondea acá para que un valor con más decimales (por ejemplo, copiado de
    /// Google Maps) no dependa de cómo lo recorte el motor al guardar.
    /// </summary>
    private static decimal? RedondearCoordenada(decimal? valor) =>
        valor is { } v ? Math.Round(v, 6) : null;

    private static IQueryable<SucursalDto> Proyectar(IQueryable<Gimnasio> consulta) =>
        consulta.Select(g => new SucursalDto
        {
            Id = g.Id,
            IdOrganizacion = g.IdOrganizacion,
            Organizacion = g.Organizacion.Nombre,
            Nombre = g.Nombre,
            Direccion = g.Direccion,
            Latitud = g.Latitud,
            Longitud = g.Longitud,
            IdZona = g.IdZona,
            Zona = g.Zona != null ? g.Zona.Nombre : null,
            Telefono = g.Telefono,
            Email = g.Email,
            EsCasaMatriz = g.EsCasaMatriz,
            Activo = g.Activo,
            FechaAlta = g.FechaAlta,
            CantidadSocios = g.Socios.Count,
            CantidadPersonal = g.Personal.Count(p => p.Activo && p.Rol.Nombre != "Socio")
        });
}

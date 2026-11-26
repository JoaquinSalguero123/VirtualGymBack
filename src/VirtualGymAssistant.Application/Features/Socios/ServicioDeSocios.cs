using Microsoft.EntityFrameworkCore;
using VirtualGymAssistant.Application.Common.Extensions;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Application.Common.Models;
using VirtualGymAssistant.Application.Features.Socios.Dtos;
using VirtualGymAssistant.Domain.Constants;
using VirtualGymAssistant.Domain.Entities;
using VirtualGymAssistant.Domain.Exceptions;

namespace VirtualGymAssistant.Application.Features.Socios;

/// <summary>
/// Casos de uso del padrón de socios: consulta del perfil propio, listado para el
/// mostrador y edición de la ficha.
/// </summary>
public class ServicioDeSocios
{
    private readonly IVirtualGymDbContext _db;
    private readonly IServicioDeContexto _contexto;
    private readonly IProveedorDeFechaHora _reloj;
    private readonly IUsuarioActual _usuarioActual;
    private readonly GestorDeFichasDeSocio _fichasDeSocio;

    public ServicioDeSocios(
        IVirtualGymDbContext db,
        IServicioDeContexto contexto,
        IProveedorDeFechaHora reloj,
        IUsuarioActual usuarioActual,
        GestorDeFichasDeSocio fichasDeSocio)
    {
        _db = db;
        _contexto = contexto;
        _reloj = reloj;
        _usuarioActual = usuarioActual;
        _fichasDeSocio = fichasDeSocio;
    }

    /// <summary>
    /// Caso de uso "Consultar el padrón de socios de la sucursal".
    /// Sólo el personal de mostrador y los administradores pueden verlo.
    /// </summary>
    public async Task<ResultadoPaginado<SocioDto>> ListarPadronAsync(
        FiltroDeSocios filtro,
        ParametrosDePaginacion paginacion,
        CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Recepcionista, RolesDelSistema.Administrador,
                           RolesDelSistema.Entrenador, RolesDelSistema.Nutricionista);

        var idGimnasio = contexto.GimnasioActivoRequerido;
        var consulta = _db.Socios.Where(s => s.IdGimnasio == idGimnasio);

        if (filtro.Texto.NormalizarONull() is { } texto)
        {
            consulta = consulta.Where(s =>
                s.Usuario.Nombre!.Contains(texto) ||
                s.Usuario.Apellido!.Contains(texto) ||
                s.Usuario.Mail.Contains(texto) ||
                s.Usuario.Username.Contains(texto) ||
                (s.NumeroSocio != null && s.NumeroSocio.ToString()!.Contains(texto)));
        }

        if (filtro.IdEstado is { } idEstado)
            consulta = consulta.Where(s => s.IdEstado == idEstado);

        if (filtro.SoloActivos)
            consulta = consulta.Where(s => s.FechaBaja == null && s.Estado.Nombre != EstadosDeSocio.Baja);

        consulta = consulta
            .OrderBy(s => s.Usuario.Apellido)
            .ThenBy(s => s.Usuario.Nombre);

        return await Proyectar(consulta).PaginarAsync(paginacion, ct);
    }

    /// <summary>
    /// Caso de uso "Consultar perfil y estado" desde la cuenta del socio.
    /// Devuelve la ficha del usuario autenticado en la sucursal activa.
    /// </summary>
    public async Task<SocioDto> ObtenerMiFichaAsync(CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        var idGimnasio = contexto.GimnasioActivoRequerido;

        return await Proyectar(_db.Socios
                .Where(s => s.IdUsuario == contexto.IdUsuario && s.IdGimnasio == idGimnasio))
            .FirstOrDefaultAsync(ct)
            ?? throw new NoEncontradoException(
                "Todavía no tenés una ficha de socio en esta sucursal.");
    }

    /// <summary>Detalle de un socio de la sucursal activa (uso del personal).</summary>
    public async Task<SocioDto> ObtenerAsync(int idSocio, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        var socio = await ExigirSocioDeLaSucursalAsync(idSocio, contexto.GimnasioActivoRequerido, ct);

        // Un socio puede ver su propia ficha; para ver la de otro hace falta ser personal.
        if (socio.IdUsuario != contexto.IdUsuario)
            contexto.ExigirRol(RolesDelSistema.Recepcionista, RolesDelSistema.Administrador,
                               RolesDelSistema.Entrenador, RolesDelSistema.Nutricionista);

        return await Proyectar(_db.Socios.Where(s => s.Id == idSocio)).FirstAsync(ct);
    }

    /// <summary>
    /// Edita la ficha del socio (estado, objetivo, observaciones).
    /// Al marcarlo como Baja se completa la fecha de baja; al reactivarlo se limpia.
    /// </summary>
    public async Task<SocioDto> ActualizarAsync(
        int idSocio, ActualizarSocioRequest request, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Recepcionista, RolesDelSistema.Administrador);

        var socio = await ExigirSocioDeLaSucursalAsync(idSocio, contexto.GimnasioActivoRequerido, ct);

        var estado = await _db.SocioEstados.FirstOrDefaultAsync(e => e.Id == request.IdEstado, ct)
            ?? throw new NoEncontradoException("el estado de socio", request.IdEstado);

        if (request.IdObjetivo is { } idObjetivo &&
            !await _db.Objetivos.AnyAsync(o => o.Id == idObjetivo, ct))
            throw new NoEncontradoException("el objetivo", idObjetivo);

        if (request.FechaNacimiento is { } nacimiento && nacimiento > _reloj.Hoy)
            throw new ReglaDeNegocioException("La fecha de nacimiento no puede ser futura.");

        socio.IdEstado = estado.Id;
        socio.IdObjetivo = request.IdObjetivo;
        socio.FechaNacimiento = request.FechaNacimiento;
        socio.Observaciones = request.Observaciones.NormalizarONull();

        // La fecha de baja acompaña al estado para que el padrón sea coherente.
        socio.FechaBaja = estado.Nombre == EstadosDeSocio.Baja
            ? socio.FechaBaja ?? _reloj.Hoy
            : null;

        await _db.SaveChangesAsync(ct);

        return await Proyectar(_db.Socios.Where(s => s.Id == idSocio)).FirstAsync(ct);
    }

    /// <summary>Catálogo de estados de socio, para los selectores del front.</summary>
    public async Task<IReadOnlyList<EstadoDeSocioDto>> ListarEstadosAsync(CancellationToken ct = default) =>
        await _db.SocioEstados
            .OrderBy(e => e.Id)
            .Select(e => new EstadoDeSocioDto { Id = e.Id, Nombre = e.Nombre })
            .ToListAsync(ct);

    /// <summary>
    /// Caso de uso "Sumarse a otra sucursal": el usuario autenticado se hace socio de una
    /// sucursal en la que todavía no tiene ninguna ficha, sin pasar de nuevo por el registro.
    ///
    /// A propósito NO pasa por <see cref="IServicioDeContexto"/>: ese contexto sólo deja
    /// operar sobre sucursales a las que el usuario ya tiene acceso, que es justo la
    /// limitación que este caso de uso tiene que salvar. Por eso resuelve el usuario
    /// directamente desde <see cref="IUsuarioActual"/>.
    /// </summary>
    public async Task<SocioDto> UnirmeAAsync(UnirseASucursalRequest request, CancellationToken ct = default)
    {
        var idUsuario = _usuarioActual.IdRequerido;

        var gimnasio = await _db.Gimnasios.FirstOrDefaultAsync(g => g.Id == request.IdGimnasio && g.Activo, ct)
            ?? throw new NoEncontradoException("La sucursal elegida no existe o no está activa.");

        if (await _fichasDeSocio.BuscarIdAsync(idUsuario, gimnasio.Id, ct) is not null)
            throw new ConflictoException("Ya sos socio de esta sucursal.");

        var idSocio = await _fichasDeSocio.ObtenerOCrearAsync(idUsuario, gimnasio.Id, ct);
        await _fichasDeSocio.AsignarRolDeSocioAsync(idUsuario, gimnasio.Id, ct);

        return await Proyectar(_db.Socios.Where(s => s.Id == idSocio)).FirstAsync(ct);
    }

    // ------------------------------------------------------------------

    /// <summary>
    /// Trae el socio comprobando que pertenezca a la sucursal en la que se está operando.
    /// Sin esta comprobación, un administrador podría editar socios de otra sede pasando
    /// un Id cualquiera por la URL.
    /// </summary>
    private async Task<Socio> ExigirSocioDeLaSucursalAsync(int idSocio, int idGimnasio, CancellationToken ct) =>
        await _db.Socios.FirstOrDefaultAsync(s => s.Id == idSocio && s.IdGimnasio == idGimnasio, ct)
        ?? throw new NoEncontradoException("el socio", idSocio);

    private IQueryable<SocioDto> Proyectar(IQueryable<Socio> consulta)
    {
        var anio = _reloj.Hoy.Year;
        var mes = _reloj.Hoy.Month;

        return consulta.Select(s => new SocioDto
        {
            Id = s.Id,
            IdUsuario = s.IdUsuario,
            IdGimnasio = s.IdGimnasio,
            Sucursal = s.Gimnasio.Nombre,
            NumeroSocio = s.NumeroSocio,
            Username = s.Usuario.Username,
            NombreCompleto = (s.Usuario.Nombre + " " + s.Usuario.Apellido).Trim(),
            Email = s.Usuario.Mail,
            Telefono = s.Usuario.Telefono,
            IdEstado = s.IdEstado,
            Estado = s.Estado.Nombre,
            IdObjetivo = s.IdObjetivo,
            Objetivo = s.Objetivo != null ? s.Objetivo.Nombre : null,
            FechaNacimiento = s.FechaNacimiento,
            FechaAlta = s.FechaAlta,
            FechaBaja = s.FechaBaja,
            Observaciones = s.Observaciones,
            PlanActivo = s.Membresias
                .Where(m => m.Activa)
                .Select(m => m.Plan.Nombre)
                .FirstOrDefault(),
            PrecioPlanActivo = s.Membresias
                .Where(m => m.Activa)
                .Select(m => (decimal?)m.PrecioAcordado)
                .FirstOrDefault(),
            CuotaAlDia = s.Pagos.Any(p =>
                p.PeriodoAnio == anio &&
                p.PeriodoMes == mes &&
                p.Estado.Nombre == EstadosDePago.Pagado)
        });
    }
}

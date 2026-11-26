using Microsoft.EntityFrameworkCore;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Application.Features.Asistencias.Dtos;
using VirtualGymAssistant.Application.Features.Socios;
using VirtualGymAssistant.Domain.Constants;
using VirtualGymAssistant.Domain.Entities;
using VirtualGymAssistant.Domain.Exceptions;

namespace VirtualGymAssistant.Application.Features.Asistencias;

/// <summary>
/// Casos de uso de asistencias: el check-in del socio, la consulta de su calendario y
/// la grilla mensual que mira el personal.
///
/// Regla central: una sola asistencia por socio y por día. Si vuelve más tarde el mismo
/// día, no se genera un registro nuevo.
/// </summary>
public class ServicioDeAsistencias
{
    private readonly IVirtualGymDbContext _db;
    private readonly IServicioDeContexto _contexto;
    private readonly IProveedorDeFechaHora _reloj;
    private readonly GestorDeFichasDeSocio _fichasDeSocio;

    public ServicioDeAsistencias(
        IVirtualGymDbContext db,
        IServicioDeContexto contexto,
        IProveedorDeFechaHora reloj,
        GestorDeFichasDeSocio fichasDeSocio)
    {
        _db = db;
        _contexto = contexto;
        _reloj = reloj;
        _fichasDeSocio = fichasDeSocio;
    }

    /// <summary>
    /// Caso de uso "Registrar asistencia": el socio marca su ingreso en la sucursal activa.
    /// </summary>
    public async Task<AsistenciaDto> MarcarAsync(CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        var idGimnasio = contexto.GimnasioActivoRequerido;

        // Regla automática del sistema: si no es socio de esta sede todavía, se le crea la ficha.
        var idSocio = await _fichasDeSocio.ObtenerOCrearAsync(contexto.IdUsuario, idGimnasio, ct);

        return await RegistrarEntradaAsync(idSocio, idGimnasio, ct);
    }

    /// <summary>
    /// Check-in tomado desde el mostrador para un socio concreto (control de acceso).
    /// </summary>
    public async Task<AsistenciaDto> MarcarParaSocioAsync(int idSocio, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Recepcionista, RolesDelSistema.Administrador);

        var idGimnasio = contexto.GimnasioActivoRequerido;

        var socio = await _db.Socios
            .Include(s => s.Estado)
            .FirstOrDefaultAsync(s => s.Id == idSocio && s.IdGimnasio == idGimnasio, ct)
            ?? throw new NoEncontradoException("el socio", idSocio);

        if (socio.Estado.Nombre is EstadosDeSocio.Baja or EstadosDeSocio.Suspendido)
            throw new ReglaDeNegocioException(
                $"El socio está en estado {socio.Estado.Nombre} y no puede ingresar.");

        return await RegistrarEntradaAsync(idSocio, idGimnasio, ct);
    }

    /// <summary>Cierra la asistencia de hoy registrando la hora de salida.</summary>
    public async Task<AsistenciaDto> RegistrarSalidaAsync(CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        var idGimnasio = contexto.GimnasioActivoRequerido;

        var idSocio = await _fichasDeSocio.BuscarIdAsync(contexto.IdUsuario, idGimnasio, ct)
            ?? throw new NoEncontradoException("Todavía no sos socio de esta sucursal.");

        var asistencia = await _db.Asistencias
            .Where(a => a.IdSocio == idSocio && a.Fecha == _reloj.Hoy)
            .OrderByDescending(a => a.FechaHoraEntrada)
            .FirstOrDefaultAsync(ct)
            ?? throw new ReglaDeNegocioException("Hoy no registraste tu ingreso.");

        if (asistencia.FechaHoraSalida is not null)
            throw new ReglaDeNegocioException("Ya registraste tu salida de hoy.");

        asistencia.FechaHoraSalida = _reloj.Ahora;
        await _db.SaveChangesAsync(ct);

        return await ObtenerDtoAsync(asistencia.Id, ct);
    }

    /// <summary>
    /// Caso de uso "Consultar asistencias del mes" desde la cuenta del socio.
    /// </summary>
    public async Task<IReadOnlyList<AsistenciaDto>> ListarMisAsistenciasAsync(
        int? anio = null, int? mes = null, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        var idGimnasio = contexto.GimnasioActivoRequerido;

        var idSocio = await _fichasDeSocio.BuscarIdAsync(contexto.IdUsuario, idGimnasio, ct);
        if (idSocio is null) return [];

        var anioConsultado = anio ?? _reloj.Hoy.Year;
        var mesConsultado = mes ?? _reloj.Hoy.Month;

        return await Proyectar(_db.Asistencias
                .Where(a => a.IdSocio == idSocio
                            && a.Fecha.Year == anioConsultado
                            && a.Fecha.Month == mesConsultado))
            .OrderBy(a => a.FechaHoraEntrada)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Caso de uso "Ver la grilla de asistencias del mes": por cada socio de la sucursal,
    /// los días en que asistió.
    /// </summary>
    public async Task<IReadOnlyList<FilaDeAsistenciasDto>> ObtenerGrillaMensualAsync(
        int? anio = null, int? mes = null, CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Recepcionista, RolesDelSistema.Administrador,
                           RolesDelSistema.Entrenador);

        var idGimnasio = contexto.GimnasioActivoRequerido;
        var anioConsultado = anio ?? _reloj.Hoy.Year;
        var mesConsultado = mes ?? _reloj.Hoy.Month;

        var socios = await _db.Socios
            .Where(s => s.IdGimnasio == idGimnasio)
            .OrderBy(s => s.Usuario.Apellido)
            .ThenBy(s => s.Usuario.Nombre)
            .Select(s => new
            {
                s.Id,
                s.NumeroSocio,
                s.Usuario.Username,
                NombreCompleto = (s.Usuario.Nombre + " " + s.Usuario.Apellido).Trim()
            })
            .ToListAsync(ct);

        var dias = await _db.Asistencias
            .Where(a => a.IdGimnasio == idGimnasio
                        && a.Fecha.Year == anioConsultado
                        && a.Fecha.Month == mesConsultado)
            .Select(a => new { a.IdSocio, Dia = a.Fecha.Day })
            .ToListAsync(ct);

        var porSocio = dias.ToLookup(d => d.IdSocio);

        return socios.Select(socio =>
        {
            var diasDelSocio = porSocio[socio.Id]
                .Select(d => d.Dia)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            return new FilaDeAsistenciasDto
            {
                IdSocio = socio.Id,
                Socio = socio.NombreCompleto,
                Username = socio.Username,
                NumeroSocio = socio.NumeroSocio,
                DiasAsistidos = diasDelSocio,
                Total = diasDelSocio.Count
            };
        }).ToList();
    }

    /// <summary>Indicadores de concurrencia de la sucursal para el panel del personal.</summary>
    public async Task<ResumenDeAsistenciasDto> ObtenerResumenAsync(CancellationToken ct = default)
    {
        var contexto = await _contexto.ObtenerAsync(ct);
        contexto.ExigirRol(RolesDelSistema.Recepcionista, RolesDelSistema.Administrador,
                           RolesDelSistema.Entrenador);

        var idGimnasio = contexto.GimnasioActivoRequerido;
        var hoy = _reloj.Hoy;

        var deHoy = await _db.Asistencias
            .Where(a => a.IdGimnasio == idGimnasio && a.Fecha == hoy)
            .Select(a => new { a.IdSocio, a.FechaHoraSalida })
            .ToListAsync(ct);

        var delMes = await _db.Asistencias
            .Where(a => a.IdGimnasio == idGimnasio
                        && a.Fecha.Year == hoy.Year
                        && a.Fecha.Month == hoy.Month)
            .Select(a => a.IdSocio)
            .ToListAsync(ct);

        var sociosDelMes = delMes.Distinct().Count();

        return new ResumenDeAsistenciasDto
        {
            Fecha = hoy,
            AsistenciasDeHoy = deHoy.Count,
            SociosDistintosHoy = deHoy.Select(a => a.IdSocio).Distinct().Count(),
            SociosEnElGimnasio = deHoy.Count(a => a.FechaHoraSalida is null),
            AsistenciasDelMes = delMes.Count,
            PromedioPorSocioEnElMes = sociosDelMes == 0
                ? 0
                : Math.Round(delMes.Count / (double)sociosDelMes, 1)
        };
    }

    // ------------------------------------------------------------------

    /// <summary>Inserta la entrada del día validando que no exista una previa.</summary>
    private async Task<AsistenciaDto> RegistrarEntradaAsync(int idSocio, int idGimnasio, CancellationToken ct)
    {
        var hoy = _reloj.Hoy;

        var yaVino = await _db.Asistencias.AnyAsync(a => a.IdSocio == idSocio && a.Fecha == hoy, ct);

        if (yaVino)
            throw new ReglaDeNegocioException("Ya hay una asistencia registrada para hoy.");

        var asistencia = new Asistencia
        {
            IdSocio = idSocio,
            IdGimnasio = idGimnasio,
            FechaHoraEntrada = _reloj.Ahora
        };

        _db.Asistencias.Add(asistencia);
        await _db.SaveChangesAsync(ct);

        return await ObtenerDtoAsync(asistencia.Id, ct);
    }

    private async Task<AsistenciaDto> ObtenerDtoAsync(int id, CancellationToken ct) =>
        await Proyectar(_db.Asistencias.Where(a => a.Id == id)).FirstAsync(ct);

    private static IQueryable<AsistenciaDto> Proyectar(IQueryable<Asistencia> consulta) =>
        consulta.Select(a => new AsistenciaDto
        {
            Id = a.Id,
            IdSocio = a.IdSocio,
            Socio = (a.Socio.Usuario.Nombre + " " + a.Socio.Usuario.Apellido).Trim(),
            IdGimnasio = a.IdGimnasio,
            Sucursal = a.Gimnasio.Nombre,
            Fecha = a.Fecha,
            Dia = a.Fecha.Day,
            FechaHoraEntrada = a.FechaHoraEntrada,
            FechaHoraSalida = a.FechaHoraSalida
        });
}

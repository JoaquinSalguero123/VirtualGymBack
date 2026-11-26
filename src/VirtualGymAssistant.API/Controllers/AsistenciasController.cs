using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualGymAssistant.Application.Features.Asistencias;
using VirtualGymAssistant.Application.Features.Asistencias.Dtos;

namespace VirtualGymAssistant.API.Controllers;

/// <summary>
/// Check-in de los socios y control de asistencias.
/// </summary>
[Authorize]
[Tags("8 - Asistencias")]
public class AsistenciasController : ControladorBase
{
    private readonly ServicioDeAsistencias _servicio;

    public AsistenciasController(ServicioDeAsistencias servicio) => _servicio = servicio;

    /// <summary>
    /// El socio marca su ingreso a la sucursal activa.
    /// </summary>
    /// <remarks>
    /// Sólo se admite una asistencia por socio y por día. Si el usuario todavía no era
    /// socio de esta sucursal, el sistema le crea la ficha automáticamente.
    /// </remarks>
    [HttpPost("marcar")]
    [ProducesResponseType(typeof(AsistenciaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AsistenciaDto>> Marcar(CancellationToken ct)
    {
        var asistencia = await _servicio.MarcarAsync(ct);

        return StatusCode(StatusCodes.Status201Created, asistencia);
    }

    /// <summary>
    /// El mostrador registra el ingreso de un socio concreto (control de acceso).
    /// </summary>
    [HttpPost("socio/{idSocio:int}/marcar")]
    [ProducesResponseType(typeof(AsistenciaDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AsistenciaDto>> MarcarParaSocio(int idSocio, CancellationToken ct)
    {
        var asistencia = await _servicio.MarcarParaSocioAsync(idSocio, ct);

        return StatusCode(StatusCodes.Status201Created, asistencia);
    }

    /// <summary>Cierra la asistencia de hoy registrando la hora de salida.</summary>
    [HttpPut("salida")]
    [ProducesResponseType(typeof(AsistenciaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AsistenciaDto>> RegistrarSalida(CancellationToken ct) =>
        Ok(await _servicio.RegistrarSalidaAsync(ct));

    /// <summary>
    /// Asistencias del socio autenticado para el mes indicado (por defecto, el actual).
    /// </summary>
    [HttpGet("mias")]
    [ProducesResponseType(typeof(IReadOnlyList<AsistenciaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AsistenciaDto>>> ListarMias(
        [FromQuery] int? anio, [FromQuery] int? mes, CancellationToken ct) =>
        Ok(await _servicio.ListarMisAsistenciasAsync(anio, mes, ct));

    /// <summary>
    /// Grilla mensual de asistencias: por cada socio, los días en que asistió.
    /// </summary>
    [HttpGet("grilla-mensual")]
    [ProducesResponseType(typeof(IReadOnlyList<FilaDeAsistenciasDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FilaDeAsistenciasDto>>> ObtenerGrillaMensual(
        [FromQuery] int? anio, [FromQuery] int? mes, CancellationToken ct) =>
        Ok(await _servicio.ObtenerGrillaMensualAsync(anio, mes, ct));

    /// <summary>Indicadores de concurrencia del día y del mes en la sucursal activa.</summary>
    [HttpGet("resumen")]
    [ProducesResponseType(typeof(ResumenDeAsistenciasDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResumenDeAsistenciasDto>> ObtenerResumen(CancellationToken ct) =>
        Ok(await _servicio.ObtenerResumenAsync(ct));
}

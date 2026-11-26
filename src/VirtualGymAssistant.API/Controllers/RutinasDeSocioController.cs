using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualGymAssistant.Application.Features.Rutinas;
using VirtualGymAssistant.Application.Features.Rutinas.Dtos;

namespace VirtualGymAssistant.API.Controllers;

/// <summary>
/// Rutinas concretas asignadas a los socios.
/// </summary>
[Authorize]
[Route("api/rutinas-de-socio")]
[Tags("10 - Entrenamiento")]
public class RutinasDeSocioController : ControladorBase
{
    private readonly ServicioDeRutinasDeSocio _servicio;

    public RutinasDeSocioController(ServicioDeRutinasDeSocio servicio) => _servicio = servicio;

    /// <summary>
    /// Genera una propuesta de rutina SIN guardarla, para que el socio la revise antes de confirmar.
    /// </summary>
    /// <remarks>
    /// Si se indica una plantilla se copia su detalle. Si sólo se indica un objetivo, se
    /// busca una plantilla que coincida y, si no hay ninguna, el sistema arma la rutina
    /// repartiendo los ejercicios entre los distintos grupos musculares.
    /// </remarks>
    [HttpPost("generar")]
    [ProducesResponseType(typeof(IReadOnlyList<DiaDeRutinaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DiaDeRutinaDto>>> GenerarPropuesta(
        [FromBody] GenerarRutinaRequest request, CancellationToken ct) =>
        Ok(await _servicio.GenerarPropuestaAsync(request, ct));

    /// <summary>
    /// Rutina vigente del socio autenticado. Devuelve 204 si todavía no tiene ninguna.
    /// </summary>
    [HttpGet("mia")]
    [ProducesResponseType(typeof(RutinaDeSocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<RutinaDeSocioDto>> ObtenerMia(CancellationToken ct)
    {
        var rutina = await _servicio.ObtenerMiRutinaAsync(ct);

        return rutina is null ? NoContent() : Ok(rutina);
    }

    /// <summary>
    /// Guarda la rutina del socio autenticado. La rutina activa anterior queda archivada.
    /// </summary>
    [HttpPost("mia")]
    [ProducesResponseType(typeof(RutinaDeSocioDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<RutinaDeSocioDto>> GuardarMia(
        [FromBody] GuardarRutinaDeSocioRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await _servicio.GuardarMiRutinaAsync(request, ct));

    /// <summary>Rutina vigente de un socio puntual. La consulta el entrenador.</summary>
    [HttpGet("socio/{idSocio:int}")]
    [ProducesResponseType(typeof(RutinaDeSocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<RutinaDeSocioDto>> ObtenerDeSocio(int idSocio, CancellationToken ct)
    {
        var rutina = await _servicio.ObtenerDeSocioAsync(idSocio, ct);

        return rutina is null ? NoContent() : Ok(rutina);
    }

    /// <summary>Historial de rutinas de un socio.</summary>
    [HttpGet("socio/{idSocio:int}/historial")]
    [ProducesResponseType(typeof(IReadOnlyList<RutinaDeSocioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RutinaDeSocioDto>>> ListarHistorial(
        int idSocio, CancellationToken ct) =>
        Ok(await _servicio.ListarHistorialDeSocioAsync(idSocio, ct));

    /// <summary>El entrenador arma y asigna una rutina a un socio de su sucursal.</summary>
    [HttpPost("asignar")]
    [ProducesResponseType(typeof(RutinaDeSocioDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<RutinaDeSocioDto>> Asignar(
        [FromBody] AsignarRutinaRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await _servicio.AsignarASocioAsync(request, ct));
}

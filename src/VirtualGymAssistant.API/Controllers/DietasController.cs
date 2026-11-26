using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualGymAssistant.Application.Features.Dietas;
using VirtualGymAssistant.Application.Features.Dietas.Dtos;

namespace VirtualGymAssistant.API.Controllers;

/// <summary>
/// Planes de alimentación de los socios.
/// </summary>
[Authorize]
[Tags("11 - Nutrición")]
public class DietasController : ControladorBase
{
    private readonly ServicioDeDietas _servicio;

    public DietasController(ServicioDeDietas servicio) => _servicio = servicio;

    /// <summary>
    /// Genera una propuesta de dieta SIN guardarla, eligiendo alimentos del catálogo
    /// según el objetivo y las categorías indicadas.
    /// </summary>
    [HttpPost("generar")]
    [ProducesResponseType(typeof(IReadOnlyList<DiaDeDietaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<DiaDeDietaDto>>> GenerarPropuesta(
        [FromBody] GenerarDietaRequest request, CancellationToken ct) =>
        Ok(await _servicio.GenerarPropuestaAsync(request, ct));

    /// <summary>
    /// Dieta vigente del socio autenticado. Devuelve 204 si todavía no tiene ninguna.
    /// </summary>
    [HttpGet("mia")]
    [ProducesResponseType(typeof(DietaDeSocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<DietaDeSocioDto>> ObtenerMia(CancellationToken ct)
    {
        var dieta = await _servicio.ObtenerMiDietaAsync(ct);

        return dieta is null ? NoContent() : Ok(dieta);
    }

    /// <summary>
    /// Guarda la dieta del socio autenticado. La dieta activa anterior queda archivada.
    /// </summary>
    [HttpPost("mia")]
    [ProducesResponseType(typeof(DietaDeSocioDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<DietaDeSocioDto>> GuardarMia(
        [FromBody] GuardarDietaRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await _servicio.GuardarMiDietaAsync(request, ct));

    /// <summary>Dieta vigente de un socio puntual. La consulta el nutricionista.</summary>
    [HttpGet("socio/{idSocio:int}")]
    [ProducesResponseType(typeof(DietaDeSocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<DietaDeSocioDto>> ObtenerDeSocio(int idSocio, CancellationToken ct)
    {
        var dieta = await _servicio.ObtenerDeSocioAsync(idSocio, ct);

        return dieta is null ? NoContent() : Ok(dieta);
    }

    /// <summary>Historial de dietas de un socio.</summary>
    [HttpGet("socio/{idSocio:int}/historial")]
    [ProducesResponseType(typeof(IReadOnlyList<DietaDeSocioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DietaDeSocioDto>>> ListarHistorial(
        int idSocio, CancellationToken ct) =>
        Ok(await _servicio.ListarHistorialDeSocioAsync(idSocio, ct));

    /// <summary>El nutricionista arma y asigna una dieta a un socio de su sucursal.</summary>
    [HttpPost("asignar")]
    [ProducesResponseType(typeof(DietaDeSocioDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<DietaDeSocioDto>> Asignar(
        [FromBody] AsignarDietaRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await _servicio.AsignarASocioAsync(request, ct));
}

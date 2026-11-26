using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualGymAssistant.Application.Features.Planes;
using VirtualGymAssistant.Application.Features.Planes.Dtos;

namespace VirtualGymAssistant.API.Controllers;

/// <summary>
/// Planes comerciales de la organización.
/// </summary>
[Authorize]
[Tags("6 - Planes y membresías")]
public class PlanesController : ControladorBase
{
    private readonly ServicioDePlanes _servicio;

    public PlanesController(ServicioDePlanes servicio) => _servicio = servicio;

    /// <summary>
    /// Lista los planes vigentes para el sitio público. No requiere autenticación.
    /// </summary>
    /// <remarks>
    /// Lo consume la portada del sitio, donde un visitante compara precios antes de
    /// crear su cuenta. Devuelve sólo los planes activos.
    /// </remarks>
    /// <param name="idGimnasio">
    /// Sucursal desde la que se consulta. Si se omite, se usa la casa matriz.
    /// </param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpGet("publicos")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<PlanDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PlanDto>>> ListarPublicos(
        [FromQuery] int? idGimnasio, CancellationToken ct) =>
        Ok(await _servicio.ListarPublicosAsync(idGimnasio, ct));

    /// <summary>
    /// Lista los planes de la organización activa.
    /// </summary>
    /// <param name="incluirInactivos">
    /// Si es true incluye los planes dados de baja. Sólo tiene efecto para el personal.
    /// </param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PlanDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PlanDto>>> Listar(
        [FromQuery] bool incluirInactivos, CancellationToken ct) =>
        Ok(await _servicio.ListarAsync(incluirInactivos, ct));

    /// <summary>Devuelve el detalle de un plan.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanDto>> Obtener(int id, CancellationToken ct) =>
        Ok(await _servicio.ObtenerAsync(id, ct));

    /// <summary>Crea un plan en la organización activa.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PlanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PlanDto>> Crear(
        [FromBody] CrearPlanRequest request, CancellationToken ct)
    {
        var creado = await _servicio.CrearAsync(request, ct);

        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }

    /// <summary>
    /// Modifica un plan. Cambiar el precio no altera lo que ya se les cobró a los socios:
    /// cada membresía conserva su propio precio acordado.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PlanDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PlanDto>> Actualizar(
        int id, [FromBody] ActualizarPlanRequest request, CancellationToken ct) =>
        Ok(await _servicio.ActualizarAsync(id, request, ct));

    /// <summary>
    /// Da de baja el plan (baja lógica): deja de ofrecerse pero se conserva en el
    /// historial de pagos.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DarDeBaja(int id, CancellationToken ct)
    {
        await _servicio.DarDeBajaAsync(id, ct);

        return NoContent();
    }
}

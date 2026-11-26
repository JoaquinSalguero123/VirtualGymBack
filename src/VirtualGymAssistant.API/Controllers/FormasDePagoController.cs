using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualGymAssistant.Application.Features.FormasDePago;
using VirtualGymAssistant.Application.Features.FormasDePago.Dtos;

namespace VirtualGymAssistant.API.Controllers;

/// <summary>
/// Medios de pago aceptados por el sistema.
/// </summary>
[Authorize]
[Route("api/formas-de-pago")]
[Tags("7 - Pagos")]
public class FormasDePagoController : ControladorBase
{
    private readonly ServicioDeFormasDePago _servicio;

    public FormasDePagoController(ServicioDeFormasDePago servicio) => _servicio = servicio;

    /// <summary>
    /// Lista las formas de pago. Por defecto sólo las vigentes, que son las que puede
    /// elegir un socio al pagar.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FormaDePagoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FormaDePagoDto>>> Listar(
        [FromQuery] bool incluirInactivas, CancellationToken ct) =>
        Ok(await _servicio.ListarAsync(incluirInactivas, ct));

    /// <summary>Devuelve el detalle de una forma de pago.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(FormaDePagoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FormaDePagoDto>> Obtener(int id, CancellationToken ct) =>
        Ok(await _servicio.ObtenerAsync(id, ct));

    /// <summary>Crea una forma de pago.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(FormaDePagoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FormaDePagoDto>> Crear(
        [FromBody] GuardarFormaDePagoRequest request, CancellationToken ct)
    {
        var creada = await _servicio.CrearAsync(request, ct);

        return CreatedAtAction(nameof(Obtener), new { id = creada.Id }, creada);
    }

    /// <summary>Modifica el nombre o la vigencia de una forma de pago.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(FormaDePagoDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<FormaDePagoDto>> Actualizar(
        int id, [FromBody] GuardarFormaDePagoRequest request, CancellationToken ct) =>
        Ok(await _servicio.ActualizarAsync(id, request, ct));

    /// <summary>
    /// Elimina la forma de pago. Si ya se usó en algún pago se la da de baja en lugar de
    /// borrarla, para no romper el historial de cobranzas.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await _servicio.EliminarAsync(id, ct);

        return NoContent();
    }
}

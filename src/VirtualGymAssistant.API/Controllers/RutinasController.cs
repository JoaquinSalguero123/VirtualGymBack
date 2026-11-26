using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualGymAssistant.Application.Features.Rutinas;
using VirtualGymAssistant.Application.Features.Rutinas.Dtos;

namespace VirtualGymAssistant.API.Controllers;

/// <summary>
/// Plantillas de rutina reutilizables de la organización.
/// </summary>
[Authorize]
[Tags("10 - Entrenamiento")]
public class RutinasController : ControladorBase
{
    private readonly ServicioDePlantillasDeRutina _servicio;

    public RutinasController(ServicioDePlantillasDeRutina servicio) => _servicio = servicio;

    /// <summary>
    /// Lista las plantillas visibles: las globales del sistema más las de la organización activa.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PlantillaDeRutinaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PlantillaDeRutinaDto>>> Listar(
        [FromQuery] int? idObjetivo, CancellationToken ct) =>
        Ok(await _servicio.ListarAsync(idObjetivo, ct));

    /// <summary>Devuelve una plantilla con su detalle día por día.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PlantillaDeRutinaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlantillaDeRutinaDto>> Obtener(int id, CancellationToken ct) =>
        Ok(await _servicio.ObtenerAsync(id, ct));

    /// <summary>Crea una plantilla en la organización activa.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PlantillaDeRutinaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PlantillaDeRutinaDto>> Crear(
        [FromBody] GuardarPlantillaRequest request, CancellationToken ct)
    {
        var creada = await _servicio.CrearAsync(request, ct);

        return CreatedAtAction(nameof(Obtener), new { id = creada.Id }, creada);
    }

    /// <summary>
    /// Modifica una plantilla propia. El detalle se reemplaza completo por el que se envía.
    /// Las plantillas globales del sistema no se pueden editar.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PlantillaDeRutinaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PlantillaDeRutinaDto>> Actualizar(
        int id, [FromBody] GuardarPlantillaRequest request, CancellationToken ct) =>
        Ok(await _servicio.ActualizarAsync(id, request, ct));

    /// <summary>
    /// Da de baja la plantilla. Las rutinas que ya salieron de ella siguen funcionando.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DarDeBaja(int id, CancellationToken ct)
    {
        await _servicio.DarDeBajaAsync(id, ct);

        return NoContent();
    }
}

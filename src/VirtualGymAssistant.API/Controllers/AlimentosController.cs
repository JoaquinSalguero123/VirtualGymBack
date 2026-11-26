using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualGymAssistant.Application.Features.Alimentos;
using VirtualGymAssistant.Application.Features.Alimentos.Dtos;

namespace VirtualGymAssistant.API.Controllers;

/// <summary>
/// Catálogo de alimentos con su información nutricional.
/// </summary>
[Authorize]
[Tags("11 - Nutrición")]
public class AlimentosController : ControladorBase
{
    private readonly ServicioDeAlimentos _servicio;

    public AlimentosController(ServicioDeAlimentos servicio) => _servicio = servicio;

    /// <summary>Lista los alimentos, con filtros por categoría, objetivo y nombre.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AlimentoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AlimentoDto>>> Listar(
        [FromQuery] int? idCategoria,
        [FromQuery] int? idObjetivo,
        [FromQuery] string? texto,
        CancellationToken ct) =>
        Ok(await _servicio.ListarAsync(idCategoria, idObjetivo, texto, ct));

    /// <summary>Devuelve el detalle de un alimento.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AlimentoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlimentoDto>> Obtener(int id, CancellationToken ct) =>
        Ok(await _servicio.ObtenerAsync(id, ct));

    /// <summary>Crea un alimento.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AlimentoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AlimentoDto>> Crear(
        [FromBody] GuardarAlimentoRequest request, CancellationToken ct)
    {
        var creado = await _servicio.CrearAsync(request, ct);

        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }

    /// <summary>Modifica un alimento.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AlimentoDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AlimentoDto>> Actualizar(
        int id, [FromBody] GuardarAlimentoRequest request, CancellationToken ct) =>
        Ok(await _servicio.ActualizarAsync(id, request, ct));

    /// <summary>Elimina un alimento. Falla si forma parte de la dieta de algún socio.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await _servicio.EliminarAsync(id, ct);

        return NoContent();
    }
}

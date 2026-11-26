using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualGymAssistant.Application.Features.Ejercicios;
using VirtualGymAssistant.Application.Features.Ejercicios.Dtos;

namespace VirtualGymAssistant.API.Controllers;

/// <summary>
/// Catálogo de ejercicios.
/// </summary>
[Authorize]
[Tags("10 - Entrenamiento")]
public class EjerciciosController : ControladorBase
{
    private readonly ServicioDeEjercicios _servicio;

    public EjerciciosController(ServicioDeEjercicios servicio) => _servicio = servicio;

    /// <summary>Lista los ejercicios, con filtro opcional por grupo muscular y por nombre.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EjercicioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EjercicioDto>>> Listar(
        [FromQuery] int? idGrupo, [FromQuery] string? texto, CancellationToken ct) =>
        Ok(await _servicio.ListarAsync(idGrupo, texto, ct));

    /// <summary>Devuelve el detalle de un ejercicio.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EjercicioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EjercicioDto>> Obtener(int id, CancellationToken ct) =>
        Ok(await _servicio.ObtenerAsync(id, ct));

    /// <summary>Crea un ejercicio.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(EjercicioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EjercicioDto>> Crear(
        [FromBody] GuardarEjercicioRequest request, CancellationToken ct)
    {
        var creado = await _servicio.CrearAsync(request, ct);

        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }

    /// <summary>Modifica un ejercicio.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(EjercicioDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EjercicioDto>> Actualizar(
        int id, [FromBody] GuardarEjercicioRequest request, CancellationToken ct) =>
        Ok(await _servicio.ActualizarAsync(id, request, ct));

    /// <summary>Elimina un ejercicio. Falla si ya forma parte de alguna rutina.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await _servicio.EliminarAsync(id, ct);

        return NoContent();
    }
}

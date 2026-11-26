using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualGymAssistant.Application.Features.Catalogos;
using VirtualGymAssistant.Application.Features.Catalogos.Dtos;

namespace VirtualGymAssistant.API.Controllers;

/// <summary>
/// Catálogos simples: objetivos, grupos musculares y categorías de alimentos.
/// </summary>
[Authorize]
[Tags("9 - Catálogos")]
public class CatalogosController : ControladorBase
{
    private readonly ServicioDeCatalogos _servicio;

    public CatalogosController(ServicioDeCatalogos servicio) => _servicio = servicio;

    // ===================== OBJETIVOS =====================

    /// <summary>Objetivos de entrenamiento y alimentación.</summary>
    [HttpGet("objetivos")]
    [ProducesResponseType(typeof(IReadOnlyList<ItemDeCatalogoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ItemDeCatalogoDto>>> ListarObjetivos(CancellationToken ct) =>
        Ok(await _servicio.ListarObjetivosAsync(ct));

    /// <summary>Crea un objetivo.</summary>
    [HttpPost("objetivos")]
    [ProducesResponseType(typeof(ItemDeCatalogoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ItemDeCatalogoDto>> CrearObjetivo(
        [FromBody] GuardarItemDeCatalogoRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await _servicio.CrearObjetivoAsync(request, ct));

    /// <summary>Renombra un objetivo.</summary>
    [HttpPut("objetivos/{id:int}")]
    [ProducesResponseType(typeof(ItemDeCatalogoDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ItemDeCatalogoDto>> ActualizarObjetivo(
        int id, [FromBody] GuardarItemDeCatalogoRequest request, CancellationToken ct) =>
        Ok(await _servicio.ActualizarObjetivoAsync(id, request, ct));

    /// <summary>Elimina un objetivo. Falla si está en uso por alimentos, socios o rutinas.</summary>
    [HttpDelete("objetivos/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EliminarObjetivo(int id, CancellationToken ct)
    {
        await _servicio.EliminarObjetivoAsync(id, ct);

        return NoContent();
    }

    // ================= GRUPOS MUSCULARES =================

    /// <summary>Grupos musculares que clasifican los ejercicios.</summary>
    [HttpGet("grupos-musculares")]
    [ProducesResponseType(typeof(IReadOnlyList<ItemDeCatalogoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ItemDeCatalogoDto>>> ListarGruposMusculares(
        CancellationToken ct) =>
        Ok(await _servicio.ListarGruposMuscularesAsync(ct));

    /// <summary>Crea un grupo muscular.</summary>
    [HttpPost("grupos-musculares")]
    [ProducesResponseType(typeof(ItemDeCatalogoDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ItemDeCatalogoDto>> CrearGrupoMuscular(
        [FromBody] GuardarItemDeCatalogoRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await _servicio.CrearGrupoMuscularAsync(request, ct));

    /// <summary>Renombra un grupo muscular.</summary>
    [HttpPut("grupos-musculares/{id:int}")]
    [ProducesResponseType(typeof(ItemDeCatalogoDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ItemDeCatalogoDto>> ActualizarGrupoMuscular(
        int id, [FromBody] GuardarItemDeCatalogoRequest request, CancellationToken ct) =>
        Ok(await _servicio.ActualizarGrupoMuscularAsync(id, request, ct));

    /// <summary>Elimina un grupo muscular. Falla si tiene ejercicios asociados.</summary>
    [HttpDelete("grupos-musculares/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> EliminarGrupoMuscular(int id, CancellationToken ct)
    {
        await _servicio.EliminarGrupoMuscularAsync(id, ct);

        return NoContent();
    }

    // ============ CATEGORÍAS DE ALIMENTOS ============

    /// <summary>Categorías nutricionales de los alimentos.</summary>
    [HttpGet("categorias-de-alimentos")]
    [ProducesResponseType(typeof(IReadOnlyList<ItemDeCatalogoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ItemDeCatalogoDto>>> ListarCategoriasDeAlimentos(
        CancellationToken ct) =>
        Ok(await _servicio.ListarCategoriasDeAlimentosAsync(ct));

    /// <summary>Crea una categoría de alimentos.</summary>
    [HttpPost("categorias-de-alimentos")]
    [ProducesResponseType(typeof(ItemDeCatalogoDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ItemDeCatalogoDto>> CrearCategoriaDeAlimento(
        [FromBody] GuardarItemDeCatalogoRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await _servicio.CrearCategoriaDeAlimentoAsync(request, ct));

    /// <summary>Renombra una categoría de alimentos.</summary>
    [HttpPut("categorias-de-alimentos/{id:int}")]
    [ProducesResponseType(typeof(ItemDeCatalogoDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ItemDeCatalogoDto>> ActualizarCategoriaDeAlimento(
        int id, [FromBody] GuardarItemDeCatalogoRequest request, CancellationToken ct) =>
        Ok(await _servicio.ActualizarCategoriaDeAlimentoAsync(id, request, ct));

    /// <summary>Elimina una categoría. Falla si tiene alimentos asociados.</summary>
    [HttpDelete("categorias-de-alimentos/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> EliminarCategoriaDeAlimento(int id, CancellationToken ct)
    {
        await _servicio.EliminarCategoriaDeAlimentoAsync(id, ct);

        return NoContent();
    }
}

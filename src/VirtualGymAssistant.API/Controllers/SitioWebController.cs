using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualGymAssistant.Application.Features.SitioWeb;
using VirtualGymAssistant.Application.Features.SitioWeb.Dtos;

namespace VirtualGymAssistant.API.Controllers;

/// <summary>
/// Contenido del sitio público: carrusel y pie de página.
/// </summary>
[Authorize]
[Route("api/sitio-web")]
[Tags("14 - Sitio web")]
public class SitioWebController : ControladorBase
{
    private readonly ServicioDeSitioWeb _servicio;

    public SitioWebController(ServicioDeSitioWeb servicio) => _servicio = servicio;

    // ===================== CARRUSEL =====================

    /// <summary>
    /// Items del carrusel para el sitio público. No requiere autenticación.
    /// </summary>
    /// <param name="tipo">Agrupador del contenido: "servicios", "frases", etc.</param>
    /// <param name="idGimnasio">Sucursal desde la que se consulta. Si se omite, se usa la casa matriz.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpGet("carousel/publico")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<CarouselItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CarouselItemDto>>> ListarCarouselPublico(
        [FromQuery] string? tipo, [FromQuery] int? idGimnasio, CancellationToken ct) =>
        Ok(await _servicio.ListarCarouselPublicoAsync(tipo, idGimnasio, ct));

    /// <summary>Items del carrusel de la organización activa, incluidos los desactivados.</summary>
    [HttpGet("carousel")]
    [ProducesResponseType(typeof(IReadOnlyList<CarouselItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CarouselItemDto>>> ListarCarousel(
        [FromQuery] string? tipo, CancellationToken ct) =>
        Ok(await _servicio.ListarCarouselAsync(tipo, ct));

    /// <summary>
    /// Crea un item del carrusel. Se envía como multipart/form-data para poder adjuntar la imagen.
    /// </summary>
    [HttpPost("carousel")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CarouselItemDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CarouselItemDto>> CrearCarouselItem(
        [FromForm] GuardarCarouselItemRequest request,
        IFormFile? imagen,
        CancellationToken ct)
    {
        await using var contenido = imagen?.OpenReadStream();

        var creado = await _servicio.CrearCarouselItemAsync(
            request, contenido, imagen?.FileName, ct);

        return StatusCode(StatusCodes.Status201Created, creado);
    }

    /// <summary>
    /// Modifica un item del carrusel. Si se envía una imagen nueva, reemplaza a la anterior.
    /// </summary>
    [HttpPut("carousel/{id:int}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CarouselItemDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CarouselItemDto>> ActualizarCarouselItem(
        int id,
        [FromForm] GuardarCarouselItemRequest request,
        IFormFile? imagen,
        CancellationToken ct)
    {
        await using var contenido = imagen?.OpenReadStream();

        return Ok(await _servicio.ActualizarCarouselItemAsync(
            id, request, contenido, imagen?.FileName, ct));
    }

    /// <summary>Elimina un item del carrusel junto con su imagen.</summary>
    [HttpDelete("carousel/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> EliminarCarouselItem(int id, CancellationToken ct)
    {
        await _servicio.EliminarCarouselItemAsync(id, ct);

        return NoContent();
    }

    // ===================== PIE DE PÁGINA =====================

    /// <summary>
    /// Datos del pie agrupados por sección, listos para dibujar el footer. No requiere autenticación.
    /// </summary>
    [HttpGet("footer/publico")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyDictionary<string, List<FooterConfigDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyDictionary<string, List<FooterConfigDto>>>> ObtenerFooterPublico(
        [FromQuery] int? idGimnasio, CancellationToken ct) =>
        Ok(await _servicio.ObtenerFooterPublicoAsync(idGimnasio, ct));

    /// <summary>Agrega un dato al pie de la organización activa.</summary>
    [HttpPost("footer")]
    [ProducesResponseType(typeof(FooterConfigDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FooterConfigDto>> CrearFooterConfig(
        [FromBody] GuardarFooterConfigRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await _servicio.CrearFooterConfigAsync(request, ct));

    /// <summary>Modifica un dato del pie.</summary>
    [HttpPut("footer/{id:int}")]
    [ProducesResponseType(typeof(FooterConfigDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<FooterConfigDto>> ActualizarFooterConfig(
        int id, [FromBody] GuardarFooterConfigRequest request, CancellationToken ct) =>
        Ok(await _servicio.ActualizarFooterConfigAsync(id, request, ct));

    /// <summary>Elimina un dato del pie.</summary>
    [HttpDelete("footer/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> EliminarFooterConfig(int id, CancellationToken ct)
    {
        await _servicio.EliminarFooterConfigAsync(id, ct);

        return NoContent();
    }
}

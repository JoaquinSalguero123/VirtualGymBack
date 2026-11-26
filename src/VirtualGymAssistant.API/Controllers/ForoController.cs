using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualGymAssistant.Application.Common.Models;
using VirtualGymAssistant.Application.Features.Foro;
using VirtualGymAssistant.Application.Features.Foro.Dtos;

namespace VirtualGymAssistant.API.Controllers;

/// <summary>
/// Foro de la comunidad del gimnasio.
/// </summary>
[Authorize]
[Tags("15 - Foro")]
public class ForoController : ControladorBase
{
    private readonly ServicioDeForo _servicio;

    public ForoController(ServicioDeForo servicio) => _servicio = servicio;

    /// <summary>
    /// Lista las publicaciones de la sucursal activa con sus respuestas anidadas.
    /// </summary>
    /// <remarks>
    /// Se pagina sólo por publicaciones raíz: las respuestas viajan siempre completas
    /// dentro de su publicación.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(ResultadoPaginado<PublicacionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultadoPaginado<PublicacionDto>>> Listar(
        [FromQuery] int? pagina, [FromQuery] int? tamanioPagina, CancellationToken ct) =>
        Ok(await _servicio.ListarAsync(LeerPaginacion(pagina, tamanioPagina), ct));

    /// <summary>
    /// Publica un mensaje o responde a otro. Se envía como multipart/form-data para
    /// poder adjuntar un archivo.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(PublicacionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PublicacionDto>> Publicar(
        [FromForm] CrearPublicacionRequest request,
        IFormFile? archivo,
        CancellationToken ct)
    {
        await using var contenido = archivo?.OpenReadStream();

        var publicacion = await _servicio.PublicarAsync(request, contenido, archivo?.FileName, ct);

        return StatusCode(StatusCodes.Status201Created, publicacion);
    }

    /// <summary>
    /// Elimina una publicación. El autor puede borrar la suya; el personal puede moderar
    /// cualquiera de su sucursal. Al borrar una publicación raíz se eliminan sus respuestas.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await _servicio.EliminarAsync(id, ct);

        return NoContent();
    }
}

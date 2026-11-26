using Microsoft.AspNetCore.Mvc;
using VirtualGymAssistant.Application.Common.Models;

namespace VirtualGymAssistant.API.Controllers;

/// <summary>
/// Base de todos los controladores de la API.
///
/// Fija la ruta (<c>/api/[controller]</c>), activa las convenciones de API y declara
/// las respuestas de error comunes para que aparezcan documentadas en Swagger sin
/// repetirlas en cada acción.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public abstract class ControladorBase : ControllerBase
{
    /// <summary>
    /// Lee los parámetros de paginación del query string (<c>?pagina=1&amp;tamanioPagina=50</c>).
    /// Los valores fuera de rango los acota el propio modelo.
    /// </summary>
    protected ParametrosDePaginacion LeerPaginacion(int? pagina, int? tamanioPagina) => new()
    {
        Pagina = pagina ?? 1,
        TamanioPagina = tamanioPagina ?? 50
    };
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualGymAssistant.Application.Common.Models;
using VirtualGymAssistant.Application.Features.Socios;
using VirtualGymAssistant.Application.Features.Socios.Dtos;

namespace VirtualGymAssistant.API.Controllers;

/// <summary>
/// Padrón de socios de la sucursal y ficha personal del socio.
/// </summary>
[Authorize]
[Tags("5 - Socios")]
public class SociosController : ControladorBase
{
    private readonly ServicioDeSocios _servicio;

    public SociosController(ServicioDeSocios servicio) => _servicio = servicio;

    /// <summary>
    /// Lista el padrón de socios de la sucursal activa, con filtros y paginación.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ResultadoPaginado<SocioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultadoPaginado<SocioDto>>> Listar(
        [FromQuery] string? texto,
        [FromQuery] int? idEstado,
        [FromQuery] bool soloActivos,
        [FromQuery] int? pagina,
        [FromQuery] int? tamanioPagina,
        CancellationToken ct)
    {
        var filtro = new FiltroDeSocios
        {
            Texto = texto,
            IdEstado = idEstado,
            SoloActivos = soloActivos
        };

        return Ok(await _servicio.ListarPadronAsync(filtro, LeerPaginacion(pagina, tamanioPagina), ct));
    }

    /// <summary>
    /// Devuelve la ficha del socio autenticado en la sucursal activa.
    /// </summary>
    [HttpGet("mi-ficha")]
    [ProducesResponseType(typeof(SocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SocioDto>> ObtenerMiFicha(CancellationToken ct) =>
        Ok(await _servicio.ObtenerMiFichaAsync(ct));

    /// <summary>Devuelve la ficha de un socio de la sucursal activa.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SocioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SocioDto>> Obtener(int id, CancellationToken ct) =>
        Ok(await _servicio.ObtenerAsync(id, ct));

    /// <summary>
    /// Edita la ficha del socio: estado, objetivo, fecha de nacimiento y observaciones.
    /// </summary>
    /// <remarks>
    /// Al pasarlo a estado Baja se completa la fecha de baja automáticamente; al
    /// reactivarlo, se limpia.
    /// </remarks>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(SocioDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SocioDto>> Actualizar(
        int id, [FromBody] ActualizarSocioRequest request, CancellationToken ct) =>
        Ok(await _servicio.ActualizarAsync(id, request, ct));

    /// <summary>Catálogo de estados de socio.</summary>
    [HttpGet("estados")]
    [ProducesResponseType(typeof(IReadOnlyList<EstadoDeSocioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EstadoDeSocioDto>>> ListarEstados(CancellationToken ct) =>
        Ok(await _servicio.ListarEstadosAsync(ct));

    /// <summary>
    /// Caso de uso "Sumarse a otra sucursal": el usuario autenticado se hace socio de una
    /// sucursal en la que todavía no tenía ficha.
    /// </summary>
    [HttpPost("unirme")]
    [ProducesResponseType(typeof(SocioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SocioDto>> Unirme(
        [FromBody] UnirseASucursalRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await _servicio.UnirmeAAsync(request, ct));
}

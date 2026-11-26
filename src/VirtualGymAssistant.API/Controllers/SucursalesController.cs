using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualGymAssistant.Application.Features.Sucursales;
using VirtualGymAssistant.Application.Features.Sucursales.Dtos;

namespace VirtualGymAssistant.API.Controllers;

/// <summary>
/// Sucursales (gimnasios) de una organización.
/// </summary>
[Authorize]
[Tags("3 - Sucursales")]
public class SucursalesController : ControladorBase
{
    private readonly ServicioDeSucursales _servicio;

    public SucursalesController(ServicioDeSucursales servicio) => _servicio = servicio;

    /// <summary>
    /// Lista las sucursales activas del sistema. Es público: lo usa el formulario de registro.
    /// </summary>
    [HttpGet("publicas")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<SucursalPublicaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SucursalPublicaDto>>> ListarPublicas(CancellationToken ct) =>
        Ok(await _servicio.ListarPublicasAsync(ct));

    /// <summary>
    /// Lista las sucursales que el usuario autenticado puede ver.
    /// </summary>
    /// <remarks>
    /// El dueño y el administrador ven todas las de la organización activa; el resto ve
    /// sólo aquellas en las que tiene un rol asignado.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SucursalDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SucursalDto>>> Listar(CancellationToken ct) =>
        Ok(await _servicio.ListarVisiblesAsync(ct));

    /// <summary>Devuelve el detalle de una sucursal a la que el usuario tiene acceso.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SucursalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SucursalDto>> Obtener(int id, CancellationToken ct) =>
        Ok(await _servicio.ObtenerAsync(id, ct));

    /// <summary>Catálogo de zonas geográficas para el selector del formulario.</summary>
    [HttpGet("zonas")]
    [ProducesResponseType(typeof(IReadOnlyList<ZonaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ZonaDto>>> ListarZonas(CancellationToken ct) =>
        Ok(await _servicio.ListarZonasAsync(ct));

    /// <summary>
    /// Registra una sucursal nueva en la organización. Sólo puede hacerlo el titular.
    /// </summary>
    /// <remarks>
    /// Si es la primera sucursal de la organización queda automáticamente como casa matriz.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(SucursalDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SucursalDto>> Crear(
        [FromBody] CrearSucursalRequest request, CancellationToken ct)
    {
        var creada = await _servicio.CrearAsync(request, ct);

        return CreatedAtAction(nameof(Obtener), new { id = creada.Id }, creada);
    }

    /// <summary>Modifica los datos de contacto y ubicación de una sucursal.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(SucursalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SucursalDto>> Actualizar(
        int id, [FromBody] ActualizarSucursalRequest request, CancellationToken ct) =>
        Ok(await _servicio.ActualizarAsync(id, request, ct));

    /// <summary>
    /// Marca la sucursal como casa matriz. La que lo era deja de serlo automáticamente.
    /// </summary>
    [HttpPut("{id:int}/casa-matriz")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarcarCasaMatriz(int id, CancellationToken ct)
    {
        await _servicio.MarcarCasaMatrizAsync(id, ct);

        return NoContent();
    }

    /// <summary>
    /// Da de baja la sucursal (baja lógica). No se permite dejar la organización sin
    /// ninguna sucursal activa.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DarDeBaja(int id, CancellationToken ct)
    {
        await _servicio.DarDeBajaAsync(id, ct);

        return NoContent();
    }

    /// <summary>Reactiva una sucursal dada de baja.</summary>
    [HttpPut("{id:int}/reactivar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reactivar(int id, CancellationToken ct)
    {
        await _servicio.ReactivarAsync(id, ct);

        return NoContent();
    }
}

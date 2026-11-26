using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualGymAssistant.Application.Features.Organizaciones;
using VirtualGymAssistant.Application.Features.Organizaciones.Dtos;

namespace VirtualGymAssistant.API.Controllers;

/// <summary>
/// Organizaciones (cadenas de gimnasios). Todas las operaciones son del dueño.
/// </summary>
[Authorize]
[Tags("2 - Organizaciones")]
public class OrganizacionesController : ControladorBase
{
    private readonly ServicioDeOrganizaciones _servicio;

    public OrganizacionesController(ServicioDeOrganizaciones servicio) => _servicio = servicio;

    /// <summary>Lista las organizaciones de las que el usuario autenticado es titular.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OrganizacionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrganizacionDto>>> Listar(CancellationToken ct) =>
        Ok(await _servicio.ListarMiasAsync(ct));

    /// <summary>Devuelve el detalle de una organización propia.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrganizacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrganizacionDto>> Obtener(int id, CancellationToken ct) =>
        Ok(await _servicio.ObtenerAsync(id, ct));

    /// <summary>
    /// Crea una organización. Quien la crea queda registrado como su titular y ya puede
    /// registrarle sucursales.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrganizacionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrganizacionDto>> Crear(
        [FromBody] CrearOrganizacionRequest request, CancellationToken ct)
    {
        var creada = await _servicio.CrearAsync(request, ct);

        return CreatedAtAction(nameof(Obtener), new { id = creada.Id }, creada);
    }

    /// <summary>Modifica los datos de una organización propia.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(OrganizacionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizacionDto>> Actualizar(
        int id, [FromBody] ActualizarOrganizacionRequest request, CancellationToken ct) =>
        Ok(await _servicio.ActualizarAsync(id, request, ct));

    /// <summary>
    /// Da de baja la organización. Es una baja lógica y sólo se permite si ya no tiene
    /// sucursales activas.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DarDeBaja(int id, CancellationToken ct)
    {
        await _servicio.DarDeBajaAsync(id, ct);

        return NoContent();
    }
}

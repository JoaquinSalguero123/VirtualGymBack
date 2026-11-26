using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualGymAssistant.Application.Features.Membresias;
using VirtualGymAssistant.Application.Features.Membresias.Dtos;

namespace VirtualGymAssistant.API.Controllers;

/// <summary>
/// Vínculo entre un socio y su plan.
/// </summary>
[Authorize]
[Tags("6 - Planes y membresías")]
public class MembresiasController : ControladorBase
{
    private readonly ServicioDeMembresias _servicio;

    public MembresiasController(ServicioDeMembresias servicio) => _servicio = servicio;

    /// <summary>Historial de membresías de un socio de la sucursal activa.</summary>
    [HttpGet("socio/{idSocio:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<MembresiaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MembresiaDto>>> ListarDeSocio(
        int idSocio, CancellationToken ct) =>
        Ok(await _servicio.ListarDeSocioAsync(idSocio, ct));

    /// <summary>Devuelve la membresía vigente del socio, o 204 si no tiene ninguna.</summary>
    [HttpGet("socio/{idSocio:int}/activa")]
    [ProducesResponseType(typeof(MembresiaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<MembresiaDto>> ObtenerActiva(int idSocio, CancellationToken ct)
    {
        var membresia = await _servicio.ObtenerActivaAsync(idSocio, ct);

        return membresia is null ? NoContent() : Ok(membresia);
    }

    /// <summary>
    /// Asigna un plan a un socio. Si ya tenía otro, la membresía anterior se cierra y se
    /// abre una nueva, de modo que el historial quede completo.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(MembresiaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MembresiaDto>> Asignar(
        [FromBody] AsignarMembresiaRequest request, CancellationToken ct) =>
        Ok(await _servicio.AsignarAsync(request, ct));

    /// <summary>Da de baja la membresía vigente del socio.</summary>
    [HttpDelete("socio/{idSocio:int}/activa")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DarDeBajaActiva(int idSocio, CancellationToken ct)
    {
        await _servicio.DarDeBajaActivaAsync(idSocio, ct);

        return NoContent();
    }
}

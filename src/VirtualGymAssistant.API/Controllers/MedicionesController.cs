using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualGymAssistant.Application.Features.Mediciones;
using VirtualGymAssistant.Application.Features.Mediciones.Dtos;

namespace VirtualGymAssistant.API.Controllers;

/// <summary>
/// Seguimiento físico de los socios.
/// </summary>
[Authorize]
[Tags("13 - Mediciones")]
public class MedicionesController : ControladorBase
{
    private readonly ServicioDeMediciones _servicio;

    public MedicionesController(ServicioDeMediciones servicio) => _servicio = servicio;

    /// <summary>Mediciones del socio autenticado en la sucursal activa.</summary>
    [HttpGet("mias")]
    [ProducesResponseType(typeof(IReadOnlyList<MedicionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MedicionDto>>> ListarMias(CancellationToken ct) =>
        Ok(await _servicio.ListarMisMedicionesAsync(ct));

    /// <summary>Historial de mediciones de un socio, de la más reciente a la más antigua.</summary>
    [HttpGet("socio/{idSocio:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<MedicionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MedicionDto>>> ListarDeSocio(
        int idSocio, CancellationToken ct) =>
        Ok(await _servicio.ListarDeSocioAsync(idSocio, ct));

    /// <summary>
    /// Compara la primera contra la última medición del socio para mostrar su evolución.
    /// </summary>
    [HttpGet("socio/{idSocio:int}/progreso")]
    [ProducesResponseType(typeof(ProgresoDelSocioDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProgresoDelSocioDto>> ObtenerProgreso(
        int idSocio, CancellationToken ct) =>
        Ok(await _servicio.ObtenerProgresoAsync(idSocio, ct));

    /// <summary>
    /// Registra una medición. La carga el entrenador, el nutricionista o la
    /// administración para cualquier socio de la sucursal, o el propio socio para sí
    /// mismo.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(MedicionDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<MedicionDto>> Crear(
        [FromBody] GuardarMedicionRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await _servicio.CrearAsync(request, ct));

    /// <summary>Elimina una medición cargada por error.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await _servicio.EliminarAsync(id, ct);

        return NoContent();
    }
}

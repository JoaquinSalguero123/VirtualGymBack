using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualGymAssistant.Application.Features.Horarios;
using VirtualGymAssistant.Application.Features.Horarios.Dtos;

namespace VirtualGymAssistant.API.Controllers;

/// <summary>
/// Horarios de apertura de la sucursal.
/// </summary>
[Authorize]
[Tags("12 - Sucursal: horarios")]
public class HorariosController : ControladorBase
{
    private readonly ServicioDeHorarios _servicio;

    public HorariosController(ServicioDeHorarios servicio) => _servicio = servicio;

    /// <summary>
    /// Horarios de una sucursal cualquiera. Es público: lo consume el sitio institucional.
    /// </summary>
    [HttpGet("publicos/{idGimnasio:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<HorarioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HorarioDto>>> ListarPublicos(
        int idGimnasio, CancellationToken ct) =>
        Ok(await _servicio.ListarPublicosAsync(idGimnasio, ct));

    /// <summary>Horarios de la sucursal activa.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<HorarioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HorarioDto>>> Listar(CancellationToken ct) =>
        Ok(await _servicio.ListarDeLaSucursalActivaAsync(ct));

    /// <summary>Agrega una franja horaria a la sucursal activa.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(HorarioDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<HorarioDto>> Crear(
        [FromBody] GuardarHorarioRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await _servicio.CrearAsync(request, ct));

    /// <summary>Modifica una franja horaria.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(HorarioDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<HorarioDto>> Actualizar(
        int id, [FromBody] GuardarHorarioRequest request, CancellationToken ct) =>
        Ok(await _servicio.ActualizarAsync(id, request, ct));

    /// <summary>Elimina una franja horaria.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await _servicio.EliminarAsync(id, ct);

        return NoContent();
    }
}

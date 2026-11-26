using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualGymAssistant.Application.Common.Models;
using VirtualGymAssistant.Application.Features.Usuarios;
using VirtualGymAssistant.Application.Features.Usuarios.Dtos;

namespace VirtualGymAssistant.API.Controllers;

/// <summary>
/// Personal del gimnasio y sus roles por sucursal.
/// </summary>
[Authorize]
[Tags("4 - Personal y roles")]
public class UsuariosController : ControladorBase
{
    private readonly ServicioDeUsuarios _servicio;

    public UsuariosController(ServicioDeUsuarios servicio) => _servicio = servicio;

    /// <summary>Catálogo de roles del sistema.</summary>
    [HttpGet("roles")]
    [ProducesResponseType(typeof(IReadOnlyList<RolDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RolDto>>> ListarRoles(CancellationToken ct) =>
        Ok(await _servicio.ListarRolesAsync(ct));

    /// <summary>
    /// Lista el personal de la sucursal activa (no incluye a los socios).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ResultadoPaginado<UsuarioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultadoPaginado<UsuarioDto>>> Listar(
        [FromQuery] string? texto,
        [FromQuery] int? pagina,
        [FromQuery] int? tamanioPagina,
        CancellationToken ct) =>
        Ok(await _servicio.ListarPersonalAsync(texto, LeerPaginacion(pagina, tamanioPagina), ct));

    /// <summary>Devuelve un usuario con el rol que tiene en la sucursal activa.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsuarioDto>> Obtener(int id, CancellationToken ct) =>
        Ok(await _servicio.ObtenerAsync(id, ct));

    /// <summary>
    /// Lista todas las sucursales de la organización en las que el usuario tiene rol.
    /// </summary>
    [HttpGet("{id:int}/asignaciones")]
    [ProducesResponseType(typeof(IReadOnlyList<AsignacionDeRolDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AsignacionDeRolDto>>> ListarAsignaciones(
        int id, CancellationToken ct) =>
        Ok(await _servicio.ListarAsignacionesAsync(id, ct));

    /// <summary>
    /// Da de alta un usuario y le asigna un rol en la sucursal activa.
    /// </summary>
    /// <remarks>
    /// Sólo el dueño puede designar administradores. El rol Dueño no se asigna a mano:
    /// surge de ser titular de la organización.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioDto>> Crear(
        [FromBody] CrearUsuarioRequest request, CancellationToken ct)
    {
        var creado = await _servicio.CrearAsync(request, ct);

        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }

    /// <summary>
    /// Edita los datos del usuario y, si se indica un rol, lo actualiza en la sucursal activa.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UsuarioDto>> Actualizar(
        int id, [FromBody] ActualizarUsuarioRequest request, CancellationToken ct) =>
        Ok(await _servicio.ActualizarAsync(id, request, ct));

    /// <summary>
    /// Asigna al usuario un rol en otra sucursal de la organización.
    /// </summary>
    [HttpPost("{id:int}/asignaciones")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AsignarEnSucursal(
        int id, [FromBody] AsignarRolRequest request, CancellationToken ct)
    {
        await _servicio.AsignarEnSucursalAsync(id, request, ct);

        return NoContent();
    }

    /// <summary>
    /// Quita al usuario de la sucursal activa. Es una baja lógica de la asignación: el
    /// usuario conserva su cuenta y sus roles en las demás sedes.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> QuitarDeSucursal(int id, CancellationToken ct)
    {
        await _servicio.QuitarDeSucursalAsync(id, ct);

        return NoContent();
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Application.Features.Autenticacion;
using VirtualGymAssistant.Application.Features.Autenticacion.Dtos;

namespace VirtualGymAssistant.API.Controllers;

/// <summary>
/// Cuenta y acceso: registro, inicio de sesión y manejo de la contraseña.
/// </summary>
[Tags("1 - Cuenta y acceso")]
public class AuthController : ControladorBase
{
    private readonly ServicioDeAutenticacion _servicio;
    private readonly IUsuarioActual _usuarioActual;

    public AuthController(ServicioDeAutenticacion servicio, IUsuarioActual usuarioActual)
    {
        _servicio = servicio;
        _usuarioActual = usuarioActual;
    }

    /// <summary>
    /// Registra un visitante como socio de una sucursal.
    /// </summary>
    /// <remarks>
    /// Crea el usuario, le genera la ficha de socio en la sucursal elegida y le asigna el
    /// rol Socio. Si no se indica sucursal se usa la casa matriz.
    ///
    /// NO devuelve token: la cuenta queda sin acceso hasta que se confirme el código de 6
    /// dígitos que se manda por mail. Eso se hace con <see cref="VerificarEmail"/>, que es
    /// quien devuelve la sesión. Mientras tanto, el login responde 403 con el código
    /// <c>EMAIL_NO_VERIFICADO</c>.
    /// </remarks>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegistroPendienteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegistroPendienteDto>> Registrar(
        [FromBody] RegistroRequest request, CancellationToken ct)
    {
        var pendiente = await _servicio.RegistrarAsync(request, ct);

        return StatusCode(StatusCodes.Status201Created, pendiente);
    }

    /// <summary>
    /// Inicia sesión y devuelve el token JWT junto con las sucursales del usuario.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SesionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SesionDto>> IniciarSesion(
        [FromBody] LoginRequest request, CancellationToken ct) =>
        Ok(await _servicio.IniciarSesionAsync(request, ct));

    /// <summary>
    /// Devuelve la sesión del usuario autenticado con los datos actualizados.
    /// </summary>
    /// <remarks>
    /// El front la usa al recargar la página y después de que un administrador le cambie
    /// los roles, para refrescar la lista de sucursales sin cerrar sesión. Si la cuenta
    /// todavía no confirmó su email responde 403 (<c>EMAIL_NO_VERIFICADO</c>) en lugar de
    /// renovar el token.
    /// </remarks>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(SesionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SesionDto>> ObtenerSesion(CancellationToken ct) =>
        Ok(await _servicio.ObtenerSesionActualAsync(_usuarioActual.IdRequerido, ct));

    /// <summary>
    /// Solicita un código para restablecer la contraseña.
    /// </summary>
    /// <remarks>
    /// La respuesta es siempre la misma exista o no el email, para no revelar qué
    /// direcciones están registradas.
    /// </remarks>
    [HttpPost("solicitar-recuperacion")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SolicitudRecuperacionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SolicitudRecuperacionDto>> SolicitarRecuperacion(
        [FromBody] SolicitarRecuperacionRequest request, CancellationToken ct) =>
        Ok(await _servicio.SolicitarRecuperacionAsync(request, ct));

    /// <summary>
    /// Restablece la contraseña usando el código recibido.
    /// </summary>
    [HttpPost("restablecer-clave")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RestablecerClave(
        [FromBody] RestablecerClaveRequest request, CancellationToken ct)
    {
        await _servicio.RestablecerClaveAsync(request, ct);

        return NoContent();
    }

    /// <summary>
    /// Cambia la contraseña del usuario autenticado verificando la actual.
    /// </summary>
    [HttpPost("cambiar-clave")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CambiarClave(
        [FromBody] CambiarClaveRequest request, CancellationToken ct)
    {
        await _servicio.CambiarClaveAsync(_usuarioActual.IdRequerido, request, ct);

        return NoContent();
    }

    /// <summary>
    /// Confirma el email con el código que llegó por mail al registrarse (o con el
    /// reenviado desde <see cref="ReenviarVerificacion"/>).
    /// </summary>
    /// <remarks>
    /// Es anónimo: mientras el email no esté verificado <see cref="IniciarSesion"/> lo
    /// rechaza, así que este paso no puede depender de un token propio. Si el código es
    /// válido devuelve la sesión ya iniciada, igual que el login. Después de registrarse es
    /// la única puerta de entrada, porque el registro no emite sesión.
    /// </remarks>
    [HttpPost("verificar-email")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SesionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SesionDto>> VerificarEmail(
        [FromBody] VerificarEmailRequest request, CancellationToken ct) =>
        Ok(await _servicio.VerificarEmailAsync(request, ct));

    /// <summary>
    /// Reenvía el código de verificación del email indicado. Se puede pedir las veces
    /// que haga falta.
    /// </summary>
    /// <remarks>
    /// La respuesta es siempre la misma exista o no el email, o ya esté verificado, para
    /// no revelar qué cuentas hay (mismo criterio que <see cref="SolicitarRecuperacion"/>).
    /// </remarks>
    [HttpPost("reenviar-verificacion")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ReenvioDeVerificacionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReenvioDeVerificacionDto>> ReenviarVerificacion(
        [FromBody] ReenviarVerificacionRequest request, CancellationToken ct) =>
        Ok(await _servicio.ReenviarVerificacionAsync(request, ct));
}

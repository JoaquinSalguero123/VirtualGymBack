namespace VirtualGymAssistant.Application.Features.Autenticacion.Dtos;

/// <summary>
/// Confirmación del email con el código de 6 dígitos que se mandó por mail.
/// Es anónimo (como la recuperación de contraseña): mientras el email no esté
/// verificado el usuario no puede loguearse, así que no puede depender de un token
/// propio para confirmar la cuenta. Por eso necesita el email además del código.
/// </summary>
public class VerificarEmailRequest
{
    public string Email { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
}

/// <summary>Pedido de reenvío del código de verificación. Anónimo, igual que arriba.</summary>
public class ReenviarVerificacionRequest
{
    public string Email { get; set; } = string.Empty;
}

/// <summary>Resultado de pedir que se reenvíe el código de verificación.</summary>
public class ReenvioDeVerificacionDto
{
    public string Mensaje { get; init; } = string.Empty;
}

/// <summary>
/// Resultado del registro: la cuenta quedó creada pero todavía NO tiene acceso.
/// A propósito no trae token ni sesión: se obtienen recién al confirmar el código
/// que se mandó por mail (ver <c>ServicioDeAutenticacion.VerificarEmailAsync</c>).
/// Si el registro devolviera la sesión, bastaría con ignorar la pantalla del código
/// para entrar sin haber verificado nada.
/// </summary>
public class RegistroPendienteDto
{
    /// <summary>Dirección a la que se mandó el código, ya normalizada (minúsculas, sin espacios).</summary>
    public string Email { get; init; } = string.Empty;

    public string Mensaje { get; init; } = string.Empty;
}

namespace VirtualGymAssistant.Application.Features.Autenticacion.Dtos;

/// <summary>Pedido de recuperación de contraseña: el usuario sólo informa su email.</summary>
public class SolicitarRecuperacionRequest
{
    public string Email { get; set; } = string.Empty;
}

/// <summary>Resultado del pedido de recuperación.</summary>
public class SolicitudRecuperacionDto
{
    /// <summary>
    /// Mensaje genérico. Es siempre el mismo exista o no el email, para no revelar
    /// qué direcciones están registradas en el sistema.
    /// </summary>
    public string Mensaje { get; init; } = string.Empty;
}

/// <summary>Restablecimiento de contraseña con el código recibido.</summary>
public class RestablecerClaveRequest
{
    public string Email { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string NuevaClave { get; set; } = string.Empty;
    public string ConfirmacionClave { get; set; } = string.Empty;
}

/// <summary>Cambio de contraseña de un usuario que ya inició sesión.</summary>
public class CambiarClaveRequest
{
    public string ClaveActual { get; set; } = string.Empty;
    public string NuevaClave { get; set; } = string.Empty;
    public string ConfirmacionClave { get; set; } = string.Empty;
}

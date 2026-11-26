namespace VirtualGymAssistant.Application.Features.Autenticacion.Dtos;

/// <summary>Credenciales de inicio de sesión.</summary>
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
}

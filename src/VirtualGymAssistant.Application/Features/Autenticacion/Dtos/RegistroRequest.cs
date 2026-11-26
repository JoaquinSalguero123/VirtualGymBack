namespace VirtualGymAssistant.Application.Features.Autenticacion.Dtos;

/// <summary>Datos que envía un visitante para crear su cuenta y quedar como socio de una sucursal.</summary>
public class RegistroRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string Contrasena { get; set; } = string.Empty;

    /// <summary>
    /// Sucursal a la que quiere asociarse. Si no se indica, se usa la casa matriz
    /// y, si tampoco existe, la primera sucursal activa del sistema.
    /// </summary>
    public int? IdGimnasio { get; set; }
}

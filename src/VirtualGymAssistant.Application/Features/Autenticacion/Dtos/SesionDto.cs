using VirtualGymAssistant.Application.Common.Models;

namespace VirtualGymAssistant.Application.Features.Autenticacion.Dtos;

/// <summary>Respuesta del login: el token y todo lo que el front necesita para armar la sesión.</summary>
public class SesionDto
{
    public string Token { get; init; } = string.Empty;

    /// <summary>Momento exacto (UTC) en que el token deja de ser válido.</summary>
    public DateTime ExpiraEn { get; init; }

    public UsuarioSesionDto Usuario { get; init; } = new();
}

/// <summary>Perfil del usuario logueado junto con las sucursales en las que puede operar.</summary>
public class UsuarioSesionDto
{
    public int Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string? Nombre { get; init; }
    public string? Apellido { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? Telefono { get; init; }
    public bool Activo { get; init; }

    /// <summary>
    /// True si confirmó el código que se le mandó por mail. En una sesión recién emitida
    /// siempre es true: sin el email confirmado no se emite sesión.
    /// </summary>
    public bool EmailVerificado { get; init; }

    /// <summary>True si es titular de alguna organización.</summary>
    public bool EsDuenio { get; init; }

    /// <summary>Sucursales disponibles, ya ordenadas (casa matriz primero).</summary>
    public IReadOnlyList<SucursalDelUsuario> Sucursales { get; init; } = [];

    /// <summary>
    /// Rol de la primera sucursal. Sirve para que el front pueda dibujar el menú antes
    /// de que el usuario elija una sucursal concreta.
    /// </summary>
    public string? RolPrincipal { get; init; }
}

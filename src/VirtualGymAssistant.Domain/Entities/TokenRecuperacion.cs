namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Token de un solo uso para restablecer la contraseña.
/// Tabla: dbo.TokensRecuperacion
/// </summary>
public class TokenRecuperacion
{
    public int Id { get; set; }
    public int IdUsuario { get; set; }

    /// <summary>Token largo enviado por mail (para el enlace de recuperación).</summary>
    public string? Token { get; set; }

    /// <summary>Código corto, pensado para el envío por SMS.</summary>
    public string? Codigo { get; set; }

    /// <summary>Canal por el que se envió: Mail o SMS.</summary>
    public string Canal { get; set; } = "Mail";

    public DateTime Expira { get; set; }
    public bool Usado { get; set; }
    public DateTime FechaCreacion { get; set; }

    public Usuario Usuario { get; set; } = null!;
}

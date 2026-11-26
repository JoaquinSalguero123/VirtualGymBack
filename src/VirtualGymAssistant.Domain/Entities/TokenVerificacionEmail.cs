namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Código de un solo uso para confirmar el mail del usuario.
/// Tabla: dbo.TokensVerificacionEmail
/// </summary>
public class TokenVerificacionEmail
{
    public int Id { get; set; }
    public int IdUsuario { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public DateTime Expira { get; set; }
    public bool Usado { get; set; }
    public DateTime FechaCreacion { get; set; }

    public Usuario Usuario { get; set; } = null!;
}

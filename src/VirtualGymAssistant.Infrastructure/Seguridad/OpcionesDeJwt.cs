namespace VirtualGymAssistant.Infrastructure.Seguridad;

/// <summary>
/// Configuración del token JWT. Se carga desde la sección "Jwt" de appsettings.json.
/// </summary>
public class OpcionesDeJwt
{
    public const string Seccion = "Jwt";

    /// <summary>Quién emite el token (esta API).</summary>
    public string Emisor { get; set; } = "VirtualGymAssistant.API";

    /// <summary>Para quién se emite el token (el front).</summary>
    public string Audiencia { get; set; } = "VirtualGymAssistant.Front";

    /// <summary>
    /// Clave con la que se firma el token. Debe tener al menos 32 caracteres y NUNCA
    /// se versiona con una clave real: en producción va por variable de entorno.
    /// </summary>
    public string ClaveSecreta { get; set; } = string.Empty;

    /// <summary>Minutos de validez del token desde su emisión.</summary>
    public int MinutosDeExpiracion { get; set; } = 480;
}

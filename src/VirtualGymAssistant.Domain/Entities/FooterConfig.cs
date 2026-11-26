namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Dato configurable del pie del sitio, guardado como sección + clave + valor
/// (por ejemplo: Contacto / telefono / 351-4000000).
/// Tabla: dbo.FooterConfig
/// </summary>
public class FooterConfig
{
    public int Id { get; set; }
    public int IdOrganizacion { get; set; }
    public string Seccion { get; set; } = string.Empty;
    public string Clave { get; set; } = string.Empty;
    public string? Valor { get; set; }
    public int? Orden { get; set; }

    public Organizacion Organizacion { get; set; } = null!;
}

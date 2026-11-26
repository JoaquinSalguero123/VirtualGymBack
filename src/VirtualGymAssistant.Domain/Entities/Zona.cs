namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Zona geográfica (barrio / localidad) a la que puede pertenecer una sucursal.
/// Tabla: dbo.Zonas
/// </summary>
public class Zona
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Localidad { get; set; }
    public string? Provincia { get; set; }

    public ICollection<Gimnasio> Gimnasios { get; set; } = new List<Gimnasio>();
}

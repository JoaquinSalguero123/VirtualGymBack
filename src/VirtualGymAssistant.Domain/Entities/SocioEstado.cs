namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Estado administrativo de un socio (Activo, Suspendido, Moroso, Baja).
/// Tabla: dbo.SocioEstados
/// </summary>
public class SocioEstado
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public ICollection<Socio> Socios { get; set; } = new List<Socio>();
}

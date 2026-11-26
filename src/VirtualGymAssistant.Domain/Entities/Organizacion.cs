namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Empresa o cadena de gimnasios. Es el tope de la jerarquía: un dueño posee una
/// organización y la organización agrupa 1..N sucursales.
/// Los planes y las plantillas de rutina son de la organización, no de la sucursal.
/// Tabla: dbo.Organizaciones
/// </summary>
public class Organizacion
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? RazonSocial { get; set; }
    public string? Cuit { get; set; }

    /// <summary>Usuario titular. Tiene permisos totales sobre todas las sucursales.</summary>
    public int IdDuenio { get; set; }

    public bool Activo { get; set; } = true;
    public DateTime FechaAlta { get; set; }

    // --- Relaciones ---
    public Usuario Duenio { get; set; } = null!;
    public ICollection<Gimnasio> Gimnasios { get; set; } = new List<Gimnasio>();
    public ICollection<Plan> Planes { get; set; } = new List<Plan>();
    public ICollection<Rutina> Rutinas { get; set; } = new List<Rutina>();
    public ICollection<CarouselItem> ItemsDeCarousel { get; set; } = new List<CarouselItem>();
    public ICollection<FooterConfig> ConfiguracionDePie { get; set; } = new List<FooterConfig>();
    public ICollection<ForoPost> PublicacionesDeForo { get; set; } = new List<ForoPost>();
}

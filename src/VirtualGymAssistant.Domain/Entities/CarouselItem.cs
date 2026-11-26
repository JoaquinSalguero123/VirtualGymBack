namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Contenido del sitio público (servicios, frases, fotos). Pertenece a la organización;
/// si además se indica una sucursal, el item se muestra sólo para esa sede.
/// Tabla: dbo.CarouselItems
/// </summary>
public class CarouselItem
{
    public int Id { get; set; }
    public int IdOrganizacion { get; set; }
    public int? IdGimnasio { get; set; }

    /// <summary>Agrupa los items dentro del sitio: "servicios", "frases", etc.</summary>
    public string? Tipo { get; set; }

    public string? Titulo { get; set; }
    public string? Descripcion { get; set; }

    /// <summary>Nombre del archivo subido. Se sirve desde la carpeta pública de uploads.</summary>
    public string? Imagen { get; set; }

    public int? Orden { get; set; }
    public bool Activo { get; set; } = true;

    // --- Relaciones ---
    public Organizacion Organizacion { get; set; } = null!;
    public Gimnasio? Gimnasio { get; set; }
}

namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Rutina concreta asignada a un socio. Puede nacer de una plantilla o armarse a mano.
/// Regla de negocio: un socio tiene como máximo una rutina activa a la vez.
/// Tabla: dbo.SocioRutinas
/// </summary>
public class SocioRutina
{
    public int Id { get; set; }
    public int IdSocio { get; set; }

    /// <summary>Plantilla de origen, si la rutina se generó a partir de una.</summary>
    public int? IdRutina { get; set; }

    /// <summary>Entrenador que la asignó. Es nulo si la armó el propio socio.</summary>
    public int? IdEntrenador { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public DateOnly FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
    public bool Activa { get; set; } = true;

    // --- Relaciones ---
    public Socio Socio { get; set; } = null!;
    public Rutina? Plantilla { get; set; }
    public Usuario? Entrenador { get; set; }
    public ICollection<SocioRutinaDetalle> Detalle { get; set; } = new List<SocioRutinaDetalle>();
}

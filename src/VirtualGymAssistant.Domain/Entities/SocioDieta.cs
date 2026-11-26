namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Plan de alimentación asignado a un socio.
/// Regla de negocio: un socio tiene como máximo una dieta activa a la vez.
/// Tabla: dbo.SocioDietas
/// </summary>
public class SocioDieta
{
    public int Id { get; set; }
    public int IdSocio { get; set; }

    /// <summary>Nutricionista que la armó. Es nulo si la generó el propio socio.</summary>
    public int? IdNutricionista { get; set; }

    public string Nombre { get; set; } = "Dieta personalizada";
    public int? IdObjetivo { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
    public bool Activa { get; set; } = true;

    // --- Relaciones ---
    public Socio Socio { get; set; } = null!;
    public Usuario? Nutricionista { get; set; }
    public Objetivo? Objetivo { get; set; }
    public ICollection<SocioDietaDetalle> Detalle { get; set; } = new List<SocioDietaDetalle>();
}

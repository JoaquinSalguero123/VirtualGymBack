namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Control físico de un socio en una fecha. La serie de mediciones es lo que permite
/// mostrar la evolución del socio.
/// Tabla: dbo.Mediciones
/// </summary>
public class Medicion
{
    public int Id { get; set; }
    public int IdSocio { get; set; }
    public DateOnly Fecha { get; set; }

    /// <summary>Peso en kilogramos. Es el único valor obligatorio.</summary>
    public decimal Peso { get; set; }

    public decimal? GrasaPct { get; set; }
    public decimal? MusculoPct { get; set; }
    public decimal? Cintura { get; set; }
    public decimal? Cadera { get; set; }
    public decimal? Pecho { get; set; }
    public decimal? Brazo { get; set; }
    public decimal? Pierna { get; set; }

    /// <summary>Usuario del staff que tomó la medición.</summary>
    public int? IdRegistradoPor { get; set; }

    public string? Observaciones { get; set; }

    // --- Relaciones ---
    public Socio Socio { get; set; } = null!;
    public Usuario? RegistradoPor { get; set; }
}

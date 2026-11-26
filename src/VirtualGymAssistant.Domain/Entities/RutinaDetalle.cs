namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Un ejercicio dentro de un día de una plantilla de rutina.
/// Tabla: dbo.RutinaDetalle
/// </summary>
public class RutinaDetalle
{
    public int Id { get; set; }
    public int IdRutina { get; set; }

    /// <summary>Número de día dentro de la rutina (1 = primer día de entrenamiento).</summary>
    public byte Dia { get; set; }

    /// <summary>Posición del ejercicio dentro del día.</summary>
    public int Orden { get; set; } = 1;

    public int IdEjercicio { get; set; }
    public int? Series { get; set; }

    /// <summary>Texto libre porque admite rangos ("8-10") o tiempos ("40 s").</summary>
    public string? Repeticiones { get; set; }

    public int? DescansoSeg { get; set; }

    // --- Relaciones ---
    public Rutina Rutina { get; set; } = null!;
    public Ejercicio Ejercicio { get; set; } = null!;
}

namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Un ejercicio dentro de un día de la rutina asignada a un socio.
/// Tabla: dbo.SocioRutinaDetalle
/// </summary>
public class SocioRutinaDetalle
{
    public int Id { get; set; }
    public int IdSocioRutina { get; set; }
    public int Dia { get; set; }
    public int Orden { get; set; } = 1;
    public int IdEjercicio { get; set; }
    public int? Series { get; set; }
    public string? Repeticiones { get; set; }
    public int? DescansoSeg { get; set; }

    /// <summary>Indicaciones del entrenador para ese ejercicio en particular.</summary>
    public string? Notas { get; set; }

    // --- Relaciones ---
    public SocioRutina SocioRutina { get; set; } = null!;
    public Ejercicio Ejercicio { get; set; } = null!;
}

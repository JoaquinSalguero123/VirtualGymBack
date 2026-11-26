namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Check-in de un socio en una sucursal. La regla de negocio permite una sola
/// asistencia por socio y por día.
/// Tabla: dbo.Asistencias
/// </summary>
public class Asistencia
{
    public int Id { get; set; }
    public int IdSocio { get; set; }
    public int IdGimnasio { get; set; }
    public DateTime FechaHoraEntrada { get; set; }
    public DateTime? FechaHoraSalida { get; set; }

    /// <summary>
    /// Columna calculada y persistida en la base (CAST de la entrada a date).
    /// EF la trata como de solo lectura: la calcula SQL Server, no la aplicación.
    /// </summary>
    public DateOnly Fecha { get; set; }

    // --- Relaciones ---
    public Socio Socio { get; set; } = null!;
    public Gimnasio Gimnasio { get; set; } = null!;
}

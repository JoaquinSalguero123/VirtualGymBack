namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Franja de apertura de una sucursal para un día de la semana.
/// Tabla: dbo.Horarios
/// </summary>
public class Horario
{
    public int Id { get; set; }
    public int IdGimnasio { get; set; }

    /// <summary>1 = Lunes ... 7 = Domingo.</summary>
    public byte DiaSemana { get; set; }

    public TimeOnly HoraApertura { get; set; }
    public TimeOnly HoraCierre { get; set; }

    /// <summary>Permite ordenar la grilla en el sitio sin depender del día.</summary>
    public int? Orden { get; set; }

    public Gimnasio Gimnasio { get; set; } = null!;
}

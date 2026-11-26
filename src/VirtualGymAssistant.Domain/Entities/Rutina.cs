namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Plantilla de rutina reutilizable. Si <see cref="IdOrganizacion"/> es nulo la plantilla
/// es global (la ven todas las organizaciones); si tiene valor, es propia de esa organización.
/// Tabla: dbo.Rutinas
/// </summary>
public class Rutina
{
    public int Id { get; set; }
    public int? IdOrganizacion { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Intensidad { get; set; }
    public int? IdObjetivo { get; set; }
    public int? DiasPorSemana { get; set; }
    public int? EjerciciosPorDia { get; set; }
    public bool Activa { get; set; } = true;

    // --- Relaciones ---
    public Organizacion? Organizacion { get; set; }
    public Objetivo? Objetivo { get; set; }
    public ICollection<RutinaDetalle> Detalle { get; set; } = new List<RutinaDetalle>();
}

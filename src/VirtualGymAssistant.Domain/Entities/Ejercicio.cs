namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Ejercicio del catálogo global, clasificado por grupo muscular.
/// Tabla: dbo.Ejercicios
/// </summary>
public class Ejercicio
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int IdGrupo { get; set; }
    public string? VideoUrl { get; set; }

    // --- Relaciones ---
    public GrupoMuscular Grupo { get; set; } = null!;
    public ICollection<RutinaDetalle> EnPlantillas { get; set; } = new List<RutinaDetalle>();
    public ICollection<SocioRutinaDetalle> EnRutinasDeSocios { get; set; } = new List<SocioRutinaDetalle>();
}

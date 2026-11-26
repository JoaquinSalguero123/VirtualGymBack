namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Grupo muscular que trabaja un ejercicio (Pecho, Espalda, Piernas...).
/// Tabla: dbo.GruposMusculares
/// </summary>
public class GrupoMuscular
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public ICollection<Ejercicio> Ejercicios { get; set; } = new List<Ejercicio>();
}

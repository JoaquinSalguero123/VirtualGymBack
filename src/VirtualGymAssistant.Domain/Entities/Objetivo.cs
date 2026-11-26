namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Objetivo de entrenamiento / alimentación (Ganar masa muscular, Perder grasa, etc.).
/// Tabla: dbo.Objetivos
/// </summary>
public class Objetivo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public ICollection<Alimento> Alimentos { get; set; } = new List<Alimento>();
    public ICollection<Socio> Socios { get; set; } = new List<Socio>();
    public ICollection<Rutina> Rutinas { get; set; } = new List<Rutina>();
}

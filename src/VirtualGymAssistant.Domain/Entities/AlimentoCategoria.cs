namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Categoría nutricional de un alimento (Proteínas, Carbohidratos, Grasas...).
/// Tabla: dbo.AlimentosCategorias
/// </summary>
public class AlimentoCategoria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public ICollection<Alimento> Alimentos { get; set; } = new List<Alimento>();
}

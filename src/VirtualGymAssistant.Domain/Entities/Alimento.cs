namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Alimento del catálogo con su información nutricional (valores por 100 g).
/// El objetivo sugerido se usa para armar dietas automáticas.
/// Tabla: dbo.Alimentos
/// </summary>
public class Alimento
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal? Calorias { get; set; }
    public decimal? Proteinas { get; set; }
    public decimal? Carbohidratos { get; set; }
    public decimal? Grasas { get; set; }
    public string? Descripcion { get; set; }
    public int? IdCategoria { get; set; }
    public int? IdObjetivo { get; set; }

    // --- Relaciones ---
    public AlimentoCategoria? Categoria { get; set; }
    public Objetivo? Objetivo { get; set; }
}

namespace VirtualGymAssistant.Application.Features.Alimentos.Dtos;

/// <summary>Alimento del catálogo con su información nutricional.</summary>
public class AlimentoDto
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public decimal? Calorias { get; init; }
    public decimal? Proteinas { get; init; }
    public decimal? Carbohidratos { get; init; }
    public decimal? Grasas { get; init; }
    public string? Descripcion { get; init; }

    public int? IdCategoria { get; init; }
    public string? Categoria { get; init; }
    public int? IdObjetivo { get; init; }
    public string? Objetivo { get; init; }
}

/// <summary>Alta o modificación de un alimento.</summary>
public class GuardarAlimentoRequest
{
    public string Nombre { get; set; } = string.Empty;
    public decimal? Calorias { get; set; }
    public decimal? Proteinas { get; set; }
    public decimal? Carbohidratos { get; set; }
    public decimal? Grasas { get; set; }
    public string? Descripcion { get; set; }
    public int? IdCategoria { get; set; }
    public int? IdObjetivo { get; set; }
}

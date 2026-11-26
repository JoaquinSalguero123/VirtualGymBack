namespace VirtualGymAssistant.Application.Features.Dietas.Dtos;

/// <summary>Una comida dentro de un día de dieta.</summary>
public class ComidaDto
{
    /// <summary>Momento del día o categoría nutricional (Desayuno, Proteínas...).</summary>
    public string Categoria { get; init; } = string.Empty;

    /// <summary>Qué se come. Es texto libre para poder describir preparaciones.</summary>
    public string Alimento { get; init; } = string.Empty;

    /// <summary>Alimento del catálogo, cuando la comida sale de ahí.</summary>
    public int? IdAlimento { get; init; }

    public decimal? Calorias { get; init; }
    public decimal? Proteinas { get; init; }
    public decimal? Carbohidratos { get; init; }
    public decimal? Grasas { get; init; }
}

/// <summary>Un día del plan de alimentación.</summary>
public class DiaDeDietaDto
{
    /// <summary>Etiqueta del día ("Día 1", "Día 2"...).</summary>
    public string Dia { get; init; } = string.Empty;

    public IReadOnlyList<ComidaDto> Comidas { get; init; } = [];

    /// <summary>Suma de calorías del día, cuando los alimentos vienen del catálogo.</summary>
    public decimal TotalCalorias => Comidas.Sum(c => c.Calorias ?? 0);
}

/// <summary>Dieta asignada a un socio.</summary>
public class DietaDeSocioDto
{
    public int Id { get; init; }
    public int IdSocio { get; init; }
    public string Socio { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;

    public int? IdObjetivo { get; init; }
    public string? Objetivo { get; init; }

    public int? IdNutricionista { get; init; }
    public string? Nutricionista { get; init; }

    public DateOnly FechaInicio { get; init; }
    public DateOnly? FechaFin { get; init; }
    public bool Activa { get; init; }

    public IReadOnlyList<DiaDeDietaDto> Dias { get; init; } = [];
}

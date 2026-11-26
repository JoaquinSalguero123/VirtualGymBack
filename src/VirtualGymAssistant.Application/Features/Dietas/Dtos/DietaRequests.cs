namespace VirtualGymAssistant.Application.Features.Dietas.Dtos;

/// <summary>Una comida que se quiere incluir en el plan.</summary>
public class ComidaRequest
{
    public string Categoria { get; set; } = string.Empty;
    public string Alimento { get; set; } = string.Empty;

    /// <summary>Alimento del catálogo, si la comida sale de ahí.</summary>
    public int? IdAlimento { get; set; }
}

/// <summary>Un día del plan con sus comidas.</summary>
public class DiaDeDietaRequest
{
    public string Dia { get; set; } = string.Empty;
    public List<ComidaRequest> Comidas { get; set; } = [];
}

/// <summary>
/// Pedido de generación automática de una dieta.
/// El sistema elige alimentos del catálogo asociados al objetivo indicado.
/// </summary>
public class GenerarDietaRequest
{
    /// <summary>Objetivo que guía la selección de alimentos.</summary>
    public int? IdObjetivo { get; set; }

    /// <summary>Cantidad de días a generar (típicamente 7 para semanal o 30 para mensual).</summary>
    public int Dias { get; set; } = 7;

    /// <summary>
    /// Categorías a incluir en cada día. Si viene vacío se usan todas las que tengan
    /// alimentos disponibles.
    /// </summary>
    public List<int> IdsDeCategorias { get; set; } = [];
}

/// <summary>Guardado de la dieta del socio.</summary>
public class GuardarDietaRequest
{
    public string? Nombre { get; set; }
    public int? IdObjetivo { get; set; }
    public List<DiaDeDietaRequest> Dias { get; set; } = [];
}

/// <summary>Asignación de una dieta a un socio por parte del nutricionista.</summary>
public class AsignarDietaRequest : GuardarDietaRequest
{
    public int IdSocio { get; set; }
}

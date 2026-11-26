namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Una comida dentro de un día de la dieta del socio.
/// Tabla: dbo.SocioDietaDetalle
/// </summary>
public class SocioDietaDetalle
{
    public int Id { get; set; }
    public int IdSocioDieta { get; set; }

    /// <summary>Etiqueta del día ("Día 1", "Día 2"...).</summary>
    public string Dia { get; set; } = string.Empty;

    /// <summary>Momento o categoría de la comida (Desayuno, Almuerzo, Proteínas...).</summary>
    public string Categoria { get; set; } = string.Empty;

    /// <summary>Descripción de lo que se come. Es texto libre para admitir preparaciones.</summary>
    public string Alimento { get; set; } = string.Empty;

    /// <summary>Referencia opcional al alimento del catálogo, para poder calcular macros.</summary>
    public int? IdAlimento { get; set; }

    // --- Relaciones ---
    public SocioDieta SocioDieta { get; set; } = null!;
    public Alimento? AlimentoDelCatalogo { get; set; }
}

namespace VirtualGymAssistant.Application.Features.Catalogos.Dtos;

/// <summary>
/// Entrada de un catálogo simple (objetivos, grupos musculares, categorías de alimentos):
/// sólo tiene un nombre y se informa si está en uso para saber si se puede borrar.
/// </summary>
public class ItemDeCatalogoDto
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;

    /// <summary>Cantidad de registros que dependen de este item.</summary>
    public int Usos { get; init; }

    /// <summary>True si no lo referencia nadie y por lo tanto se puede eliminar.</summary>
    public bool SePuedeEliminar => Usos == 0;
}

/// <summary>Alta o modificación de un item de catálogo.</summary>
public class GuardarItemDeCatalogoRequest
{
    public string Nombre { get; set; } = string.Empty;
}

namespace VirtualGymAssistant.Application.Features.FormasDePago.Dtos;

/// <summary>Medio de pago aceptado.</summary>
public class FormaDePagoDto
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public bool Activo { get; init; }

    /// <summary>Cantidad de pagos registrados con esta forma. Si es mayor a cero, no se puede borrar.</summary>
    public int PagosRegistrados { get; init; }
}

/// <summary>Alta o modificación de una forma de pago.</summary>
public class GuardarFormaDePagoRequest
{
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}

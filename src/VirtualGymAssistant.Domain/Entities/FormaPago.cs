namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Medio de pago aceptado (Efectivo, Transferencia, Mercado Pago...).
/// Se da de baja de forma lógica con <see cref="Activo"/> para no romper el historial de pagos.
/// Tabla: dbo.FormasPago
/// </summary>
public class FormaPago
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}

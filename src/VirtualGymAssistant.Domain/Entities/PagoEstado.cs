namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Estado de una cuota (Pendiente, Pagado, Rechazado, Vencido, Anulado).
/// Tabla: dbo.PagoEstados
/// </summary>
public class PagoEstado
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}

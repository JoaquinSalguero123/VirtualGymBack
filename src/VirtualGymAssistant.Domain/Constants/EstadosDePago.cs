namespace VirtualGymAssistant.Domain.Constants;

/// <summary>Nombres de los estados cargados en dbo.PagoEstados.</summary>
public static class EstadosDePago
{
    public const string Pendiente = "Pendiente";
    public const string Pagado = "Pagado";
    public const string Rechazado = "Rechazado";
    public const string Vencido = "Vencido";
    public const string Anulado = "Anulado";

    /// <summary>
    /// Estados que bloquean un nuevo pago del mismo período: ya hay uno en curso o aprobado.
    /// </summary>
    public static readonly string[] BloqueanNuevoPago = [Pendiente, Pagado];

    /// <summary>
    /// Estados desde los que se puede reintentar el pago reutilizando el mismo registro.
    /// </summary>
    public static readonly string[] PermitenReintento = [Rechazado, Vencido, Anulado];
}

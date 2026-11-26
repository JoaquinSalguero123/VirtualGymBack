namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Cuota de un socio para un período (año + mes). La base impone un único pago por
/// socio y período, así que un "reintento" reutiliza el registro rechazado en lugar
/// de crear uno nuevo.
/// Tabla: dbo.Pagos
/// </summary>
public class Pago
{
    public int Id { get; set; }
    public int IdSocio { get; set; }
    public int IdGimnasio { get; set; }
    public int? IdMembresia { get; set; }
    public int? IdPlan { get; set; }
    public int IdFormaPago { get; set; }

    /// <summary>Año del período que se está pagando.</summary>
    public short PeriodoAnio { get; set; }

    /// <summary>Mes del período que se está pagando (1 a 12).</summary>
    public byte PeriodoMes { get; set; }

    public decimal Monto { get; set; }
    public int IdEstadoPago { get; set; }
    public DateOnly? FechaVencimiento { get; set; }

    /// <summary>Se completa recién cuando el staff aprueba el pago.</summary>
    public DateTime? FechaPago { get; set; }

    /// <summary>Momento en que el socio cargó el pago.</summary>
    public DateTime FechaRegistro { get; set; }

    public string? Comprobante { get; set; }
    public string? Observaciones { get; set; }

    // --- Relaciones ---
    public Socio Socio { get; set; } = null!;
    public Gimnasio Gimnasio { get; set; } = null!;
    public Membresia? Membresia { get; set; }
    public Plan? Plan { get; set; }
    public FormaPago FormaPago { get; set; } = null!;
    public PagoEstado Estado { get; set; } = null!;
}

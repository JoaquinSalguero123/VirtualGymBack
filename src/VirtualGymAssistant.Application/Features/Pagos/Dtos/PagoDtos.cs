namespace VirtualGymAssistant.Application.Features.Pagos.Dtos;

/// <summary>Un pago de cuota con todos los datos que muestran las grillas.</summary>
public class PagoDto
{
    public int Id { get; init; }
    public int IdSocio { get; init; }
    public string Socio { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public int? NumeroSocio { get; init; }

    public int IdGimnasio { get; init; }
    public string Sucursal { get; init; } = string.Empty;

    public short PeriodoAnio { get; init; }
    public byte PeriodoMes { get; init; }

    /// <summary>Nombre del mes del período, listo para mostrar.</summary>
    public string Mes { get; init; } = string.Empty;

    public decimal Monto { get; init; }

    public int IdEstado { get; init; }
    public string Estado { get; init; } = string.Empty;

    public int IdFormaPago { get; init; }
    public string FormaPago { get; init; } = string.Empty;

    public int? IdPlan { get; init; }
    public string? Plan { get; init; }

    public DateOnly? FechaVencimiento { get; init; }
    public DateTime? FechaPago { get; init; }
    public DateTime FechaRegistro { get; init; }
    public string? Comprobante { get; init; }
    public string? Observaciones { get; init; }
}

/// <summary>Carga de un pago por parte del socio: queda pendiente de aprobación.</summary>
public class RegistrarPagoRequest
{
    /// <summary>Mes del período. Si no se indica, se toma el mes en curso.</summary>
    public int? Mes { get; set; }

    /// <summary>Año del período. Si no se indica, se toma el año en curso.</summary>
    public int? Anio { get; set; }

    public int IdPlan { get; set; }
    public int IdFormaPago { get; set; }

    /// <summary>Número de comprobante o de operación, cuando el medio de pago lo genera.</summary>
    public string? Comprobante { get; set; }
}

/// <summary>Cobro tomado en el mostrador: se registra ya aprobado.</summary>
public class RegistrarCobroPresencialRequest
{
    public int IdSocio { get; set; }
    public int? Mes { get; set; }
    public int? Anio { get; set; }
    public int IdPlan { get; set; }
    public int IdFormaPago { get; set; }
    public string? Comprobante { get; set; }

    /// <summary>Monto efectivamente cobrado. Si no se indica se usa el precio del plan.</summary>
    public decimal? Monto { get; set; }
}

/// <summary>Pedido para mandar por mail la constancia de un pago ya aprobado.</summary>
public class EnviarConstanciaDePagoRequest
{
    /// <summary>
    /// El PDF de la constancia, en base64. Lo arma el navegador con el mismo criterio
    /// que la rutina o el plan de alimentación (ver dominio/pdf.js del front); acá sólo
    /// se adjunta al mail.
    /// </summary>
    public string PdfBase64 { get; set; } = string.Empty;
}

/// <summary>Aprobación o rechazo de un pago pendiente.</summary>
public class CambiarEstadoDePagoRequest
{
    /// <summary>Estado destino: Pagado, Rechazado, Vencido o Anulado.</summary>
    public string Estado { get; set; } = string.Empty;

    /// <summary>Motivo del rechazo o aclaración del cobro.</summary>
    public string? Observaciones { get; set; }
}

/// <summary>Fila de la grilla anual de cuotas: un socio y el estado de sus 12 meses.</summary>
public class FilaDeGrillaAnualDto
{
    public int IdSocio { get; init; }
    public string Socio { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public int? NumeroSocio { get; init; }
    public string Plan { get; init; } = "Sin plan";

    /// <summary>
    /// Estado de cada mes, de enero a diciembre. La posición 0 es enero.
    /// Un mes sin pago cargado viene en null.
    /// </summary>
    public string?[] EstadoPorMes { get; init; } = new string?[12];

    public int MesesPagados { get; init; }
    public decimal TotalPagado { get; init; }
    public decimal TotalPendiente { get; init; }
}

/// <summary>Métricas de cobranza de la sucursal para un año.</summary>
public class ResumenDeCobranzaDto
{
    public int Anio { get; init; }
    public int Mes { get; init; }

    public int SociosTotales { get; init; }
    public int SociosActivos { get; init; }

    public int CuotasPagadas { get; init; }
    public int CuotasPendientes { get; init; }
    public int CuotasRechazadas { get; init; }

    public decimal RecaudacionAnual { get; init; }
    public decimal RecaudacionDelMes { get; init; }

    /// <summary>Monto cargado por los socios que todavía espera aprobación.</summary>
    public decimal MontoPendienteDeAprobacion { get; init; }

    /// <summary>Socios con la cuota del mes en curso aprobada.</summary>
    public int SociosAlDia { get; init; }

    /// <summary>Socios sin la cuota del mes en curso aprobada.</summary>
    public int SociosConDeuda { get; init; }
}

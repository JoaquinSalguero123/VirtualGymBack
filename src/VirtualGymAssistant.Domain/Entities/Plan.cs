namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Plan comercial que la organización ofrece a sus socios (Full, Estándar, Básico...).
/// El precio del plan es el que se toma como monto de la cuota al registrar un pago.
/// Tabla: dbo.Planes
/// </summary>
public class Plan
{
    public int Id { get; set; }
    public int IdOrganizacion { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }

    /// <summary>Duración en días de una cuota del plan (por defecto 30).</summary>
    public int DuracionDias { get; set; } = 30;

    public string? Nivel { get; set; }

    /// <summary>Si es true, el socio puede entrenar en cualquier sucursal de la organización.</summary>
    public bool AccesoMultiSede { get; set; }

    /// <summary>Baja lógica: el plan deja de ofrecerse pero se conserva en el historial de pagos.</summary>
    public bool Activo { get; set; } = true;

    // --- Relaciones ---
    public Organizacion Organizacion { get; set; } = null!;
    public ICollection<Membresia> Membresias { get; set; } = new List<Membresia>();
    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}

namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Vínculo vigente entre un socio y un plan. La base garantiza (índice único filtrado)
/// que un socio tenga como máximo UNA membresía activa a la vez.
/// Tabla: dbo.Membresias
/// </summary>
public class Membresia
{
    public int Id { get; set; }
    public int IdSocio { get; set; }
    public int IdPlan { get; set; }
    public int IdGimnasio { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }

    /// <summary>
    /// Precio congelado al momento del alta. Se guarda aparte del precio del plan para que
    /// un aumento posterior no altere lo que ya se le cobró al socio.
    /// </summary>
    public decimal PrecioAcordado { get; set; }

    public bool Activa { get; set; } = true;

    // --- Relaciones ---
    public Socio Socio { get; set; } = null!;
    public Plan Plan { get; set; } = null!;
    public Gimnasio Gimnasio { get; set; } = null!;
    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}

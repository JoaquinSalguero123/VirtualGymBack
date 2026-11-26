namespace VirtualGymAssistant.Application.Features.Membresias.Dtos;

/// <summary>Membresía de un socio con su plan.</summary>
public class MembresiaDto
{
    public int Id { get; init; }
    public int IdSocio { get; init; }
    public string Socio { get; init; } = string.Empty;
    public int IdPlan { get; init; }
    public string Plan { get; init; } = string.Empty;
    public int IdGimnasio { get; init; }
    public string Sucursal { get; init; } = string.Empty;

    public DateOnly FechaInicio { get; init; }
    public DateOnly? FechaFin { get; init; }

    /// <summary>Precio congelado al momento del alta de la membresía.</summary>
    public decimal PrecioAcordado { get; init; }

    public bool Activa { get; init; }
    public bool AccesoMultiSede { get; init; }
}

/// <summary>Alta o cambio de plan de un socio.</summary>
public class AsignarMembresiaRequest
{
    public int IdSocio { get; set; }
    public int IdPlan { get; set; }

    /// <summary>
    /// Precio a cobrar. Si no se indica, se toma el precio actual del plan.
    /// Permite pactar un precio distinto (promociones, convenios).
    /// </summary>
    public decimal? PrecioAcordado { get; set; }
}

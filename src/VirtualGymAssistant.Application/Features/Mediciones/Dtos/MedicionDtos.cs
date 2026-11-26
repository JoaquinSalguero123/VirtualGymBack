namespace VirtualGymAssistant.Application.Features.Mediciones.Dtos;

/// <summary>Control físico de un socio en una fecha.</summary>
public class MedicionDto
{
    public int Id { get; init; }
    public int IdSocio { get; init; }
    public string Socio { get; init; } = string.Empty;
    public DateOnly Fecha { get; init; }

    public decimal Peso { get; init; }
    public decimal? GrasaPct { get; init; }
    public decimal? MusculoPct { get; init; }
    public decimal? Cintura { get; init; }
    public decimal? Cadera { get; init; }
    public decimal? Pecho { get; init; }
    public decimal? Brazo { get; init; }
    public decimal? Pierna { get; init; }

    public int? IdRegistradoPor { get; init; }
    public string? RegistradoPor { get; init; }
    public string? Observaciones { get; init; }
}

/// <summary>Carga de una medición.</summary>
public class GuardarMedicionRequest
{
    public int IdSocio { get; set; }

    /// <summary>Si no se indica, se toma la fecha de hoy.</summary>
    public DateOnly? Fecha { get; set; }

    public decimal Peso { get; set; }
    public decimal? GrasaPct { get; set; }
    public decimal? MusculoPct { get; set; }
    public decimal? Cintura { get; set; }
    public decimal? Cadera { get; set; }
    public decimal? Pecho { get; set; }
    public decimal? Brazo { get; set; }
    public decimal? Pierna { get; set; }
    public string? Observaciones { get; set; }
}

/// <summary>Evolución del socio entre su primera y su última medición.</summary>
public class ProgresoDelSocioDto
{
    public int IdSocio { get; init; }
    public MedicionDto? PrimeraMedicion { get; init; }
    public MedicionDto? UltimaMedicion { get; init; }

    /// <summary>Diferencia de peso entre la última y la primera medición (negativa si bajó).</summary>
    public decimal? DiferenciaDePeso { get; init; }

    public decimal? DiferenciaDeGrasa { get; init; }
    public decimal? DiferenciaDeMusculo { get; init; }
    public int CantidadDeMediciones { get; init; }
}

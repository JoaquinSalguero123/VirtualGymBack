namespace VirtualGymAssistant.Application.Features.Horarios.Dtos;

/// <summary>Franja horaria de apertura de una sucursal.</summary>
public class HorarioDto
{
    public int Id { get; init; }
    public int IdGimnasio { get; init; }
    public byte DiaSemana { get; init; }

    /// <summary>Nombre del día, listo para mostrar.</summary>
    public string Dia { get; init; } = string.Empty;

    /// <summary>Hora de apertura en formato HH:mm.</summary>
    public string Apertura { get; init; } = string.Empty;

    /// <summary>Hora de cierre en formato HH:mm.</summary>
    public string Cierre { get; init; } = string.Empty;

    public int? Orden { get; init; }
}

/// <summary>Alta o modificación de una franja horaria.</summary>
public class GuardarHorarioRequest
{
    /// <summary>1 = Lunes ... 7 = Domingo.</summary>
    public byte DiaSemana { get; set; }

    /// <summary>Hora de apertura en formato HH:mm.</summary>
    public string Apertura { get; set; } = string.Empty;

    /// <summary>Hora de cierre en formato HH:mm.</summary>
    public string Cierre { get; set; } = string.Empty;

    public int? Orden { get; set; }
}

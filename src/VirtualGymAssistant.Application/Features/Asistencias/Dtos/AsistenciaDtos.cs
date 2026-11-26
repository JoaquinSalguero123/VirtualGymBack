namespace VirtualGymAssistant.Application.Features.Asistencias.Dtos;

/// <summary>Un check-in concreto de un socio.</summary>
public class AsistenciaDto
{
    public int Id { get; init; }
    public int IdSocio { get; init; }
    public string Socio { get; init; } = string.Empty;
    public int IdGimnasio { get; init; }
    public string Sucursal { get; init; } = string.Empty;

    public DateOnly Fecha { get; init; }
    public int Dia { get; init; }
    public DateTime FechaHoraEntrada { get; init; }
    public DateTime? FechaHoraSalida { get; init; }

    /// <summary>
    /// Minutos que estuvo en el gimnasio. Es null mientras no registre la salida.
    /// Se calcula acá y no en la consulta para que la capa Application no dependa de
    /// funciones propias de SQL Server.
    /// </summary>
    public int? MinutosEnElGimnasio => FechaHoraSalida is null
        ? null
        : (int)(FechaHoraSalida.Value - FechaHoraEntrada).TotalMinutes;
}

/// <summary>Fila de la grilla mensual de asistencias de la sucursal.</summary>
public class FilaDeAsistenciasDto
{
    public int IdSocio { get; init; }
    public string Socio { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public int? NumeroSocio { get; init; }

    // marcador
    /// <summary>Días del mes en los que asistió, ordenados de menor a mayor.</summary>
    public IReadOnlyList<int> DiasAsistidos { get; init; } = [];

    public int Total { get; init; }
}

/// <summary>Resumen rápido para el panel de la sucursal.</summary>
public class ResumenDeAsistenciasDto
{
    public DateOnly Fecha { get; init; }

    /// <summary>Check-ins registrados hoy.</summary>
    public int AsistenciasDeHoy { get; init; }

    /// <summary>Socios distintos que vinieron hoy.</summary>
    public int SociosDistintosHoy { get; init; }

    /// <summary>Socios que entraron y todavía no registraron salida.</summary>
    public int SociosEnElGimnasio { get; init; }

    public int AsistenciasDelMes { get; init; }

    /// <summary>Promedio de días asistidos por socio en el mes en curso.</summary>
    public double PromedioPorSocioEnElMes { get; init; }
}

namespace VirtualGymAssistant.Application.Features.Rutinas.Dtos;

/// <summary>Un ejercicio dentro de un día de rutina.</summary>
public class EjercicioDeRutinaDto
{
    public int Orden { get; init; }
    public int IdEjercicio { get; init; }
    public string Ejercicio { get; init; } = string.Empty;
    public string Grupo { get; init; } = string.Empty;
    public int? Series { get; init; }
    public string? Repeticiones { get; init; }
    public int? DescansoSeg { get; init; }
    public string? Notas { get; init; }
    public string? VideoUrl { get; init; }
}

/// <summary>Un día de entrenamiento con sus ejercicios en orden.</summary>
public class DiaDeRutinaDto
{
    public int Dia { get; init; }
    public IReadOnlyList<EjercicioDeRutinaDto> Ejercicios { get; init; } = [];
}

/// <summary>Plantilla de rutina reutilizable.</summary>
public class PlantillaDeRutinaDto
{
    public int Id { get; init; }
    public int? IdOrganizacion { get; init; }

    /// <summary>True si la plantilla es del sistema y la comparten todas las organizaciones.</summary>
    public bool EsGlobal { get; init; }

    public string Nombre { get; init; } = string.Empty;
    public string? Descripcion { get; init; }
    public string? Intensidad { get; init; }
    public int? IdObjetivo { get; init; }
    public string? Objetivo { get; init; }
    public int? DiasPorSemana { get; init; }
    public int? EjerciciosPorDia { get; init; }
    public bool Activa { get; init; }

    /// <summary>Detalle día por día. Viene vacío en los listados y completo en el detalle.</summary>
    public IReadOnlyList<DiaDeRutinaDto> Dias { get; init; } = [];
}

/// <summary>Rutina concreta asignada a un socio.</summary>
public class RutinaDeSocioDto
{
    public int Id { get; init; }
    public int IdSocio { get; init; }
    public string Socio { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;

    public int? IdPlantilla { get; init; }
    public string? Plantilla { get; init; }

    public int? IdEntrenador { get; init; }
    public string? Entrenador { get; init; }

    public DateOnly FechaInicio { get; init; }
    public DateOnly? FechaFin { get; init; }
    public bool Activa { get; init; }

    public IReadOnlyList<DiaDeRutinaDto> Dias { get; init; } = [];
}

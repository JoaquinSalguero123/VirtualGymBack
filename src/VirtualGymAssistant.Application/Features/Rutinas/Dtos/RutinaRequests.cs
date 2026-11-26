namespace VirtualGymAssistant.Application.Features.Rutinas.Dtos;

/// <summary>Un ejercicio que se quiere incluir en un día de rutina.</summary>
public class EjercicioDeRutinaRequest
{
    public int IdEjercicio { get; set; }
    public int? Series { get; set; }
    public string? Repeticiones { get; set; }
    public int? DescansoSeg { get; set; }
    public string? Notas { get; set; }
}

/// <summary>Un día con la lista ordenada de ejercicios.</summary>
public class DiaDeRutinaRequest
{
    /// <summary>Número de día dentro de la rutina, empezando en 1.</summary>
    public int Dia { get; set; }

    public List<EjercicioDeRutinaRequest> Ejercicios { get; set; } = [];
}

/// <summary>Alta o modificación de una plantilla de rutina.</summary>
public class GuardarPlantillaRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Intensidad { get; set; }
    public int? IdObjetivo { get; set; }
    public int? DiasPorSemana { get; set; }
    public int? EjerciciosPorDia { get; set; }

    /// <summary>Detalle completo. Si viene vacío, la plantilla queda sin ejercicios cargados.</summary>
    public List<DiaDeRutinaRequest> Dias { get; set; } = [];
}

/// <summary>
/// Pedido de generación automática de una rutina.
/// Se puede partir de una plantilla o dejar que el sistema arme una según el objetivo.
/// </summary>
public class GenerarRutinaRequest
{
    /// <summary>Plantilla de la que copiar el detalle. Tiene prioridad sobre el objetivo.</summary>
    public int? IdPlantilla { get; set; }

    /// <summary>Objetivo para elegir la plantilla más adecuada o armar la rutina desde cero.</summary>
    public int? IdObjetivo { get; set; }

    public int DiasPorSemana { get; set; } = 3;
    public int EjerciciosPorDia { get; set; } = 5;
}

/// <summary>Guardado de la rutina del socio (generada o armada a mano).</summary>
public class GuardarRutinaDeSocioRequest
{
    public string? Nombre { get; set; }

    /// <summary>Plantilla de la que salió, si corresponde. Queda registrado como origen.</summary>
    public int? IdPlantilla { get; set; }

    public List<DiaDeRutinaRequest> Dias { get; set; } = [];
}

/// <summary>Asignación de una rutina a un socio por parte del entrenador.</summary>
public class AsignarRutinaRequest : GuardarRutinaDeSocioRequest
{
    public int IdSocio { get; set; }
}

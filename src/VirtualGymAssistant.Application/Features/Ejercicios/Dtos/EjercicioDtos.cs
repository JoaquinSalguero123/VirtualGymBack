namespace VirtualGymAssistant.Application.Features.Ejercicios.Dtos;

/// <summary>Ejercicio del catálogo con su grupo muscular.</summary>
public class EjercicioDto
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? Descripcion { get; init; }
    public int IdGrupo { get; init; }
    public string Grupo { get; init; } = string.Empty;
    public string? VideoUrl { get; init; }

    /// <summary>Veces que aparece en plantillas o rutinas asignadas.</summary>
    public int Usos { get; init; }
}

/// <summary>Alta o modificación de un ejercicio.</summary>
public class GuardarEjercicioRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int IdGrupo { get; set; }
    public string? VideoUrl { get; set; }
}

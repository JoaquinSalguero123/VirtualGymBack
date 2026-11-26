namespace VirtualGymAssistant.Application.Features.Foro.Dtos;

/// <summary>Publicación del foro con sus respuestas.</summary>
public class PublicacionDto
{
    public int Id { get; init; }
    public int IdUsuario { get; init; }
    public string Usuario { get; init; } = string.Empty;

    /// <summary>Rol del autor en la sucursal, para poder destacar al personal.</summary>
    public string? RolDelAutor { get; init; }

    public int? IdGimnasio { get; init; }
    public string? Sucursal { get; init; }
    public int? IdPostPadre { get; init; }

    public string? Contenido { get; init; }
    public string? Archivo { get; init; }
    public DateTime Fecha { get; init; }

    /// <summary>True si la publicación es del usuario que está consultando.</summary>
    public bool EsMia { get; init; }

    /// <summary>True si el usuario puede borrarla (es el autor o es personal).</summary>
    public bool PuedeEliminar { get; init; }

    public IReadOnlyList<PublicacionDto> Respuestas { get; init; } = [];
}

/// <summary>Alta de una publicación o de una respuesta.</summary>
public class CrearPublicacionRequest
{
    public string? Contenido { get; set; }

    /// <summary>Publicación a la que responde. Si es null, es una publicación nueva.</summary>
    public int? IdPostPadre { get; set; }
}

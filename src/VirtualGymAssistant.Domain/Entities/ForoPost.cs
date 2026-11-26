namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Publicación del foro. Si <see cref="IdGimnasio"/> es nulo, el post es de toda la
/// organización; si tiene valor, sólo lo ve esa sucursal.
/// <see cref="IdPostPadre"/> permite responder a otra publicación.
/// Tabla: dbo.ForoPosts
/// </summary>
public class ForoPost
{
    public int Id { get; set; }
    public int IdOrganizacion { get; set; }
    public int? IdGimnasio { get; set; }
    public int IdUsuario { get; set; }
    public int? IdPostPadre { get; set; }
    public string? Contenido { get; set; }

    /// <summary>Nombre del archivo adjunto subido junto al post.</summary>
    public string? Archivo { get; set; }

    public DateTime Fecha { get; set; }

    // --- Relaciones ---
    public Organizacion Organizacion { get; set; } = null!;
    public Gimnasio? Gimnasio { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public ForoPost? PostPadre { get; set; }
    public ICollection<ForoPost> Respuestas { get; set; } = new List<ForoPost>();
}

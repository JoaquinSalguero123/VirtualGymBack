namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Ficha de un usuario como miembro de una sucursal. Un mismo usuario puede tener
/// una ficha por sucursal; los pagos, asistencias, rutinas y dietas cuelgan de ella.
/// Tabla: dbo.Socios
/// </summary>
public class Socio
{
    public int Id { get; set; }
    public int IdUsuario { get; set; }

    /// <summary>Sucursal de origen del socio.</summary>
    public int IdGimnasio { get; set; }

    /// <summary>Número correlativo dentro de la sucursal (único cuando no es nulo).</summary>
    public int? NumeroSocio { get; set; }

    public int IdEstado { get; set; }
    public int? IdObjetivo { get; set; }
    public DateOnly? FechaNacimiento { get; set; }
    public DateOnly FechaAlta { get; set; }
    public DateOnly? FechaBaja { get; set; }
    public string? Observaciones { get; set; }

    // --- Relaciones ---
    public Usuario Usuario { get; set; } = null!;
    public Gimnasio Gimnasio { get; set; } = null!;
    public SocioEstado Estado { get; set; } = null!;
    public Objetivo? Objetivo { get; set; }
    public ICollection<Membresia> Membresias { get; set; } = new List<Membresia>();
    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
    public ICollection<Asistencia> Asistencias { get; set; } = new List<Asistencia>();
    public ICollection<SocioRutina> Rutinas { get; set; } = new List<SocioRutina>();
    public ICollection<SocioDieta> Dietas { get; set; } = new List<SocioDieta>();
    public ICollection<Medicion> Mediciones { get; set; } = new List<Medicion>();
    public ICollection<SocioAccesoGimnasio> AccesosAdicionales { get; set; } = new List<SocioAccesoGimnasio>();
}

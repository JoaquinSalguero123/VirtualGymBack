namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Sucursal concreta de una organización. Es la unidad operativa del sistema:
/// socios, personal, asistencias, pagos y horarios pertenecen siempre a una sucursal.
/// Tabla: dbo.Gimnasios
/// </summary>
public class Gimnasio
{
    public int Id { get; set; }
    public int IdOrganizacion { get; set; }

    /// <summary>Nombre de la sucursal. Único dentro de la organización.</summary>
    public string Nombre { get; set; } = string.Empty;

    public string Direccion { get; set; } = string.Empty;

    /// <summary>
    /// Ubicación en el mapa del sitio público, en grados decimales (WGS84). Son opcionales:
    /// una sucursal sin coordenadas se muestra igual en las listas, sólo que sin pin. Van las
    /// dos o ninguna (lo garantiza también un CHECK en la base).
    /// </summary>
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }

    public int? IdZona { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }

    /// <summary>Marca la sede principal. Se usa como sucursal por defecto en registros y listados.</summary>
    public bool EsCasaMatriz { get; set; }

    /// <summary>Baja lógica: la sucursal deja de operar pero conserva su historial.</summary>
    public bool Activo { get; set; } = true;

    public DateTime FechaAlta { get; set; }

    // --- Relaciones ---
    public Organizacion Organizacion { get; set; } = null!;
    public Zona? Zona { get; set; }
    public ICollection<UsuarioGimnasioRol> Personal { get; set; } = new List<UsuarioGimnasioRol>();
    public ICollection<Socio> Socios { get; set; } = new List<Socio>();
    public ICollection<Membresia> Membresias { get; set; } = new List<Membresia>();
    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
    public ICollection<Asistencia> Asistencias { get; set; } = new List<Asistencia>();
    public ICollection<Horario> Horarios { get; set; } = new List<Horario>();
    public ICollection<GimnasioMaquina> Maquinas { get; set; } = new List<GimnasioMaquina>();
}

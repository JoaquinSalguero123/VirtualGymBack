namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Asignación de un rol a un usuario EN UNA SUCURSAL concreta.
/// Es el corazón del modelo de permisos: la misma persona puede ser Entrenador en una
/// sede y Recepcionista en otra. Se da de baja en forma lógica (Activo = false).
/// Tabla: dbo.UsuarioGimnasioRol
/// </summary>
public class UsuarioGimnasioRol
{
    public int Id { get; set; }
    public int IdUsuario { get; set; }
    public int IdGimnasio { get; set; }
    public int IdRol { get; set; }

    public DateOnly FechaDesde { get; set; }
    public DateOnly? FechaHasta { get; set; }
    public bool Activo { get; set; } = true;

    // --- Relaciones ---
    public Usuario Usuario { get; set; } = null!;
    public Gimnasio Gimnasio { get; set; } = null!;
    public Rol Rol { get; set; } = null!;
}

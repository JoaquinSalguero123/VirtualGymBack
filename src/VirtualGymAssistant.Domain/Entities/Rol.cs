namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Rol que un usuario puede tener DENTRO de una sucursal (Socio, Entrenador, etc.).
/// Los nombres válidos están centralizados en <see cref="Constants.RolesDelSistema"/>.
/// Tabla: dbo.Roles
/// </summary>
public class Rol
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public ICollection<UsuarioGimnasioRol> Asignaciones { get; set; } = new List<UsuarioGimnasioRol>();
}

namespace VirtualGymAssistant.Domain.Entities;

/// <summary>
/// Identidad única del sistema: dueños, personal y socios comparten esta tabla.
/// Lo que diferencia a una persona de otra NO es esta entidad, sino el rol que tiene
/// asignado en cada sucursal (ver <see cref="UsuarioGimnasioRol"/>).
/// Tabla: dbo.Usuarios
/// </summary>
public class Usuario
{
    public int Id { get; set; }

    /// <summary>Nombre de usuario único. Se genera a partir del mail si no se indica.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Email de login. Único en todo el sistema.</summary>
    public string Mail { get; set; } = string.Empty;

    public string? Nombre { get; set; }
    public string? Apellido { get; set; }

    /// <summary>Teléfono. Único cuando no es nulo (índice filtrado en la base).</summary>
    public string? Telefono { get; set; }

    /// <summary>Hash BCrypt de la contraseña. Nunca se guarda ni se expone en texto plano.</summary>
    public string Clave { get; set; } = string.Empty;

    /// <summary>Baja lógica: un usuario inactivo no puede iniciar sesión.</summary>
    public bool Activo { get; set; } = true;

    /// <summary>
    /// True si confirmó el código que se le mandó por mail. Mientras sea false la cuenta no
    /// tiene acceso: el registro no emite sesión y el login la rechaza hasta que lo confirme.
    /// </summary>
    public bool EmailVerificado { get; set; }

    public DateTime FechaAlta { get; set; }

    // --- Relaciones ---
    public ICollection<Organizacion> OrganizacionesQuePosee { get; set; } = new List<Organizacion>();
    public ICollection<UsuarioGimnasioRol> RolesPorSucursal { get; set; } = new List<UsuarioGimnasioRol>();
    public ICollection<Socio> FichasDeSocio { get; set; } = new List<Socio>();
    public ICollection<TokenRecuperacion> TokensDeRecuperacion { get; set; } = new List<TokenRecuperacion>();
    public ICollection<TokenVerificacionEmail> TokensDeVerificacion { get; set; } = new List<TokenVerificacionEmail>();

    /// <summary>Nombre y apellido listos para mostrar; cae al username si faltan los datos.</summary>
    public string NombreCompleto =>
        string.IsNullOrWhiteSpace(Nombre) && string.IsNullOrWhiteSpace(Apellido)
            ? Username
            : $"{Nombre} {Apellido}".Trim();
}

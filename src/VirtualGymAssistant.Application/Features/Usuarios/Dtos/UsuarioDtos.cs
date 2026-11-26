namespace VirtualGymAssistant.Application.Features.Usuarios.Dtos;

/// <summary>Usuario del personal con el rol que tiene en la sucursal consultada.</summary>
public class UsuarioDto
{
    public int Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string? Nombre { get; init; }
    public string? Apellido { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? Telefono { get; init; }
    public bool Activo { get; init; }
    public DateTime FechaAlta { get; init; }

    /// <summary>Rol en la sucursal desde la que se hizo la consulta.</summary>
    public int? IdRol { get; init; }
    public string? Rol { get; init; }
}

/// <summary>Sucursal y rol de un usuario, para la pantalla de asignaciones múltiples.</summary>
public class AsignacionDeRolDto
{
    public int IdAsignacion { get; init; }
    public int IdGimnasio { get; init; }
    public string Sucursal { get; init; } = string.Empty;
    public int IdRol { get; init; }
    public string Rol { get; init; } = string.Empty;
    public DateOnly FechaDesde { get; init; }
    public DateOnly? FechaHasta { get; init; }
    public bool Activo { get; init; }
}

/// <summary>Alta de personal en la sucursal activa.</summary>
public class CrearUsuarioRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }

    /// <summary>Si no se indica, se genera a partir del email.</summary>
    public string? Username { get; set; }

    public string Contrasena { get; set; } = string.Empty;

    /// <summary>Rol que tendrá en la sucursal activa.</summary>
    public int IdRol { get; set; }
}

/// <summary>Edición de los datos de un usuario y, opcionalmente, de su rol en la sucursal.</summary>
public class ActualizarUsuarioRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Username { get; set; }
    public bool Activo { get; set; } = true;

    /// <summary>Nuevo rol en la sucursal activa. Si es null, el rol queda como está.</summary>
    public int? IdRol { get; set; }
}

/// <summary>Asignación de un rol a un usuario en una sucursal concreta.</summary>
public class AsignarRolRequest
{
    public int IdGimnasio { get; set; }
    public int IdRol { get; set; }
}

/// <summary>Rol del catálogo.</summary>
public class RolDto
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? Descripcion { get; init; }
}

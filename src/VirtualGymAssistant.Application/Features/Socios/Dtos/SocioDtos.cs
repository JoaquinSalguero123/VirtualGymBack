namespace VirtualGymAssistant.Application.Features.Socios.Dtos;

/// <summary>Fila del padrón de socios de una sucursal.</summary>
public class SocioDto
{
    public int Id { get; init; }
    public int IdUsuario { get; init; }
    public int IdGimnasio { get; init; }
    public string Sucursal { get; init; } = string.Empty;
    public int? NumeroSocio { get; init; }

    public string Username { get; init; } = string.Empty;
    public string NombreCompleto { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Telefono { get; init; }

    public int IdEstado { get; init; }
    public string Estado { get; init; } = string.Empty;
    public int? IdObjetivo { get; init; }
    public string? Objetivo { get; init; }

    public DateOnly? FechaNacimiento { get; init; }
    public DateOnly FechaAlta { get; init; }
    public DateOnly? FechaBaja { get; init; }
    public string? Observaciones { get; init; }

    /// <summary>Plan de la membresía activa, si tiene una.</summary>
    public string? PlanActivo { get; init; }
    public decimal? PrecioPlanActivo { get; init; }

    /// <summary>True si la cuota del mes en curso figura como pagada.</summary>
    public bool CuotaAlDia { get; init; }
}

/// <summary>Filtros del padrón de socios.</summary>
public class FiltroDeSocios
{
    /// <summary>Busca por nombre, apellido, email, username o número de socio.</summary>
    public string? Texto { get; set; }

    /// <summary>Filtra por estado (Activo, Moroso, Suspendido, Baja).</summary>
    public int? IdEstado { get; set; }

    /// <summary>Si es true, deja fuera a los socios dados de baja.</summary>
    public bool SoloActivos { get; set; }
}

/// <summary>Modificación de la ficha de un socio por parte del personal.</summary>
public class ActualizarSocioRequest
{
    public int IdEstado { get; set; }
    public int? IdObjetivo { get; set; }
    public DateOnly? FechaNacimiento { get; set; }
    public string? Observaciones { get; set; }
}

/// <summary>Estado del catálogo de estados de socio.</summary>
public class EstadoDeSocioDto
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
}

/// <summary>Caso de uso "Sumarse a otra sucursal": a qué sucursal se quiere sumar.</summary>
public class UnirseASucursalRequest
{
    public int IdGimnasio { get; set; }
}

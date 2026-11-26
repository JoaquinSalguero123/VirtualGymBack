namespace VirtualGymAssistant.Application.Features.Sucursales.Dtos;

/// <summary>Sucursal con sus datos de gestión.</summary>
public class SucursalDto
{
    public int Id { get; init; }
    public int IdOrganizacion { get; init; }
    public string Organizacion { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string Direccion { get; init; } = string.Empty;
    public decimal? Latitud { get; init; }
    public decimal? Longitud { get; init; }
    public int? IdZona { get; init; }
    public string? Zona { get; init; }
    public string? Telefono { get; init; }
    public string? Email { get; init; }
    public bool EsCasaMatriz { get; init; }
    public bool Activo { get; init; }
    public DateTime FechaAlta { get; init; }

    public int CantidadSocios { get; init; }
    public int CantidadPersonal { get; init; }
}

/// <summary>
/// Vista mínima de una sucursal para el sitio público (la usa el formulario de registro,
/// donde todavía no hay usuario autenticado).
/// </summary>
public class SucursalPublicaDto
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Organizacion { get; init; } = string.Empty;
    public string Direccion { get; init; } = string.Empty;

    /// <summary>
    /// Ubicación para el mapa del sitio público. Es null si la sucursal todavía no la tiene
    /// cargada: en ese caso aparece en la lista pero sin pin.
    /// </summary>
    public decimal? Latitud { get; init; }
    public decimal? Longitud { get; init; }

    public bool EsCasaMatriz { get; init; }
}

/// <summary>Alta de una sucursal dentro de una organización propia.</summary>
public class CrearSucursalRequest
{
    /// <summary>
    /// Organización donde se crea la sucursal. Si no se indica se usa la organización
    /// de la sucursal activa del usuario.
    /// </summary>
    public int? IdOrganizacion { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;

    /// <summary>Ubicación en el mapa. Van las dos o ninguna; sin ellas la sucursal no tiene pin.</summary>
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }

    public int? IdZona { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }

    /// <summary>Si se marca, esta sucursal pasa a ser la casa matriz y la anterior deja de serlo.</summary>
    public bool EsCasaMatriz { get; set; }
}

/// <summary>Modificación de los datos de una sucursal.</summary>
public class ActualizarSucursalRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;

    /// <summary>Ubicación en el mapa. Van las dos o ninguna; sin ellas la sucursal no tiene pin.</summary>
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }

    public int? IdZona { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
}

/// <summary>Zona geográfica del catálogo.</summary>
public class ZonaDto
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? Localidad { get; init; }
    public string? Provincia { get; init; }
}

namespace VirtualGymAssistant.Application.Features.Organizaciones.Dtos;

/// <summary>Organización tal como se muestra en el panel del dueño.</summary>
public class OrganizacionDto
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? RazonSocial { get; init; }
    public string? Cuit { get; init; }
    public bool Activo { get; init; }
    public DateTime FechaAlta { get; init; }

    public int IdDuenio { get; init; }
    public string Duenio { get; init; } = string.Empty;

    /// <summary>Cantidad de sucursales que cuelgan de la organización.</summary>
    public int CantidadSucursales { get; init; }

    /// <summary>Cantidad total de socios sumando todas las sucursales.</summary>
    public int CantidadSocios { get; init; }
}

/// <summary>Alta de una organización. Quien la crea queda como titular.</summary>
public class CrearOrganizacionRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? RazonSocial { get; set; }
    public string? Cuit { get; set; }
}

/// <summary>Modificación de los datos de la organización.</summary>
public class ActualizarOrganizacionRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? RazonSocial { get; set; }
    public string? Cuit { get; set; }
    public bool Activo { get; set; } = true;
}

namespace VirtualGymAssistant.Application.Features.SitioWeb.Dtos;

/// <summary>Item del carrusel del sitio público.</summary>
public class CarouselItemDto
{
    public int Id { get; init; }
    public int IdOrganizacion { get; init; }
    public int? IdGimnasio { get; init; }
    public string? Sucursal { get; init; }
    public string? Tipo { get; init; }
    public string? Titulo { get; init; }
    public string? Descripcion { get; init; }

    /// <summary>Nombre del archivo. El front lo resuelve contra la ruta pública de uploads.</summary>
    public string? Imagen { get; init; }

    public int? Orden { get; init; }
    public bool Activo { get; init; }
}

/// <summary>Alta o modificación de un item del carrusel.</summary>
public class GuardarCarouselItemRequest
{
    /// <summary>Agrupador dentro del sitio: "servicios", "frases", etc.</summary>
    public string? Tipo { get; set; }

    public string? Titulo { get; set; }
    public string? Descripcion { get; set; }
    public int? Orden { get; set; }

    /// <summary>Si se indica, el item se muestra sólo para esa sucursal.</summary>
    public int? IdGimnasio { get; set; }

    public bool Activo { get; set; } = true;
}

/// <summary>Dato configurable del pie del sitio.</summary>
public class FooterConfigDto
{
    public int Id { get; init; }
    public int IdOrganizacion { get; init; }
    public string Seccion { get; init; } = string.Empty;
    public string Clave { get; init; } = string.Empty;
    public string? Valor { get; init; }
    public int? Orden { get; init; }
}

/// <summary>Alta o modificación de un dato del pie.</summary>
public class GuardarFooterConfigRequest
{
    public string Seccion { get; set; } = string.Empty;
    public string Clave { get; set; } = string.Empty;
    public string? Valor { get; set; }
    public int? Orden { get; set; }
}

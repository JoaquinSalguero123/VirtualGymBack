namespace VirtualGymAssistant.Application.Features.Planes.Dtos;

/// <summary>Plan comercial de la organización.</summary>
public class PlanDto
{
    public int Id { get; init; }
    public int IdOrganizacion { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? Descripcion { get; init; }
    public decimal Precio { get; init; }
    public int DuracionDias { get; init; }
    public string? Nivel { get; init; }
    public bool AccesoMultiSede { get; init; }
    public bool Activo { get; init; }

    /// <summary>Socios que hoy tienen una membresía activa con este plan.</summary>
    public int SociosConEstePlan { get; init; }
}

/// <summary>Alta de un plan.</summary>
public class CrearPlanRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public int DuracionDias { get; set; } = 30;
    public string? Nivel { get; set; }
    public bool AccesoMultiSede { get; set; }
}

/// <summary>Modificación de un plan.</summary>
public class ActualizarPlanRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public int DuracionDias { get; set; } = 30;
    public string? Nivel { get; set; }
    public bool AccesoMultiSede { get; set; }
    public bool Activo { get; set; } = true;
}

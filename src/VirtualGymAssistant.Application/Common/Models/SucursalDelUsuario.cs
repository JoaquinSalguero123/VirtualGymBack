namespace VirtualGymAssistant.Application.Common.Models;

/// <summary>
/// Una sucursal en la que el usuario puede operar, junto con el rol más alto que tiene
/// en ella. Es lo que alimenta el selector de sucursal del front.
/// </summary>
public class SucursalDelUsuario
{
    public int IdGimnasio { get; init; }
    public string Gimnasio { get; init; } = string.Empty;
    public int IdOrganizacion { get; init; }
    public string Organizacion { get; init; } = string.Empty;
    public bool EsCasaMatriz { get; init; }

    /// <summary>Rol con el que el usuario opera en esta sucursal.</summary>
    public string Rol { get; init; } = string.Empty;
}

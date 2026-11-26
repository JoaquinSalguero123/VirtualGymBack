using VirtualGymAssistant.Domain.Constants;
using VirtualGymAssistant.Domain.Exceptions;

namespace VirtualGymAssistant.Application.Common.Models;

/// <summary>
/// Fotografía de "quién es y desde dónde opera" el usuario en la petición actual.
///
/// Es la pieza central del modelo multi-sucursal: como una misma persona puede tener
/// distinto rol en distintas sedes, no alcanza con saber quién es; hay que saber
/// también sobre qué sucursal está trabajando.
/// </summary>
public class ContextoDeUsuario
{
    public required int IdUsuario { get; init; }

    /// <summary>Todas las sucursales en las que el usuario puede operar.</summary>
    public required IReadOnlyList<SucursalDelUsuario> Sucursales { get; init; }

    /// <summary>True si el usuario es titular de al menos una organización.</summary>
    public required bool EsDuenio { get; init; }

    /// <summary>
    /// Sucursal sobre la que se está trabajando: la pedida por cabecera, o la primera
    /// disponible. Es null si el usuario todavía no está vinculado a ninguna sucursal.
    /// </summary>
    public SucursalDelUsuario? SucursalActiva { get; init; }

    /// <summary>Rol del usuario en la sucursal activa.</summary>
    public string? RolActivo => SucursalActiva?.Rol;

    public int? IdGimnasioActivo => SucursalActiva?.IdGimnasio;
    public int? IdOrganizacionActiva => SucursalActiva?.IdOrganizacion;

    /// <summary>
    /// Id de la sucursal activa, exigiendo que exista.
    /// Se usa en toda operación que sin una sede concreta no tiene sentido (cobrar,
    /// marcar asistencia, listar el padrón).
    /// </summary>
    public int GimnasioActivoRequerido => IdGimnasioActivo
        ?? throw new ReglaDeNegocioException(
            "No hay una sucursal seleccionada. Elegí una sucursal para continuar.");

    /// <summary>Id de la organización activa, exigiendo que exista.</summary>
    public int OrganizacionActivaRequerida => IdOrganizacionActiva
        ?? throw new ReglaDeNegocioException(
            "No hay una sucursal seleccionada, por lo que no se puede determinar la organización.");

    /// <summary>
    /// True si el rol activo es uno de los indicados.
    /// El dueño siempre pasa: es el titular y puede hacer todo dentro de sus sucursales.
    /// </summary>
    public bool TieneRol(params string[] rolesPermitidos) =>
        EsDuenio || (RolActivo is not null &&
                     rolesPermitidos.Contains(RolActivo, StringComparer.OrdinalIgnoreCase));

    /// <summary>True si el rol activo tiene al menos el nivel jerárquico indicado.</summary>
    public bool AlcanzaNivel(string rolMinimo) =>
        EsDuenio || RolesDelSistema.AlcanzaNivel(RolActivo, rolMinimo);

    /// <summary>True si el usuario forma parte del personal (no es solamente socio).</summary>
    public bool EsStaff => EsDuenio || RolesDelSistema.AlcanzaNivel(RolActivo, RolesDelSistema.Nutricionista);

    /// <summary>Corta la operación con 403 si el rol activo no está entre los permitidos.</summary>
    public void ExigirRol(params string[] rolesPermitidos)
    {
        if (TieneRol(rolesPermitidos)) return;

        throw new AccesoDenegadoException(
            $"La acción requiere alguno de estos roles: {string.Join(", ", rolesPermitidos)}. " +
            $"Tu rol en la sucursal activa es: {RolActivo ?? "sin rol"}.");
    }

    /// <summary>True si el usuario puede operar sobre la sucursal indicada.</summary>
    public bool PuedeOperarEn(int idGimnasio) =>
        Sucursales.Any(s => s.IdGimnasio == idGimnasio);

    /// <summary>Corta la operación con 403 si el usuario no tiene acceso a esa sucursal.</summary>
    public void ExigirAccesoA(int idGimnasio)
    {
        if (!PuedeOperarEn(idGimnasio))
            throw new AccesoDenegadoException("No tenés acceso a la sucursal indicada.");
    }
}

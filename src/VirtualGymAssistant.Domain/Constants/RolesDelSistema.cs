namespace VirtualGymAssistant.Domain.Constants;

/// <summary>
/// Nombres de los roles tal como están cargados en dbo.Roles, y la jerarquía entre ellos.
/// Se centralizan acá para no repetir cadenas sueltas por todo el código.
/// </summary>
public static class RolesDelSistema
{
    public const string Duenio = "Dueño";
    public const string Administrador = "Administrador";
    public const string Recepcionista = "Recepcionista";
    public const string Entrenador = "Entrenador";
    public const string Nutricionista = "Nutricionista";
    public const string Socio = "Socio";

    /// <summary>
    /// Nivel de cada rol: a mayor número, más permisos.
    /// Entrenador y Nutricionista comparten nivel porque son roles técnicos paralelos:
    /// ninguno manda sobre el otro, cada uno administra su propio catálogo.
    /// </summary>
    private static readonly Dictionary<string, int> Jerarquia = new(StringComparer.OrdinalIgnoreCase)
    {
        [Duenio] = 5,
        [Administrador] = 4,
        [Recepcionista] = 3,
        [Entrenador] = 2,
        [Nutricionista] = 2,
        [Socio] = 1,
    };

    /// <summary>Roles que forman parte del personal del gimnasio (todo lo que no es Socio).</summary>
    public static readonly string[] Staff =
        [Duenio, Administrador, Recepcionista, Entrenador, Nutricionista];

    /// <summary>Devuelve el nivel del rol; 0 si el nombre no corresponde a un rol conocido.</summary>
    public static int Nivel(string? rol) =>
        rol is not null && Jerarquia.TryGetValue(rol, out var nivel) ? nivel : 0;

    /// <summary>True si <paramref name="rol"/> tiene al menos el nivel de <paramref name="rolMinimo"/>.</summary>
    public static bool AlcanzaNivel(string? rol, string rolMinimo) =>
        Nivel(rol) >= Nivel(rolMinimo);
}

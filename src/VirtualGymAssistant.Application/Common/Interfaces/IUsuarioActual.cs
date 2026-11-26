namespace VirtualGymAssistant.Application.Common.Interfaces;

/// <summary>
/// Datos del usuario que está haciendo la petición actual.
/// Lo implementa la capa API leyendo el token JWT y las cabeceras HTTP; la capa
/// Application lo consume sin saber que existe HTTP de por medio.
/// </summary>
public interface IUsuarioActual
{
    /// <summary>Id del usuario autenticado, o null si la petición es anónima.</summary>
    int? Id { get; }

    string? Mail { get; }
    string? Username { get; }

    bool EstaAutenticado { get; }

    /// <summary>
    /// Sucursal sobre la que el usuario quiere trabajar. Llega en la cabecera
    /// <c>X-Gimnasio-Id</c> o en el query string <c>?gimnasio=</c>.
    /// Si no se indica, el sistema elige la primera sucursal disponible del usuario.
    /// </summary>
    int? IdGimnasioSolicitado { get; }

    /// <summary>Id del usuario autenticado; lanza si la petición es anónima.</summary>
    int IdRequerido { get; }
}

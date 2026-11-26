using VirtualGymAssistant.Application.Common.Models;

namespace VirtualGymAssistant.Application.Common.Interfaces;

/// <summary>
/// Resuelve en qué sucursales puede operar el usuario autenticado y con qué rol en cada una.
/// </summary>
public interface IServicioDeContexto
{
    /// <summary>
    /// Devuelve el contexto del usuario de la petición actual.
    /// El resultado se cachea mientras dura la petición, así varias llamadas no
    /// repiten la consulta a la base.
    /// </summary>
    Task<ContextoDeUsuario> ObtenerAsync(CancellationToken ct = default);

    /// <summary>Lista las sucursales de un usuario puntual, con el rol más alto en cada una.</summary>
    Task<IReadOnlyList<SucursalDelUsuario>> ObtenerSucursalesAsync(int idUsuario, CancellationToken ct = default);
}

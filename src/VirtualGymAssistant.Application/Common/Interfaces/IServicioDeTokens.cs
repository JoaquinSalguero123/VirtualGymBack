using VirtualGymAssistant.Domain.Entities;

namespace VirtualGymAssistant.Application.Common.Interfaces;

/// <summary>Genera los tokens JWT con los que el front autentica cada petición.</summary>
public interface IServicioDeTokens
{
    /// <summary>Firma un token para el usuario indicado.</summary>
    /// <returns>El token serializado y el momento exacto en que expira.</returns>
    (string Token, DateTime Expira) GenerarToken(Usuario usuario);
}

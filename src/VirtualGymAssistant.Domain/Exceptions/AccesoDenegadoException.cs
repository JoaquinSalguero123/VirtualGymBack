namespace VirtualGymAssistant.Domain.Exceptions;

/// <summary>
/// El usuario está autenticado pero no tiene permiso sobre el recurso pedido.
/// Devuelve 403.
/// </summary>
public class AccesoDenegadoException : ExcepcionDeDominio
{
    public AccesoDenegadoException(string mensaje) : base(mensaje) { }
}

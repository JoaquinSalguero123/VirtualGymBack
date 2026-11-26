namespace VirtualGymAssistant.Domain.Exceptions;

/// <summary>
/// Falta el token o las credenciales son incorrectas. Devuelve 401.
/// </summary>
public class NoAutenticadoException : ExcepcionDeDominio
{
    public NoAutenticadoException(string mensaje) : base(mensaje) { }
}

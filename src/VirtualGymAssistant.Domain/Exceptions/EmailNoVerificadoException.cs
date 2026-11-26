namespace VirtualGymAssistant.Domain.Exceptions;

/// <summary>
/// El email y la contraseña son correctos, pero el usuario todavía no confirmó el
/// email. Devuelve 403 con un código distinguible (ver ManejadorGlobalDeExcepciones)
/// para que el front ofrezca reenviar/confirmar el código en vez de un error genérico.
/// </summary>
public class EmailNoVerificadoException : ExcepcionDeDominio
{
    public EmailNoVerificadoException(string mensaje) : base(mensaje) { }
}

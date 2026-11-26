namespace VirtualGymAssistant.Domain.Exceptions;

/// <summary>
/// Excepción base de todos los errores esperables del negocio.
/// El middleware de la API las traduce a un código HTTP; cualquier otra excepción
/// se considera un fallo no controlado y devuelve 500.
/// </summary>
public abstract class ExcepcionDeDominio : Exception
{
    protected ExcepcionDeDominio(string mensaje) : base(mensaje) { }
}

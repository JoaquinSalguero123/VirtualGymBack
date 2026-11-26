namespace VirtualGymAssistant.Domain.Exceptions;

/// <summary>
/// El recurso choca con otro ya existente (mail repetido, sucursal con nombre duplicado).
/// Devuelve 409.
/// </summary>
public class ConflictoException : ExcepcionDeDominio
{
    public ConflictoException(string mensaje) : base(mensaje) { }
}

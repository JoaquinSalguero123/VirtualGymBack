namespace VirtualGymAssistant.Domain.Exceptions;

/// <summary>
/// La operación es válida en cuanto a formato pero rompe una regla del negocio
/// (por ejemplo, marcar asistencia dos veces el mismo día). Devuelve 400.
/// </summary>
public class ReglaDeNegocioException : ExcepcionDeDominio
{
    public ReglaDeNegocioException(string mensaje) : base(mensaje) { }
}

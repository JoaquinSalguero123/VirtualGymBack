namespace VirtualGymAssistant.Domain.Exceptions;

/// <summary>
/// Los datos recibidos no pasan las validaciones de entrada.
/// Agrupa los errores por campo para que el front pueda marcarlos en el formulario.
/// Devuelve 400 con el detalle por campo.
/// </summary>
public class ValidacionException : ExcepcionDeDominio
{
    public IReadOnlyDictionary<string, string[]> Errores { get; }

    public ValidacionException(IReadOnlyDictionary<string, string[]> errores)
        : base("Se encontraron uno o más errores de validación.")
    {
        Errores = errores;
    }

    public ValidacionException(string campo, string mensaje)
        : this(new Dictionary<string, string[]> { [campo] = [mensaje] }) { }
}

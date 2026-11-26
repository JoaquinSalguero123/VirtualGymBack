namespace VirtualGymAssistant.Domain.Exceptions;

/// <summary>Se pidió un recurso que no existe o que no pertenece al contexto actual. Devuelve 404.</summary>
public class NoEncontradoException : ExcepcionDeDominio
{
    public NoEncontradoException(string mensaje) : base(mensaje) { }

    /// <summary>Atajo para el mensaje habitual: "No se encontró el Plan con ID 7."</summary>
    public NoEncontradoException(string entidad, object id)
        : base($"No se encontró {entidad} con ID {id}.") { }
}

namespace VirtualGymAssistant.Application.Common.Interfaces;

/// <summary>
/// Expone en qué entorno está corriendo la API, para las poquísimas reglas que
/// dependen de ello (por ejemplo, devolver el código de recuperación en desarrollo).
/// </summary>
public interface IEntornoDeEjecucion
{
    bool EsDesarrollo { get; }
}

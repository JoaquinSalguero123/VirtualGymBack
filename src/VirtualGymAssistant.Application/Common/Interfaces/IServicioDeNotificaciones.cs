namespace VirtualGymAssistant.Application.Common.Interfaces;

/// <summary>
/// Canal de salida para avisarle algo a un usuario (mail o SMS).
/// La implementación por defecto sólo deja el mensaje en el log; se puede reemplazar
/// por un envío real sin tocar la lógica de negocio.
/// </summary>
public interface IServicioDeNotificaciones
{
    /// <summary>Envía el código de recuperación de contraseña al usuario.</summary>
    Task EnviarCodigoDeRecuperacionAsync(string email, string codigo, CancellationToken ct = default);

    /// <summary>Envía el código para confirmar el mail del usuario recién registrado.</summary>
    Task EnviarCodigoDeVerificacionAsync(string email, string codigo, CancellationToken ct = default);

    /// <summary>Envía la constancia de un pago aprobado, con el PDF adjunto.</summary>
    Task EnviarConstanciaDePagoAsync(
        string email, string nombreSocio, byte[] pdf, CancellationToken ct = default);
}

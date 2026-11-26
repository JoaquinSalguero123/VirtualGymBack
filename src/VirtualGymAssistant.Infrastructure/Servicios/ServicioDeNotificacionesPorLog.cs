using Microsoft.Extensions.Logging;
using VirtualGymAssistant.Application.Common.Interfaces;

namespace VirtualGymAssistant.Infrastructure.Servicios;

/// <summary>
/// Implementación de notificaciones que sólo escribe en el log.
///
/// Es la que se usa cuando no hay un SMTP configurado (ver <c>InyeccionDeDependencias</c>
/// de esta capa): así el flujo de recuperación de clave y de verificación de email
/// funcionan de punta a punta en cualquier entorno de desarrollo, sin depender de una
/// cuenta de correo real. Con un SMTP configurado, <see cref="ServicioDeNotificacionesPorEmail"/>
/// la reemplaza sin que la lógica de negocio se entere.
/// </summary>
public class ServicioDeNotificacionesPorLog : IServicioDeNotificaciones
{
    private readonly ILogger<ServicioDeNotificacionesPorLog> _log;

    public ServicioDeNotificacionesPorLog(ILogger<ServicioDeNotificacionesPorLog> log) => _log = log;

    public Task EnviarCodigoDeRecuperacionAsync(string email, string codigo, CancellationToken ct = default)
    {
        _log.LogInformation(
            "Código de recuperación para {Email}: {Codigo} (no se envió por mail: no hay SMTP configurado).",
            email, codigo);

        return Task.CompletedTask;
    }

    public Task EnviarCodigoDeVerificacionAsync(string email, string codigo, CancellationToken ct = default)
    {
        _log.LogInformation(
            "Código de verificación de email para {Email}: {Codigo} (no se envió por mail: no hay SMTP configurado).",
            email, codigo);

        return Task.CompletedTask;
    }

    public Task EnviarConstanciaDePagoAsync(
        string email, string nombreSocio, byte[] pdf, CancellationToken ct = default)
    {
        _log.LogInformation(
            "Constancia de pago de {NombreSocio} para {Email}: {Bytes} bytes " +
            "(no se envió por mail: no hay SMTP configurado).",
            nombreSocio, email, pdf.Length);

        return Task.CompletedTask;
    }
}

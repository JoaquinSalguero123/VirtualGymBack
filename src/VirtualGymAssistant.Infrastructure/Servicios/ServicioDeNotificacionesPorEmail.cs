using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using VirtualGymAssistant.Application.Common.Interfaces;

namespace VirtualGymAssistant.Infrastructure.Servicios;

/// <summary>
/// Envía las notificaciones por mail de verdad, vía SMTP (pensado para Gmail con una
/// contraseña de aplicación, pero sirve para cualquier proveedor SMTP estándar).
///
/// Se registra en el contenedor sólo cuando appsettings tiene "Smtp:Host" y "Smtp:Usuario"
/// configurados (ver <c>InyeccionDeDependencias</c> de esta capa); si no, se sigue usando
/// <see cref="ServicioDeNotificacionesPorLog"/> para no exigirle SMTP a cada desarrollador
/// que quiera correr el proyecto.
///
/// Si el envío falla (SMTP caído, credenciales vencidas), se loguea el error pero NO se
/// relanza: un mail que no salió no tiene por qué tirar abajo el registro o el pedido de
/// recuperación, que ya quedaron guardados en la base. El usuario puede reintentar el
/// reenvío del código más tarde.
/// </summary>
public class ServicioDeNotificacionesPorEmail : IServicioDeNotificaciones
{
    private readonly IConfiguration _configuracion;
    private readonly ILogger<ServicioDeNotificacionesPorEmail> _log;

    public ServicioDeNotificacionesPorEmail(
        IConfiguration configuracion, ILogger<ServicioDeNotificacionesPorEmail> log)
    {
        _configuracion = configuracion;
        _log = log;
    }

    public Task EnviarCodigoDeRecuperacionAsync(string email, string codigo, CancellationToken ct = default) =>
        EnviarCodigoAsync(
            email,
            "Recuperá tu contraseña de VirtualGymAssistant",
            "Recuperá tu contraseña",
            "Usá este código para elegir una contraseña nueva. Vence en 30 minutos.",
            codigo,
            ct);

    public Task EnviarCodigoDeVerificacionAsync(string email, string codigo, CancellationToken ct = default) =>
        EnviarCodigoAsync(
            email,
            "Confirmá tu email en VirtualGymAssistant",
            "Confirmá tu cuenta",
            "Usá este código para confirmar tu email. Vence en 30 minutos.",
            codigo,
            ct);

    public Task EnviarConstanciaDePagoAsync(
        string email, string nombreSocio, byte[] pdf, CancellationToken ct = default)
    {
        var cuerpo = new BodyBuilder { HtmlBody = ArmarHtmlDeConstancia(nombreSocio) };
        cuerpo.Attachments.Add("constancia-de-pago.pdf", pdf, ContentType.Parse("application/pdf"));

        return EnviarAsync(email, "Tu constancia de pago de VirtualGymAssistant", cuerpo.ToMessageBody(), ct);
    }

    private Task EnviarCodigoAsync(
        string destinatario, string asunto, string titulo, string bajada, string codigo, CancellationToken ct) =>
        EnviarAsync(
            destinatario, asunto, new TextPart("html") { Text = ArmarHtml(titulo, bajada, codigo) }, ct);

    /// <summary>Arma la conexión SMTP, manda el cuerpo ya construido y la cierra.</summary>
    private async Task EnviarAsync(string destinatario, string asunto, MimeEntity cuerpo, CancellationToken ct)
    {
        try
        {
            // InyeccionDeDependencias sólo registra esta clase cuando Smtp:Host y
            // Smtp:Usuario están cargados; esto es sólo una red de seguridad adicional.
            var host = _configuracion["Smtp:Host"]
                ?? throw new InvalidOperationException("Falta configurar Smtp:Host.");
            var usuario = _configuracion["Smtp:Usuario"]
                ?? throw new InvalidOperationException("Falta configurar Smtp:Usuario.");
            var clave = _configuracion["Smtp:Clave"] ?? string.Empty;
            var remitenteEmail = _configuracion["Smtp:RemitenteEmail"] ?? usuario;

            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress(
                _configuracion["Smtp:RemitenteNombre"] ?? "VirtualGymAssistant", remitenteEmail));
            mensaje.To.Add(MailboxAddress.Parse(destinatario));
            mensaje.Subject = asunto;
            mensaje.Body = cuerpo;

            var puerto = int.Parse(_configuracion["Smtp:Puerto"] ?? "587");
            var usarSsl = bool.Parse(_configuracion["Smtp:UsarSsl"] ?? "true");

            using var cliente = new SmtpClient();

            await cliente.ConnectAsync(
                host, puerto, usarSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto, ct);
            await cliente.AuthenticateAsync(usuario, clave, ct);
            await cliente.SendAsync(mensaje, ct);
            await cliente.DisconnectAsync(true, ct);

            _log.LogInformation("Mail enviado a {Email}: {Asunto}.", destinatario, asunto);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "No se pudo enviar el mail a {Email} ({Asunto}).", destinatario, asunto);
        }
    }

    private static string ArmarHtmlDeConstancia(string nombreSocio) => $"""
        <div style="font-family: Arial, sans-serif; max-width: 480px; margin: 0 auto; padding: 24px; color: #1a1a1a;">
          <h2 style="margin-bottom: 4px;">Tu constancia de pago</h2>
          <p style="color: #555;">
            Hola {nombreSocio}, te adjuntamos la constancia del pago que confirmamos en el mostrador.
          </p>
          <p style="color: #888; font-size: 13px;">Si no esperabas este mail, podés ignorarlo.</p>
        </div>
        """;

    private static string ArmarHtml(string titulo, string bajada, string codigo) => $"""
        <div style="font-family: Arial, sans-serif; max-width: 480px; margin: 0 auto; padding: 24px; color: #1a1a1a;">
          <h2 style="margin-bottom: 4px;">{titulo}</h2>
          <p style="color: #555;">{bajada}</p>
          <p style="font-size: 32px; font-weight: bold; letter-spacing: 6px; text-align: center;
                    background: #f4f4f5; padding: 16px; border-radius: 8px; margin: 24px 0;">
            {codigo}
          </p>
          <p style="color: #888; font-size: 13px;">Si no fuiste vos, podés ignorar este mensaje.</p>
        </div>
        """;
}

using Microsoft.EntityFrameworkCore;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Domain.Entities;

namespace VirtualGymAssistant.Application.Features.Autenticacion;

/// <summary>
/// Emite y manda por mail el código con el que una cuenta confirma su email.
///
/// Está en una clase aparte porque lo usan tres casos de uso (el registro propio, el
/// reenvío del código y el alta de personal por parte de la administración) y el código
/// tiene que generarse, vencer y enviarse siempre de la misma forma. El formato y la
/// vigencia son los mismos que los del código de recuperación de contraseña, por eso
/// ese flujo también los toma de acá.
/// </summary>
public class GestorDeVerificacionDeEmail
{
    /// <summary>Minutos que dura un código de un solo uso antes de vencer.</summary>
    public const int MinutosDeVigenciaDelCodigo = 30;

    private readonly IVirtualGymDbContext _db;
    private readonly IProveedorDeFechaHora _reloj;
    private readonly IServicioDeNotificaciones _notificaciones;

    public GestorDeVerificacionDeEmail(
        IVirtualGymDbContext db,
        IProveedorDeFechaHora reloj,
        IServicioDeNotificaciones notificaciones)
    {
        _db = db;
        _reloj = reloj;
        _notificaciones = notificaciones;
    }

    /// <summary>
    /// Genera un código nuevo, invalida los anteriores sin usar y lo manda por mail.
    /// Si el mail no sale, la cuenta igual queda guardada: la persona puede pedir otro
    /// código desde la pantalla de ingreso.
    /// </summary>
    public async Task EnviarCodigoAsync(Usuario usuario, CancellationToken ct = default)
    {
        var anteriores = await _db.TokensVerificacionEmail
            .Where(t => t.IdUsuario == usuario.Id && !t.Usado)
            .ToListAsync(ct);

        foreach (var token in anteriores) token.Usado = true;

        var codigo = GenerarCodigoDeUnSoloUso();

        _db.TokensVerificacionEmail.Add(new TokenVerificacionEmail
        {
            IdUsuario = usuario.Id,
            Codigo = codigo,
            Expira = _reloj.Ahora.AddMinutes(MinutosDeVigenciaDelCodigo),
            Usado = false,
            FechaCreacion = _reloj.Ahora
        });

        await _db.SaveChangesAsync(ct);
        await _notificaciones.EnviarCodigoDeVerificacionAsync(usuario.Mail, codigo, ct);
    }

    /// <summary>Código numérico de 6 dígitos para confirmar por mail o SMS.</summary>
    public static string GenerarCodigoDeUnSoloUso() => Random.Shared.Next(100_000, 1_000_000).ToString();
}

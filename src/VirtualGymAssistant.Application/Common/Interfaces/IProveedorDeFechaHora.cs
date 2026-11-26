namespace VirtualGymAssistant.Application.Common.Interfaces;

/// <summary>
/// Envuelve el reloj del sistema. Al no usar DateTime.Now directamente en las reglas
/// de negocio, los tests pueden fijar una fecha concreta y verificar vencimientos,
/// períodos de pago y asistencias sin depender del día en que se ejecuten.
/// </summary>
public interface IProveedorDeFechaHora
{
    DateTime Ahora { get; }
    DateOnly Hoy { get; }
}

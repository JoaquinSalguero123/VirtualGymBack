using VirtualGymAssistant.Application.Common.Interfaces;

namespace VirtualGymAssistant.Infrastructure.Servicios;

/// <summary>
/// Reloj real del sistema.
///
/// Se usa la hora LOCAL y no UTC a propósito: los períodos de cuota, las asistencias
/// diarias y los horarios de apertura son conceptos del huso horario del gimnasio.
/// Guardar UTC haría que un check-in de las 22 hs figure al día siguiente.
/// </summary>
public class ProveedorDeFechaHora : IProveedorDeFechaHora
{
    public DateTime Ahora => DateTime.Now;

    public DateOnly Hoy => DateOnly.FromDateTime(DateTime.Now);
}

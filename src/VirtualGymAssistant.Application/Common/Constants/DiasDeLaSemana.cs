namespace VirtualGymAssistant.Application.Common.Constants;

/// <summary>
/// Días de la semana con la numeración que usa la base (1 = Lunes ... 7 = Domingo),
/// que no coincide con DayOfWeek de .NET (donde 0 = Domingo).
/// </summary>
public static class DiasDeLaSemana
{
    public static readonly string[] Nombres =
    [
        "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo"
    ];

    /// <summary>Devuelve el nombre del día (1 a 7); cadena vacía si el número es inválido.</summary>
    public static string Nombre(int dia) =>
        dia is >= 1 and <= 7 ? Nombres[dia - 1] : string.Empty;
}

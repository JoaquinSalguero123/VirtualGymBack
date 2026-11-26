namespace VirtualGymAssistant.Application.Common.Constants;

/// <summary>
/// Nombres de los meses en español. La base guarda el período como número, pero tanto
/// el front como los reportes lo muestran por nombre, así que la conversión vive acá
/// para no repetirla en cada servicio.
/// </summary>
public static class Meses
{
    public static readonly string[] Nombres =
    [
        "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
        "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
    ];

    /// <summary>Devuelve el nombre del mes (1 a 12); cadena vacía si el número es inválido.</summary>
    public static string Nombre(int mes) =>
        mes is >= 1 and <= 12 ? Nombres[mes - 1] : string.Empty;

    /// <summary>
    /// Interpreta un mes que puede venir como número ("3") o como nombre ("Marzo").
    /// Devuelve null si no se puede reconocer.
    /// </summary>
    public static int? Interpretar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;

        if (int.TryParse(valor, out var numero))
            return numero is >= 1 and <= 12 ? numero : null;

        var indice = Array.FindIndex(Nombres,
            n => string.Equals(n, valor.Trim(), StringComparison.OrdinalIgnoreCase));

        return indice >= 0 ? indice + 1 : null;
    }
}

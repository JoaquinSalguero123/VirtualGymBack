using System.Text;
using System.Globalization;

namespace VirtualGymAssistant.Application.Common.Extensions;

/// <summary>Helpers de texto usados al normalizar datos que ingresa el usuario.</summary>
public static class TextoExtensions
{
    /// <summary>Recorta los espacios y convierte la cadena vacía en null.</summary>
    public static string? NormalizarONull(this string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    /// <summary>
    /// Deja el texto apto para usarse como nombre de usuario: sin acentos, en minúsculas
    /// y sólo con letras, números, punto, guion y guion bajo.
    /// </summary>
    public static string ASlugDeUsuario(this string texto)
    {
        var sinAcentos = new string(texto
            .Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray())
            .Normalize(NormalizationForm.FormC);

        var limpio = new string(sinAcentos
            .ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) || c is '.' or '_' or '-')
            .ToArray());

        return limpio.Length == 0 ? "usuario" : limpio[..Math.Min(limpio.Length, 40)];
    }
}

namespace VirtualGymAssistant.Application.Common.Models;

/// <summary>
/// Página de resultados de un listado. Evita traer padrones completos a memoria
/// cuando la sucursal tiene muchos socios.
/// </summary>
/// <typeparam name="T">Tipo de cada elemento devuelto.</typeparam>
public class ResultadoPaginado<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>Cantidad total de registros que cumplen el filtro (sin paginar).</summary>
    public int TotalRegistros { get; init; }

    public int Pagina { get; init; }
    public int TamanioPagina { get; init; }

    public int TotalPaginas => TamanioPagina <= 0
        ? 0
        : (int)Math.Ceiling(TotalRegistros / (double)TamanioPagina);

    public bool HayPaginaAnterior => Pagina > 1;
    public bool HayPaginaSiguiente => Pagina < TotalPaginas;

    public static ResultadoPaginado<T> Crear(IReadOnlyList<T> items, int total, int pagina, int tamanio) =>
        new() { Items = items, TotalRegistros = total, Pagina = pagina, TamanioPagina = tamanio };
}

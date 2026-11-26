using Microsoft.EntityFrameworkCore;
using VirtualGymAssistant.Application.Common.Models;

namespace VirtualGymAssistant.Application.Common.Extensions;

/// <summary>Utilidades de consulta reutilizadas por los servicios de la aplicación.</summary>
public static class ConsultaExtensions
{
    /// <summary>
    /// Ejecuta la consulta contando el total y trayendo sólo la página pedida.
    /// Son dos viajes a la base, pero evita materializar listados completos.
    /// </summary>
    public static async Task<ResultadoPaginado<T>> PaginarAsync<T>(
        this IQueryable<T> consulta,
        ParametrosDePaginacion parametros,
        CancellationToken ct = default)
    {
        var total = await consulta.CountAsync(ct);

        var items = await consulta
            .Skip(parametros.Salteo)
            .Take(parametros.TamanioPagina)
            .ToListAsync(ct);

        return ResultadoPaginado<T>.Crear(items, total, parametros.Pagina, parametros.TamanioPagina);
    }
}

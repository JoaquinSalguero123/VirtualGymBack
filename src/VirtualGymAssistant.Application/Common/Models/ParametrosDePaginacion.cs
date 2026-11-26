namespace VirtualGymAssistant.Application.Common.Models;

/// <summary>
/// Parámetros de paginación que aceptan los listados. Los valores se acotan solos
/// para que un pedido malformado no termine trayendo la tabla entera.
/// </summary>
public class ParametrosDePaginacion
{
    private const int TamanioMaximo = 200;

    private int _pagina = 1;
    private int _tamanioPagina = 50;

    /// <summary>Número de página, empezando en 1.</summary>
    public int Pagina
    {
        get => _pagina;
        set => _pagina = value < 1 ? 1 : value;
    }

    /// <summary>Cantidad de elementos por página (máximo 200).</summary>
    public int TamanioPagina
    {
        get => _tamanioPagina;
        set => _tamanioPagina = value switch
        {
            < 1 => 1,
            > TamanioMaximo => TamanioMaximo,
            _ => value
        };
    }

    /// <summary>Cantidad de registros a saltear para llegar a la página pedida.</summary>
    public int Salteo => (Pagina - 1) * TamanioPagina;
}

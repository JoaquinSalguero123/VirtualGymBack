using System.Security.Claims;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Domain.Exceptions;

namespace VirtualGymAssistant.API.Servicios;

/// <summary>
/// Lee del pedido HTTP quién está autenticado y sobre qué sucursal quiere trabajar.
///
/// Es el puente entre el mundo HTTP y la capa Application: la lógica de negocio sólo ve
/// la interfaz <see cref="IUsuarioActual"/> y no sabe que existen tokens ni cabeceras.
/// </summary>
public class UsuarioActual : IUsuarioActual
{
    /// <summary>Cabecera con la que el front indica la sucursal activa.</summary>
    public const string CabeceraDeGimnasio = "X-Gimnasio-Id";

    /// <summary>Alternativa por query string, cómoda para probar desde el navegador.</summary>
    private const string QueryDeGimnasio = "gimnasio";

    private readonly IHttpContextAccessor _accesor;

    public UsuarioActual(IHttpContextAccessor accesor) => _accesor = accesor;

    private ClaimsPrincipal? Usuario => _accesor.HttpContext?.User;

    public int? Id
    {
        get
        {
            var valor = Usuario?.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(valor, out var id) ? id : null;
        }
    }

    public string? Mail => Usuario?.FindFirstValue(ClaimTypes.Email);

    public string? Username => Usuario?.FindFirstValue(ClaimTypes.Name);

    public bool EstaAutenticado => Usuario?.Identity?.IsAuthenticated == true && Id is not null;

    public int? IdGimnasioSolicitado
    {
        get
        {
            var http = _accesor.HttpContext;
            if (http is null) return null;

            if (http.Request.Headers.TryGetValue(CabeceraDeGimnasio, out var cabecera) &&
                int.TryParse(cabecera.FirstOrDefault(), out var desdeCabecera))
                return desdeCabecera;

            if (http.Request.Query.TryGetValue(QueryDeGimnasio, out var query) &&
                int.TryParse(query.FirstOrDefault(), out var desdeQuery))
                return desdeQuery;

            return null;
        }
    }

    public int IdRequerido => Id
        ?? throw new NoAutenticadoException("La operación requiere un usuario autenticado.");
}

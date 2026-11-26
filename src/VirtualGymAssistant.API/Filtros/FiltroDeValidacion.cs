using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;
using VirtualGymAssistant.Domain.Exceptions;

namespace VirtualGymAssistant.API.Filtros;

/// <summary>
/// Ejecuta automáticamente el validador de FluentValidation que corresponda a cada
/// objeto recibido en el cuerpo del pedido, antes de que se ejecute la acción.
///
/// Si algo no valida, lanza <see cref="ValidacionException"/> y el middleware de errores
/// la convierte en un 400 con el detalle campo por campo. Así los controladores nunca
/// tienen que acordarse de validar.
/// </summary>
public class FiltroDeValidacion : IAsyncActionFilter
{
    private readonly IServiceProvider _servicios;

    public FiltroDeValidacion(IServiceProvider servicios) => _servicios = servicios;

    public async Task OnActionExecutionAsync(ActionExecutingContext contexto, ActionExecutionDelegate siguiente)
    {
        var errores = new Dictionary<string, string[]>();

        foreach (var argumento in contexto.ActionArguments.Values)
        {
            if (argumento is null) continue;

            // Se busca un IValidator<TipoDelArgumento> registrado en el contenedor.
            var tipoDeValidador = typeof(IValidator<>).MakeGenericType(argumento.GetType());

            if (_servicios.GetService(tipoDeValidador) is not IValidator validador) continue;

            var resultado = await validador.ValidateAsync(
                new ValidationContext<object>(argumento), contexto.HttpContext.RequestAborted);

            if (resultado.IsValid) continue;

            foreach (var grupo in resultado.Errors.GroupBy(e => e.PropertyName))
                errores[grupo.Key] = grupo.Select(e => e.ErrorMessage).ToArray();
        }

        if (errores.Count > 0)
            throw new ValidacionException(errores);

        await siguiente();
    }
}

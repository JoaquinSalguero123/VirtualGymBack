using System.Linq.Expressions;
using FluentValidation;
using VirtualGymAssistant.Application.Features.Sucursales.Dtos;

namespace VirtualGymAssistant.Application.Features.Sucursales.Validadores;

/// <summary>
/// Reglas de la ubicación en el mapa, compartidas por el alta y la edición: van las dos
/// coordenadas o ninguna, y dentro del rango que existe sobre la Tierra. La base repite
/// la misma regla en un CHECK, pero acá el error le llega al formulario campo por campo
/// en vez de como un fallo de SQL.
/// </summary>
internal static class ReglasDeUbicacion
{
    public static void Aplicar<T>(
        AbstractValidator<T> validador,
        Expression<Func<T, decimal?>> latitud,
        Expression<Func<T, decimal?>> longitud)
    {
        var tieneLatitud = latitud.Compile();
        var tieneLongitud = longitud.Compile();

        validador.RuleFor(latitud)
            .InclusiveBetween(-90m, 90m).WithMessage("La latitud tiene que estar entre -90 y 90.")
            .When(x => tieneLatitud(x).HasValue);

        validador.RuleFor(longitud)
            .InclusiveBetween(-180m, 180m).WithMessage("La longitud tiene que estar entre -180 y 180.")
            .When(x => tieneLongitud(x).HasValue);

        validador.RuleFor(latitud)
            .NotNull().WithMessage("Falta la latitud: la ubicación necesita latitud y longitud.")
            .When(x => tieneLongitud(x).HasValue);

        validador.RuleFor(longitud)
            .NotNull().WithMessage("Falta la longitud: la ubicación necesita latitud y longitud.")
            .When(x => tieneLatitud(x).HasValue);
    }
}

public class CrearSucursalRequestValidator : AbstractValidator<CrearSucursalRequest>
{
    public CrearSucursalRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la sucursal es obligatorio.")
            .MaximumLength(80);

        RuleFor(x => x.Direccion)
            .NotEmpty().WithMessage("La dirección es obligatoria.")
            .MaximumLength(150);

        ReglasDeUbicacion.Aplicar(this, x => x.Latitud, x => x.Longitud);

        RuleFor(x => x.Telefono).MaximumLength(30);

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("El email no tiene un formato válido.")
            .MaximumLength(254)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public class ActualizarSucursalRequestValidator : AbstractValidator<ActualizarSucursalRequest>
{
    public ActualizarSucursalRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la sucursal es obligatorio.")
            .MaximumLength(80);

        RuleFor(x => x.Direccion)
            .NotEmpty().WithMessage("La dirección es obligatoria.")
            .MaximumLength(150);

        ReglasDeUbicacion.Aplicar(this, x => x.Latitud, x => x.Longitud);

        RuleFor(x => x.Telefono).MaximumLength(30);

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("El email no tiene un formato válido.")
            .MaximumLength(254)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

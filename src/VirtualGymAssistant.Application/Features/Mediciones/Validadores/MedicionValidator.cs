using FluentValidation;
using VirtualGymAssistant.Application.Features.Mediciones.Dtos;

namespace VirtualGymAssistant.Application.Features.Mediciones.Validadores;

public class GuardarMedicionRequestValidator : AbstractValidator<GuardarMedicionRequest>
{
    public GuardarMedicionRequestValidator()
    {
        RuleFor(x => x.IdSocio).GreaterThan(0).WithMessage("Tenés que indicar el socio.");

        RuleFor(x => x.Peso)
            .GreaterThan(0).WithMessage("El peso tiene que ser mayor a cero.")
            .LessThan(500).WithMessage("El peso ingresado no es razonable.");

        RuleFor(x => x.GrasaPct)
            .InclusiveBetween(0, 100).WithMessage("El porcentaje de grasa debe estar entre 0 y 100.")
            .When(x => x.GrasaPct.HasValue);

        RuleFor(x => x.MusculoPct)
            .InclusiveBetween(0, 100).WithMessage("El porcentaje de músculo debe estar entre 0 y 100.")
            .When(x => x.MusculoPct.HasValue);

        RuleFor(x => x.Observaciones).MaximumLength(255);
    }
}

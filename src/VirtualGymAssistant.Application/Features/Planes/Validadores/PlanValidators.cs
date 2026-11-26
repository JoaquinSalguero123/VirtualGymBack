using FluentValidation;
using VirtualGymAssistant.Application.Features.Planes.Dtos;

namespace VirtualGymAssistant.Application.Features.Planes.Validadores;

public class CrearPlanRequestValidator : AbstractValidator<CrearPlanRequest>
{
    public CrearPlanRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del plan es obligatorio.")
            .MaximumLength(100);

        RuleFor(x => x.Descripcion).MaximumLength(255);

        RuleFor(x => x.Precio)
            .GreaterThanOrEqualTo(0).WithMessage("El precio no puede ser negativo.");

        RuleFor(x => x.DuracionDias)
            .InclusiveBetween(1, 3650).WithMessage("La duración debe estar entre 1 y 3650 días.");

        RuleFor(x => x.Nivel).MaximumLength(50);
    }
}

public class ActualizarPlanRequestValidator : AbstractValidator<ActualizarPlanRequest>
{
    public ActualizarPlanRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del plan es obligatorio.")
            .MaximumLength(100);

        RuleFor(x => x.Descripcion).MaximumLength(255);

        RuleFor(x => x.Precio)
            .GreaterThanOrEqualTo(0).WithMessage("El precio no puede ser negativo.");

        RuleFor(x => x.DuracionDias)
            .InclusiveBetween(1, 3650).WithMessage("La duración debe estar entre 1 y 3650 días.");

        RuleFor(x => x.Nivel).MaximumLength(50);
    }
}

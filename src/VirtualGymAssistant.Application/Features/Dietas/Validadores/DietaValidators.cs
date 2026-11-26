using FluentValidation;
using VirtualGymAssistant.Application.Features.Dietas.Dtos;

namespace VirtualGymAssistant.Application.Features.Dietas.Validadores;

public class GenerarDietaRequestValidator : AbstractValidator<GenerarDietaRequest>
{
    public GenerarDietaRequestValidator()
    {
        RuleFor(x => x.Dias)
            .InclusiveBetween(1, 30).WithMessage("La dieta puede generarse de 1 a 30 días.");
    }
}

public class DiaDeDietaRequestValidator : AbstractValidator<DiaDeDietaRequest>
{
    public DiaDeDietaRequestValidator()
    {
        RuleFor(x => x.Dia)
            .NotEmpty().WithMessage("Cada día tiene que tener una etiqueta.")
            .MaximumLength(20);

        RuleForEach(x => x.Comidas).ChildRules(comida =>
        {
            comida.RuleFor(c => c.Categoria)
                .NotEmpty().WithMessage("Cada comida tiene que tener una categoría.")
                .MaximumLength(50);

            comida.RuleFor(c => c.Alimento)
                .NotEmpty().WithMessage("Cada comida tiene que indicar qué se come.")
                .MaximumLength(150);
        });
    }
}

public class GuardarDietaRequestValidator : AbstractValidator<GuardarDietaRequest>
{
    public GuardarDietaRequestValidator()
    {
        RuleFor(x => x.Nombre).MaximumLength(100);

        RuleFor(x => x.Dias)
            .NotEmpty().WithMessage("La dieta tiene que tener al menos un día cargado.");

        RuleForEach(x => x.Dias).SetValidator(new DiaDeDietaRequestValidator());
    }
}

public class AsignarDietaRequestValidator : AbstractValidator<AsignarDietaRequest>
{
    public AsignarDietaRequestValidator()
    {
        RuleFor(x => x.IdSocio).GreaterThan(0).WithMessage("Tenés que indicar el socio.");
        RuleFor(x => x.Nombre).MaximumLength(100);

        RuleFor(x => x.Dias)
            .NotEmpty().WithMessage("La dieta tiene que tener al menos un día cargado.");

        RuleForEach(x => x.Dias).SetValidator(new DiaDeDietaRequestValidator());
    }
}

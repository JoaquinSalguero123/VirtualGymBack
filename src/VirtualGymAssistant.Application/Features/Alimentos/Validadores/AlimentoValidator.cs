using FluentValidation;
using VirtualGymAssistant.Application.Features.Alimentos.Dtos;

namespace VirtualGymAssistant.Application.Features.Alimentos.Validadores;

public class GuardarAlimentoRequestValidator : AbstractValidator<GuardarAlimentoRequest>
{
    public GuardarAlimentoRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del alimento es obligatorio.")
            .MaximumLength(100);

        RuleFor(x => x.Descripcion).MaximumLength(255);

        RuleFor(x => x.Calorias)
            .InclusiveBetween(0, 10000).WithMessage("Las calorías deben estar entre 0 y 10000.")
            .When(x => x.Calorias.HasValue);

        RuleFor(x => x.Proteinas)
            .InclusiveBetween(0, 1000).WithMessage("Las proteínas no pueden ser negativas.")
            .When(x => x.Proteinas.HasValue);

        RuleFor(x => x.Carbohidratos)
            .InclusiveBetween(0, 1000).WithMessage("Los carbohidratos no pueden ser negativos.")
            .When(x => x.Carbohidratos.HasValue);

        RuleFor(x => x.Grasas)
            .InclusiveBetween(0, 1000).WithMessage("Las grasas no pueden ser negativas.")
            .When(x => x.Grasas.HasValue);
    }
}

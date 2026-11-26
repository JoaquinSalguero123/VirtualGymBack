using FluentValidation;
using VirtualGymAssistant.Application.Features.Catalogos.Dtos;

namespace VirtualGymAssistant.Application.Features.Catalogos.Validadores;

public class GuardarItemDeCatalogoRequestValidator : AbstractValidator<GuardarItemDeCatalogoRequest>
{
    public GuardarItemDeCatalogoRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(50);
    }
}

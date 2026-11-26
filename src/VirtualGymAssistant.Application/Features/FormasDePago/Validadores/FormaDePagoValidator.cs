using FluentValidation;
using VirtualGymAssistant.Application.Features.FormasDePago.Dtos;

namespace VirtualGymAssistant.Application.Features.FormasDePago.Validadores;

public class GuardarFormaDePagoRequestValidator : AbstractValidator<GuardarFormaDePagoRequest>
{
    public GuardarFormaDePagoRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la forma de pago es obligatorio.")
            .MaximumLength(50);
    }
}

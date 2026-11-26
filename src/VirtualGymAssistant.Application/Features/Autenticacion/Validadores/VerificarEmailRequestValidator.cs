using FluentValidation;
using VirtualGymAssistant.Application.Features.Autenticacion.Dtos;

namespace VirtualGymAssistant.Application.Features.Autenticacion.Validadores;

public class VerificarEmailRequestValidator : AbstractValidator<VerificarEmailRequest>
{
    public VerificarEmailRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .EmailAddress().WithMessage("El email no tiene un formato válido.");

        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("Ingresá el código que te mandamos por mail.")
            .Length(6).WithMessage("El código tiene 6 dígitos.");
    }
}

public class ReenviarVerificacionRequestValidator : AbstractValidator<ReenviarVerificacionRequest>
{
    public ReenviarVerificacionRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .EmailAddress().WithMessage("El email no tiene un formato válido.");
    }
}

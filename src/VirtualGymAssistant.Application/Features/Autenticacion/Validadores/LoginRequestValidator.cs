using FluentValidation;
using VirtualGymAssistant.Application.Features.Autenticacion.Dtos;

namespace VirtualGymAssistant.Application.Features.Autenticacion.Validadores;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .EmailAddress().WithMessage("El email no tiene un formato válido.");

        RuleFor(x => x.Contrasena)
            .NotEmpty().WithMessage("La contraseña es obligatoria.");
    }
}

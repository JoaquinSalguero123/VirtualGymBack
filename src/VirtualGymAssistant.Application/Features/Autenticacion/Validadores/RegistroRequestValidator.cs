using FluentValidation;
using VirtualGymAssistant.Application.Features.Autenticacion.Dtos;

namespace VirtualGymAssistant.Application.Features.Autenticacion.Validadores;

/// <summary>Validaciones de formato del registro. Las reglas de negocio viven en el servicio.</summary>
public class RegistroRequestValidator : AbstractValidator<RegistroRequest>
{
    public RegistroRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(60);

        RuleFor(x => x.Apellido)
            .NotEmpty().WithMessage("El apellido es obligatorio.")
            .MaximumLength(60);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .EmailAddress().WithMessage("El email no tiene un formato válido.")
            .MaximumLength(254);

        RuleFor(x => x.Telefono)
            .MaximumLength(30)
            .When(x => !string.IsNullOrWhiteSpace(x.Telefono));

        RuleFor(x => x.Contrasena)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.")
            .MaximumLength(100);
    }
}

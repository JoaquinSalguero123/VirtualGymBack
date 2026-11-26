using FluentValidation;
using VirtualGymAssistant.Application.Features.Autenticacion.Dtos;

namespace VirtualGymAssistant.Application.Features.Autenticacion.Validadores;

public class SolicitarRecuperacionRequestValidator : AbstractValidator<SolicitarRecuperacionRequest>
{
    public SolicitarRecuperacionRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .EmailAddress().WithMessage("El email no tiene un formato válido.");
    }
}

public class RestablecerClaveRequestValidator : AbstractValidator<RestablecerClaveRequest>
{
    public RestablecerClaveRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().EmailAddress();

        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("El código es obligatorio.");

        RuleFor(x => x.NuevaClave)
            .NotEmpty().WithMessage("La nueva contraseña es obligatoria.")
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.");

        RuleFor(x => x.ConfirmacionClave)
            .Equal(x => x.NuevaClave).WithMessage("Las contraseñas no coinciden.");
    }
}

public class CambiarClaveRequestValidator : AbstractValidator<CambiarClaveRequest>
{
    public CambiarClaveRequestValidator()
    {
        RuleFor(x => x.ClaveActual)
            .NotEmpty().WithMessage("Tenés que indicar tu contraseña actual.");

        RuleFor(x => x.NuevaClave)
            .NotEmpty().WithMessage("La nueva contraseña es obligatoria.")
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.")
            .NotEqual(x => x.ClaveActual).WithMessage("La nueva contraseña tiene que ser distinta de la actual.");

        RuleFor(x => x.ConfirmacionClave)
            .Equal(x => x.NuevaClave).WithMessage("Las contraseñas no coinciden.");
    }
}

using FluentValidation;
using VirtualGymAssistant.Application.Features.Usuarios.Dtos;

namespace VirtualGymAssistant.Application.Features.Usuarios.Validadores;

public class CrearUsuarioRequestValidator : AbstractValidator<CrearUsuarioRequest>
{
    public CrearUsuarioRequestValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().WithMessage("El nombre es obligatorio.").MaximumLength(60);
        RuleFor(x => x.Apellido).NotEmpty().WithMessage("El apellido es obligatorio.").MaximumLength(60);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .EmailAddress().WithMessage("El email no tiene un formato válido.")
            .MaximumLength(254);

        RuleFor(x => x.Telefono).MaximumLength(30);
        RuleFor(x => x.Username).MaximumLength(50);

        RuleFor(x => x.Contrasena)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.");

        RuleFor(x => x.IdRol)
            .GreaterThan(0).WithMessage("Tenés que elegir un rol para el usuario.");
    }
}

public class ActualizarUsuarioRequestValidator : AbstractValidator<ActualizarUsuarioRequest>
{
    public ActualizarUsuarioRequestValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().WithMessage("El nombre es obligatorio.").MaximumLength(60);
        RuleFor(x => x.Apellido).NotEmpty().WithMessage("El apellido es obligatorio.").MaximumLength(60);
        RuleFor(x => x.Telefono).MaximumLength(30);
        RuleFor(x => x.Username).MaximumLength(50);
    }
}

public class AsignarRolRequestValidator : AbstractValidator<AsignarRolRequest>
{
    public AsignarRolRequestValidator()
    {
        RuleFor(x => x.IdGimnasio).GreaterThan(0).WithMessage("Tenés que indicar la sucursal.");
        RuleFor(x => x.IdRol).GreaterThan(0).WithMessage("Tenés que indicar el rol.");
    }
}

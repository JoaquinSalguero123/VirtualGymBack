using FluentValidation;
using VirtualGymAssistant.Application.Features.Ejercicios.Dtos;

namespace VirtualGymAssistant.Application.Features.Ejercicios.Validadores;

public class GuardarEjercicioRequestValidator : AbstractValidator<GuardarEjercicioRequest>
{
    public GuardarEjercicioRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del ejercicio es obligatorio.")
            .MaximumLength(100);

        RuleFor(x => x.Descripcion).MaximumLength(500);

        RuleFor(x => x.IdGrupo)
            .GreaterThan(0).WithMessage("Tenés que elegir un grupo muscular.");

        RuleFor(x => x.VideoUrl)
            .MaximumLength(300)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.VideoUrl))
            .WithMessage("El enlace del video no es una URL válida.");
    }
}

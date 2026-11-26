using FluentValidation;
using VirtualGymAssistant.Application.Features.Foro.Dtos;

namespace VirtualGymAssistant.Application.Features.Foro.Validadores;

public class CrearPublicacionRequestValidator : AbstractValidator<CrearPublicacionRequest>
{
    public CrearPublicacionRequestValidator()
    {
        RuleFor(x => x.Contenido).MaximumLength(4000);

        RuleFor(x => x.IdPostPadre)
            .GreaterThan(0).WithMessage("La publicación a la que respondés no es válida.")
            .When(x => x.IdPostPadre is not null);
    }
}

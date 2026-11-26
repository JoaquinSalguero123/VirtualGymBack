using FluentValidation;
using VirtualGymAssistant.Application.Features.SitioWeb.Dtos;

namespace VirtualGymAssistant.Application.Features.SitioWeb.Validadores;

public class GuardarCarouselItemRequestValidator : AbstractValidator<GuardarCarouselItemRequest>
{
    public GuardarCarouselItemRequestValidator()
    {
        RuleFor(x => x.Tipo).MaximumLength(50);
        RuleFor(x => x.Titulo).MaximumLength(100);
        RuleFor(x => x.Descripcion).MaximumLength(255);
    }
}

public class GuardarFooterConfigRequestValidator : AbstractValidator<GuardarFooterConfigRequest>
{
    public GuardarFooterConfigRequestValidator()
    {
        RuleFor(x => x.Seccion)
            .NotEmpty().WithMessage("La sección es obligatoria.")
            .MaximumLength(50);

        RuleFor(x => x.Clave)
            .NotEmpty().WithMessage("La clave es obligatoria.")
            .MaximumLength(50);

        RuleFor(x => x.Valor).MaximumLength(255);
    }
}

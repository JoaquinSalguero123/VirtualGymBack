using FluentValidation;
using VirtualGymAssistant.Application.Features.Pagos.Dtos;

namespace VirtualGymAssistant.Application.Features.Pagos.Validadores;

public class RegistrarPagoRequestValidator : AbstractValidator<RegistrarPagoRequest>
{
    public RegistrarPagoRequestValidator()
    {
        RuleFor(x => x.Mes)
            .InclusiveBetween(1, 12).WithMessage("El mes debe estar entre 1 y 12.")
            .When(x => x.Mes.HasValue);

        RuleFor(x => x.Anio)
            .InclusiveBetween(2000, 2100).WithMessage("El año del período no es válido.")
            .When(x => x.Anio.HasValue);

        RuleFor(x => x.IdPlan).GreaterThan(0).WithMessage("Tenés que elegir un plan.");
        RuleFor(x => x.IdFormaPago).GreaterThan(0).WithMessage("Tenés que elegir una forma de pago.");
        RuleFor(x => x.Comprobante).MaximumLength(50);
    }
}

public class RegistrarCobroPresencialRequestValidator : AbstractValidator<RegistrarCobroPresencialRequest>
{
    public RegistrarCobroPresencialRequestValidator()
    {
        RuleFor(x => x.IdSocio).GreaterThan(0).WithMessage("Tenés que indicar el socio.");

        RuleFor(x => x.Mes)
            .InclusiveBetween(1, 12).WithMessage("El mes debe estar entre 1 y 12.")
            .When(x => x.Mes.HasValue);

        RuleFor(x => x.Anio)
            .InclusiveBetween(2000, 2100).WithMessage("El año del período no es válido.")
            .When(x => x.Anio.HasValue);

        RuleFor(x => x.IdPlan).GreaterThan(0).WithMessage("Tenés que elegir un plan.");
        RuleFor(x => x.IdFormaPago).GreaterThan(0).WithMessage("Tenés que elegir una forma de pago.");
        RuleFor(x => x.Comprobante).MaximumLength(50);

        RuleFor(x => x.Monto)
            .GreaterThanOrEqualTo(0).WithMessage("El monto no puede ser negativo.")
            .When(x => x.Monto.HasValue);
    }
}

public class EnviarConstanciaDePagoRequestValidator : AbstractValidator<EnviarConstanciaDePagoRequest>
{
    public EnviarConstanciaDePagoRequestValidator()
    {
        RuleFor(x => x.PdfBase64).NotEmpty().WithMessage("Falta el PDF de la constancia.");
    }
}

public class CambiarEstadoDePagoRequestValidator : AbstractValidator<CambiarEstadoDePagoRequest>
{
    public CambiarEstadoDePagoRequestValidator()
    {
        RuleFor(x => x.Estado)
            .NotEmpty().WithMessage("Tenés que indicar el nuevo estado del pago.");

        RuleFor(x => x.Observaciones).MaximumLength(255);
    }
}

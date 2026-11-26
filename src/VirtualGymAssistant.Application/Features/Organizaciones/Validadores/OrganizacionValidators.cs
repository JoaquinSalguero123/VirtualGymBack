using System.Text.RegularExpressions;
using FluentValidation;
using VirtualGymAssistant.Application.Features.Organizaciones.Dtos;

namespace VirtualGymAssistant.Application.Features.Organizaciones.Validadores;

/// <summary>Validaciones compartidas por el alta y la edición de organizaciones.</summary>
public static class ReglasDeOrganizacion
{
    /// <summary>Acepta el CUIT con o sin guiones: 30-12345678-9 o 30123456789.</summary>
    public static readonly Regex FormatoCuit = new(@"^\d{2}-?\d{8}-?\d$", RegexOptions.Compiled);
}

public class CrearOrganizacionRequestValidator : AbstractValidator<CrearOrganizacionRequest>
{
    public CrearOrganizacionRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la organización es obligatorio.")
            .MaximumLength(100);

        RuleFor(x => x.RazonSocial).MaximumLength(150);

        RuleFor(x => x.Cuit)
            .Must(cuit => ReglasDeOrganizacion.FormatoCuit.IsMatch(cuit!))
            .When(x => !string.IsNullOrWhiteSpace(x.Cuit))
            .WithMessage("El CUIT debe tener el formato 30-12345678-9.");
    }
}

public class ActualizarOrganizacionRequestValidator : AbstractValidator<ActualizarOrganizacionRequest>
{
    public ActualizarOrganizacionRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la organización es obligatorio.")
            .MaximumLength(100);

        RuleFor(x => x.RazonSocial).MaximumLength(150);

        RuleFor(x => x.Cuit)
            .Must(cuit => ReglasDeOrganizacion.FormatoCuit.IsMatch(cuit!))
            .When(x => !string.IsNullOrWhiteSpace(x.Cuit))
            .WithMessage("El CUIT debe tener el formato 30-12345678-9.");
    }
}

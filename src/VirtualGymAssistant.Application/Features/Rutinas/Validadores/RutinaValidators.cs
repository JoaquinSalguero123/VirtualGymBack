using FluentValidation;
using VirtualGymAssistant.Application.Features.Rutinas.Dtos;

namespace VirtualGymAssistant.Application.Features.Rutinas.Validadores;

public class DiaDeRutinaRequestValidator : AbstractValidator<DiaDeRutinaRequest>
{
    public DiaDeRutinaRequestValidator()
    {
        RuleFor(x => x.Dia)
            .InclusiveBetween(1, 7).WithMessage("El día debe estar entre 1 y 7.");

        RuleForEach(x => x.Ejercicios).ChildRules(ejercicio =>
            ejercicio.RuleFor(e => e.IdEjercicio)
                .GreaterThan(0).WithMessage("Cada renglón debe tener un ejercicio válido."));
    }
}

public class GuardarPlantillaRequestValidator : AbstractValidator<GuardarPlantillaRequest>
{
    public GuardarPlantillaRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la rutina es obligatorio.")
            .MaximumLength(100);

        RuleFor(x => x.Descripcion).MaximumLength(255);
        RuleFor(x => x.Intensidad).MaximumLength(50);

        RuleFor(x => x.DiasPorSemana)
            .InclusiveBetween(1, 7).WithMessage("Los días por semana deben estar entre 1 y 7.")
            .When(x => x.DiasPorSemana.HasValue);

        RuleFor(x => x.EjerciciosPorDia)
            .InclusiveBetween(1, 20).WithMessage("Los ejercicios por día deben estar entre 1 y 20.")
            .When(x => x.EjerciciosPorDia.HasValue);

        RuleForEach(x => x.Dias).SetValidator(new DiaDeRutinaRequestValidator());
    }
}

public class GenerarRutinaRequestValidator : AbstractValidator<GenerarRutinaRequest>
{
    public GenerarRutinaRequestValidator()
    {
        RuleFor(x => x.DiasPorSemana)
            .InclusiveBetween(1, 7).WithMessage("Los días por semana deben estar entre 1 y 7.");

        RuleFor(x => x.EjerciciosPorDia)
            .InclusiveBetween(1, 15).WithMessage("Los ejercicios por día deben estar entre 1 y 15.");
    }
}

public class GuardarRutinaDeSocioRequestValidator : AbstractValidator<GuardarRutinaDeSocioRequest>
{
    public GuardarRutinaDeSocioRequestValidator()
    {
        RuleFor(x => x.Nombre).MaximumLength(100);

        RuleFor(x => x.Dias)
            .NotEmpty().WithMessage("La rutina tiene que tener al menos un día cargado.");

        RuleForEach(x => x.Dias).SetValidator(new DiaDeRutinaRequestValidator());
    }
}

public class AsignarRutinaRequestValidator : AbstractValidator<AsignarRutinaRequest>
{
    public AsignarRutinaRequestValidator()
    {
        RuleFor(x => x.IdSocio).GreaterThan(0).WithMessage("Tenés que indicar el socio.");
        RuleFor(x => x.Nombre).MaximumLength(100);

        RuleFor(x => x.Dias)
            .NotEmpty().WithMessage("La rutina tiene que tener al menos un día cargado.");

        RuleForEach(x => x.Dias).SetValidator(new DiaDeRutinaRequestValidator());
    }
}

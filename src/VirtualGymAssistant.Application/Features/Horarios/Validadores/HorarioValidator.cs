using FluentValidation;
using VirtualGymAssistant.Application.Features.Horarios.Dtos;

namespace VirtualGymAssistant.Application.Features.Horarios.Validadores;

public class GuardarHorarioRequestValidator : AbstractValidator<GuardarHorarioRequest>
{
    public GuardarHorarioRequestValidator()
    {
        RuleFor(x => x.DiaSemana)
            .InclusiveBetween((byte)1, (byte)7)
            .WithMessage("El día debe estar entre 1 (lunes) y 7 (domingo).");

        RuleFor(x => x.Apertura)
            .NotEmpty().WithMessage("La hora de apertura es obligatoria.")
            .Must(SerHoraValida).WithMessage("La hora de apertura debe tener el formato HH:mm.");

        RuleFor(x => x.Cierre)
            .NotEmpty().WithMessage("La hora de cierre es obligatoria.")
            .Must(SerHoraValida).WithMessage("La hora de cierre debe tener el formato HH:mm.");
    }

    private static bool SerHoraValida(string valor) => TimeOnly.TryParse(valor, out _);
}

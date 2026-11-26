using FluentValidation;
using VirtualGymAssistant.Application.Features.Socios.Dtos;

namespace VirtualGymAssistant.Application.Features.Socios.Validadores;

public class UnirseASucursalRequestValidator : AbstractValidator<UnirseASucursalRequest>
{
    public UnirseASucursalRequestValidator()
    {
        RuleFor(x => x.IdGimnasio).GreaterThan(0).WithMessage("Tenés que elegir una sucursal.");
    }
}

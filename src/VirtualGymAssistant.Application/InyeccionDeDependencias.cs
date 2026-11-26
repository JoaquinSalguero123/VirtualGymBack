using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Application.Features.Alimentos;
using VirtualGymAssistant.Application.Features.Asistencias;
using VirtualGymAssistant.Application.Features.Autenticacion;
using VirtualGymAssistant.Application.Features.Catalogos;
using VirtualGymAssistant.Application.Features.Contexto;
using VirtualGymAssistant.Application.Features.Dietas;
using VirtualGymAssistant.Application.Features.Ejercicios;
using VirtualGymAssistant.Application.Features.Foro;
using VirtualGymAssistant.Application.Features.FormasDePago;
using VirtualGymAssistant.Application.Features.Horarios;
using VirtualGymAssistant.Application.Features.Mediciones;
using VirtualGymAssistant.Application.Features.Membresias;
using VirtualGymAssistant.Application.Features.Organizaciones;
using VirtualGymAssistant.Application.Features.Pagos;
using VirtualGymAssistant.Application.Features.Planes;
using VirtualGymAssistant.Application.Features.Rutinas;
using VirtualGymAssistant.Application.Features.SitioWeb;
using VirtualGymAssistant.Application.Features.Socios;
using VirtualGymAssistant.Application.Features.Sucursales;
using VirtualGymAssistant.Application.Features.Usuarios;

namespace VirtualGymAssistant.Application;

/// <summary>
/// Registra los servicios de casos de uso y los validadores de la capa Application.
///
/// Todos van con ciclo de vida Scoped (uno por petición HTTP), igual que el DbContext:
/// así todos los servicios que participan de una misma petición comparten la unidad de
/// trabajo y el contexto de sucursal ya resuelto.
/// </summary>
public static class InyeccionDeDependencias
{
    public static IServiceCollection AgregarAplicacion(this IServiceCollection servicios)
    {
        // Contexto multi-sucursal: lo consume casi todo el resto.
        servicios.AddScoped<IServicioDeContexto, ServicioDeContexto>();

        // Regla compartida de creación automática de la ficha de socio.
        servicios.AddScoped<GestorDeFichasDeSocio>();

        // Regla compartida de emisión y envío del código de verificación de email.
        servicios.AddScoped<GestorDeVerificacionDeEmail>();

        // --- Casos de uso ---
        servicios.AddScoped<ServicioDeAutenticacion>();
        servicios.AddScoped<ServicioDeOrganizaciones>();
        servicios.AddScoped<ServicioDeSucursales>();
        servicios.AddScoped<ServicioDeUsuarios>();
        servicios.AddScoped<ServicioDeSocios>();
        servicios.AddScoped<ServicioDePlanes>();
        servicios.AddScoped<ServicioDeMembresias>();
        servicios.AddScoped<ServicioDeFormasDePago>();
        servicios.AddScoped<ServicioDePagos>();
        servicios.AddScoped<ServicioDeAsistencias>();
        servicios.AddScoped<ServicioDeHorarios>();
        servicios.AddScoped<ServicioDeMediciones>();
        servicios.AddScoped<ServicioDeCatalogos>();
        servicios.AddScoped<ServicioDeEjercicios>();
        servicios.AddScoped<ServicioDeAlimentos>();
        servicios.AddScoped<ServicioDePlantillasDeRutina>();
        servicios.AddScoped<ServicioDeRutinasDeSocio>();
        servicios.AddScoped<ServicioDeDietas>();
        servicios.AddScoped<ServicioDeSitioWeb>();
        servicios.AddScoped<ServicioDeForo>();

        // Toma todos los AbstractValidator<T> de este ensamblado de una sola vez.
        servicios.AddValidatorsFromAssembly(typeof(InyeccionDeDependencias).Assembly);

        return servicios;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Infrastructure.Persistence;
using VirtualGymAssistant.Infrastructure.Seguridad;
using VirtualGymAssistant.Infrastructure.Servicios;

namespace VirtualGymAssistant.Infrastructure;

/// <summary>
/// Registra en el contenedor todo lo que aporta la capa de infraestructura:
/// el acceso a datos y las implementaciones concretas de los puertos que definió
/// la capa Application.
/// </summary>
public static class InyeccionDeDependencias
{
    public static IServiceCollection AgregarInfraestructura(
        this IServiceCollection servicios, IConfiguration configuracion)
    {
        // --- Base de datos ---
        var cadenaDeConexion = configuracion.GetConnectionString("VirtualGymAssistant")
            ?? throw new InvalidOperationException(
                "Falta la cadena de conexión 'VirtualGymAssistant' en appsettings.json.");

        servicios.AddDbContext<VirtualGymDbContext>(opciones =>
            opciones.UseSqlServer(cadenaDeConexion, sql =>
            {
                // Reintenta ante caídas momentáneas de red o de la instancia de SQL Server.
                sql.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
            }));

        // La capa Application depende de la interfaz, no del contexto concreto.
        servicios.AddScoped<IVirtualGymDbContext>(sp => sp.GetRequiredService<VirtualGymDbContext>());

        // --- Seguridad ---
        servicios.Configure<OpcionesDeJwt>(configuracion.GetSection(OpcionesDeJwt.Seccion));
        servicios.AddScoped<IServicioDeTokens, ServicioDeTokensJwt>();
        servicios.AddSingleton<IHasheadorDeClaves, HasheadorDeClavesBCrypt>();

        // --- Servicios de apoyo ---
        servicios.AddSingleton<IProveedorDeFechaHora, ProveedorDeFechaHora>();
        servicios.AddSingleton<IAlmacenDeArchivos, AlmacenDeArchivosLocal>();

        // Con un SMTP configurado (Smtp:Host + Smtp:Usuario) se manda el mail de verdad;
        // si no, se sigue usando el que sólo loguea, para no exigirle credenciales de
        // correo a cada desarrollador que quiera correr el proyecto.
        var haySmtpConfigurado =
            !string.IsNullOrWhiteSpace(configuracion["Smtp:Host"]) &&
            !string.IsNullOrWhiteSpace(configuracion["Smtp:Usuario"]);

        if (haySmtpConfigurado)
            servicios.AddScoped<IServicioDeNotificaciones, ServicioDeNotificacionesPorEmail>();
        else
            servicios.AddScoped<IServicioDeNotificaciones, ServicioDeNotificacionesPorLog>();

        return servicios;
    }
}

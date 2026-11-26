using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using VirtualGymAssistant.API.Servicios;

namespace VirtualGymAssistant.API.Configuracion;

/// <summary>
/// Arma la documentación interactiva de la API (Swagger / OpenAPI).
/// </summary>
public static class ConfiguracionDeSwagger
{
    /// <summary>Identificador del esquema de seguridad dentro del documento OpenAPI.</summary>
    private const string EsquemaBearer = "Bearer";

    public static IServiceCollection AgregarSwagger(this IServiceCollection servicios)
    {
        servicios.AddEndpointsApiExplorer();

        servicios.AddSwaggerGen(opciones =>
        {
            opciones.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "VirtualGymAssistant API",
                Version = "v1",
                Description =
                    "API de gestión integral para gimnasios multi-sucursal.\n\n" +
                    "**Cómo probar los endpoints protegidos:**\n\n" +
                    "1. Llamá a `POST /api/auth/login` con un usuario y copiá el `token` de la respuesta.\n" +
                    "2. Tocá el botón **Authorize** de arriba y pegá el token.\n" +
                    "3. Si el usuario opera en más de una sucursal, completá la cabecera " +
                    "`X-Gimnasio-Id` para elegir sobre cuál trabajar. Si la dejás vacía, " +
                    "se usa la primera sucursal disponible.\n\n" +
                    "**Usuarios de prueba** (contraseña `123456`): `duenio@vga.com`, " +
                    "`admin@vga.com`, `recepcion@vga.com`, `entrenador@vga.com`, " +
                    "`nutricionista@vga.com`, `socio@vga.com`."
            });

            // Botón "Authorize" para pegar el token JWT.
            opciones.AddSecurityDefinition(EsquemaBearer, new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Pegá solamente el token (Swagger le agrega el prefijo \"Bearer\")."
            });

            // Aplica ese esquema a todas las operaciones documentadas.
            // La referencia necesita el documento que la aloja, por eso se arma con una
            // función en lugar de con un objeto ya construido.
            opciones.AddSecurityRequirement(documento => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(EsquemaBearer, documento, externalResource: null)] = []
            });

            // Agrega la cabecera de sucursal activa a todos los endpoints.
            opciones.OperationFilter<CabeceraDeSucursalFilter>();

            // Incorpora los comentarios XML del código a la documentación.
            var archivoXml = $"{typeof(ConfiguracionDeSwagger).Assembly.GetName().Name}.xml";
            var rutaXml = Path.Combine(AppContext.BaseDirectory, archivoXml);

            if (File.Exists(rutaXml))
                opciones.IncludeXmlComments(rutaXml, includeControllerXmlComments: true);
        });

        return servicios;
    }
}

/// <summary>
/// Agrega la cabecera <c>X-Gimnasio-Id</c> como parámetro opcional en Swagger, para poder
/// cambiar de sucursal desde la propia documentación sin herramientas externas.
/// </summary>
public class CabeceraDeSucursalFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operacion, OperationFilterContext contexto)
    {
        operacion.Parameters ??= [];

        operacion.Parameters.Add(new OpenApiParameter
        {
            Name = UsuarioActual.CabeceraDeGimnasio,
            In = ParameterLocation.Header,
            Required = false,
            Description = "Id de la sucursal sobre la que se quiere trabajar. " +
                          "Si se deja vacío se usa la primera sucursal del usuario.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" }
        });
    }
}

using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using VirtualGymAssistant.API.Configuracion;
using VirtualGymAssistant.API.Filtros;
using VirtualGymAssistant.API.Middleware;
using VirtualGymAssistant.API.Servicios;
using VirtualGymAssistant.Application;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Infrastructure;
using VirtualGymAssistant.Infrastructure.Seguridad;

var builder = WebApplication.CreateBuilder(args);

// =====================================================================
//  SERVICIOS
// =====================================================================

// --- Capas de la aplicación ---
// Cada capa registra lo suyo: Application los casos de uso, Infrastructure el acceso
// a datos y las implementaciones concretas. La API sólo las compone.
builder.Services.AgregarAplicacion();
builder.Services.AgregarInfraestructura(builder.Configuration);

// --- Puentes entre el mundo HTTP y la capa Application ---
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActual, UsuarioActual>();
builder.Services.AddSingleton<IEntornoDeEjecucion, EntornoDeEjecucion>();

// --- Controladores ---
builder.Services
    .AddControllers(opciones =>
    {
        // Valida automáticamente todo request que tenga un validador de FluentValidation.
        opciones.Filters.Add<FiltroDeValidacion>();
    })
    .AddJsonOptions(opciones =>
    {
        // El front recibe las propiedades en camelCase.
        opciones.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;

        // Los enums viajan como texto y no como número: es más legible y no se rompe
        // si mañana se agrega un valor en el medio.
        opciones.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

        // Las entidades tienen referencias circulares (Socio -> Gimnasio -> Socios).
        // Se ignoran los ciclos por las dudas, aunque la API siempre devuelve DTOs planos.
        opciones.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// --- Documentación ---
builder.Services.AgregarSwagger();

// --- Autenticación con JWT ---
var opcionesDeJwt = builder.Configuration.GetSection(OpcionesDeJwt.Seccion).Get<OpcionesDeJwt>()
    ?? throw new InvalidOperationException("Falta la sección 'Jwt' en appsettings.json.");

if (opcionesDeJwt.ClaveSecreta.Length < 32)
    throw new InvalidOperationException(
        "Jwt:ClaveSecreta debe tener al menos 32 caracteres para poder firmar con HMAC-SHA256.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = opcionesDeJwt.Emisor,

            ValidateAudience = true,
            ValidAudience = opcionesDeJwt.Audiencia,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opcionesDeJwt.ClaveSecreta)),

            ValidateLifetime = true,

            // Sin esto .NET tolera 5 minutos de diferencia y un token vencido seguiría
            // siendo aceptado un rato más.
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// --- CORS para el front ---
const string PoliticaDeCors = "FrontDeVirtualGym";
var origenesPermitidos = builder.Configuration
    .GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [];

builder.Services.AddCors(opciones =>
{
    opciones.AddPolicy(PoliticaDeCors, politica =>
    {
        if (origenesPermitidos.Length > 0)
        {
            politica.WithOrigins(origenesPermitidos)
                    .AllowAnyHeader()
                    .AllowAnyMethod()

                    // Permite que el front lea la cabecera de paginación si se agrega más adelante.
                    .WithExposedHeaders("X-Total-Registros");
        }
        else
        {
            // Sin orígenes configurados se deja pasar todo: sirve sólo mientras se desarrolla.
            politica.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
    });
});

var app = builder.Build();

// =====================================================================
//  PIPELINE HTTP
//  El orden importa: cada middleware envuelve a los que siguen.
// =====================================================================

// Va primero para poder capturar cualquier error que ocurra más abajo.
app.UseMiddleware<ManejadorGlobalDeExcepciones>();

// Swagger queda habilitado también fuera de desarrollo: es la documentación de la API
// que se entrega con el trabajo y la forma más cómoda de probarla.
app.UseSwagger();
app.UseSwaggerUI(opciones =>
{
    opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "VirtualGymAssistant API v1");

    // Deja Swagger en la raíz: entrando a https://localhost:puerto/ ya se ve la documentación.
    opciones.RoutePrefix = string.Empty;
    opciones.DocumentTitle = "VirtualGymAssistant API";
});

app.UseHttpsRedirection();

// Sirve las imágenes del carrusel y los adjuntos del foro desde /uploads.
var carpetaDeArchivos = Path.Combine(
    AppContext.BaseDirectory,
    app.Configuration["Archivos:Carpeta"] ?? "Uploads");

Directory.CreateDirectory(carpetaDeArchivos);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(carpetaDeArchivos),
    RequestPath = "/uploads"
});

app.UseCors(PoliticaDeCors);

// Primero se identifica al usuario (Authentication) y después se verifica si puede
// hacer lo que pide (Authorization). Invertir el orden rompe la autenticación.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Endpoint simple para verificar que la API está levantada.
app.MapGet("/api/health", () => Results.Ok(new
{
    estado = "ok",
    fecha = DateTime.Now,
    entorno = app.Environment.EnvironmentName
}))
.WithName("Health")
.WithTags("Sistema")
.AllowAnonymous();

app.Run();

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VirtualGymAssistant.Domain.Exceptions;

namespace VirtualGymAssistant.API.Middleware;

/// <summary>
/// Middleware que centraliza el manejo de errores de toda la API.
///
/// Gracias a esto los servicios de negocio simplemente lanzan la excepción que
/// corresponde (por ejemplo <see cref="NoEncontradoException"/>) y no necesitan saber
/// nada de códigos HTTP: la traducción ocurre en un único lugar.
///
/// Todas las respuestas de error usan el formato ProblemDetails (RFC 7807), que es el
/// estándar de ASP.NET Core y el que espera Swagger.
/// </summary>
public class ManejadorGlobalDeExcepciones
{
    private readonly RequestDelegate _siguiente;
    private readonly ILogger<ManejadorGlobalDeExcepciones> _log;
    private readonly IWebHostEnvironment _entorno;

    public ManejadorGlobalDeExcepciones(
        RequestDelegate siguiente,
        ILogger<ManejadorGlobalDeExcepciones> log,
        IWebHostEnvironment entorno)
    {
        _siguiente = siguiente;
        _log = log;
        _entorno = entorno;
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await _siguiente(contexto);
        }
        catch (Exception ex)
        {
            await ResponderAsync(contexto, ex);
        }
    }

    private async Task ResponderAsync(HttpContext contexto, Exception ex)
    {
        // Si ya se empezó a escribir la respuesta no se puede cambiar el código de estado;
        // lo único que queda es registrar el error.
        if (contexto.Response.HasStarted)
        {
            _log.LogError(ex, "Error después de haber empezado a enviar la respuesta.");
            return;
        }

        var problema = Traducir(ex, contexto);

        // Las excepciones de negocio son parte del funcionamiento normal (un mail repetido,
        // un permiso faltante), así que se registran como advertencia. Sólo lo inesperado
        // se registra como error.
        if (ex is ExcepcionDeDominio)
            _log.LogWarning("{Tipo}: {Mensaje}", ex.GetType().Name, ex.Message);
        else
            _log.LogError(ex, "Error no controlado al procesar {Metodo} {Ruta}.",
                contexto.Request.Method, contexto.Request.Path);

        problema.Instance = contexto.Request.Path;
        problema.Extensions["traceId"] = contexto.TraceIdentifier;

        contexto.Response.StatusCode = problema.Status ?? StatusCodes.Status500InternalServerError;
        contexto.Response.ContentType = "application/problem+json";

        await contexto.Response.WriteAsync(JsonSerializer.Serialize(problema, OpcionesJson));
    }

    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>Convierte una excepción en la respuesta HTTP que le corresponde.</summary>
    private ProblemDetails Traducir(Exception ex, HttpContext contexto) => ex switch
    {
        // --- Errores de negocio esperables ---
        ValidacionException validacion => new ValidationProblemDetails(
            validacion.Errores.ToDictionary(e => e.Key, e => e.Value))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Hay datos inválidos en el pedido.",
            Detail = validacion.Message
        },

        ReglaDeNegocioException regla => Crear(
            StatusCodes.Status400BadRequest, "No se puede completar la operación.", regla.Message),

        NoAutenticadoException noAutenticado => Crear(
            StatusCodes.Status401Unauthorized, "No autenticado.", noAutenticado.Message),

        AccesoDenegadoException denegado => Crear(
            StatusCodes.Status403Forbidden, "Acceso denegado.", denegado.Message),

        // Mismo 403 que AccesoDenegadoException, pero con un código aparte en las
        // extensions: el front lo necesita para distinguir "todavía no verificaste tu
        // email" (ofrece reenviar/confirmar el código) de cualquier otro 403.
        EmailNoVerificadoException noVerificado => new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Acceso denegado.",
            Detail = noVerificado.Message,
            Extensions = { ["codigo"] = "EMAIL_NO_VERIFICADO" }
        },

        NoEncontradoException noEncontrado => Crear(
            StatusCodes.Status404NotFound, "Recurso no encontrado.", noEncontrado.Message),

        ConflictoException conflicto => Crear(
            StatusCodes.Status409Conflict, "Conflicto con un registro existente.", conflicto.Message),

        // --- Errores de infraestructura que sí sabemos interpretar ---
        DbUpdateException actualizacion when EsViolacionDeUnicidad(actualizacion) => Crear(
            StatusCodes.Status409Conflict,
            "Conflicto con un registro existente.",
            "Ya existe un registro con esos datos. Revisá los campos que deben ser únicos."),

        DbUpdateConcurrencyException => Crear(
            StatusCodes.Status409Conflict,
            "El registro fue modificado por otro usuario.",
            "Volvé a cargar los datos e intentá de nuevo."),

        OperationCanceledException when contexto.RequestAborted.IsCancellationRequested => Crear(
            StatusCodesExtra.ClientClosedRequest,
            "Pedido cancelado.",
            "El cliente canceló la operación antes de que terminara."),

        // --- Cualquier otra cosa: error no previsto ---
        _ => Crear(
            StatusCodes.Status500InternalServerError,
            "Error interno del servidor.",

            // En desarrollo se muestra el detalle para poder depurar; en producción se
            // oculta para no filtrar información sobre el funcionamiento interno.
            _entorno.IsDevelopment()
                ? ex.ToString()
                : "Ocurrió un error inesperado. Si el problema persiste, contactá al administrador.")
    };

    private static ProblemDetails Crear(int estado, string titulo, string detalle) => new()
    {
        Status = estado,
        Title = titulo,
        Detail = detalle
    };

    /// <summary>
    /// Detecta si el error de base es una violación de índice único o de clave primaria.
    /// Son los números 2601 y 2627 de SQL Server.
    /// </summary>
    private static bool EsViolacionDeUnicidad(DbUpdateException ex) =>
        ex.InnerException is SqlException sql && sql.Number is 2601 or 2627;
}

/// <summary>Códigos de estado que ASP.NET Core no define como constante.</summary>
internal static class StatusCodesExtra
{
    /// <summary>499: el cliente cerró la conexión antes de recibir la respuesta (convención de nginx).</summary>
    public const int ClientClosedRequest = 499;
}

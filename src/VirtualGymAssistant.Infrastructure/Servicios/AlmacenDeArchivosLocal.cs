using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VirtualGymAssistant.Application.Common.Interfaces;
using VirtualGymAssistant.Domain.Exceptions;

namespace VirtualGymAssistant.Infrastructure.Servicios;

/// <summary>
/// Guarda los archivos subidos en una carpeta del servidor.
///
/// Para el alcance del trabajo alcanza con el disco local; si en el futuro se pasa a un
/// almacenamiento en la nube, basta con escribir otra implementación de
/// <see cref="IAlmacenDeArchivos"/> sin tocar la lógica de negocio.
/// </summary>
public class AlmacenDeArchivosLocal : IAlmacenDeArchivos
{
    /// <summary>Tamaño máximo aceptado por archivo (8 MB).</summary>
    private const long TamanioMaximoEnBytes = 8 * 1024 * 1024;

    /// <summary>
    /// Extensiones permitidas. Es una lista blanca a propósito: aceptar cualquier
    /// extensión permitiría subir ejecutables o scripts al servidor.
    /// </summary>
    private static readonly string[] ExtensionesPermitidas =
        [".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf"];

    private readonly string _carpeta;
    private readonly ILogger<AlmacenDeArchivosLocal> _log;

    public AlmacenDeArchivosLocal(IConfiguration configuracion, ILogger<AlmacenDeArchivosLocal> log)
    {
        _log = log;
        _carpeta = Path.Combine(
            AppContext.BaseDirectory,
            configuracion["Archivos:Carpeta"] ?? "Uploads");

        Directory.CreateDirectory(_carpeta);
    }

    public async Task<string> GuardarAsync(Stream contenido, string nombreOriginal, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(nombreOriginal).ToLowerInvariant();

        if (!ExtensionesPermitidas.Contains(extension))
            throw new ReglaDeNegocioException(
                $"El tipo de archivo \"{extension}\" no está permitido. " +
                $"Aceptamos: {string.Join(", ", ExtensionesPermitidas)}.");

        // El nombre se genera del lado del servidor: nunca se usa el que mandó el usuario,
        // porque podría contener rutas ("../../") y escapar de la carpeta de subidas.
        var nombreFinal = $"{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}{extension}";
        var destino = Path.Combine(_carpeta, nombreFinal);

        // Se copia con un límite de bytes en vez de confiar en Stream.Length: si el stream
        // no es "seekable" (depende de cómo llegue el multipart), Length no es confiable y
        // el límite de tamaño quedaba sin aplicarse.
        var buffer = new byte[81920];
        long totalCopiado = 0;

        try
        {
            await using var archivo = File.Create(destino);

            int leidos;
            while ((leidos = await contenido.ReadAsync(buffer, ct)) > 0)
            {
                totalCopiado += leidos;

                if (totalCopiado > TamanioMaximoEnBytes)
                    throw new ReglaDeNegocioException("El archivo supera el tamaño máximo de 8 MB.");

                await archivo.WriteAsync(buffer.AsMemory(0, leidos), ct);
            }
        }
        catch
        {
            if (File.Exists(destino)) File.Delete(destino);
            throw;
        }

        return nombreFinal;
    }

    public void Eliminar(string? nombreArchivo)
    {
        if (string.IsNullOrWhiteSpace(nombreArchivo)) return;

        // Se vuelve a tomar sólo el nombre por si llegara una ruta completa.
        var destino = Path.Combine(_carpeta, Path.GetFileName(nombreArchivo));

        try
        {
            if (File.Exists(destino)) File.Delete(destino);
        }
        catch (IOException ex)
        {
            // Que no se pueda borrar un archivo viejo no debe hacer fallar la operación
            // de negocio: queda registrado para poder limpiarlo después.
            _log.LogWarning(ex, "No se pudo eliminar el archivo {Archivo}.", destino);
        }
    }
}

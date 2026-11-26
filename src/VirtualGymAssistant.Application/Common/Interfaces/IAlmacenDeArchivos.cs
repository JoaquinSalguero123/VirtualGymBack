namespace VirtualGymAssistant.Application.Common.Interfaces;

/// <summary>
/// Guarda los archivos que suben los usuarios (imágenes del carrusel, adjuntos del foro).
/// La capa Application sólo maneja el nombre resultante; dónde termina el archivo es
/// una decisión de infraestructura.
/// </summary>
public interface IAlmacenDeArchivos
{
    /// <summary>
    /// Persiste el contenido y devuelve el nombre único con el que quedó guardado.
    /// </summary>
    /// <param name="contenido">Flujo con los bytes del archivo.</param>
    /// <param name="nombreOriginal">Nombre con el que lo subió el usuario; se usa para conservar la extensión.</param>
    Task<string> GuardarAsync(Stream contenido, string nombreOriginal, CancellationToken ct = default);

    /// <summary>Borra un archivo previamente guardado. No falla si el archivo ya no existe.</summary>
    void Eliminar(string? nombreArchivo);
}

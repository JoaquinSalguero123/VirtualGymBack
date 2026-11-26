using VirtualGymAssistant.Application.Common.Interfaces;

namespace VirtualGymAssistant.Infrastructure.Seguridad;

/// <summary>
/// Hashea contraseñas con BCrypt.
///
/// Se eligió BCrypt (y no el hasher propio de ASP.NET Identity) porque la base ya tiene
/// cargados hashes en formato BCrypt, generados por la versión anterior del backend:
/// de esta forma los usuarios existentes siguen pudiendo iniciar sesión sin migrar nada.
/// </summary>
public class HasheadorDeClavesBCrypt : IHasheadorDeClaves
{
    /// <summary>
    /// Factor de costo. Cada unidad duplica el tiempo de cálculo; 11 es un punto
    /// razonable entre seguridad y velocidad de login.
    /// </summary>
    private const int FactorDeTrabajo = 11;

    public string Hashear(string claveEnTextoPlano) =>
        BCrypt.Net.BCrypt.HashPassword(claveEnTextoPlano, FactorDeTrabajo);

    public bool Verificar(string claveEnTextoPlano, string hashAlmacenado)
    {
        // Un hash corrupto o con un formato desconocido hace que BCrypt lance excepción.
        // Se traduce a "no coincide" para que el login responda con un 401 normal y no
        // con un error 500 que además revelaría el estado interno del registro.
        try
        {
            return BCrypt.Net.BCrypt.Verify(claveEnTextoPlano, hashAlmacenado);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}

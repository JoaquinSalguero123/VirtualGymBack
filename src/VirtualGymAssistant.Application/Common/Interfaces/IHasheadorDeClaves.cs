namespace VirtualGymAssistant.Application.Common.Interfaces;

/// <summary>
/// Abstracción del algoritmo de hash de contraseñas.
/// La implementación usa BCrypt, compatible con los hashes que ya tiene cargados la base.
/// </summary>
public interface IHasheadorDeClaves
{
    /// <summary>Devuelve el hash a guardar en la base.</summary>
    string Hashear(string claveEnTextoPlano);

    /// <summary>Compara una clave en texto plano contra un hash almacenado.</summary>
    bool Verificar(string claveEnTextoPlano, string hashAlmacenado);
}

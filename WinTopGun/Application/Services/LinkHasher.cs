using System.Security.Cryptography;
using System.Text;

namespace WinTopGun.Application.Services;

/// <summary>
/// Genera el código hash de identificación de cada enlace de partido.
/// Se usa SHA-256 sobre la URL: determinístico, estable entre ejecuciones
/// y con probabilidad de colisión despreciable.
/// </summary>
public static class LinkHasher
{
    /// <summary>
    /// Calcula el hash SHA-256 ( hexadecimal, 64 caracteres) de un enlace.
    /// </summary>
    /// <param name="url">Enlace a hashear.</param>
    /// <returns>Hash hexadecimal en mayúsculas.</returns>
    public static string ComputeHash(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(url));
        return Convert.ToHexString(bytes);
    }
}

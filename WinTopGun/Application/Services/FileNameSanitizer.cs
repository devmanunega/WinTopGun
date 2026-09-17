using System.Text;

namespace WinTopGun.Application.Services;

/// <summary>
/// Sanitiza fragmentos de texto usados como nombres de archivo en Windows,
/// reemplazando caracteres inválidos por guion bajo.
/// </summary>
public static class FileNameSanitizer
{
    private static readonly string FallbackName = "sin_nombre";

    /// <summary>
    /// Reemplaza los caracteres inválidos para nombres de archivo.
    /// </summary>
    /// <param name="value">Texto a sanitizar.</param>
    /// <returns>Texto seguro para usar en un nombre de archivo.</returns>
    public static string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return FallbackName;
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);

        foreach (var c in value)
        {
            builder.Append(Array.IndexOf(invalidChars, c) >= 0 ? '_' : c);
        }

        string result = builder.ToString().Trim();
        return result.Length == 0 ? FallbackName : result;
    }
}

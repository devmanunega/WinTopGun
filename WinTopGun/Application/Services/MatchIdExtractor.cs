using System.Security.Cryptography;
using System.Text;

namespace WinTopGun.Application.Services;

/// <summary>
/// Extrae el identificador único de un partido a partir de su URL.
/// En FBref, el segmento posterior a <c>/matches/</c> (p. ej. <c>d5932b03</c> en
/// <c>/en/matches/d5932b03/Arsenal-Manchester-City-...</c>) es único por partido:
/// es una clave de deduplicación más confiable que la URL completa, cuyo slug
/// podría cambiar (ver Ideas/Partidos.md, observaciones sobre el HTML).
/// Si la URL no contiene un ID de partido, se usa un hash estable del URL como
/// identificador alternativo.
/// </summary>
public static class MatchIdExtractor
{
    /// <summary>
    /// Extrae el ID del partido de la URL indicada.
    /// </summary>
    /// <param name="url">URL absoluta del reporte del partido.</param>
    /// <returns>El ID del partido, o un hash estable del URL si no se encuentra el patrón.</returns>
    public static string ExtractId(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return LinkHasher.ComputeHash(url);
        }

        string[] segmentos = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < segmentos.Length - 1; i++)
        {
            if (string.Equals(segmentos[i], "matches", StringComparison.OrdinalIgnoreCase))
            {
                return segmentos[i + 1];
            }
        }

        // La URL no sigue el patrón /matches/{id}/...: hash estable como identificador.
        return LinkHasher.ComputeHash(url);
    }
}

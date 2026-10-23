using System.Text.RegularExpressions;
using WinTopGun.Application.Interfaces;
using WinTopGun.Domain.Models;

namespace WinTopGun.Application.Services;

/// <summary>
/// Implementación de la fase de depuración: para cada subdirectorio (grupo de
/// patrón) compara el encabezado (primera línea no vacía, con espacios
/// normalizados) de todos los archivos y reporta los que no encajan con el
/// encabezado mayoritario, además de archivos vacíos o ilegibles.
/// </summary>
public sealed partial class DepuracionService : IDepuracionService
{
    [GeneratedRegex(@"\s+")]
    private static partial Regex EspaciosMultiples();

    /// <summary>Extensiones de archivo consideradas por la depuración.</summary>
    public static readonly string[] ExtensionesPermitidas = ClasificacionService.ExtensionesPermitidas;

    /// <inheritdoc />
    public DepuracionInforme Depurar(string directorio)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directorio);

        if (!Directory.Exists(directorio))
        {
            throw new DirectoryNotFoundException($"El directorio no existe: {directorio}");
        }

        var informe = new DepuracionInforme { Directorio = directorio };

        foreach (string subdirectorio in Directory.EnumerateDirectories(directorio)
                     .OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
        {
            informe.Grupos.Add(DepurarGrupo(subdirectorio));
        }

        return informe;
    }

    /// <summary>Depura los archivos de un grupo (subdirectorio).</summary>
    private static GrupoDepuracion DepurarGrupo(string subdirectorio)
    {
        var grupo = new GrupoDepuracion
        {
            Patron = Path.GetFileName(subdirectorio),
        };

        List<string> rutas = [.. Directory.EnumerateFiles(subdirectorio, "*", SearchOption.TopDirectoryOnly)
            .Where(r => ExtensionesPermitidas.Contains(Path.GetExtension(r), StringComparer.OrdinalIgnoreCase))
            .OrderBy(r => r, StringComparer.OrdinalIgnoreCase)];

        grupo.TotalArchivos = rutas.Count;

        if (rutas.Count == 0)
        {
            grupo.Incidencias.Add("El grupo no contiene archivos a depurar.");
            return grupo;
        }

        // Encabezado de cada archivo: primera línea no vacía, espacios normalizados.
        var encabezados = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (string ruta in rutas)
        {
            string nombre = Path.GetFileName(ruta);

            string? encabezado;
            try
            {
                encabezado = LeerEncabezado(ruta);
            }
            catch (Exception ex)
            {
                grupo.Incidencias.Add($"No se pudo leer '{nombre}': {ex.Message}");
                continue;
            }

            if (encabezado is null)
            {
                grupo.Incidencias.Add($"'{nombre}' está vacío.");
                continue;
            }

            encabezados.Add(nombre, encabezado);
        }

        if (encabezados.Count == 0)
        {
            return grupo;
        }

        // Encabezado mayoritario (desempate: orden alfabético, para determinismo).
        string esperado = encabezados.Values
            .GroupBy(e => e, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .First().Key;

        foreach (var (nombre, encabezado) in encabezados)
        {
            if (!string.Equals(encabezado, esperado, StringComparison.OrdinalIgnoreCase))
            {
                grupo.Incidencias.Add(
                    $"'{nombre}' tiene un encabezado distinto: \"{encabezado}\" (esperado: \"{esperado}\").");
            }
        }

        foreach (var linea in encabezados.Values
                     .GroupBy(e => e, StringComparer.OrdinalIgnoreCase)
                     .OrderByDescending(g => g.Count())
                     .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            grupo.Encabezados.Add($"{linea.Count()} archivo(s): \"{linea.Key}\"");
        }

        return grupo;
    }

    /// <summary>
    /// Devuelve el encabezado del archivo: primera línea no vacía con espacios
    /// múltiples colapsados, o <c>null</c> si el archivo está vacío.
    /// </summary>
    private static string? LeerEncabezado(string ruta)
    {
        foreach (string linea in File.ReadLines(ruta))
        {
            string recortada = EspaciosMultiples().Replace(linea.Trim(), " ");
            if (recortada.Length > 0)
            {
                return recortada;
            }
        }

        return null;
    }
}

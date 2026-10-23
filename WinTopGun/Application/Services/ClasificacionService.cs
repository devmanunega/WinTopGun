using System.Text;
using WinTopGun.Application.Interfaces;
using WinTopGun.Domain.Models;

namespace WinTopGun.Application.Services;

/// <summary>
/// Implementación del algoritmo de clasificación "patrón anclado en la palabra
/// tabla" (Ideas/Clasificacion.md): extrae el patrón de cada nombre de archivo a
/// partir del ancla <c>tabla</c>, agrupa por igualdad exacta de la cadena y mueve
/// cada grupo a un subdirectorio con el nombre del patrón.
/// </summary>
public sealed class ClasificacionService : IClasificacionService
{
    /// <summary>Ancla que debe contener el nombre para clasificar el archivo.</summary>
    public const string Ancla = "tabla";

    /// <summary>Palabras de ruido eliminadas del patrón (lista editable).</summary>
    public static readonly string[] PalabrasRuido = ["copy", "final", "backup", "old"];

    /// <summary>Extensiones de archivo consideradas por el inventario.</summary>
    public static readonly string[] ExtensionesPermitidas = [".txt", ".csv", ".json"];

    /// <summary>Sufijo que identifica los archivos consolidados (excluidos del inventario).</summary>
    public const string SufijoConsolidado = "_Consolidado";

    /// <inheritdoc />
    public ClasificacionPlan GenerarPlan(string directorio)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directorio);

        if (!Directory.Exists(directorio))
        {
            throw new DirectoryNotFoundException($"El directorio no existe: {directorio}");
        }

        var plan = new ClasificacionPlan { DirectorioOrigen = directorio };
        var grupos = new Dictionary<string, GrupoClasificacion>(StringComparer.Ordinal);

        foreach (string ruta in Directory.EnumerateFiles(directorio, "*", SearchOption.TopDirectoryOnly))
        {
            string nombre = Path.GetFileName(ruta);

            if (!EsArchivoInventariable(nombre))
            {
                plan.Ignorados.Add(nombre);
                continue;
            }

            string? patron = ExtraerPatron(nombre);

            if (patron is null)
            {
                plan.SinClasificar.Add(nombre);
                continue;
            }

            if (!grupos.TryGetValue(patron, out var grupo))
            {
                grupo = new GrupoClasificacion { Patron = patron };
                grupos.Add(patron, grupo);
            }

            grupo.Archivos.Add(nombre);
        }

        plan.Grupos.AddRange(grupos.Values.OrderBy(g => g.Patron, StringComparer.Ordinal));
        return plan;
    }

    /// <inheritdoc />
    public ClasificacionResultado EjecutarPlan(ClasificacionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(plan.DirectorioOrigen);

        var resultado = new ClasificacionResultado();

        foreach (var grupo in plan.Grupos)
        {
            string nombreDirectorio = FileNameSanitizer.Sanitize(grupo.Patron);
            string destino = Path.Combine(plan.DirectorioOrigen, nombreDirectorio);
            grupo.DirectorioExistente = Directory.Exists(destino);

            MoverGrupo(grupo, plan.DirectorioOrigen, destino, resultado);
        }

        return resultado;
    }

    /// <summary>
    /// Indica si el archivo entra en el inventario: extensión permitida, no es
    /// consolidado ni temporal.
    /// </summary>
    private static bool EsArchivoInventariable(string nombre)
    {
        if (!ExtensionesPermitidas.Contains(Path.GetExtension(nombre), StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        string nombreSinExtension = Path.GetFileNameWithoutExtension(nombre);
        if (nombreSinExtension.EndsWith(SufijoConsolidado, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !nombre.StartsWith("~$", StringComparison.Ordinal);
    }

    /// <summary>
    /// Extrae el patrón del nombre: localiza el ancla "tabla" (palabra completa,
    /// primera aparición seguida de al menos una palabra conservada), descarta lo
    /// anterior y construye la secuencia con los tokens posteriores válidos.
    /// Devuelve <c>null</c> si no hay ancla o ninguna palabra válida después.
    /// </summary>
    public static string? ExtraerPatron(string nombreArchivo)
    {
        string[] tokens = Tokenizar(nombreArchivo);

        for (int i = 0; i < tokens.Length; i++)
        {
            if (tokens[i] != Ancla)
            {
                continue;
            }

            var conservados = new List<string>();

            for (int j = i + 1; j < tokens.Length; j++)
            {
                if (tokens[j] == Ancla)
                {
                    // Otra aparición del ancla: esta no tiene palabras válidas
                    // propias; se usa la nueva aparición como ancla.
                    break;
                }

                if (EsTokenConservado(tokens[j]))
                {
                    conservados.Add(tokens[j]);
                }
            }

            if (conservados.Count == 0)
            {
                continue;
            }

            StringBuilder patron = new(Ancla);
            foreach (string token in conservados)
            {
                patron.Append('_').Append(token);
            }

            return patron.ToString();
        }

        return null;
    }

    /// <summary>
    /// Separa el nombre sin extensión por <c>_</c>, <c>-</c>, espacios y puntos,
    /// en minúsculas y sin tildes.
    /// </summary>
    private static string[] Tokenizar(string nombreArchivo)
    {
        string nombre = Path.GetFileNameWithoutExtension(nombreArchivo);
        string normalizado = QuitarTildes(nombre).ToLowerInvariant();
        return normalizado.Split(['_', '-', ' ', '.'], StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Determina si el token se conserva en el patrón: solo las palabras (letras).
    /// Los alfanuméricos mezclados, los numéricos puros y las palabras de ruido
    /// se eliminan; se saltan sin cortar la secuencia.
    /// </summary>
    private static bool EsTokenConservado(string token)
    {
        if (token.All(char.IsDigit))
        {
            return false;
        }

        if (PalabrasRuido.Contains(token, StringComparer.Ordinal))
        {
            return false;
        }

        return token.All(char.IsLetter);
    }

    /// <summary>Elimina las tildes del texto (descomposición Unicode + filtrado).</summary>
    private static string QuitarTildes(string texto)
    {
        string formaD = texto.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(formaD.Length);

        foreach (char c in formaD)
        {
            if (char.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>Mueve los archivos del grupo al subdirectorio destino.</summary>
    private static void MoverGrupo(
        GrupoClasificacion grupo,
        string directorioOrigen,
        string destino,
        ClasificacionResultado resultado)
    {
        Directory.CreateDirectory(destino);

        if (!grupo.DirectorioExistente)
        {
            resultado.DirectoriosCreados++;
        }

        foreach (string nombre in grupo.Archivos)
        {
            string origen = Path.Combine(directorioOrigen, nombre);
            string rutaDestino = Path.Combine(destino, nombre);

            if (File.Exists(rutaDestino))
            {
                resultado.Errors.Add($"No se movió '{nombre}': ya existe un archivo con ese nombre en '{Path.GetFileName(destino)}'.");
                continue;
            }

            try
            {
                File.Move(origen, rutaDestino);
                resultado.ArchivosMovidos++;
            }
            catch (Exception ex)
            {
                resultado.Errors.Add($"Error al mover '{nombre}': {ex.Message}");
            }
        }
    }
}

using System.Text;
using WinTopGun.Application.Interfaces;
using WinTopGun.Application.Services;

namespace WinTopGun.Infrastructure.Persistence;

/// <summary>
/// Exporta tablas como archivos de texto plano en UTF-8, generando nombres
/// de archivo seguros y creando el directorio destino si no existe.
/// </summary>
public sealed class TextFileTableExporter : ITableExporter
{
    /// <inheritdoc />
    public async Task<string> ExportTableAsync(
        string outputDirectory,
        string pageSegment,
        string tableId,
        string content,
        CancellationToken cancellationToken = default)
    {
        string sanitizedSegment = FileNameSanitizer.Sanitize(pageSegment);
        string sanitizedTableId = FileNameSanitizer.Sanitize(tableId);
        string fileName = $"{sanitizedSegment}_tabla_{sanitizedTableId}.txt";
        string fullPath = Path.Combine(outputDirectory, fileName);

        Directory.CreateDirectory(outputDirectory);
        await File.WriteAllTextAsync(fullPath, content, Encoding.UTF8, cancellationToken);

        return fullPath;
    }
}

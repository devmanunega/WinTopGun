namespace WinTopGun.Application.Interfaces;

/// <summary>
/// Persiste el contenido de una tabla extraída. Implementaciones concretas:
/// texto plano, CSV, JSON, base de datos, etc.
/// </summary>
public interface ITableExporter
{
    /// <summary>
    /// Exporta el contenido de una tabla al directorio indicado.
    /// </summary>
    /// <param name="outputDirectory">Directorio destino (se crea si no existe).</param>
    /// <param name="pageSegment">Segmento de la página de origen (parte del nombre de archivo).</param>
    /// <param name="tableId">Identificador de la tabla (parte del nombre de archivo).</param>
    /// <param name="content">Contenido textual de la tabla.</param>
    /// <param name="cancellationToken">Token para cancelar la operación.</param>
    /// <returns>Ruta completa del archivo generado.</returns>
    Task<string> ExportTableAsync(
        string outputDirectory,
        string pageSegment,
        string tableId,
        string content,
        CancellationToken cancellationToken = default);
}

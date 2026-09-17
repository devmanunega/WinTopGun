using WinTopGun.Domain.Models;

namespace WinTopGun.Application.Interfaces;

/// <summary>
/// Orquesta el proceso de extracción de tablas de las ligas del sitio objetivo.
/// </summary>
public interface IScrapingService
{
    /// <summary>
    /// Extrae las tablas de las ligas y las exporta al directorio indicado.
    /// </summary>
    /// <param name="outputDirectory">Directorio destino de los archivos generados.</param>
    /// <param name="progress">Receptor opcional de reportes de avance.</param>
    /// <param name="cancellationToken">Token para cancelar la operación.</param>
    /// <returns>Resultado consolidado de la ejecución.</returns>
    Task<ScrapingResult> ExtractLeagueTablesAsync(
        string outputDirectory,
        IProgress<ScrapingProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

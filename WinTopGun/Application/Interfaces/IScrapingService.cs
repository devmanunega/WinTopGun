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

    /// <summary>
    /// Recorre las ligas, abre únicamente los enlaces en posición par que cumplen
    /// el selector de enlaces de jugadores, espera la carga completa de cada página
    /// y extrae sus tablas con <c>id</c>.
    /// </summary>
    /// <param name="outputDirectory">Directorio destino de los archivos generados.</param>
    /// <param name="progress">Receptor opcional de reportes de avance.</param>
    /// <param name="cancellationToken">Token para cancelar la operación.</param>
    /// <returns>Resultado consolidado de la ejecución.</returns>
    Task<ScrapingResult> ExtractPlayerTablesAsync(
        string outputDirectory,
        IProgress<ScrapingProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Recorre las ligas y, por cada una, itera los equipos de su tabla de
    /// clasificación (primer <c>td</c> de cada <c>tr</c>), abre el reporte de cada
    /// partido (penúltimo <c>td</c> de la tabla <c>matchlogs_for</c>) y extrae de
    /// cada página las tablas con <c>id</c>, el div <c>scorebox_meta</c> y el div
    /// <c>events_wrap</c>. Los archivos se guardan en la subcarpeta
    /// <c>Detalles_Partidos</c> del directorio indicado.
    /// </summary>
    /// <param name="outputDirectory">Directorio base de los archivos generados.</param>
    /// <param name="progress">Receptor opcional de reportes de avance.</param>
    /// <param name="cancellationToken">Token para cancelar la operación.</param>
    /// <returns>Resultado consolidado de la ejecución.</returns>
    Task<ScrapingResult> ExtractMatchDetailsAsync(
        string outputDirectory,
        IProgress<ScrapingProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

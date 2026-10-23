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
    /// Recorre las ligas y, en la tabla de posiciones (primera tabla con <c>id</c>)
    /// de cada una, itera cada equipo (fila) y abre su página; de la tabla de
    /// historial (<c>matchlogs_for</c>) de cada equipo recolecta los enlaces con
    /// valor "Match Report". Cada enlace se identifica con su <c>MatchId</c> y se
    /// registra en una estructura persistida a nivel de aplicación; solo se abren
    /// los enlaces sin procesar. De cada página de partido se extraen las tablas
    /// con <c>id</c>, el div <c>scorebox_meta</c> y el div <c>events_wrap</c>,
    /// guardados en la subcarpeta <c>Detalles_Partidos</c>. En caso de
    /// interrupción, la siguiente ejecución procesa solo los partidos pendientes.
    /// </summary>
    /// <param name="outputDirectory">Directorio base de los archivos generados.</param>
    /// <param name="progress">Receptor opcional de reportes de avance.</param>
    /// <param name="cancellationToken">Token para cancelar la operación.</param>
    /// <returns>Resultado consolidado de la ejecución.</returns>
    Task<ScrapingResult> ExtractMatchDetailsAsync(
        string outputDirectory,
        IProgress<ScrapingProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Igual que <see cref="ExtractMatchDetailsAsync"/>, pero no recorre las 5
    /// ligas principales: trabaja sobre la página que el navegador tiene abierta
    /// en ese momento (la liga), iterando los equipos de su tabla de posiciones.
    /// </summary>
    /// <param name="outputDirectory">Directorio base de los archivos generados.</param>
    /// <param name="progress">Receptor opcional de reportes de avance.</param>
    /// <param name="cancellationToken">Token para cancelar la operación.</param>
    /// <returns>Resultado consolidado de la ejecución.</returns>
    Task<ScrapingResult> ExtractCurrentLeagueMatchDetailsAsync(
        string outputDirectory,
        IProgress<ScrapingProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

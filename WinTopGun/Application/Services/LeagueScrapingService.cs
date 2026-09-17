using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using WinTopGun.Application.Interfaces;
using WinTopGun.Domain.Models;
using WinTopGun.Infrastructure.Selenium;

namespace WinTopGun.Application.Services;

/// <summary>
/// Implementación del proceso de extracción: recorre las ligas de la página
/// principal, accede a cada una, extrae sus tablas con <c>id</c> y las exporta.
/// Toda la lógica que antes vivía en <c>btnExtract_Click</c> reside aquí.
/// </summary>
public sealed class LeagueScrapingService : IScrapingService
{
    private readonly IDriverFactory _driverFactory;
    private readonly ITableExporter _exporter;
    private readonly ScrapingOptions _options;
    private readonly ILogger<LeagueScrapingService> _logger;

    public LeagueScrapingService(
        IDriverFactory driverFactory,
        ITableExporter exporter,
        ScrapingOptions options,
        ILogger<LeagueScrapingService> logger)
    {
        _driverFactory = driverFactory;
        _exporter = exporter;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ScrapingResult> ExtractLeagueTablesAsync(
        string outputDirectory,
        IProgress<ScrapingProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        cancellationToken.ThrowIfCancellationRequested();

        // Selenium es una API bloqueante: se ejecuta en un hilo de trabajo
        // para no congelar la interfaz gráfica.
        return await Task.Run(
            () => ExtractCore(outputDirectory, progress, cancellationToken),
            cancellationToken);
    }

    private ScrapingResult ExtractCore(
        string outputDirectory,
        IProgress<ScrapingProgress>? progress,
        CancellationToken cancellationToken)
    {
        var result = new ScrapingResult();
        IWebDriver driver = _driverFactory.CreateDriver();

        try
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(_options.WaitTimeoutSeconds));

            wait.Until(d => d.FindElements(Selectors.LeagueLinks).Count > 0);
            int totalLeagues = driver.FindElements(Selectors.LeagueLinks).Count;
            _logger.LogInformation("Extracción iniciada: {TotalLeagues} ligas detectadas", totalLeagues);

            for (int i = 0; i < totalLeagues; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Re-localizar los enlaces en cada iteración (el DOM puede refrescarse).
                IList<IWebElement> enlaces = driver.FindElements(Selectors.LeagueLinks);
                IWebElement enlaceActual = enlaces[i];
                string leagueName = enlaceActual.Text;

                _logger.LogInformation("[Liga {Current}/{Total}] Abriendo enlace: {League}",
                    i + 1, totalLeagues, leagueName);
                progress?.Report(ScrapingProgress.ForLeague(i + 1, totalLeagues, leagueName));

                enlaceActual.Click();

                try
                {
                    ExtractLeagueTables(driver, wait, outputDirectory, i + 1, totalLeagues, result, progress, cancellationToken);
                    result.LeaguesProcessed++;
                }
                catch (WebDriverTimeoutException ex)
                {
                    _logger.LogWarning(ex, "Tiempo de espera agotado en la liga '{League}'", leagueName);
                    result.Errors.Add($"Liga '{leagueName}': la página no cargó tablas con 'table[id]'.");
                }

                // Volver a la página principal antes del siguiente ciclo.
                driver.Navigate().Back();
                wait.Until(d => d.FindElements(Selectors.LeagueLinks).Count > 0);
            }
        }
        catch (OperationCanceledException)
        {
            result.WasCancelled = true;
            _logger.LogInformation("Extracción cancelada por el usuario");
        }
        finally
        {
            driver.Quit();
            _logger.LogInformation("Extracción finalizada: {Leagues} ligas, {Tables} tablas exportadas, {Errors} errores",
                result.LeaguesProcessed, result.TablesExported, result.Errors.Count);
        }

        return result;
    }

    private void ExtractLeagueTables(
        IWebDriver driver,
        WebDriverWait wait,
        string outputDirectory,
        int currentLeague,
        int totalLeagues,
        ScrapingResult result,
        IProgress<ScrapingProgress>? progress,
        CancellationToken cancellationToken)
    {
        wait.Until(d => d.FindElements(Selectors.TablesWithId).Count > 0);

        string pageSegment = GetPageSegment(driver);
        List<string> tableIds = [.. driver
            .FindElements(Selectors.TablesWithId)
            .Select(table => table.GetAttribute("id"))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Cast<string>()];

        _logger.LogInformation("Liga '{Segment}': {Count} tablas con ID detectadas", pageSegment, tableIds.Count);

        for (int j = 0; j < tableIds.Count; j++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string tableId = tableIds[j];

            try
            {
                IWebElement table = driver.FindElement(Selectors.TableById(tableId));
                string content = table.GetDomProperty("innerText")
                    ?? table.GetAttribute("innerText")
                    ?? string.Empty;

                _exporter.ExportTableAsync(outputDirectory, pageSegment, tableId, content, cancellationToken)
                    .GetAwaiter()
                    .GetResult();

                result.TablesExported++;
                progress?.Report(ScrapingProgress.ForTable(currentLeague, totalLeagues, j + 1, tableIds.Count));

                _logger.LogDebug("Tabla '{TableId}' exportada ({Current}/{Total})",
                    tableId, j + 1, tableIds.Count);
            }
            catch (NoSuchElementException)
            {
                _logger.LogWarning("La tabla '{TableId}' ya no existe en el DOM", tableId);
                result.Errors.Add($"Liga '{pageSegment}': la tabla '{tableId}' ya no existe en el DOM.");
            }
            catch (StaleElementReferenceException)
            {
                _logger.LogWarning("Elemento obsoleto al procesar la tabla '{TableId}'", tableId);
                result.Errors.Add($"Liga '{pageSegment}': la tabla '{tableId}' quedó obsoleta (stale element).");
            }
        }
    }

    private static string GetPageSegment(IWebDriver driver)
    {
        var uri = new Uri(driver.Url);
        return Path.GetFileName(uri.AbsolutePath.TrimEnd('/'));
    }
}

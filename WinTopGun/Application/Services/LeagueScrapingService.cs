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
    /// <summary>Subcarpeta (dentro del directorio de salida) donde se guardan los detalles de partidos.</summary>
    public const string MatchDetailsSubfolder = "Detalles_Partidos";

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
    public async Task<ScrapingResult> ExtractPlayerTablesAsync(
        string outputDirectory,
        IProgress<ScrapingProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        cancellationToken.ThrowIfCancellationRequested();

        // Selenium es una API bloqueante: se ejecuta en un hilo de trabajo
        // para no congelar la interfaz gráfica.
        return await Task.Run(
            () => ExtractPlayerTablesCore(outputDirectory, progress, cancellationToken),
            cancellationToken);
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

    /// <inheritdoc />
    public async Task<ScrapingResult> ExtractMatchDetailsAsync(
        string outputDirectory,
        IProgress<ScrapingProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        cancellationToken.ThrowIfCancellationRequested();

        // Selenium es una API bloqueante: se ejecuta en un hilo de trabajo
        // para no congelar la interfaz gráfica.
        return await Task.Run(
            () => ExtractMatchDetailsCore(outputDirectory, progress, cancellationToken),
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

    /// <summary>
    /// Núcleo del proceso de jugadores: recorre las ligas, recolecta los enlaces
    /// que cumplen <see cref="Selectors.PlayerLinks"/>, abre solo los de posición
    /// par, espera la carga completa de cada página y extrae sus tablas con <c>id</c>.
    /// </summary>
    private ScrapingResult ExtractPlayerTablesCore(
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
            _logger.LogInformation("Extracción de jugadores iniciada: {TotalLeagues} ligas detectadas", totalLeagues);

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
                    wait.Until(d => d.FindElements(Selectors.PlayerLinks).Count > 0);
                    OpenEvenPlayerLinks(driver, wait, outputDirectory, i + 1, totalLeagues, result, progress, cancellationToken);
                    result.LeaguesProcessed++;
                }
                catch (WebDriverTimeoutException ex)
                {
                    _logger.LogWarning(ex, "Tiempo de espera agotado en la liga '{League}'", leagueName);
                    result.Errors.Add($"Liga '{leagueName}': la página no cargó los enlaces de jugadores.");
                }

                // Volver a la página principal antes del siguiente ciclo.
                driver.Navigate().Back();
                wait.Until(d => d.FindElements(Selectors.LeagueLinks).Count > 0);
            }
        }
        catch (OperationCanceledException)
        {
            result.WasCancelled = true;
            _logger.LogInformation("Extracción de jugadores cancelada por el usuario");
        }
        finally
        {
            driver.Quit();
            _logger.LogInformation("Extracción de jugadores finalizada: {Leagues} ligas, {Tables} tablas exportadas, {Errors} errores",
                result.LeaguesProcessed, result.TablesExported, result.Errors.Count);
        }

        return result;
    }

    /// <summary>
    /// Abre secuencialmente los enlaces de jugadores ubicados en posición par
    /// (2.º, 4.º, 6.º, … dentro del grupo de enlaces encontrados) y extrae las
    /// tablas con <c>id</c> de cada página.
    /// </summary>
    private void OpenEvenPlayerLinks(
        IWebDriver driver,
        WebDriverWait wait,
        string outputDirectory,
        int currentLeague,
        int totalLeagues,
        ScrapingResult result,
        IProgress<ScrapingProgress>? progress,
        CancellationToken cancellationToken)
    {
        // Recolectar URL y texto por adelantado: navegar por URL evita elementos
        // obsoletos (stale) al regresar de cada página de detalle.
        // Posición par en base 1 (2.º, 4.º, …) equivale a índice impar en base 0.
        List<(string Url, string Text)> targets = [.. driver
            .FindElements(Selectors.PlayerLinks)
            .Select((link, index) => (Link: link, Index: index))
            .Where(x => x.Index % 2 == 1)
            .Select(x => (Url: x.Link.GetAttribute("href"), Text: x.Link.Text))
            .Where(x => !string.IsNullOrWhiteSpace(x.Url))];

        _logger.LogInformation("Liga {Current}/{Total}: {Total} enlaces pares detectados de {All} enlaces",
            currentLeague, totalLeagues, targets.Count, driver.FindElements(Selectors.PlayerLinks).Count);

        for (int k = 0; k < targets.Count; k++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            (string url, string linkText) = targets[k];
            _logger.LogInformation("[Enlace {Current}/{Total}] Abriendo: {Url}", k + 1, targets.Count, url);
            progress?.Report(ScrapingProgress.ForPlayerLink(currentLeague, totalLeagues, k + 1, targets.Count, linkText));

            driver.Navigate().GoToUrl(url);
            WaitForPageComplete(driver, wait);

            // Pausa aleatoria para simular la navegación de una persona antes
            // de proceder con la extracción de las tablas.
            PauseToSimulateHumanNavigation(cancellationToken);

            try
            {
                wait.Until(d => d.FindElements(Selectors.TablesWithId).Count > 0);
            }
            catch (WebDriverTimeoutException)
            {
                _logger.LogWarning("La página '{Url}' no cargó tablas con 'table[id]'", url);
                result.Errors.Add($"Liga {currentLeague}: la página '{linkText}' no cargó tablas con 'table[id]'.");

                // Regresar a la página de liga antes del siguiente enlace.
                driver.Navigate().Back();
                wait.Until(d => d.FindElements(Selectors.PlayerLinks).Count > 0);
                continue;
            }

            string pageSegment = GetPageSegment(driver);
            ExtractTablesOnCurrentPage(driver, outputDirectory, pageSegment, currentLeague, totalLeagues, result, progress, cancellationToken);

            // Volver a la página de liga antes del siguiente enlace.
            driver.Navigate().Back();
            wait.Until(d => d.FindElements(Selectors.PlayerLinks).Count > 0);
        }
    }

    /// <summary>
    /// Núcleo del proceso de partidos: recorre las ligas, itera los equipos de la
    /// primera tabla de cada liga (enlaces en el primer <c>td</c> de cada <c>tr</c>),
    /// abre los reportes de partido (penúltimo <c>td</c> de la tabla
    /// <c>matchlogs_for</c>) y extrae de cada página las tablas con <c>id</c> y los
    /// divs <c>scorebox_meta</c> y <c>events_wrap</c>, guardándolos en
    /// <c>&lt;outputDirectory&gt;/Detalles_Partidos</c>.
    /// </summary>
    private ScrapingResult ExtractMatchDetailsCore(
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
            _logger.LogInformation("Extracción de partidos iniciada: {TotalLeagues} ligas detectadas", totalLeagues);

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
                    ProcessLeagueMatches(driver, wait, outputDirectory, i + 1, totalLeagues, result, progress, cancellationToken);
                    result.LeaguesProcessed++;
                }
                catch (WebDriverTimeoutException ex)
                {
                    _logger.LogWarning(ex, "Tiempo de espera agotado en la liga '{League}'", leagueName);
                    result.Errors.Add($"Liga '{leagueName}': la página no cargó la tabla de equipos.");
                }

                // Volver a la página principal antes del siguiente ciclo.
                driver.Navigate().Back();
                wait.Until(d => d.FindElements(Selectors.LeagueLinks).Count > 0);
            }
        }
        catch (OperationCanceledException)
        {
            result.WasCancelled = true;
            _logger.LogInformation("Extracción de partidos cancelada por el usuario");
        }
        finally
        {
            driver.Quit();
            _logger.LogInformation("Extracción de partidos finalizada: {Leagues} ligas, {Tables} tablas exportadas, {Errors} errores",
                result.LeaguesProcessed, result.TablesExported, result.Errors.Count);
        }

        return result;
    }

    /// <summary>Texto que identifica el enlace de un partido ya disputado.</summary>
    private const string MatchReportText = "Match Report";

    /// <summary>
    /// Indica si el texto del enlace corresponde a un partido disputado
    /// ("Match Report"). Los partidos pendientes no tienen reporte.
    /// </summary>
    private static bool EsEnlacePartidoDisputado(string linkText) =>
        string.Equals(linkText.Trim(), MatchReportText, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Itera los equipos de la tabla de clasificación de la liga actual (primer
    /// <c>td</c> de cada <c>tr</c>), abre la página de cada uno y delega en
    /// <see cref="ProcessTeamMatches"/> la extracción de sus partidos.
    /// </summary>
    private void ProcessLeagueMatches(
        IWebDriver driver,
        WebDriverWait wait,
        string outputDirectory,
        int currentLeague,
        int totalLeagues,
        ScrapingResult result,
        IProgress<ScrapingProgress>? progress,
        CancellationToken cancellationToken)
    {
        // Los detalles de partidos se guardan en la subcarpeta Detalles_Partidos.
        string matchDirectory = Path.Combine(outputDirectory, MatchDetailsSubfolder);

        // Primera tabla de la página de liga (clasificación); contiene la lista de equipos.
        wait.Until(d => d.FindElements(Selectors.TablesWithId).Count > 0);
        IWebElement tablaEquipos = FindStandingsTable(driver);

        // Recolectar los enlaces de equipos por adelantado: navegar por URL evita
        // elementos obsoletos (stale) al regresar de cada página visitada.
        List<(string Url, string Text)> equipos = ExtraerEnlacesEquipos(tablaEquipos);

        _logger.LogInformation("Liga {Current}/{Total}: {Count} equipos detectados",
            currentLeague, totalLeagues, equipos.Count);

        for (int e = 0; e < equipos.Count; e++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            (string teamUrl, string teamName) = equipos[e];
            _logger.LogInformation("[Equipo {Current}/{Total}] Abriendo: {Team}", e + 1, equipos.Count, teamUrl);
            progress?.Report(ScrapingProgress.ForTeamLink(currentLeague, totalLeagues, e + 1, equipos.Count, teamName));

            driver.Navigate().GoToUrl(teamUrl);
            WaitForPageComplete(driver, wait);
            PauseToSimulateHumanNavigation(cancellationToken);

            try
            {
                wait.Until(d => d.FindElements(Selectors.MatchLogsTable).Count > 0);
            }
            catch (WebDriverTimeoutException)
            {
                _logger.LogWarning("El equipo '{Team}' no cargó la tabla de partidos ('matchlogs_for')", teamUrl);
                result.Errors.Add($"Liga {currentLeague}: el equipo '{teamName}' no cargó la tabla 'matchlogs_for'.");
                continue;
            }

            ProcessTeamMatches(driver, wait, matchDirectory, currentLeague, totalLeagues, e + 1, equipos.Count, teamName, result, progress, cancellationToken);

            // Volver a la página de liga antes del siguiente equipo.
            driver.Navigate().Back();
            WaitForPageComplete(driver, wait);
            wait.Until(d => d.FindElements(Selectors.TablesWithId).Count > 0);
        }
    }

    /// <summary>
    /// Abre secuencialmente los reportes de partido del equipo actual (enlaces en
    /// el penúltimo <c>td</c> de cada fila de <c>matchlogs_for</c>) y, en cada
    /// página, extrae las tablas con <c>id</c> y los divs <c>scorebox_meta</c> y
    /// <c>events_wrap</c>, exportándolos al directorio de detalles de partidos.
    /// </summary>
    private void ProcessTeamMatches(
        IWebDriver driver,
        WebDriverWait wait,
        string matchDirectory,
        int currentLeague,
        int totalLeagues,
        int currentTeam,
        int totalTeams,
        string teamName,
        ScrapingResult result,
        IProgress<ScrapingProgress>? progress,
        CancellationToken cancellationToken)
    {
        IWebElement tablaPartidos = driver.FindElement(Selectors.MatchLogsTable);

        // Recolectar los enlaces de partidos por adelantado: se prefiere la columna
        // estándar (data-stat="match_report") y, como alternativa, el penúltimo td
        // de cada fila. Solo se procesan partidos ya disputados: el enlace del
        // reporte contiene el texto "Match Report"; los encuentros pendientes no lo
        // tienen, por lo que quedan fuera de la extracción.
        List<(string Url, string Text)> partidos = [.. tablaPartidos
            .FindElements(Selectors.MatchLogRows)
            .Select(row => row.FindElements(Selectors.MatchReportLink).FirstOrDefault()
                ?? row.FindElements(Selectors.PenultimateCellLink).FirstOrDefault())
            .Where(link => link is not null)
            .Select(link => (Url: link!.GetAttribute("href"), Text: link.Text))
            .Where(x => !string.IsNullOrWhiteSpace(x.Url) && EsEnlacePartidoDisputado(x.Text))];

        _logger.LogInformation("Equipo '{Team}': {Count} partidos disputados detectados (de {Rows} filas en matchlogs_for)",
            teamName, partidos.Count, tablaPartidos.FindElements(Selectors.MatchLogRows).Count);

        for (int m = 0; m < partidos.Count; m++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            (string matchUrl, string matchText) = partidos[m];
            _logger.LogInformation("[Partido {Current}/{Total}] Abriendo: {Match}", m + 1, partidos.Count, matchUrl);
            progress?.Report(ScrapingProgress.ForMatchLink(currentLeague, totalLeagues, currentTeam, totalTeams, m + 1, partidos.Count, matchText));

            driver.Navigate().GoToUrl(matchUrl);
            WaitForPageComplete(driver, wait);
            PauseToSimulateHumanNavigation(cancellationToken);

            try
            {
                wait.Until(d => d.FindElements(Selectors.TablesWithId).Count > 0);
            }
            catch (WebDriverTimeoutException)
            {
                _logger.LogWarning("La página del partido '{Url}' no cargó tablas con 'table[id]'", matchUrl);
                result.Errors.Add($"Liga {currentLeague}: la página del partido '{matchText}' no cargó tablas con 'table[id]'.");
                continue;
            }

            string pageSegment = GetPageSegment(driver);
            ExtractTablesOnCurrentPage(driver, matchDirectory, pageSegment, currentLeague, totalLeagues, result, progress, cancellationToken);

            // Elementos adicionales de la página de detalle del partido.
            ExportElementContent(driver, matchDirectory, pageSegment, Selectors.ScoreboxMeta, "scorebox_meta", result, cancellationToken);
            ExportElementContent(driver, matchDirectory, pageSegment, Selectors.EventsWrap, "events_wrap", result, cancellationToken);

            // Volver a la página del equipo antes del siguiente partido.
            driver.Navigate().Back();
            WaitForPageComplete(driver, wait);
            wait.Until(d => d.FindElements(Selectors.MatchLogsTable).Count > 0);
        }
    }

    /// <summary>
    /// Extrae el contenido de texto de un elemento (p. ej. los divs
    /// <c>scorebox_meta</c> y <c>events_wrap</c>) y lo exporta con el mismo
    /// mecanismo usado para las tablas.
    /// </summary>
    private void ExportElementContent(
        IWebDriver driver,
        string outputDirectory,
        string pageSegment,
        By locator,
        string elementId,
        ScrapingResult result,
        CancellationToken cancellationToken)
    {
        try
        {
            IWebElement element = driver.FindElement(locator);
            string content = element.GetDomProperty("innerText")
                ?? element.GetAttribute("innerText")
                ?? string.Empty;

            _exporter.ExportTableAsync(outputDirectory, pageSegment, elementId, content, cancellationToken)
                .GetAwaiter()
                .GetResult();

            result.TablesExported++;
            _logger.LogDebug("Elemento '{ElementId}' exportado desde '{Segment}'", elementId, pageSegment);
        }
        catch (NoSuchElementException)
        {
            _logger.LogWarning("El elemento '{ElementId}' no existe en la página '{Segment}'", elementId, pageSegment);
            result.Errors.Add($"Partido '{pageSegment}': el elemento '{elementId}' no existe en la página.");
        }
        catch (StaleElementReferenceException)
        {
            _logger.LogWarning("Elemento obsoleto al procesar '{ElementId}' en '{Segment}'", elementId, pageSegment);
            result.Errors.Add($"Partido '{pageSegment}': el elemento '{elementId}' quedó obsoleto (stale element).");
        }
    }

    /// <summary>
    /// Extrae los enlaces de equipo de la tabla de clasificación: usa la celda
    /// estándar del sitio (<c>data-stat="team"</c>) y, si no produce resultados,
    /// el primer <c>td</c> de cada fila.
    /// </summary>
    private static List<(string Url, string Text)> ExtraerEnlacesEquipos(IWebElement tablaEquipos)
    {
        List<IWebElement> enlaces = [.. tablaEquipos.FindElements(Selectors.TeamLink)];

        if (enlaces.Count == 0)
        {
            enlaces = [.. tablaEquipos.FindElements(Selectors.FirstDataCellLink)];
        }

        return [.. enlaces
            .Select(a => (Url: a.GetAttribute("href"), Text: a.Text))
            .Where(x => !string.IsNullOrWhiteSpace(x.Url))];
    }

    /// <summary>
    /// Localiza la tabla de clasificación de la liga: prioriza la tabla cuyo <c>id</c>
    /// sigue el patrón del sitio (<c>results…_overall</c>, p. ej.
    /// <c>results2026-202791_overall</c>) y, si no existe, usa la primera <c>table[id]</c>.
    /// </summary>
    private static IWebElement FindStandingsTable(IWebDriver driver)
    {
        List<IWebElement> tablas = [.. driver.FindElements(Selectors.TablesWithId)];
        return tablas.FirstOrDefault(t =>
        {
            string id = t.GetAttribute("id") ?? string.Empty;
            return id.StartsWith("results", StringComparison.Ordinal) && id.EndsWith("_overall", StringComparison.Ordinal);
        })
            ?? tablas.FirstOrDefault()
            ?? throw new NoSuchElementException("No se encontró la tabla de clasificación en la página de la liga.");
    }

    /// <summary>
    /// Espera a que el documento alcance el estado de carga "complete".
    /// Con <see cref="PageLoadStrategy.Eager"/> la navegación devuelve el control
    /// antes de terminar de cargar todos los recursos, por lo que esta espera
    /// explícita es necesaria.
    /// </summary>
    private static void WaitForPageComplete(IWebDriver driver, WebDriverWait wait)
    {
        wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString() == "complete");
    }

    /// <summary>
    /// Pausa un intervalo aleatorio (entre <see cref="ScrapingOptions.HumanPauseMinMilliseconds"/> y
    /// <see cref="ScrapingOptions.HumanPauseMaxMilliseconds"/>) tras la carga completa de una página,
    /// para simular la navegación de una persona. La espera se realiza en fragmentos para
    /// responder con rapidez a una solicitud de cancelación.
    /// </summary>
    private void PauseToSimulateHumanNavigation(CancellationToken cancellationToken)
    {
        int min = _options.HumanPauseMinMilliseconds;
        int max = _options.HumanPauseMaxMilliseconds;

        if (max <= 0 || min > max)
        {
            return;
        }

        int delayMs = Random.Shared.Next(min, max + 1);
        _logger.LogInformation("Pausa de {Seconds:N1} s para simular navegación humana", delayMs / 1000.0);

        const int SliceMs = 200;
        int remaining = delayMs;
        while (remaining > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int slice = Math.Min(SliceMs, remaining);
            Thread.Sleep(slice);
            remaining -= slice;
        }
    }

    /// <summary>
    /// Extrae y exporta todas las tablas con <c>id</c> presentes en la página actual.
    /// Reutilizado por el flujo de ligas y por el flujo de jugadores.
    /// </summary>
    private void ExtractTablesOnCurrentPage(
        IWebDriver driver,
        string outputDirectory,
        string pageSegment,
        int currentLeague,
        int totalLeagues,
        ScrapingResult result,
        IProgress<ScrapingProgress>? progress,
        CancellationToken cancellationToken)
    {
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

    /// <summary>
    /// Flujo original: espera a que la página de liga muestre tablas y delega
    /// en <see cref="ExtractTablesOnCurrentPage"/>.
    /// </summary>
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
        ExtractTablesOnCurrentPage(driver, outputDirectory, pageSegment, currentLeague, totalLeagues, result, progress, cancellationToken);
    }

    private static string GetPageSegment(IWebDriver driver)
    {
        var uri = new Uri(driver.Url);
        return Path.GetFileName(uri.AbsolutePath.TrimEnd('/'));
    }
}

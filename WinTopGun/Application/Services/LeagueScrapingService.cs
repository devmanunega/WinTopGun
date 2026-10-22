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
    private readonly IMatchLinkRegistry _matchLinkRegistry;
    private readonly ScrapingOptions _options;
    private readonly ILogger<LeagueScrapingService> _logger;

    public LeagueScrapingService(
        IDriverFactory driverFactory,
        ITableExporter exporter,
        IMatchLinkRegistry matchLinkRegistry,
        ScrapingOptions options,
        ILogger<LeagueScrapingService> logger)
    {
        _driverFactory = driverFactory;
        _exporter = exporter;
        _matchLinkRegistry = matchLinkRegistry;
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
    /// Núcleo del proceso de partidos: recorre las ligas y, en la tabla de
    /// posiciones (primera tabla con <c>id</c>) de cada una, itera cada equipo;
    /// de la página de cada equipo recolecta los enlaces "Match Report" de su
    /// tabla de historial, genera su hash y abre únicamente los enlaces sin
    /// procesar según el registro persistido.
    /// </summary>
    private ScrapingResult ExtractMatchDetailsCore(
        string outputDirectory,
        IProgress<ScrapingProgress>? progress,
        CancellationToken cancellationToken)
    {
        var result = new ScrapingResult();
        string matchDirectory = Path.Combine(outputDirectory, MatchDetailsSubfolder);

        // 1.5/1.8: cargar la estructura a nivel de aplicación que registra los
        // enlaces procesados; permite reanudar tras una interrupción.
        _matchLinkRegistry.Initialize(matchDirectory);
        if (_matchLinkRegistry.ProcessedCount > 0)
        {
            _logger.LogInformation("Reanudando extracción de partidos: {Completed} enlaces ya procesados; solo se abrirán los pendientes",
                _matchLinkRegistry.ProcessedCount);
        }

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
                    wait.Until(d => d.FindElements(Selectors.TablesWithId).Count > 0);
                    ProcessLeagueMatchLinks(driver, wait, matchDirectory, i + 1, totalLeagues, result, progress, cancellationToken);
                    result.LeaguesProcessed++;
                }
                catch (WebDriverTimeoutException ex)
                {
                    _logger.LogWarning(ex, "Tiempo de espera agotado en la liga '{League}'", leagueName);
                    result.Errors.Add($"Liga '{leagueName}': la página no cargó la tabla de posiciones.");
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
            _logger.LogInformation("Extracción de partidos finalizada: {Leagues} ligas, {Tables} tablas exportadas, {Skipped} partidos omitidos (ya procesados), {Errors} errores",
                result.LeaguesProcessed, result.TablesExported, result.MatchesSkipped, result.Errors.Count);
        }

        return result;
    }

    /// <summary>
    /// Procesa los partidos de la liga actual recorriendo cada equipo de la tabla
    /// de posiciones (1.1): abre la página de cada equipo, recolecta de su tabla
    /// de historial (<c>matchlogs_for</c>) los enlaces con valor "Match Report"
    /// (1.2–1.4) y abre únicamente los que no constan en el registro (1.7). De
    /// cada partido extrae las tablas con <c>id</c> y los elementos de detalle, y
    /// actualiza su estado a "procesado" en la estructura (persistido de inmediato).
    /// </summary>
    private void ProcessLeagueMatchLinks(
        IWebDriver driver,
        WebDriverWait wait,
        string matchDirectory,
        int currentLeague,
        int totalLeagues,
        ScrapingResult result,
        IProgress<ScrapingProgress>? progress,
        CancellationToken cancellationToken)
    {
        // 1: tabla de posiciones = primera tabla con atributo id.
        IWebElement tablaPosiciones = FindStandingsTable(driver);

        // 1.1: recorrer cada equipo (fila de la tabla de posiciones). Los enlaces
        // de equipos se recolectan por adelantado para evitar elementos obsoletos.
        List<(string Url, string Text)> equipos = ExtraerEnlacesEquipos(tablaPosiciones);

        _logger.LogInformation("Liga {Current}/{Total}: {Count} equipos detectados en la tabla de posiciones",
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
                VolverAPaginaDeLiga(driver, wait);
                continue;
            }

            // 1.2–1.4: recolectar los enlaces "Match Report" del equipo, extraer
            // sus href, normalizarlos a URL absoluta y obtener el ID único de cada partido.
            List<MatchLinkTarget> targets = CollectMatchReportLinks(driver.FindElement(Selectors.MatchLogsTable), teamName, driver.Url);

            // 1.2: dar de alta los enlaces en el registro (los nuevos como Pending;
            // los existentes no se sobrescriben).
            foreach (MatchLinkTarget target in targets)
            {
                _matchLinkRegistry.Register(target);
            }

            int pendientes = 0;

            for (int k = 0; k < targets.Count; k++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                MatchLinkTarget target = targets[k];

                // 1.7 / 2.1–2.2: abrir solo los enlaces cuyo estado es distinto de
                // Processed. Los ya procesados se omiten y se continúa con el siguiente.
                if (_matchLinkRegistry.IsProcessed(target.MatchId))
                {
                    result.MatchesSkipped++;
                    _logger.LogDebug("Partido '{Url}' ya procesado; se omite", target.Url);
                    continue;
                }

                pendientes++;
                _logger.LogInformation("[Partido {Current}/{Total}] Abriendo: {Url}", k + 1, targets.Count, target.Url);
                progress?.Report(ScrapingProgress.ForMatchLink(currentLeague, totalLeagues, k + 1, targets.Count, teamName));

                driver.Navigate().GoToUrl(target.Url);
                WaitForPageComplete(driver, wait);

                // Simula un usuario revisando el resultado: pausa larga (3-8 s por defecto)
                // y pequeños desplazamientos de página antes de extraer los datos.
                SimularRevisionPartido(driver, cancellationToken);

                try
                {
                    wait.Until(d => d.FindElements(Selectors.TablesWithId).Count > 0);
                }
                catch (WebDriverTimeoutException)
                {
                    _logger.LogWarning("La página del partido '{Url}' no cargó tablas con 'table[id]'", target.Url);
                    result.Errors.Add($"Liga {currentLeague}: la página del partido '{target.Url}' no cargó tablas con 'table[id]'.");
                    _matchLinkRegistry.MarkFailed(target);
                    RegresarAPaginaDelEquipo(driver, wait);
                    continue;
                }

                string pageSegment = GetPageSegment(driver);
                int erroresAntes = result.Errors.Count;

                ExtractTablesOnCurrentPage(driver, matchDirectory, pageSegment, currentLeague, totalLeagues, result, progress, cancellationToken);

                // Elementos adicionales de la página de detalle del partido. Son
                // complementarios: su ausencia no impide registrar el partido como
                // procesado (en FBref el div scorebox_meta no existe en todas las páginas).
                ExportElementContent(driver, matchDirectory, pageSegment, Selectors.ScoreboxMeta, "scorebox_meta", result, cancellationToken, esOpcional: true);
                ExportElementContent(driver, matchDirectory, pageSegment, Selectors.EventsWrap, "events_wrap", result, cancellationToken, esOpcional: true);

                // 2.4: marcar según el resultado — Processed solo si la exportación
                // fue limpia; Failed (reintento en la próxima ejecución) si hubo
                // incidencias. Ambos estados se persisten de inmediato.
                if (result.Errors.Count == erroresAntes)
                {
                    _matchLinkRegistry.MarkProcessed(target);
                }
                else
                {
                    _matchLinkRegistry.MarkFailed(target);
                    _logger.LogWarning("El partido '{Match}' terminó con {Count} incidencia(s); se reintentará en la próxima ejecución",
                        target.Url, result.Errors.Count - erroresAntes);
                }

                // Volver a la página del equipo antes del siguiente partido.
                RegresarAPaginaDelEquipo(driver, wait);
            }

            _logger.LogInformation("Equipo '{Team}': {Total} partidos disputados, {Pendientes} procesados en esta ejecución",
                teamName, targets.Count, pendientes);

            // Volver a la página de liga antes del siguiente equipo.
            VolverAPaginaDeLiga(driver, wait);
        }
    }

    /// <summary>Regresa a la página de liga y espera a que su tabla de posiciones esté disponible.</summary>
    private static void VolverAPaginaDeLiga(IWebDriver driver, WebDriverWait wait)
    {
        driver.Navigate().Back();
        WaitForPageComplete(driver, wait);
        wait.Until(d => d.FindElements(Selectors.TablesWithId).Count > 0);
    }

    /// <summary>Regresa a la página del equipo y espera a que su tabla de partidos esté disponible.</summary>
    private static void RegresarAPaginaDelEquipo(IWebDriver driver, WebDriverWait wait)
    {
        driver.Navigate().Back();
        WaitForPageComplete(driver, wait);
        wait.Until(d => d.FindElements(Selectors.MatchLogsTable).Count > 0);
    }

    /// <summary>
    /// Extrae los enlaces de equipo de la tabla de posiciones: usa la celda
    /// estándar del sitio (<c>data-stat="team"</c>) y, si no produce resultados,
    /// el primer <c>td</c> de cada fila. Se omiten filas sin enlace (p. ej. cabeceras).
    /// </summary>
    private static List<(string Url, string Text)> ExtraerEnlacesEquipos(IWebElement tablaPosiciones)
    {
        List<IWebElement> filas = [.. tablaPosiciones.FindElements(Selectors.TableRows)];

        List<IWebElement> enlaces = [.. filas
            .Select(fila => fila.FindElements(Selectors.TeamLink).FirstOrDefault()
                ?? fila.FindElements(Selectors.FirstDataCellLink).FirstOrDefault())
            .Where(link => link is not null)
            .Select(link => link!)
            .Distinct()];

        return [.. enlaces
            .Select(a => (Url: a.GetAttribute("href"), Text: a.Text))
            .Where(x => !string.IsNullOrWhiteSpace(x.Url))];
    }

    /// <summary>
    /// Recolecta los enlaces "Match Report" de la tabla de historial del equipo
    /// (fase 1.0–1.2 de Ideas/Partidos.md): usa el selector robusto
    /// (<c>data-stat="match_report"</c> + texto, filas con <c>data-row</c>),
    /// normaliza cada href a URL absoluta (1.1) y extrae el ID único del partido
    /// como clave de deduplicación. Los elementos se leen como texto por adelantado
    /// para evitar <c>StaleElementReferenceException</c> al navegar (punto de
    /// atención 1 del documento).
    /// </summary>
    private List<MatchLinkTarget> CollectMatchReportLinks(IWebElement tablaPartidos, string teamName, string pageUrl)
    {
        var targets = new List<MatchLinkTarget>();
        var matchIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (IWebElement fila in tablaPartidos.FindElements(Selectors.MatchLogDataRows))
        {
            int rowIndex = int.TryParse(fila.GetAttribute("data-row"), out int valor) ? valor : -1;

            // Selector recomendado: celda data-stat="match_report" con texto exacto.
            IWebElement? link = fila.FindElements(Selectors.MatchReportCellLink).FirstOrDefault();
            if (link is null)
            {
                continue; // Partido futuro o fila sin reporte: se tolera.
            }

            // 1.1: href normalizado a URL absoluta.
            string href = link.GetAttribute("href");
            if (string.IsNullOrWhiteSpace(href))
            {
                continue;
            }

            string urlAbsoluta = NormalizarUrl(href, pageUrl);

            // 1.1: ID único del partido (clave de deduplicación).
            string matchId = MatchIdExtractor.ExtractId(urlAbsoluta);
            if (!matchIds.Add(matchId))
            {
                continue;
            }

            targets.Add(new MatchLinkTarget(matchId, urlAbsoluta, teamName, rowIndex));
        }

        return targets;
    }

    /// <summary>Convierte un href relativo a URL absoluta usando la página actual como base.</summary>
    private static string NormalizarUrl(string href, string pageUrl)
    {
        if (Uri.TryCreate(href, UriKind.Absolute, out _))
        {
            return href;
        }

        return Uri.TryCreate(pageUrl, UriKind.Absolute, out var baseUri)
            ? new Uri(baseUri, href).ToString()
            : href;
    }

    /// <summary>
    /// Extrae el contenido de texto de un elemento (p. ej. los divs
    /// <c>scorebox_meta</c> y <c>events_wrap</c>) y lo exporta con el mismo
    /// mecanismo usado para las tablas.
    /// </summary>
    /// <param name="esOpcional">
    /// Si es verdadero, la ausencia o obsolescencia del elemento solo genera una
    /// advertencia (no impide que el partido se registre como procesado).
    /// </param>
    private void ExportElementContent(
        IWebDriver driver,
        string outputDirectory,
        string pageSegment,
        By locator,
        string elementId,
        ScrapingResult result,
        CancellationToken cancellationToken,
        bool esOpcional = false)
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
            if (esOpcional)
            {
                _logger.LogWarning("El elemento opcional '{ElementId}' no existe en la página '{Segment}'; se omite", elementId, pageSegment);
                return;
            }

            _logger.LogWarning("El elemento '{ElementId}' no existe en la página '{Segment}'", elementId, pageSegment);
            result.Errors.Add($"Partido '{pageSegment}': el elemento '{elementId}' no existe en la página.");
        }
        catch (StaleElementReferenceException)
        {
            if (esOpcional)
            {
                _logger.LogWarning("Elemento opcional '{ElementId}' obsoleto en '{Segment}'; se omite", elementId, pageSegment);
                return;
            }

            _logger.LogWarning("Elemento obsoleto al procesar '{ElementId}' en '{Segment}'", elementId, pageSegment);
            result.Errors.Add($"Partido '{pageSegment}': el elemento '{elementId}' quedó obsoleto (stale element).");
        }
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
    private void PauseToSimulateHumanNavigation(CancellationToken cancellationToken) =>
        PausaAleatoria(_options.HumanPauseMinMilliseconds, _options.HumanPauseMaxMilliseconds, cancellationToken);

    /// <summary>
    /// Simula la revisión del resultado de un partido antes de extraer sus datos:
    /// pausa aleatoria (entre <see cref="ScrapingOptions.MatchPauseMinMilliseconds"/> y
    /// <see cref="ScrapingOptions.MatchPauseMaxMilliseconds"/>) seguida de pequeños
    /// desplazamientos de página con pausas breves de "lectura".
    /// </summary>
    private void SimularRevisionPartido(IWebDriver driver, CancellationToken cancellationToken)
    {
        PausaAleatoria(_options.MatchPauseMinMilliseconds, _options.MatchPauseMaxMilliseconds, cancellationToken);
        SimularScrollDeLectura(driver, cancellationToken);
    }

    /// <summary>Pausa un intervalo aleatorio entre <paramref name="minMs"/> y <paramref name="maxMs"/> (0 o negativo desactiva la pausa).</summary>
    private void PausaAleatoria(int minMs, int maxMs, CancellationToken cancellationToken)
    {
        if (maxMs <= 0 || minMs > maxMs)
        {
            return;
        }

        int delayMs = Random.Shared.Next(minMs, maxMs + 1);
        _logger.LogInformation("Pausa de {Seconds:N1} s para simular navegación humana", delayMs / 1000.0);
        DormirConCancelacion(delayMs, cancellationToken);
    }

    /// <summary>Espera en fragmentos de 200 ms para responder con rapidez a una solicitud de cancelación.</summary>
    private static void DormirConCancelacion(int delayMs, CancellationToken cancellationToken)
    {
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
    /// Ejecuta pequeños desplazamientos de página (200-600 px) con pausas breves
    /// entre cada uno, simulando la lectura del resultado del partido. El bucle se
    /// repite hasta alcanzar el final de la página, y se garantiza la llegada al
    /// fondo con un último desplazamiento absoluto.
    /// </summary>
    private static void SimularScrollDeLectura(IWebDriver driver, CancellationToken cancellationToken)
    {
        var executor = (IJavaScriptExecutor)driver;
        const int MaximosDesplazamientos = 100; // Salvaguarda frente a páginas de longitud anómala.

        for (int i = 0; i < MaximosDesplazamientos; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // ¿Se alcanzó el final de la página?
            bool alFinal = Convert.ToBoolean(executor.ExecuteScript(
                "return window.scrollY + window.innerHeight >= document.documentElement.scrollHeight - 1;"));

            if (alFinal)
            {
                return;
            }

            int pixeles = Random.Shared.Next(200, 600);
            executor.ExecuteScript($"window.scrollBy(0, {pixeles});");
            DormirConCancelacion(Random.Shared.Next(200, 500), cancellationToken);
        }

        // Refuerzo: garantizar la llegada al fondo aunque el paso aleatorio
        // haya quedado a pocos píxeles del final (p. ej. por zoom o DPI).
        executor.ExecuteScript("window.scrollTo(0, document.documentElement.scrollHeight);");
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

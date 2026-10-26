using System.Collections.ObjectModel;
using OpenQA.Selenium;

namespace WinTopGun.Tests.Fakes;

/// <summary>Fila de la tabla de posiciones simulada (un equipo por fila).</summary>
/// <param name="TeamName">Nombre visible del equipo.</param>
/// <param name="TeamHref">Enlace del equipo (celda data-stat="team").</param>
internal sealed record FakeStandingsRow(string TeamName, string TeamHref);

/// <summary>
/// Implementación mínima de <see cref="IWebDriver"/> que simula una página principal
/// con N ligas, páginas de liga con tabla de posiciones (filas con enlaces "Match
/// Report") y páginas de partido con tablas con <c>id</c>. Permite ejercitar el
/// algoritmo de <c>LeagueScrapingService</c> sin un navegador real.
/// </summary>
internal sealed class FakeWebDriver : IWebDriver, IJavaScriptExecutor
{
    private readonly Dictionary<string, IWebElement> _tablesById = [];

    /// <summary>Enlaces de ligas visibles en la página principal.</summary>
    public List<string> LeagueNames { get; set; } = ["Liga A", "Liga B"];

    /// <summary>Enlaces de jugadores (href) visibles en la página de liga, en orden de aparición.</summary>
    public List<string> PlayerLinkHrefs { get; set; } = [];

    /// <summary>Filas de la tabla de posiciones de la página de liga (proceso de partidos).</summary>
    public List<FakeStandingsRow> StandingsRows { get; set; } = [];

    /// <summary>Enlaces de reportes de partido (href) en las filas de 'matchlogs_for', uno por fila.</summary>
    public List<string> MatchReportHrefs { get; set; } = [];

    /// <summary>Textos de los enlaces de partido, alineados con MatchReportHrefs. Nulo o fuera de rango = "Match Report".</summary>
    public List<string>? MatchLinkTexts { get; set; }

    /// <summary>Indica si la tabla 'matchlogs_for' es visible; falso para simular un timeout en la página del equipo.</summary>
    public bool MatchLogsVisible { get; set; } = true;

    /// <summary>Elementos adicionales por id (p. ej. 'scorebox_meta', 'events_wrap') con su contenido de texto.</summary>
    public Dictionary<string, string> ExtraElementsById { get; set; } = [];

    /// <summary>Altura total simulada de la página (px) para los scripts de scroll.</summary>
    public int ScrollHeight { get; set; } = 2500;

    /// <summary>Altura simulada del viewport (px) para los scripts de scroll.</summary>
    public int InnerHeight { get; set; } = 800;

    /// <summary>Posición vertical simulada del scroll (px).</summary>
    public int CurrentScrollY { get; private set; }

    /// <summary>Tablas (id + contenido) visibles en la página de liga o de partido.</summary>
    public List<(string Id, string Content)> Tables { get; set; } = [];

    /// <summary>Ids de tablas que "desaparecen" del DOM (producen NoSuchElementException).</summary>
    public HashSet<string> MissingTableIds { get; } = [];

    /// <summary>Indica si las tablas son visibles; falso para simular un timeout.</summary>
    public bool TablesVisible { get; set; } = true;

    /// <summary>Estado devuelto por <c>document.readyState</c>.</summary>
    public string ReadyState { get; set; } = "complete";

    /// <summary>URLs visitadas mediante GoToUrl, en orden.</summary>
    public List<string> VisitedUrls { get; } = [];

    /// <summary>Cantidad de veces que se navegó hacia atrás.</summary>
    public int NavigationsBack { get; private set; }

    /// <summary>Scripts ejecutados mediante ExecuteScript, en orden (p. ej. scrolls).</summary>
    public List<string> ExecutedScripts { get; } = [];

    /// <summary>Indica si se invocó Quit().</summary>
    public bool QuitCalled { get; private set; }

    public string Url { get; set; } = "https://sitio-de-prueba.test/liga/primeraliga";

    public string Title => "Liga de Prueba";

    public string PageSource => string.Empty;

    public string CurrentWindowHandle => "handle-1";

    public ReadOnlyCollection<string> WindowHandles { get; } = ["handle-1"];

    public IOptions Manage() => throw new NotImplementedException();

    public INavigation Navigate() => new FakeNavigation(RecordGoToUrl, OnNavigatedBack);

    public ITargetLocator SwitchTo() => throw new NotImplementedException();

    public void Close() { }

    public void Quit() => QuitCalled = true;

    public object? ExecuteScript(string script, params object?[]? args)
    {
        ExecutedScripts.Add(script);

        if (script.Contains("readyState"))
        {
            return ReadyState;
        }

        // Simular la geometría de scroll de una página real.
        if (script.Contains("scrollY + window.innerHeight"))
        {
            int maxScrollY = Math.Max(ScrollHeight - InnerHeight, 0);
            return CurrentScrollY >= maxScrollY;
        }

        if (script.StartsWith("window.scrollBy"))
        {
            int inicio = script.IndexOf('(') + 1;
            int fin = script.IndexOf(')');
            string[] partes = script[inicio..fin].Split(',');
            int pixeles = int.Parse(partes[1].Trim());
            CurrentScrollY = Math.Min(CurrentScrollY + pixeles, Math.Max(ScrollHeight - InnerHeight, 0));
            return null;
        }

        if (script.StartsWith("window.scrollTo"))
        {
            CurrentScrollY = Math.Max(ScrollHeight - InnerHeight, 0);
            return null;
        }

        return null;
    }

    public object? ExecuteScript(PinnedScript script, params object?[]? args) =>
        ExecuteScript(script.Source, args);

    public object? ExecuteAsyncScript(string script, params object?[]? args) => null;

    public IWebElement FindElement(By by)
    {
        var candidates = FindElements(by);
        return candidates.Count > 0 ? candidates[0] : throw new NoSuchElementException($"No match: {by}");
    }

    public ReadOnlyCollection<IWebElement> FindElements(By by)
    {
        if (by.Mechanism != "css selector")
        {
            return [];
        }

        // Enlaces de jugadores en la página de liga.
        // (se comprueba antes que el selector por id: también empieza por '#')
        if (by.Criteria.Contains("section_heading_text"))
        {
            return [.. PlayerLinkHrefs.Select(href => (IWebElement)new FakeWebElement($"enlace {href}", href: href))];
        }

        if (by.Criteria == "table[id]")
        {
            return TablesVisible ? [.. ListTables()] : [];
        }

        if (by.Criteria.Contains("gridtitle"))
        {
            return [.. LeagueNames.Select(name => (IWebElement)new FakeWebElement(name))];
        }

        // En Selenium 4.49, By.Id se materializa como selector CSS "#id".
        if (by.Criteria is ['#', .. var elementId])
        {
            // Selenium escapa caracteres especiales (p. ej. "-" como "\-") al materializar By.Id como CSS.
            elementId = elementId.Replace("\\-", "-");

            // La tabla de partidos puede "desaparecer" del DOM para simular timeouts.
            if (elementId == "matchlogs_for" && !MatchLogsVisible)
            {
                return [];
            }

            if (_tablesById.TryGetValue(elementId, out var element))
            {
                return [element];
            }

            return ExtraElementsById.TryGetValue(elementId, out var content)
                ? [new FakeWebElement($"div {elementId}", elementId, innerText: content)]
                : [];
        }

        // Selectores por clase (p. ej. "div.scorebox"): el nombre de la clase
        // actúa como clave para localizar el div en ExtraElementsById.
        if (by.Criteria.StartsWith("div.", StringComparison.Ordinal) && !by.Criteria.Contains(' '))
        {
            string className = by.Criteria["div.".Length..];

            return ExtraElementsById.TryGetValue(className, out var content)
                ? [new FakeWebElement($"div {className}", className, innerText: content)]
                : [];
        }

        return [];
    }

    /// <summary>Prepara las tablas de la "página de liga". Los ids presentes en MissingTableIds se listan pero no se pueden localizar (simula DOM stale).</summary>
    public void PrepareTables()
    {
        _tablesById.Clear();

        foreach (var (id, content) in Tables)
        {
            if (!MissingTableIds.Contains(id))
            {
                _tablesById[id] = id == "matchlogs_for"
                    ? new FakeWebElement($"tabla {id}", id, content).WithMatchLogRows(MatchReportHrefs, MatchLinkTexts)
                    : new FakeWebElement($"tabla {id}", id, content);
            }
        }
    }

    /// <summary>Crea los elementos de listado de tablas. La primera tabla expone las filas de la tabla de posiciones.</summary>
    private IEnumerable<IWebElement> ListTables() =>
        Tables.Select((t, index) =>
        {
            var element = new FakeWebElement($"tabla {t.Id}", t.Id);
            return index == 0 && StandingsRows.Count > 0
                ? element.WithStandingsRows(StandingsRows)
                : (IWebElement)element;
        });

    private void RecordGoToUrl(string url)
    {
        VisitedUrls.Add(url);
        CurrentScrollY = 0; // Una página nueva comienza en la parte superior.
    }

    private void OnNavigatedBack()
    {
        NavigationsBack++;
        CurrentScrollY = 0; // Al volver atrás la página se recarga desde el principio.
    }

    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private sealed class FakeNavigation(Action<string> onGoToUrl, Action onBack) : INavigation
    {
        public void Back() => onBack();

        public void Forward() { }

        public void GoToUrl(string url) => onGoToUrl(url);

        public void GoToUrl(Uri url) => onGoToUrl(url.ToString());

        public void Refresh() { }

        public Task BackAsync() { onBack(); return Task.CompletedTask; }

        public Task ForwardAsync() => Task.CompletedTask;

        public Task GoToUrlAsync(string url) { onGoToUrl(url); return Task.CompletedTask; }

        public Task GoToUrlAsync(Uri url) { onGoToUrl(url.ToString()); return Task.CompletedTask; }

        public Task RefreshAsync() => Task.CompletedTask;
    }
}

/// <summary>Extensión interna para construir elementos falsos con búsqueda relativa.</summary>
internal static class FakeWebElementExtensions
{
    /// <summary>
    /// Configura la primera tabla para responder como tabla de posiciones: filas
    /// (XPath ".//tr") con el enlace y la celda del equipo.
    /// </summary>
    public static FakeWebElement WithStandingsRows(this FakeWebElement element, List<FakeStandingsRow> rows)
    {
        element.SetFindElementsHandler(criteria => criteria == ".//tr"
            ? [.. rows.Select(row => (IWebElement)new FakeWebElement(
                row.TeamName,
                findElementsHandler: rowCriteria =>
                {
                    // Enlace del equipo dentro de la fila (celda estándar o primer td).
                    if (rowCriteria.Contains("'team'") || rowCriteria.Contains("first-of-type"))
                    {
                        return [new FakeWebElement(row.TeamName, href: row.TeamHref)];
                    }

                    // Celdas de la fila (nombre del equipo vía primer td).
                    if (rowCriteria == "td")
                    {
                        return [new FakeWebElement(row.TeamName)];
                    }

                    return null;
                }))]
            : null);
        return element;
    }

    /// <summary>
    /// Configura el elemento para responder como tabla 'matchlogs_for': filas con
    /// atributo data-row (selector MatchLogDataRows) que contienen el enlace
    /// "Match Report" en la celda data-stat="match_report". Simula también el
    /// filtro de texto del XPath: filas con texto distinto de "Match Report"
    /// (partidos futuros) no devuelven enlace.
    /// </summary>
    public static FakeWebElement WithMatchLogRows(this FakeWebElement element, List<string> hrefs, List<string>? texts = null)
    {
        element.SetFindElementsHandler(criteria => criteria == ".//tr[@data-row]"
            ? [.. hrefs.Select((href, index) =>
            {
                string texto = texts is not null && index < texts.Count ? texts[index] : "Match Report";
                return (IWebElement)new FakeWebElement(
                    texto,
                    dataRow: index.ToString(),
                    findElementsHandler: rowCriteria =>
                        rowCriteria.Contains("match_report") && string.Equals(texto.Trim(), "Match Report", StringComparison.Ordinal)
                            ? [new FakeWebElement(texto, href: href)]
                            : null);
            })]
            : null);
        return element;
    }
}

using System.Collections.ObjectModel;
using OpenQA.Selenium;

namespace WinTopGun.Tests.Fakes;

/// <summary>
/// Implementación mínima de <see cref="IWebDriver"/> que simula una página principal
/// con N ligas y páginas de liga con M tablas con <c>id</c>. Permite ejercitar el
/// algoritmo de <c>LeagueScrapingService</c> sin un navegador real.
/// </summary>
internal sealed class FakeWebDriver : IWebDriver
{
    private readonly Dictionary<string, IWebElement> _tablesById = [];

    /// <summary>Enlaces de ligas visibles en la página principal.</summary>
    public List<string> LeagueNames { get; set; } = ["Liga A", "Liga B"];

    /// <summary>Tablas (id + contenido) visibles en la página de liga.</summary>
    public List<(string Id, string Content)> Tables { get; set; } = [];

    /// <summary>Ids de tablas que "desaparecen" del DOM (producen NoSuchElementException).</summary>
    public HashSet<string> MissingTableIds { get; } = [];

    /// <summary>Indica si las tablas son visibles; falso para simular un timeout.</summary>
    public bool TablesVisible { get; set; } = true;

    /// <summary>Cantidad de veces que se navegó hacia atrás.</summary>
    public int NavigationsBack { get; private set; }

    /// <summary>Indica si se invocó Quit().</summary>
    public bool QuitCalled { get; private set; }

    public string Url { get; set; } = "https://sitio-de-prueba.test/liga/primeraliga";

    public string Title => "Liga de Prueba";

    public string PageSource => string.Empty;

    public string CurrentWindowHandle => "handle-1";

    public ReadOnlyCollection<string> WindowHandles { get; } = ["handle-1"];

    public IOptions Manage() => throw new NotImplementedException();

    public INavigation Navigate() => new FakeNavigation(() => NavigationsBack++);

    public ITargetLocator SwitchTo() => throw new NotImplementedException();

    public void Close() { }

    public void Quit() => QuitCalled = true;

    public IWebElement FindElement(By by)
    {
        var candidates = FindElements(by);
        return candidates.Count > 0 ? candidates[0] : throw new NoSuchElementException($"No match: {by}");
    }

    public ReadOnlyCollection<IWebElement> FindElements(By by)
    {
        if (by.Mechanism == "css selector" && by.Criteria == "table[id]")
        {
            return TablesVisible ? [.. ListTables()] : [];
        }

        if (by.Mechanism == "css selector" && by.Criteria.Contains("gridtitle"))
        {
            return [.. LeagueNames.Select(name => (IWebElement)new FakeWebElement(name))];
        }

        // En Selenium 4.49, By.Id se materializa como selector CSS "#id".
        if (by.Mechanism == "css selector" && by.Criteria is ['#', .. var tableId])
        {
            return _tablesById.TryGetValue(tableId, out var element)
                ? [element]
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
                _tablesById[id] = new FakeWebElement($"tabla {id}", id, content);
            }
        }
    }

    /// <summary>Crea los elementos de listado de tablas (todos, incluidos los stale).</summary>
    private IEnumerable<IWebElement> ListTables() =>
        Tables.Select(t => (IWebElement)new FakeWebElement($"tabla {t.Id}", t.Id));

    public void Dispose() { }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private sealed class FakeNavigation(Action onBack) : INavigation
    {
        public void Back() => onBack();

        public void Forward() { }

        public void GoToUrl(string url) { }

        public void GoToUrl(Uri url) { }

        public void Refresh() { }

        public Task BackAsync() { onBack(); return Task.CompletedTask; }

        public Task ForwardAsync() => Task.CompletedTask;

        public Task GoToUrlAsync(string url) => Task.CompletedTask;

        public Task GoToUrlAsync(Uri url) => Task.CompletedTask;

        public Task RefreshAsync() => Task.CompletedTask;
    }
}

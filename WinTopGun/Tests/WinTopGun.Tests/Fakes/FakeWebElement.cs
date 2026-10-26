using System.Collections.ObjectModel;
using OpenQA.Selenium;

namespace WinTopGun.Tests.Fakes;

/// <summary>
/// Implementación mínima de <see cref="IWebElement"/> para pruebas unitarias.
/// </summary>
internal sealed class FakeWebElement(
    string text,
    string? id = null,
    string? innerText = null,
    string? outerHtml = null,
    string? href = null,
    string? dataRow = null,
    Func<string, ReadOnlyCollection<IWebElement>?>? findElementsHandler = null) : IWebElement
{
    private Func<string, ReadOnlyCollection<IWebElement>?>? _findElementsHandler = findElementsHandler;

    public int Clicks { get; private set; }

    public string Text => text;

    public string TagName => "a";

    public bool Enabled => true;

    public bool Selected => false;

    public bool Displayed => true;

    public System.Drawing.Point Location => new(0, 0);

    public System.Drawing.Size Size => new(0, 0);

    public void Click() => Clicks++;

    public void SendKeys(string text) => throw new NotImplementedException();

    public void Submit() => throw new NotImplementedException();

    public void Clear() => throw new NotImplementedException();

    public string GetAttribute(string attributeName) => attributeName switch
    {
        "id" when id is not null => id,
        "href" when href is not null => href,
        "data-row" when dataRow is not null => dataRow,
        _ => throw new NoSuchElementException($"Atributo '{attributeName}' no disponible."),
    };

    public string GetDomProperty(string propertyName) => propertyName switch
    {
        "innerText" => innerText ?? throw new NotImplementedException(),
        "outerHTML" => outerHtml ?? throw new NotImplementedException(),
        _ => throw new NotImplementedException(),
    };

    public string GetDomAttribute(string attributeName) => throw new NotImplementedException();

    public string GetCssValue(string propertyName) => throw new NotImplementedException();

    public ISearchContext GetShadowRoot() => throw new NotImplementedException();

    public IWebElement FindElement(By by)
    {
        var elements = FindElements(by);
        return elements.Count > 0 ? elements[0] : throw new NoSuchElementException($"No match: {by}");
    }

    public ReadOnlyCollection<IWebElement> FindElements(By by)
    {
        var result = _findElementsHandler?.Invoke(by.Criteria);
        return result ?? [];
    }

    /// <summary>Configura la búsqueda relativa del elemento (para pruebas del proceso de partidos).</summary>
    public void SetFindElementsHandler(Func<string, ReadOnlyCollection<IWebElement>?> handler) =>
        _findElementsHandler = handler;
}

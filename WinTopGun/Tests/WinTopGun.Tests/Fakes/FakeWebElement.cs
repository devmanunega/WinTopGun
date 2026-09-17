using System.Collections.ObjectModel;
using OpenQA.Selenium;

namespace WinTopGun.Tests.Fakes;

/// <summary>
/// Implementación mínima de <see cref="IWebElement"/> para pruebas unitarias.
/// </summary>
internal sealed class FakeWebElement(string text, string? id = null, string? innerText = null) : IWebElement
{
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

    public string GetAttribute(string attributeName) =>
        attributeName == "id" ? (id ?? throw new NoSuchElementException()) : throw new NotImplementedException();

    public string GetDomProperty(string propertyName) =>
        propertyName == "innerText" ? (innerText ?? throw new NotImplementedException()) : throw new NotImplementedException();

    public string GetDomAttribute(string attributeName) => throw new NotImplementedException();

    public string GetCssValue(string propertyName) => throw new NotImplementedException();

    public ISearchContext GetShadowRoot() => throw new NotImplementedException();

    public IWebElement FindElement(By by) => throw new NoSuchElementException();

    public ReadOnlyCollection<IWebElement> FindElements(By by) => [];
}

using OpenQA.Selenium;

namespace WinTopGun.Application.Interfaces;

/// <summary>
/// Fábrica de drivers de navegador. Permite desacoplar el scraping de la
/// tecnología concreta (Chrome, Firefox, ...) y facilita el uso de fakes en pruebas.
/// </summary>
public interface IDriverFactory
{
    /// <summary>Crea una instancia configurada de <see cref="IWebDriver"/>.</summary>
    IWebDriver CreateDriver();
}

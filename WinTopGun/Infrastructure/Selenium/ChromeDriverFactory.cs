using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using WinTopGun.Application.Interfaces;
using WinTopGun.Application.Services;

namespace WinTopGun.Infrastructure.Selenium;

/// <summary>
/// Crea instancias de <see cref="ChromeDriver"/> configuradas para el scraping.
/// No oculta los errores de arranque del driver: se propagan con su detalle original.
/// </summary>
public sealed class ChromeDriverFactory : IDriverFactory
{
    private readonly ScrapingOptions _options;
    private readonly ILogger<ChromeDriverFactory> _logger;

    public ChromeDriverFactory(ScrapingOptions options, ILogger<ChromeDriverFactory> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public IWebDriver CreateDriver()
    {
        _logger.LogDebug("Creando ChromeDriver (debugger: {DebuggerAddress})",
            _options.ChromeDebuggerAddress ?? "(autónomo)");

        var options = new ChromeOptions();

        if (!string.IsNullOrWhiteSpace(_options.ChromeDebuggerAddress))
        {
            options.DebuggerAddress = _options.ChromeDebuggerAddress;
        }

        options.PageLoadStrategy = PageLoadStrategy.Eager;
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-infobars");

        return new ChromeDriver(options);
    }
}

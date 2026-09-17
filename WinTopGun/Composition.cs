using System.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WinTopGun.Application.Interfaces;
using WinTopGun.Application.Services;
using WinTopGun.Infrastructure.Configuration;
using WinTopGun.Infrastructure.Logging;
using WinTopGun.Infrastructure.Persistence;
using WinTopGun.Infrastructure.Selenium;

namespace WinTopGun;

/// <summary>
/// Punto único de composición de dependencias de la aplicación
/// (Composition Root). Registra las implementaciones concretas de cada interfaz.
/// </summary>
public static class Composition
{
    /// <summary>Contenedor de servicios de la aplicación.</summary>
    public static IServiceProvider Services { get; } = BuildServiceProvider();

    private static IServiceProvider BuildServiceProvider()
    {
        string logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinTopGun",
            "logs");

        return new ServiceCollection()
            .AddSingleton(LoadScrapingOptions())
            .AddSingleton<ISettingsProvider, AppSettingsProvider>()
            .AddSingleton<IDriverFactory, ChromeDriverFactory>()
            .AddSingleton<ITableExporter, TextFileTableExporter>()
            .AddSingleton<IScrapingService, LeagueScrapingService>()
            .AddLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Information);
                logging.AddProvider(new FileLoggerProvider(logDirectory));
            })
            .BuildServiceProvider();
    }

    private static ScrapingOptions LoadScrapingOptions() => new()
    {
        WaitTimeoutSeconds = ReadIntSetting(ScrapingOptions.WaitTimeoutKey, ScrapingOptions.DefaultWaitTimeoutSeconds),
        ChromeDebuggerAddress = ConfigurationManager.AppSettings[ScrapingOptions.DebuggerAddressKey]
            ?? ScrapingOptions.DefaultDebuggerAddress,
    };

    private static int ReadIntSetting(string key, int fallback) =>
        int.TryParse(ConfigurationManager.AppSettings[key], out int value) && value > 0 ? value : fallback;
}

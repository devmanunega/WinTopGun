namespace WinTopGun.Application.Services;

/// <summary>
/// Opciones configurables del proceso de scraping. Sus valores se cargan
/// desde <c>App.config</c> (sección <c>appSettings</c>).
/// </summary>
public sealed class ScrapingOptions
{
    /// <summary>Clave de configuración para <see cref="WaitTimeoutSeconds"/>.</summary>
    public const string WaitTimeoutKey = "Scraping:WaitTimeoutSeconds";

    /// <summary>Clave de configuración para <see cref="ChromeDebuggerAddress"/>.</summary>
    public const string DebuggerAddressKey = "Scraping:ChromeDebuggerAddress";

    /// <summary>Valor por defecto del tiempo de espera.</summary>
    public const int DefaultWaitTimeoutSeconds = 10;

    /// <summary>Valor por defecto de la dirección de depuración de Chrome.</summary>
    public const string DefaultDebuggerAddress = "localhost:9222";

    /// <summary>Segundos máximos de espera explícita al cargar páginas o elementos.</summary>
    public int WaitTimeoutSeconds { get; set; } = DefaultWaitTimeoutSeconds;

    /// <summary>Dirección de depuración remota de Chrome (ej. <c>localhost:9222</c>). Nulo para iniciar Chrome de forma autónoma.</summary>
    public string? ChromeDebuggerAddress { get; set; } = DefaultDebuggerAddress;
}

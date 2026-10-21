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

    /// <summary>Clave de configuración para <see cref="HumanPauseMinMilliseconds"/>.</summary>
    public const string HumanPauseMinKey = "Scraping:HumanPauseMinMs";

    /// <summary>Clave de configuración para <see cref="HumanPauseMaxMilliseconds"/>.</summary>
    public const string HumanPauseMaxKey = "Scraping:HumanPauseMaxMs";

    /// <summary>Clave de configuración para <see cref="MatchPauseMinMilliseconds"/>.</summary>
    public const string MatchPauseMinKey = "Scraping:MatchPauseMinMs";

    /// <summary>Clave de configuración para <see cref="MatchPauseMaxMilliseconds"/>.</summary>
    public const string MatchPauseMaxKey = "Scraping:MatchPauseMaxMs";

    /// <summary>Valor por defecto del tiempo de espera (1 minuto para la aparición de elementos).</summary>
    public const int DefaultWaitTimeoutSeconds = 60;

    /// <summary>Valor por defecto de la dirección de depuración de Chrome.</summary>
    public const string DefaultDebuggerAddress = "localhost:9222";

    /// <summary>Valor por defecto del límite inferior de la pausa humana (ms): 10 segundos antes de hacer click.</summary>
    public const int DefaultHumanPauseMinMs = 10000;

    /// <summary>Valor por defecto del límite superior de la pausa humana (ms): 20 segundos antes de hacer click.</summary>
    public const int DefaultHumanPauseMaxMs = 20000;

    /// <summary>Valor por defecto del límite inferior de la pausa en páginas de partido (ms).</summary>
    public const int DefaultMatchPauseMinMs = 3000;

    /// <summary>Valor por defecto del límite superior de la pausa en páginas de partido (ms).</summary>
    public const int DefaultMatchPauseMaxMs = 8000;

    /// <summary>Segundos máximos de espera explícita al cargar páginas o elementos.</summary>
    public int WaitTimeoutSeconds { get; set; } = DefaultWaitTimeoutSeconds;

    /// <summary>Dirección de depuración remota de Chrome (ej. <c>localhost:9222</c>). Nulo para iniciar Chrome de forma autónoma.</summary>
    public string? ChromeDebuggerAddress { get; set; } = DefaultDebuggerAddress;

    /// <summary>Límite inferior (ms) de la pausa aleatoria tras la carga completa de una página, para simular navegación humana. 0 o negativo desactiva la pausa.</summary>
    public int HumanPauseMinMilliseconds { get; set; } = DefaultHumanPauseMinMs;

    /// <summary>Límite superior (ms) de la pausa aleatoria tras la carga completa de una página, para simular navegación humana.</summary>
    public int HumanPauseMaxMilliseconds { get; set; } = DefaultHumanPauseMaxMs;

    /// <summary>Límite inferior (ms) de la pausa aleatoria en la página de un partido, antes de extraer los datos. 0 o negativo desactiva la pausa.</summary>
    public int MatchPauseMinMilliseconds { get; set; } = DefaultMatchPauseMinMs;

    /// <summary>Límite superior (ms) de la pausa aleatoria en la página de un partido, antes de extraer los datos.</summary>
    public int MatchPauseMaxMilliseconds { get; set; } = DefaultMatchPauseMaxMs;
}

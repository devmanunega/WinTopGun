namespace WinTopGun.Domain.Models;

/// <summary>
/// Resultado consolidado de una ejecución de extracción.
/// </summary>
public sealed class ScrapingResult
{
    /// <summary>Cantidad de ligas procesadas completamente.</summary>
    public int LeaguesProcessed { get; internal set; }

    /// <summary>Cantidad de tablas exportadas a disco.</summary>
    public int TablesExported { get; internal set; }

    /// <summary>Descripción de los errores no fatales encontrados durante la ejecución.</summary>
    public List<string> Errors { get; } = [];

    /// <summary>Indica si la ejecución fue cancelada por el usuario.</summary>
    public bool WasCancelled { get; internal set; }

    /// <summary>Indica si la ejecución terminó sin errores ni cancelaciones.</summary>
    public bool Success => Errors.Count == 0 && !WasCancelled;
}

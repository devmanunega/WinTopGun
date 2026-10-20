namespace WinTopGun.Domain.Models;

/// <summary>
/// Estado completo del registro de progreso del proceso de partidos,
/// persistido en <c>progreso_partidos.json</c>. Lleva el conteo de partidos
/// procesados por equipo para reanudar la extracción desde el siguiente enlace.
/// </summary>
public sealed class MatchProgressState
{
    /// <summary>Versión del formato del registro, para evolucionarlo sin romper compatibilidad.</summary>
    public int Version { get; set; } = CurrentVersion;

    /// <summary>Última actualización del registro (UTC).</summary>
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>Progreso por equipo, indexado por la URL de la página del equipo.</summary>
    public Dictionary<string, TeamProgress> Teams { get; set; } = [];

    /// <summary>Versión actual del formato.</summary>
    public const int CurrentVersion = 1;
}

/// <summary>
/// Progreso de los partidos de un equipo: lista ordenada de los partidos
/// procesados exitosamente. El orden corresponde al orden de procesamiento,
/// que es el mismo orden de aparición en la tabla <c>matchlogs_for</c> del
/// sitio, lo que permite omitir los primeros N enlaces al reanudar.
/// </summary>
public sealed class TeamProgress
{
    /// <summary>Nombre del equipo.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URLs de los partidos procesados exitosamente, en orden de procesamiento.
    /// Los partidos con errores de exportación no se incluyen: se reintentan.
    /// </summary>
    public List<string> ProcessedMatchUrls { get; set; } = [];
}

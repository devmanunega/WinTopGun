namespace WinTopGun.Domain.Models;

/// <summary>
/// Registro de un partido procesado por el proceso de extracción de partidos.
/// Un partido con <see cref="Errors"/> vacío se considera completado; con errores,
/// queda a modo de diagnóstico y será reintentado en la siguiente ejecución.
/// </summary>
public sealed class MatchProgressRecord
{
    /// <summary>URL de la página del equipo al que pertenece el partido.</summary>
    public string TeamUrl { get; set; } = string.Empty;

    /// <summary>Nombre del equipo (diagnóstico).</summary>
    public string TeamName { get; set; } = string.Empty;

    /// <summary>URL del reporte del partido (clave única del registro).</summary>
    public string MatchUrl { get; set; } = string.Empty;

    /// <summary>Último segmento de la URL del partido (diagnóstico y trazabilidad con los archivos exportados).</summary>
    public string Segment { get; set; } = string.Empty;

    /// <summary>Fecha UTC en que se registró el partido.</summary>
    public DateTime ProcessedAtUtc { get; set; }

    /// <summary>Incidencias de exportación; vacío si el partido se completó correctamente.</summary>
    public List<string> Errors { get; set; } = [];
}

/// <summary>
/// Estado completo del registro de progreso del proceso de partidos,
/// persistido en <c>progreso_partidos.json</c>.
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

/// <summary>Progreso de los partidos de un equipo.</summary>
public sealed class TeamProgress
{
    /// <summary>Nombre del equipo.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Partidos procesados, indexados por la URL del reporte del partido.</summary>
    public Dictionary<string, MatchProgressRecord> Matches { get; set; } = [];
}

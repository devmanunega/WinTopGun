namespace WinTopGun.Domain.Models;

/// <summary>
/// Resultado de la depuración de un grupo (subdirectorio de un patrón):
/// verifica que los encabezados de los archivos sean consistentes.
/// </summary>
public sealed class GrupoDepuracion
{
    /// <summary>Patrón del grupo (nombre del subdirectorio analizado).</summary>
    public string Patron { get; init; } = string.Empty;

    /// <summary>Cantidad de archivos analizados en el grupo.</summary>
    public int TotalArchivos { get; internal set; }

    /// <summary>Encabezados distintos detectados en el grupo, con su conteo.</summary>
    public List<string> Encabezados { get; } = [];

    /// <summary>Incidencias detectadas (archivos vacíos, encabezados que no encajan, errores de lectura).</summary>
    public List<string> Incidencias { get; } = [];

    /// <summary>Indica si el grupo no presentó incidencias.</summary>
    public bool Consistente => Incidencias.Count == 0;
}

/// <summary>
/// Informe consolidado de la fase de depuración sobre un directorio organizado.
/// </summary>
public sealed class DepuracionInforme
{
    /// <summary>Directorio raíz analizado.</summary>
    public string Directorio { get; init; } = string.Empty;

    /// <summary>Grupos (subdirectorios) analizados.</summary>
    public List<GrupoDepuracion> Grupos { get; } = [];

    /// <summary>Cantidad total de archivos analizados.</summary>
    public int TotalArchivos => Grupos.Sum(g => g.TotalArchivos);

    /// <summary>Cantidad total de incidencias detectadas.</summary>
    public int TotalIncidencias => Grupos.Sum(g => g.Incidencias.Count);
}

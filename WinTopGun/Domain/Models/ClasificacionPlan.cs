namespace WinTopGun.Domain.Models;

/// <summary>
/// Grupo de archivos que comparten el mismo patrón (destino: un subdirectorio).
/// </summary>
public sealed class GrupoClasificacion
{
    /// <summary>Patrón anclado en la palabra "tabla" (nombre del subdirectorio).</summary>
    public string Patron { get; init; } = string.Empty;

    /// <summary>Nombres de archivo (con extensión) que pertenecen al grupo.</summary>
    public List<string> Archivos { get; } = [];

    /// <summary>Indica si el grupo tiene un solo archivo (advertencia: posible token de ruido no detectado).</summary>
    public bool Sospechoso => Archivos.Count == 1;

    /// <summary>Indica si el subdirectorio destino ya existía antes de ejecutar el plan.</summary>
    public bool DirectorioExistente { get; internal set; }
}

/// <summary>
/// Plan de organización generado a partir del inventario de un directorio:
/// agrupa los archivos por patrón y define los movimientos a ejecutar.
/// </summary>
public sealed class ClasificacionPlan
{
    /// <summary>Directorio raíz analizado.</summary>
    public string DirectorioOrigen { get; init; } = string.Empty;

    /// <summary>Grupos de archivos por patrón (cada grupo es un subdirectorio).</summary>
    public List<GrupoClasificacion> Grupos { get; } = [];

    /// <summary>Archivos sin ancla "tabla": no se mueven.</summary>
    public List<string> SinClasificar { get; } = [];

    /// <summary>Archivos ignorados por el inventario (Consolidado, temporales, extensión no permitida).</summary>
    public List<string> Ignorados { get; } = [];

    /// <summary>Cantidad total de archivos que el plan moverá.</summary>
    public int TotalArchivosAMover => Grupos.Sum(g => g.Archivos.Count);
}

/// <summary>
/// Resultado consolidado de la ejecución de un <see cref="ClasificacionPlan"/>.
/// </summary>
public sealed class ClasificacionResultado
{
    /// <summary>Cantidad de archivos movidos a su subdirectorio.</summary>
    public int ArchivosMovidos { get; internal set; }

    /// <summary>Cantidad de subdirectorios creados.</summary>
    public int DirectoriosCreados { get; internal set; }

    /// <summary>Descripción de incidencias (colisiones en destino, errores de E/S).</summary>
    public List<string> Errors { get; } = [];

    /// <summary>Indica si la ejecución terminó sin errores.</summary>
    public bool Success => Errors.Count == 0;
}

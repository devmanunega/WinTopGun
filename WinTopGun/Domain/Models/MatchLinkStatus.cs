namespace WinTopGun.Domain.Models;

/// <summary>
/// Estado de un enlace de partido dentro del registro persistente.
/// </summary>
public enum MatchLinkStatus
{
    /// <summary>Enlace descubierto, aún no procesado.</summary>
    Pending,

    /// <summary>Enlace procesado exitosamente; nunca se vuelve a procesar.</summary>
    Processed,

    /// <summary>El procesamiento terminó con incidencias; se reintenta en la próxima ejecución.</summary>
    Failed,
}

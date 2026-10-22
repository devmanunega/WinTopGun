using WinTopGun.Domain.Models;

namespace WinTopGun.Application.Interfaces;

/// <summary>
/// Estructura a nivel de aplicación que registra los enlaces de partido y su
/// estado (Pending / Processed / Failed), persistida en disco para garantizar
/// que, ante una interrupción, solo se procesen los partidos pendientes.
/// La clave es el <see cref="MatchLinkTarget.MatchId"/> (ID único del partido).
/// Ver Ideas/Partidos.md.
/// </summary>
public interface IMatchLinkRegistry
{
    /// <summary>Cantidad de enlaces en estado Processed.</summary>
    int ProcessedCount { get; }

    /// <summary>
    /// Carga (o inicializa) la estructura de registro desde el directorio indicado.
    /// Debe invocarse antes de iniciar un proceso de extracción (fase 0).
    /// </summary>
    /// <param name="outputDirectory">Directorio donde reside el archivo de registro.</param>
    void Initialize(string outputDirectory);

    /// <summary>
    /// Da de alta un enlace descubierto (fase 1.2). Si es nuevo entra como
    /// <see cref="MatchLinkStatus.Pending"/>; si ya existe, <b>no se sobrescribe
    /// su estado</b> (error común que reprocesaría todo).
    /// </summary>
    /// <param name="target">Enlace descubierto.</param>
    void Register(MatchLinkTarget target);

    /// <summary>
    /// Indica si un enlace está en estado <see cref="MatchLinkStatus.Processed"/>
    /// (fases 1.2.2 y 2.1): solo entonces se omite su procesamiento.
    /// Los enlaces Pending y Failed se (re)procesan.
    /// </summary>
    /// <param name="matchId">Identificador único del partido.</param>
    bool IsProcessed(string matchId);

    /// <summary>
    /// Marca un enlace como <see cref="MatchLinkStatus.Processed"/> y persiste el
    /// cambio de inmediato (escritura atómica). Debe invocarse solo tras una
    /// extracción exitosa (fase 2.4).
    /// </summary>
    /// <param name="target">Enlace procesado.</param>
    void MarkProcessed(MatchLinkTarget target);

    /// <summary>
    /// Marca un enlace como <see cref="MatchLinkStatus.Failed"/> y persiste el
    /// cambio de inmediato. Los fallidos se reintentan en la próxima ejecución.
    /// </summary>
    /// <param name="target">Enlace cuyo procesamiento terminó con incidencias.</param>
    void MarkFailed(MatchLinkTarget target);

    /// <summary>
    /// Elimina el registro de enlaces (vuelve a empezar de cero: la próxima
    /// extracción reprocesará todos los partidos).
    /// </summary>
    void Reset();
}

using WinTopGun.Domain.Models;

namespace WinTopGun.Application.Interfaces;

/// <summary>
/// Registro de progreso del proceso de partidos: permite reanudar la extracción
/// procesando únicamente los partidos pendientes de cada equipo.
/// </summary>
public interface IMatchProgressRepository
{
    /// <summary>
    /// Carga el estado completo del registro. Si el archivo no existe devuelve un
    /// estado vacío; si está corrupto, lo aparta y devuelve un estado vacío.
    /// </summary>
    MatchProgressState Load();

    /// <summary>
    /// Registra un partido y persiste el registro de inmediato de forma atómica
    /// (escritura temporal + reemplazo), de modo que un fallo no lo corrompa.
    /// </summary>
    void MarkMatchProcessed(MatchProgressRecord record);

    /// <summary>
    /// Indica si un partido ya fue procesado <b>exitosamente</b>: los partidos con
    /// errores de exportación no cuentan como procesados y se reintentarán.
    /// </summary>
    bool IsProcessed(string teamUrl, string matchUrl);

    /// <summary>Elimina el registro de progreso (vuelve a empezar de cero).</summary>
    void Reset();
}

/// <summary>
/// Fábrica de repositorios de progreso: el directorio de detalles de partidos solo
/// se conoce en tiempo de ejecución (depende del directorio de salida del usuario).
/// </summary>
public interface IMatchProgressRepositoryFactory
{
    /// <summary>Crea un repositorio de progreso para el directorio de detalles indicado.</summary>
    IMatchProgressRepository Create(string matchDetailsDirectory);
}

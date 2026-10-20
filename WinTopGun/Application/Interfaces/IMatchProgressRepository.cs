using WinTopGun.Domain.Models;

namespace WinTopGun.Application.Interfaces;

/// <summary>
/// Registro de progreso del proceso de partidos: lleva el conteo de partidos
/// procesados por equipo para reanudar la extracción desde el siguiente enlace.
/// </summary>
public interface IMatchProgressRepository
{
    /// <summary>
    /// Carga el estado completo del registro. Si el archivo no existe devuelve un
    /// estado vacío; si está corrupto, lo aparta y devuelve un estado vacío.
    /// </summary>
    MatchProgressState Load();

    /// <summary>
    /// Registra un partido como procesado para el equipo indicado y persiste el
    /// registro de inmediato de forma atómica (escritura temporal + reemplazo).
    /// </summary>
    void MarkMatchProcessed(string teamUrl, string teamName, string matchUrl);

    /// <summary>
    /// Devuelve las URLs de los partidos procesados exitosamente del equipo, en el
    /// orden en que fueron procesados. Los partidos con errores no se incluyen.
    /// </summary>
    IReadOnlyList<string> GetProcessedMatchUrls(string teamUrl);

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

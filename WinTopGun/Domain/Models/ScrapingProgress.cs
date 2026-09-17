namespace WinTopGun.Domain.Models;

/// <summary>
/// Estado de avance de un proceso de extracción, usado para actualizar la UI
/// mediante <see cref="Progress{T}"/>.
/// </summary>
/// <param name="CurrentLeague">Liga en proceso (1-based).</param>
/// <param name="TotalLeagues">Total de ligas detectadas.</param>
/// <param name="CurrentTable">Tabla en proceso (1-based); 0 si aún no inicia la fase de tablas.</param>
/// <param name="TotalTables">Total de tablas de la liga actual.</param>
/// <param name="Message">Mensaje legible para mostrar al usuario.</param>
public sealed record ScrapingProgress(
    int CurrentLeague,
    int TotalLeagues,
    int CurrentTable,
    int TotalTables,
    string Message)
{
    /// <summary>Crea un reporte de avance a nivel de liga.</summary>
    public static ScrapingProgress ForLeague(int currentLeague, int totalLeagues, string leagueName) =>
        new(currentLeague, totalLeagues, 0, 0, $"Liga {currentLeague}/{totalLeagues}: {leagueName}");

    /// <summary>Crea un reporte de avance a nivel de tabla.</summary>
    public static ScrapingProgress ForTable(int currentLeague, int totalLeagues, int currentTable, int totalTables) =>
        new(currentLeague, totalLeagues, currentTable, totalTables, $"Liga {currentLeague}/{totalLeagues} — Tabla {currentTable}/{totalTables}");
}

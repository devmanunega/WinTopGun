using WinTopGun.Application.Interfaces;
using WinTopGun.Domain.Models;

namespace WinTopGun.Tests.Fakes;

/// <summary>
/// Registro de progreso en memoria para pruebas del proceso de partidos.
/// </summary>
internal sealed class InMemoryMatchProgressRepository : IMatchProgressRepository
{
    public MatchProgressState State { get; } = new();

    public int Resets { get; private set; }

    public MatchProgressState Load() => State;

    public void MarkMatchProcessed(string teamUrl, string teamName, string matchUrl)
    {
        if (!State.Teams.TryGetValue(teamUrl, out var equipo))
        {
            equipo = new TeamProgress();
            State.Teams[teamUrl] = equipo;
        }

        equipo.Name = teamName;

        if (!equipo.ProcessedMatchUrls.Contains(matchUrl, StringComparer.OrdinalIgnoreCase))
        {
            equipo.ProcessedMatchUrls.Add(matchUrl);
        }
    }

    public IReadOnlyList<string> GetProcessedMatchUrls(string teamUrl) =>
        State.Teams.TryGetValue(teamUrl, out var equipo)
            ? equipo.ProcessedMatchUrls
            : [];

    public void Reset()
    {
        Resets++;
        State.Teams.Clear();
    }
}

/// <summary>Fábrica que siempre devuelve el mismo repositorio en memoria.</summary>
internal sealed class InMemoryMatchProgressRepositoryFactory : IMatchProgressRepositoryFactory
{
    public InMemoryMatchProgressRepository Repository { get; } = new();

    public IMatchProgressRepository Create(string matchDetailsDirectory) => Repository;
}

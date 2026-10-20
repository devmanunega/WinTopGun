using System.Collections.ObjectModel;
using OpenQA.Selenium;
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

    public void MarkMatchProcessed(MatchProgressRecord record)
    {
        record.ProcessedAtUtc = DateTime.UtcNow;

        if (!State.Teams.TryGetValue(record.TeamUrl, out var equipo))
        {
            equipo = new TeamProgress();
            State.Teams[record.TeamUrl] = equipo;
        }

        equipo.Name = record.TeamName;
        equipo.Matches[record.MatchUrl] = record;
    }

    public bool IsProcessed(string teamUrl, string matchUrl) =>
        State.Teams.TryGetValue(teamUrl, out var equipo)
        && equipo.Matches.TryGetValue(matchUrl, out var partido)
        && partido.Errors.Count == 0;

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

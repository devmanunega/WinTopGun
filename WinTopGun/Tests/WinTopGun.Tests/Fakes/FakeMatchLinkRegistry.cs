using WinTopGun.Application.Interfaces;
using WinTopGun.Domain.Models;

namespace WinTopGun.Tests.Fakes;

/// <summary>
/// Fake de <see cref="IMatchLinkRegistry"/> que mantiene el registro en memoria,
/// sin acceso a disco.
/// </summary>
internal sealed class FakeMatchLinkRegistry : IMatchLinkRegistry
{
    /// <summary>Registro en memoria: MatchId → registro con estado.</summary>
    public Dictionary<string, MatchLinkRecord> Records { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Cantidad de veces que se invocó MarkProcessed.</summary>
    public int MarkProcessedCalls { get; private set; }

    /// <summary>Cantidad de veces que se invocó MarkFailed.</summary>
    public int MarkFailedCalls { get; private set; }

    /// <summary>Último directorio con el que se inicializó el registro.</summary>
    public string? InitializedWith { get; private set; }

    public int ProcessedCount => Records.Values.Count(r => r.Status == MatchLinkStatus.Processed);

    public void Initialize(string outputDirectory) => InitializedWith = outputDirectory;

    public void Register(MatchLinkTarget target)
    {
        // Los existentes no se sobrescriben (regla 1.2 del documento).
        Records.TryAdd(target.MatchId, new MatchLinkRecord(
            target.MatchId, target.Url, target.Team, target.RowIndex,
            MatchLinkStatus.Pending, null));
    }

    public bool IsProcessed(string matchId) =>
        Records.TryGetValue(matchId, out var record) && record.Status == MatchLinkStatus.Processed;

    public void MarkProcessed(MatchLinkTarget target)
    {
        MarkProcessedCalls++;
        Upsert(target, MatchLinkStatus.Processed);
    }

    public void MarkFailed(MatchLinkTarget target)
    {
        MarkFailedCalls++;
        Upsert(target, MatchLinkStatus.Failed);
    }

    public void Reset() => Records.Clear();

    private void Upsert(MatchLinkTarget target, MatchLinkStatus status)
    {
        Records[target.MatchId] = new MatchLinkRecord(
            target.MatchId, target.Url, target.Team, target.RowIndex,
            status, status == MatchLinkStatus.Processed ? DateTime.UtcNow : null);
    }
}

/// <summary>
/// Exportador que falla a partir de la exportación número <c>maxSuccesses + 1</c>.
/// Simula una interrupción del proceso a mitad de ejecución.
/// </summary>
internal sealed class FailAfterExporter(int maxSuccesses) : ITableExporter
{
    /// <summary>Cantidad de exportaciones exitosas realizadas.</summary>
    public int Successes { get; private set; }

    public Task<string> ExportTableAsync(
        string outputDirectory,
        string pageSegment,
        string tableId,
        string content,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Successes >= maxSuccesses)
        {
            throw new IOException($"Fallo simulado al exportar '{tableId}'.");
        }

        Successes++;
        return Task.FromResult(Path.Combine(outputDirectory, $"{pageSegment}_{tableId}.txt"));
    }
}

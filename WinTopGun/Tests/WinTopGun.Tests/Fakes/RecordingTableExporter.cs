using WinTopGun.Application.Interfaces;
using WinTopGun.Domain.Models;

namespace WinTopGun.Tests.Fakes;

/// <summary>
/// Fake de <see cref="ITableExporter"/> que registra las exportaciones en memoria
/// en lugar de escribir a disco.
/// </summary>
internal sealed class RecordingTableExporter : ITableExporter
{
    public List<(string OutputDirectory, string PageSegment, string TableId, string Content)> Exports { get; } = [];

    public List<string> PathsReturned { get; } = [];

    public int FailuresBeforeSuccess { get; set; }

    public Task<string> ExportTableAsync(
        string outputDirectory,
        string pageSegment,
        string tableId,
        string content,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (FailuresBeforeSuccess > 0 && Exports.Count < FailuresBeforeSuccess)
        {
            throw new IOException($"Fallo simulado al exportar '{tableId}'.");
        }

        Exports.Add((outputDirectory, pageSegment, tableId, content));
        string path = Path.Combine(outputDirectory, $"{pageSegment}_tabla_{tableId}.txt");
        PathsReturned.Add(path);
        return Task.FromResult(path);
    }
}

/// <summary>Receptor de progreso síncrono para pruebas.</summary>
internal sealed class RecordingProgress : IProgress<ScrapingProgress>
{
    public List<ScrapingProgress> Reports { get; } = [];

    public void Report(ScrapingProgress value) => Reports.Add(value);
}

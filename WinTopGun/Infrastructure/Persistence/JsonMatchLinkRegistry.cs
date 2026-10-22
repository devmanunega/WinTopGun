using System.Text.Json;
using System.Text.Json.Serialization;
using WinTopGun.Application.Interfaces;
using WinTopGun.Domain.Models;

namespace WinTopGun.Infrastructure.Persistence;

/// <summary>
/// Registro de enlaces de partidos y su estado, persistido en un archivo JSON
/// dentro del directorio de salida (fase 1.2.1 de Ideas/Partidos.md: diccionario
/// en memoria indexado por <c>MatchId</c>, respaldado en disco en JSON).
/// Cada cambio de estado se escribe de inmediato de forma atómica (archivo
/// temporal + reemplazo), garantizando que una interrupción del proceso no
/// pierda el avance ya registrado (fase 2.4).
/// </summary>
public sealed class JsonMatchLinkRegistry : IMatchLinkRegistry
{
    /// <summary>Nombre del archivo de registro dentro del directorio de salida.</summary>
    public const string RegistryFileName = "enlaces_partidos_procesados.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly object _sync = new();
    private Dictionary<string, MatchLinkRecord> _records = new(StringComparer.OrdinalIgnoreCase);
    private string _filePath = string.Empty;

    /// <inheritdoc />
    public int ProcessedCount
    {
        get
        {
            lock (_sync)
            {
                return _records.Values.Count(r => r.Status == MatchLinkStatus.Processed);
            }
        }
    }

    /// <inheritdoc />
    public void Initialize(string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        Directory.CreateDirectory(outputDirectory);
        _filePath = Path.Combine(outputDirectory, RegistryFileName);

        lock (_sync)
        {
            _records = Load();
        }
    }

    /// <inheritdoc />
    public void Register(MatchLinkTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        lock (_sync)
        {
            // 1.2: los enlaces nuevos entran como Pending; los existentes no se
            // sobrescriben (evita reiniciar estados ya consolidados).
            if (!_records.ContainsKey(target.MatchId))
            {
                _records[target.MatchId] = new MatchLinkRecord(
                    target.MatchId, target.Url, target.Team, target.RowIndex,
                    MatchLinkStatus.Pending, ProcessedAtUtc: null);
            }
        }
    }

    /// <inheritdoc />
    public bool IsProcessed(string matchId)
    {
        lock (_sync)
        {
            return _records.TryGetValue(matchId, out var record)
                && record.Status == MatchLinkStatus.Processed;
        }
    }

    /// <inheritdoc />
    public void MarkProcessed(MatchLinkTarget target)
    {
        ChangeStatus(target, MatchLinkStatus.Processed);
    }

    /// <inheritdoc />
    public void MarkFailed(MatchLinkTarget target)
    {
        ChangeStatus(target, MatchLinkStatus.Failed);
    }

    /// <inheritdoc />
    public void Reset()
    {
        lock (_sync)
        {
            _records.Clear();

            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }
    }

    private void ChangeStatus(MatchLinkTarget target, MatchLinkStatus status)
    {
        ArgumentNullException.ThrowIfNull(target);

        lock (_sync)
        {
            MatchLinkRecord previo = _records.GetValueOrDefault(target.MatchId);

            _records[target.MatchId] = new MatchLinkRecord(
                target.MatchId,
                target.Url,
                target.Team,
                target.RowIndex,
                status,
                status == MatchLinkStatus.Processed ? DateTime.UtcNow : previo?.ProcessedAtUtc);

            Save();
        }
    }

    private Dictionary<string, MatchLinkRecord> Load()
    {
        if (!File.Exists(_filePath))
        {
            return new Dictionary<string, MatchLinkRecord>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var records = JsonSerializer.Deserialize<Dictionary<string, MatchLinkRecord>>(
                File.ReadAllText(_filePath), JsonOptions);

            return records is not null
                ? new Dictionary<string, MatchLinkRecord>(records, StringComparer.OrdinalIgnoreCase)
                : [];
        }
        catch (JsonException)
        {
            // Archivo corrupto o ilegible: se continúa con un registro vacío
            // en lugar de abortar el proceso (los enlaces se volverán a procesar).
            return new Dictionary<string, MatchLinkRecord>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void Save()
    {
        string tempPath = $"{_filePath}.tmp";

        using (var stream = File.Create(tempPath))
        {
            JsonSerializer.Serialize(stream, _records, JsonOptions);
        }

        File.Move(tempPath, _filePath, overwrite: true);
    }
}

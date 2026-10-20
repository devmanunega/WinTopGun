using System.Text.Json;
using Microsoft.Extensions.Logging;
using WinTopGun.Application.Interfaces;
using WinTopGun.Domain.Models;

namespace WinTopGun.Infrastructure.Persistence;

/// <summary>
/// Fábrica de <see cref="JsonMatchProgressRepository"/>. Resuelve el logger en el
/// momento de la creación, cuando ya se conoce el directorio de detalles.
/// </summary>
public sealed class JsonMatchProgressRepositoryFactory(ILoggerFactory loggerFactory)
    : IMatchProgressRepositoryFactory
{
    /// <inheritdoc />
    public IMatchProgressRepository Create(string matchDetailsDirectory) =>
        new JsonMatchProgressRepository(matchDetailsDirectory, loggerFactory.CreateLogger<JsonMatchProgressRepository>());
}

/// <summary>
/// Registro de progreso persistido en JSON dentro del directorio de detalles de
/// partidos. Escritura atómica (temporal + reemplazo) y recuperación ante
/// corrupción: un archivo ilegible se aparta con sufijo de fecha y se continúa
/// con un registro vacío, nunca bloqueando la extracción.
/// </summary>
public sealed class JsonMatchProgressRepository(
    string matchDetailsDirectory,
    ILogger<JsonMatchProgressRepository> logger) : IMatchProgressRepository
{
    /// <summary>Nombre del archivo de registro dentro del directorio de detalles.</summary>
    public const string FileName = "progreso_partidos.json";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _filePath = Path.Combine(matchDetailsDirectory, FileName);
    private readonly ILogger<JsonMatchProgressRepository> _logger = logger;

    private MatchProgressState _state = new();
    private bool _loaded;

    /// <inheritdoc />
    public MatchProgressState Load()
    {
        if (_loaded)
        {
            return _state;
        }

        _loaded = true;

        try
        {
            if (!File.Exists(_filePath))
            {
                _logger.LogInformation("No existe registro de progreso previo ('{File}'); se inicia de cero", _filePath);
                return _state = new MatchProgressState();
            }

            string json = File.ReadAllText(_filePath);
            _state = JsonSerializer.Deserialize<MatchProgressState>(json) ?? new MatchProgressState();

            int completados = CountCompleted();
            _logger.LogInformation("Registro de progreso cargado: {Completed} partidos completados de {Teams} equipos",
                completados, _state.Teams.Count);
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            string backupPath = $"{_filePath}.corrupto-{DateTime.Now:yyyyMMdd-HHmmss}";

            try
            {
                File.Move(_filePath, backupPath);
                _logger.LogWarning(ex,
                    "El registro de progreso está corrupto; se apartó como '{Backup}' y se continúa con un registro vacío",
                    backupPath);
            }
            catch (IOException moveEx)
            {
                _logger.LogWarning(moveEx,
                    "El registro de progreso está corrupto y no pudo apartarse; se continúa con un registro vacío");
            }

            _state = new MatchProgressState();
        }

        return _state;
    }

    /// <inheritdoc />
    public void MarkMatchProcessed(MatchProgressRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentException.ThrowIfNullOrWhiteSpace(record.TeamUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(record.MatchUrl);

        var state = Load();
        record.ProcessedAtUtc = DateTime.UtcNow;

        if (!state.Teams.TryGetValue(record.TeamUrl, out var equipo))
        {
            equipo = new TeamProgress();
            state.Teams[record.TeamUrl] = equipo;
        }

        equipo.Name = record.TeamName;
        equipo.Matches[record.MatchUrl] = record;
        state.UpdatedAtUtc = DateTime.UtcNow;

        Persist(state);
    }

    /// <inheritdoc />
    public bool IsProcessed(string teamUrl, string matchUrl)
    {
        var state = Load();
        return state.Teams.TryGetValue(teamUrl, out var equipo)
            && equipo.Matches.TryGetValue(matchUrl, out var partido)
            && partido.Errors.Count == 0;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _state = new MatchProgressState();
        _loaded = true;

        try
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
                _logger.LogInformation("Registro de progreso eliminado: '{File}'", _filePath);
            }
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "No se pudo eliminar el registro de progreso '{File}'", _filePath);
        }
    }

    /// <summary>Cantidad de partidos completados (sin errores) en el estado actual.</summary>
    private int CountCompleted() =>
        _state.Teams.Values.Sum(equipo => equipo.Matches.Values.Count(p => p.Errors.Count == 0));

    /// <summary>Persiste el estado de forma atómica: escribe un temporal y lo reemplaza.</summary>
    private void Persist(MatchProgressState state)
    {
        string tempPath = $"{_filePath}.tmp";
        string json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, _filePath, overwrite: true);
    }
}

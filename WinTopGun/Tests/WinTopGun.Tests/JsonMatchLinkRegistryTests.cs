using WinTopGun.Application.Services;
using WinTopGun.Domain.Models;
using WinTopGun.Infrastructure.Persistence;

namespace WinTopGun.Tests;

/// <summary>
/// Pruebas de <see cref="JsonMatchLinkRegistry"/> con archivos reales en un
/// directorio temporal: alta de pendientes, cambios de estado, persistencia,
/// reanudación y recuperación ante archivos corruptos (Ideas/Partidos.md).
/// </summary>
public sealed class JsonMatchLinkRegistryTests : IDisposable
{
    private readonly string _directorio = Path.Combine(Path.GetTempPath(), $"wintopgun-tests-{Guid.NewGuid():N}");

    private static MatchLinkTarget Target(string url, string team = "Equipo", int rowIndex = 0) => new(
        MatchIdExtractor.ExtractId(url), url, team, rowIndex);

    public void Dispose()
    {
        if (Directory.Exists(_directorio))
        {
            Directory.Delete(_directorio, recursive: true);
        }
    }

    [Fact]
    public void RegistroVacio_AlInicializarSinArchivo()
    {
        var registro = new JsonMatchLinkRegistry();
        registro.Initialize(_directorio);

        Assert.Equal(0, registro.ProcessedCount);
        Assert.False(registro.IsProcessed("hash-inexistente"));
        Assert.True(Directory.Exists(_directorio));
    }

    [Fact]
    public void Register_EnlacesNuevos_EntranComoPending()
    {
        var registro = new JsonMatchLinkRegistry();
        registro.Initialize(_directorio);
        var target = Target("https://fbref.com/en/matches/abc12345/A-vs-B");

        registro.Register(target);

        Assert.Equal(0, registro.ProcessedCount); // Pending no cuenta como procesado
        Assert.False(registro.IsProcessed(target.MatchId));
    }

    [Fact]
    public void Register_EnlaceExistente_NoSobrescribeSuEstado()
    {
        // Regla 1.2: si ya existe, no se sobrescribe su estado
        // (error común que reprocesaría todo).
        var registro = new JsonMatchLinkRegistry();
        registro.Initialize(_directorio);
        var target = Target("https://fbref.com/en/matches/abc12345/A-vs-B");

        registro.MarkProcessed(target);
        registro.Register(target); // redescubierto en otra ejecución/equipo

        Assert.Equal(1, registro.ProcessedCount);
        Assert.True(registro.IsProcessed(target.MatchId));
    }

    [Fact]
    public void MarkProcessed_PersisteDeInmediato_YEsVisibleEnNuevaInstancia()
    {
        string url = "https://fbref.com/en/matches/abc12345/A-vs-B";
        var target = Target(url);

        var registro = new JsonMatchLinkRegistry();
        registro.Initialize(_directorio);
        registro.Register(target);
        registro.MarkProcessed(target);

        Assert.Equal(1, registro.ProcessedCount);
        Assert.True(registro.IsProcessed(target.MatchId));
        Assert.False(File.Exists(Path.Combine(_directorio, JsonMatchLinkRegistry.RegistryFileName + ".tmp")));

        // Una nueva instancia (nueva ejecución de la aplicación) recupera el estado.
        var registroReanudado = new JsonMatchLinkRegistry();
        registroReanudado.Initialize(_directorio);
        Assert.True(registroReanudado.IsProcessed(target.MatchId));
        Assert.Equal(1, registroReanudado.ProcessedCount);
    }

    [Fact]
    public void MarkFailed_EstadoFailed_ContenidoPersistido()
    {
        string url = "https://fbref.com/en/matches/def67890/C-vs-D";
        var target = Target(url, team: "Equipo X", rowIndex: 7);

        var registro = new JsonMatchLinkRegistry();
        registro.Initialize(_directorio);
        registro.MarkFailed(target);

        Assert.False(registro.IsProcessed(target.MatchId)); // Failed se reintenta

        // El estado Failed sobrevive al reinicio de la aplicación (auditoría).
        var registroReanudado = new JsonMatchLinkRegistry();
        registroReanudado.Initialize(_directorio);
        Assert.False(registroReanudado.IsProcessed(target.MatchId));
        string json = File.ReadAllText(Path.Combine(_directorio, JsonMatchLinkRegistry.RegistryFileName));
        Assert.Contains("\"Failed\"", json);
        Assert.Contains("Equipo X", json);
    }

    [Fact]
    public void ArchivoCorrupto_SeInicializaVacio_SinLanzarExcepcion()
    {
        Directory.CreateDirectory(_directorio);
        File.WriteAllText(Path.Combine(_directorio, JsonMatchLinkRegistry.RegistryFileName), "{ json invalido");

        var registro = new JsonMatchLinkRegistry();
        registro.Initialize(_directorio);

        Assert.Equal(0, registro.ProcessedCount);
        Assert.False(registro.IsProcessed("cualquiera"));
    }

    [Fact]
    public void Reset_EliminaElRegistroPersistido()
    {
        var registro = new JsonMatchLinkRegistry();
        registro.Initialize(_directorio);
        registro.MarkProcessed(Target("https://fbref.com/en/matches/fff00011/E-vs-F"));

        registro.Reset();

        Assert.Equal(0, registro.ProcessedCount);
        Assert.False(File.Exists(Path.Combine(_directorio, JsonMatchLinkRegistry.RegistryFileName)));

        var registroReanudado = new JsonMatchLinkRegistry();
        registroReanudado.Initialize(_directorio);
        Assert.Equal(0, registroReanudado.ProcessedCount);
    }
}

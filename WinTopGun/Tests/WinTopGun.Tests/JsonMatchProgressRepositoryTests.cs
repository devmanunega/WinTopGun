using Microsoft.Extensions.Logging.Abstractions;
using WinTopGun.Domain.Models;
using WinTopGun.Infrastructure.Persistence;

namespace WinTopGun.Tests;

/// <summary>
/// Pruebas unitarias de <see cref="JsonMatchProgressRepository"/> usando archivos
/// temporales reales: persistencia, conteo por equipo, escritura atómica,
/// corrupción y reinicio.
/// </summary>
public sealed class JsonMatchProgressRepositoryTests : IDisposable
{
    private const string TeamUrl = "https://sitio.test/equipos/equipo-1";
    private const string TeamName = "Equipo de prueba";

    private readonly string _directorio;

    public JsonMatchProgressRepositoryTests()
    {
        _directorio = Path.Combine(Path.GetTempPath(), "WinTopGun-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directorio);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directorio, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort: los temporales no deben afectar la ejecución de las pruebas.
        }
    }

    private JsonMatchProgressRepository CrearRepositorio() =>
        new(_directorio, NullLogger<JsonMatchProgressRepository>.Instance);

    [Fact]
    public void Persistencia_RoundTrip_RecuperaLosDatos()
    {
        // Arrange
        var repositorio = CrearRepositorio();
        repositorio.MarkMatchProcessed(TeamUrl, TeamName, "https://sitio.test/partidos/partido-1");
        repositorio.MarkMatchProcessed(TeamUrl, TeamName, "https://sitio.test/partidos/partido-2");

        // Act: nueva instancia sobre el mismo directorio (simula reinicio de la app).
        var repositorio2 = CrearRepositorio();
        var estado = repositorio2.Load();

        // Assert: conteo y orden de partidos procesados del equipo.
        var procesados = repositorio2.GetProcessedMatchUrls(TeamUrl);
        Assert.Equal(2, procesados.Count);
        Assert.Equal("https://sitio.test/partidos/partido-1", procesados[0]);
        Assert.Equal("https://sitio.test/partidos/partido-2", procesados[1]);
        Assert.Equal(TeamName, estado.Teams[TeamUrl].Name);
        Assert.Equal(MatchProgressState.CurrentVersion, estado.Version);
    }

    [Fact]
    public void MarkMatchProcessed_MarcaDuplicada_NoDuplicaElConteo()
    {
        // Arrange
        var repositorio = CrearRepositorio();

        // Act
        repositorio.MarkMatchProcessed(TeamUrl, TeamName, "https://sitio.test/partidos/partido-1");
        repositorio.MarkMatchProcessed(TeamUrl, TeamName, "https://sitio.test/partidos/partido-1");

        // Assert
        Assert.Single(repositorio.GetProcessedMatchUrls(TeamUrl));
    }

    [Fact]
    public void MarkMatchProcessed_EscrituraAtomica_NoDejaTemporales()
    {
        // Arrange
        var repositorio = CrearRepositorio();

        // Act
        repositorio.MarkMatchProcessed(TeamUrl, TeamName, "https://sitio.test/partidos/partido-1");
        repositorio.MarkMatchProcessed(TeamUrl, TeamName, "https://sitio.test/partidos/partido-2");

        // Assert: el registro existe y es el único archivo del directorio (sin temporales residuales).
        Assert.True(File.Exists(Path.Combine(_directorio, JsonMatchProgressRepository.FileName)));
        Assert.Single(Directory.GetFiles(_directorio));
    }

    [Fact]
    public void ArchivoCorrupto_SeApartaYContinuaVacio()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_directorio, JsonMatchProgressRepository.FileName), "{ esto no es json");
        var repositorio = CrearRepositorio();

        // Act
        var estado = repositorio.Load();

        // Assert: estado vacío y el archivo corrupto apartado con sufijo.
        Assert.Empty(estado.Teams);
        Assert.False(File.Exists(Path.Combine(_directorio, JsonMatchProgressRepository.FileName)));
        Assert.Single(Directory.GetFiles(_directorio, $"{JsonMatchProgressRepository.FileName}.corrupto-*"));
    }

    [Fact]
    public void Reset_EliminaElRegistroYVaciaElEstado()
    {
        // Arrange
        var repositorio = CrearRepositorio();
        repositorio.MarkMatchProcessed(TeamUrl, TeamName, "https://sitio.test/partidos/partido-1");

        // Act
        repositorio.Reset();

        // Assert
        Assert.False(File.Exists(Path.Combine(_directorio, JsonMatchProgressRepository.FileName)));
        Assert.Empty(repositorio.GetProcessedMatchUrls(TeamUrl));
    }

    [Fact]
    public void GetProcessedMatchUrls_EquipoDesconocido_RetornaListaVacia()
    {
        // Arrange
        var repositorio = CrearRepositorio();

        // Act + Assert
        Assert.Empty(repositorio.GetProcessedMatchUrls("https://sitio.test/equipos/desconocido"));
    }

    [Fact]
    public void Load_SinArchivoPrevio_RetornaEstadoVacio()
    {
        // Arrange
        var repositorio = CrearRepositorio();

        // Act
        var estado = repositorio.Load();

        // Assert
        Assert.Empty(estado.Teams);
        Assert.Equal(MatchProgressState.CurrentVersion, estado.Version);
    }
}

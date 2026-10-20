using Microsoft.Extensions.Logging.Abstractions;
using WinTopGun.Domain.Models;
using WinTopGun.Infrastructure.Persistence;

namespace WinTopGun.Tests;

/// <summary>
/// Pruebas unitarias de <see cref="JsonMatchProgressRepository"/> usando archivos
/// temporales reales: persistencia, escritura atómica, corrupción y reinicio.
/// </summary>
public sealed class JsonMatchProgressRepositoryTests : IDisposable
{
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

    private MatchProgressRecord CrearRegistro(int numero) => new()
    {
        TeamUrl = "https://sitio.test/equipos/equipo-1",
        TeamName = "Equipo de prueba",
        MatchUrl = $"https://sitio.test/partidos/partido-{numero}",
        Segment = $"partido-{numero}",
    };

    [Fact]
    public void Persistencia_RoundTrip_RecuperaLosDatos()
    {
        // Arrange
        CrearRepositorio().MarkMatchProcessed(CrearRegistro(1));

        // Act: nueva instancia sobre el mismo directorio (simula reinicio de la app).
        var repositorio = CrearRepositorio();
        var estado = repositorio.Load();

        // Assert
        Assert.True(repositorio.IsProcessed("https://sitio.test/equipos/equipo-1", "https://sitio.test/partidos/partido-1"));
        var equipo = Assert.Single(estado.Teams.Values);
        Assert.Equal("Equipo de prueba", equipo.Name);
        var registro = Assert.Single(equipo.Matches.Values);
        Assert.Equal("partido-1", registro.Segment);
        Assert.Equal(MatchProgressState.CurrentVersion, estado.Version);
    }

    [Fact]
    public void MarkMatchProcessed_EscrituraAtomica_NoDejaTemporales()
    {
        // Arrange
        var repositorio = CrearRepositorio();

        // Act
        repositorio.MarkMatchProcessed(CrearRegistro(1));
        repositorio.MarkMatchProcessed(CrearRegistro(2));

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
        repositorio.MarkMatchProcessed(CrearRegistro(1));

        // Act
        repositorio.Reset();

        // Assert
        Assert.False(File.Exists(Path.Combine(_directorio, JsonMatchProgressRepository.FileName)));
        Assert.False(repositorio.IsProcessed("https://sitio.test/equipos/equipo-1", "https://sitio.test/partidos/partido-1"));
    }

    [Fact]
    public void IsProcessed_PartidoConErrores_RetornaFalso()
    {
        // Arrange: partido registrado pero con incidencias de exportación.
        var repositorio = CrearRepositorio();
        var registro = CrearRegistro(1);
        registro.Errors.Add("Partido 'partido-1': la tabla 'x' no existe.");
        repositorio.MarkMatchProcessed(registro);

        // Act + Assert
        Assert.False(repositorio.IsProcessed(registro.TeamUrl, registro.MatchUrl));
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

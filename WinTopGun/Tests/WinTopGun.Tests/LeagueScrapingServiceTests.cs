using Microsoft.Extensions.Logging.Abstractions;
using WinTopGun.Application.Interfaces;
using WinTopGun.Application.Services;
using WinTopGun.Domain.Models;
using WinTopGun.Tests.Fakes;

namespace WinTopGun.Tests;

/// <summary>
/// Pruebas unitarias de <see cref="LeagueScrapingService"/> usando fakes de
/// Selenium y del exportador, sin navegador real ni acceso a disco.
/// </summary>
public sealed class LeagueScrapingServiceTests
{
    private const string OutputDirectory = @"C:\salida-fake";

    private static LeagueScrapingService CreateService(
        FakeWebDriver driver,
        out FakeDriverFactory factory,
        out RecordingTableExporter exporter)
    {
        factory = new FakeDriverFactory(driver);
        exporter = new RecordingTableExporter();
        var options = new ScrapingOptions { WaitTimeoutSeconds = 1 };

        return new LeagueScrapingService(
            factory,
            exporter,
            options,
            NullLogger<LeagueScrapingService>.Instance);
    }

    private static FakeWebDriver CreateDriverWithTwoLeaguesAndTwoTables()
    {
        var driver = new FakeWebDriver
        {
            LeagueNames = ["Liga A", "Liga B"],
            Url = "https://sitio-de-prueba.test/liga/primeraliga",
            Tables = [("tabla_general", "contenido general"), ("tabla_plantilla", "contenido plantilla")],
        };
        driver.PrepareTables();
        return driver;
    }

    [Fact]
    public async Task HappyPath_ExportaTodasLasTablas()
    {
        // Arrange
        var driver = CreateDriverWithTwoLeaguesAndTwoTables();
        var service = CreateService(driver, out var factory, out var exporter);

        // Act
        ScrapingResult resultado = await service.ExtractLeagueTablesAsync(OutputDirectory);

        // Assert
        Assert.True(resultado.Success);
        Assert.Equal(2, resultado.LeaguesProcessed);
        Assert.Equal(4, resultado.TablesExported);
        Assert.Equal(4, exporter.Exports.Count);
        Assert.True(driver.QuitCalled);
        Assert.All(exporter.Exports, e => Assert.Equal(OutputDirectory, e.OutputDirectory));
        Assert.All(exporter.Exports, e => Assert.Equal("primeraliga", e.PageSegment));
    }

    [Fact]
    public async Task TablaInexistenteEnDom_ReportaErrorYContinua()
    {
        // Arrange
        var driver = CreateDriverWithTwoLeaguesAndTwoTables();
        driver.MissingTableIds.Add("tabla_general");
        driver.PrepareTables(); // reconstruir con la tabla "stale" excluida
        var service = CreateService(driver, out _, out var exporter);

        // Act
        ScrapingResult resultado = await service.ExtractLeagueTablesAsync(OutputDirectory);

        // Assert
        Assert.False(resultado.Success);
        Assert.Equal(2, resultado.Errors.Count);
        Assert.Contains(resultado.Errors, e => e.Contains("tabla_general"));
        Assert.Equal(2, resultado.TablesExported); // 1 tabla por liga
        Assert.Equal(2, exporter.Exports.Count(e => e.TableId == "tabla_plantilla"));
        Assert.DoesNotContain(exporter.Exports, e => e.TableId == "tabla_general");
    }

    [Fact]
    public async Task TimeoutSinTablas_RegistraErrorYPeroNoExporta()
    {
        // Arrange
        var driver = CreateDriverWithTwoLeaguesAndTwoTables();
        driver.TablesVisible = false;
        var service = CreateService(driver, out _, out var exporter);

        // Act
        ScrapingResult resultado = await service.ExtractLeagueTablesAsync(OutputDirectory);

        // Assert
        Assert.False(resultado.Success);
        Assert.Equal(0, resultado.TablesExported);
        Assert.Equal(0, resultado.LeaguesProcessed);
        Assert.Equal(2, resultado.Errors.Count);
        Assert.True(driver.QuitCalled);
        Assert.Empty(exporter.Exports);
    }

    [Fact]
    public async Task TokenCanceladoAntesDeIniciar_LanzaOperacionCancelada()
    {
        // Arrange
        var driver = CreateDriverWithTwoLeaguesAndTwoTables();
        var service = CreateService(driver, out _, out _);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act + Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.ExtractLeagueTablesAsync(OutputDirectory, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task DirectorioSalidaVacio_LanzaArgumentException()
    {
        // Arrange
        var driver = CreateDriverWithTwoLeaguesAndTwoTables();
        var service = CreateService(driver, out _, out _);

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ExtractLeagueTablesAsync(string.Empty));
    }

    [Fact]
    public async Task Progreso_ReportaLigasYTablas()
    {
        // Arrange
        var driver = CreateDriverWithTwoLeaguesAndTwoTables();
        var service = CreateService(driver, out _, out _);
        var progress = new RecordingProgress();

        // Act
        await service.ExtractLeagueTablesAsync(OutputDirectory, progress);

        // Assert
        Assert.Contains(progress.Reports, r => r is { CurrentLeague: 1, TotalLeagues: 2, CurrentTable: 0 });
        Assert.Contains(progress.Reports, r => r is { CurrentLeague: 2, TotalLeagues: 2 });
        Assert.Contains(progress.Reports, r => r is { CurrentTable: 2, TotalTables: 2 });
    }

    [Fact]
    public async Task Always_LlamaQuitAunqueHayaExcepcion()
    {
        // Arrange
        var driver = new FakeWebDriver { LeagueNames = ["Liga A"] }; // sin tablas visibles
        var service = CreateService(driver, out _, out _);

        // Act
        ScrapingResult resultado = await service.ExtractLeagueTablesAsync(OutputDirectory);

        // Assert
        Assert.True(driver.QuitCalled);
        Assert.Single(resultado.Errors);
    }
}

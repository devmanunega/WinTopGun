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
        var options = new ScrapingOptions
        {
            WaitTimeoutSeconds = 1,
            HumanPauseMinMilliseconds = 0,
            HumanPauseMaxMilliseconds = 0,
        };

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

    // ------------------------------------------------------------------
    // Flujo de jugadores (btnExtractPlayers)
    // ------------------------------------------------------------------

    private static FakeWebDriver CreateDriverForPlayers(int leagues = 2, int playerLinks = 4)
    {
        var driver = new FakeWebDriver
        {
            LeagueNames = [.. Enumerable.Range(1, leagues).Select(i => $"Liga {i}")],
            PlayerLinkHrefs = [.. Enumerable.Range(1, playerLinks).Select(i => $"https://sitio-de-prueba.test/jugador/{i}")],
            Url = "https://sitio-de-prueba.test/liga/primeraliga",
            Tables = [("tabla_jugadores", "contenido jugadores")],
        };
        driver.PrepareTables();
        return driver;
    }

    [Fact]
    public async Task Jugadores_SoloAbreEnlacesParesYExportaTablas()
    {
        // Arrange: 2 ligas × 4 enlaces → solo se abren el 2.º y el 4.º
        var driver = CreateDriverForPlayers();
        var service = CreateService(driver, out var factory, out var exporter);

        // Act
        ScrapingResult resultado = await service.ExtractPlayerTablesAsync(OutputDirectory);

        // Assert
        Assert.True(resultado.Success);
        Assert.Equal(2, resultado.LeaguesProcessed);
        Assert.Equal(4, resultado.TablesExported); // 2 enlaces pares × 2 ligas × 1 tabla
        Assert.Equal(4, exporter.Exports.Count);

        // Solo se navegaron los enlaces en posición par (2.º y 4.º), dos veces (una por liga).
        Assert.All(driver.VisitedUrls, url => Assert.True(
            url.EndsWith("/jugador/2") || url.EndsWith("/jugador/4"),
            $"Se navegó a un enlace impar: {url}"));
        Assert.Equal(2, driver.VisitedUrls.Count(url => url.EndsWith("/jugador/2", StringComparison.Ordinal)));
        Assert.Equal(2, driver.VisitedUrls.Count(url => url.EndsWith("/jugador/4", StringComparison.Ordinal)));
        Assert.DoesNotContain(driver.VisitedUrls, url => url.EndsWith("/jugador/1"));
        Assert.DoesNotContain(driver.VisitedUrls, url => url.EndsWith("/jugador/3"));
        Assert.True(driver.QuitCalled);
    }

    [Fact]
    public async Task Jugadores_Progreso_ReportaEnlacesYTablas()
    {
        // Arrange
        var driver = CreateDriverForPlayers();
        var service = CreateService(driver, out _, out _);
        var progress = new RecordingProgress();

        // Act
        await service.ExtractPlayerTablesAsync(OutputDirectory, progress);

        // Assert
        Assert.Contains(progress.Reports, r => r is { CurrentLeague: 1, TotalLeagues: 2, CurrentTable: 0 });
        Assert.Contains(progress.Reports, r => r.Message.Contains("Enlace 1/2"));
        Assert.Contains(progress.Reports, r => r.Message.Contains("Enlace 2/2"));
        Assert.Contains(progress.Reports, r => r is { CurrentTable: 1, TotalTables: 1 });
    }

    [Fact]
    public async Task Jugadores_TimeoutSinTablas_RegistraErrorYContinua()
    {
        // Arrange
        var driver = CreateDriverForPlayers();
        driver.TablesVisible = false;
        var service = CreateService(driver, out _, out var exporter);

        // Act
        ScrapingResult resultado = await service.ExtractPlayerTablesAsync(OutputDirectory);

        // Assert
        Assert.False(resultado.Success);
        Assert.Equal(0, resultado.TablesExported);
        Assert.Empty(exporter.Exports);
        Assert.Equal(4, resultado.Errors.Count); // 2 ligas × 2 enlaces pares
        Assert.True(driver.QuitCalled);
    }

    [Fact]
    public async Task Jugadores_DirectorioSalidaVacio_LanzaArgumentException()
    {
        // Arrange
        var driver = CreateDriverForPlayers();
        var service = CreateService(driver, out _, out _);

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ExtractPlayerTablesAsync(string.Empty));
    }

    [Fact]
    public async Task Jugadores_TokenCanceladoAntesDeIniciar_LanzaOperacionCancelada()
    {
        // Arrange
        var driver = CreateDriverForPlayers();
        var service = CreateService(driver, out _, out _);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act + Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.ExtractPlayerTablesAsync(OutputDirectory, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task Jugadores_PausaHumana_SeEjecutaTrasCadaCargaCompleta()
    {
        // Arrange: pausa reducida (100-150 ms) pero verificable; 2 ligas × 2 enlaces pares = 4 pausas.
        var driver = CreateDriverForPlayers();
        var factory = new FakeDriverFactory(driver);
        var exporter = new RecordingTableExporter();
        var options = new ScrapingOptions
        {
            WaitTimeoutSeconds = 1,
            HumanPauseMinMilliseconds = 100,
            HumanPauseMaxMilliseconds = 150,
        };
        var service = new LeagueScrapingService(
            factory,
            exporter,
            options,
            NullLogger<LeagueScrapingService>.Instance);
        var cronometro = System.Diagnostics.Stopwatch.StartNew();

        // Act
        ScrapingResult resultado = await service.ExtractPlayerTablesAsync(OutputDirectory);
        cronometro.Stop();

        // Assert: 4 pausas de al menos 100 ms cada una.
        Assert.True(resultado.Success);
        Assert.True(cronometro.ElapsedMilliseconds >= 400,
            $"Tiempo transcurrido insuficiente para 4 pausas: {cronometro.ElapsedMilliseconds} ms");
    }
}

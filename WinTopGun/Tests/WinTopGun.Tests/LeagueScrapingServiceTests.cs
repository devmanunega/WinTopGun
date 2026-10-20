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
        out RecordingTableExporter exporter,
        InMemoryMatchProgressRepositoryFactory? progressFactory = null)
    {
        factory = new FakeDriverFactory(driver);
        exporter = new RecordingTableExporter();
        progressFactory ??= new InMemoryMatchProgressRepositoryFactory();
        var options = new ScrapingOptions
        {
            WaitTimeoutSeconds = 1,
            HumanPauseMinMilliseconds = 0,
            HumanPauseMaxMilliseconds = 0,
            MatchPauseMinMilliseconds = 0,
            MatchPauseMaxMilliseconds = 0,
        };

        return new LeagueScrapingService(
            factory,
            exporter,
            options,
            progressFactory,
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
        Assert.True(resultado.Success, string.Join(" | ", resultado.Errors));
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
        Assert.True(resultado.Success, string.Join(" | ", resultado.Errors));
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
            new InMemoryMatchProgressRepositoryFactory(),
            NullLogger<LeagueScrapingService>.Instance);
        var cronometro = System.Diagnostics.Stopwatch.StartNew();

        // Act
        ScrapingResult resultado = await service.ExtractPlayerTablesAsync(OutputDirectory);
        cronometro.Stop();

        // Assert: 4 pausas de al menos 100 ms cada una.
        Assert.True(resultado.Success, string.Join(" | ", resultado.Errors));
        Assert.True(cronometro.ElapsedMilliseconds >= 400,
            $"Tiempo transcurrido insuficiente para 4 pausas: {cronometro.ElapsedMilliseconds} ms");
    }

    private static FakeWebDriver CreateDriverForMatches(int leagues = 1, int teams = 1, int matchesPerTeam = 2, List<string>? matchLinkTexts = null)
    {
        var driver = new FakeWebDriver
        {
            LeagueNames = Enumerable.Range(1, leagues).Select(l => $"Liga {l}").ToList(),
            Url = "https://sitio-de-prueba.test/liga/primeraliga",
            TeamLinkHrefs = Enumerable.Range(1, teams)
                .Select(t => $"https://sitio-de-prueba.test/equipos/equipo-{t}").ToList(),
            MatchReportHrefs = Enumerable.Range(1, matchesPerTeam)
                .Select(m => $"https://sitio-de-prueba.test/partidos/partido-{m}").ToList(),
            Tables =
            [
                ("results2026-202791_overall", "clasificacion"),
                ("matchlogs_for", "partidos del equipo"),
                ("tabla_partido", "contenido partido"),
            ],
            MatchLinkTexts = matchLinkTexts,
            ExtraElementsById = new Dictionary<string, string>
            {
                ["scorebox_meta"] = "metadatos del partido",
                ["events_wrap"] = "eventos del partido",
            },
        };
        driver.PrepareTables();
        return driver;
    }

    [Fact]
    public async Task Partidos_HappyPath_ExportaTablasYElementosEnSubcarpeta()
    {
        // Arrange
        var driver = CreateDriverForMatches(teams: 1, matchesPerTeam: 2);
        var service = CreateService(driver, out var factory, out var exporter);
        string expectedDirectory = Path.Combine(OutputDirectory, "Detalles_Partidos");

        // Act
        ScrapingResult resultado = await service.ExtractMatchDetailsAsync(OutputDirectory);

        // Assert: por partido se exportan 3 tablas con id + scorebox_meta + events_wrap = 5; dos partidos = 10.
        Assert.True(resultado.Success, string.Join(" | ", resultado.Errors));
        Assert.Equal(1, resultado.LeaguesProcessed);
        Assert.Equal(10, resultado.TablesExported);
        Assert.Equal(10, exporter.Exports.Count);
        Assert.All(exporter.Exports, e => Assert.Equal(expectedDirectory, e.OutputDirectory));
        Assert.Equal(2, exporter.Exports.Count(e => e.TableId == "scorebox_meta"));
        Assert.Equal(2, exporter.Exports.Count(e => e.TableId == "events_wrap"));

        // Navegación: equipo + 2 partidos visitados por URL; retornos: 2 (partido→equipo) + 1 (equipo→liga) + 1 (liga→principal).
        Assert.Contains("https://sitio-de-prueba.test/equipos/equipo-1", driver.VisitedUrls);
        Assert.Contains("https://sitio-de-prueba.test/partidos/partido-1", driver.VisitedUrls);
        Assert.Contains("https://sitio-de-prueba.test/partidos/partido-2", driver.VisitedUrls);
        Assert.Equal(4, driver.NavigationsBack);
        Assert.True(driver.QuitCalled);
    }

    [Fact]
    public async Task Partidos_MultiplesEquipos_VisitaTodosLosEquipos()
    {
        // Arrange
        var driver = CreateDriverForMatches(teams: 3, matchesPerTeam: 1);
        var service = CreateService(driver, out _, out var exporter);

        // Act
        ScrapingResult resultado = await service.ExtractMatchDetailsAsync(OutputDirectory);

        // Assert: 3 equipos × (2 tablas/partido... 3 tablas con id + 2 divs = 5) = 15 exportaciones.
        Assert.True(resultado.Success, string.Join(" | ", resultado.Errors));
        Assert.Equal(3, exporter.Exports.Count(e => e.TableId == "scorebox_meta"));
        for (int t = 1; t <= 3; t++)
        {
            Assert.Contains($"https://sitio-de-prueba.test/equipos/equipo-{t}", driver.VisitedUrls);
        }
    }

    [Fact]
    public async Task Partidos_SoloProcesaPartidosConMatchReport()
    {
        // Arrange: 3 filas en matchlogs_for; la tercera es un partido pendiente sin reporte.
        var driver = CreateDriverForMatches(teams: 1, matchesPerTeam: 3, matchLinkTexts: ["Match Report", "Match Report", ""]);
        var service = CreateService(driver, out _, out var exporter);

        // Act
        ScrapingResult resultado = await service.ExtractMatchDetailsAsync(OutputDirectory);

        // Assert: solo los 2 partidos disputados generan exportaciones (3 tablas + 2 divs cada uno).
        Assert.True(resultado.Success, string.Join(" | ", resultado.Errors));
        Assert.Equal(10, resultado.TablesExported);
        Assert.Equal(10, exporter.Exports.Count);
        Assert.Contains("https://sitio-de-prueba.test/partidos/partido-1", driver.VisitedUrls);
        Assert.Contains("https://sitio-de-prueba.test/partidos/partido-2", driver.VisitedUrls);
        Assert.DoesNotContain("https://sitio-de-prueba.test/partidos/partido-3", driver.VisitedUrls);
    }

    [Fact]
    public async Task Partidos_PausaYScrollSimulanRevisionDelPartido()
    {
        // Arrange: pausa de partido reducida (100-150 ms) pero verificable; 2 partidos = 2 pausas.
        var driver = CreateDriverForMatches(teams: 1, matchesPerTeam: 2);
        var factory = new FakeDriverFactory(driver);
        var exporter = new RecordingTableExporter();
        var options = new ScrapingOptions
        {
            WaitTimeoutSeconds = 1,
            HumanPauseMinMilliseconds = 0,
            HumanPauseMaxMilliseconds = 0,
            MatchPauseMinMilliseconds = 100,
            MatchPauseMaxMilliseconds = 150,
        };
        var service = new LeagueScrapingService(factory, exporter, options, new InMemoryMatchProgressRepositoryFactory(), NullLogger<LeagueScrapingService>.Instance);
        var cronometro = System.Diagnostics.Stopwatch.StartNew();

        // Act
        ScrapingResult resultado = await service.ExtractMatchDetailsAsync(OutputDirectory);
        cronometro.Stop();

        // Assert: 2 pausas de al menos 100 ms cada una y scroll de lectura en cada partido.
        Assert.True(resultado.Success, string.Join(" | ", resultado.Errors));
        Assert.True(cronometro.ElapsedMilliseconds >= 200,
            $"Tiempo transcurrido insuficiente para 2 pausas de partido: {cronometro.ElapsedMilliseconds} ms");
        Assert.True(driver.ExecutedScripts.Count(s => s.Contains("scrollBy")) >= 2,
            $"Se esperaban al menos 2 scrolls, hubo: {driver.ExecutedScripts.Count(s => s.Contains("scrollBy"))}");
    }

    [Fact]
    public async Task Partidos_ReanudaProceso_SoloPartidosPendientes()
    {
        // Arrange: 3 partidos disputados; partido-1 y partido-2 ya procesados en ejecución previa.
        var driver = CreateDriverForMatches(teams: 1, matchesPerTeam: 3);
        var progressFactory = new InMemoryMatchProgressRepositoryFactory();
        string teamUrl = "https://sitio-de-prueba.test/equipos/equipo-1";
        foreach (int i in new[] { 1, 2 })
        {
            progressFactory.Repository.MarkMatchProcessed(
                teamUrl, "Equipo de prueba", $"https://sitio-de-prueba.test/partidos/partido-{i}");
        }
        var service = CreateService(driver, out _, out var exporter, progressFactory);

        // Act
        ScrapingResult resultado = await service.ExtractMatchDetailsAsync(OutputDirectory);

        // Assert: solo partido-3 se visita y exporta (3 tablas + 2 divs = 5).
        Assert.True(resultado.Success, string.Join(" | ", resultado.Errors));
        Assert.Equal(2, resultado.MatchesSkipped);
        Assert.Equal(5, exporter.Exports.Count);
        Assert.Contains("https://sitio-de-prueba.test/partidos/partido-3", driver.VisitedUrls);
        Assert.DoesNotContain(driver.VisitedUrls, u => u.Contains("partido-1"));
        Assert.DoesNotContain(driver.VisitedUrls, u => u.Contains("partido-2"));
    }

    [Fact]
    public async Task Partidos_EquipoCompleto_OmiteTodosSusPartidos()
    {
        // Arrange: todos los partidos del equipo ya procesados.
        var driver = CreateDriverForMatches(teams: 1, matchesPerTeam: 2);
        var progressFactory = new InMemoryMatchProgressRepositoryFactory();
        string teamUrl = "https://sitio-de-prueba.test/equipos/equipo-1";
        foreach (int i in new[] { 1, 2 })
        {
            progressFactory.Repository.MarkMatchProcessed(
                teamUrl, "Equipo de prueba", $"https://sitio-de-prueba.test/partidos/partido-{i}");
        }
        var service = CreateService(driver, out _, out var exporter, progressFactory);

        // Act
        ScrapingResult resultado = await service.ExtractMatchDetailsAsync(OutputDirectory);

        // Assert: no se navega a ningún partido y no hay exportaciones.
        Assert.True(resultado.Success, string.Join(" | ", resultado.Errors));
        Assert.Equal(2, resultado.MatchesSkipped);
        Assert.Empty(exporter.Exports);
        Assert.DoesNotContain(driver.VisitedUrls, u => u.Contains("/partidos/"));
    }

    [Fact]
    public async Task Partidos_PartidoConErrores_NoSeMarcaComoProcesado()
    {
        // Arrange: la tabla del partido falla al exportarse (incidencia registrada).
        var driver = CreateDriverForMatches(teams: 1, matchesPerTeam: 1);
        driver.MissingTableIds.Add("tabla_partido");
        driver.PrepareTables();
        var progressFactory = new InMemoryMatchProgressRepositoryFactory();
        var service = CreateService(driver, out _, out _, progressFactory);

        // Act
        ScrapingResult resultado = await service.ExtractMatchDetailsAsync(OutputDirectory);

        // Assert: el partido con errores NO se registra como procesado (se reintentará).
        Assert.False(resultado.Success);
        Assert.Empty(progressFactory.Repository.GetProcessedMatchUrls("https://sitio-de-prueba.test/equipos/equipo-1"));
    }

    [Fact]
    public async Task Partidos_EquipoSinMatchlogs_ReportaErrorYContinua()
    {
        // Arrange
        var driver = CreateDriverForMatches(teams: 2, matchesPerTeam: 1);
        driver.MatchLogsVisible = false;
        var service = CreateService(driver, out _, out var exporter);

        // Act
        ScrapingResult resultado = await service.ExtractMatchDetailsAsync(OutputDirectory);

        // Assert: ninguno de los dos equipos carga 'matchlogs_for'; la liga sí se procesa.
        Assert.False(resultado.Success);
        Assert.Equal(1, resultado.LeaguesProcessed);
        Assert.Equal(2, resultado.Errors.Count);
        Assert.Contains(resultado.Errors, e => e.Contains("matchlogs_for"));
        Assert.Empty(exporter.Exports);
        Assert.True(driver.QuitCalled);
    }

    [Fact]
    public async Task Partidos_ElementosFaltantesEnPagina_ReportaErrorYContinua()
    {
        // Arrange: la página del partido no contiene los divs scorebox_meta ni events_wrap.
        var driver = CreateDriverForMatches(teams: 1, matchesPerTeam: 1);
        driver.ExtraElementsById.Clear();
        var service = CreateService(driver, out _, out var exporter);

        // Act
        ScrapingResult resultado = await service.ExtractMatchDetailsAsync(OutputDirectory);

        // Assert: las tablas con id sí se exportan; los divs faltantes generan un error cada uno.
        Assert.False(resultado.Success);
        Assert.Equal(2, resultado.Errors.Count);
        Assert.Contains(resultado.Errors, e => e.Contains("scorebox_meta"));
        Assert.Contains(resultado.Errors, e => e.Contains("events_wrap"));
        Assert.Equal(3, exporter.Exports.Count); // solo las 3 tablas con id del partido
    }
}



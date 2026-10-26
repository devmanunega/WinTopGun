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
        FakeMatchLinkRegistry? registry = null,
        ITableExporter? exportador = null)
    {
        factory = new FakeDriverFactory(driver);
        exporter = (exportador as RecordingTableExporter) ?? new RecordingTableExporter();
        registry ??= new FakeMatchLinkRegistry();
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
            exportador ?? exporter,
            registry,
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
            new FakeMatchLinkRegistry(),
            options,
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

    // ------------------------------------------------------------------
    // Flujo de partidos (btnExtractPartidos): tabla de posiciones → página de
    // cada equipo → enlaces "Match Report" (matchlogs_for) → hash → registro
    // persistente → solo pendientes.
    // ------------------------------------------------------------------

    private static FakeWebDriver CreateDriverForMatches(int leagues = 1, int teams = 2, int matchesPerTeam = 2, List<string>? matchLinkTexts = null)
    {
        var driver = new FakeWebDriver
        {
            LeagueNames = [.. Enumerable.Range(1, leagues).Select(l => $"Liga {l}")],
            Url = "https://sitio-de-prueba.test/liga/primeraliga",
            StandingsRows = [.. Enumerable.Range(1, teams).Select(t => new FakeStandingsRow(
                $"Equipo {t}",
                $"https://sitio-de-prueba.test/equipos/equipo-{t}"))],
            MatchReportHrefs = [.. Enumerable.Range(1, matchesPerTeam).Select(m => $"https://sitio-de-prueba.test/partidos/partido-{m}")],
            Tables =
            [
                ("results2026-202791_overall", "clasificacion"),
                ("matchlogs_for", "partidos del equipo"),
                ("tabla_partido", "contenido partido"),
            ],
            MatchLinkTexts = matchLinkTexts,
            ExtraElementsById = new Dictionary<string, string>
            {
                ["scorebox"] = "marcador del partido",
                ["field_wrap"] = "datos de campo del partido",
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
        // Arrange: 2 equipos; el fake comparte la tabla 'matchlogs_for' entre páginas,
        // por lo que el equipo 2 encuentra los mismos partidos ya procesados por el 1.
        var driver = CreateDriverForMatches(teams: 2, matchesPerTeam: 2);
        var service = CreateService(driver, out var factory, out var exporter);
        string expectedDirectory = Path.Combine(OutputDirectory, "Detalles_Partidos");

        // Act
        ScrapingResult resultado = await service.ExtractMatchDetailsAsync(OutputDirectory);

        // Assert: por partido se exportan 3 tablas con id + scorebox + field_wrap
        // + scorebox_meta + events_wrap = 7; dos partidos = 14.
        Assert.True(resultado.Success, string.Join(" | ", resultado.Errors));
        Assert.Equal(1, resultado.LeaguesProcessed);
        Assert.Equal(14, resultado.TablesExported);
        Assert.Equal(14, exporter.Exports.Count);
        Assert.All(exporter.Exports, e => Assert.Equal(expectedDirectory, e.OutputDirectory));
        Assert.Equal(2, exporter.Exports.Count(e => e.TableId == "scorebox"));
        Assert.Equal(2, exporter.Exports.Count(e => e.TableId == "field_wrap"));
        Assert.Equal(2, exporter.Exports.Count(e => e.TableId == "scorebox_meta"));
        Assert.Equal(2, exporter.Exports.Count(e => e.TableId == "events_wrap"));

        // Navegación: 2 páginas de equipo + 2 páginas de partido (cada partido se
        // abre una sola vez aunque dos equipos lo referencien: desduplicación por hash).
        Assert.Equal(2, driver.VisitedUrls.Count(u => u.Contains("/equipos/")));
        Assert.Equal(2, driver.VisitedUrls.Count(u => u.Contains("/partidos/")));
        Assert.Equal(5, driver.NavigationsBack); // 2 (partido→equipo) + 2 (equipo→liga) + 1 (liga→principal)
        Assert.Equal(2, resultado.MatchesSkipped);
        Assert.True(driver.QuitCalled);
    }

    [Fact]
    public async Task Partidos_ElementosNoTabla_ExportaHtmlExterno()
    {
        // Arrange: scorebox, field_wrap y events_wrap no son <table>: se exporta
        // su outerHTML; scorebox_meta sigue exportándose como texto (innerText).
        var driver = CreateDriverForMatches(teams: 1, matchesPerTeam: 1);
        var service = CreateService(driver, out _, out var exporter);

        // Act
        ScrapingResult resultado = await service.ExtractMatchDetailsAsync(OutputDirectory);

        // Assert
        Assert.True(resultado.Success, string.Join(" | ", resultado.Errors));
        Assert.Contains(
            "<div class=\"scorebox\">",
            exporter.Exports.Single(e => e.TableId == "scorebox").Content);
        Assert.Contains(
            "<div id=\"field_wrap\">",
            exporter.Exports.Single(e => e.TableId == "field_wrap").Content);
        Assert.Contains(
            "<div id=\"events_wrap\">",
            exporter.Exports.Single(e => e.TableId == "events_wrap").Content);
        Assert.DoesNotContain("<div", exporter.Exports.Single(e => e.TableId == "scorebox_meta").Content);
    }

    [Fact]
    public async Task Partidos_SoloProcesaPartidosConMatchReport()
    {
        // Arrange: la tabla del equipo tiene 3 filas; la tercera es un partido pendiente sin reporte.
        var driver = CreateDriverForMatches(teams: 1, matchesPerTeam: 3, matchLinkTexts: ["Match Report", "Match Report", ""]);
        var service = CreateService(driver, out _, out var exporter);

        // Act
        ScrapingResult resultado = await service.ExtractMatchDetailsAsync(OutputDirectory);

        // Assert: solo los 2 partidos disputados generan exportaciones (3 tablas + 4 divs cada uno).
        Assert.True(resultado.Success, string.Join(" | ", resultado.Errors));
        Assert.Equal(14, resultado.TablesExported);
        Assert.Equal(14, exporter.Exports.Count);
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
        var registry = new FakeMatchLinkRegistry();
        var options = new ScrapingOptions
        {
            WaitTimeoutSeconds = 1,
            HumanPauseMinMilliseconds = 0,
            HumanPauseMaxMilliseconds = 0,
            MatchPauseMinMilliseconds = 100,
            MatchPauseMaxMilliseconds = 150,
        };
        var service = new LeagueScrapingService(
            factory,
            new RecordingTableExporter(),
            registry,
            options,
            NullLogger<LeagueScrapingService>.Instance);
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
    public async Task Partidos_ReanudaProceso_SoloEnlacesPendientes()
    {
        // Arrange: 2 equipos × 2 partidos; el partido-1 ya fue procesado en una
        // ejecución previa (ejemplo ideal 1: se identifican 4 enlaces, 3 ya constan).
        var driver = CreateDriverForMatches(teams: 2, matchesPerTeam: 2);
        var registry = new FakeMatchLinkRegistry();
        string partido1 = "https://sitio-de-prueba.test/partidos/partido-1";
        registry.MarkProcessed(new MatchLinkTarget(
            MatchIdExtractor.ExtractId(partido1), partido1, "Equipo previo", RowIndex: 0));
        var service = CreateService(driver, out _, out var exporter, registry);

        // Act
        ScrapingResult resultado = await service.ExtractMatchDetailsAsync(OutputDirectory);

        // Assert: el equipo 1 procesa solo partido-2; el equipo 2 omite ambos.
        Assert.True(resultado.Success, string.Join(" | ", resultado.Errors));
        Assert.Equal(3, resultado.MatchesSkipped);
        Assert.Equal(7, exporter.Exports.Count); // 1 partido × (3 tablas + 4 divs)
        Assert.Single(driver.VisitedUrls.Where(u => u.Contains("/partidos/") && u.Contains("partido-2")));
        Assert.DoesNotContain(driver.VisitedUrls, u => u.Contains("/partidos/") && u.Contains("partido-1"));
        Assert.Equal(2, registry.ProcessedCount); // 1 previo + 1 nuevo
    }

    [Fact]
    public async Task Partidos_TodosProcesados_NoAbreNingunPartido()
    {
        // Arrange (ejemplo ideal 2): todos los enlaces ya aparecen como procesados.
        var driver = CreateDriverForMatches(teams: 2, matchesPerTeam: 2);
        var registry = new FakeMatchLinkRegistry();
        foreach (string href in driver.MatchReportHrefs)
        {
            registry.MarkProcessed(new MatchLinkTarget(
                MatchIdExtractor.ExtractId(href), href, "Equipo previo", RowIndex: 0));
        }
        var service = CreateService(driver, out _, out var exporter, registry);

        // Act
        ScrapingResult resultado = await service.ExtractMatchDetailsAsync(OutputDirectory);

        // Assert: no se navega a ningún partido y no hay exportaciones; se continúa
        // con el siguiente equipo (las páginas de equipo sí se recorren).
        Assert.True(resultado.Success, string.Join(" | ", resultado.Errors));
        Assert.Equal(4, resultado.MatchesSkipped);
        Assert.Empty(exporter.Exports);
        Assert.DoesNotContain(driver.VisitedUrls, u => u.Contains("/partidos/"));
        Assert.Equal(2, driver.VisitedUrls.Count(u => u.Contains("/equipos/")));
    }

    [Fact]
    public async Task Partidos_DosEjecucionesSeguidas_LaSegundaNoProcesaNada()
    {
        // Criterio de aceptación (2.4): ejecutar el proceso dos veces seguidas
        // sobre el mismo equipo resulta en cero partidos procesados en la segunda.
        var driver = CreateDriverForMatches(teams: 1, matchesPerTeam: 3);
        var registry = new FakeMatchLinkRegistry();
        var service = CreateService(driver, out _, out var exporter, registry);

        await service.ExtractMatchDetailsAsync(OutputDirectory);
        int exportacionesPrimera = exporter.Exports.Count;
        int visitasPartidosPrimera = driver.VisitedUrls.Count(u => u.Contains("/partidos/"));

        ScrapingResult segunda = await service.ExtractMatchDetailsAsync(OutputDirectory);

        Assert.Equal(3, segunda.MatchesSkipped);
        Assert.Equal(exportacionesPrimera, exporter.Exports.Count); // sin nuevas exportaciones
        Assert.Equal(visitasPartidosPrimera, driver.VisitedUrls.Count(u => u.Contains("/partidos/")));
        Assert.True(segunda.Success, string.Join(" | ", segunda.Errors));
    }

    [Fact]
    public async Task Partidos_Fallido_SeReintentaEnLaSiguienteEjecucion()
    {
        // Estado Failed: la primera ejecución falla (tabla con error); la segunda lo reintenta.
        var driver = CreateDriverForMatches(teams: 1, matchesPerTeam: 1);
        driver.MissingTableIds.Add("tabla_partido");
        driver.PrepareTables();
        var registry = new FakeMatchLinkRegistry();
        var service = CreateService(driver, out _, out var exporter, registry);

        ScrapingResult primera = await service.ExtractMatchDetailsAsync(OutputDirectory);

        Assert.False(primera.Success);
        Assert.Equal(0, registry.ProcessedCount);
        Assert.Equal(1, registry.MarkFailedCalls);

        // Segunda ejecución: la tabla ya no falla y el partido se procesa.
        driver.MissingTableIds.Clear();
        driver.PrepareTables();
        ScrapingResult segunda = await service.ExtractMatchDetailsAsync(OutputDirectory);

        Assert.True(segunda.Success, string.Join(" | ", segunda.Errors));
        Assert.Equal(1, registry.ProcessedCount);
    }

    [Fact]
    public async Task Partidos_PartidoConErrores_NoSeMarcaComoProcesado()
    {
        // Arrange: la tabla del partido falla al exportarse (incidencia registrada).
        var driver = CreateDriverForMatches(teams: 1, matchesPerTeam: 1);
        driver.MissingTableIds.Add("tabla_partido");
        driver.PrepareTables();
        var registry = new FakeMatchLinkRegistry();
        var service = CreateService(driver, out _, out var exporter, registry);

        // Act
        ScrapingResult resultado = await service.ExtractMatchDetailsAsync(OutputDirectory);

        // Assert: el partido queda en estado Failed y NO como procesado (se reintentará).
        Assert.False(resultado.Success);
        Assert.Equal(0, registry.ProcessedCount);
        Assert.Equal(1, registry.MarkFailedCalls);
        Assert.Equal(6, exporter.Exports.Count); // 2 tablas con id restantes + scorebox + field_wrap + scorebox_meta + events_wrap
    }

    [Fact]
    public async Task Partidos_Interrupcion_ConservaProgresoYaRegistrado()
    {
        // Arrange: el primer partido se procesa por completo (3 tablas + scorebox
        // + field_wrap + scorebox_meta + events_wrap = 7 exportaciones); la primera
        // exportación del segundo partido lanza IOException (interrupción simulada).
        var driver = CreateDriverForMatches(teams: 1, matchesPerTeam: 2);
        var registry = new FakeMatchLinkRegistry();
        var factory = new FakeDriverFactory(driver);
        var options = new ScrapingOptions
        {
            WaitTimeoutSeconds = 1,
            HumanPauseMinMilliseconds = 0,
            HumanPauseMaxMilliseconds = 0,
            MatchPauseMinMilliseconds = 0,
            MatchPauseMaxMilliseconds = 0,
        };
        var service = new LeagueScrapingService(
            factory,
            new FailAfterExporter(maxSuccesses: 7),
            registry,
            options,
            NullLogger<LeagueScrapingService>.Instance);

        // Act
        await Assert.ThrowsAsync<IOException>(() => service.ExtractMatchDetailsAsync(OutputDirectory));

        // Assert: el partido completado quedó registrado; el driver se liberó.
        Assert.Equal(1, registry.ProcessedCount);
        Assert.Contains(registry.Records.Values, r => r.Url.Contains("partido-1") && r.Status == MatchLinkStatus.Processed);
        Assert.True(driver.QuitCalled);
    }

    [Fact]
    public async Task Partidos_LigaSinTablaPosiciones_ReportaErrorYContinua()
    {
        // Arrange
        var driver = CreateDriverForMatches(leagues: 2, teams: 1, matchesPerTeam: 1);
        driver.TablesVisible = false;
        var service = CreateService(driver, out _, out var exporter);

        // Act
        ScrapingResult resultado = await service.ExtractMatchDetailsAsync(OutputDirectory);

        // Assert: ninguna liga carga la tabla de posiciones.
        Assert.False(resultado.Success);
        Assert.Equal(0, resultado.LeaguesProcessed);
        Assert.Equal(2, resultado.Errors.Count);
        Assert.Contains(resultado.Errors, e => e.Contains("tabla de posiciones"));
        Assert.Empty(exporter.Exports);
        Assert.True(driver.QuitCalled);
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
    public async Task Partidos_ElementosOpcionalesFaltantes_NoImpidenRegistrarElPartido()
    {
        // Arrange: la página del partido no contiene los divs scorebox_meta ni events_wrap
        // (en FBref real scorebox_meta no existe); son complementarios y no bloquean el registro.
        var driver = CreateDriverForMatches(teams: 1, matchesPerTeam: 1);
        driver.ExtraElementsById.Clear();
        var registry = new FakeMatchLinkRegistry();
        var service = CreateService(driver, out _, out var exporter, registry);

        // Act
        ScrapingResult resultado = await service.ExtractMatchDetailsAsync(OutputDirectory);

        // Assert: las tablas con id sí se exportan; el partido SÍ se registra como procesado.
        Assert.True(resultado.Success, string.Join(" | ", resultado.Errors));
        Assert.Empty(resultado.Errors);
        Assert.Equal(3, exporter.Exports.Count); // las 3 tablas con id del partido
        Assert.Equal(1, registry.ProcessedCount);
    }

    [Fact]
    public async Task Partidos_Progreso_ReportaEquiposYEnlaces()
    {
        // Arrange
        var driver = CreateDriverForMatches(teams: 2, matchesPerTeam: 1);
        var service = CreateService(driver, out _, out _);
        var progress = new RecordingProgress();

        // Act
        await service.ExtractMatchDetailsAsync(OutputDirectory, progress);

        // Assert
        Assert.Contains(progress.Reports, r => r is { CurrentLeague: 1, TotalLeagues: 1, CurrentTable: 0 });
        Assert.Contains(progress.Reports, r => r.Message.Contains("Equipo 1/2"));
        Assert.Contains(progress.Reports, r => r.Message.Contains("Equipo 2/2"));
        Assert.Contains(progress.Reports, r => r.Message.Contains("Partido 1/1"));
    }

    // ------------------------------------------------------------------
    // Flujo "Partidos por Liga" (btnPartidosPorLiga): igual que el proceso de
    // partidos, pero sobre la liga que el navegador tiene abierta (sin recorrer
    // las 5 ligas principales).
    // ------------------------------------------------------------------

    [Fact]
    public async Task PartidosPorLiga_HappyPath_ProcesaSoloLaLigaActual()
    {
        // Arrange: el navegador está sobre la página de una liga.
        var driver = CreateDriverForMatches(teams: 2, matchesPerTeam: 2);
        var service = CreateService(driver, out var factory, out var exporter);
        string expectedDirectory = Path.Combine(OutputDirectory, "Detalles_Partidos");

        // Act
        ScrapingResult resultado = await service.ExtractCurrentLeagueMatchDetailsAsync(OutputDirectory);

        // Assert: 2 partidos × (3 tablas + 4 divs) = 14; el equipo 2 omite los ya
        // procesados por el 1 (mismo MatchId).
        Assert.True(resultado.Success, string.Join(" | ", resultado.Errors));
        Assert.Equal(1, resultado.LeaguesProcessed);
        Assert.Equal(14, resultado.TablesExported);
        Assert.Equal(14, exporter.Exports.Count);
        Assert.All(exporter.Exports, e => Assert.Equal(expectedDirectory, e.OutputDirectory));
        Assert.Equal(2, driver.VisitedUrls.Count(u => u.Contains("/equipos/")));
        Assert.Equal(2, driver.VisitedUrls.Count(u => u.Contains("/partidos/")));
        Assert.Equal(4, driver.NavigationsBack); // 2 (partido→equipo) + 2 (equipo→liga); sin vuelta a la página principal
        Assert.Equal(2, resultado.MatchesSkipped);
        Assert.True(driver.QuitCalled);
    }

    [Fact]
    public async Task PartidosPorLiga_SinTablaPosiciones_ReportaError()
    {
        // Arrange: la página actual no carga la tabla de posiciones.
        var driver = CreateDriverForMatches(teams: 2, matchesPerTeam: 2);
        driver.TablesVisible = false;
        var service = CreateService(driver, out _, out var exporter);

        // Act
        ScrapingResult resultado = await service.ExtractCurrentLeagueMatchDetailsAsync(OutputDirectory);

        // Assert
        Assert.False(resultado.Success);
        Assert.Equal(0, resultado.LeaguesProcessed);
        Assert.Contains(resultado.Errors, e => e.Contains("página actual") && e.Contains("tabla de posiciones"));
        Assert.Empty(exporter.Exports);
        Assert.True(driver.QuitCalled);
    }

    [Fact]
    public async Task PartidosPorLiga_Reanuda_SoloEnlacesPendientes()
    {
        // Arrange: el partido-1 ya fue procesado en una ejecución previa.
        var driver = CreateDriverForMatches(teams: 1, matchesPerTeam: 2);
        var registry = new FakeMatchLinkRegistry();
        string partido1 = "https://sitio-de-prueba.test/partidos/partido-1";
        registry.MarkProcessed(new MatchLinkTarget(
            MatchIdExtractor.ExtractId(partido1), partido1, "Equipo previo", RowIndex: 0));
        var service = CreateService(driver, out _, out var exporter, registry);

        // Act
        ScrapingResult resultado = await service.ExtractCurrentLeagueMatchDetailsAsync(OutputDirectory);

        // Assert: solo se procesa partido-2.
        Assert.True(resultado.Success, string.Join(" | ", resultado.Errors));
        Assert.Equal(1, resultado.MatchesSkipped);
        Assert.Equal(7, exporter.Exports.Count); // 1 partido × (3 tablas + 4 divs)
        Assert.Single(driver.VisitedUrls.Where(u => u.Contains("/partidos/") && u.Contains("partido-2")));
        Assert.DoesNotContain(driver.VisitedUrls, u => u.Contains("/partidos/") && u.Contains("partido-1"));
    }

    [Fact]
    public async Task PartidosPorLiga_Progreso_ReportaLigaUnica()
    {
        // Arrange
        var driver = CreateDriverForMatches(teams: 2, matchesPerTeam: 1);
        var service = CreateService(driver, out _, out _);
        var progress = new RecordingProgress();

        // Act
        await service.ExtractCurrentLeagueMatchDetailsAsync(OutputDirectory, progress);

        // Assert: una sola liga (1/1) y sus equipos.
        Assert.Contains(progress.Reports, r => r is { CurrentLeague: 1, TotalLeagues: 1, CurrentTable: 0 });
        Assert.Contains(progress.Reports, r => r.Message.Contains("Equipo 1/2"));
        Assert.Contains(progress.Reports, r => r.Message.Contains("Equipo 2/2"));
        Assert.DoesNotContain(progress.Reports, r => r.TotalLeagues > 1);
    }
}

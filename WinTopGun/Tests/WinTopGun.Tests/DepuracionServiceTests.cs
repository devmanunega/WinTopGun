using WinTopGun.Application.Services;
using WinTopGun.Domain.Models;

namespace WinTopGun.Tests;

/// <summary>
/// Pruebas de <see cref="DepuracionService"/>: validación de encabezados de los
/// archivos agrupados por patrón (Ideas/Clasificacion.md, punto 7).
/// </summary>
public sealed class DepuracionServiceTests : IDisposable
{
    private readonly DepuracionService _servicio = new();
    private readonly string _directorio;

    public DepuracionServiceTests()
    {
        _directorio = Path.Combine(Path.GetTempPath(), $"depura_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directorio);
    }

    public void Dispose()
    {
        Directory.Delete(_directorio, recursive: true);
    }

    private string CrearGrupo(string patron) =>
        Directory.CreateDirectory(Path.Combine(_directorio, patron)).FullName;

    private void CrearArchivo(string rutaGrupo, string nombre, string contenido) =>
        File.WriteAllText(Path.Combine(rutaGrupo, nombre), contenido);

    [Fact]
    public void Depurar_GrupoConsistente_NoReportaIncidencias()
    {
        string grupo = CrearGrupo("tabla_events");
        CrearArchivo(grupo, "liga1_tabla_events.txt", "Rk,Equipo,J,G\n1,A,10,8\n");
        CrearArchivo(grupo, "liga2_tabla_events.txt", "Rk,Equipo,J,G\n2,B,10,5\n");

        DepuracionInforme informe = _servicio.Depurar(_directorio);

        GrupoDepuracion g = Assert.Single(informe.Grupos);
        Assert.Equal("tabla_events", g.Patron);
        Assert.Equal(2, g.TotalArchivos);
        Assert.True(g.Consistente);
        Assert.Equal(0, informe.TotalIncidencias);
    }

    [Fact]
    public void Depurar_EncabezadosDistintos_ReportaLosQueNoEncajan()
    {
        string grupo = CrearGrupo("tabla_stats_summary");
        CrearArchivo(grupo, "a_tabla_stats_summary.txt", "Rk,Equipo,PJ\n");
        CrearArchivo(grupo, "b_tabla_stats_summary.txt", "Rk,Equipo,PJ\n");
        CrearArchivo(grupo, "c_tabla_stats_summary.txt", "Ranking,Team,MP\n");

        DepuracionInforme informe = _servicio.Depurar(_directorio);

        GrupoDepuracion g = Assert.Single(informe.Grupos);
        Assert.False(g.Consistente);
        Assert.Single(g.Incidencias);
        Assert.Contains("c_tabla_stats_summary.txt", g.Incidencias[0]);
        Assert.Contains("Ranking,Team,MP", g.Incidencias[0]);
        Assert.Equal(2, g.Encabezados.Count);
    }

    [Fact]
    public void Depurar_ArchivoVacio_ReportaIncidencia()
    {
        string grupo = CrearGrupo("tabla_events");
        CrearArchivo(grupo, "liga1_tabla_events.txt", "Rk,Equipo\n");
        CrearArchivo(grupo, "vacio_tabla_events.txt", "\n  \n");

        DepuracionInforme informe = _servicio.Depurar(_directorio);

        GrupoDepuracion g = Assert.Single(informe.Grupos);
        Assert.False(g.Consistente);
        Assert.Single(g.Incidencias);
        Assert.Contains("vacio_tabla_events.txt", g.Incidencias[0]);
        Assert.Contains("vacío", g.Incidencias[0]);
    }

    [Fact]
    public void Depurar_GrupoSinArchivos_ReportaIncidencia()
    {
        CrearGrupo("tabla_events");

        DepuracionInforme informe = _servicio.Depurar(_directorio);

        GrupoDepuracion g = Assert.Single(informe.Grupos);
        Assert.Equal(0, g.TotalArchivos);
        Assert.Single(g.Incidencias);
    }

    [Fact]
    public void Depurar_ConsideraSoloExtensionesPermitidas()
    {
        string grupo = CrearGrupo("tabla_events");
        CrearArchivo(grupo, "liga1_tabla_events.txt", "Rk,Equipo\n");
        CrearArchivo(grupo, "nota.txt.bak", "otro encabezado");

        DepuracionInforme informe = _servicio.Depurar(_directorio);

        Assert.Single(informe.Grupos);
        Assert.True(informe.Grupos[0].Consistente);
        Assert.Equal(1, informe.Grupos[0].TotalArchivos);
    }

    [Fact]
    public void Depurar_TodosLosArchivosVacios_SoloReportaVacios()
    {
        string grupo = CrearGrupo("tabla_events");
        CrearArchivo(grupo, "a_tabla_events.txt", "");
        CrearArchivo(grupo, "b_tabla_events.txt", "");

        DepuracionInforme informe = _servicio.Depurar(_directorio);

        GrupoDepuracion g = Assert.Single(informe.Grupos);
        Assert.Equal(2, g.Incidencias.Count);
        Assert.Empty(g.Encabezados);
    }

    [Fact]
    public void Depurar_DirectorioInexistente_LanzaExcepcion()
    {
        Assert.ThrowsAny<Exception>(() =>
            _servicio.Depurar(Path.Combine(_directorio, "no_existe")));
    }

    [Fact]
    public void Depurar_EspaciosMultiples_SeNormalizanAlComparar()
    {
        string grupo = CrearGrupo("tabla_events");
        CrearArchivo(grupo, "a_tabla_events.txt", "Rk   Equipo   PJ\n");
        CrearArchivo(grupo, "b_tabla_events.txt", "Rk Equipo PJ\n");

        DepuracionInforme informe = _servicio.Depurar(_directorio);

        Assert.True(informe.Grupos[0].Consistente);
    }
}

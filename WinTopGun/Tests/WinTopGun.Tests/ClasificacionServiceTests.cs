using WinTopGun.Application.Services;
using WinTopGun.Domain.Models;

namespace WinTopGun.Tests;

/// <summary>
/// Pruebas de <see cref="ClasificacionService"/>: extracción del patrón anclado
/// en la palabra "tabla" y ejecución del plan de organización
/// (ver Ideas/Clasificacion.md).
/// </summary>
public sealed class ClasificacionServiceTests : IDisposable
{
    private readonly ClasificacionService _servicio = new();
    private readonly string _directorio;

    public ClasificacionServiceTests()
    {
        _directorio = Path.Combine(Path.GetTempPath(), $"clasif_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directorio);
    }

    public void Dispose()
    {
        Directory.Delete(_directorio, recursive: true);
    }

    private string CrearArchivo(string nombre)
    {
        string ruta = Path.Combine(_directorio, nombre);
        File.WriteAllText(ruta, "contenido");
        return ruta;
    }

    #region ExtraerPatrón

    [Theory]
    [InlineData("archivo1_tabla_events.txt", "tabla_events")]
    [InlineData("x_tabla_events_wrap.txt", "tabla_events_wrap")]
    [InlineData("tabla_keeper_stats.txt", "tabla_keeper_stats")]
    [InlineData("archivo2_tabla_stats.txt", "tabla_stats")]
    [InlineData("tabla_stats_a8f3k2_summary.txt", "tabla_stats_summary")]
    [InlineData("tabla_stats_9c1d77_summary.txt", "tabla_stats_summary")]
    [InlineData("tabla_events_2.txt", "tabla_events")]
    [InlineData("reporte_TABLA_EVENTS.TXT", "tabla_events")]
    [InlineData("liga_tabla-general-stats.csv", "tabla_general_stats")]
    public void ExtraerPatron_NombresDelDocumento_GeneraPatronEsperado(string nombre, string esperado)
    {
        Assert.Equal(esperado, ClasificacionService.ExtraerPatron(nombre));
    }

    [Theory]
    [InlineData("notas.txt")]
    [InlineData("tablas.txt")]
    [InlineData("mitabla_events.txt")]
    [InlineData("tabla1_events.txt")]
    [InlineData("tabla.txt")]
    public void ExtraerPatron_SinAnclaValida_DevuelveNulo(string nombre)
    {
        Assert.Null(ClasificacionService.ExtraerPatron(nombre));
    }

    [Fact]
    public void ExtraerPatron_AnclaSinPalabrasValidas_AplicaSiguienteAncla()
    {
        // La primera aparición de "tabla" no tiene palabra válida después;
        // se usa la segunda aparición.
        Assert.Equal("tabla_events", ClasificacionService.ExtraerPatron("tabla_20260816_tabla_events.txt"));
    }

    [Fact]
    public void ExtraerPatron_PalabrasDeRuido_SeEliminan()
    {
        Assert.Equal("tabla_events", ClasificacionService.ExtraerPatron("tabla_events_final_copy.txt"));
    }

    [Fact]
    public void ExtraerPatron_TokensEliminados_NoCortanLaSecuencia()
    {
        Assert.Equal("tabla_stats_summary", ClasificacionService.ExtraerPatron("tabla_stats_01_final_summary.txt"));
    }

    [Fact]
    public void ExtraerPatron_Tildes_SeNormalizan()
    {
        Assert.Equal("tabla_eventos", ClasificacionService.ExtraerPatron("tabla_eventós.txt"));
    }

    #endregion

    #region GenerarPlan

    [Fact]
    public void GenerarPlan_EjemploDelDocumento_AgrupaPorPatronExacto()
    {
        foreach (string nombre in new[]
        {
            "archivo1_tabla_events.txt",
            "archivo4_tabla_events.txt",
            "x_tabla_events_wrap.txt",
            "tabla_keeper_stats.txt",
            "archivo2_tabla_stats.txt",
            "tabla_stats_a8f3k2_summary.txt",
            "tabla_stats_9c1d77_summary.txt",
            "notas.txt"
        })
        {
            CrearArchivo(nombre);
        }

        ClasificacionPlan plan = _servicio.GenerarPlan(_directorio);

        Assert.Equal(5, plan.Grupos.Count);
        Assert.Single(plan.SinClasificar);
        Assert.Equal("notas.txt", plan.SinClasificar[0]);

        var tablaEvents = plan.Grupos.Single(g => g.Patron == "tabla_events");
        Assert.Equal(2, tablaEvents.Archivos.Count);
        Assert.False(tablaEvents.Sospechoso);

        var tablaStatsSummary = plan.Grupos.Single(g => g.Patron == "tabla_stats_summary");
        Assert.Equal(2, tablaStatsSummary.Archivos.Count);

        var tablaKeeper = plan.Grupos.Single(g => g.Patron == "tabla_keeper_stats");
        Assert.True(tablaKeeper.Sospechoso);

        Assert.Equal(7, plan.TotalArchivosAMover);
    }

    [Fact]
    public void GenerarPlan_ConsolidadosYTemporales_SeIgnoran()
    {
        CrearArchivo("tabla_events_Consolidado.txt");
        CrearArchivo("~$tabla_events.tmp");
        CrearArchivo("tabla_events.dat");

        ClasificacionPlan plan = _servicio.GenerarPlan(_directorio);

        Assert.Empty(plan.Grupos);
        Assert.Equal(3, plan.Ignorados.Count);
    }

    [Fact]
    public void GenerarPlan_DirectorioInexistente_LanzaExcepcion()
    {
        Assert.ThrowsAny<Exception>(() => _servicio.GenerarPlan(Path.Combine(_directorio, "no_existe")));
    }

    #endregion

    #region EjecutarPlan

    [Fact]
    public void EjecutarPlan_MueveArchivosASubdirectorios_ConservandoNombre()
    {
        CrearArchivo("archivo1_tabla_events.txt");
        CrearArchivo("x_tabla_events_wrap.txt");
        CrearArchivo("notas.txt");

        ClasificacionPlan plan = _servicio.GenerarPlan(_directorio);
        ClasificacionResultado resultado = _servicio.EjecutarPlan(plan);

        Assert.True(resultado.Success);
        Assert.Equal(2, resultado.ArchivosMovidos);
        Assert.Equal(2, resultado.DirectoriosCreados);
        Assert.True(File.Exists(Path.Combine(_directorio, "tabla_events", "archivo1_tabla_events.txt")));
        Assert.True(File.Exists(Path.Combine(_directorio, "tabla_events_wrap", "x_tabla_events_wrap.txt")));
        Assert.True(File.Exists(Path.Combine(_directorio, "notas.txt")));
    }

    [Fact]
    public void EjecutarPlan_ArchivoExistenteEnDestino_NoSeSobrescribe()
    {
        CrearArchivo("archivo1_tabla_events.txt");
        ClasificacionPlan plan = _servicio.GenerarPlan(_directorio);
        ClasificacionResultado primera = _servicio.EjecutarPlan(plan);
        Assert.True(primera.Success);

        // Se restaura el mismo archivo en la raíz: el destino ya lo tiene.
        CrearArchivo("archivo1_tabla_events.txt");
        ClasificacionPlan plan2 = _servicio.GenerarPlan(_directorio);
        ClasificacionResultado segunda = _servicio.EjecutarPlan(plan2);

        Assert.False(segunda.Success);
        Assert.Equal(0, segunda.ArchivosMovidos);
        Assert.Single(segunda.Errors);
        Assert.True(File.Exists(Path.Combine(_directorio, "tabla_events", "archivo1_tabla_events.txt")));
        Assert.True(File.Exists(Path.Combine(_directorio, "archivo1_tabla_events.txt")));
    }

    [Fact]
    public void EjecutarPlan_DirectorioExistente_SeReutiliza()
    {
        Directory.CreateDirectory(Path.Combine(_directorio, "tabla_events"));
        CrearArchivo("archivo1_tabla_events.txt");

        ClasificacionPlan plan = _servicio.GenerarPlan(_directorio);
        ClasificacionResultado resultado = _servicio.EjecutarPlan(plan);

        Assert.True(resultado.Success);
        Assert.Equal(0, resultado.DirectoriosCreados);
        Assert.True(plan.Grupos[0].DirectorioExistente);
    }

    #endregion
}

using Microsoft.Extensions.DependencyInjection;
using WinTopGun.Application.Interfaces;
using WinTopGun.Application.Services;
using WinTopGun.Domain.Models;
using WinTopGun.UI.Forms.Opciones;

namespace WinTopGun;

/// <summary>
/// Formulario principal. Solo orquesta la interacción del usuario con los
/// servicios de aplicación: la lógica de extracción vive en <see cref="LeagueScrapingService"/>.
/// </summary>
public partial class frmMain : Form
{
    private readonly IScrapingService _scrapingService;
    private readonly ISettingsProvider _settingsProvider;
    private readonly IMatchProgressRepositoryFactory _progressRepositoryFactory;
    private CancellationTokenSource? _cancelacion;

    /// <summary>Constructor usado por el diseñador y por <see cref="Program"/>.</summary>
    public frmMain() : this(Composition.Services)
    {
    }

    /// <summary>Constructor con inyección de dependencias (útil para pruebas).</summary>
    public frmMain(IServiceProvider services)
    {
        InitializeComponent();
        _scrapingService = services.GetRequiredService<IScrapingService>();
        _settingsProvider = services.GetRequiredService<ISettingsProvider>();
        _progressRepositoryFactory = services.GetRequiredService<IMatchProgressRepositoryFactory>();
    }

    /// <summary>
    /// Opciones → Reiniciar registro de partidos: elimina el registro de progreso
    /// para que la próxima extracción reprocese todos los partidos.
    /// </summary>
    private void reiniciarRegistroPartidosToolStripMenuItem_Click(object sender, EventArgs e)
    {
        string outputDirectory = _settingsProvider.OutputDirectory;

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            MessageBox.Show(
                "Primero configure el directorio de destino (Opciones → Directorio destino).",
                "Directorio no configurado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var respuesta = MessageBox.Show(
            "Se eliminará el registro de partidos procesados. La próxima extracción reprocesará todos los partidos (los archivos existentes se sobrescribirán).\n\n¿Desea continuar?",
            "Reiniciar registro de partidos",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (respuesta != DialogResult.Yes)
        {
            return;
        }

        string matchDetailsDirectory = Path.Combine(outputDirectory, LeagueScrapingService.MatchDetailsSubfolder);
        _progressRepositoryFactory.Create(matchDetailsDirectory).Reset();

        MessageBox.Show(
            "Registro de partidos eliminado.",
            "Operación completada",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void directorioDestinoToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using var frmConfiguracion = new frmConfiguracion(_settingsProvider);
        frmConfiguracion.ShowDialog(this);
    }

    /// <summary>Proceso de equipos: recorre las ligas y extrae sus tablas con <c>id</c>.</summary>
    private async void btnExtract_Click(object sender, EventArgs e) =>
        await EjecutarExtraccionAsync(
            (servicio, directorio, progreso, token) =>
                servicio.ExtractLeagueTablesAsync(directorio, progreso, token));

    /// <summary>
    /// Proceso de jugadores: recorre las ligas, abre solo los enlaces en posición
    /// par del selector de jugadores y extrae las tablas con <c>id</c> de cada página.
    /// </summary>
    private async void btnExtractPlayers_Click(object sender, EventArgs e) =>
        await EjecutarExtraccionAsync(
            (servicio, directorio, progreso, token) =>
                servicio.ExtractPlayerTablesAsync(directorio, progreso, token));

    /// <summary>
    /// Proceso de partidos: recorre las ligas, itera los equipos de su tabla de
    /// clasificación, abre el reporte de cada partido y extrae las tablas con
    /// <c>id</c> y los divs <c>scorebox_meta</c> y <c>events_wrap</c> de cada
    /// página. Los archivos se guardan en la subcarpeta <c>Detalles_Partidos</c>.
    /// </summary>
    private async void btnExtractPartidos_Click(object sender, EventArgs e) =>
        await EjecutarExtraccionAsync(
            (servicio, directorio, progreso, token) =>
                servicio.ExtractMatchDetailsAsync(directorio, progreso, token));

    /// <summary>
    /// Orquestación compartida por ambos procesos de extracción: valida el directorio
    /// destino, ejecuta la operación con progreso y cancelación, y muestra el resultado.
    /// </summary>
    /// <param name="operacion">Operación de extracción a ejecutar sobre el servicio.</param>
    private async Task EjecutarExtraccionAsync(
        Func<IScrapingService, string, IProgress<ScrapingProgress>, CancellationToken, Task<ScrapingResult>> operacion)
    {
        string outputDirectory = _settingsProvider.OutputDirectory;

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            var respuesta = MessageBox.Show(
                "No se ha configurado el directorio de destino. ¿Desea configurarlo ahora?",
                "Configuración requerida",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (respuesta == DialogResult.Yes)
            {
                directorioDestinoToolStripMenuItem_Click(this, EventArgs.Empty);
            }

            return;
        }

        _cancelacion = new CancellationTokenSource();
        SetOperacionEnCurso(true);

        var progress = new Progress<ScrapingProgress>(p =>
        {
            lblEstado.Text = p.Message;
            progressBarExtraccion.Maximum = Math.Max(p.TotalLeagues, 1);
            progressBarExtraccion.Value = Math.Clamp(p.CurrentLeague, 0, progressBarExtraccion.Maximum);
        });

        try
        {
            ScrapingResult resultado = await operacion(_scrapingService, outputDirectory, progress, _cancelacion.Token)
                .ConfigureAwait(true);

            MostrarResultado(resultado);
        }
        catch (OperationCanceledException)
        {
            MessageBox.Show("La extracción fue cancelada.", "Información",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Ocurrió un error inesperado durante la extracción:{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            SetOperacionEnCurso(false);
            _cancelacion?.Dispose();
            _cancelacion = null;
        }
    }

    private void btnCancelar_Click(object sender, EventArgs e)
    {
        _cancelacion?.Cancel();
        lblEstado.Text = "Cancelando...";
    }

    private void MostrarResultado(ScrapingResult resultado)
    {
        if (resultado.WasCancelled)
        {
            MessageBox.Show("La extracción fue cancelada.", "Información",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else if (resultado.Success)
        {
            string omitidos = resultado.MatchesSkipped > 0
                ? $" Se omitieron {resultado.MatchesSkipped} partido(s) ya procesados en ejecuciones anteriores."
                : string.Empty;

            MessageBox.Show(
                $"Extracción finalizada: {resultado.TablesExported} tablas exportadas de {resultado.LeaguesProcessed} ligas.{omitidos}",
                "Proceso completado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        else
        {
            string detalle = string.Join(Environment.NewLine, resultado.Errors.Take(5));
            MessageBox.Show(
                $"Extracción finalizada con {resultado.Errors.Count} incidencia(s).{Environment.NewLine}" +
                $"Se exportaron {resultado.TablesExported} tablas.{Environment.NewLine}{Environment.NewLine}{detalle}",
                "Proceso completado con advertencias",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void SetOperacionEnCurso(bool enCurso)
    {
        btnExtract.Enabled = !enCurso;
        btnExtractPlayers.Enabled = !enCurso;
        btnExtractPartidos.Enabled = !enCurso;
        btnCancelar.Enabled = enCurso;

        if (!enCurso)
        {
            lblEstado.Text = "Listo.";
            progressBarExtraccion.Value = 0;
        }
    }
}

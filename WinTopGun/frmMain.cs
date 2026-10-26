using Microsoft.Extensions.DependencyInjection;
using System.Text;
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
    private readonly IMatchLinkRegistry _matchLinkRegistry;
    private readonly IClasificacionService _clasificacionService;
    private readonly IDepuracionService _depuracionService;
    private CancellationTokenSource? _cancelacion;

    /// <summary>
    /// Directorio seleccionado para los procesos de clasificación
    /// (organizar y depurar). Se elige con el botón "Directorio".
    /// </summary>
    private string? _directorioClasificacion;

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
        _matchLinkRegistry = services.GetRequiredService<IMatchLinkRegistry>();
        _clasificacionService = services.GetRequiredService<IClasificacionService>();
        _depuracionService = services.GetRequiredService<IDepuracionService>();

        // Mostrar desde el arranque el directorio destino configurado: evita que
        // una extracción se ejecute contra una ruta que el usuario creía cambiada.
        ActualizarEstadoDirectorioDestino();
    }

    /// <summary>
    /// Refleja en la barra de estado el directorio destino actualmente
    /// configurado, para que siempre sea visible antes de extraer.
    /// </summary>
    private void ActualizarEstadoDirectorioDestino()
    {
        string ruta = _settingsProvider.OutputDirectory;
        lblEstado.Text = string.IsNullOrWhiteSpace(ruta)
            ? "Directorio destino: (sin configurar) — Opciones → Directorio destino"
            : $"Directorio destino: {ruta}";
    }

    /// <summary>
    /// Opciones → Reiniciar registro de partidos: elimina el registro de enlaces
    /// procesados para que la próxima extracción reprocese todos los partidos.
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
        _matchLinkRegistry.Initialize(matchDetailsDirectory);
        _matchLinkRegistry.Reset();

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

        // Al cerrar la configuración, refrescar la ruta mostrada (pudo cambiar).
        ActualizarEstadoDirectorioDestino();
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
    /// Proceso de partidos: recorre las ligas, recolecta de la tabla de posiciones
    /// (primera tabla con <c>id</c>) los enlaces "Match Report" de cada equipo,
    /// los identifica por hash en un registro persistente y abre solo los no
    /// procesados. De cada página extrae las tablas con <c>id</c>, los divs
    /// <c>scorebox</c>, <c>field_wrap</c> y <c>events_wrap</c> (como outerHTML)
    /// y el div <c>scorebox_meta</c> (como texto), en la subcarpeta
    /// <c>Detalles_Partidos</c>.
    /// </summary>
    private async void btnExtractPartidos_Click(object sender, EventArgs e) =>
        await EjecutarExtraccionAsync(
            (servicio, directorio, progreso, token) =>
                servicio.ExtractMatchDetailsAsync(directorio, progreso, token));

    /// <summary>
    /// Proceso "Partidos por Liga": igual que el de partidos, pero no recorre las
    /// 5 ligas principales — procesa los equipos de la tabla de posiciones de la
    /// liga que el navegador tenga abierta en ese momento.
    /// </summary>
    private async void btnPartidosPorLiga_Click(object sender, EventArgs e) =>
        await EjecutarExtraccionAsync(
            (servicio, directorio, progreso, token) =>
                servicio.ExtractCurrentLeagueMatchDetailsAsync(directorio, progreso, token));

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

            MostrarResultado(resultado, outputDirectory);
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

    /// <summary>
    /// Clasificación → Directorio: selecciona la carpeta cuyos archivos se
    /// organizarán y depurarán según el patrón anclado en la palabra "tabla"
    /// (ver Ideas/Clasificacion.md).
    /// </summary>
    private void btnClasificacionDirectorio_Click(object sender, EventArgs e)
    {
        using var dialogo = new FolderBrowserDialog
        {
            Description = "Seleccione el directorio cuyos archivos desea clasificar.",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (!string.IsNullOrWhiteSpace(_directorioClasificacion)
            && Directory.Exists(_directorioClasificacion))
        {
            dialogo.SelectedPath = _directorioClasificacion;
        }

        if (dialogo.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _directorioClasificacion = dialogo.SelectedPath;
        lblEstado.Text = $"Directorio de clasificación: {_directorioClasificacion}";
    }

    /// <summary>
    /// Clasificación → Organizar: genera el plan a partir del directorio
    /// seleccionado, muestra una vista previa de los grupos y, si el usuario
    /// confirma, crea los subdirectorios y mueve los archivos.
    /// </summary>
    private void btnClasificacionOrganizar_Click(object sender, EventArgs e)
    {
        if (!ValidarDirectorioClasificacion())
        {
            return;
        }

        ClasificacionPlan plan;
        try
        {
            plan = _clasificacionService.GenerarPlan(_directorioClasificacion!);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"No se pudo analizar el directorio:{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        if (plan.TotalArchivosAMover == 0)
        {
            MessageBox.Show(
                "No se encontraron archivos clasificables en el directorio seleccionado.",
                "Nada por organizar",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        string vistaPrevia = ConstruirVistaPrevia(plan);

        if (MessageBox.Show(
                vistaPrevia,
                "Vista previa de organización",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        ClasificacionResultado resultado;
        try
        {
            resultado = _clasificacionService.EjecutarPlan(plan);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Ocurrió un error inesperado durante la organización:{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        MostrarResultadoOrganizacion(resultado);
    }

    /// <summary>Construye el texto de vista previa del plan de organización.</summary>
    private static string ConstruirVistaPrevia(ClasificacionPlan plan)
    {
        var detalle = new StringBuilder();
        detalle.AppendLine($"Directorio: {plan.DirectorioOrigen}");
        detalle.AppendLine();
        detalle.AppendLine($"Se crearán/usarán {plan.Grupos.Count} directorio(s):");

        foreach (var grupo in plan.Grupos)
        {
            string advertencia = grupo.Sospechoso ? "  [advertencia: un solo archivo]" : string.Empty;
            detalle.AppendLine($"  {grupo.Patron}  ({grupo.Archivos.Count} archivo(s)){advertencia}");
        }

        if (plan.SinClasificar.Count > 0)
        {
            detalle.AppendLine();
            detalle.AppendLine($"{plan.SinClasificar.Count} archivo(s) sin ancla 'tabla' no se moverán.");
        }

        detalle.AppendLine();
        detalle.Append("¿Desea ejecutar la organización?");
        return detalle.ToString();
    }

    /// <summary>Muestra el resultado de la ejecución del plan.</summary>
    private static void MostrarResultadoOrganizacion(ClasificacionResultado resultado)
    {
        if (resultado.Success)
        {
            MessageBox.Show(
                $"Organización finalizada: {resultado.ArchivosMovidos} archivo(s) movidos " +
                $"en {resultado.DirectoriosCreados} directorio(s) nuevo(s).",
                "Proceso completado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        string detalle = string.Join(Environment.NewLine, resultado.Errors.Take(5));
        MessageBox.Show(
            $"Organización finalizada con {resultado.Errors.Count} incidencia(s)." +
            $"{Environment.NewLine}Se movieron {resultado.ArchivosMovidos} archivo(s)." +
            $"{Environment.NewLine}{Environment.NewLine}{detalle}",
            "Proceso completado con advertencias",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    /// <summary>
    /// Clasificación → Depurar: valida la estructura interna (encabezados) de
    /// los archivos ya agrupados por el proceso de organización y muestra un
    /// informe con las incidencias detectadas.
    /// </summary>
    private void btnClasificacionDepurar_Click(object sender, EventArgs e)
    {
        if (!ValidarDirectorioClasificacion())
        {
            return;
        }

        DepuracionInforme informe;
        try
        {
            informe = _depuracionService.Depurar(_directorioClasificacion!);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"No se pudo depurar el directorio:{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        MostrarInformeDepuracion(informe);
    }

    /// <summary>Muestra el informe de depuración agrupado por patrón.</summary>
    private static void MostrarInformeDepuracion(DepuracionInforme informe)
    {
        if (informe.Grupos.Count == 0)
        {
            MessageBox.Show(
                "No se encontraron subdirectorios para depurar. Ejecute primero el proceso 'Organizar'.",
                "Nada por depurar",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var detalle = new StringBuilder();
        detalle.AppendLine($"Directorio: {informe.Directorio}");
        detalle.AppendLine();

        foreach (var grupo in informe.Grupos)
        {
            if (grupo.Consistente)
            {
                detalle.AppendLine($"✔ {grupo.Patron}: {grupo.TotalArchivos} archivo(s), encabezados consistentes.");
                continue;
            }

            detalle.AppendLine($"⚠ {grupo.Patron}: {grupo.TotalArchivos} archivo(s), {grupo.Incidencias.Count} incidencia(s):");

            foreach (string incidencia in grupo.Incidencias.Take(3))
            {
                detalle.AppendLine($"   - {incidencia}");
            }

            if (grupo.Incidencias.Count > 3)
            {
                detalle.AppendLine($"   ... y {grupo.Incidencias.Count - 3} más.");
            }
        }

        string titulo = informe.TotalIncidencias == 0 ? "Depuración sin incidencias" : "Depuración con incidencias";
        MessageBoxIcon icono = informe.TotalIncidencias == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning;

        detalle.AppendLine();
        detalle.Append($"{informe.TotalArchivos} archivo(s) analizados en {informe.Grupos.Count} grupo(s); " +
            $"{informe.TotalIncidencias} incidencia(s) en total.");

        MessageBox.Show(detalle.ToString(), titulo, MessageBoxButtons.OK, icono);
    }

    /// <summary>
    /// Verifica que se haya seleccionado un directorio válido para los
    /// procesos de clasificación; si no, lo informa al usuario.
    /// </summary>
    private bool ValidarDirectorioClasificacion()
    {
        if (!string.IsNullOrWhiteSpace(_directorioClasificacion)
            && Directory.Exists(_directorioClasificacion))
        {
            return true;
        }

        MessageBox.Show(
            "Primero seleccione el directorio a clasificar (botón Directorio).",
            "Directorio no seleccionado",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        return false;
    }

    private void MostrarResultado(ScrapingResult resultado, string outputDirectory)
    {
        string destino = $"Los archivos se guardaron en:{Environment.NewLine}{outputDirectory}";

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
                $"Extracción finalizada: {resultado.TablesExported} tablas exportadas de {resultado.LeaguesProcessed} ligas.{omitidos}{Environment.NewLine}{Environment.NewLine}{destino}",
                "Proceso completado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        else
        {
            string detalle = string.Join(Environment.NewLine, resultado.Errors.Take(5));
            MessageBox.Show(
                $"Extracción finalizada con {resultado.Errors.Count} incidencia(s).{Environment.NewLine}" +
                $"Se exportaron {resultado.TablesExported} tablas.{Environment.NewLine}{Environment.NewLine}{detalle}" +
                $"{Environment.NewLine}{Environment.NewLine}{destino}",
                "Proceso completado con advertencias",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void SetOperacionEnCurso(bool enCurso)
    {
        // Se recorren los botones del grupo de procesos: al agregar un botón
        // de extracción al diseñador queda cubierto automáticamente.
        foreach (Control control in flowProcesos.Controls)
        {
            if (control is Button boton)
            {
                boton.Enabled = !enCurso;
            }
        }

        btnCancelar.Enabled = enCurso;

        if (!enCurso)
        {
            lblEstado.Text = "Listo.";
            progressBarExtraccion.Value = 0;
        }
    }
}

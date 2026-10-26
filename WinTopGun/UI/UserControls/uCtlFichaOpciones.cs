using Microsoft.Extensions.DependencyInjection;
using WinTopGun.Application.Interfaces;

namespace WinTopGun.UI.UserControls;

/// <summary>
/// Ficha de opciones para configurar el directorio destino de los datos extraídos.
/// Muestra en todo momento la ruta actualmente configurada para que el usuario
/// pueda verificarla antes de ejecutar un proceso de extracción.
/// </summary>
public partial class uCtlFichaOpciones : UserControl
{
    private readonly ISettingsProvider _settingsProvider;

    /// <summary>Constructor usado por el diseñador.</summary>
    public uCtlFichaOpciones() : this(Composition.Services.GetRequiredService<ISettingsProvider>())
    {
    }

    /// <summary>Constructor con inyección de dependencias.</summary>
    public uCtlFichaOpciones(ISettingsProvider settingsProvider)
    {
        InitializeComponent();
        _settingsProvider = settingsProvider;

        // Toda la ficha es clicable: los textos junto al icono también abren el
        // selector, para que no exista una zona "muerta" que parezca interactiva.
        lblTituloDirectorio.Click += picBoxDirectorio_Click;
        lblSubtituloDirectorio.Click += picBoxDirectorio_Click;
        lblTituloDirectorio.Cursor = Cursors.Hand;
        lblSubtituloDirectorio.Cursor = Cursors.Hand;
    }

    private void picBoxDirectorio_Click(object? sender, EventArgs e)
    {
        string rutaActual = _settingsProvider.OutputDirectory;

        using var folderBrowserDialog = new FolderBrowserDialog
        {
            Description = "Seleccione el directorio donde se guardarán los datos extraídos.",
            ShowNewFolderButton = true,
            ShowPinnedPlaces = true,
        };

        // Preseleccionar la ruta vigente: deja claro cuál está configurada y evita
        // guardar por accidente una carpeta distinta de la que el usuario creía.
        if (!string.IsNullOrWhiteSpace(rutaActual) && Directory.Exists(rutaActual))
        {
            folderBrowserDialog.SelectedPath = rutaActual;
        }

        if (folderBrowserDialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        string nuevaRuta = folderBrowserDialog.SelectedPath;

        // PLAN 2.6: validar que el directorio exista antes de guardar.
        if (!Directory.Exists(nuevaRuta))
        {
            MessageBox.Show(
                $"El directorio seleccionado no existe:\n\n{nuevaRuta}\n\nNo se guardó la configuración.",
                "Directorio no válido",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (string.Equals(nuevaRuta, rutaActual, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(
                $"El directorio seleccionado ya es el configurado actualmente:\n\n{nuevaRuta}",
                "Sin cambios",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        _settingsProvider.OutputDirectory = nuevaRuta;
        _settingsProvider.Save();
        MostrarRutaActual();

        MessageBox.Show(
            $"Directorio de destino actualizado correctamente. Los archivos se guardarán en:\n\n{nuevaRuta}",
            "Información",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void uCtlFichaOpciones_Load(object sender, EventArgs e)
    {
        MostrarRutaActual();
    }

    /// <summary>Refleja en la ficha la ruta de destino actualmente configurada.</summary>
    private void MostrarRutaActual()
    {
        string ruta = _settingsProvider.OutputDirectory;
        lblRutaActual.Text = string.IsNullOrWhiteSpace(ruta) ? "(sin configurar)" : ruta;
    }
}

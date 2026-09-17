using Microsoft.Extensions.DependencyInjection;
using WinTopGun.Application.Interfaces;

namespace WinTopGun.UI.UserControls;

/// <summary>
/// Ficha de opciones para configurar el directorio destino de los datos extraídos.
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
    }

    private void picBoxDirectorio_Click(object sender, EventArgs e)
    {
        using var folderBrowserDialog = new FolderBrowserDialog
        {
            ShowNewFolderButton = true,
            ShowPinnedPlaces = true,
        };

        if (folderBrowserDialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        _settingsProvider.OutputDirectory = folderBrowserDialog.SelectedPath;
        _settingsProvider.Save();

        MessageBox.Show(
            $"{folderBrowserDialog.SelectedPath} - Directorio de destino actualizado correctamente.",
            "Información",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void uCtlFichaOpciones_Load(object sender, EventArgs e)
    {

    }
}

using Microsoft.Extensions.DependencyInjection;
using WinTopGun.Application.Interfaces;
using WinTopGun.UI.UserControls;

namespace WinTopGun.UI.Forms.Opciones;

/// <summary>
/// Formulario de configuración de la aplicación.
/// </summary>
public partial class frmConfiguracion : Form
{
    private readonly ISettingsProvider _settingsProvider;

    /// <summary>Constructor usado por el diseñador.</summary>
    public frmConfiguracion() : this(Composition.Services.GetRequiredService<ISettingsProvider>())
    {
    }

    /// <summary>Constructor con inyección de dependencias.</summary>
    public frmConfiguracion(ISettingsProvider settingsProvider)
    {
        InitializeComponent();
        _settingsProvider = settingsProvider;
    }

    private void frmConfiguracion_Load(object sender, EventArgs e)
    {
        floLaPaContainer.Controls.Add(new uCtlFichaOpciones(_settingsProvider));
    }
}

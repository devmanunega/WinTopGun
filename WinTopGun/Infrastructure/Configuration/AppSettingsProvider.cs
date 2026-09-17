using WinTopGun.Application.Interfaces;

namespace WinTopGun.Infrastructure.Configuration;

/// <summary>
/// Implementación de <see cref="ISettingsProvider"/> sobre <c>Properties.Settings</c>.
/// Mantiene el mecanismo de persistencia aislado del resto de la aplicación.
/// </summary>
public sealed class AppSettingsProvider : ISettingsProvider
{
    /// <inheritdoc />
    public string OutputDirectory
    {
        get => Properties.Settings.Default.RutaDatosExtraidos;
        set => Properties.Settings.Default.RutaDatosExtraidos = value;
    }

    /// <inheritdoc />
    public void Save() => Properties.Settings.Default.Save();
}

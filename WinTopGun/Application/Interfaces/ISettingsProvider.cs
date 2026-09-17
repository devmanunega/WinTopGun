namespace WinTopGun.Application.Interfaces;

/// <summary>
/// Acceso abstracto a la configuración persistente de la aplicación,
/// sin acoplar la capa de aplicación a <c>Properties.Settings</c>.
/// </summary>
public interface ISettingsProvider
{
    /// <summary>Directorio donde se guardan los datos extraídos.</summary>
    string OutputDirectory { get; set; }

    /// <summary>Persiste los cambios de configuración.</summary>
    void Save();
}

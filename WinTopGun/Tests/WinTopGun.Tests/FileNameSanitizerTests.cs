using WinTopGun.Application.Services;

namespace WinTopGun.Tests;

/// <summary>
/// Pruebas de <see cref="FileNameSanitizer"/>: garantiza nombres de archivo
/// seguros en Windows.
/// </summary>
public sealed class FileNameSanitizerTests
{
    [Theory]
    [InlineData("liga?espana*2025", "liga_espana_2025")]
    [InlineData("archivo/con:barra", "archivo_con_barra")]
    [InlineData("comillas\"dobles", "comillas_dobles")]
    [InlineData("mayor>menor<", "mayor_menor_")]
    public void Sanitize_ReemplazaCaracteresInvalidos(string input, string expected)
    {
        Assert.Equal(expected, FileNameSanitizer.Sanitize(input));
    }

    [Fact]
    public void Sanitize_TextoVacio_DevuelveNombreDeRespaldo()
    {
        Assert.Equal("sin_nombre", FileNameSanitizer.Sanitize(string.Empty));
        Assert.Equal("sin_nombre", FileNameSanitizer.Sanitize("   "));
    }

    [Fact]
    public void Sanitize_TextoValido_NoSeModifica()
    {
        const string valid = "primeraliga_tabla_general";
        Assert.Equal(valid, FileNameSanitizer.Sanitize(valid));
    }
}

using WinTopGun.Infrastructure.Persistence;

namespace WinTopGun.Tests;

/// <summary>
/// Pruebas de <see cref="TextFileTableExporter"/> usando un directorio temporal real.
/// </summary>
public sealed class TextFileTableExporterTests : IDisposable
{
    private readonly string _tempDirectory;

    public TextFileTableExporterTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"wintopgun-tests-{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ExportTableAsync_EscribeContenidoConNombreEsperado()
    {
        var exporter = new TextFileTableExporter();

        string path = await exporter.ExportTableAsync(_tempDirectory, "primeraliga", "general", "contenido de prueba");

        Assert.True(File.Exists(path));
        Assert.EndsWith("primeraliga_tabla_general.txt", path);
        Assert.Equal("contenido de prueba", await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task ExportTableAsync_CreaElDirectorioSiNoExiste()
    {
        var exporter = new TextFileTableExporter();
        string nestedDirectory = Path.Combine(_tempDirectory, "sub", "dir");

        await exporter.ExportTableAsync(nestedDirectory, "liga", "t1", "datos");

        Assert.True(Directory.Exists(nestedDirectory));
        Assert.True(File.Exists(Path.Combine(nestedDirectory, "liga_tabla_t1.txt")));
    }

    [Fact]
    public async Task ExportTableAsync_SanitizaIdsConCaracteresInvalidos()
    {
        var exporter = new TextFileTableExporter();

        string path = await exporter.ExportTableAsync(_tempDirectory, "liga", "id:con?caracteres", "datos");

        string fileName = Path.GetFileName(path);
        Assert.DoesNotContain(":", fileName);
        Assert.DoesNotContain("?", fileName);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public async Task ExportTableAsync_ContenidoVacio_CreaArchivoVacio()
    {
        var exporter = new TextFileTableExporter();

        string path = await exporter.ExportTableAsync(_tempDirectory, "liga", "vacia", string.Empty);

        Assert.True(File.Exists(path));
        Assert.Equal(string.Empty, await File.ReadAllTextAsync(path)); // el BOM UTF-8 no produce contenido visible
    }
}

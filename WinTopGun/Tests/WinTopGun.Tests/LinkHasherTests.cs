using WinTopGun.Application.Services;

namespace WinTopGun.Tests;

/// <summary>
/// Pruebas de <see cref="LinkHasher"/>: el hash identifica de forma estable cada
/// enlace de partido (1.4 del proceso de partidos).
/// </summary>
public sealed class LinkHasherTests
{
    [Fact]
    public void MismoEnlace_ProduceElMismoHash()
    {
        string url = "https://sitio-de-prueba.test/partidos/partido-1";

        Assert.Equal(LinkHasher.ComputeHash(url), LinkHasher.ComputeHash(url));
    }

    [Fact]
    public void EnlacesDistintos_ProducenHashesDistintos()
    {
        string hash1 = LinkHasher.ComputeHash("https://sitio-de-prueba.test/partidos/partido-1");
        string hash2 = LinkHasher.ComputeHash("https://sitio-de-prueba.test/partidos/partido-2");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HashEsHexadecimalDe64Caracteres()
    {
        string hash = LinkHasher.ComputeHash("https://sitio-de-prueba.test/partidos/partido-3");

        Assert.Equal(64, hash.Length); // SHA-256 = 256 bits = 64 caracteres hex
        Assert.All(hash, c => Assert.True(
            char.IsAsciiHexDigit(c),
            $"Carácter '{c}' no es hexadecimal."));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EnlaceVacio_LanzaArgumentException(string url)
    {
        Assert.Throws<ArgumentException>(() => LinkHasher.ComputeHash(url));
    }
}

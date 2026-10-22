using WinTopGun.Application.Services;

namespace WinTopGun.Tests;

/// <summary>
/// Pruebas de <see cref="MatchIdExtractor"/>: el ID del partido (segmento posterior
/// a <c>/matches/</c>) es la clave de deduplicación, más confiable que la URL
/// completa cuyo slug puede cambiar (Ideas/Partidos.md, observaciones del HTML).
/// </summary>
public sealed class MatchIdExtractorTests
{
    [Fact]
    public void UrlDeFbref_ExtraeElIdDelPartido()
    {
        string url = "https://fbref.com/en/matches/d5932b03/Arsenal-Manchester-City-August-16-2026";

        Assert.Equal("d5932b03", MatchIdExtractor.ExtractId(url));
    }

    [Fact]
    public void MismoPartidoConSlugDistinto_MismoId()
    {
        string url1 = "https://fbref.com/en/matches/d5932b03/Arsenal-Manchester-City-August-16-2026";
        string url2 = "https://fbref.com/en/matches/d5932b03/Slug-Cambiado-En-El-Futuro";

        Assert.Equal(MatchIdExtractor.ExtractId(url1), MatchIdExtractor.ExtractId(url2));
    }

    [Fact]
    public void PartidosDistintos_IdsDistintos()
    {
        string id1 = MatchIdExtractor.ExtractId("https://fbref.com/en/matches/10027be4/Tottenham-Newcastle");
        string id2 = MatchIdExtractor.ExtractId("https://fbref.com/en/matches/198be7a7/Newcastle-Bournemouth");

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void UrlSinPatronMatches_DevuelveHashEstable()
    {
        string url = "https://sitio-de-prueba.test/partido/xyz";
        string id1 = MatchIdExtractor.ExtractId(url);
        string id2 = MatchIdExtractor.ExtractId(url);

        Assert.Equal(id1, id2);
        Assert.NotEqual(url, id1);
        Assert.Equal(64, id1.Length); // fallback: hash SHA-256 hexadecimal
    }
}

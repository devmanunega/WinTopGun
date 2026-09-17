using WinTopGun.Domain.Models;

namespace WinTopGun.Tests;

public sealed class ScrapingResultTests
{
    [Fact]
    public void Success_SinErroresYNoCancelado_EsVerdadero()
    {
        var result = new ScrapingResult();
        Assert.True(result.Success);
    }

    [Fact]
    public void Success_ConErrores_EsFalso()
    {
        var result = new ScrapingResult();
        result.Errors.Add("algún error");
        Assert.False(result.Success);
    }

    [Fact]
    public void Success_Cancelado_EsFalso()
    {
        var result = new ScrapingResult { WasCancelled = true };
        Assert.False(result.Success);
    }
}

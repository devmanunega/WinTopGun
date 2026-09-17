using OpenQA.Selenium;
using WinTopGun.Application.Interfaces;

namespace WinTopGun.Tests.Fakes;

/// <summary>Fake de <see cref="IDriverFactory"/> que entrega un <see cref="FakeWebDriver"/>.</summary>
internal sealed class FakeDriverFactory(FakeWebDriver driver) : IDriverFactory
{
    public int CreatedDrivers { get; private set; }

    public IWebDriver CreateDriver()
    {
        CreatedDrivers++;
        return driver;
    }
}

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Text;

namespace WinTopGun.Selenium
{
    internal class SeleniumServices
    {
        public static IWebDriver CreateChromeDriver()
        {
			try
			{
				var options = new ChromeOptions();

				options.DebuggerAddress = "localhost:9222";
				options.PageLoadStrategy = PageLoadStrategy.Eager;
				options.AddArgument("--no-sandbox");
				options.AddArgument("--disable-infobars");

                return new ChromeDriver(options);
			}
			catch (Exception)
			{
				throw new InvalidOperationException("Error al iniciar ChromeDriver");
            }
        }
    }
}

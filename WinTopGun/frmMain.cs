using OpenQA.Selenium;
using System.Collections.ObjectModel;
using WinTopGun.Selenium;
using WinTopGun.UI.Forms.Opciones;

namespace WinTopGun
{
    public partial class fmrMain : Form
    {
        public fmrMain()
        {
            InitializeComponent();
        }

        private void directorioDestinoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmConfiguracion frmConfiguracion = new frmConfiguracion();
            frmConfiguracion.ShowDialog();
        }

        private void btnExtract_Click(object sender, EventArgs e)
        {
            IWebDriver driver = SeleniumServices.CreateChromeDriver();

            try
            {
                var locatorEnlaces = By.CssSelector("#div_league_summary .data_grid_box .gridtitle a");
                int totalEnlaces = driver.FindElements(locatorEnlaces).Count;
                
                for (int i = 0; i < totalEnlaces; i++)
                {
                    // Se re-localizan los elementos en cada iteración para evitar StaleElementReferenceException
                    IList<IWebElement> enlaces = driver.FindElements(locatorEnlaces);
                    IWebElement enlaceActual = enlaces[i];

                    // Hacer clic en el enlace
                    enlaceActual.Click();

                    // --- PROCESAMIENTO ---
                    Thread.Sleep(new Random().Next(1000, 7000));

                    driver.Navigate().Back();

                }
                ;

            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                if (driver != null)
                {
                    driver.Quit();
                }
            }
        }
    }
}

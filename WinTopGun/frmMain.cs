using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Collections.ObjectModel;
using System.Text;
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
            string directorioSalida = Properties.Settings.Default.RutaDatosExtraidos;

            try
            {
                // Las 5 ligas principales de Europa
                By locatorEnlaces = By.CssSelector("#div_league_summary .data_grid_box .gridtitle a");

                // Crear wait reusable
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                // Esperar que carguen los enlaces iniciales
                wait.Until(d => d.FindElements(locatorEnlaces).Count > 0);
                int totalEnlaces = driver.FindElements(locatorEnlaces).Count;

                for (int i = 0; i < totalEnlaces; i++)
                {
                    // Re-localizar los enlaces en cada iteración
                    IList<IWebElement> enlaces = driver.FindElements(locatorEnlaces);
                    IWebElement enlaceActual = enlaces[i];

                    Console.WriteLine($"\n[Liga {i + 1}/{totalEnlaces}] Abriendo enlace: {enlaceActual.Text}");

                    // Hacer clic en el enlace
                    enlaceActual.Click();

                    #region ExtraerTablas

                    try
                    {
                        // Obtener el título y la última parte de la URL
                        string tituloPagina = driver.Title;
                        Uri uri = new Uri(driver.Url);
                        string segmentoUrl = Path.GetFileName(uri.AbsolutePath.TrimEnd('/'));

                        By selectorTablasConId = By.CssSelector("table[id]");

                        // Esperar a que exista al menos una tabla con ID en la nueva página
                        wait.Until(d => d.FindElements(selectorTablasConId).Count > 0);

                        // PASO 1: Extraer todos los IDs de las tablas
                        List<string> listaIds = driver.FindElements(selectorTablasConId)
                                                      .Select(tabla => tabla.GetAttribute("id"))
                                                      .Where(id => !string.IsNullOrWhiteSpace(id))
                                                      .ToList();

                        Console.WriteLine($"-> Se encontraron {listaIds.Count} tablas con ID.");

                        // CORRECCIÓN: Usar índice 'j' para recorrer listaIds
                        for (int j = 0; j < listaIds.Count; j++)
                        {
                            string idActual = listaIds[j]; // <--- CORREGIDO (antes decía 'i')

                            By selectorTablaActual = By.Id(idActual);

                            try
                            {
                                IWebElement tablaActual = driver.FindElement(selectorTablaActual);

                                string innerTextTabla = tablaActual.GetDomProperty("innerText")
                                                     ?? tablaActual.GetAttribute("innerText")
                                                     ?? string.Empty;

                                // Nombre y guardado del archivo
                                string nombreArchivo = $"{segmentoUrl}_tabla_{idActual}.txt";
                                string rutaArchivoCompleta = Path.Combine(Properties.Settings.Default.RutaDatosExtraidos, nombreArchivo);

                                File.WriteAllText(rutaArchivoCompleta, innerTextTabla, Encoding.UTF8);

                                // CORRECCIÓN: Mostrar 'j + 1' en lugar de 'i + 1'
                                Console.WriteLine($"   [{j + 1}/{listaIds.Count}] Procesado ID '{idActual}' -> {nombreArchivo}");
                            }
                            catch (NoSuchElementException)
                            {
                                Console.WriteLine($"   [x] La tabla con ID '{idActual}' ya no existe en el DOM.");
                            }
                        }
                    }
                    catch (WebDriverTimeoutException)
                    {
                        Console.WriteLine($"[!] Tiempo de espera agotado: La página no cargó tablas con 'table[id]'.");
                    }

                    #endregion

                    // Volver a la página principal
                    driver.Navigate().Back();

                    // ESPERA CRÍTICA: Asegurar que la página principal volvió a cargar antes del siguiente ciclo
                    wait.Until(d => d.FindElements(locatorEnlaces).Count > 0);
                }
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

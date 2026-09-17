# Plan de Trabajo — Optimización de WinTopGun

> **ESTADO: ✅ EJECUTADO** (rama `Refactorización`). Fases 0–4 completadas: arquitectura en capas, DI, UI asíncrona con cancelación, robustez de Selenium y 20 pruebas unitarias en verde (`dotnet test`). La Fase 5 queda como backlog opcional.

> Proyecto: WinTopGun (WinForms, .NET 10, Selenium WebDriver)
> Objetivo: evolucionar el prototipo actual hacia una aplicación mantenible, testeable y robusta aplicando principios de ingeniería de software (SOLID, separación de responsabilidades, DRY, KISS).

---

## 1. Diagnóstico del estado actual

### 1.1 Estructura actual

```
WinTopGun/
├── Program.cs                      → Entry point
├── frmMain.cs / .Designer.cs       → Formulario principal (contiene lógica de scraping)
├── App.config                      → Settings (RutaDatosExtraidos)
├── Models/                         → (vacía)
├── Utilities/                      → (vacía)
├── Services/
│   ├── Domain/                     → (vacía)
│   ├── Models/                     → (vacía)
│   ├── Services/                   → (vacía)
│   ├── UI/                         → (vacía)
│   └── Utilities/                  → (vacía)   ← estructura anidada accidental
├── Selenium/
│   └── SeleniumServices.cs         → Factory estática de ChromeDriver
└── UI/
    ├── Forms/Opciones/frmConfiguracion.cs
    └── UserControls/uCtlFichaOpciones.cs
```

### 1.2 Problemas detectados

| # | Problema | Ubicación | Severidad |
|---|----------|-----------|-----------|
| 1 | **Lógica de negocio en la UI**: scraping, navegación, extracción de tablas y escritura a disco están dentro de `btnExtract_Click` (~120 líneas) | `frmMain.cs` | 🔴 Alta |
| 2 | **Operación bloqueante en el hilo de UI**: Selenium congela la ventana durante toda la extracción | `frmMain.cs` | 🔴 Alta |
| 3 | **`catch (Exception) { throw; }`** inútil que pierde contexto | `frmMain.cs` | 🟡 Media |
| 4 | Sin cancelación: no hay forma de abortar un scraping largo | `frmMain.cs` | 🟡 Media |
| 5 | Sin logging ni reporte de progreso: la salida va a `Console.WriteLine` (invisible en WinForms) | `frmMain.cs` | 🟡 Media |
| 6 | Typo en el nombre de la clase: `fmrMain` debería ser `frmMain` | `frmMain.cs/.Designer.cs` | 🟡 Media |
| 7 | **Directorios vacíos y estructura anidada residual** (`Services/Domain`, `Services/Models`, `Services/Services`, `Services/UI`, `Services/Utilities`; `Models/`; `Utilities/`) | Raíz | 🟢 Baja |
| 8 | `SeleniumServices` es una clase estática acoplada (difícil de mockear/testear), sin interfaz | `Selenium/SeleniumServices.cs` | 🟡 Media |
| 9 | Nombre de archivo hardcodeado y sin validación: si `RutaDatosExtraidos` está vacía, `File.WriteAllText` falla en runtime | `frmMain.cs` | 🟡 Media |
| 10 | `FolderBrowserDialog` no se libera (falta `using`) | `uCtlFichaOpciones.cs` | 🟢 Baja |
| 11 | Convenciones de nomenclatura inconsistentes (`frm`/`uCtl` húngaro vs. resto; recursos con acentos: `icons8-prismáticos-50.png`) | Varios | 🟢 Baja |
| 12 | Sin tests, sin analizadores de código, sin `.editorconfig`, sin `.gitignore` | Proyecto | 🟡 Media |
| 13 | Sin manejo de elementos obsoletos (*stale elements*) ni reintentos ante fallos de red | `frmMain.cs` | 🟡 Media |

---

## 2. Fases del plan

### Fase 0 — Higiene del repositorio y estructura ✅ *rápida, bajo riesgo*

- [ ] **0.1** Eliminar directorios vacíos: `Models/`, `Utilities/` y el anidado accidental dentro de `Services/` (`Domain`, `Models`, `Services`, `UI`, `Utilities`).
- [ ] **0.2** Añadir `.gitignore` (plantilla de Visual Studio: excluir `bin/`, `obj/`, `.vs/`).
- [ ] **0.3** Añadir `.editorconfig` con convenciones del equipo (indentación, `var`, orden de usings, naming en PascalCase/camelCase, prefijo `I` en interfaces).
- [ ] **0.4** Renombrar `fmrMain` → `frmMain` (clase, archivos y todos los usos) para corregir el typo antes de que se propague.
- [ ] **0.5** Renombrar el recurso `icons8-prismáticos-50.png` → `icons8-prismaticos-50.png` (evitar acentos en nombres de recursos/rutas).
- [ ] **0.6** Activar analizadores en el `.csproj`:
  ```xml
  <PropertyGroup>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
  </PropertyGroup>
  ```

**Criterio de salida:** el proyecto compila, no hay carpetas vacías, advertencias de analizador revisadas.

---

### Fase 1 — Arquitectura en capas (separación de responsabilidades) ⭐ *núcleo del plan*

Separar el código en tres capas con dependencias en una sola dirección: **UI → Servicios → Infraestructura**.

#### Estructura propuesta

```
WinTopGun/
├── Program.cs
├── UI/
│   ├── Forms/
│   │   ├── frmMain.cs                  → solo orquesta eventos y muestra progreso
│   │   └── Opciones/frmConfiguracion.cs
│   └── UserControls/uCtlFichaOpciones.cs
├── Application/                        → casos de uso (orquestación)
│   ├── Interfaces/
│   │   ├── IScrapingService.cs
│   │   ├── ITableExporter.cs
│   │   └── IDriverFactory.cs
│   └── Services/
│       └── LeagueScrapingService.cs    → el algoritmo que hoy vive en btnExtract_Click
├── Domain/                             → modelos puros, sin dependencias de Selenium
│   └── Models/
│       ├── LeagueInfo.cs               → nombre, url, etc.
│       └── ExtractedTable.cs           → Id, título, contenido, ruta de salida
├── Infrastructure/
│   ├── Selenium/
│   │   ├── ChromeDriverFactory.cs      → reemplaza a SeleniumServices (implementa IDriverFactory)
│   │   └── Selectors.cs                → constantes de localizadores (By) centralizados
│   ├── Persistence/
│   │   └── TextFileTableExporter.cs    → implementa ITableExporter (File.WriteAllText)
│   └── Configuration/
│       └── AppSettingsProvider.cs      → envuelve Properties.Settings (evita acoplamiento directo)
└── Tests/
    └── WinTopGun.Tests/                → proyecto xUnit (Fase 4)
```

#### Principios aplicados

- **SRP**: `frmMain` solo maneja UI; `LeagueScrapingService` solo scrapea; `TextFileTableExporter` solo persiste.
- **DIP**: la capa de aplicación depende de `IDriverFactory` e `ITableExporter`, no de clases concretas de Selenium ni de `File`.
- **OCP**: agregar exportación a CSV/JSON = nueva implementación de `ITableExporter`, sin tocar el scraper.

#### Sketch de la pieza clave

```csharp
// Application/Interfaces/IScrapingService.cs
public interface IScrapingService
{
    Task<ScrapingResult> ExtractLeagueTablesAsync(
        string outputDirectory,
        IProgress<ScrapingProgress> progress,
        CancellationToken cancellationToken);
}
```

```csharp
// Application/Services/LeagueScrapingService.cs (resumen)
public class LeagueScrapingService : IScrapingService
{
    private readonly IDriverFactory _driverFactory;
    private readonly ITableExporter _exporter;

    public async Task<ScrapingResult> ExtractLeagueTablesAsync(...) { /* lógica actual de btnExtract_Click */ }
}
```

- [ ] **1.1** Crear proyectos de carpetas y modelos de dominio (`LeagueInfo`, `ExtractedTable`, `ScrapingResult`, `ScrapingProgress`).
- [ ] **1.2** Definir interfaces: `IScrapingService`, `ITableExporter`, `IDriverFactory`, `IAppSettings`.
- [ ] **1.3** Migrar el algoritmo de `btnExtract_Click` a `LeagueScrapingService` (extraer método por método, sin refactorizar aún la lógica).
- [ ] **1.4** Implementar `ChromeDriverFactory`, `TextFileTableExporter` y `AppSettingsProvider`.
- [ ] **1.5** Inyección de dependencias: usar `Microsoft.Extensions.DependencyInjection` en `Program.cs` (composition root) y pasar los servicios a los forms (o usar un `IServiceProvider` accesible).
- [ ] **1.6** `btnExtract_Click` queda reducido a: validar entrada → llamar al servicio con `await` → mostrar resultado.

**Criterio de salida:** `frmMain.cs` no contiene `OpenQA.Selenium` ni `File.`/`Path.`; compila y funciona igual que antes.

---

### Fase 2 — UI responsiva: async, progreso y cancelación

- [ ] **2.1** Convertir la extracción a `async/await`: envolver las llamadas bloqueantes de Selenium con `Task.Run` (Selenium no es async nativo) para no congelar la UI.
- [ ] **2.2** Reportar progreso con `IProgress<T>`: barra de progreso/label "Liga 3/5 — Tabla 12/30" en `frmMain` (sustituye los `Console.WriteLine`).
- [ ] **2.3** Soportar `CancellationToken`: botón "Cancelar" que aborta el scraping de forma limpia; usar `CancellationToken.ThrowIfCancellationRequested()` en cada iteración.
- [ ] **2.4** Deshabilitar `btnExtract` durante la ejecución y mostrar estado de ocupado (`UseWaitCursor` / spinner).
- [ ] **2.5** Reemplazar `catch (Exception) { throw; }` por manejo real: capturar excepciones específicas (`WebDriverException`, `IOException`, `OperationCanceledException`) y mostrar un `MessageBox` con mensaje accionable + logging del detalle.
- [ ] **2.6** `uCtlFichaOpciones.cs`: envolver `FolderBrowserDialog` en `using`; validar que el directorio exista antes de guardar.
- [ ] **2.7** Validar al inicio de la extracción que `RutaDatosExtraidos` no esté vacía; si lo está, ofrecer abrir la configuración en lugar de fallar.

**Criterio de salida:** la ventana responde durante el scraping, se puede cancelar, y el usuario ve el progreso.

---

### Fase 3 — Robustez de Selenium e infraestructura

- [ ] **3.1** Centralizar selectores en una clase estática `Selectors` (hoy `By.CssSelector(...)` está inline): un solo punto de mantenimiento si cambia el DOM del sitio.
- [ ] **3.2** Configuración de timeouts en un solo lugar (constantes o settings: `WaitTimeoutSeconds`, `PageLoadTimeout`).
- [ ] **3.3** Manejo de *stale elements*: re-localizar elementos como ya se hace, pero encapsularlo en un helper (p. ej. `RetryOnStaleElement`).
- [ ] **3.4** Reintentos configurables ante fallos transitorios (p. ej. 3 intentos con backoff) para navegación y descargas.
- [ ] **3.5** `ChromeDriverFactory`: quitar el `try/catch` que relanza como `InvalidOperationException` sin InnerException (pierde el detalle); incluir la excepción original.
- [ ] **3.6** Sanitizar `idActual` y `segmentoUrl` antes de construir nombres de archivo (caracteres inválidos en Windows: `Path.GetInvalidFileNameChars`).
- [ ] **3.7** Garantizar liberación del driver con `await using` / `IAsyncDisposable` o `try/finally` en el servicio (no en la UI).
- [ ] **3.8** Considerar `WebDriverManager`/Selenium Manager para versiones de driver (Selenium 4.49 ya integra Selenium Manager — verificar que no haya dependencia manual).

**Criterio de salida:** fallos transitorios no abortan todo el proceso; los archivos generados siempre tienen nombres válidos.

---

### Fase 4 — Calidad: pruebas, logging y configuración

- [ ] **4.1** Crear proyecto de tests `WinTopGun.Tests` (xUnit) referenciando el principal.
- [ ] **4.2** Tests unitarios con mocks de `IDriverFactory` y `ITableExporter` para `LeagueScrapingService`:
  - genera el nombre de archivo esperado;
  - exporta todas las tablas encontradas;
  - continúa cuando una tabla desaparece del DOM (*stale*);
  - respeta la cancelación;
  - sanitiza nombres de archivo inválidos.
- [ ] **4.3** Tests de `TextFileTableExporter` con directorio temporal.
- [ ] **4.4** Integrar `Microsoft.Extensions.Logging` (ILogger) — consola y archivo (Serilog opcional). Sustituir todos los `Console.WriteLine`.
- [ ] **4.5** Mover valores configurables (timeout, URL base, flags de Chrome) a `App.config`/Settings o `appsettings.json` con una clase de opciones tipada (`ScrapingOptions`).
- [ ] **4.6** Documentar las APIs públicas con comentarios XML (`///`).

**Criterio de salida:** `dotnet test` en verde; sin `Console.WriteLine` en producción.

---

### Fase 5 — Mejoras opcionales (backlog)

- [ ] **5.1** Exportación alternativa a CSV/JSON (nueva implementación de `ITableExporter` + selector en UI).
- [ ] **5.2** Parsear las tablas a modelos estructurados (filas/columnas) en lugar de `innerText` plano.
- [ ] **5.3** Historial de ejecuciones (SQLite o JSON local).
- [ ] **5.4** Publicación: `dotnet publish` con perfil single-file self-contained para distribuir sin runtime.
- [ ] **5.5** Evaluar migración de nombres `frm*`/`uCtl*` a PascalCase estándar (`MainForm`, `OptionsSheetControl`) — decisión de equipo, requiere tocar los `.Designer.cs`.
- [ ] **5.6** CI (GitHub Actions): build + tests en cada push.

---

## 3. Orden de ejecución y esfuerzo estimado

| Fase | Alcance | Esfuerzo estimado | Riesgo |
|------|---------|-------------------|--------|
| 0 — Higiene | Limpieza, naming, editorconfig | 1–2 h | Bajo |
| 1 — Arquitectura | Capas, interfaces, DI | 4–6 h | Medio |
| 2 — UI async | Async, progreso, cancelación | 3–4 h | Medio |
| 3 — Robustez | Selectores, reintentos, sanitización | 2–3 h | Bajo |
| 4 — Calidad | Tests, logging, configuración | 4–6 h | Bajo |
| 5 — Backlog | Opcional | Según prioridad | — |

**Recomendación:** ejecutar las fases 0 → 1 → 2 en ese orden; la fase 1 es la que aporta mayor valor y desbloquea todo lo demás (sin capas no hay tests ni mocks posibles).

## 4. Métricas de éxito

- `frmMain.cs` < 50 líneas y sin referencias a Selenium/IO.
- Cobertura de tests > 70 % en `Application/`.
- Cero advertencias del analizador en `latest-recommended`.
- La UI permanece responsiva durante la extracción y el proceso es cancelable.
- Un cambio de selector o formato de exportación toca exactamente un archivo.

# Documentación del Proyecto — WinTopGun

> **Versión del documento:** 1.0 (rama `documentación`)
> **Aplicación:** WinTopGun — scraper de escritorio de ligas de fútbol (tablas de posiciones/estadísticas)
> **Stack:** C# / .NET 10 (Windows Forms) + Selenium WebDriver 4.49
> **Documento complementario:** [`PLAN_DE_TRABAJO.md`](../PLAN_DE_TRABAJO.md) (plan de optimización ya ejecutado)

---

## 1. ¿Qué hace WinTopGun?

WinTopGun es una aplicación de escritorio Windows que:

1. Abre una página de estadísticas deportivas mediante **Selenium WebDriver** (Chrome).
2. Detecta los enlaces de las **5 ligas principales** de Europa en la página principal.
3. Entra a cada liga y extrae **todas las tablas HTML que poseen atributo `id`**.
4. Guarda cada tabla como **archivo de texto plano UTF-8** en un directorio configurado por el usuario.

Su arquitectura fue refactorizada desde un prototipo monolítico hacia un diseño **en capas con inyección de dependencias**, siguiendo los principios SOLID. El detalle del proceso está en el plan de trabajo.

---

## 2. Mapa general de directorios

```
WinTopGun/                      ← raíz del proyecto (repo: WinTopGun/)
│
├── Domain/                     → Capa de dominio: modelos puros del negocio
│   └── Models/
├── Application/                → Capa de aplicación: casos de uso e interfaces
│   ├── Interfaces/
│   └── Services/
├── Infrastructure/             → Capa de infraestructura: implementaciones concretas
│   ├── Selenium/
│   ├── Persistence/
│   ├── Configuration/
│   └── Logging/
├── UI/                         → Capa de presentación (Windows Forms)
│   ├── Forms/
│   └── UserControls/
├── Tests/
│   └── WinTopGun.Tests/        → Proyecto de pruebas unitarias (xUnit)
├── Properties/                 → Generados por Visual Studio (settings y recursos)
├── Resources/                  → Imágenes y recursos incrustados
└── Docs/                       → Esta documentación
```

**Regla de dependencias** (una sola dirección, nunca al revés):

```
UI  →  Application  →  Domain
              ↓
        Infrastructure  →  Domain
```

- `Domain` no depende de nada del proyecto (ni siquiera de Selenium).
- `Application` define **interfaces**; no conoce implementaciones.
- `Infrastructure` implementa esas interfaces con tecnología concreta.
- `UI` solo orquesta eventos de usuario y muestra resultados.

---

## 3. Documentación detallada por directorio

### 📁 `Domain/Models/` — Modelos del dominio

| Aspecto | Detalle |
|---|---|
| **¿Qué es?** | Registros (records) inmutables que representan los conceptos del negocio y los datos que fluyen entre capas. |
| **¿Por qué se creó?** | Para que las capas se comuniquen mediante tipos con significado de negocio, en lugar de cadenas, listas sueltas o clases de Selenium. También habilita las pruebas unitarias sin navegador. |
| **¿Para qué se usa?** | Como parámetros y valores de retorno de los servicios de aplicación y de la UI. |

**Archivos:**

| Archivo | Contenido |
|---|---|
| `LeagueInfo.cs` | Una liga detectada en la página principal (`Index`, `Name`, `Url`). |
| `ExtractedTable.cs` | Una tabla HTML extraída (`TableId`, `Content`). |
| `ScrapingProgress.cs` | Estado de avance de la extracción. Reportado vía `IProgress<T>` para actualizar la barra de progreso y el texto de estado de `frmMain` (fábricas `ForLeague` y `ForTable`). |
| `ScrapingResult.cs` | Resultado consolidado de una ejecución: ligas procesadas, tablas exportadas, lista de errores (incidencias no fatales) y bandera `WasCancelled`. Su propiedad `Success` resume el desenlace. |

---

### 📁 `Application/Interfaces/` — Contratos de la capa de aplicación

| Aspecto | Detalle |
|---|---|
| **¿Qué es?** | Interfaces (contratos) que definen **qué** hace la aplicación, sin decir **cómo**. |
| **¿Por qué se creó?** | Para aplicar el principio de **Inversión de Dependencias (DIP)**: la lógica de negocio depende de abstracciones, y las tecnologías concretas (Selenium, disco, `Properties.Settings`) se inyectan desde fuera. Esto permite reemplazar implementaciones y crear fakes en las pruebas. |
| **¿Para qué se usa?** | La UI consume `IScrapingService` e `ISettingsProvider`; `LeagueScrapingService` consume `IDriverFactory` e `ITableExporter`; `Composition.cs` las enlaza con sus implementaciones. |

**Archivos:**

| Archivo | Contrato | Implementación real |
|---|---|---|
| `IScrapingService.cs` | Orquesta el proceso completo de extracción. Recibe directorio destino, `IProgress<ScrapingProgress>` opcional y `CancellationToken`. | `Infrastructure/.../LeagueScrapingService` (ver nota) |
| `ITableExporter.cs` | Persiste el contenido de una tabla (`ExportTableAsync`). | `TextFileTableExporter` |
| `IDriverFactory.cs` | Fábrica de `IWebDriver`. | `ChromeDriverFactory` |
| `ISettingsProvider.cs` | Lectura/escritura de la configuración persistente (`OutputDirectory`). | `AppSettingsProvider` |

> **Nota:** `LeagueScrapingService` vive en `Application/Services/` (es un caso de uso) e implementa `IScrapingService` directamente.

---

### 📁 `Application/Services/` — Casos de uso y lógica de aplicación

| Aspecto | Detalle |
|---|---|
| **¿Qué es?** | El corazón de la aplicación: el algoritmo de negocio y los servicios de apoyo, libres de UI y de detalles de infraestructura. |
| **¿Por qué se creó?** | En el prototipo, todo el algoritmo de scraping (~120 líneas) vivía dentro del evento `btnExtract_Click` del formulario principal. Se migró aquí para cumplir el **principio de responsabilidad única (SRP)**, hacer el código testeable y desacoplarlo de WinForms. |
| **¿Para qué se usa?** | `frmMain` invoca `IScrapingService.ExtractLeagueTablesAsync(...)` y muestra el resultado; ya no contiene nada de Selenium ni de E/S de disco. |

**Archivos:**

| Archivo | Contenido |
|---|---|
| `LeagueScrapingService.cs` | **El algoritmo completo de extracción**: crea el driver, espera los enlaces de ligas, itera sobre ellas (re-localizando elementos en cada ciclo para evitar *stale elements*), extrae el `id` de cada tabla, lee su `innerText`, delega la exportación y reporta progreso. Captura `WebDriverTimeoutException` (liga sin tablas → se registra y continúa), `NoSuchElementException`/`StaleElementReferenceException` (tabla que desapareció del DOM → incidencia no fatal) y `OperationCanceledException` (marca el resultado como cancelado). Garantiza `driver.Quit()` en `finally`. Ejecuta el trabajo bloqueante mediante `Task.Run` para no congelar la UI. |
| `ScrapingOptions.cs` | Opciones configurables del scraping: `WaitTimeoutSeconds` y `ChromeDebuggerAddress`. Se cargan desde `App.config` (sección `appSettings`) en `Composition.cs`. |
| `FileNameSanitizer.cs` | Sanitiza fragmentos de texto para usarlos como nombres de archivo en Windows (reemplaza caracteres inválidos como `? : * " < > | / \` por `_`). Evita que un `id` de tabla malicioso o extraño rompa el guardado. |

---

### 📁 `Infrastructure/Selenium/` — Integración con Selenium WebDriver

| Aspecto | Detalle |
|---|---|
| **¿Qué es?** | Todo lo que toca directamente la API de OpenQA.Selenium. |
| **¿Por qué se creó?** | Para aislar la tecnología de automatización de navegador en un solo lugar. Si Selenium cambia de API, o se quiere soportar Firefox, solo se modifica esta carpeta (principio **OCP**: abierto a extensión, cerrado a modificación). |
| **¿Para qué se usa?** | `LeagueScrapingService` consume `IDriverFactory` y los selectores a través de las abstracciones definidas en `Application`. |

**Archivos:**

| Archivo | Contenido |
|---|---|
| `ChromeDriverFactory.cs` | Implementa `IDriverFactory`. Construye `ChromeDriver` con: conexión a Chrome por depuración remota (`DebuggerAddress`, configurable), `PageLoadStrategy.Eager`, `--no-sandbox` y `--disable-infobars`. A diferencia del prototipo, **no traga excepciones**: si ChromeDriver falla al arrancar, el error original (con su causa raíz) llega hasta la UI. |
| `Selectors.cs` | Localizadores centralizados del sitio objetivo: `LeagueLinks` (`#div_league_summary .data_grid_box .gridtitle a`) y `TablesWithId` (`table[id]`), además del método `TableById(id)`. Si el sitio cambia su HTML, este es el **único archivo** a actualizar. |

---

### 📁 `Infrastructure/Persistence/` — Persistencia de datos extraídos

| Aspecto | Detalle |
|---|---|
| **¿Qué es?** | Implementaciones de exportación/guardado de los datos obtenidos. |
| **¿Por qué se creó?** | Para separar el "cómo se guarda" del "qué se extrae". El formato actual es texto plano, pero la interfaz `ITableExporter` permite agregar CSV, JSON o base de datos sin tocar el scraper. |
| **¿Para qué se usa?** | Cada tabla extraída se escribe como `<segmentoPagina>_tabla_<id>.txt` en el directorio configurado por el usuario. |

**Archivos:**

| Archivo | Contenido |
|---|---|
| `TextFileTableExporter.cs` | Implementa `ITableExporter`. Sanitiza los nombres con `FileNameSanitizer`, crea el directorio destino si no existe y escribe el contenido en UTF-8 de forma asíncrona (`File.WriteAllTextAsync`). Devuelve la ruta completa del archivo generado. |

---

### 📁 `Infrastructure/Configuration/` — Configuración persistente

| Aspecto | Detalle |
|---|---|
| **¿Qué es?** | Adaptador entre la aplicación y el mecanismo de configuración de .NET (`Properties.Settings` / `App.config`). |
| **¿Por qué se creó?** | Para que la capa de aplicación no dependa directamente de `Properties.Settings.Default` (un detalle de implementación de WinForms). Si mañana se migra a `appsettings.json`, solo cambia este adaptador. |
| **¿Para qué se usa?** | La pantalla de configuración (directorio destino) lee y escribe a través de `ISettingsProvider`. |

**Archivos:**

| Archivo | Contenido |
|---|---|
| `AppSettingsProvider.cs` | Implementa `ISettingsProvider` sobre `Properties.Settings`. Expone `OutputDirectory` (mapea a `RutaDatosExtraidos`) y `Save()` para persistir los cambios en el perfil del usuario. |

---

### 📁 `Infrastructure/Logging/` — Registro de eventos

| Aspecto | Detalle |
|---|---|
| **¿Qué es?** | Proveedor de logging que escribe a archivo, integrado con `Microsoft.Extensions.Logging`. |
| **¿Por qué se creó?** | El prototipo usaba `Console.WriteLine`, invisible en una aplicación WinForms. Se necesita visibilidad de qué ligas/tablas se procesan y de los errores, para diagnóstico post-mortem. |
| **¿Para qué se usa?** | `Composition.cs` registra el proveedor en el pipeline de logging; los servicios inyectan `ILogger<T>` y registran el inicio/fin de la extracción, cada liga y tabla procesada, y las incidencias. |

**Archivos:**

| Archivo | Contenido |
|---|---|
| `FileLoggerProvider.cs` | `ILoggerProvider` propio (sin dependencias extra) que escribe líneas con timestamp, nivel y categoría en `%LOCALAPPDATA%\WinTopGun\logs\WinTopGun.log`. Thread-safe (escritura bajo lock), con umbral mínimo `Information`. |

---

### 📁 `UI/Forms/` — Formularios (ventanas) de la aplicación

| Aspecto | Detalle |
|---|---|
| **¿Qué es?** | Las ventanas WinForms. Cada formulario tiene dos archivos: `*.cs` (lógica) y `*.Designer.cs` (código generado por el diseñador visual de Visual Studio — no editar a mano salvo sea necesario). |
| **¿Por qué se creó?** | Es la capa de presentación. Tras la refactorización, su única responsabilidad es **orquestar la interacción del usuario** (principio SRP aplicado a la UI). |
| **¿Para qué se usa?** | Capturar eventos, validar entradas mínimas, mostrar progreso y resultados, y delegar todo lo demás a los servicios de aplicación. |

**Formularios:**

| Formulario | Responsabilidad |
|---|---|
| `frmMain.cs` (en la raíz del proyecto) | Ventana principal. Contiene el botón **Extraer**, el botón **Cancelar**, la barra de progreso y el menú (Archivo / Opciones → Directorio destino). En `btnExtract_Click`: valida que exista directorio destino (ofrece abrir la configuración si no), deshabilita controles, lanza la extracción con `await`, muestra progreso vía `IProgress<ScrapingProgress>` y presenta el `ScrapingResult` (éxito, cancelación o advertencias) en `MessageBox`. No conoce Selenium ni el sistema de archivos. |
| `Opciones/frmConfiguracion.cs` | Ventana de configuración. Recibe `ISettingsProvider` por constructor (inyección de dependencias) y aloja las fichas de opciones dentro de un `FlowLayoutPanel`. |

---

### 📁 `UI/UserControls/` — Controles reutilizables

| Aspecto | Detalle |
|---|---|
| **¿Qué es?** | Controles compuestos reutilizables (UserControls) que se incrustan en formularios. |
| **¿Por qué se creó?** | Para modularizar la pantalla de opciones en "fichas" independientes que puedan crecer (más opciones de configuración) sin agrandar el formulario contenedor. |
| **¿Para qué se usa?** | `frmConfiguracion` agrega dinámicamente una `uCtlFichaOpciones` en su `Load`. |

**Controles:**

| Control | Responsabilidad |
|---|---|
| `uCtlFichaOpciones.cs` | Ficha "Directorio destino": imagen clicable + título + subtítulo. Al hacer clic abre un `FolderBrowserDialog` (liberado con `using`), guarda la ruta seleccionada mediante `ISettingsProvider` y confirma al usuario. Recibe el proveedor de configuración por constructor. |

---

### 📁 `Tests/WinTopGun.Tests/` — Pruebas unitarias

| Aspecto | Detalle |
|---|---|
| **¿Qué es?** | Proyecto de pruebas xUnit independiente (referenciado en `WinTopGun.slnx`). |
| **¿Por qué se creó?** | La refactorización extrajo la lógica a capas precisamente para hacerla testeable. Estas pruebas protegen el comportamiento del scraper contra regresiones **sin abrir un navegador real**. |
| **¿Para qué se usa?** | Ejecutar con `dotnet test`. Cobertura actual: 20 pruebas en verde. |

**Estructura interna:**

| Carpeta/Archivo | Contenido |
|---|---|
| `Fakes/` | Dobles de prueba escritos a mano (sin librerías de mocking): `FakeWebDriver` e `FakeWebElement` (implementaciones mínimas de las interfaces de Selenium que simulan ligas y tablas; notable: en Selenium 4.49 `By.Id` se materializa como selector CSS `#id`), `FakeDriverFactory` y `RecordingTableExporter` (exportador en memoria), `RecordingProgress` (captura los reportes de avance). |
| `LeagueScrapingServiceTests.cs` | Pruebas del algoritmo: happy path (4 tablas/2 ligas), tabla que desaparece del DOM (continúa y reporta), timeout sin tablas (error por liga, `Quit` garantizado), token cancelado antes de iniciar, directorio vacío (ArgumentException), reportes de progreso y liberación del driver ante excepciones. |
| `TextFileTableExporterTests.cs` | Pruebas de escritura real en directorio temporal: nombre de archivo esperado, creación de directorios anidados, sanitización de ids y contenido vacío. |
| `FileNameSanitizerTests.cs` | Teorías de sanitización de caracteres inválidos de Windows. |
| `ScrapingResultTests.cs` | Semántica de la propiedad `Success`. |

---

### 📁 `Properties/` — Metadatos y código generado (Visual Studio)

| Aspecto | Detalle |
|---|---|
| **¿Qué es?** | Carpeta convencional de Visual Studio con código **generado automáticamente**. No editar los `*.Designer.cs` a mano. |
| **¿Por qué existe?** | Estándar del ecosistema WinForms para configuración de usuario y recursos incrustados. |
| **¿Para qué se usa?** | Ver tabla. |

| Archivo | Contenido |
|---|---|
| `Settings.settings` / `Settings.Designer.cs` | Configuración de usuario. Define `RutaDatosExtraidos` (String, ámbito User): el directorio donde se guardan las tablas extraídas. Se persiste por usuario en `%LOCALAPPDATA%`. Consumida por `AppSettingsProvider`. |
| `Resources.resx` / `Resources.Designer.cs` | Recursos incrustados: los íconos PNG de los botones/controles. Accesibles vía `Properties.Resources.<nombre>`. |

---

### 📁 `Resources/` — Recursos gráficos

| Aspecto | Detalle |
|---|---|
| **¿Qué es?** | Archivos de imagen (PNG) usados por la interfaz. |
| **¿Por qué existe?** | Mantener los activos visuales separados del código y referenciados desde el `.resx`. |
| **¿Para qué se usa?** | `icons8-prismaticos-50.png` → imagen del botón "Extraer" de `frmMain`; `icons8-abrir-carpeta-50.png` → imagen clicable de `uCtlFichaOpciones`. |

> **Convención:** los nombres de recursos no llevan acentos ni caracteres especiales (por eso se renombró `icons8-prismáticos-50.png` → `icons8-prismaticos-50.png` durante la Fase 0).

---

### 📁 `Docs/` — Documentación del proyecto

| Aspecto | Detalle |
|---|---|
| **¿Qué es?** | Carpeta creada para centralizar los documentos de diseño y arquitectura en Markdown. |
| **¿Por qué se creó?** | Mantener la documentación **junto al código** (docs-as-code) permite versionarla, revisarla y actualizarla en los mismos pull requests que los cambios. |
| **¿Para qué se usa?** | Contiene este documento; es el lugar natural para futuros documentos (guía de contribución, decisiones de arquitectura/ADRs, manual de usuario). |

---

### 📄 Archivos clave en la raíz del proyecto

| Archivo | ¿Qué es? / ¿Para qué se usa? |
|---|---|
| `Program.cs` | **Entry point**. Inicializa la configuración de la aplicación (DPI, fuente) y ejecuta `frmMain`. |
| `Composition.cs` | **Composition Root**: único lugar donde se registran las dependencias (`ServiceCollection`). Carga `ScrapingOptions` desde `App.config`, registra las implementaciones de cada interfaz y configura el logging con `FileLoggerProvider`. Si cambia el wiring de la aplicación, cambia solo este archivo. |
| `frmMain.cs` / `frmMain.Designer.cs` / `frmMain.resx` | Formulario principal (ver `UI/Forms/`). El `.resx` contiene los recursos del formulario. |
| `App.config` | Configuración de la aplicación: `appSettings` del scraping (`Scraping:WaitTimeoutSeconds`, `Scraping:ChromeDebuggerAddress`) y `userSettings` (`RutaDatosExtraidos`). ⚠️ `configSections` debe ser siempre el primer elemento hijo de `<configuration>` (error corregido en el merge: el orden inverso provocaba `TypeInitializationException` en `Composition`). |
| `WinTopGun.csproj` | Proyecto SDK-style: `net10.0-windows`, WinForms, nullable habilitado, analizadores `latest-recommended`. Paquetes: `Selenium.WebDriver`, `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.Logging`. Excluye `Tests/**` de la compilación y declara `InternalsVisibleTo` para el proyecto de tests. |
| `PLAN_DE_TRABAJO.md` | Plan de optimización ejecutado (fases 0–4 completadas, fase 5 como backlog). |

---

## 4. Flujo de ejecución de una extracción (de punta a punta)

```
Usuario hace clic en "Extraer" (frmMain)
        │
        ▼
frmMain valida el directorio destino (ISettingsProvider)
        │
        ▼
IScrapingService.ExtractLeagueTablesAsync(...)      ← LeagueScrapingService
        │
        ▼
Task.Run (hilo de trabajo, la UI queda libre)
        │
        ├── IDriverFactory.CreateDriver()            ← ChromeDriverFactory
        ├── WebDriverWait: enlaces de ligas          ← Selectors.LeagueLinks
        ├── por cada liga:
        │     ├── Click en el enlace
        │     ├── Selectors.TablesWithId → ids de tablas
        │     ├── por cada tabla:
        │     │     ├── FindElement(TableById)       (stale → incidencia)
        │     │     ├── GetDomProperty("innerText")
        │     │     ├── ITableExporter.ExportTableAsync   ← TextFileTableExporter
        │     │     │     └── FileNameSanitizer + File.WriteAllTextAsync
        │     │     └── progress.Report(...)         ← barra de progreso en frmMain
        │     └── Navigate().Back() + espera
        └── finally: driver.Quit()
        │
        ▼
ScrapingResult → frmMain muestra éxito / cancelación / advertencias
```

---

## 5. Convenciones del proyecto

- **Lenguaje:** C# 13 con `file-scoped namespaces`, expresiones `record` para modelos inmutables y colecciones por expresión (`[.. ]`).
- **Naming:** PascalCase para tipos/métodos/propiedades; interfaces con prefijo `I`; formularios con prefijo histórico `frm` y controles `uCtl` (decisión de equipo pendiente de migrar a PascalCase puro — Fase 5 del plan).
- **Estilo:** reglas en `.editorconfig` (raíz del repositorio); analizadores `latest-recommended` activados en build.
- **Commits:** mensajes descriptivos por fase/tema, en español.
- **Pruebas:** convención de nombre `Método_Escenario_Resultado` (CA1707 suprimido solo en el proyecto de tests).
- **Backlog (Fase 5 del plan):** exportación CSV/JSON, historial de ejecuciones, CI (build + tests en cada push), publicación single-file y migración de nomenclatura de la UI.

---

*Documento creado en la rama `documentación`. Mantener actualizado al agregar o renombrar directorios.*

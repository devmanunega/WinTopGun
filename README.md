# WinTopGun

Aplicación de escritorio (WinForms, .NET 10 + Selenium WebDriver) para extraer estadísticas de ligas deportivas y exportarlas a archivos de texto plano.

## Procesos de extracción

| Proceso | Descripción | Salida |
|---|---|---|
| **Equipos** | Recorre las ligas y extrae sus tablas con `id` | `<OutputDirectory>/<pagina>_tabla_<id>.txt` |
| **Jugadores** | Recorre las ligas, abre los enlaces pares de jugadores y extrae sus tablas | `<OutputDirectory>/<pagina>_tabla_<id>.txt` |
| **Partidos** | Recorre las ligas → equipos de la tabla de clasificación → reportes "Match Report" disputados; extrae tablas con `id`, `scorebox_meta` y `events_wrap` | `<OutputDirectory>/Detalles_Partidos/…` |

## Continuidad del proceso "Partidos"

La extracción de partidos es larga (pausas humanas de 2-5 s entre páginas y 3-8 s con scrolls de lectura en cada partido). Para no perder progreso ante fallos o interrupciones:

- Por cada equipo, el registro `Detalles_Partidos/progreso_partidos.json` guarda el **conteo de partidos procesados exitosamente** (lista ordenada de sus URLs, escritura atómica).
- Al iniciar una nueva extracción, el programa **reanuda automáticamente por conteo**: si un equipo lleva 5 de 10 partidos procesados, omite los primeros 5 enlaces y procesa desde el enlace 6.
- Un partido que terminó con errores de exportación **no se cuenta** como procesado y **se reintenta** en la siguiente ejecución.
- La verificación es posición por posición (URL incluida): si el orden del sitio cambiara, el conteo se detiene para nunca omitir partidos por error.
- Menú **Opciones → Reiniciar registro de partidos**: elimina el registro para reprocesar todo desde cero (los archivos existentes se sobrescriben).
- Si el registro está corrupto, se aparta con sufijo `.corrupto-<fecha>` y el proceso continúa con registro vacío.

## Configuración (`App.config`)

| Clave | Descripción | Valor por defecto |
|---|---|---|
| `Scraping:WaitTimeoutSeconds` | Espera máxima de carga de páginas/elementos | `10` |
| `Scraping:ChromeDebuggerAddress` | Chrome al que se conecta el scraper (ya abierto en la página principal del sitio) | `localhost:9222` |
| `Scraping:HumanPauseMinMs` / `MaxMs` | Pausa aleatoria de navegación humana entre páginas | `2000` / `5000` |
| `Scraping:MatchPauseMinMs` / `MaxMs` | Pausa aleatoria en la página de cada partido, antes de extraer | `3000` / `8000` |

## Arquitectura

Arquitectura en capas (Domain / Application / Infrastructure / UI) con inyección de dependencias (`Composition.cs`), logging a `%LocalAppData%\WinTopGun\logs` y 41 pruebas unitarias (`dotnet test`).

- `specs/001-layered-architecture/` — plan de refactorización a capas (ejecutado).
- `specs/002-progreso-partidos/` — plan de continuidad/reanudación del proceso de partidos (ejecutado).
- `WinTopGun/PLAN_DE_TRABAJO.md` — diagnóstico y plan original de optimización (ejecutado).

u# Flujo refinado: extracción de reportes de partido por equipo

## Observaciones sobre el HTML de ejemplo

- La celda objetivo se identifica de forma estable con `data-stat="match_report"`. Es mejor que depender de "penúltimo `<td>`", porque si la página añade o quita columnas, la posición cambia. Además, `notes` es el último `<td>`, lo que confirma que el enlace es el penúltimo, pero el atributo es más robusto.
- El `href` es **relativo** (`/en/matches/d5932b03/...`). Hay que convertirlo a URL absoluta antes de guardarlo o navegar.
- El identificador `d5932b03` dentro del `href` es único por partido. Sirve como **clave de deduplicación**, más confiable que la URL completa (el slug del texto podría cambiar, el ID no).
- Algunas filas pueden no tener enlace "Match Report" (partidos futuros o sin reporte). El texto de la celda estaría vacío o tendría otro contenido, así que hay que tolerarlo.

## Selector recomendado

```
//tr[@data-row]/td[@data-stat='match_report']/a[normalize-space(text())='Match Report']
```

Combina el atributo estable con la validación del texto, y descarta filas sin reporte.

## Paso a paso

### Fase 0: Preparación (una sola vez al iniciar la aplicación)

**0.1 - Cargar el registro persistente**
- Al arrancar, leer desde disco la estructura de enlaces procesados. Si no existe, crearla vacía.
- Esto garantiza que la persistencia sobreviva a cierres y fallos de la aplicación.

### Fase 1: Recolección de enlaces (por cada equipo)

**1.0 - Obtener los elementos `<a>`**
- Navegar a la página del calendario del equipo y esperar a que la tabla esté presente (espera explícita, no `Thread.Sleep`).
- Localizar todos los `<a>` con el selector anterior.

**1.1 - Extraer el `href`**
- Leer el atributo `href` de cada elemento.
- Normalizarlo a URL absoluta.
- Extraer el ID del partido.

**1.2 - Registrar en la estructura de datos**
- Cada entrada debe guardar como mínimo: ID del partido, URL, equipo, **posición** (el valor de `data-row`) y **estado**.

**Diseño de la estructura:**

| Campo | Propósito |
|---|---|
| `MatchId` | Clave única, evita duplicados |
| `Url` | Enlace absoluto a navegar |
| `Team` | Equipo al que pertenece el enlace |
| `RowIndex` | Posición en la tabla (cumple 2.3) |
| `Status` | `Pending`, `Processed`, `Failed` |
| `ProcessedAt` | Fecha/hora del procesamiento (opcional pero útil para auditoría) |

- **1.2.1 Persistencia a nivel de aplicación:** mantener la estructura en memoria con un diccionario indexado por `MatchId`, y respaldarla en disco. Para un proyecto Windows Forms, un archivo JSON es suficiente; SQLite es mejor si esperas miles de registros o necesitas consultas.
- **1.2.2 / 1.2.3 Consulta:** la regla es simple: si `MatchId` existe con estado `Processed`, se omite.
- Al registrar un enlace nuevo, entra como `Pending`. Si ya existe, **no se sobrescribe su estado** (error común que reprocesaría todo).

### Fase 2: Procesamiento de partidos

**2.1 - Consultar antes de procesar**
- Antes de navegar a cada enlace, preguntar a la estructura si ese `MatchId` está `Processed`.

**2.2 - Omitir procesados**
- Si lo está, saltar al siguiente sin abrir la página. Esto es lo que ahorra tiempo y evita bloqueos del sitio.

**2.3 - Identificar posición**
- Con `RowIndex` se puede reportar en la interfaz "partido 14 de 38, omitido" o reanudar desde un punto específico.

**2.4 - Procesar solo los pendientes**
- Para cada enlace `Pending` (o `Failed`, si decides reintentar):
  1. Navegar a la URL.
  2. Esperar a que las tablas estén cargadas.
  3. Extraer las tablas de resultados.
  4. **Solo si la extracción fue exitosa**, marcar como `Processed` y guardar a disco.
- Marcar como procesado **después** del éxito, no antes. Si la aplicación falla a mitad de camino, ese partido quedará pendiente y se reintentará.
- Guardar a disco **después de cada partido**, no solo al final. Así un cierre inesperado no pierde el progreso.

## Puntos de atención

1. **Cambio de página y elementos obsoletos:** al navegar a un partido, los `WebElement` de la lista original pierden validez (`StaleElementReferenceException`). Por eso conviene extraer **todos los `href` como texto en la Fase 1** antes de empezar a navegar, y no iterar sobre los elementos vivos.
2. **Un mismo partido en varios equipos:** un partido entre Arsenal y Manchester City aparece en el calendario de ambos. Como la clave es `MatchId` y no el par equipo-enlace, se procesará una sola vez. Decide si quieres ese comportamiento (probablemente sí) o si necesitas conservar la relación con ambos equipos en la estructura.
3. **Concurrencia en Windows Forms:** si el scraping corre en un hilo secundario o con `async`, el acceso a la estructura compartida debe estar protegido (por ejemplo con `ConcurrentDictionary` o un lock), y las actualizaciones de la interfaz deben hacerse por el hilo de UI.
4. **Ritmo de peticiones:** este tipo de sitios suele aplicar límites de velocidad. Incluye una pausa entre navegaciones; la omisión de procesados ya reduce mucho la carga.
5. **Estado `Failed`:** definir si los fallidos se reintentan automáticamente en la siguiente ejecución o requieren acción manual.

## Criterio de aceptación (2.4 reformulado)

- Un enlace con estado `Processed` en el registro **nunca** se abre ni se procesa de nuevo.
- Un enlace solo pasa a `Processed` tras extraer sus tablas con éxito.
- Al reiniciar la aplicación, el registro se recupera y se respeta.
- Ejecutar el proceso dos veces seguidas sobre el mismo equipo resulta en **cero** partidos procesados en la segunda ejecución.

Si quieres, el siguiente paso puede ser definir el formato de persistencia (JSON vs SQLite) o detallar la extracción de las tablas dentro de cada reporte de partido.

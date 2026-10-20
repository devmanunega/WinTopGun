# Plan de Trabajo — Continuidad del proceso "Partidos" (reanudación con registro de progreso)

> **Estado: ✅ EJECUTADO** (rama `ProgresoPartidos`). Fases 1–6 completadas: registro de progreso JSON con escritura atómica, reanudación automática por partido, opción de reinicio en el menú y 41 pruebas en verde (`dotnet test`).
> **Objetivo**: garantizar que, si el programa falla o se interrumpe a mitad de la extracción de partidos, el proceso pueda **reanudarse donde quedó** sin repetir trabajo ya hecho.

---

## 1. Problema

El proceso "Partidos" es largo (pausas humanas de 3-8 s + scrolls por partido, decenas de equipos y cientos de partidos por liga). Cuando el programa falla (excepción no prevista, cierre accidental, pérdida de conexión), **todo el progreso se pierde**: al reiniciar, vuelve a procesar partidos ya extraídos.

## 2. Solución propuesta

**Registro de progreso (checkpoint) en disco**: un archivo que registra los partidos ya procesados. Al iniciar una extracción, el programa carga el registro y **solo procesa los partidos pendientes** de cada equipo.

> Ejemplo: un equipo con 10 partidos jugados de los cuales 4 ya fueron procesados → en la siguiente ejecución se procesan únicamente los 6 restantes.

### 2.1 Decisiones de diseño

| # | Decisión | Elección | Justificación |
|---|----------|----------|---------------|
| D1 | Clave de un partido procesado | **URL del partido** (`/en/matches/<hash>/...`) | Única e inmutable por partido; independiente de nombres de archivo |
| D2 | Formato del registro | **JSON** con estructura legible y versionada | Permite guardar contexto (equipo, fecha, estado) y evolucionar sin romper compatibilidad |
| D3 | Ubicación | `Detalles_Partidos/progreso_partidos.json` | Vive junto a los datos que respalda; se borra con la carpeta |
| D4 | Escritura | **Atómica** (archivo temporal + `File.Move` con reemplazo) tras cada partido | Un fallo a mitad de escritura no corrompe el registro |
| D5 | Cuándo marcar un partido como procesado | **Solo si la exportación completa del partido no generó errores** (todas las tablas + `scorebox_meta` + `events_wrap`) | Un partido con exportación parcial se reintenta en la siguiente ejecución |
| D6 | Semántica de reanudación | **Automática**: si existe registro, se reanuda | Cero pasos manuales; el usuario no debe gestionar nada |
| D7 | Reprocesar todo | Opción en el menú **Opciones → Reiniciar registro de partidos** | Escapes explícitos cuando se quiere partir de cero sin borrar datos |
| D8 | Registro corrupto o ilegible | Se renombra a `progreso_partidos.json.corrupto-<timestamp>` y se empieza con registro vacío (log de advertencia) | El proceso nunca debe bloquearse por el propio mecanismo de progreso |

### 2.2 Estructura del archivo

```json
{
  "version": 1,
  "actualizado": "2026-10-05T14:32:11Z",
  "equipos": {
    "/en/squads/b8fd03ef/Manchester-City-Stats": {
      "nombre": "Manchester City",
      "partidos": {
        "https://fbref.com/en/matches/d5932b03/Arsenal-Manchester-City-...": {
          "segmento": "Arsenal-Manchester-City-August-16-2026-FA-Community-Shield",
          "procesado": "2026-10-05T14:30:01Z",
          "errores": []
        }
      }
    }
  }
}
```

- `errores`: lista vacía = partido completado correctamente. Si la exportación tuvo incidencias, el partido **no** se considera procesado (D5) y el detalle queda a modo de diagnóstico.
- La clave de equipo permite, en fases futuras, omitir por completo equipos cuyos partidos ya están todos procesados (optimización, ver §6).

---

## 3. Alcance

**Incluye**
- Registro y reanudación a nivel de **partido** (la unidad costosa del proceso).
- Reanudación automática y opción de reinicio manual del registro.
- Integridad del registro (escritura atómica, recuperación ante corrupción).
- Pruebas unitarias del mecanismo completo.

**No incluye (backlog)**
- Checkpoint a nivel de liga o de equipo (recorridos baratos: se repiten sin coste).
- Omisión total de equipos completos (optimización futura, §6).
- Registro de progreso para los procesos "Equipos" y "Jugadores" (se mantienen como están).

---

## 4. Plan de implementación (fases)

### Fase 1 — Modelo y contrato de persistencia de progreso
**Capa:** Domain + Application
- [ ] `Domain/Models/MatchProgress.cs`: `MatchProgressRecord` (equipo, urlPartido, segmento, fecha, errores) y `MatchProgressState` (versión, fecha de actualización, diccionario equipos → partidos).
- [ ] `Application/Interfaces/IMatchProgressRepository.cs`:
  - `MatchProgressState Load();`
  - `void MarkMatchProcessed(MatchProgressRecord registro);` (persiste de inmediato, atómico)
  - `bool IsProcessed(string teamUrl, string matchUrl);`
  - `void Reset();`
- [ ] Inyección por DI en `Composition.cs` (singleton; el archivo solo se usa en el proceso de partidos).

### Fase 2 — Implementación del repositorio JSON
**Capa:** Infrastructure
- [ ] `Infrastructure/Persistence/JsonMatchProgressRepository.cs`:
  - `System.Text.Json` (source-gen opcional), deserialización tolerante (propiedades faltantes = valores por defecto).
  - `MarkMatchProcessed`: actualizar en memoria + escritura atómica (`*.tmp` → `File.Move(..., overwrite: true)`).
  - `Load`: si el archivo no existe → estado vacío; si es ilegible → renombrar `.corrupto-<timestamp>` + estado vacío + log de advertencia (D8).
  - `Reset`: eliminar el archivo.
- [ ] Constantes de nombres de archivo en un único lugar (junto al repositorio).

### Fase 3 — Integración en el proceso de extracción
**Capa:** Application (`LeagueScrapingService`)
- [ ] `ExtractMatchDetailsAsync` recibe (vía DI) el repositorio de progreso.
- [ ] En `ProcessLeagueMatches`: cargar estado una sola vez al iniciar; log inicial: *"Reanudando: N partidos ya procesados"* (o *"No hay registro previo; iniciando de cero"*).
- [ ] En `ProcessTeamMatches`: al recolectar los enlaces "Match Report", **excluir** los ya procesados (`IsProcessed`) y loguear: *"Equipo 'X': M pendientes de P disputados"*.
- [ ] Si un equipo no tiene pendientes: omitir sus partidos sin navegar a ellos (aun así se visitó la página del equipo para reconstruir la lista; ver optimización §6).
- [ ] Tras terminar un partido **sin errores de exportación**: `MarkMatchProcessed(...)` (D5).
- [ ] `ScrapingResult`: contador nuevo `MatchesSkipped` (partidos omitidos por registro) para informar al usuario cuánto trabajo se ahorró.

### Fase 4 — UI y configuración
**Capa:** UI + Infrastructure/Configuration
- [ ] Menú **Opciones → Reiniciar registro de partidos**: confirmación con `MessageBox` → `Reset()` → mensaje de confirmación.
- [ ] Diálogo de resultado ampliado: *"N partidos procesados, M omitidos (ya procesados), K errores"*.
- [ ] (Opcional) Etiqueta de estado inicial: "Reanudando extracción de partidos…".

### Fase 5 — Pruebas unitarias
**Capa:** Tests
- [ ] `InMemoryMatchProgressRepository` (fake) para el servicio.
- [ ] Escenarios en `LeagueScrapingServiceTests`:
  - [ ] Reanudación parcial: partido-1 y partido-2 ya procesados → solo partido-3 se visita/exporta; `MatchesSkipped == 2`.
  - [ ] Partido con errores de exportación **no** se marca como procesado → en la segunda ejecución se reintenta.
  - [ ] Registro vacío (primera ejecución) → procesa todo.
  - [ ] Todos los partidos de un equipo procesados → no se navega a ningún partido de ese equipo.
- [ ] Escenarios en `JsonMatchProgressRepositoryTests` (con archivos temporales):
  - [ ] Persistencia y recarga (round-trip).
  - [ ] Escritura atómica no deja `.tmp` residuales.
  - [ ] Archivo corrupto → renombrado + estado vacío (D8).
  - [ ] `Reset` elimina el registro.

### Fase 6 — Validación y documentación
- [ ] `dotnet test` en verde.
- [ ] Prueba manual: interrumpir la extracción a mitad (cierre forzado) y reanudar; verificar que no se repiten partidos y que los archivos previos no se duplican.
- [ ] Documentar el mecanismo en `README.md` (formato del registro, opción de reinicio).
- [ ] Marcar este plan como ✅ EJECUTADO.

---

## 5. Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| El registro queda inconsistente si el proceso muere durante la escritura | Escritura atómica temporal+move (D4); append solo tras completar el partido |
| El sitio cambia la URL de un partido entre ejecuciones → se re-procesa | La URL incluye hash estable del partido; riesgo bajo. Si ocurre, solo implica trabajo repetido, no corrupción |
| Registro crece indefinidamente | Tamaño insignificante (KB); `Reset` manual disponible (D7) |
| Usuario espera re-extracción fresca y obtiene reanudación | Mensaje claro en log y diálogo de resultado (Fase 4); opción de reinicio visible |

## 6. Optimización futura (backlog)

- **Omisión de equipos completos**: guardar en el registro el total de partidos detectados por equipo; si `procesados == detectados`, saltar la visita a la página del equipo por completo. Requiere confianza en que la lista de partidos no cambió entre ejecuciones (los partidos disputados no "des-aparecen", riesgo bajo).
- Extender el mecanismo a los procesos "Equipos" y "Jugadores".

---

## 7. Estimación

| Fase | Esfuerzo estimado |
|---|---|
| 1. Modelo y contrato | Bajo |
| 2. Repositorio JSON | Bajo-Medio |
| 3. Integración en el servicio | Medio |
| 4. UI y configuración | Bajo |
| 5. Pruebas | Medio |
| 6. Validación y documentación | Bajo |

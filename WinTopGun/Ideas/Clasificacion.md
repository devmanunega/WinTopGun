# Algoritmo refinado: patrón anclado en la palabra "tabla"

Esta aclaración simplifica el algoritmo. Ya no hace falta **descubrir** patrones comparando todas las combinaciones de palabras. Basta **extraer** el patrón de cada nombre a partir de un ancla (`tabla`). Eso reemplaza las fases 3 a 8 del algoritmo anterior (candidatos, soporte, redundancia, fragmentación y puntuación).

## Cambio de enfoque

| Antes | Ahora |
|---|---|
| Se generaban candidatos y se elegía el más específico | Cada archivo produce **un solo patrón** de forma directa |
| Un archivo podía coincidir con varios patrones | Un archivo pertenece a **exactamente un patrón**, sin empates |
| Había que decidir si `tabla_events` y `tabla_events_wrap` competían | Son patrones distintos porque la **cadena completa** es distinta |

Como el patrón es la secuencia completa normalizada, `tabla_stats`, `tabla_stats_summary`, `tabla_events` y `tabla_events_wrap` nunca se confunden por contener uno al otro. Se agrupa por **igualdad exacta** de la cadena, no por "contiene".

## Pasos del algoritmo

**1. Inventario**
Igual que antes: solo el nivel raíz, extensiones permitidas, excluyendo `*_Consolidado.*` y temporales.

**2. Tokenización**
- Tomar el nombre sin extensión.
- Comparar en minúsculas y sin tildes.
- Separar por `_`, `-`, espacios y puntos.
- Conservar el nombre original del archivo, que no se modifica.

**3. Localizar el ancla**
- Buscar el token `tabla` como **palabra completa**. `tablas`, `mitabla` y `tabla1` no cuentan.
- Si aparece varias veces, usar la primera aparición que esté seguida de al menos una palabra válida.
- Todo lo que está **antes** del ancla (`archivo1`, `reporte`, fechas) se descarta.
- Si no hay ancla, el archivo va a **sin clasificar**.

**4. Recolectar los tokens posteriores**
Recorrer los tokens que siguen al ancla y clasificar cada uno:

| Tipo de token | Ejemplo | Acción |
|---|---|---|
| Palabra (solo letras) | `events`, `wrap`, `keeper`, `stats`, `summary` | **Se conserva** |
| Alfanumérico mezclado (letras y dígitos) | `a8f3k2`, `9c1d77`, `v2` | **Se elimina** |
| Numérico puro | `2`, `01`, `20260816` | **Se elimina** |
| Palabra de ruido configurable | `copy`, `final`, `backup`, `old` | **Se elimina** (lista editable) |

Los tokens eliminados **se saltan, no cortan** la secuencia. Esto es lo que resuelve tu caso `tabla_stats_[aleatorio]_summary`: el token aleatorio se descarta y `summary` sigue formando parte del patrón.

**5. Construir el patrón**
Unir `tabla` con los tokens conservados, en orden, usando `_`. Esa cadena es el patrón y el nombre del directorio.

**6. Agrupar**
Agrupar los archivos por patrón exacto. Cada grupo es un directorio.

**7. Validar y generar el plan**
Igual que antes: vista previa, sanitización del nombre, directorio existente reutilizado, sin sobrescribir al mover y log de movimientos.

## Ejemplo resuelto

| Archivo | Tokens relevantes tras el ancla | Patrón |
|---|---|---|
| `archivo1_tabla_events.txt` | `events` | `tabla_events` |
| `archivo4_tabla_events.txt` | `events` | `tabla_events` |
| `x_tabla_events_wrap.txt` | `events`, `wrap` | `tabla_events_wrap` |
| `tabla_keeper_stats.txt` | `keeper`, `stats` | `tabla_keeper_stats` |
| `archivo2_tabla_stats.txt` | `stats` | `tabla_stats` |
| `tabla_stats_a8f3k2_summary.txt` | `stats`, ~~`a8f3k2`~~, `summary` | `tabla_stats_summary` |
| `tabla_stats_9c1d77_summary.txt` | `stats`, ~~`9c1d77`~~, `summary` | `tabla_stats_summary` |
| `notas.txt` | (sin ancla) | sin clasificar |

Resultado: 5 directorios, y los dos archivos con identificador aleatorio quedan juntos en `tabla_stats_summary`.

## Puntos delicados

1. **Qué se limpia y qué no.** La eliminación del identificador aleatorio aplica al **nombre del directorio** (el patrón). Los archivos conservan su nombre original al moverse, porque renombrarlos podría generar colisiones y perder la trazabilidad. Si quisieras renombrar también los archivos, habría que definir qué hacer cuando dos terminan con el mismo nombre.
2. **Detección del identificador aleatorio.** La regla "mezcla de letras y dígitos" cubre la mayoría de los casos, pero falla si el identificador es solo letras (`kqzxwp`) o solo números (se elimina como numérico, sin problema). Para el caso de solo letras, una señal de respaldo es que el token aparezca en un único archivo mientras sus vecinos sí se repiten. Estos casos se marcan como **sospechosos** en la vista previa en lugar de decidirlos en silencio.
3. **Palabras con dígitos legítimas.** Un token como `v2` o `q3` podría ser parte real del patrón. Si ocurre, la regla debe tener una lista de excepciones o un modo en que el usuario marque ciertos tokens como "conservar".
4. **Sufijos numéricos.** En la versión anterior `tabla_events_2` era un patrón separado. Con esta regla, el número se elimina y esos archivos caerían en `tabla_events`. Hay que confirmar cuál comportamiento quieres.
5. **Patrones nuevos o raros.** Un error de escritura (`tabla_evnts`) crea un directorio propio. Conviene mantener un **catálogo de patrones conocidos** (`tabla_events`, `tabla_events_wrap`, `tabla_keeper_stats`, `tabla_stats`, `tabla_stats_summary`) y, en la vista previa, distinguir los que coinciden con el catálogo de los **nuevos**, para revisarlos antes de crear el directorio.
6. **Grupos con un solo archivo.** Al no depender de un umbral de repeticiones, un archivo suelto con ancla genera su propio directorio. Se sugiere marcar los grupos de un archivo como advertencia, sin bloquearlos.
7. **Mismo patrón, distinta estructura.** Con `tabla_stats_summary` agrupando archivos de orígenes distintos, la validación de encabezados de la fase de depuración es la que detectará si alguno no encaja.

## Decisiones pendientes

1. ¿Los números sueltos tras el ancla (`tabla_events_2`) se eliminan o crean un patrón propio?
2. ¿Usar un catálogo fijo de patrones conocidos, descubrimiento libre o ambos con confirmación en la vista previa?
3. ¿Qué palabras entran en la lista de ruido (`copy`, `final`, `backup`...)?
4. ¿Los archivos conservan su nombre original al moverse?
5. ¿El identificador aleatorio siempre mezcla letras y dígitos, o puede ser solo letras?

Con esas respuestas, el siguiente paso puede ser definir las reglas exactas de detección del identificador aleatorio o el diseño de la vista previa con el catálogo de patrones.

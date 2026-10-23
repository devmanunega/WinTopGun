using WinTopGun.Domain.Models;

namespace WinTopGun.Application.Interfaces;

/// <summary>
/// Organiza los archivos de un directorio en subdirectorios según el patrón
/// anclado en la palabra "tabla" de su nombre (ver Ideas/Clasificacion.md).
/// </summary>
public interface IClasificacionService
{
    /// <summary>
    /// Analiza el directorio indicado (solo nivel raíz) y genera el plan de
    /// organización sin mover ningún archivo: permite mostrar una vista previa.
    /// </summary>
    /// <param name="directorio">Directorio cuyos archivos se organizarán.</param>
    /// <returns>Plan con los grupos por patrón y los archivos sin clasificar.</returns>
    ClasificacionPlan GenerarPlan(string directorio);

    /// <summary>
    /// Ejecuta el plan: crea los subdirectorios y mueve los archivos a su grupo,
    /// conservando el nombre original y sin sobrescribir archivos existentes.
    /// </summary>
    /// <param name="plan">Plan generado previamente con <see cref="GenerarPlan"/>.</param>
    /// <returns>Resultado con la cantidad de movimientos e incidencias.</returns>
    ClasificacionResultado EjecutarPlan(ClasificacionPlan plan);
}

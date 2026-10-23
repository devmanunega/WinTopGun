using WinTopGun.Domain.Models;

namespace WinTopGun.Application.Interfaces;

/// <summary>
/// Depura los archivos ya organizados en subdirectorios por patrón: valida que
/// los encabezados de los archivos de cada grupo sean consistentes, detectando
/// archivos de orígenes distintos que no encajan (Ideas/Clasificacion.md, punto 7).
/// </summary>
public interface IDepuracionService
{
    /// <summary>
    /// Analiza los subdirectorios del directorio indicado y genera el informe
    /// de depuración. No modifica ningún archivo.
    /// </summary>
    /// <param name="directorio">Directorio raíz con los subdirectorios por patrón.</param>
    /// <returns>Informe con el estado de cada grupo y sus incidencias.</returns>
    DepuracionInforme Depurar(string directorio);
}

namespace WinTopGun.Domain.Models;

/// <summary>
/// Enlace de reporte de partido ("Match Report") detectado en la tabla de
/// historial de un equipo, identificado por el ID único del partido.
/// </summary>
/// <param name="MatchId">
/// Identificador único del partido (p. ej. <c>d5932b03</c> en
/// <c>/en/matches/d5932b03/...</c>). Es la clave de deduplicación: más confiable
/// que la URL completa, cuyo slug puede cambiar.
/// </param>
/// <param name="Url">URL absoluta del reporte del partido.</param>
/// <param name="Team">Equipo al que pertenece el enlace.</param>
/// <param name="RowIndex">Posición de la fila en la tabla (atributo <c>data-row</c>); -1 si no está disponible.</param>
public sealed record MatchLinkTarget(string MatchId, string Url, string Team, int RowIndex);

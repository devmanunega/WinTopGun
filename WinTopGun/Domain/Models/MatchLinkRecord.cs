namespace WinTopGun.Domain.Models;

/// <summary>
/// Registro persistido de un enlace de partido dentro de la estructura de
/// seguimiento (ver Ideas/Partidos.md, fase 1.2).
/// </summary>
/// <param name="MatchId">Identificador único del partido (clave del diccionario).</param>
/// <param name="Url">URL absoluta del reporte del partido.</param>
/// <param name="Team">Equipo al que pertenece el enlace (el primero que lo registró).</param>
/// <param name="RowIndex">Posición de la fila en la tabla del equipo.</param>
/// <param name="Status">Estado del enlace: Pending, Processed o Failed.</param>
/// <param name="ProcessedAtUtc">Fecha/hora (UTC) en que pasó a Processed; nulo si aún no.</param>
public sealed record MatchLinkRecord(
    string MatchId,
    string Url,
    string Team,
    int RowIndex,
    MatchLinkStatus Status,
    DateTime? ProcessedAtUtc);

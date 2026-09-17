namespace WinTopGun.Domain.Models;

/// <summary>
/// Tabla HTML extraída de una página de liga.
/// </summary>
/// <param name="TableId">Identificador del atributo <c>id</c> de la tabla.</param>
/// <param name="Content">Contenido textual (innerText) de la tabla.</param>
public sealed record ExtractedTable(string TableId, string Content);

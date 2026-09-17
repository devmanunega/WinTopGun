namespace WinTopGun.Domain.Models;

/// <summary>
/// Liga detectada en la página principal del sitio objetivo.
/// </summary>
/// <param name="Index">Índice basado en 1 dentro de la lista de ligas.</param>
/// <param name="Name">Nombre visible del enlace de la liga.</param>
/// <param name="Url">URL del enlace, si está disponible.</param>
public sealed record LeagueInfo(int Index, string Name, string? Url);

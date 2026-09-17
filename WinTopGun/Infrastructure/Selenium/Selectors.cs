using OpenQA.Selenium;

namespace WinTopGun.Infrastructure.Selenium;

/// <summary>
/// Localizadores (selectores) del sitio objetivo centralizados en un único punto:
/// si cambia el DOM del sitio, solo este archivo debe actualizarse.
/// </summary>
public static class Selectors
{
    /// <summary>Enlaces a las ligas en la página principal.</summary>
    public static readonly By LeagueLinks = By.CssSelector("#div_league_summary .data_grid_box .gridtitle a");

    /// <summary>Tablas que poseen atributo <c>id</c>.</summary>
    public static readonly By TablesWithId = By.CssSelector("table[id]");

    /// <summary>Tabla con un identificador concreto.</summary>
    public static By TableById(string id) => By.Id(id);
}

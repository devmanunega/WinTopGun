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

    /// <summary>Enlaces (p. ej. a páginas de jugadores) dentro de las secciones de contenido de una página de liga.</summary>
    public static readonly By PlayerLinks = By.CssSelector("#content .section_heading_text > ul > li > a");

    /// <summary>Filas de la tabla de posiciones (un equipo por fila).</summary>
    public static readonly By TableRows = By.XPath(".//tr");

    /// <summary>Tablas que poseen atributo <c>id</c>.</summary>
    public static readonly By TablesWithId = By.CssSelector("table[id]");

    /// <summary>Tabla con un identificador concreto.</summary>
    public static By TableById(string id) => By.Id(id);

    /// <summary>Enlace de equipo en la tabla de clasificación: celda estándar del sitio (<c>data-stat="team"</c>).</summary>
    public static readonly By TeamLink = By.CssSelector("td[data-stat='team'] a");

    /// <summary>Enlace de equipo alternativo: primer <c>td</c> de cada <c>tr</c> (la primera celda puede ser un <c>th</c> con la posición).</summary>
    public static readonly By FirstDataCellLink = By.CssSelector("td:first-of-type a");

    /// <summary>Tabla de historial de partidos de un equipo (<c>matchlogs_for</c>), donde residen los enlaces "Match Report".</summary>
    public static readonly By MatchLogsTable = By.Id("matchlogs_for");

    /// <summary>
    /// Filas de datos de la tabla de historial de partidos. El atributo
    /// <c>data-row</c> identifica de forma estable cada fila (y su posición).
    /// </summary>
    public static readonly By MatchLogDataRows = By.XPath(".//tr[@data-row]");

    /// <summary>
    /// Enlace del reporte del partido dentro de una fila: celda estándar
    /// (<c>data-stat="match_report"</c>) con texto exacto "Match Report". Combina
    /// el atributo estable con la validación del texto y descarta filas sin
    /// reporte (partidos futuros). Ver Ideas/Partidos.md — selector recomendado.
    /// </summary>
    public static readonly By MatchReportCellLink =
        By.XPath(".//td[@data-stat='match_report']/a[normalize-space(text())='Match Report']");

    /// <summary>Div con el marcador del partido en la página de detalle (no es una tabla).</summary>
    public static readonly By Scorebox = By.Id("scorebox");

    /// <summary>Div con los datos de campo del partido en la página de detalle (no es una tabla).</summary>
    public static readonly By FieldWrap = By.Id("field_wrap");

    /// <summary>Div de metadatos del partido en la página de detalle.</summary>
    public static readonly By ScoreboxMeta = By.Id("scorebox_meta");

    /// <summary>Div de eventos del partido en la página de detalle.</summary>
    public static readonly By EventsWrap = By.Id("events_wrap");
}

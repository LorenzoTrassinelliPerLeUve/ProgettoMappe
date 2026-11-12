namespace ProgettoMappe.Web.Report;

/// <summary>
/// Il modello grafico dei report PowerPoint (task #198): colori e caratteri del marchio Perleuve/4Grapes (gli stessi
/// di <c>app.css</c>), piè di pagina. Montserrat e Source Sans 3 non sono di serie in Office: dove mancano, PowerPoint
/// li sostituisce con un carattere simile (il file non li incorpora).
/// </summary>
public sealed record ModelloReport(
    string Verde = "8DC63E",
    string Viola = "3C3950",
    string Ardesia = "5F727F",
    string Chiaro = "F4F6F2",
    string CarattereTitoli = "Montserrat",
    string CarattereTesto = "Source Sans 3",
    string PieDiPagina = "Perleuve · 4Grapes")
{
    public static ModelloReport Perleuve { get; } = new();
}

/// <summary>Una colonna di tabella: <see cref="Numero"/> = allineata a destra; <see cref="Peso"/> = larghezza relativa.</summary>
public sealed record ColonnaReport(string Titolo, bool Numero = false, double Peso = 1);

/// <summary>Una tessera numerica (KPI) accanto a un grafico.</summary>
public sealed record TesseraReport(string Etichetta, string Numero, string? Dettaglio = null);

/// <summary>Una barra del grafico (Valore null = barra mancante).</summary>
public sealed record BarraReport(string Etichetta, double? Valore);

/// <summary>Fascia colorata di un quadrante (colore #rrggbb), nelle unità del valore.</summary>
public sealed record FasciaReport(double Da, double A, string Colore);

/// <summary>
/// Un quadrante (gauge) disegnato con forme vere: fasce colorate su mezzo cerchio, lancetta sul
/// <see cref="Valore"/>, <see cref="Testo"/> sotto il centro e <see cref="Sotto"/> in fondo.
/// </summary>
public sealed record QuadranteReport(string Titolo, double Minimo, double Massimo, IReadOnlyList<FasciaReport> Fasce,
    double? Valore, string? Testo, string? Sotto);

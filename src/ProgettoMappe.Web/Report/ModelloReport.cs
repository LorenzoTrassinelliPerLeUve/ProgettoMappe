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
    string PieDiPagina = "Perleuve · 4Grapes",
    LogoReport? Logo = null)
{
    /// <summary>Il modello Perleuve, con il logo delle presentazioni Perleuve (task #197) in copertina e nel piè di pagina.</summary>
    public static ModelloReport Perleuve { get; } = new(PieDiPagina: "4Grapes", Logo: LogoReport.Perleuve);
}

/// <summary>Un logo PNG con le sue dimensioni in pixel (servono per non deformarlo).</summary>
public sealed record LogoReport(byte[] Png, int Larghezza, int Altezza)
{
    /// <summary>Il logo Perleuve (<c>Report/Risorse/logo-perleuve.png</c>, 492 × 155, incluso nell'assembly).</summary>
    public static LogoReport Perleuve { get; } = Carica("ProgettoMappe.Web.logo-perleuve.png");

    private static LogoReport Carica(string risorsa)
    {
        using var flusso = typeof(LogoReport).Assembly.GetManifestResourceStream(risorsa)
                           ?? throw new InvalidOperationException($"Risorsa {risorsa} mancante");
        using var copia = new MemoryStream();
        flusso.CopyTo(copia);
        var png = copia.ToArray();
        // Larghezza e altezza stanno nell'intestazione IHDR del PNG, ai byte 16–23 (big endian).
        return new LogoReport(png, System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16)),
            System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20)));
    }

    /// <summary>Larghezza in EMU per l'altezza data, con le proporzioni del logo.</summary>
    public long LarghezzaPer(long altezza) => altezza * Larghezza / Altezza;
}

/// <summary>Una colonna di tabella: <see cref="Numero"/> = allineata a destra; <see cref="Peso"/> = larghezza relativa.</summary>
public sealed record ColonnaReport(string Titolo, bool Numero = false, double Peso = 1);

/// <summary>Una tessera numerica (KPI) accanto a un grafico.</summary>
public sealed record TesseraReport(string Etichetta, string Numero, string? Dettaglio = null);

/// <summary>Una serie di un grafico a più serie: un valore per voce (null = mancante); colore #rrggbb o null = dal modello.</summary>
public sealed record SerieReport(string Nome, IReadOnlyList<double?> Valori, string? Colore = null);

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

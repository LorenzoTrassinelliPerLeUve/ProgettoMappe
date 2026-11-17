using System.Globalization;
using ProgettoMappe.Web.Report;
using ProgettoMappe.Web.Tematismi;

namespace ProgettoMappe.Web.ReportAnalisiSuolo;

/// <summary>
/// Il PowerPoint del report analisi del suolo (task #317), al posto dell'automazione Qlik PPT_REPORT_ANALISI_DEL_TERRENO:
/// una slide per foglio di <see cref="CalcoloAnalisiSuolo.Fogli"/> nello stesso ordine (52), sempre tutte anche senza dati,
/// con grafici veri al posto delle immagini dei fogli. Le tabelle oltre 25 righe continuano su altre slide.
/// </summary>
public static class PowerPointAnalisiSuolo
{
    public const string RossoRiferimento = "B82D23";
    private static readonly string[] ColoriTessitura = ["8E477D", "F8981D", "FFCF02"];
    private static readonly string[] ColoriCsc = ["8DC63E", "3C6FB4", "C0392B", "F8981D", "5F727F"];
    private static readonly CultureInfo It = CultureInfo.GetCultureInfo("it-IT");

    /// <summary>Come l'automazione: «Report Analisi del Terreno - {RagioneSociale}»; caratteri non ammessi → «_».</summary>
    public static string NomeFile(string ragioneSociale, string estensione = ".pptx")
    {
        var vietati = Path.GetInvalidFileNameChars().Concat(['/', '\\', ':', '*', '?', '"', '<', '>', '|']).ToHashSet();
        var nome = $"Report Analisi del Terreno - {ragioneSociale}";
        return new string(nome.Select(c => vietati.Contains(c) ? '_' : c).ToArray()).Trim() + estensione;
    }

    /// <summary>Le selezioni attive, scritte come riga (vanno nella nota di ogni slide, come «Add Selection To Report» di Qlik).</summary>
    public static string Selezioni(SelezioneSuolo s)
    {
        var parti = new List<string>();
        void Se(string nome, object? valore)
        {
            if (valore is not null)
            {
                parti.Add($"{nome}: {(valore is DateOnly d ? d.ToString("dd/MM/yyyy", It) : valore)}");
            }
        }
        Se("Anno", s.Anno);
        Se("Vigna", s.Vigna);
        Se("Vitigno", s.Vitigno);
        Se("Sottovigneto", s.SubVigneto);
        Se("Data prelievo", s.DataPrelievo);
        Se("Profondità cm", s.Profondita);
        Se("Vigneto", s.Vigneto);
        Se("Codice punto", s.CodicePunto);
        return parti.Count == 0 ? "Tutte le analisi" : string.Join(" · ", parti);
    }

    public static byte[] Crea(string ragioneSociale, IReadOnlyList<RigaSuolo> righe, SelezioneSuolo selezione, DateTimeOffset adesso)
    {
        var oggi = TimeZoneInfo.ConvertTime(adesso, TematismiBigot.FusoItalia).DateTime;
        var ppt = new PresentazionePptx($"Report Analisi del Terreno - {ragioneSociale}",
            $"Report Analisi del Terreno · {ragioneSociale} · generato il {oggi.ToString("dd/MM/yyyy", It)}");
        var nota = Selezioni(selezione);
        const string vuoto = "Nessun dato nella selezione.";
        string N(double? v, string formato) => CalcoloAnalisiSuolo.Numero(v, formato);

        foreach (var f in CalcoloAnalisiSuolo.Fogli)
        {
            switch (f.Tipo)
            {
                case TipoFoglioSuolo.Parametro or TipoFoglioSuolo.ConLimiti:
                {
                    var barre = CalcoloAnalisiSuolo.Barre(righe, f);
                    var voci = barre.Select(b => f.Tipo == TipoFoglioSuolo.ConLimiti ? b.Voce : b.Etichetta).ToList();
                    var serie = new List<SerieReport> { new(f.Titolo, barre.Select(b => (double?)b.Valore).ToList()) };
                    if (f.Tipo == TipoFoglioSuolo.ConLimiti)
                    {
                        serie.Add(new("Limite Min", barre.Select(b => b.Minimo).ToList(), "3C6FB4"));
                        serie.Add(new("Limite Max", barre.Select(b => b.Massimo).ToList(), RossoRiferimento));
                    }
                    serie.AddRange(f.Linee.Select(l => new SerieReport(N(l, "0.0"), barre.Select(_ => (double?)l).ToList(),
                        RossoRiferimento, Tratteggiata: true)));
                    ppt.GraficoSerie(f.Titolo, nota, voci, serie, false,
                        [new(f.Titolo, N(CalcoloAnalisiSuolo.Kpi(righe, f.Campo!, f.Fattore), f.Formato))], vuoto, f.Formato,
                        f.Minimo, f.Massimo, serieLinea: serie.Count - 1);
                    break;
                }
                case TipoFoglioSuolo.Tessitura:
                {
                    var t = CalcoloAnalisiSuolo.Tessitura(righe);
                    ppt.GraficoSerie(f.Titolo, nota, t.Select(x => x.Vigneto).ToList(),
                    [
                        new("Argilla ‰", t.Select(x => x.Argilla).ToList(), ColoriTessitura[0]),
                        new("Limo ‰", t.Select(x => x.Limo).ToList(), ColoriTessitura[1]),
                        new("Sabbia ‰", t.Select(x => x.Sabbia).ToList(), ColoriTessitura[2]),
                    ], false, Frazioni(righe), vuoto, "0", 0, 1000, impilato: true);
                    break;
                }
                case TipoFoglioSuolo.TriangoloUsda:
                    ppt.Tabella("Classe Tessiturale", $"{nota} · {string.Join(" · ", Frazioni(righe).Select(x => $"{x.Etichetta} {x.Numero}"))}",
                    [
                        new("Vigna", Peso: 1.4), new("Vigneto", Peso: 1.6), new("Codice Punto"), new("Classe Tessiturale", Peso: 1.4),
                        new("Argilla ‰", true), new("Limo ‰", true), new("Sabbia ‰", true),
                    ], CalcoloAnalisiSuolo.Classi(righe).Select(c => (IReadOnlyList<string?>)
                    [
                        c.Vigna, c.Vigneto, c.Punto, c.Classe, N(CalcoloAnalisiSuolo.Media(c.Terne.Select(x => x.Argilla)) * 10, "0"),
                        N(CalcoloAnalisiSuolo.Media(c.Terne.Select(x => x.Limo)) * 10, "0"),
                        N(CalcoloAnalisiSuolo.Media(c.Terne.Select(x => x.Sabbia)) * 10, "0"),
                    ]).ToList(), vuoto);
                    break;
                case TipoFoglioSuolo.Idropedologici:
                    ppt.Tabella(f.Titolo, $"{nota} · {string.Join(" · ", Idropedologici(righe).Select(x => $"{x.Etichetta} {x.Numero}"))}",
                    [
                        new("Vigna", Peso: 1.3), new("Vigneto", Peso: 1.5), new("Classe", Peso: 1.3), new("Codice Punto"),
                        new("Capacità di Campo (% v/v)", true), new("Punto di Appassimento (% v/v)", true),
                        new("Acqua Disponibile (l/m³)", true), new("Peso Specifico (t/m³)", true), new("Conducibilità Idraulica (cm/h)", true),
                    ], CalcoloAnalisiSuolo.Classi(righe).Select(c => (IReadOnlyList<string?>)
                    [
                        c.Vigna, c.Vigneto, c.Classe, c.Punto, N(CalcoloAnalisiSuolo.Media(c.Terne.Select(x => x.CapacitaDiCampo)), "0.0"),
                        N(CalcoloAnalisiSuolo.Media(c.Terne.Select(x => x.PuntoDiAppassimento)), "0.0"),
                        N(CalcoloAnalisiSuolo.Media(c.Terne.Select(x => x.AcquaDisponibileMmM)), "0.0"),
                        N(CalcoloAnalisiSuolo.Media(c.Terne.Select(x => x.PesoSpecifico)), "0.00"),
                        N(CalcoloAnalisiSuolo.Media(c.Terne.Select(x => x.ConducibilitaIdraulica)), "0.00"),
                    ]).ToList(), vuoto);
                    break;
                case TipoFoglioSuolo.ComposizioneCsc:
                {
                    var c = CalcoloAnalisiSuolo.ComposizioneCsc(righe);
                    var kpi = CalcoloAnalisiSuolo.ComponentiCsc.Select(x => CalcoloAnalisiSuolo.Kpi(righe, x.Campo)).ToList();
                    ppt.GraficoSerie(f.Titolo, nota, c.Select(x => x.Punto).ToList(),
                        CalcoloAnalisiSuolo.ComponentiCsc.Select((x, i) => new SerieReport($"{x.Nome} cmol(₊)/kg", c.Select(p => p.Valori[i]).ToList(),
                            ColoriCsc[i])).ToList(), false,
                    [
                        new("Ca cmol(₊)/kg", N(kpi[0], "0.00")), new("Mg cmol(₊)/kg", N(kpi[1], "0.00")), new("K cmol(₊)/kg", N(kpi[2], "0.00")),
                        new("Na · Acidità cmol(₊)/kg", $"{N(kpi[3], "0.00")} · {N(kpi[4], "0.00")}"),
                    ], vuoto, f.Formato, 0, null, impilato: true);
                    break;
                }
                case TipoFoglioSuolo.Considerazioni:
                    // Riquadro vuoto da riempire a mano, come in Qlik (un paragrafo vuoto: il testo non può essere senza).
                    ppt.Testo(f.Titolo, nota, [" "]);
                    break;
            }
        }
        return ppt.Salva();
    }

    /// <summary>I tre KPI delle frazioni (‰), medie per terna distinta.</summary>
    public static IReadOnlyList<TesseraReport> Frazioni(IReadOnlyList<RigaSuolo> righe) =>
    [
        new("Frazione Limosa ‰", CalcoloAnalisiSuolo.Numero(CalcoloAnalisiSuolo.MediaTessitura(righe, t => t.Limo) * 10, "0")),
        new("Frazione Sabbiosa ‰", CalcoloAnalisiSuolo.Numero(CalcoloAnalisiSuolo.MediaTessitura(righe, t => t.Sabbia) * 10, "0")),
        new("Frazione Argillosa ‰", CalcoloAnalisiSuolo.Numero(CalcoloAnalisiSuolo.MediaTessitura(righe, t => t.Argilla) * 10, "0")),
    ];

    /// <summary>I cinque KPI del foglio «Parametri Idropedologici».</summary>
    public static IReadOnlyList<TesseraReport> Idropedologici(IReadOnlyList<RigaSuolo> righe) =>
    [
        new("Punto di Appassimento", CalcoloAnalisiSuolo.Numero(CalcoloAnalisiSuolo.MediaTessitura(righe, t => t.PuntoDiAppassimento), "0.0")),
        new("Acqua Disponibile", CalcoloAnalisiSuolo.Numero(CalcoloAnalisiSuolo.MediaTessitura(righe, t => t.AcquaDisponibileMmM), "0.0")),
        new("Capacità di Campo", CalcoloAnalisiSuolo.Numero(CalcoloAnalisiSuolo.MediaTessitura(righe, t => t.CapacitaDiCampo), "0.0")),
        new("Conducibilità Idraulica", CalcoloAnalisiSuolo.Numero(CalcoloAnalisiSuolo.MediaTessitura(righe, t => t.ConducibilitaIdraulica), "0.00")),
        new("Peso Specifico", CalcoloAnalisiSuolo.Numero(CalcoloAnalisiSuolo.MediaTessitura(righe, t => t.PesoSpecifico), "0.00")),
    ];
}

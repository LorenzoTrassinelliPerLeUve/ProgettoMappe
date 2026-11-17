using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using ProgettoMappe.Api.AnalisiSuolo;
using ProgettoMappe.Contracts;
using ProgettoMappe.Web.ReportAnalisiSuolo;
using Xunit;

namespace ProgettoMappe.Api.Tests;

/// <summary>
/// Parità del Report analisi del suolo con Qlik (task #322): i KPI dei 52 fogli letti da Qlik per 3 aziende, tutti gli anni e
/// ultimo anno (<c>docs/specifiche/report-analisi-suolo/dati/parita-qlik-suolo.json</c>), contro i numeri di Mappe calcolati sulle
/// query di <see cref="FourGrapesLettureAnalisiSuolo"/> eseguite dal dba (Excel in <c>MAPPE_SUOLO_DATI</c>, predefinita la cartella
/// del lavoro). Ogni differenza ha la sua causa in <see cref="Causa"/>; la tabella completa va in <c>MAPPE_PARITA_SUOLO_USCITA</c>
/// (predefinito <c>parita-qlik-suolo-esito.md</c> nella cartella temporanea).
/// </summary>
public class ParitaAnalisiSuoloTests
{
    private static readonly string CartellaDati = Environment.GetEnvironmentVariable("MAPPE_SUOLO_DATI") is { Length: > 0 } p
        ? p
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "progetti", "lavori", "2026-11-17-report-maturazione-suolo", "dati", "query-mappe-317");

    private static readonly string Verita = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "specifiche",
        "report-analisi-suolo", "dati", "parita-qlik-suolo.json");

    /// <summary>Le 3 aziende della verità: chiave nel JSON → IdEnte.</summary>
    private static readonly (string Chiave, int Id)[] Aziende = [("*TIKVES*VELES*", 1862), ("*Jidvei*", 35), ("*Mirafiore*", 425)];

    public enum Esito { Uguale, Voluta, DaCapire, SenzaTessitura }

    /// <summary>Un numero da confrontare: foglio di Qlik, misura, come si legge in Qlik e come lo calcola Mappe.</summary>
    private sealed record Voce(int Foglio, string Misura, Func<JsonElement, double?> Qlik, Func<IReadOnlyList<RigaSuolo>, double?> Mappe,
        bool UsaTessitura = false)
    {
        /// <summary>Il campo del foglio parametro (per confrontare anche le barre).</summary>
        public string? Campo { get; init; }
    }

    /// <param name="MappeLaboratorio">Lo stesso numero con il valore del laboratorio al posto di quello calcolato (come gli Excel di
    /// Qlik): serve a riconoscere le differenze dovute alla decisione 2 del 17/11.</param>
    public sealed record Confronto(int Foglio, string Misura, string Azienda, string Selezione, double? Qlik, double? Mappe,
        double? MappeLaboratorio, string? Barre, Esito Esito, string? Causa);

    /// <summary>Colonne calcolate da Mappe → colonna del laboratorio nel 4g.</summary>
    private static readonly (string Campo, string Colonna)[] Laboratorio =
    [
        (CampiSuolo.So, "Suolo_SO_perc"), (CampiSuolo.CN, "Suolo_C_N"), (CampiSuolo.CaCsc, "Suolo_CSC_Ca"), (CampiSuolo.MgCsc, "Suolo_CSC_Mg"),
        (CampiSuolo.KCsc, "Suolo_CSC_K"), (CampiSuolo.NaCsc, "Suolo_CSC_Na"), (CampiSuolo.Saturazione, "Suolo_saturazionebasica_perc"),
        (CampiSuolo.AciditaCsc, "Suolo_aciditascambio_perc"), (CampiSuolo.MgK, "Suolo_Mg_K_meq"), (CampiSuolo.CaMg, "Suolo_Ca_Mg_meq"),
        (CampiSuolo.CaK, "Suolo_Ca_K_meq"), (CampiSuolo.SoArgilla, "Suolo_SO_Argilla"), (CampiSuolo.K2, "Suolo_K2"),
    ];

    /// <summary>Le righe con i valori del laboratorio dove ci sono, e l'azoto senza la regola dell'unità.</summary>
    private static IReadOnlyList<RigaSuolo> ComeLaboratorio(IEnumerable<RigaSuolo> righe) =>
        righe.Select(r =>
        {
            var c = r.Campi.ToDictionary();
            foreach (var (campo, colonna) in Laboratorio)
            {
                if (r.Analisi.Valori.TryGetValue(colonna, out var v))
                {
                    c[campo] = v;
                }
            }
            if ((r.Analisi.Valori.TryGetValue("Suolo_N_totale_permille", out var n) ? n
                    : r.Analisi.Valori.TryGetValue("Suolo_N_totale_perc", out var p) ? p : (double?)null) is { } azoto)
            {
                c[CampiSuolo.Azoto] = azoto;
            }
            return r with { Campi = c };
        }).ToList();

    private static Func<IReadOnlyList<RigaSuolo>, double?> K(string campo, double fattore = 1) => r => CalcoloAnalisiSuolo.Kpi(r, campo, fattore);

    private static Func<IReadOnlyList<RigaSuolo>, double?> T(Func<TessituraSuoloDto, double?> campo, double fattore = 1) =>
        r => CalcoloAnalisiSuolo.MediaTessitura(r, campo) * fattore;

    /// <summary>Il KPI di Qlik la cui misura comincia con <paramref name="etichetta"/> (spazi ignorati); null = il primo KPI.</summary>
    private static Func<JsonElement, double?> Kpi(string? etichetta = null) => foglio =>
    {
        foreach (var o in foglio.EnumerateObject().Where(o => o.Value.GetProperty("tipo").GetString() == "kpi"))
        {
            foreach (var m in o.Value.GetProperty("misure").EnumerateObject())
            {
                if (etichetta is null || Compatto(m.Name).StartsWith(Compatto(etichetta), StringComparison.OrdinalIgnoreCase))
                {
                    return Numero(m.Value);
                }
            }
        }
        throw new InvalidOperationException($"KPI «{etichetta}» non trovato");
    };

    /// <summary>Il totale <paramref name="i"/> del grafico (non KPI) del foglio: per i combo valore, minimo, massimo.</summary>
    private static Func<JsonElement, double?> Totale(int i) => foglio =>
        foglio.EnumerateObject().First(o => o.Value.GetProperty("tipo").GetString() != "kpi").Value.GetProperty("totali")[i] is var t
            ? Numero(t)
            : null;

    private static string Compatto(string s) => new(s.Where(c => !char.IsWhiteSpace(c)).ToArray());

    private static double? Numero(JsonElement v) => v.GetProperty("num") is { ValueKind: JsonValueKind.Number } n ? n.GetDouble() : null;

    private static Voce P(int foglio, string misura, string campo, double fattore = 1, bool tessitura = false) =>
        new(foglio, misura, Kpi(), K(campo, fattore), tessitura) { Campo = campo };

    /// <summary>
    /// Le barre del grafico di Qlik (Vigna × punto/vigneto) confrontate con quelle di Mappe dello stesso campo: «uguali/di Qlik»,
    /// più quelle che ha solo Qlik o solo Mappe. Dice se una differenza del KPI viene da poche righe diverse.
    /// </summary>
    private static string? Barre(JsonElement foglio, IReadOnlyList<RigaSuolo> righe, string campo)
    {
        var grafico = foglio.EnumerateObject().Select(o => o.Value)
            .FirstOrDefault(o => o.GetProperty("tipo").GetString() == "barchart" && o.GetProperty("n_dimensioni").GetInt32() == 2);
        if (grafico.ValueKind != JsonValueKind.Object)
        {
            return null;
        }
        var qlik = grafico.GetProperty("righe").EnumerateArray()
            .Select(r => (Chiave: (r[0].GetProperty("testo").GetString()!.Trim(), r[1].GetProperty("testo").GetString()!.Trim()), Valore: Numero(r[2])))
            .Where(r => r.Valore is not null).GroupBy(r => r.Chiave).ToDictionary(g => g.Key, g => g.First().Valore);
        var f = CalcoloAnalisiSuolo.Fogli.First(x => x.Tipo == TipoFoglioSuolo.Parametro && x.Campo == campo);
        var mappe = CalcoloAnalisiSuolo.Barre(righe, f).GroupBy(b => (b.Vigna.Trim(), b.Voce.Trim()))
            .ToDictionary(g => g.Key, g => (double?)g.First().Valore);
        if (qlik.Count == 0 && mappe.Count == 0)
        {
            return null;
        }
        var uguali = qlik.Count(q => mappe.TryGetValue(q.Key, out var m) && Uguali(q.Value, m));
        return $"{uguali}/{qlik.Count}" + (qlik.Keys.Except(mappe.Keys).Count() is var sq and > 0 ? $", {sq} solo Qlik" : "")
               + (mappe.Keys.Except(qlik.Keys).Count() is var sm and > 0 ? $", {sm} solo Mappe" : "");
    }

    /// <summary>I numeri di ogni foglio di Qlik (numerazione dei fogli dell'app, come nella verità).</summary>
    private static readonly Voce[] Voci =
    [
        new(1, "Frazione Limosa ‰", Kpi("Frazione Limosa"), T(t => t.Limo, 10), true),
        new(1, "Frazione Sabbiosa ‰", Kpi("Frazione Sabbiosa"), T(t => t.Sabbia, 10), true),
        new(1, "Frazione Argillosa ‰", Kpi("Frazione Argillosa"), T(t => t.Argilla, 10), true),
        P(2, "pH in H₂O", CampiSuolo.PhH2o),
        P(3, "pH in KCL", CampiSuolo.PhKcl),
        P(4, "Carbonati totali ‰", CampiSuolo.Carbonati, 10),
        P(5, "Calcare attivo ‰", CampiSuolo.Calcare, 10),
        P(6, "Conducibilità mS/cm", CampiSuolo.Conducibilita),
        P(7, "Sostanza organica ‰", CampiSuolo.So, 10),
        P(8, "SO su argilla %", CampiSuolo.SoArgilla, tessitura: true),
        P(9, "Azoto N ‰", CampiSuolo.Azoto),
        P(10, "C/N", CampiSuolo.CN),
        P(11, "Coefficiente K2", CampiSuolo.K2, tessitura: true),
        P(12, "CSC cmol/kg", CampiSuolo.Csc),
        new(13, "Composizione CSC: Ca", Totale(0), K(CampiSuolo.CaMeq)),
        new(13, "Composizione CSC: Mg", Totale(1), K(CampiSuolo.MgMeq)),
        new(13, "Composizione CSC: K", Totale(2), K(CampiSuolo.KMeq)),
        new(13, "Composizione CSC: Na", Totale(3), K(CampiSuolo.NaMeq)),
        new(13, "Composizione CSC: acidità", Totale(4), K(CampiSuolo.AciditaMeq)),
        P(14, "Ca % CSC", CampiSuolo.CaCsc),
        P(15, "Ca scambiabile ppm", CampiSuolo.CaPpm),
        P(16, "K % CSC", CampiSuolo.KCsc),
        P(17, "K scambiabile ppm", CampiSuolo.KPpm),
        P(18, "Mg % CSC", CampiSuolo.MgCsc),
        P(19, "Mg scambiabile ppm", CampiSuolo.MgPpm),
        P(20, "Na % CSC", CampiSuolo.NaCsc),
        P(21, "Na scambiabile ppm", CampiSuolo.NaPpm),
        P(22, "Acidità di scambio % CSC", CampiSuolo.AciditaCsc),
        P(23, "Mg/K", CampiSuolo.MgK),
        P(24, "Ca/Mg", CampiSuolo.CaMg),
        P(25, "P Olsen ppm", CampiSuolo.POlsen),
        P(26, "P Bray Kurz ppm", CampiSuolo.PBray),
        P(27, "Cu DTPA ppm", CampiSuolo.CuDtpa),
        P(28, "Fe DTPA ppm", CampiSuolo.FeDtpa),
        P(29, "Mn DTPA ppm", CampiSuolo.MnDtpa),
        P(30, "Zn DTPA ppm", CampiSuolo.ZnDtpa),
        P(31, "Cu EDTA ppm", CampiSuolo.CuEdta),
        // In Qlik il foglio 32 è un secondo Rame EDTA; Mappe al suo posto mostra il Ferro EDTA (decisione 4 del 17/11).
        P(32, "Cu EDTA ppm (Mappe: Fe EDTA)", CampiSuolo.FeEdta),
        P(33, "Mn EDTA ppm", CampiSuolo.MnEdta),
        P(34, "Zn EDTA ppm", CampiSuolo.ZnEdta),
        P(35, "Boro ppm", CampiSuolo.Boro),
        new(36, "SO ‰ (con limiti)", Totale(0), K(CampiSuolo.So, 10)),
        new(36, "SO: limite min", Totale(1), r => CalcoloAnalisiSuolo.Limiti(r, LimitiSuolo.SostanzaOrganica).Minimo, true),
        new(36, "SO: limite max", Totale(2), r => CalcoloAnalisiSuolo.Limiti(r, LimitiSuolo.SostanzaOrganica).Massimo, true),
        new(37, "Azoto ‰ (con limiti)", Totale(0), K(CampiSuolo.Azoto)) { Campo = CampiSuolo.Azoto },
        new(37, "Azoto: limite min", Totale(1), r => CalcoloAnalisiSuolo.Limiti(r, LimitiSuolo.Azoto).Minimo, true),
        new(37, "Azoto: limite max", Totale(2), r => CalcoloAnalisiSuolo.Limiti(r, LimitiSuolo.Azoto).Massimo, true),
        P(38, "Saturazione basica %", CampiSuolo.Saturazione),
        new(39, "Punto di appassimento", Kpi("Punto di App"), T(t => t.PuntoDiAppassimento), true),
        new(39, "Acqua disponibile", Kpi("Acqua Dispon"), T(t => t.AcquaDisponibileMmM), true),
        new(39, "Capacità di campo", Kpi("Capacità di"), T(t => t.CapacitaDiCampo), true),
        new(39, "Conducibilità idraulica", Kpi("Conducibilit"), T(t => t.ConducibilitaIdraulica), true),
        new(39, "Peso specifico", Kpi("Peso Specifi"), T(t => t.PesoSpecifico), true),
        new(40, "Triangolo: limo ‰", Kpi("Frazione Limosa"), T(t => t.Limo, 10), true),
        new(40, "Triangolo: sabbia ‰", Kpi("Frazione Sabbiosa"), T(t => t.Sabbia, 10), true),
        new(40, "Triangolo: argilla ‰", Kpi("Frazione Argillosa"), T(t => t.Argilla, 10), true),
        P(41, "Zolfo ppm", CampiSuolo.Zolfo),
        P(42, "Molibdeno ppm", CampiSuolo.Molibdeno),
        P(43, "Ca idrosolubile ppm", CampiSuolo.CaIdro),
        P(44, "Mg idrosolubile ppm", CampiSuolo.MgIdro),
        P(45, "K idrosolubile ppm", CampiSuolo.KIdro),
        P(46, "Na idrosolubile ppm", CampiSuolo.NaIdro),
        P(47, "Cl idrosolubile ppm", CampiSuolo.ClIdro),
        new(48, "P Olsen ppm (con limiti)", Totale(0), K(CampiSuolo.POlsen)),
        new(48, "P Olsen: limite min", Totale(1), r => CalcoloAnalisiSuolo.Limiti(r, LimitiSuolo.Fosforo).Minimo, true),
        new(48, "P Olsen: limite max", Totale(2), r => CalcoloAnalisiSuolo.Limiti(r, LimitiSuolo.Fosforo).Massimo, true),
        new(49, "P Bray Kurz ppm (con limiti)", Totale(0), K(CampiSuolo.PBray)),
        new(49, "Bray Kurz: limite min", Totale(1), r => CalcoloAnalisiSuolo.Limiti(r, LimitiSuolo.BrayKurz).Minimo),
        new(49, "Bray Kurz: limite max", Totale(2), r => CalcoloAnalisiSuolo.Limiti(r, LimitiSuolo.BrayKurz).Massimo),
    ];

    /// <summary>
    /// Perché un numero di Mappe è diverso da Qlik (null = da capire). Qlik legge gli Excel dei laboratori, Mappe i rilievi 144 del
    /// 4Grapes e le decisioni del 17/11 (<c>regole-dati.md</c>).
    /// </summary>
    public static string? Causa(int foglio, string azienda, string selezione, double? qlik, double? laboratorio, int nonValidi = 0,
        bool usaTessitura = false)
    {
        if (usaTessitura && azienda == "Casa E. Mirafiore" && selezione == "ultimo_anno")
        {
            return "Fonte: i 76 rilievi 2024 di Mirafiore nel 4g non hanno IdTessitura (l'Excel ha sabbia, limo e argilla)";
        }
        if (nonValidi > 0)
        {
            return $"Regola del 17/11: {nonValidi} dati non validi scartati (azoto < 0,1 ‰ → C/N del laboratorio; C/N fuori 1–50 vuoto)";
        }
        if (Uguali(qlik, laboratorio))
        {
            return foglio is 9 or 37
                ? "Decisione 3: azoto `_perc` sotto 0,4 letto in % (× 10); Qlik media l'Excel con le due unità mescolate"
                : "Decisione 2: Mappe calcola sempre la colonna (CO × 1,724, rapporti dei meq…); col valore del laboratorio torna";
        }
        return azienda switch
        {
            "TIKVES - VELES" => "Fonte: l'import TIKVES nel 4g non è l'Excel di Qlik (184 rilievi senza data di prelievo → anno 2023, " +
                                "in Qlik 2023 e 2024; CodCampione vuoti o diversi; pH KCl, conducibilità, Ca e Mn assenti; CSC diversa)",
            "Jidvei" => "Fonte: il 4g ha 90 analisi del 2019 che l'Excel non ha e date di prelievo diverse (vigneti del 2018 di Qlik in " +
                        "un altro anno); P Bray Kurz e saturazione 100% solo nell'Excel, Fe DTPA solo nel 4g per molti punti",
            "Casa E. Mirafiore" when selezione == "tutti_gli_anni" =>
                "Fonte: alcune analisi 2001–2023 hanno righe o valori diversi fra Excel e 4g (il 2024 torna barra per barra)",
            _ => null,
        };
    }

    public static bool Uguali(double? a, double? b) =>
        (a, b) switch
        {
            (null, null) => true,
            ({ } x, { } y) => Math.Abs(x - y) <= Math.Max(1e-4, 1e-3 * Math.Max(Math.Abs(x), Math.Abs(y))),
            _ => false,
        };

    [Fact]
    public void Parita_SuiKpiDiQlik()
    {
        if (!Directory.Exists(CartellaDati) || !File.Exists(Verita))
        {
            return; // servono i dati veri (cartella del lavoro o MAPPE_SUOLO_DATI)
        }

        using var json = JsonDocument.Parse(File.ReadAllText(Verita));
        var confronti = new List<Confronto>();
        var conTessitura = new Dictionary<string, bool>();
        foreach (var (chiave, id) in Aziende)
        {
            var azienda = json.RootElement.GetProperty("aziende").GetProperty(chiave);
            var nome = azienda.GetProperty("nome").GetString()!;
            var dati = Carica(id, nome);
            conTessitura[nome] = dati.Tessiture.Count > 0;
            var righe = CalcoloAnalisiSuolo.Righe(dati);
            foreach (var (sel, chiaveSel) in new[] { ("tutti gli anni", "tutti_gli_anni"), ("ultimo anno", "ultimo_anno") })
            {
                var s = azienda.GetProperty(chiaveSel);
                int? anno = s.TryGetProperty("anno", out var a) ? a.GetInt32() : null;
                var selezione = anno is null ? righe : CalcoloAnalisiSuolo.Filtra(righe, new SelezioneSuolo(Anno: anno));
                var etichetta = anno is null ? sel : $"{sel} ({anno})";
                var fogli = s.GetProperty("slide").EnumerateObject()
                    .ToDictionary(f => int.Parse(f.Name[..f.Name.IndexOf('.')], CultureInfo.InvariantCulture), f => f.Value);
                foreach (var v in Voci)
                {
                    var q = v.Qlik(fogli[v.Foglio]);
                    var m = v.Mappe(selezione);
                    var lab = v.Mappe(ComeLaboratorio(selezione));
                    var causa = Causa(v.Foglio, nome, chiaveSel, q, lab, CalcoloAnalisiSuolo.NonValidi(selezione, v.Campo), v.UsaTessitura);
                    var esito = Uguali(q, m) ? Esito.Uguale
                        : v.UsaTessitura && !conTessitura[nome] ? Esito.SenzaTessitura
                        : causa is not null ? Esito.Voluta : Esito.DaCapire;
                    var barre = v.Campo is null ? null : Barre(fogli[v.Foglio], selezione, v.Campo);
                    confronti.Add(new Confronto(v.Foglio, v.Misura, nome, etichetta, q, m, lab, barre, esito,
                        esito == Esito.Voluta ? causa : null));
                }
            }
        }

        File.WriteAllText(Environment.GetEnvironmentVariable("MAPPE_PARITA_SUOLO_USCITA") is { Length: > 0 } uscita
            ? uscita
            : Path.Combine(Path.GetTempPath(), "parita-qlik-suolo-esito.md"), Tabella(confronti));
        var daCapire = confronti.Where(c => c.Esito == Esito.DaCapire).ToList();
        Assert.True(daCapire.Count == 0,
            $"{daCapire.Count} numeri da capire: " + string.Join("; ", daCapire.Take(10).Select(c => $"{c.Foglio} {c.Misura} {c.Azienda} {c.Selezione}: Qlik {c.Qlik} Mappe {c.Mappe}")));
    }

    private static string Tabella(IReadOnlyList<Confronto> confronti)
    {
        static string N(double? v) => v is { } x ? x.ToString("0.###", CultureInfo.GetCultureInfo("it-IT")) : "-";
        var sb = new StringBuilder("# Parità del Report analisi del suolo con Qlik (task #322)\n\nGenerato da `ParitaAnalisiSuoloTests`. " +
            "Esito per foglio (il peggiore delle sue misure): = uguale, F = voluta per la fonte (Excel ≠ 4g), D = voluta per una decisione " +
            "o regola del 17/11 (anche i dati non validi), T = non verificato (manca la tessitura), ? = da capire.\n\n");
        var colonne = confronti.Select(c => (c.Azienda, c.Selezione)).Distinct().ToList();
        sb.Append("| Foglio | " + string.Join(" | ", colonne.Select(c => $"{c.Azienda} {c.Selezione}")) + " |\n|---|"
                  + string.Concat(colonne.Select(_ => "---|")) + "\n");
        foreach (var f in confronti.GroupBy(c => c.Foglio).OrderBy(g => g.Key))
        {
            sb.Append(CultureInfo.InvariantCulture, $"| {f.Key} {f.First().Misura} | ");
            sb.Append(string.Join(" | ", colonne.Select(col => f.Where(c => (c.Azienda, c.Selezione) == col)
                .Select(c => c.Esito switch
                {
                    Esito.Uguale => 0,
                    Esito.Voluta when c.Causa!.StartsWith("Decisione", StringComparison.Ordinal)
                                      || c.Causa.StartsWith("Regola", StringComparison.Ordinal) => 1,
                    Esito.Voluta => 2,
                    Esito.SenzaTessitura => 3,
                    _ => 4,
                }).Max() switch { 0 => "=", 1 => "D", 2 => "F", 3 => "T", _ => "?" })));
            sb.Append(" |\n");
        }
        sb.Append('\n');
        foreach (var g in confronti.GroupBy(c => c.Esito))
        {
            sb.Append(CultureInfo.InvariantCulture, $"{g.Key}: {g.Count()} numeri. ");
        }
        sb.Append("\n\n## Tutti i numeri\n\n");
        sb.Append(
            "| Foglio | Misura | Azienda | Selezione | Qlik | Mappe | Mappe col laboratorio | Barre uguali | Esito | Causa |\n|---|---|---|---|---|---|---|---|---|---|\n");
        foreach (var c in confronti.OrderBy(c => c.Foglio))
        {
            sb.Append(CultureInfo.InvariantCulture,
                $"| {c.Foglio} | {c.Misura} | {c.Azienda} | {c.Selezione} | {N(c.Qlik)} | {N(c.Mappe)} | {N(c.MappeLaboratorio)} | {c.Barre} | {c.Esito} | {c.Causa} |\n");
        }
        return sb.ToString();
    }

    /// <summary>Le analisi di un'azienda dagli Excel del dba (stesse query di <see cref="FourGrapesLettureAnalisiSuolo"/>).</summary>
    public static AnalisiSuoloAziendaDto Carica(int id, string nome)
    {
        var analisi = Leggi(Path.Combine(CartellaDati, $"azienda-{id}.xlsx")).Select(FourGrapesLettureAnalisiSuolo.Analisi)
            .OfType<AnalisiSuoloDto>().ToList();
        var fileTessitura = new[] { CartellaDati, Path.GetDirectoryName(Path.GetDirectoryName(Verita))! }
            .Select(c => Path.Combine(c, $"tessitura-{id}.xlsx")).FirstOrDefault(File.Exists) ?? "";
        var tessiture = File.Exists(fileTessitura)
            ? Leggi(fileTessitura).Select(FourGrapesLettureAnalisiSuolo.Tessitura).OfType<TessituraSuoloDto>().ToList()
            : [];
        return new AnalisiSuoloAziendaDto(id, nome, analisi, tessiture);
    }

    private static readonly HashSet<string> Interi = ["IdVigneto", "ProfonditaDelCampione", "IdTessitura"];

    /// <summary>Le righe del foglio «Dati» come <see cref="IDataRecord"/> (numeri interi come int, date come DateTime).</summary>
    private static IEnumerable<IDataRecord> Leggi(string file)
    {
        using var wb = new XLWorkbook(file);
        var ws = wb.Worksheet(1);
        var intestazione = ws.Row(1).CellsUsed().Select(c => c.GetString()).ToList();
        var righe = new List<IDataRecord>();
        foreach (var riga in ws.RowsUsed().Skip(1))
        {
            var valori = new object[intestazione.Count];
            for (var i = 0; i < intestazione.Count; i++)
            {
                var v = riga.Cell(i + 1).Value;
                valori[i] = v.IsBlank ? DBNull.Value
                    : v.IsDateTime ? v.GetDateTime()
                    : v.IsNumber && Interi.Contains(intestazione[i]) ? (int)v.GetNumber()
                    : v.IsNumber ? v.GetNumber()
                    : v.IsText ? v.GetText()
                    : v.ToString();
            }
            righe.Add(new Riga(intestazione, valori));
        }
        return righe;
    }

    /// <summary>Una riga dell'Excel letta come un record del database.</summary>
    private sealed class Riga(IReadOnlyList<string> nomi, object[] valori) : IDataRecord
    {
        public object this[int i] => valori[i];
        public object this[string name] => valori[GetOrdinal(name)];
        public int FieldCount => valori.Length;
        public int GetOrdinal(string name) => nomi.ToList().IndexOf(name) is var i and >= 0 ? i : throw new IndexOutOfRangeException(name);
        public string GetName(int i) => nomi[i];
        public object GetValue(int i) => valori[i];
        public bool IsDBNull(int i) => valori[i] is DBNull;
        public Type GetFieldType(int i) => valori[i].GetType();
        public string GetDataTypeName(int i) => GetFieldType(i).Name;
        public int GetValues(object[] values) { Array.Copy(valori, values, Math.Min(values.Length, valori.Length)); return valori.Length; }
        public bool GetBoolean(int i) => (bool)valori[i];
        public byte GetByte(int i) => (byte)valori[i];
        public long GetBytes(int i, long o, byte[]? b, int bo, int l) => throw new NotSupportedException();
        public char GetChar(int i) => (char)valori[i];
        public long GetChars(int i, long o, char[]? b, int bo, int l) => throw new NotSupportedException();
        public IDataReader GetData(int i) => throw new NotSupportedException();
        public DateTime GetDateTime(int i) => (DateTime)valori[i];
        public decimal GetDecimal(int i) => Convert.ToDecimal(valori[i], CultureInfo.InvariantCulture);
        public double GetDouble(int i) => Convert.ToDouble(valori[i], CultureInfo.InvariantCulture);
        public float GetFloat(int i) => (float)GetDouble(i);
        public Guid GetGuid(int i) => (Guid)valori[i];
        public short GetInt16(int i) => (short)valori[i];
        public int GetInt32(int i) => (int)valori[i];
        public long GetInt64(int i) => Convert.ToInt64(valori[i], CultureInfo.InvariantCulture);
        public string GetString(int i) => (string)valori[i];
    }
}

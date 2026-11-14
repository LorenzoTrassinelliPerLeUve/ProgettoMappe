using System.Globalization;
using ProgettoMappe.Contracts;
using ProgettoMappe.Web.Report;
using ProgettoMappe.Web.ReportFineStagione;
using ProgettoMappe.Web.ReportSettimanale;
using ProgettoMappe.Web.Tematismi;
using SerieSettimane = ProgettoMappe.Web.ReportSettimanale.SerieSettimane;

namespace ProgettoMappe.Web.ReportSettimanaleLaore;

/// <summary>
/// Il PowerPoint del report LAORE (task #217): una slide per foglio dell'app Qlik nell'ordine di <see cref="SezioniLaore"/>,
/// sempre tutte anche senza dati, con i numeri di <see cref="CalcoloLaore"/> (gli stessi della pagina).
/// </summary>
public static class PowerPointLaore
{
    private static readonly string[] Colori = ["8DC63E", "3C6FB4", "C0392B", "F8981D", "3C3950", "5F727F", "8E477D", "276E27"];

    /// <summary>
    /// Nessuna automazione Qlik usa l'app LAORE, quindi nessun nome da copiare: come il settimanale IT,
    /// «{AAMMGG} - LAORE {territorio} - Report settimanale» (data di oggi); caratteri non ammessi → «_».
    /// </summary>
    public static string NomeFile(string territorio, DateTimeOffset adesso, string estensione = ".pptx")
    {
        var oggi = TimeZoneInfo.ConvertTime(adesso, TematismiBigot.FusoItalia);
        var nome = $"{oggi.ToString("yyMMdd", CultureInfo.InvariantCulture)} - LAORE {territorio} - Report settimanale";
        var vietati = Path.GetInvalidFileNameChars().Concat(['/', '\\', ':', '*', '?', '"', '<', '>', '|']).ToHashSet();
        return new string(nome.Select(c => vietati.Contains(c) ? '_' : c).ToArray()).Trim() + estensione;
    }

    public static byte[] Crea(DatiLaore d, DateTimeOffset adesso)
    {
        var c = TestiReport.Cultura(LinguaReport.It);
        var oggi = TimeZoneInfo.ConvertTime(adesso, TematismiBigot.FusoItalia).DateTime;
        var s = d.Settimana;
        var sel = d.Settimanali;
        var stagione = d.Stagione;
        var settimane = Enumerable.Range(1, s.Numero).ToList();
        var etichette = settimane.Select(SettimanaQlik.Etichetta).ToList();
        var nota = T("nota.settimana", s.Etichetta(LinguaReport.It));
        var ppt = new PresentazionePptx($"{T("titolo")} - {d.Territorio}",
            $"{T("titolo")} · {d.Territorio} · {s.Etichetta(LinguaReport.It)} · {T("generato.il", oggi.ToString("dd/MM/yyyy", c))}");

        IReadOnlyList<SerieReport> Serie(IReadOnlyList<SerieSettimane> serie) =>
            serie.Select((x, i) => new SerieReport(x.Nome, x.Valori, Colori[i % Colori.Length])).ToList();

        foreach (var chiave in SezioniLaore.Tutte)
        {
            if (CalcoloLaore.Malattie.FirstOrDefault(m => m.Chiave == chiave) is { } malattia)
            {
                ppt.Grafico(T(chiave, s.Anno), nota, T("provincia"),
                    CalcoloLaore.MalattiaPerProvincia(sel, d.Luoghi, malattia).Select(p => new BarraReport(p.Provincia, p.Valore)).ToList(),
                    [], T("vuoto"), formato: "0.0%");
                continue;
            }
            switch (chiave)
            {
                case "sezione.copertina":
                    var (esca, tignole) = CalcoloLaore.Presenze(sel);
                    ppt.Copertina(T("titolo"), d.Territorio,
                    [
                        s.Etichetta(LinguaReport.It), T("cruscotto.al", s.A.ToString("dd/MM", CultureInfo.InvariantCulture)),
                        T("presenze", T(esca ? "si" : "no"), T(tignole ? "si" : "no")),
                        d.Rilievi.Snapshot is { } costruito
                            ? T("dati.snapshot", TimeZoneInfo.ConvertTime(costruito, TematismiBigot.FusoItalia).ToString("g", c))
                            : T("dati.diretti"),
                    ], CalcoloSettimanale.TestiControlli([], s, sel.Count, (k, v) => T(k, v)));
                    break;

                case "sezione.dashboard":
                    var cr = CalcoloSettimanale.Cruscotto(sel, d.VignetiTotali);
                    ppt.Grafico(T(chiave), nota, T("cruscotto.rilievi"),
                        cr.PerTipo.Select(t => new BarraReport(NomeTipo(d.Rilievi, t.Tipo), t.Rilievi)).ToList(),
                        [
                            new(T("cruscotto.rilievi"), cr.Rilievi.ToString("N0", c)),
                            new(T("cruscotto.organi"), cr.Organi.ToString("N0", c)),
                            new(T("cruscotto.monitorati"), cr.Monitorati.ToString(c)),
                            new(T("vigneti.perimetro"), cr.VignetiTotali.ToString(c)),
                        ], T("vuoto"), formato: "0");
                    break;

                case "sezione.bbch-foglie" or "sezione.bbch-grappoli":
                    var codici = chiave == "sezione.bbch-foglie" ? CalcoloLaore.BbchFoglie : CalcoloLaore.BbchGrappoli;
                    ppt.GraficoSerie(T(chiave), T("nota.stagione"), etichette,
                        Serie(CalcoloLaore.BbchPerProvincia(stagione, d.Luoghi, codici, s.Anno, settimane)), true, [], T("nessun.dato"));
                    break;

                case "sezione.bbch-storico-foglie" or "sezione.bbch-storico-grappoli":
                    var storico = CalcoloLaore.BbchStorico(StessaSettimana(d), chiave == "sezione.bbch-storico-foglie"
                        ? CalcoloLaore.BbchFoglie : CalcoloLaore.BbchGrappoli, s.Anno, settimane);
                    ppt.GraficoSerie(T(chiave), $"{T("nota.stagione")} {T("nota.storico")}", etichette,
                    [
                        new(T("bbch.anno"), storico.Anno, "8DC63E"), new(T("bbch.precedente"), storico.Precedente, "3C6FB4"),
                        new(T("bbch.storico"), storico.Storico, "5F727F"),
                    ], true, [], T("nessun.dato"));
                    break;

                case "sezione.cryptoblabes" or "sezione.lobesia":
                    var tipo = chiave == "sezione.cryptoblabes" ? CalcoloLaore.Cryptoblabes : CalcoloLaore.Lobesia;
                    ppt.GraficoSerie(T(chiave), T("nota.stagione"), etichette,
                        Serie(CalcoloLaore.CatturePerProvincia(stagione, d.Luoghi, tipo, settimane)), true, [], T("nessun.dato"));
                    break;

                case "sezione.esca":
                    var totali = CalcoloSettimanale.Esca(sel, d.Rilievi.Esca);
                    var province = CalcoloLaore.EscaPerProvincia(sel, d.Luoghi, d.Rilievi.Esca);
                    var classi = CalcoloSettimanale.ValoriEsca.Where(v => v > 0).ToList();
                    ppt.GraficoSerie(T(chiave, s.Anno), nota, province.Select(p => p.Provincia).ToList(),
                        classi.Select((v, i) => new SerieReport(T("esca." + v),
                            province.Select(p => (double?)p.PerValore.GetValueOrDefault(v)).ToList(), Colori[i % Colori.Length])).ToList(),
                        false,
                        [
                            new(T("esca.rilievi"), totali.Rilievi.ToString(c)), new(T("esca.piante"), totali.Piante.ToString("N0", c)),
                            .. CalcoloSettimanale.ValoriEsca.Select(v => new TesseraReport(T("esca." + v),
                                totali.PerValore.GetValueOrDefault(v).ToString("N0", c))),
                        ], T("vuoto"), formato: "0.0%");
                    break;
            }
        }
        return ppt.Salva();
    }

    /// <summary>
    /// I rilievi degli anni della media storica fino alla stessa settimana dell'anno (la linea dell'anno in corso si ferma
    /// alla settimana scelta, quelle degli anni prima no: in Qlik le settimane sono l'asse).
    /// </summary>
    public static IReadOnlyList<RilievoSettimanaDto> StessaSettimana(DatiLaore d) =>
        d.Rilievi.Rilievi.Where(r => r.Data.Year < d.Settimana.Anno || d.Settimana.Contiene(r)).ToList();

    private static string NomeTipo(RilieviSettimanaDto rilievi, int tipo) =>
        rilievi.NomiTipi.TryGetValue(tipo, out var nome) && nome.Length > 0 ? nome
        : tipo == 0 ? T("rilievi.bbch") : tipo.ToString(CultureInfo.InvariantCulture);

    private static string T(string chiave, params object[] valori) => TestiLaore.Testo(chiave, valori);
}

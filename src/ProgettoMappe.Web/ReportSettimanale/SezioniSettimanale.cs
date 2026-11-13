namespace ProgettoMappe.Web.ReportSettimanale;

/// <summary>
/// Una sezione del report settimanale: chiave del titolo in <see cref="TestiSettimanale"/>, foglio dell'app Qlik, tipi di
/// rilievo che usa (vuoto = non ancora noti), se usa le fasi BBCH, tappa in cui arriva il contenuto.
/// </summary>
public sealed record SezioneSettimanale(string Chiave, string FoglioQlik, IReadOnlyList<int> Tipi, bool Bbch = false, int Tappa = 2);

/// <summary>
/// Le sezioni nell'ordine dei fogli dell'app Qlik «REPORT SETTIMANALE» (rank dei fogli). Fuori: «Filtri» e «Report»
/// (filtri e pulsante dell'automazione), le copie «(1)»/«(2)» e «Il mio nuovo foglio», da rivedere con l'elenco delle
/// slide dell'automazione (automazione.md, task #211). Tipi dalle formule: esca 9, tignole 13 e trappole
/// 148/149/153/155, germogli 113, stato idrico 146; quelli delle sanità e della vigoria arrivano con la struttura.
/// </summary>
public static class SezioniSettimanale
{
    /// <summary>La tappa del lavoro già fatta (le sezioni con tappa più alta si mostrano «in arrivo»).</summary>
    public const int TappaFatta = 1;

    public static readonly IReadOnlyList<SezioneSettimanale> Tutte =
    [
        new("sezione.dashboard", "Dashboard", []),
        new("sezione.fenologia", "Fenologia", [], Bbch: true),
        new("sezione.bbch-vitigno", "Confronto BBCH - Anno in Corso - Vitigno", [], Bbch: true),
        new("sezione.bbch-storico", "Confronto BBCH - Storico", [], Bbch: true),
        new("sezione.sanita-grappoli", "Sanità Grappoli KPI", []),
        new("sezione.sanita-foglia", "Sanità Foglia KPI", []),
        new("sezione.grafico-foglia", "Grafico Foglia", []),
        new("sezione.grafico-grappoli", "Grafico Grappoli", []),
        new("sezione.vigoria", "Vigoria", []),
        new("sezione.germogli", "Lunghezza Germogli", [113]),
        new("sezione.deficit-idrico", "Deficit Idrico", [146]),
        new("sezione.deficit-anni", "Deficit Idrico confronto Anni", [146]),
        new("sezione.tignole", "Tignole", [13, 148, 149, 153, 155]),
        new("sezione.esca", "Mal dell'Esca", [9]),
        new("sezione.considerazioni", "CONSIDERAZIONI", []),
        new("sezione.vigoria-sub", "Vigoria Sub Vigneto", []),
        new("sezione.foto", "Foto", []),
    ];

    /// <summary>I rilievi della settimana che la sezione usa (null se i suoi tipi non sono ancora noti).</summary>
    public static int? Rilievi(SezioneSettimanale sezione, IEnumerable<ProgettoMappe.Contracts.RilievoSettimanaDto> rilievi) =>
        sezione.Tipi.Count == 0 && !sezione.Bbch ? null
        : rilievi.Count(r => sezione.Tipi.Contains(r.TipoRilievo) || (sezione.Bbch && r.Bbch is not null));
}

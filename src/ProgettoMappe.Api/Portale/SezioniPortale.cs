using ProgettoMappe.Infrastructure.Repositories;

namespace ProgettoMappe.Api.Portale;

/// <summary>
/// Chi può aprire una sezione: la stessa logica di <c>AuthorizePortalAttribute</c> del portale.
/// L'utente deve avere l'app abilitata; se la regola chiede dei ruoli basta uno; poi l'abilitazione (una sola,
/// almeno una fra <see cref="AbilitazioniAlmenoUna"/>) oppure l'app indicata in <see cref="OppureIdApp"/>.
/// </summary>
public sealed record RegolaAccesso(
    bool MasterUser = false,
    string? Abilitazione = null,
    IReadOnlyList<string>? AbilitazioniAlmenoUna = null,
    IReadOnlyList<int>? OppureIdApp = null)
{
    public static readonly RegolaAccesso SoloLogin = new();

    public bool Consente(PermessiUtente utente)
    {
        var autorizzato = utente.IsAppEnabled;
        if (MasterUser)
        {
            autorizzato &= utente.IsMasterUser;
        }

        var oppureApp = OppureIdApp is not null && utente.IdApp is { } app && OppureIdApp.Contains(app);
        if (Abilitazione is not null)
        {
            autorizzato &= utente.Ha(Abilitazione) || oppureApp;
        }

        if (AbilitazioniAlmenoUna is not null)
        {
            autorizzato &= AbilitazioniAlmenoUna.Any(utente.Ha) || oppureApp;
        }

        return autorizzato;
    }
}

/// <summary>Una delle 22 sezioni del portale rifatte in Mappe (numeri di docs/specifiche/portale/elenco-sezioni.md).</summary>
/// <param name="Chiave">Pezzo dell'indirizzo: <c>/portale/{Chiave}</c> nel Web, <c>api/portale/{Chiave}/…</c> nell'Api.</param>
/// <param name="Controller">Il controller del portale da cui viene (per ritrovare le regole nei sorgenti).</param>
/// <param name="Tappa">La tappa del piano in cui arriva (docs/specifiche/portale/piano.md §3).</param>
/// <param name="Pronta">false = la voce c'è ma la pagina dice «in arrivo».</param>
public sealed record SezionePortale(
    string Chiave,
    int Numero,
    string Titolo,
    string Gruppo,
    string Descrizione,
    string Controller,
    RegolaAccesso Regola,
    int Tappa,
    bool Pronta = false);

/// <summary>
/// Le 23 sezioni scelte dal proprietario (14/11/2026; la 21 Notifiche aggiunta alle 15), con le regole d'accesso lette nei sorgenti del portale
/// (attributo <c>[AuthorizePortal]</c> sul controller). Dove il menù del portale usa un'abilitazione diversa da
/// quella del controller vale quella del controller, che è quella che decide chi entra:
/// Avversità da trattare (menù AVVERSITA_DA_TRATTARE, controller IsMasterUser), Irroratrici dell'azienda
/// (menù IRRORATRICI_ENTE, controller IRRORATRICI), Asportazioni (nessuna voce nel menù, controller PRODUZIONE).
/// </summary>
public static class SezioniPortale
{
    public const string Difesa = "Difesa e trattamenti";
    public const string DatiCampo = "Dati di campo";
    public const string Nutrizione = "Nutrizione";
    public const string Amministrazione = "Utenti e amministrazione";
    public const string Generale = "Generale";

    /// <summary>App «Sardegna» (<c>App.SARDEGNA</c> del portale): entra nei Trattamenti anche senza abilitazione.</summary>
    public const int AppSardegna = 100;

    private static RegolaAccesso Ab(string abilitazione) => new(Abilitazione: abilitazione);

    public static readonly IReadOnlyList<SezionePortale> Tutte =
    [
        new("home", 27, "Home", Generale, "La pagina dopo l'accesso, con lo smistamento verso le sezioni.",
            "HomeController", RegolaAccesso.SoloLogin, 1, Pronta: true),
        new("indicatori", 3, "Indicatori aziendali", Generale,
            "Probabilità di infezione di peronospora e oidio, pressione e protezione per aziende e comuni.",
            "IndicatoriAziendaliController", Ab("INDICATORI_AZIENDALI"), 1, Pronta: true),

        new("rilievi", 5, "Rilievi (export)", DatiCampo, "Scarica in Excel i rilievi tuoi, condivisi e dell'azienda.",
            "RilieviController", Ab("RILIEVI"), 1, Pronta: true),
        new("produzione", 6, "Dati produzione", DatiCampo, "Rilievi di vendemmia e obiettivo produttivo.",
            "ProduzioneController", Ab("PRODUZIONE"), 2),
        new("fallanze", 7, "Fallanze", DatiCampo, "Piante mancanti, in allevamento, capitozzate.",
            "FallanzeController", Ab("FALLANZE"), 2),
        new("analisi-uve", 8, "Analisi uve", DatiCampo, "Rilievi di maturazione.",
            "AnalisiUveController", Ab("ANALISI_UVE"), 2),
        new("analisi-terreni", 9, "Analisi dei terreni", DatiCampo, "Tessitura e chimica del suolo.",
            "TessituraController", Ab("ANALISI_DEI_TERRENI"), 2),
        new("galleria", 10, "Galleria fotografica", DatiCampo, "Foto dei rilievi per azienda, vigneto e tipo.",
            "GalleriaFotograficaController", Ab("GALLERIA_FOTOGRAFICA"), 1, Pronta: true),

        new("trattamenti", 11, "Trattamenti", Difesa,
            "Registro dei trattamenti, da suggerito a effettivo, calcolo della dose, storico e tabelle.",
            "TrattamentiController",
            new RegolaAccesso(AbilitazioniAlmenoUna: ["TRATTAMENTI", "TRATTAMENTI_AZIENDALI", "TRATTAMENTI_AUTOMATICI"],
                OppureIdApp: [AppSardegna]), 4),
        new("bollettini", 12, "Bollettini trattamenti", Difesa, "Bollettini aziendali giornalieri e invio per email.",
            "BollettiniTrattamentiController", Ab("BOLLETTINI_AZIENDALI"), 4),
        new("linee-difesa", 13, "Linee di difesa", Difesa, "Tabelle delle linee di difesa.",
            "LineeDifesaController", Ab("LINEE_DI_DIFESA"), 2),
        new("soglie-trattamenti", 14, "Soglie trattamenti", Difesa, "Soglie di intervento per avversità.",
            "SoglieTrattamentiController", Ab("SOGLIE_TRATTAMENTI"), 2),
        new("avversita", 15, "Avversità da trattare", Difesa, "Tabella delle avversità.",
            "AvversitaDaTrattareController", new RegolaAccesso(MasterUser: true), 2),
        new("irroratrici", 16, "Irroratrici", Difesa, "Anagrafica delle macchine.",
            "IrroratriciController", new RegolaAccesso(MasterUser: true, Abilitazione: "IRRORATRICI"), 2),
        new("irroratrici-azienda", 17, "Irroratrici dell'azienda", Difesa, "Macchine e regolazioni per azienda.",
            "IrroratriciEnteController", Ab("IRRORATRICI"), 2),

        new("concimazioni", 18, "Concimazioni", Nutrizione,
            "Target enologico, unità e fattori di concimazione, concimazione reale e resoconto.",
            "ConcimazioniController", Ab("CONCIMAZIONI"), 3),
        new("asportazioni", 19, "Asportazioni", Nutrizione, "Asportazioni e fabbisogni di nutrienti per vigneto.",
            "AsportazioniController", Ab("PRODUZIONE"), 1, Pronta: true),

        new("notifiche", 21, "Notifiche", Amministrazione,
            "Notifiche push (Firebase) ed email (SendGrid) a utenti e gruppi.",
            "NotificheController", Ab("INVIA_NOTIFICHE"), 5),
        new("account", 22, "Accesso e password", Amministrazione,
            "Uscita, password dimenticata, cambio utente per l'amministratore.", "AccountController",
            RegolaAccesso.SoloLogin, 0),
        new("utenti", 23, "Utenti", Amministrazione, "Utenti, aziende abbinate, abbonamenti, app abilitata.",
            "UtentiController", Ab("UTENTI"), 5),
        new("abilitazioni", 24, "Abilitazioni", Amministrazione, "Permessi per ruolo.",
            "AbilitazioniController", Ab("ABILITAZIONI"), 5),
        new("gruppi-utenti", 25, "Gruppi di utenti", Amministrazione, "Liste di utenti.",
            "GruppiUtentiController", new RegolaAccesso(MasterUser: true), 5),
        new("impostazioni", 26, "Impostazioni", Amministrazione,
            "Parametri del sistema (il ricalcolo dei modelli di protezione resta sul portale 4Grapes).",
            "ImpostazioniController", Ab("IMPOSTAZIONI"), 5),
    ];

    public static SezionePortale? Trova(string? chiave) =>
        Tutte.FirstOrDefault(s => string.Equals(s.Chiave, chiave, StringComparison.OrdinalIgnoreCase));

    /// <summary>I gruppi nell'ordine del menù.</summary>
    public static readonly IReadOnlyList<string> Gruppi = [Generale, DatiCampo, Difesa, Nutrizione, Amministrazione];
}

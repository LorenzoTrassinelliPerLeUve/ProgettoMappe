using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.FourGrapes;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>
/// Letture dell'Amministrazione del Portale (vedi <see cref="ILettureAmministrazionePortale"/>). Colonne di <c>Utente</c> concesse a
/// ProgettoMappe_ReadOnly (dba, 16/11: tutte tranne CodiceCambioPassword, NuovaPassword, CodiceSicurezza); <c>Impostazioni</c> intera.
/// </summary>
public class FourGrapesLettureAmministrazionePortale : ILettureAmministrazionePortale
{
    /// <summary>I coefficienti della chioma del portale (<c>Impostazioni.GetCoefficientiCanopy</c>).</summary>
    public static readonly IReadOnlyList<string> CoefficientiCanopy = ["OLD", "LWA", "TRV"];

    private readonly FourGrapesDbContext _db;

    public FourGrapesLettureAmministrazionePortale(FourGrapesDbContext db)
    {
        _db = db;
    }

    public Task<IReadOnlyList<UtenteListaDto>> UtentiAsync(CancellationToken ct = default) =>
        LeggiAsync("SELECT idUtente, LoginName, Nome, Cognome, StatoEntita FROM dbo.Utente ORDER BY LoginName", [],
            r => r.Intero("idUtente") is { } id
                ? new UtenteListaDto(id, r.Testo("LoginName"), NomeCompleto(r.Testo("Cognome"), r.Testo("Nome")), r.Intero("StatoEntita") == 1)
                : null, ct);

    public async Task<UtenteSchedaDto?> UtenteAsync(int id, CancellationToken ct = default) =>
        (await LeggiAsync("""
            SELECT idUtente, LoginName, Nome, Cognome, Lingua, Cellulare, IsAppEnabled, IsMasterUser, IsUtentePerleuve, IsGuest,
                   StatoEntita, DataCreazione, DataUltimoAggiornamento
            FROM dbo.Utente WHERE idUtente = @Id
            """, [new SqlParameter("@Id", SqlDbType.Int) { Value = id }],
            r => r.Intero("idUtente") is { } i
                ? new UtenteSchedaDto(i, r.Testo("LoginName"), r.Testo("Nome"), r.Testo("Cognome"), r.Testo("Lingua"), r.Testo("Cellulare"),
                    Vero(r, "IsAppEnabled"), Vero(r, "IsMasterUser"), Vero(r, "IsUtentePerleuve"), Vero(r, "IsGuest"),
                    r.Intero("StatoEntita") == 1, DataOra(r, "DataCreazione"), DataOra(r, "DataUltimoAggiornamento"))
                : null, ct)).FirstOrDefault();

    public async Task<ImpostazioniDto?> ImpostazioniAsync(CancellationToken ct = default) =>
        (await LeggiAsync("""
            SELECT AltezzaMaxRifChioma, LarghezzaMaxRifChioma, CoefficenteCanopy, DataUltimaElaborazioneModelliBBCH,
                   DataUltimaElaborazioneModelliInfezione, DataUltimaElaborazioneModelliProtezione,
                   DataUltimaGenerazioneTrattamentiAutomatici, ElaborazioneModelliProtezioneInCorso
            FROM dbo.Impostazioni WHERE Id = 0
            """, [],
            r => new ImpostazioniDto(r.Decimale("AltezzaMaxRifChioma"), r.Decimale("LarghezzaMaxRifChioma"), r.Testo("CoefficenteCanopy"),
                CoefficientiCanopy, DataOra(r, "DataUltimaElaborazioneModelliBBCH"), DataOra(r, "DataUltimaElaborazioneModelliInfezione"),
                DataOra(r, "DataUltimaElaborazioneModelliProtezione"), DataOra(r, "DataUltimaGenerazioneTrattamentiAutomatici"),
                Vero(r, "ElaborazioneModelliProtezioneInCorso")), ct)).FirstOrDefault();

    public Task<IReadOnlyList<AbilitazioneCategoriaDto>> AbilitazioniCategoriaAsync(CancellationToken ct = default) =>
        LeggiAsync("SELECT Id, SysAdmin, MasterUser, UtentePerLeUve, DSSUser, SoilUser, AppEnabled FROM dbo.Abilitazione ORDER BY Id", [],
            r => r.Testo("Id") is { } id
                ? new AbilitazioneCategoriaDto(id, Vero(r, "SysAdmin"), Vero(r, "MasterUser"), Vero(r, "UtentePerLeUve"), Vero(r, "DSSUser"),
                    Vero(r, "SoilUser"), Vero(r, "AppEnabled"))
                : null, ct);

    public Task<IReadOnlyList<AbilitazionePersonaDto>> AbilitazioniPersonaAsync(int idUtente, DateOnly oggi, CancellationToken ct = default) =>
        LeggiAsync("""
            SELECT a.Id, a.IdAbilitazione, a.IdEnte, e.RagioneSociale, e.NomeCommerciale, a.StatoEntita, a.DataInizio, a.DataFine, a.Note
            FROM dbo.AbilitazioneUtente a LEFT JOIN dbo.Ente e ON e.IdEnte = a.IdEnte
            WHERE a.IdUtente = @IdUtente
            ORDER BY a.IdAbilitazione, e.RagioneSociale
            """, [Intero("@IdUtente", idUtente)],
            r =>
            {
                if (r.Intero("Id") is not { } id || r.Testo("IdAbilitazione") is not { } codice) return null;
                var attiva = r.Intero("StatoEntita") == 1;
                var inizio = r.Data("DataInizio");
                var fine = r.Data("DataFine");
                return new AbilitazionePersonaDto(id, codice.ToUpperInvariant(), r.Intero("IdEnte"),
                    NomeAzienda(r.Testo("NomeCommerciale"), r.Testo("RagioneSociale")), attiva, inizio, fine, r.Testo("Note"),
                    attiva && (inizio is null || inizio <= oggi) && (fine is null || fine >= oggi));
            }, ct);

    public async Task<IReadOnlyList<AziendaPortaleDto>> AziendeUtenteAsync(int idUtente, CancellationToken ct = default) =>
        (await LeggiAsync("""
            SELECT DISTINCT e.IdEnte, e.RagioneSociale, e.NomeCommerciale
            FROM dbo.Utente_Ente ue JOIN dbo.Ente e ON e.IdEnte = ue.Ente_IdEnte
            WHERE ue.Utente_idUtente = @IdUtente AND ue.StatoEntita = 1 AND e.IdEnte > 0
            """, [Intero("@IdUtente", idUtente)],
            r => r.Intero("IdEnte") is { } id
                ? new AziendaPortaleDto(id, NomeAzienda(r.Testo("NomeCommerciale"), r.Testo("RagioneSociale")) ?? $"Azienda {id}")
                : null, ct)).OrderBy(a => a.Nome, StringComparer.CurrentCultureIgnoreCase).ToList();

    public Task<IReadOnlyList<GruppoUtentiDto>> GruppiUtentiAsync(CancellationToken ct = default) =>
        LeggiAsync("""
            SELECT g.Id, g.Descrizione, (SELECT COUNT(*) FROM dbo.ComponenteGruppoUtenti c WHERE c.IdGruppo = g.Id) AS Partecipanti
            FROM dbo.GruppoUtenti g ORDER BY g.Descrizione
            """, [],
            r => r.Intero("Id") is { } id ? new GruppoUtentiDto(id, r.Testo("Descrizione"), r.Intero("Partecipanti") ?? 0) : null, ct);

    public async Task<GruppoUtentiSchedaDto?> GruppoUtentiAsync(int id, CancellationToken ct = default)
    {
        var gruppo = (await LeggiAsync("SELECT Id, Descrizione FROM dbo.GruppoUtenti WHERE Id = @Id", [Intero("@Id", id)],
            r => r.Intero("Id") is { } i ? new GruppoUtentiSchedaDto(i, r.Testo("Descrizione"), []) : null, ct)).FirstOrDefault();
        if (gruppo is null) return null;
        var partecipanti = await LeggiAsync("""
            SELECT u.idUtente, u.Nome, u.Cognome, u.LoginName
            FROM dbo.ComponenteGruppoUtenti c JOIN dbo.Utente u ON u.idUtente = c.IdUtente
            WHERE c.IdGruppo = @Id ORDER BY u.Cognome, u.Nome
            """, [Intero("@Id", id)],
            r => r.Intero("idUtente") is { } u
                ? new PartecipanteGruppoDto(u, NomeCompleto(r.Testo("Cognome"), r.Testo("Nome")), r.Testo("LoginName"))
                : null, ct);
        return gruppo with { Partecipanti = partecipanti };
    }

    public Task<IReadOnlyList<PartecipanteGruppoDto>> CercaUtentiAsync(string testo, CancellationToken ct = default) =>
        LeggiAsync("""
            SELECT TOP 50 idUtente, Nome, Cognome, LoginName FROM dbo.Utente
            WHERE CONCAT(Nome, ' ', Cognome, ' (', LoginName, ')') LIKE @Testo ESCAPE '\'
            ORDER BY Cognome, Nome
            """, [new SqlParameter("@Testo", SqlDbType.NVarChar, 300) { Value = "%" + Escape(testo) + "%" }],
            r => r.Intero("idUtente") is { } u
                ? new PartecipanteGruppoDto(u, NomeCompleto(r.Testo("Cognome"), r.Testo("Nome")), r.Testo("LoginName"))
                : null, ct);

    /// <summary>Il testo cercato dentro un LIKE: i caratteri speciali valgono per sé stessi.</summary>
    public static string Escape(string testo) =>
        testo.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_").Replace("[", @"\[");

    private static string? NomeAzienda(string? commerciale, string? ragioneSociale) =>
        string.IsNullOrWhiteSpace(commerciale) ? ragioneSociale?.Trim() : commerciale.Trim();

    private static SqlParameter Intero(string nome, int valore) => new(nome, SqlDbType.Int) { Value = valore };

    /// <summary>«Cognome Nome» come <c>Utente.NomeCompleto</c> del portale.</summary>
    public static string NomeCompleto(string? cognome, string? nome) => $"{cognome} {nome}".Trim();

    private static bool Vero(IDataRecord r, string colonna) => r.Valore(colonna) is true;

    private static DateTime? DataOra(IDataRecord r, string colonna) => r.Valore(colonna) as DateTime?;

    private async Task<IReadOnlyList<T>> LeggiAsync<T>(string sql, IEnumerable<SqlParameter> parametri, Func<IDataRecord, T?> riga,
        CancellationToken ct)
    {
        var connessione = (SqlConnection)_db.Database.GetDbConnection();
        if (connessione.State != ConnectionState.Open)
        {
            await connessione.OpenAsync(ct);
        }

        await using var comando = connessione.CreateCommand();
        comando.CommandText = sql;
        comando.CommandTimeout = 120;
        foreach (var parametro in parametri)
        {
            comando.Parameters.Add(parametro);
        }

        await using var reader = await comando.ExecuteReaderAsync(ct);
        var risultati = new List<T>();
        while (await reader.ReadAsync(ct))
        {
            if (riga(reader) is { } valore)
            {
                risultati.Add(valore);
            }
        }

        return risultati;
    }
}

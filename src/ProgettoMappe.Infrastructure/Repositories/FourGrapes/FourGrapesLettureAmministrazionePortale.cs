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

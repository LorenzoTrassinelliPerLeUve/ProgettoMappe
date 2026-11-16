using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProgettoMappe.Contracts;
using ProgettoMappe.Infrastructure.FourGrapes;

namespace ProgettoMappe.Infrastructure.Repositories.FourGrapes;

/// <summary>Letture della scheda del vigneto (task #296): una riga di <c>Vigneto</c> e le tendine.</summary>
public class FourGrapesLettureVignetiPortale : ILettureVignetiPortale
{
    /// <summary>Errore SQL «permesso negato» (manca il GRANT).</summary>
    public const int PermessoNegato = 229;

    private static readonly MenuVignetoDto MenuVuoto = new([], [], [], [], [], [], [], [], [], []);

    private readonly FourGrapesDbContext _db;

    public FourGrapesLettureVignetiPortale(FourGrapesDbContext db)
    {
        _db = db;
    }

    public async Task<VignetoSchedaDto?> VignetoAsync(int idVigneto, CancellationToken ct = default)
    {
        var righe = await LeggiAsync("""
            SELECT v.IdVigneto, g.Ente_IdEnte AS IdEnte, e.RagioneSociale, g.Vigna, v.NomeVigneto, v.Vitigno_idVitigno, v.AnnoImpianto,
                   v.SullaFila, v.TraFila, v.Estirpo, v.DataEstirpo, v.AnnoEstirpo, v.Carico, v.IdTipologiaLinea, v.IdRischio,
                   v.ElaboraMeteo, v.PesoMedioGrappolo, v.Note, v.Irrigazione_idIrrigazione, v.Giacitura_idGiacitura,
                   v.FormaAllevamento_idFormaAllevamento, v.Portainnesto_idPortainnesto, v.Clone_IdClone, v.Conduzione_idConduzione,
                   v.Esposizione, v.QuotaSLM, v.Pendenza, v.Orientamento, v.NumeroPianteContate, v.SuperficieDichiarata,
                   v.SuperficieMisurata, v.Latitudine, v.Longitudine, v.DataAggiornamentoStato,
                   m.AdministrativeAreaLevel1 AS Regione, m.AdministrativeAreaLevel2 AS Provincia, m.AdministrativeAreaLevel3 AS Comune
            FROM dbo.Vigneto v
            JOIN dbo.Vigna g ON g.IdVigna = v.Vigna_IdVigna
            JOIN dbo.Ente e ON e.IdEnte = g.Ente_IdEnte
            LEFT JOIN dbo.VignetoMaps m ON m.IdVigneto = v.IdVigneto
            WHERE v.IdVigneto = @Id AND v.IdVigneto > 0
            """, [new SqlParameter("@Id", SqlDbType.Int) { Value = idVigneto }], Scheda, ct);
        return righe.Count == 0 ? null : righe[0];
    }

    private static VignetoSchedaDto? Scheda(IDataRecord r)
    {
        if (r.Intero("IdVigneto") is not { } id || r.Intero("IdEnte") is not { } ente)
        {
            return null;
        }

        var dati = new VignetoSalvaDto(
            r.Testo("NomeVigneto"), r.Intero("Vitigno_idVitigno"), r.Intero("AnnoImpianto"), r.Decimale("SullaFila"),
            r.Decimale("TraFila"), Vero(r, "Estirpo"), DataOra(r, "DataEstirpo"), Vero(r, "Carico"), r.Intero("IdTipologiaLinea"),
            r.Intero("IdRischio"), Vero(r, "ElaboraMeteo"), r.Intero("PesoMedioGrappolo"), r.Testo("Note"),
            r.Intero("Irrigazione_idIrrigazione"), r.Intero("Giacitura_idGiacitura"), r.Intero("FormaAllevamento_idFormaAllevamento"),
            r.Testo("Portainnesto_idPortainnesto"), r.Testo("Clone_IdClone"), r.Intero("Conduzione_idConduzione"), r.Testo("Esposizione"),
            r.Intero("QuotaSLM"), r.Intero("Pendenza"), r.Intero("Orientamento"), r.Intero("NumeroPianteContate"),
            r.Decimale("SuperficieDichiarata"), r.Decimale("SuperficieMisurata"));
        return new VignetoSchedaDto(id, ente, r.Testo("RagioneSociale") ?? $"Azienda {ente}", r.Testo("Vigna"), dati,
            r.Decimale("Latitudine"), r.Decimale("Longitudine"), r.Testo("Regione"), r.Testo("Provincia"), r.Testo("Comune"),
            r.Intero("AnnoEstirpo"), DataOra(r, "DataAggiornamentoStato"), false, false, false, MenuVuoto);
    }

    private static bool Vero(IDataRecord r, string colonna) => r.Valore(colonna) is true;

    private static DateTime? DataOra(IDataRecord r, string colonna) => r.Valore(colonna) is DateTime d ? d : null;

    public async Task<MenuVignetoDto> MenuAsync(CancellationToken ct = default)
    {
        var nonLeggibili = new List<string>();

        async Task<IReadOnlyList<VoceVignetoDto>> Voci(string tabella, string sql, string id, string testo, string? vitigno = null)
        {
            try
            {
                return await LeggiAsync(sql, [], r => r.Valore(id) is { } valore
                    ? new VoceVignetoDto(Convert.ToString(valore, CultureInfo.InvariantCulture)!.Trim(),
                        r.Testo(testo) ?? Convert.ToString(valore, CultureInfo.InvariantCulture)!,
                        vitigno is null ? null : r.Intero(vitigno))
                    : null, ct);
            }
            catch (SqlException ex) when (ex.Number == PermessoNegato)
            {
                nonLeggibili.Add(tabella);
                return [];
            }
        }

        return new MenuVignetoDto(
            await Voci("Vitigno", "SELECT idVitigno, Vitigno FROM dbo.Vitigno ORDER BY Vitigno", "idVitigno", "Vitigno"),
            await Voci("Irrigazione", "SELECT idIrrigazione, Irrigazione FROM dbo.Irrigazione ORDER BY Irrigazione", "idIrrigazione", "Irrigazione"),
            await Voci("Giacitura", "SELECT idGiacitura, Giacitura FROM dbo.Giacitura ORDER BY Giacitura", "idGiacitura", "Giacitura"),
            await Voci("FormaAllevamento", "SELECT idFormaAllevamento, FormaAllevamento FROM dbo.FormaAllevamento ORDER BY FormaAllevamento",
                "idFormaAllevamento", "FormaAllevamento"),
            await Voci("Portainnesto", "SELECT idPortainnesto, Portainnesto FROM dbo.Portainnesto ORDER BY Portainnesto", "idPortainnesto", "Portainnesto"),
            await Voci("Clone", "SELECT IdClone, Clone, Vitigno_idVitigno FROM dbo.Clone ORDER BY Clone", "IdClone", "Clone", "Vitigno_idVitigno"),
            await Voci("Conduzione", "SELECT idConduzione, Conduzione FROM dbo.Conduzione ORDER BY Conduzione", "idConduzione", "Conduzione"),
            await Voci("TipologiaLinea", "SELECT Id, Descrizione FROM dbo.TipologiaLinea ORDER BY Descrizione", "Id", "Descrizione"),
            await Voci("Rischio", "SELECT Id, Descrizione FROM dbo.Rischio ORDER BY Descrizione", "Id", "Descrizione"),
            nonLeggibili);
    }

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

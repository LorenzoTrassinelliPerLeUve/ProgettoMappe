using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace ProgettoMappe.Infrastructure.Scrittura;

/// <summary>
/// Una tabella di 4Grapes in cui il Portale può scrivere. Solo quelle elencate in
/// <see cref="TabelleScrivibili"/>: i nomi di tabella e colonne finiscono nel testo SQL, i valori mai (sempre parametri).
/// </summary>
/// <param name="Nome">Nome della tabella (schema dbo), anche quello che va in <c>LogOp.Tabella</c>.</param>
/// <param name="Chiave">Colonna chiave primaria.</param>
/// <param name="ChiaveIdentity">true = la chiave la genera SQL Server (si legge con SCOPE_IDENTITY()).</param>
/// <param name="Chiave2">Seconda colonna della chiave primaria, se è doppia (es. <c>UnitaBaseConcimazione</c>: Anno + IdVigneto):
/// allora la chiave si passa come <see cref="ChiaveDoppia"/>.</param>
/// <param name="SenzaLogOp">Righe figlie che il portale salva senza la loro riga di LogOp (<c>SaveAndLog</c> registra solo la
/// testata): es. <c>TrattamentoAvversita</c>.</param>
/// <param name="FuoriDalLog">Colonne che non vanno nel Json di LogOp (password e codici segreti di <c>Utente</c>).</param>
public sealed record TabellaScrivibile(string Nome, string Chiave, bool ChiaveIdentity, string? Chiave2 = null, bool SenzaLogOp = false,
    IReadOnlyList<string>? FuoriDalLog = null)
{
    private static readonly Regex Identificatore = new("^[A-Za-z_][A-Za-z0-9_]{0,127}$", RegexOptions.CultureInvariant);

    public static bool NomeValido(string nome) => Identificatore.IsMatch(nome);

    /// <summary>La riga come va nel Json di LogOp: senza le colonne <see cref="FuoriDalLog"/>.</summary>
    public IReadOnlyList<KeyValuePair<string, object?>> PerLog(IReadOnlyList<KeyValuePair<string, object?>> riga) =>
        FuoriDalLog is { Count: > 0 } fuori
            ? riga.Where(c => !fuori.Contains(c.Key, StringComparer.OrdinalIgnoreCase)).ToList()
            : riga;

    internal void Valida()
    {
        if (!NomeValido(Nome) || !NomeValido(Chiave) || Chiave2 is not null && !NomeValido(Chiave2))
        {
            throw new ArgumentException($"Nome di tabella o chiave non valido: {Nome}.{Chiave}");
        }
    }

    /// <summary>
    /// «[Chiave] = @chiave» (o le due colonne della chiave doppia) con i suoi parametri; <paramref name="diversa"/> = la riga
    /// esclusa dei conteggi.
    /// </summary>
    internal (string Condizione, List<SqlParameter> Parametri) Dove(object chiave, string nome = "@chiave", bool diversa = false)
    {
        if (Chiave2 is null)
        {
            if (chiave is ChiaveDoppia) throw new ArgumentException($"{Nome} ha una chiave sola");
            return ($"[{Chiave}] {(diversa ? "<>" : "=")} {nome}", [ComandiScrittura.Parametro(nome, chiave)]);
        }

        if (chiave is not ChiaveDoppia doppia) throw new ArgumentException($"{Nome} ha una chiave doppia ({Chiave}, {Chiave2})");
        var condizione = $"[{Chiave}] = {nome} AND [{Chiave2}] = {nome}2";
        return (diversa ? $"NOT ({condizione})" : condizione,
            [ComandiScrittura.Parametro(nome, doppia.Prima), ComandiScrittura.Parametro(nome + "2", doppia.Seconda)]);
    }

    /// <summary>La chiave della riga presa dai suoi valori (tabelle senza identity).</summary>
    public object ChiaveDa(IReadOnlyDictionary<string, object?> valori)
    {
        object Valore(string colonna) => valori.TryGetValue(colonna, out var c) && c is not null
            ? c
            : throw new ArgumentException($"Manca la chiave {colonna}");
        return Chiave2 is null ? Valore(Chiave) : new ChiaveDoppia(Valore(Chiave), Valore(Chiave2));
    }
}

/// <summary>Chiave primaria su due colonne, nell'ordine di <see cref="TabellaScrivibile.Chiave"/> e <see cref="TabellaScrivibile.Chiave2"/>.</summary>
public sealed record ChiaveDoppia(object Prima, object Seconda);

/// <summary>Un comando SQL già costruito: testo e parametri. Puro, testato senza database.</summary>
public sealed record ComandoSql(string Testo, IReadOnlyList<SqlParameter> Parametri);

/// <summary>
/// Costruisce i comandi di scrittura. Le date vanno sempre come <c>datetime2</c> (mai <c>datetime</c>, che
/// arrotonda: in 4Grapes <c>Rilievo.IdRilievo</c> è un datetime2 usato come chiave), i testi come nvarchar.
/// </summary>
public static class ComandiScrittura
{
    public static ComandoSql Inserisci(TabellaScrivibile tabella, IReadOnlyDictionary<string, object?> valori)
    {
        tabella.Valida();
        if (valori.Count == 0)
        {
            throw new ArgumentException("Nessun valore da inserire");
        }

        var nomi = Colonne(valori.Keys);
        var parametri = nomi.Select((c, i) => Parametro($"@v{i}", valori[c])).ToList();
        var testo = $"INSERT INTO dbo.[{tabella.Nome}] ({string.Join(", ", nomi.Select(c => $"[{c}]"))}) " +
                    $"VALUES ({string.Join(", ", parametri.Select(p => p.ParameterName))});";
        if (tabella.ChiaveIdentity)
        {
            testo += " SELECT CAST(SCOPE_IDENTITY() AS int);";
        }

        return new ComandoSql(testo, parametri);
    }

    /// <summary>
    /// UPDATE della riga con quella chiave. <paramref name="attesi"/> (facoltativo) sono i valori letti prima:
    /// se nel frattempo qualcuno li ha cambiati, la riga non si aggiorna (0 righe → conflitto).
    /// Il comando restituisce il numero di righe aggiornate.
    /// </summary>
    public static ComandoSql Aggiorna(TabellaScrivibile tabella, object chiave, IReadOnlyDictionary<string, object?> valori,
        IReadOnlyDictionary<string, object?>? attesi = null)
    {
        tabella.Valida();
        if (valori.Count == 0)
        {
            throw new ArgumentException("Nessun valore da aggiornare");
        }

        var nomi = Colonne(valori.Keys);
        if (nomi.Contains(tabella.Chiave, StringComparer.OrdinalIgnoreCase)
            || tabella.Chiave2 is not null && nomi.Contains(tabella.Chiave2, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("La chiave non si modifica");
        }

        var parametri = nomi.Select((c, i) => Parametro($"@v{i}", valori[c])).ToList();
        var set = string.Join(", ", nomi.Select((c, i) => $"[{c}] = @v{i}"));
        var (dove, parametriChiave) = tabella.Dove(chiave);
        var condizioni = new List<string> { dove };
        parametri.AddRange(parametriChiave);
        if (attesi is not null)
        {
            var i = 0;
            foreach (var c in Colonne(attesi.Keys))
            {
                if (attesi[c] is null)
                {
                    condizioni.Add($"[{c}] IS NULL");
                }
                else
                {
                    condizioni.Add($"[{c}] = @a{i}");
                    parametri.Add(Parametro($"@a{i}", attesi[c]));
                    i++;
                }
            }
        }

        var testo = $"UPDATE dbo.[{tabella.Nome}] SET {set} WHERE {string.Join(" AND ", condizioni)}; SELECT @@ROWCOUNT;";
        return new ComandoSql(testo, parametri);
    }

    public static ComandoSql Elimina(TabellaScrivibile tabella, object chiave)
    {
        tabella.Valida();
        var (dove, parametri) = tabella.Dove(chiave);
        return new ComandoSql($"DELETE FROM dbo.[{tabella.Nome}] WHERE {dove}; SELECT @@ROWCOUNT;", parametri);
    }

    /// <summary>
    /// <c>dbo.EliminaRilievoCompleto</c> (dba, task #256): foto, conte, posizione e rilievo in una transazione, con la sua riga
    /// di LogOp; controlla da sola che l'utente possa cancellare. Restituisce una riga con i conteggi.
    /// </summary>
    public static ComandoSql EliminaRilievoCompleto(DateTime idRilievo, int idUtente) =>
        new("EXEC dbo.EliminaRilievoCompleto @IdRilievo = @IdRilievo, @IdUtente = @IdUtente;",
            [new SqlParameter("@IdRilievo", SqlDbType.DateTime2) { Value = idRilievo }, Parametro("@IdUtente", idUtente)]);

    /// <summary>
    /// <c>SELECT COUNT(*)</c> delle righe con quei valori (null = IS NULL, come il confronto di EF6 fra nullable), senza la
    /// riga con chiave <paramref name="esclusa"/>. Per i controlli «già esistente» del portale.
    /// </summary>
    public static ComandoSql Conta(TabellaScrivibile tabella, IReadOnlyDictionary<string, object?> uguali, object? esclusa = null)
    {
        tabella.Valida();
        var condizioni = new List<string>();
        var parametri = new List<SqlParameter>();
        var i = 0;
        foreach (var c in Colonne(uguali.Keys))
        {
            if (uguali[c] is null)
            {
                condizioni.Add($"[{c}] IS NULL");
            }
            else
            {
                condizioni.Add($"[{c}] = @u{i}");
                parametri.Add(Parametro($"@u{i}", uguali[c]));
                i++;
            }
        }

        if (esclusa is not null)
        {
            var (diversa, parametriChiave) = tabella.Dove(esclusa, "@esclusa", diversa: true);
            condizioni.Add(diversa);
            parametri.AddRange(parametriChiave);
        }

        var dove = condizioni.Count == 0 ? "" : " WHERE " + string.Join(" AND ", condizioni);
        return new ComandoSql($"SELECT COUNT(*) FROM dbo.[{tabella.Nome}]{dove};", parametri);
    }

    /// <summary>La riga intera (tutte le colonne, per il Json di LogOp), letta dentro la stessa transazione.</summary>
    public static ComandoSql Leggi(TabellaScrivibile tabella, object chiave)
    {
        tabella.Valida();
        var (dove, parametri) = tabella.Dove(chiave);
        return new ComandoSql($"SELECT * FROM dbo.[{tabella.Nome}] WHERE {dove};", parametri);
    }

    /// <summary>La riga di <c>dbo.LogOp</c> (schema dal dba, 2026-11-14: Id identity, nessun default né trigger).</summary>
    public static ComandoSql InserisciLogOp(RigaLogOp riga) => new(
        "INSERT INTO dbo.LogOp (IdUtente, Data, Operazione, Tabella, IdRiga, Json) " +
        "VALUES (@IdUtente, @Data, @Operazione, @Tabella, @IdRiga, @Json); SELECT CAST(SCOPE_IDENTITY() AS int);",
        [
            new SqlParameter("@IdUtente", SqlDbType.Int) { Value = riga.IdUtente },
            new SqlParameter("@Data", SqlDbType.DateTime2) { Value = riga.Data },
            new SqlParameter("@Operazione", SqlDbType.NVarChar, 255) { Value = riga.Operazione },
            new SqlParameter("@Tabella", SqlDbType.NVarChar, 255) { Value = riga.Tabella },
            new SqlParameter("@IdRiga", SqlDbType.Int) { Value = riga.IdRiga },
            new SqlParameter("@Json", SqlDbType.NVarChar, -1) { Value = riga.Json },
        ]);

    private static List<string> Colonne(IEnumerable<string> nomi)
    {
        var elenco = nomi.ToList();
        foreach (var nome in elenco)
        {
            if (!TabellaScrivibile.NomeValido(nome))
            {
                throw new ArgumentException($"Nome di colonna non valido: {nome}");
            }
        }

        return elenco;
    }

    public static SqlParameter Parametro(string nome, object? valore) => valore switch
    {
        null => new SqlParameter(nome, DBNull.Value),
        DateTime d => new SqlParameter(nome, SqlDbType.DateTime2) { Value = d },
        DateTimeOffset d => new SqlParameter(nome, SqlDbType.DateTimeOffset) { Value = d },
        DateOnly d => new SqlParameter(nome, SqlDbType.Date) { Value = d.ToDateTime(TimeOnly.MinValue) },
        string s => new SqlParameter(nome, SqlDbType.NVarChar, s.Length <= 4000 ? 4000 : -1) { Value = s },
        bool b => new SqlParameter(nome, SqlDbType.Bit) { Value = b },
        int n => new SqlParameter(nome, SqlDbType.Int) { Value = n },
        long n => new SqlParameter(nome, SqlDbType.BigInt) { Value = n },
        decimal n => new SqlParameter(nome, SqlDbType.Decimal) { Value = n },
        double n => new SqlParameter(nome, SqlDbType.Float) { Value = n },
        _ => new SqlParameter(nome, valore),
    };
}

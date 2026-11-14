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
public sealed record TabellaScrivibile(string Nome, string Chiave, bool ChiaveIdentity)
{
    private static readonly Regex Identificatore = new("^[A-Za-z_][A-Za-z0-9_]{0,127}$", RegexOptions.CultureInvariant);

    public static bool NomeValido(string nome) => Identificatore.IsMatch(nome);

    internal void Valida()
    {
        if (!NomeValido(Nome) || !NomeValido(Chiave))
        {
            throw new ArgumentException($"Nome di tabella o chiave non valido: {Nome}.{Chiave}");
        }
    }
}

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
        if (nomi.Contains(tabella.Chiave, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("La chiave non si modifica");
        }

        var parametri = nomi.Select((c, i) => Parametro($"@v{i}", valori[c])).ToList();
        var set = string.Join(", ", nomi.Select((c, i) => $"[{c}] = @v{i}"));
        var condizioni = new List<string> { $"[{tabella.Chiave}] = @chiave" };
        parametri.Add(Parametro("@chiave", chiave));
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
        return new ComandoSql($"DELETE FROM dbo.[{tabella.Nome}] WHERE [{tabella.Chiave}] = @chiave; SELECT @@ROWCOUNT;",
            [Parametro("@chiave", chiave)]);
    }

    /// <summary>La riga intera (tutte le colonne, per il Json di LogOp), letta dentro la stessa transazione.</summary>
    public static ComandoSql Leggi(TabellaScrivibile tabella, object chiave)
    {
        tabella.Valida();
        return new ComandoSql($"SELECT * FROM dbo.[{tabella.Nome}] WHERE [{tabella.Chiave}] = @chiave;",
            [Parametro("@chiave", chiave)]);
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

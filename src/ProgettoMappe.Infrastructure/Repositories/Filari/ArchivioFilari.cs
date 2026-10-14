using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.Repositories.Ndvi;

namespace ProgettoMappe.Infrastructure.Repositories.Filari;

/// <summary>Sezione <c>Disegni</c> dell'Api: dove sta il database SQLite dei filari disegnati.</summary>
public sealed class DisegniOptions
{
    public const string Sezione = "Disegni";

    /// <summary>Percorso del file; <c>~</c> all'inizio è la cartella dell'utente che fa girare l'Api.</summary>
    public string Database { get; set; } = "~/progetti/mappe/dati/filari.db";
}

/// <summary>
/// Archivio dei filari disegnati (task #95): un database SQLite di Mappe, **non** 4Grapes (che resta in
/// sola lettura). Ogni salvataggio è una nuova versione per vigneto; la più alta è quella valida, le
/// altre restano per lo storico. Il file e la tabella nascono al primo salvataggio; finché non c'è il
/// file le letture rispondono «nessun filare», senza errore. Le letture aprono il file in sola lettura.
/// </summary>
public sealed class ArchivioFilari
{
    private const string Colonne =
        "id_vigneto, versione, geometria, interfila_m, azimut_gradi, scostamento_m, n_filari, lunghezza_m, autore, autore_nome, creato_il, note";

    private readonly string _percorso;
    private readonly SemaphoreSlim _scrittura = new(1, 1);

    public ArchivioFilari(IOptions<DisegniOptions> opzioni)
    {
        _percorso = LettoreNdvi.Espandi(opzioni.Value.Database);
    }

    public string Percorso => _percorso;

    /// <summary>L'ultima versione di ognuno dei vigneti indicati che ne ha una.</summary>
    public async Task<IReadOnlyList<VersioneFilari>> UltimeAsync(IReadOnlyCollection<int> idVigneti, CancellationToken ct)
    {
        var vigneti = idVigneti.Distinct().ToList();
        if (vigneti.Count == 0)
        {
            return [];
        }

        var risultato = new List<VersioneFilari>();
        foreach (var blocco in vigneti.Chunk(500))
        {
            risultato.AddRange(await LeggiAsync(comando =>
            {
                var nomi = blocco.Select((id, i) =>
                {
                    var nome = "$v" + i.ToString(CultureInfo.InvariantCulture);
                    comando.Parameters.AddWithValue(nome, id);
                    return nome;
                });
                comando.CommandText = $"""
                    SELECT {Colonne} FROM filari f
                    WHERE f.id_vigneto IN ({string.Join(", ", nomi)})
                      AND f.versione = (SELECT MAX(versione) FROM filari u WHERE u.id_vigneto = f.id_vigneto)
                    """;
            }, ct));
        }

        return risultato;
    }

    /// <summary>Tutte le versioni del vigneto, dalla più recente.</summary>
    public Task<IReadOnlyList<VersioneFilari>> StoricoAsync(int idVigneto, CancellationToken ct) =>
        LeggiAsync(comando =>
        {
            comando.CommandText = $"SELECT {Colonne} FROM filari WHERE id_vigneto = $id ORDER BY versione DESC";
            comando.Parameters.AddWithValue("$id", idVigneto);
        }, ct);

    /// <summary>Una versione del vigneto, o null se non c'è.</summary>
    public async Task<VersioneFilari?> VersioneAsync(int idVigneto, int versione, CancellationToken ct) =>
        (await LeggiAsync(comando =>
        {
            comando.CommandText = $"SELECT {Colonne} FROM filari WHERE id_vigneto = $id AND versione = $versione";
            comando.Parameters.AddWithValue("$id", idVigneto);
            comando.Parameters.AddWithValue("$versione", versione);
        }, ct)).FirstOrDefault();

    /// <summary>
    /// Scrive una nuova versione (numero = ultima + 1; quello di <paramref name="filari"/> si ignora) e la
    /// restituisce. Crea cartella, file e tabella se mancano; se il percorso non è scrivibile l'eccezione sale.
    /// </summary>
    public async Task<VersioneFilari> SalvaAsync(VersioneFilari filari, CancellationToken ct)
    {
        await _scrittura.WaitAsync(ct);
        try
        {
            if (Path.GetDirectoryName(_percorso) is { Length: > 0 } cartella)
            {
                Directory.CreateDirectory(cartella);
            }

            var stringa = new SqliteConnectionStringBuilder { DataSource = _percorso, Mode = SqliteOpenMode.ReadWriteCreate, Pooling = false };
            await using var connessione = new SqliteConnection(stringa.ToString());
            await connessione.OpenAsync(ct);

            await using (var schema = connessione.CreateCommand())
            {
                schema.CommandText = """
                    CREATE TABLE IF NOT EXISTS filari (
                        id_vigneto    INTEGER NOT NULL,
                        versione      INTEGER NOT NULL,
                        geometria     TEXT    NOT NULL,
                        interfila_m   REAL    NOT NULL,
                        azimut_gradi  REAL,
                        scostamento_m REAL    NOT NULL,
                        n_filari      INTEGER NOT NULL,
                        lunghezza_m   REAL    NOT NULL,
                        autore        TEXT    NOT NULL,
                        autore_nome   TEXT,
                        creato_il     TEXT    NOT NULL,
                        note          TEXT,
                        PRIMARY KEY (id_vigneto, versione)
                    )
                    """;
                await schema.ExecuteNonQueryAsync(ct);
            }

            await using var transazione = (SqliteTransaction)await connessione.BeginTransactionAsync(ct);
            int versione;
            await using (var massimo = connessione.CreateCommand())
            {
                massimo.Transaction = transazione;
                massimo.CommandText = "SELECT COALESCE(MAX(versione), 0) + 1 FROM filari WHERE id_vigneto = $id";
                massimo.Parameters.AddWithValue("$id", filari.IdVigneto);
                versione = Convert.ToInt32(await massimo.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
            }

            var salvata = filari with { Versione = versione };
            await using (var inserisci = connessione.CreateCommand())
            {
                inserisci.Transaction = transazione;
                inserisci.CommandText = $"""
                    INSERT INTO filari ({Colonne})
                    VALUES ($id, $versione, $geometria, $interfila, $azimut, $scostamento, $n, $lunghezza, $autore, $autoreNome, $creato, $note)
                    """;
                inserisci.Parameters.AddWithValue("$id", salvata.IdVigneto);
                inserisci.Parameters.AddWithValue("$versione", salvata.Versione);
                inserisci.Parameters.AddWithValue("$geometria", salvata.GeometriaWkt);
                inserisci.Parameters.AddWithValue("$interfila", salvata.InterfilaM);
                inserisci.Parameters.AddWithValue("$azimut", (object?)salvata.AzimutGradi ?? DBNull.Value);
                inserisci.Parameters.AddWithValue("$scostamento", salvata.ScostamentoM);
                inserisci.Parameters.AddWithValue("$n", salvata.NumFilari);
                inserisci.Parameters.AddWithValue("$lunghezza", salvata.LunghezzaM);
                inserisci.Parameters.AddWithValue("$autore", salvata.Autore);
                inserisci.Parameters.AddWithValue("$autoreNome", (object?)salvata.AutoreNome ?? DBNull.Value);
                inserisci.Parameters.AddWithValue("$creato", salvata.CreatoIl.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
                inserisci.Parameters.AddWithValue("$note", (object?)salvata.Note ?? DBNull.Value);
                await inserisci.ExecuteNonQueryAsync(ct);
            }

            await transazione.CommitAsync(ct);
            return salvata;
        }
        finally
        {
            _scrittura.Release();
        }
    }

    private async Task<IReadOnlyList<VersioneFilari>> LeggiAsync(Action<SqliteCommand> prepara, CancellationToken ct)
    {
        if (!File.Exists(_percorso))
        {
            return [];
        }

        var stringa = new SqliteConnectionStringBuilder { DataSource = _percorso, Mode = SqliteOpenMode.ReadOnly, Pooling = false };
        await using var connessione = new SqliteConnection(stringa.ToString());
        await connessione.OpenAsync(ct);

        await using (var esiste = connessione.CreateCommand())
        {
            esiste.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'filari'";
            if (await esiste.ExecuteScalarAsync(ct) is null)
            {
                return [];
            }
        }

        await using var comando = connessione.CreateCommand();
        prepara(comando);
        var risultato = new List<VersioneFilari>();
        await using var lettore = await comando.ExecuteReaderAsync(ct);
        while (await lettore.ReadAsync(ct))
        {
            risultato.Add(new VersioneFilari(
                lettore.GetInt32(0),
                lettore.GetInt32(1),
                lettore.GetString(2),
                lettore.GetDouble(3),
                lettore.IsDBNull(4) ? null : lettore.GetDouble(4),
                lettore.GetDouble(5),
                lettore.GetInt32(6),
                lettore.GetDouble(7),
                lettore.GetString(8),
                lettore.IsDBNull(9) ? null : lettore.GetString(9),
                DateTime.Parse(lettore.GetString(10), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
                lettore.IsDBNull(11) ? null : lettore.GetString(11)));
        }

        return risultato;
    }
}

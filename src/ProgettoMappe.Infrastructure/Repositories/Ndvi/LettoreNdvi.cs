using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using ProgettoMappe.Domain.Entities;

namespace ProgettoMappe.Infrastructure.Repositories.Ndvi;

/// <summary>Sezione <c>Ndvi</c> dell'Api: dove sta il database SQLite scritto dal programma notturno.</summary>
public sealed class NdviOptions
{
    public const string Sezione = "Ndvi";

    /// <summary>Percorso del file; <c>~</c> all'inizio è la cartella dell'utente che fa girare l'Api.</summary>
    public string Database { get; set; } = "~/progetti/mappe/ndvi/dati/ndvi.db";
}

/// <summary>
/// Legge l'NDVI (task #84) dal database SQLite del programma <c>ndvi/</c> (schema in
/// <c>ndvi/LEGGIMI.md</c>), sempre in sola lettura (<c>Mode=ReadOnly</c>): non crea il file e non
/// scrive mai. Se il file non c'è risponde <see cref="NdviAzienda.NonCalcolato"/>, senza errore.
/// Dati sporchi (data illeggibile, media non numerica) si saltano, mai un'eccezione.
/// </summary>
public sealed class LettoreNdvi
{
    private readonly string _percorso;

    public LettoreNdvi(IOptions<NdviOptions> opzioni)
    {
        _percorso = Espandi(opzioni.Value.Database);
    }

    public string Percorso => _percorso;

    /// <summary>«~/x» → cartella dell'utente + «/x»; gli altri percorsi restano come sono.</summary>
    public static string Espandi(string percorso) =>
        percorso == "~" || percorso.StartsWith("~/", StringComparison.Ordinal)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), percorso.Length > 2 ? percorso[2..] : "")
            : percorso;

    /// <summary>
    /// Passaggi dell'azienda (tutti, puliti e no, dal più recente) e punti NDVI dei vigneti indicati,
    /// solo dei passaggi puliti dell'azienda e solo con una media.
    /// </summary>
    public async Task<NdviAzienda> LeggiAziendaAsync(int idAzienda, IReadOnlyCollection<int> idVigneti, CancellationToken ct)
    {
        if (!File.Exists(_percorso))
        {
            return NdviAzienda.NonCalcolato;
        }

        var stringa = new SqliteConnectionStringBuilder { DataSource = _percorso, Mode = SqliteOpenMode.ReadOnly, Pooling = false };
        await using var connessione = new SqliteConnection(stringa.ToString());
        await connessione.OpenAsync(ct);

        var passaggi = new List<PassaggioNdvi>();
        await using (var comando = connessione.CreateCommand())
        {
            comando.CommandText = "SELECT data, nuvole_pct, valido FROM passaggi WHERE id_azienda = $azienda ORDER BY data DESC";
            comando.Parameters.AddWithValue("$azienda", idAzienda);
            await using var lettore = await comando.ExecuteReaderAsync(ct);
            while (await lettore.ReadAsync(ct))
            {
                if (Giorno(lettore.GetValue(0)) is { } data)
                {
                    passaggi.Add(new PassaggioNdvi(data, Numero(lettore.GetValue(1)), Numero(lettore.GetValue(2)) == 1));
                }
            }
        }

        var punti = new List<PuntoNdvi>();
        var vigneti = idVigneti.Distinct().ToList();
        if (vigneti.Count > 0 && passaggi.Any(p => p.Valido))
        {
            await using var comando = connessione.CreateCommand();
            // Gli Id dei vigneti sono interi: in IN come parametri, uno per vigneto.
            var nomi = vigneti.Select((id, i) =>
            {
                var nome = "$v" + i.ToString(CultureInfo.InvariantCulture);
                comando.Parameters.AddWithValue(nome, id);
                return nome;
            });
            comando.CommandText = $"""
                SELECT n.id_vigneto, n.data, n.media, n.nuvole_pct
                FROM ndvi n JOIN passaggi p ON p.data = n.data AND p.id_azienda = $azienda AND p.valido = 1
                WHERE n.media IS NOT NULL AND n.id_vigneto IN ({string.Join(", ", nomi)})
                ORDER BY n.id_vigneto, n.data
                """;
            comando.Parameters.AddWithValue("$azienda", idAzienda);
            await using var lettore = await comando.ExecuteReaderAsync(ct);
            while (await lettore.ReadAsync(ct))
            {
                if (Numero(lettore.GetValue(0)) is { } id && Giorno(lettore.GetValue(1)) is { } data && Numero(lettore.GetValue(2)) is { } media)
                {
                    punti.Add(new PuntoNdvi((int)id, data, Math.Round(media, 3), Numero(lettore.GetValue(3))));
                }
            }
        }

        return new NdviAzienda(true, passaggi, punti);
    }

    /// <summary>Un giorno «AAAA-MM-GG» (anche con l'ora dopo), o null.</summary>
    public static DateOnly? Giorno(object? valore) =>
        valore is string testo && testo.Length >= 10
        && DateOnly.TryParseExact(testo[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var giorno)
            ? giorno
            : null;

    /// <summary>Un numero finito (intero o reale di SQLite), o null.</summary>
    public static decimal? Numero(object? valore) => valore switch
    {
        long intero => intero,
        double reale when double.IsFinite(reale) && Math.Abs(reale) < 1e15 => (decimal)reale,
        _ => null,
    };
}

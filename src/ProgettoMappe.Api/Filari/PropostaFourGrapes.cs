using System.Globalization;
using System.Text.Json.Nodes;
using ProgettoMappe.Domain.Entities;
using ProgettoMappe.Infrastructure.Repositories.Filari;

namespace ProgettoMappe.Api.Filari;

/// <summary>
/// «Carica in 4Grapes» (task #146): i filari approvati diventano una proposta per l'esecutore del dba (JSON
/// nel formato delle sue proposte: titolo, sql, righe attese, controlli, come tornare indietro). L'Api non
/// scrive mai in 4Grapes: deposita il file, l'esecutore lo manda al proprietario e lo esegue solo dopo il suo ✅.
/// In 4Grapes non c'è posto per le linee (dba, task #96): si scrivono solo i numeri che già conosce,
/// Vigneto.Filari e Vigneto.Orientamento. TraFila no: cambierebbe le piante stimate (trigger) e le stime di produzione.
/// </summary>
public static class PropostaFourGrapes
{
    /// <summary>Numero di filari e orientamento (gradi da nord, intero 0–179) da scrivere per una versione salvata.</summary>
    public sealed record Valori(int Filari, int Orientamento);

    /// <summary>
    /// I valori per 4Grapes: filari = allineamenti distinti (un filare spezzato da un buco è un filare solo,
    /// come lo conta chi lo cammina), orientamento = direzione della versione arrotondata al grado. Null se
    /// la versione non ha filari o direzione.
    /// </summary>
    public static Valori? Calcola(VersioneFilari versione, IReadOnlyList<Filare> filari)
    {
        if (filari.Count == 0 || versione.AzimutGradi is not { } azimut)
        {
            return null;
        }

        var gradi = (int)Math.Round(azimut, MidpointRounding.AwayFromZero) % 180;
        return new Valori(Allineamenti(filari, azimut, versione.InterfilaM), gradi);
    }

    /// <summary>
    /// Quanti allineamenti distinti: i centri dei filari proiettati sulla perpendicolare alla direzione,
    /// raggruppati quando distano meno di mezza interfila (1 m se l'interfila non c'è).
    /// </summary>
    public static int Allineamenti(IReadOnlyList<Filare> filari, double azimutGradi, double interfilaM)
    {
        if (filari.Count == 0)
        {
            return 0;
        }

        var utm = ProiezioneUtm.Per(filari.Average(f => (f.Lon1 + f.Lon2) / 2), filari.Average(f => (f.Lat1 + f.Lat2) / 2));
        var r = azimutGradi * Math.PI / 180;
        var (nx, ny) = (Math.Cos(r), -Math.Sin(r));
        var tolleranza = interfilaM > 0 ? interfilaM / 2 : 1;
        var distanze = filari
            .Select(f =>
            {
                var (x, y) = utm.Avanti((f.Lon1 + f.Lon2) / 2, (f.Lat1 + f.Lat2) / 2);
                return x * nx + y * ny;
            })
            .Order()
            .ToList();

        var gruppi = 1;
        for (var i = 1; i < distanze.Count; i++)
        {
            if (distanze[i] - distanze[i - 1] >= tolleranza)
            {
                gruppi++;
            }
        }
        return gruppi;
    }

    /// <summary>
    /// La proposta JSON per l'esecutore. L'UPDATE vale solo se Filari e Orientamento sono ancora quelli letti
    /// (<paramref name="vigneto"/>), così non si scavalca una modifica fatta nel frattempo in 4Grapes.
    /// </summary>
    public static JsonObject Componi(Vigneto vigneto, string nomeAzienda, VersioneFilari versione, Valori valori,
        string autore, string? autoreNome, DateTime oraItaliana)
    {
        var id = vigneto.Id;
        var chi = string.IsNullOrWhiteSpace(autoreNome) ? autore : $"{autoreNome.Trim()} ({autore})";
        var vecchiFilari = Sql(vigneto.FilariDichiarati);
        var vecchioOrientamento = Sql(vigneto.OrientamentoGradi);
        var condizione = $"IdVigneto = {id} AND {Uguale("Filari", vigneto.FilariDichiarati)} AND {Uguale("Orientamento", vigneto.OrientamentoGradi)}";

        return new JsonObject
        {
            ["titolo"] = $"Filari del vigneto {Testo(vigneto.Nome)} ({id}, {Testo(nomeAzienda)}): {valori.Filari} filari, orientamento {valori.Orientamento}°",
            ["richiesta_da"] = $"Mappe, «Carica in 4Grapes», utente {chi}, {oraItaliana.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}, task #146",
            ["perche"] = $"I filari del vigneto sono stati disegnati e approvati in Mappe (versione {versione.Versione}, " +
                $"{versione.NumFilari} tratti, {Math.Round(versione.LunghezzaM)} m, interfila {versione.InterfilaM.ToString("0.##", CultureInfo.InvariantCulture)} m). " +
                $"In 4Grapes non c'è posto per le linee: si riportano solo numero di filari ({vecchiFilari} → {valori.Filari}) e " +
                $"orientamento in gradi da nord ({vecchioOrientamento} → {valori.Orientamento}). TraFila e SullaFila non si toccano.",
            ["sql"] = $"UPDATE dbo.Vigneto SET Filari = {valori.Filari}, Orientamento = {valori.Orientamento} WHERE {condizione};",
            ["righe_attese"] = new JsonObject { ["dbo.Vigneto"] = 1 },
            ["effetti_indiretti"] = "Su dbo.Vigneto c'è il trigger trg_Vigneto_AggiornaNumeroPianteStimate: ricalcola le piante stimate da " +
                "superficie, TraFila e SullaFila, che non cambiano, quindi il valore resta lo stesso (da confermare dal dba). " +
                "Nessun'altra tabella cambia.",
            ["verifica_prima"] = $"SELECT IdVigneto, Filari, Orientamento, TraFila, SullaFila, NumeroPianteStimate FROM dbo.Vigneto WHERE IdVigneto = {id}",
            ["controllo_dopo"] = new JsonObject
            {
                ["sql"] = $"SELECT COUNT(*) AS aggiornato FROM dbo.Vigneto WHERE IdVigneto = {id} AND Filari = {valori.Filari} AND Orientamento = {valori.Orientamento}",
                ["atteso"] = new JsonArray(1),
            },
            ["come_tornare_indietro"] = $"UPDATE dbo.Vigneto SET Filari = {vecchiFilari}, Orientamento = {vecchioOrientamento} WHERE IdVigneto = {id};",
            ["rischio"] = "basso: due campi numerici su una riga, si torna indietro con un UPDATE.",
        };
    }

    /// <summary>Nome del file della proposta: data, ora e vigneto (le proposte dell'esecutore si ordinano per nome).</summary>
    public static string NomeFile(int idVigneto, DateTime oraItaliana) =>
        $"{oraItaliana.ToString("yyyy-MM-dd-HHmmss", CultureInfo.InvariantCulture)}-mappe-filari-vigneto-{idVigneto}.json";

    private static string Sql(int? valore) => valore?.ToString(CultureInfo.InvariantCulture) ?? "NULL";

    private static string Uguale(string colonna, int? valore) =>
        valore is { } v ? $"{colonna} = {v.ToString(CultureInfo.InvariantCulture)}" : $"{colonna} IS NULL";

    /// <summary>Nomi da 4Grapes nel titolo: una riga sola, corti.</summary>
    private static string Testo(string? nome)
    {
        var pulito = string.Join(' ', (nome ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return pulito.Length > 80 ? pulito[..80] + "…" : pulito;
    }
}

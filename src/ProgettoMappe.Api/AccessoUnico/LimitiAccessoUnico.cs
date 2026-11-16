using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace ProgettoMappe.Api.AccessoUnico;

/// <summary>Contatore a finestra fissa per chiave (in memoria: si azzera al riavvio, basta per frenare gli abusi).</summary>
public sealed class ContatoreFinestra
{
    private const int MaxVoci = 10_000;

    private readonly TimeProvider _orologio;
    private readonly TimeSpan _finestra;
    private readonly ConcurrentDictionary<string, (int Conteggio, DateTimeOffset Inizio)> _voci = new();

    public ContatoreFinestra(TimeProvider orologio, TimeSpan finestra)
    {
        _orologio = orologio;
        _finestra = finestra;
    }

    public int Conteggio(string chiave) =>
        _voci.TryGetValue(Normalizza(chiave), out var voce) && _orologio.GetUtcNow() - voce.Inizio < _finestra ? voce.Conteggio : 0;

    /// <summary>Conta una volta e dice il nuovo totale nella finestra.</summary>
    public int Conta(string chiave)
    {
        var adesso = _orologio.GetUtcNow();
        if (_voci.Count >= MaxVoci)
        {
            foreach (var (k, v) in _voci)
            {
                if (adesso - v.Inizio >= _finestra)
                {
                    _voci.TryRemove(k, out _);
                }
            }
        }

        return _voci.AddOrUpdate(Normalizza(chiave),
            _ => (1, adesso),
            (_, v) => adesso - v.Inizio < _finestra ? (v.Conteggio + 1, v.Inizio) : (1, adesso)).Conteggio;
    }

    public void Azzera(string chiave) => _voci.TryRemove(Normalizza(chiave), out _);

    // Come la collation di LoginName: maiuscole e spazi ai bordi non contano.
    private static string Normalizza(string chiave) => chiave.Trim().ToUpperInvariant();
}

/// <summary>I due limiti dell'accesso unico: richieste al minuto per app, errori di password per email (15 minuti).</summary>
public sealed class LimitiAccessoUnico
{
    public static readonly TimeSpan FinestraErrori = TimeSpan.FromMinutes(15);

    public LimitiAccessoUnico(TimeProvider orologio)
    {
        RichiesteApp = new ContatoreFinestra(orologio, TimeSpan.FromMinutes(1));
        ErroriEmail = new ContatoreFinestra(orologio, FinestraErrori);
        RichiesteRecupero = new ContatoreFinestra(orologio, TimeSpan.FromHours(1));
    }

    /// <summary>«Password dimenticata» (task #289): richieste in un'ora, per email («e:…») e per indirizzo IP («ip:…»).</summary>
    public ContatoreFinestra RichiesteRecupero { get; }

    public const int MaxRecuperoPerEmail = 3, MaxRecuperoPerIp = 10;

    public ContatoreFinestra RichiesteApp { get; }

    public ContatoreFinestra ErroriEmail { get; }
}

/// <summary>Riconoscimento dell'app chiamante dalle intestazioni <c>X-Mappe-App</c> e <c>X-Mappe-App-Segreto</c>.</summary>
public static class AppChiamante
{
    public const string IntestazioneNome = "X-Mappe-App";
    public const string IntestazioneSegreto = "X-Mappe-App-Segreto";

    public enum Esito { Riconosciuta, Sconosciuta, Disabilitata }

    public static (Esito Esito, string? Nome, AppEsternaOptions? App) Riconosci(IHeaderDictionary intestazioni, AccessoUnicoOptions opzioni,
        SegretiApp? segretiApp = null)
    {
        var nomi = intestazioni[IntestazioneNome];
        var segreti = intestazioni[IntestazioneSegreto];
        if (nomi.Count != 1 || segreti.Count != 1 || string.IsNullOrWhiteSpace(nomi[0]) || string.IsNullOrEmpty(segreti[0])
            || !opzioni.App.TryGetValue(nomi[0]!.Trim(), out var app)
            || !(SegretoValido(segreti[0]!, app.SegretoSha256) || UgualeA(segreti[0]!, segretiApp?.Di(nomi[0]!.Trim()))))
        {
            return (Esito.Sconosciuta, null, null);
        }

        // Il nome come scritto in configurazione: è l'aud del token, deve essere sempre uguale.
        var nome = opzioni.App.Keys.First(k => string.Equals(k, nomi[0]!.Trim(), StringComparison.OrdinalIgnoreCase));
        return app.Abilitata ? (Esito.Riconosciuta, nome, app) : (Esito.Disabilitata, nome, app);
    }

    /// <summary>Confronto a tempo costante con il segreto creato da Mappe; nessun segreto = nessun accesso.</summary>
    public static bool UgualeA(string ricevuto, string? atteso) =>
        !string.IsNullOrEmpty(atteso)
        && CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(ricevuto)), SHA256.HashData(Encoding.UTF8.GetBytes(atteso)));

    /// <summary>Confronto a tempo costante dell'impronta; un'impronta configurata non valida non accetta niente.</summary>
    public static bool SegretoValido(string segreto, string? impronta)
    {
        var attesa = impronta?.Trim();
        if (attesa is not { Length: 64 } || !attesa.All(char.IsAsciiHexDigit))
        {
            return false;
        }

        var calcolata = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(segreto)));
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(calcolata), Encoding.ASCII.GetBytes(attesa.ToUpperInvariant()));
    }
}

using System.Collections.Concurrent;

namespace ProgettoMappe.Web.Accesso;

/// <summary>
/// Frena chi prova password a ripetizione: dopo <see cref="MaxErrori"/> errori sullo stesso nome
/// utente in <see cref="Finestra"/>, quel nome è bloccato fino alla fine della finestra. Le
/// password di 4Grapes sono SHA-256 senza sale: indovinarle va reso lento. In memoria (un solo
/// processo Web): un riavvio azzera i conteggi.
/// </summary>
public class LimiteTentativi
{
    public const int MaxErrori = 5;
    public static readonly TimeSpan Finestra = TimeSpan.FromMinutes(15);
    private const int MaxVoci = 10_000;

    private readonly TimeProvider _orologio;
    private readonly ConcurrentDictionary<string, (int Errori, DateTimeOffset Inizio)> _voci = new();

    public LimiteTentativi(TimeProvider orologio)
    {
        _orologio = orologio;
    }

    public bool Bloccato(string nomeUtente)
    {
        var adesso = _orologio.GetUtcNow();
        return _voci.TryGetValue(Chiave(nomeUtente), out var voce)
            && adesso - voce.Inizio < Finestra
            && voce.Errori >= MaxErrori;
    }

    public void RegistraErrore(string nomeUtente)
    {
        var adesso = _orologio.GetUtcNow();
        if (_voci.Count >= MaxVoci)
            Pulisci(adesso);

        _voci.AddOrUpdate(Chiave(nomeUtente),
            _ => (1, adesso),
            (_, voce) => adesso - voce.Inizio < Finestra ? (voce.Errori + 1, voce.Inizio) : (1, adesso));
    }

    public void RegistraSuccesso(string nomeUtente) => _voci.TryRemove(Chiave(nomeUtente), out _);

    // Come la collation di LoginName (Latin1_General_CI_AS): maiuscole e spazi ai bordi non contano.
    private static string Chiave(string nomeUtente) => nomeUtente.Trim().ToUpperInvariant();

    private void Pulisci(DateTimeOffset adesso)
    {
        foreach (var (chiave, voce) in _voci)
        {
            if (adesso - voce.Inizio >= Finestra)
                _voci.TryRemove(chiave, out _);
        }
    }
}

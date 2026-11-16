using ProgettoMappe.Contracts;

namespace ProgettoMappe.Infrastructure.Repositories;

/// <summary>
/// Letture dei rilievi della nutrizione del Portale (tappa 2, sezioni 6–9), con le condizioni dei controller del portale
/// (FallanzeController, AnalisiUveController, …).
/// </summary>
public interface ILettureNutrizionePortale
{
    /// <summary>
    /// Le righe della vista della sezione (colonne anagrafiche comuni: IdRilievo, InizioRilievo, Anno, IdEnte, RagioneSociale,
    /// IdVigna, Vigna, IdVigneto, NomeVigneto, IdVitigno, Vitigno) più le colonne dei campi, come testo invariante;
    /// <paramref name="soloEnti"/> = solo quelle aziende, null = tutte. Vista e colonne vengono solo dal codice.
    /// </summary>
    Task<IReadOnlyList<RilievoDto>> RilieviAsync(string vista, IReadOnlyList<string> colonne, IReadOnlyList<int>? soloEnti,
        CancellationToken ct = default);

    /// <summary>Vigne e vigneti dell'azienda (<c>IdVigna &gt; 0</c>, <c>IdVigneto &gt; 0</c>) per i menù della scheda.</summary>
    Task<IReadOnlyList<VignetoMenuDto>> VignetiAziendaAsync(int azienda, CancellationToken ct = default);

    /// <summary>Quello che il portale copia dal vigneto in un rilievo nuovo; null se il vigneto non c'è.</summary>
    Task<VignetoRilievo?> VignetoRilievoAsync(int idVigneto, CancellationToken ct = default);

    /// <summary>L'azienda e la data del rilievo con quell'IdRilievo e quel tipo; null se non c'è.</summary>
    Task<RilievoEsistente?> RilievoAsync(DateTime idRilievo, int tipo, CancellationToken ct = default);
}

/// <summary>Le colonne del vigneto che il portale scrive in un rilievo nuovo (<c>…DaApp</c>, posizione, conduzione).</summary>
public record VignetoRilievo(int IdVigneto, int IdVigna, int IdEnte, int? IdVitigno, decimal? Latitudine, decimal? Longitudine,
    int? IdConduzione);

public record RilievoEsistente(DateTime IdRilievo, int? IdEnte, DateTime InizioRilievo);

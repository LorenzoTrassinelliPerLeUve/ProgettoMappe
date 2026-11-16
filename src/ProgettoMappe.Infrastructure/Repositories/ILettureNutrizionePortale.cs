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
    /// <remarks>
    /// <paramref name="vista"/> null = direttamente <c>dbo.Rilievo</c> di quel <paramref name="tipo"/> con vigneto, vigna,
    /// azienda, vitigno (Anno = anno di InizioRilievo) e, per Sabbia/Limo/Argilla, la <c>Tessitura</c> del rilievo.
    /// </remarks>
    Task<IReadOnlyList<RilievoDto>> RilieviAsync(string? vista, int tipo, IReadOnlyList<string> colonne, IReadOnlyList<int>? soloEnti,
        CancellationToken ct = default);

    /// <summary>La tessitura con quella terna (<c>Tessitura</c> ne ha una sola per terna, dba 16/11); null se non c'è.</summary>
    Task<(int IdTessitura, int IdTessituraSuolo)?> TessituraAsync(int sabbia, int limo, int argilla, CancellationToken ct = default);

    /// <summary>I sottovigneti del vigneto (<c>dbo.SubVigneto</c>, <c>IdSubVigneto &gt; 0</c>), per nome.</summary>
    Task<IReadOnlyList<SottovignetoMenuDto>> SottovignetiAsync(int idVigneto, CancellationToken ct = default);

    /// <summary>Vigne e vigneti dell'azienda (<c>IdVigna &gt; 0</c>, <c>IdVigneto &gt; 0</c>) per i menù della scheda.</summary>
    Task<IReadOnlyList<VignetoMenuDto>> VignetiAziendaAsync(int azienda, CancellationToken ct = default);

    /// <summary>Quello che il portale copia dal vigneto in un rilievo nuovo; null se il vigneto non c'è.</summary>
    Task<VignetoRilievo?> VignetoRilievoAsync(int idVigneto, CancellationToken ct = default);

    /// <summary>L'azienda e la data del rilievo con quell'IdRilievo e quel tipo; null se non c'è.</summary>
    Task<RilievoEsistente?> RilievoAsync(DateTime idRilievo, int tipo, CancellationToken ct = default);

    /// <summary><c>VistaVignetiConProduzioni</c>; <paramref name="soloEnti"/> = solo quelle aziende, null = tutte.</summary>
    Task<IReadOnlyList<ObiettivoRigaDto>> ObiettiviAsync(IReadOnlyList<int>? soloEnti, CancellationToken ct = default);

    /// <summary>
    /// L'obiettivo (<c>TargetProduttivo</c>, prima riga per anno e vigneto come il portale) con piante e superficie del vigneto;
    /// null se il vigneto non c'è.
    /// </summary>
    Task<ObiettivoEsistente?> ObiettivoAsync(int idVigneto, int anno, CancellationToken ct = default);
}

/// <param name="IdTargetProduttivo">La riga di <c>TargetProduttivo</c>, null se l'anno non ha ancora un obiettivo.</param>
public record ObiettivoEsistente(int? IdTargetProduttivo, int IdEnte, ObiettivoDto Obiettivo);

/// <summary>Le colonne del vigneto che il portale scrive in un rilievo nuovo (<c>…DaApp</c>, posizione, conduzione).</summary>
public record VignetoRilievo(int IdVigneto, int IdVigna, int IdEnte, int? IdVitigno, decimal? Latitudine, decimal? Longitudine,
    int? IdConduzione);

public record RilievoEsistente(DateTime IdRilievo, int? IdEnte, DateTime InizioRilievo);

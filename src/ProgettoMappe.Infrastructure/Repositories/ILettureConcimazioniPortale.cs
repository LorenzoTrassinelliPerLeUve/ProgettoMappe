using ProgettoMappe.Contracts;

namespace ProgettoMappe.Infrastructure.Repositories;

/// <summary>
/// Letture della sezione 18 Concimazioni del Portale (tappa 3, task #260). Le funzioni di 4Grapes sono inline TVF: le legge
/// <c>ProgettoMappe_ReadOnly</c> con il permesso SELECT (dba, 16/11), con gli stessi parametri del portale.
/// </summary>
public interface ILettureConcimazioniPortale
{
    /// <summary><c>dbo.TargetEnologico</c> con <c>IdTarget &gt; 0</c>, per descrizione.</summary>
    Task<IReadOnlyList<TargetEnologicoDto>> TargetAsync(CancellationToken ct = default);

    /// <summary><c>dbo.EpocaConcimazione</c>, per descrizione (italiano).</summary>
    Task<IReadOnlyList<VocePortaleDto>> EpocheAsync(CancellationToken ct = default);

    /// <summary><c>dbo.ModalitaConcimazione</c>, per descrizione (italiano).</summary>
    Task<IReadOnlyList<VocePortaleDto>> ModalitaAsync(CancellationToken ct = default);

    /// <summary>I vigneti dell'azienda (<c>IdVigneto &gt; 0</c>), con vigna, vitigno e se sono in carico e non estirpati.</summary>
    Task<IReadOnlyList<VignetoConcimazioneDto>> VignetiAsync(int azienda, CancellationToken ct = default);

    /// <summary>Le righe di <c>UnitaBaseConcimazione</c> dei vigneti dell'azienda, tutti gli anni.</summary>
    Task<IReadOnlyList<UnitaBaseDto>> UnitaBaseAsync(int azienda, CancellationToken ct = default);

    /// <summary>
    /// Le tre tabelle di «Unità di concimazione»: <c>FnT_FattoriConcimazione_ConVariazione_ByEnte</c> (vigneti, per nome),
    /// <c>…_PerVigne</c> (vigne) e <c>…_PerAziende</c> (aziende), tutte con (@Anno, @IdEnte). I filtri li fa il controller.
    /// </summary>
    Task<UnitaConcimazioneDto> UnitaConcimazioneAsync(int anno, int azienda, CancellationToken ct = default);

    /// <summary><c>FnT_FattoriConcimazione(@Anno, @IdEnte)</c> con <c>IdEnte = azienda</c>, per nome del vigneto.</summary>
    Task<IReadOnlyList<FattoreConcimazioneDto>> FattoriAsync(int anno, int azienda, CancellationToken ct = default);

    /// <summary>
    /// Il resoconto: <c>…_ConVariazione_ByEnte(@Anno, @IdEnte)</c> con i delta di
    /// <c>FnT_DiffConcimazione_Elementi(@Anno, @IdEnte, @IdTarget, @IdEpocaConcimazione)</c>, poi
    /// <c>VistaConcimazioneRealePerTarget</c> e <c>VistaConcimazioneReale</c> dell'anno e dell'azienda. Target ed epoca filtrano
    /// come nel portale (null = tutti).
    /// </summary>
    Task<ResocontoConcimazioniDto> ResocontoAsync(int anno, int azienda, int? target, int? epoca, CancellationToken ct = default);

    /// <summary>Una riga di <c>ConcimazioneReale</c>, null se non c'è.</summary>
    Task<ConcimazioneRealeDto?> ConcimazioneRealeAsync(int id, CancellationToken ct = default);

    /// <summary><c>MAX(IdInserimento) + 1</c> di <c>ConcimazioneReale</c> (1 se non ce ne sono), come il portale.</summary>
    Task<int> ProssimoInserimentoAsync(CancellationToken ct = default);

    /// <summary>
    /// L'export «EXPORT» delle unità: <c>VistaExportFattoriConcimazione{Vigneti|Vigne|Aziende}_{lingua}</c> dell'azienda e
    /// dell'anno, nello stesso ordine del portale.
    /// </summary>
    Task<IReadOnlyList<(string Foglio, TabellaPortaleDto Tabella)>> EsportaUnitaAsync(string lingua, int anno, int azienda,
        CancellationToken ct = default);
}

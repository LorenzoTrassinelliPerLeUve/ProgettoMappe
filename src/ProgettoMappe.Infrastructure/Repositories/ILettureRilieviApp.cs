using ProgettoMappe.Infrastructure.Scrittura;

namespace ProgettoMappe.Infrastructure.Repositories;

/// <summary>
/// Un tipo di rilievo scritto dalle app di Jan (task #265, script del dba <c>docs/specifiche/app-jan/2026-11-16-tipi-rilievo-app.sql</c>):
/// il rilievo in <c>dbo.Rilievo</c> e una riga nella tabella del tipo, chiave <c>IdRilievo</c> (ON DELETE CASCADE), con il
/// codice dell'app UNIQUE.
/// </summary>
public sealed record TipoRilievoApp(int Tipo, TabellaScrivibile Tabella, string ColonnaCodice);

public static class TipiRilievoApp
{
    /// <summary>169 «PROVA IRRORATRICE (4Sprayers)», codice della prova in 4Sprayers (RS-/RA-…).</summary>
    public static readonly TipoRilievoApp ProvaIrroratrice = new(169, TabelleScrivibili.RilievoProvaIrroratrice, "CodiceProva");

    /// <summary>170 «RIASSUNTO POTATURA (App potatura)», codice del rilievo nell'App potatura (Guid).</summary>
    public static readonly TipoRilievoApp RiassuntoPotatura = new(170, TabelleScrivibili.RilievoRiassuntoPotatura, "CodiceOrigine");
}

/// <summary>
/// Un rilievo di un'app come sta in 4Grapes. <paramref name="IdEnte"/> = <c>IdAziendaDaApp</c>, o l'azienda del vigneto se è
/// vuota (come <c>EliminaRilievoCompleto</c>); <paramref name="Valori"/> = le colonne della tabella del tipo senza IdRilievo.
/// </summary>
public sealed record RilievoApp(DateTime IdRilievo, int? IdEnte, int? IdVigneto, int IdUtente, DateTime InizioRilievo, string? Note,
    IReadOnlyDictionary<string, object?> Valori);

/// <summary>Letture dei rilievi delle app (ProgettoMappe_ReadOnly ha SELECT su Rilievo e sulle due tabelle, dba 16/11).</summary>
public interface ILettureRilieviApp
{
    /// <summary>I rilievi del tipo dell'azienda, dal più recente; vigneto e giorni (da/a compresi) facoltativi.</summary>
    Task<IReadOnlyList<RilievoApp>> RilieviAsync(TipoRilievoApp tipo, int idEnte, int? idVigneto, DateOnly? da, DateOnly? a,
        CancellationToken ct = default);

    /// <summary>Il rilievo con quel codice dell'app, null se non c'è.</summary>
    Task<RilievoApp?> PerCodiceAsync(TipoRilievoApp tipo, string codice, CancellationToken ct = default);
}

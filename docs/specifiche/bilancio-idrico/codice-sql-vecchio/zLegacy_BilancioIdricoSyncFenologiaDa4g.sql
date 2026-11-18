
/*
4) Wrapper sync fenologia per orchestratore.
   La fenologia DSS e view-based (storico + previsione), quindi non richiede materializzazione.
*/
CREATE   PROCEDURE dbo.BilancioIdricoSyncFenologiaDa4g
    @DataMin date = NULL,
    @DryRun bit = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'dbo.VistaBilancioIdricoFenologiaGiornaliera', N'V') IS NULL
        THROW 50611, 'Manca dbo.VistaBilancioIdricoFenologiaGiornaliera.', 1;

    IF @DryRun = 1
    BEGIN
        SELECT TOP (300)
            f.IdVigneto,
            f.[Data],
            f.BBCH,
            f.CodiceFase,
            f.FonteFenologia,
            f.FlagCarryForward,
            f.FlagFenologiaMancante
        FROM dbo.VistaBilancioIdricoFenologiaGiornaliera f
        WHERE (@DataMin IS NULL OR f.[Data] >= @DataMin)
        ORDER BY f.[Data] DESC, f.IdVigneto;
    END
    ELSE
    BEGIN
        SELECT
            N'OK' AS Esito,
            N'Fenologia gestita da vista (nessuna scrittura necessaria).' AS Messaggio;
    END
END;

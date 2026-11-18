
CREATE   PROCEDURE dbo.SP_BI_PublishRun
    @IdRun bigint,
    @IdEnte int,
    @Utente nvarchar(128) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.BI_Run WHERE IdRun = @IdRun AND IdEnte = @IdEnte AND Stato = 'COMPLETATO')
        THROW 51002, 'Run non pubblicabile: stato diverso da COMPLETATO o IdEnte non coerente.', 1;

    MERGE dbo.BI_RunPublication AS tgt
    USING (SELECT @IdEnte AS IdEnte, @IdRun AS IdRunCorrente) AS src
      ON tgt.IdEnte = src.IdEnte
    WHEN MATCHED THEN
        UPDATE SET
            IdRunCorrente = src.IdRunCorrente,
            PubblicatoIlUtc = sysutcdatetime(),
            PubblicatoDa = @Utente
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (IdEnte, IdRunCorrente, PubblicatoIlUtc, PubblicatoDa)
        VALUES (src.IdEnte, src.IdRunCorrente, sysutcdatetime(), @Utente);
END;

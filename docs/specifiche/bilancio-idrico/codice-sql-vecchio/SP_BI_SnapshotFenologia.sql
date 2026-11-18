
CREATE   PROCEDURE dbo.SP_BI_SnapshotFenologia
    @IdEnte int,
    @DataDa date,
    @DataA date
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH SourceData AS
    (
        SELECT s.*
        FROM dbo.VistaBI_SourceFenologia s
        WHERE s.IdEnte = @IdEnte
          AND s.[Data] BETWEEN @DataDa AND @DataA
    )
    MERGE dbo.BI_InputFenologiaGiornaliera AS tgt
    USING SourceData src
      ON tgt.IdEnte = src.IdEnte
     AND tgt.IdVigneto = src.IdVigneto
     AND tgt.[Data] = src.[Data]
    WHEN MATCHED THEN
        UPDATE SET
            BBCH = src.BBCH,
            IdFase = src.IdFase,
            FonteFenologia = src.FonteFenologia,
            FlagCarryForward = src.FlagCarryForward,
            FlagFenologiaMancante = src.FlagFenologiaMancante,
            AggiornatoIlUtc = sysutcdatetime()
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (IdEnte, IdVigneto, [Data], BBCH, IdFase, FonteFenologia, FlagCarryForward, FlagFenologiaMancante, CreatoIlUtc, AggiornatoIlUtc)
        VALUES (src.IdEnte, src.IdVigneto, src.[Data], src.BBCH, src.IdFase, src.FonteFenologia, src.FlagCarryForward, src.FlagFenologiaMancante, sysutcdatetime(), sysutcdatetime())
    WHEN NOT MATCHED BY SOURCE
         AND tgt.IdEnte = @IdEnte
         AND tgt.[Data] BETWEEN @DataDa AND @DataA
    THEN DELETE;
END;

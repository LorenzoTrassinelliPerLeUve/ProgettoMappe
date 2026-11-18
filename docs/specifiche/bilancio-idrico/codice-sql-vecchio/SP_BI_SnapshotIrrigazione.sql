
CREATE   PROCEDURE dbo.SP_BI_SnapshotIrrigazione
    @IdEnte int,
    @DataDa date,
    @DataA date
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.BI_InputIrrigazioneSettoreVignetoGiornaliera AS tgt
    USING
    (
        SELECT
            s.IdEnte,
            s.IdSettore,
            s.IdVigneto,
            s.[Data],
            s.IrrigazioneMm,
            CONVERT(int, s.NumeroContributi) AS NumeroContributi
        FROM dbo.VistaBI_SourceIrrigazioneDettaglio s
        WHERE s.IdEnte = @IdEnte
          AND s.[Data] BETWEEN @DataDa AND @DataA
    ) src
      ON tgt.IdEnte = src.IdEnte
     AND tgt.IdSettore = src.IdSettore
     AND tgt.IdVigneto = src.IdVigneto
     AND tgt.[Data] = src.[Data]
    WHEN MATCHED THEN
        UPDATE SET
            IrrigazioneMm = src.IrrigazioneMm,
            NumeroContributi = src.NumeroContributi,
            AggiornatoIlUtc = sysutcdatetime()
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (IdEnte, IdSettore, IdVigneto, [Data], IrrigazioneMm, NumeroContributi, AggiornatoIlUtc)
        VALUES (src.IdEnte, src.IdSettore, src.IdVigneto, src.[Data], src.IrrigazioneMm, src.NumeroContributi, sysutcdatetime());
END;

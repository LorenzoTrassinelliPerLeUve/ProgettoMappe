
CREATE   PROCEDURE dbo.SP_BI_ComputeSettore
    @IdRun bigint,
    @IdEnte int,
    @DataDa date,
    @DataA date
AS
BEGIN
    SET NOCOUNT ON;

    DELETE dbo.BI_ResultSettoreGiornaliero
    WHERE IdRun = @IdRun
      AND IdEnte = @IdEnte
      AND [Data] BETWEEN @DataDa AND @DataA;

    INSERT dbo.BI_ResultSettoreGiornaliero
    (
        IdRun, IdEnte, IdSettore, [Data], IrrigazioneSuggeritaMm, IrrigazioneSuggeritaOre, PrioritaCriticita, AggiornatoIlUtc
    )
    SELECT
        @IdRun,
        r.IdEnte,
        r.IdSettore,
        r.[Data],
        CAST(SUM(r.IrrigazioneSuggeritaMm * (sv.PercentualeNelSettore / 100.0)) AS decimal(12,4)) AS IrrigazioneSuggeritaMm,
        CAST(SUM(r.IrrigazioneSuggeritaOre * (sv.PercentualeNelSettore / 100.0)) AS decimal(12,4)) AS IrrigazioneSuggeritaOre,
        MAX(r.PrioritaCriticita) AS PrioritaCriticita,
        sysutcdatetime()
    FROM dbo.BI_ResultSettoreVignetoGiornaliero r
    JOIN dbo.BI_SettoreVigneto sv
      ON sv.IdSettore = r.IdSettore
     AND sv.IdVigneto = r.IdVigneto
    WHERE r.IdRun = @IdRun
      AND r.IdEnte = @IdEnte
      AND r.[Data] BETWEEN @DataDa AND @DataA
    GROUP BY r.IdEnte, r.IdSettore, r.[Data];
END;

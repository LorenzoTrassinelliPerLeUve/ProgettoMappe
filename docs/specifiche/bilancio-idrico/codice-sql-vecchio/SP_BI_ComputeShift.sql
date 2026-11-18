
CREATE   PROCEDURE dbo.SP_BI_ComputeShift
    @IdRun bigint,
    @IdEnte int,
    @DataDa date,
    @DataA date
AS
BEGIN
    SET NOCOUNT ON;

    DELETE dbo.BI_ResultShiftGiornaliero
    WHERE IdRun = @IdRun
      AND IdEnte = @IdEnte
      AND [Data] BETWEEN @DataDa AND @DataA;

    INSERT dbo.BI_ResultShiftGiornaliero
    (
        IdRun, IdEnte, IdShift, [Data], OreComandoShift, SettoriCoinvolti, AggiornatoIlUtc
    )
    SELECT
        @IdRun,
        @IdEnte,
        ss.IdShift,
        rs.[Data],
        MAX(rs.IrrigazioneSuggeritaOre) AS OreComandoShift,
        COUNT(DISTINCT rs.IdSettore) AS SettoriCoinvolti,
        sysutcdatetime()
    FROM dbo.BI_ResultSettoreGiornaliero rs
    JOIN dbo.BI_ShiftSettore ss
      ON ss.IdSettore = rs.IdSettore
     AND ss.ValidoDal <= CAST(rs.[Data] AS datetime2(0))
     AND (ss.ValidoAl IS NULL OR ss.ValidoAl >= CAST(rs.[Data] AS datetime2(0)))
    JOIN dbo.BI_Shift sh
      ON sh.IdShift = ss.IdShift
     AND sh.IdEnte = @IdEnte
    WHERE rs.IdRun = @IdRun
      AND rs.IdEnte = @IdEnte
      AND rs.[Data] BETWEEN @DataDa AND @DataA
    GROUP BY ss.IdShift, rs.[Data];
END;


CREATE   PROCEDURE dbo.SP_BI_CompareRunBaseline
    @IdRunBaseline bigint,
    @IdRunNuovo bigint
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        COALESCE(b.IdEnte, n.IdEnte) AS IdEnte,
        COALESCE(b.IdSettore, n.IdSettore) AS IdSettore,
        COALESCE(b.IdVigneto, n.IdVigneto) AS IdVigneto,
        COALESCE(b.[Data], n.[Data]) AS [Data],
        b.IrrigazioneSuggeritaMm AS Irr_Baseline,
        n.IrrigazioneSuggeritaMm AS Irr_Nuovo,
        n.IrrigazioneSuggeritaMm - b.IrrigazioneSuggeritaMm AS Delta_IrrMm,
        b.PdlwpMpa AS Pdlwp_Baseline,
        n.PdlwpMpa AS Pdlwp_Nuovo,
        n.PdlwpMpa - b.PdlwpMpa AS Delta_Pdlwp
    FROM dbo.BI_ResultSettoreVignetoGiornaliero b
    FULL OUTER JOIN dbo.BI_ResultSettoreVignetoGiornaliero n
      ON n.IdEnte = b.IdEnte
     AND n.IdSettore = b.IdSettore
     AND n.IdVigneto = b.IdVigneto
     AND n.[Data] = b.[Data]
     AND n.IdRun = @IdRunNuovo
    WHERE b.IdRun = @IdRunBaseline;
END;

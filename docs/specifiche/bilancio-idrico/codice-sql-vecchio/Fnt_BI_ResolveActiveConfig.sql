
CREATE   FUNCTION dbo.Fnt_BI_ResolveActiveConfig
(
    @IdSettore int,
    @Data date
)
RETURNS TABLE
AS
RETURN
(
    SELECT TOP (1)
        sc.IdSettoreConfig,
        sc.IdTipologiaImpianto,
        sc.TassoIrrigazioneMmOra,
        COALESCE(sc.EfficienzaReale, ti.CoefficienteEfficienza, CONVERT(decimal(8,4), 1.0)) AS EfficienzaUsata
    FROM dbo.BI_SettoreConfig sc
    LEFT JOIN dbo.BI_TipologiaImpianto ti
      ON ti.IdTipologiaImpianto = sc.IdTipologiaImpianto
    WHERE sc.IdSettore = @IdSettore
      AND sc.ValidoDal <= @Data
      AND (sc.ValidoAl IS NULL OR sc.ValidoAl >= @Data)
    ORDER BY sc.ValidoDal DESC, sc.IdSettoreConfig DESC
);

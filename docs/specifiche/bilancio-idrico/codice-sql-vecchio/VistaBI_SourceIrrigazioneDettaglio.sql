
CREATE   VIEW dbo.VistaBI_SourceIrrigazioneDettaglio
AS
WITH EventiSettore AS
(
    SELECT
        e.IdEnte,
        e.IdEventoIrrigazione,
        CAST(e.DataOra AS date) AS [Data],
        e.IdSettoreDiretto AS IdSettore,
        CAST(
            COALESCE(
                e.VolumeAdacquamentoUtileMm,
                e.VolumeAdacquamentoMm * COALESCE(e.CoefficienteEfficienza, 1.0),
                CASE
                    WHEN e.DurataOre IS NOT NULL
                         THEN e.DurataOre * cfg.TassoIrrigazioneMmOra
                    ELSE NULL
                END
            ) AS decimal(12,4)
        ) AS VolumeMm
    FROM dbo.BI_InputIrrigazioneEvento e
    OUTER APPLY
    (
        SELECT TOP (1) sc.TassoIrrigazioneMmOra
        FROM dbo.BI_SettoreConfig sc
        WHERE sc.IdSettore = e.IdSettoreDiretto
          AND sc.ValidoDal <= CAST(e.DataOra AS date)
          AND (sc.ValidoAl IS NULL OR sc.ValidoAl >= CAST(e.DataOra AS date))
        ORDER BY sc.ValidoDal DESC, sc.IdSettoreConfig DESC
    ) cfg
    WHERE e.IdSettoreDiretto IS NOT NULL

    UNION ALL

    SELECT
        e.IdEnte,
        e.IdEventoIrrigazione,
        CAST(e.DataOra AS date) AS [Data],
        ss.IdSettore,
        CAST(
            CASE
                WHEN e.DurataOre IS NOT NULL THEN e.DurataOre * cfg2.TassoIrrigazioneMmOra
                ELSE NULL
            END AS decimal(12,4)
        ) AS VolumeMm
    FROM dbo.BI_InputIrrigazioneEvento e
    JOIN dbo.BI_ShiftSettore ss
      ON ss.IdShift = e.IdShift
     AND ss.ValidoDal <= e.DataOra
     AND (ss.ValidoAl IS NULL OR ss.ValidoAl >= e.DataOra)
    OUTER APPLY
    (
        SELECT TOP (1) sc.TassoIrrigazioneMmOra
        FROM dbo.BI_SettoreConfig sc
        WHERE sc.IdSettore = ss.IdSettore
          AND sc.ValidoDal <= CAST(e.DataOra AS date)
          AND (sc.ValidoAl IS NULL OR sc.ValidoAl >= CAST(e.DataOra AS date))
        ORDER BY sc.ValidoDal DESC, sc.IdSettoreConfig DESC
    ) cfg2
    WHERE e.IdShift IS NOT NULL
      AND e.IdSettoreDiretto IS NULL
)
SELECT
    es.IdEnte,
    es.IdSettore,
    sv.IdVigneto,
    es.[Data],
    CAST(SUM(es.VolumeMm * (sv.PercentualeNelSettore / 100.0)) AS decimal(12,4)) AS IrrigazioneMm,
    COUNT_BIG(*) AS NumeroContributi
FROM EventiSettore es
JOIN dbo.BI_SettoreVigneto sv ON sv.IdSettore = es.IdSettore
WHERE es.VolumeMm IS NOT NULL
GROUP BY es.IdEnte, es.IdSettore, sv.IdVigneto, es.[Data];

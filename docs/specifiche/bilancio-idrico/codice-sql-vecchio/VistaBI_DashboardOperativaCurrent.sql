
/* ============================================================================
   BI - NOMI TESTUALI PER VISTE DASHBOARD-SAFE

   Motivo:
   - Le viste dashboard-safe devono essere direttamente leggibili dal portale.
   - Aggiunge NomeSettore, NomeShift e NomeVigneto senza cambiare la semantica:
     la root resta dbo.BI_RunPublicationDashboard.

   Nota cardinalita':
   - BI_Settore e' joinata su PK IdSettore.
   - BI_Shift e' joinata su PK IdShift dentro OUTER APPLY TOP (1).
   - Vigneto e' joinata su PK IdVigneto.
   Queste join non moltiplicano le righe.
============================================================================ */

CREATE   VIEW dbo.VistaBI_DashboardOperativaCurrent
AS
WITH ResultSV AS
(
    SELECT r.*
    FROM dbo.BI_ResultSettoreVignetoGiornaliero r
    JOIN dbo.BI_RunPublicationDashboard p
      ON p.IdEnte = r.IdEnte
     AND p.IdRunCorrenteDashboard = r.IdRun
),
AggSV AS
(
    SELECT
        r.IdRun,
        r.IdEnte,
        r.IdSettore,
        r.[Data],
        COUNT(DISTINCT r.IdVigneto) AS NumeroVignetiCoinvolti,
        COUNT(DISTINCT CASE WHEN COALESCE(r.IrrigazioneSuggeritaMm, 0) > 0 THEN r.IdVigneto END) AS NumeroVignetiDaIrrigare,
        SUM(COALESCE(r.IrrigazioneMm, 0)) AS IrrigazioneRegistrataMm,
        SUM(COALESCE(r.IrrigazioneSuggeritaMm, 0)) AS IrrigazioneSuggeritaMmDaVigneti,
        MAX(r.PrioritaCriticita) AS PrioritaDaVigneti,
        MAX(r.AggiornatoIlUtc) AS AggiornatoIlUtc,
        SUM(CASE
            WHEN r.AWCmm IS NULL
              OR r.FlagMeteoMancante = 1
              OR r.FlagFenologiaMancante = 1
              OR r.FlagSuoloMancante = 1
              OR r.FlagConfigMancante = 1
            THEN 1 ELSE 0
        END) AS NumeroProblemiDati,
        MAX(CASE
            WHEN r.AWCmm IS NULL
              OR r.FlagMeteoMancante = 1
              OR r.FlagFenologiaMancante = 1
              OR r.FlagSuoloMancante = 1
              OR r.FlagConfigMancante = 1
            THEN 1 ELSE 0
        END) AS FlagDatiMancanti,
        MAX(CASE WHEN r.SuggerimentoIrrigazione = N'IRRIGARE' OR COALESCE(r.IrrigazioneSuggeritaMm, 0) > 0 THEN 1 ELSE 0 END) AS FlagIrrigare
    FROM ResultSV r
    GROUP BY r.IdRun, r.IdEnte, r.IdSettore, r.[Data]
),
ResultSettore AS
(
    SELECT r.*
    FROM dbo.BI_ResultSettoreGiornaliero r
    JOIN dbo.BI_RunPublicationDashboard p
      ON p.IdEnte = r.IdEnte
     AND p.IdRunCorrenteDashboard = r.IdRun
)
SELECT
    COALESCE(s.IdRun, a.IdRun) AS IdRun,
    COALESCE(s.IdEnte, a.IdEnte) AS IdEnte,
    COALESCE(s.IdSettore, a.IdSettore) AS IdSettore,
    bs.NomeSettore,
    COALESCE(s.[Data], a.[Data]) AS [Data],
    CAST(CASE WHEN COALESCE(a.FlagIrrigare, CASE WHEN COALESCE(s.IrrigazioneSuggeritaMm, 0) > 0 THEN 1 ELSE 0 END) = 1 THEN N'IRRIGARE' ELSE N'NON_IRRIGARE' END AS nvarchar(20)) AS SuggerimentoIrrigazione,
    COALESCE(s.IrrigazioneSuggeritaMm, a.IrrigazioneSuggeritaMmDaVigneti) AS IrrigazioneSuggeritaMm,
    s.IrrigazioneSuggeritaOre,
    COALESCE(s.PrioritaCriticita, a.PrioritaDaVigneti) AS PrioritaCriticita,
    COALESCE(a.IrrigazioneRegistrataMm, 0) AS IrrigazioneRegistrataMm,
    COALESCE(a.NumeroVignetiCoinvolti, 0) AS NumeroVignetiCoinvolti,
    COALESCE(a.NumeroVignetiDaIrrigare, 0) AS NumeroVignetiDaIrrigare,
    COALESCE(a.NumeroProblemiDati, 0) AS NumeroProblemiDati,
    CAST(COALESCE(a.FlagDatiMancanti, 0) AS bit) AS FlagDatiMancanti,
    sh.IdShift,
    sh.NomeShift,
    sh.OreComandoShift,
    sh.SettoriCoinvolti,
    COALESCE(s.AggiornatoIlUtc, a.AggiornatoIlUtc, sh.AggiornatoIlUtc) AS AggiornatoIlUtc
FROM ResultSettore s
FULL OUTER JOIN AggSV a
  ON a.IdRun = s.IdRun
 AND a.IdEnte = s.IdEnte
 AND a.IdSettore = s.IdSettore
 AND a.[Data] = s.[Data]
LEFT JOIN dbo.BI_Settore bs
  ON bs.IdSettore = COALESCE(s.IdSettore, a.IdSettore)
OUTER APPLY
(
    SELECT TOP (1)
        rs.IdShift,
        bsh.NomeShift,
        rs.OreComandoShift,
        rs.SettoriCoinvolti,
        rs.AggiornatoIlUtc
    FROM dbo.BI_ShiftSettore ss
    JOIN dbo.BI_ResultShiftGiornaliero rs
      ON rs.IdRun = COALESCE(s.IdRun, a.IdRun)
     AND rs.IdEnte = COALESCE(s.IdEnte, a.IdEnte)
     AND rs.IdShift = ss.IdShift
     AND rs.[Data] = COALESCE(s.[Data], a.[Data])
    JOIN dbo.BI_Shift bsh
      ON bsh.IdShift = rs.IdShift
    WHERE ss.IdSettore = COALESCE(s.IdSettore, a.IdSettore)
      AND ss.ValidoDal <= CONVERT(datetime2(0), COALESCE(s.[Data], a.[Data]))
      AND (ss.ValidoAl IS NULL OR ss.ValidoAl >= CONVERT(datetime2(0), COALESCE(s.[Data], a.[Data])))
    ORDER BY COALESCE(rs.OreComandoShift, 0) DESC, rs.IdShift
) sh;

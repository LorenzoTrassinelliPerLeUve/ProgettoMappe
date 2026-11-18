
/* ============================================================================
   View: dashboard-safe run status
============================================================================ */

CREATE   VIEW dbo.VistaBI_RunDashboardStatus
AS
WITH Enti AS
(
    SELECT DISTINCT IdEnte FROM dbo.BI_Settore
    UNION
    SELECT DISTINCT IdEnte FROM dbo.BI_RunPublicationDashboard
    UNION
    SELECT DISTINCT IdEnte FROM dbo.BI_RunPublication
    UNION
    SELECT DISTINCT IdEnte FROM dbo.BI_RecalcQueue
),
SettoriAttivi AS
(
    SELECT s.IdEnte, COUNT_BIG(1) AS SettoriAttivi
    FROM dbo.BI_Settore s
    WHERE s.Attivo = 1
    GROUP BY s.IdEnte
),
ScopeTeorico AS
(
    SELECT
        s.IdEnte,
        COUNT_BIG(1) AS ScopeTeoricoRighe,
        COUNT(DISTINCT sv.IdSettore) AS ScopeTeoricoSettori,
        COUNT(DISTINCT sv.IdVigneto) AS ScopeTeoricoVigneti
    FROM dbo.BI_Settore s
    JOIN dbo.BI_SettoreVigneto sv ON sv.IdSettore = s.IdSettore
    WHERE s.Attivo = 1
    GROUP BY s.IdEnte
),
SettoriSenzaVigneti AS
(
    SELECT s.IdEnte, COUNT_BIG(1) AS SettoriSenzaVigneti
    FROM dbo.BI_Settore s
    WHERE s.Attivo = 1
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.BI_SettoreVigneto sv
          WHERE sv.IdSettore = s.IdSettore
      )
    GROUP BY s.IdEnte
),
Pub AS
(
    SELECT
        e.IdEnte,
        p.IdRunCorrenteDashboard AS IdRunCorrente,
        p.PubblicatoIlUtc
    FROM Enti e
    LEFT JOIN dbo.BI_RunPublicationDashboard p ON p.IdEnte = e.IdEnte
),
RunData AS
(
    SELECT
        p.IdEnte,
        p.IdRunCorrente,
        r.DataDa,
        r.DataA,
        r.Stato AS StatoRun,
        p.PubblicatoIlUtc
    FROM Pub p
    LEFT JOIN dbo.BI_Run r ON r.IdRun = p.IdRunCorrente
),
ScopeRun AS
(
    SELECT
        p.IdEnte,
        COUNT_BIG(1) AS ScopeRunRighe,
        COUNT(DISTINCT rs.IdSettore) AS ScopeRunSettori,
        COUNT(DISTINCT rs.IdVigneto) AS ScopeRunVigneti
    FROM Pub p
    JOIN dbo.BI_RunScope rs ON rs.IdRun = p.IdRunCorrente
    GROUP BY p.IdEnte
),
ResultRunDistinct AS
(
    SELECT DISTINCT
        p.IdEnte,
        r.IdSettore,
        r.IdVigneto
    FROM Pub p
    JOIN dbo.BI_ResultSettoreVignetoGiornaliero r
      ON r.IdRun = p.IdRunCorrente
     AND r.IdEnte = p.IdEnte
),
ResultRun AS
(
    SELECT
        IdEnte,
        COUNT_BIG(1) AS ResultRunRighe,
        COUNT(DISTINCT IdSettore) AS ResultRunSettori,
        COUNT(DISTINCT IdVigneto) AS ResultRunVigneti
    FROM ResultRunDistinct
    GROUP BY IdEnte
),
PeriodoDashboard AS
(
    SELECT DATEADD(day, v.N, CAST(GETDATE() AS date)) AS [Data]
    FROM (VALUES (0),(1),(2),(3),(4),(5),(6)) v(N)
),
ResultPeriodoDashboard AS
(
    SELECT
        p.IdEnte,
        COUNT_BIG(1) AS ResultPeriodoDashboardRighe
    FROM Pub p
    JOIN dbo.BI_ResultSettoreVignetoGiornaliero r
      ON r.IdRun = p.IdRunCorrente
     AND r.IdEnte = p.IdEnte
    JOIN PeriodoDashboard pe
      ON pe.[Data] = r.[Data]
    GROUP BY p.IdEnte
),
Queue AS
(
    SELECT
        q.IdEnte,
        SUM(CASE WHEN q.Stato = N'PENDING' THEN 1 ELSE 0 END) AS QueuePending,
        SUM(CASE WHEN q.Stato = N'PROCESSING' THEN 1 ELSE 0 END) AS QueueProcessing,
        SUM(CASE WHEN q.Stato = N'ERROR' THEN 1 ELSE 0 END) AS QueueError
    FROM dbo.BI_RecalcQueue q
    WHERE q.Stato IN (N'PENDING', N'PROCESSING', N'ERROR')
    GROUP BY q.IdEnte
),
LatestRun AS
(
    SELECT
        r.IdEnte,
        r.IdRun,
        r.Stato,
        r.ConclusoIlUtc,
        ROW_NUMBER() OVER (PARTITION BY r.IdEnte ORDER BY r.AvviatoIlUtc DESC, r.IdRun DESC) AS rn
    FROM dbo.BI_Run r
),
MaxAgg AS
(
    SELECT
        p.IdEnte,
        MAX(r.AggiornatoIlUtc) AS MaxAggiornatoIlUtc
    FROM Pub p
    JOIN dbo.BI_ResultSettoreVignetoGiornaliero r
      ON r.IdRun = p.IdRunCorrente
     AND r.IdEnte = p.IdEnte
    GROUP BY p.IdEnte
),
Base AS
(
    SELECT
        e.IdEnte,
        rd.IdRunCorrente,
        rd.DataDa,
        rd.DataA,
        rd.StatoRun,
        rd.PubblicatoIlUtc,
        CONVERT(bigint, COALESCE(st.ScopeTeoricoRighe, 0)) AS ScopeTeoricoRighe,
        CONVERT(int, COALESCE(st.ScopeTeoricoSettori, 0)) AS ScopeTeoricoSettori,
        CONVERT(int, COALESCE(st.ScopeTeoricoVigneti, 0)) AS ScopeTeoricoVigneti,
        CONVERT(bigint, COALESCE(sr.ScopeRunRighe, 0)) AS ScopeRunRighe,
        CONVERT(int, COALESCE(sr.ScopeRunSettori, 0)) AS ScopeRunSettori,
        CONVERT(int, COALESCE(sr.ScopeRunVigneti, 0)) AS ScopeRunVigneti,
        CONVERT(bigint, COALESCE(rr.ResultRunRighe, 0)) AS ResultRunRighe,
        CONVERT(int, COALESCE(rr.ResultRunSettori, 0)) AS ResultRunSettori,
        CONVERT(int, COALESCE(rr.ResultRunVigneti, 0)) AS ResultRunVigneti,
        CONVERT(bigint, COALESCE(rp.ResultPeriodoDashboardRighe, 0)) AS ResultPeriodoDashboardRighe,
        CONVERT(bigint, COALESCE(sa.SettoriAttivi, 0)) AS SettoriAttivi,
        CONVERT(bigint, COALESCE(ssv.SettoriSenzaVigneti, 0)) AS SettoriSenzaVigneti,
        COALESCE(q.QueuePending, 0) AS QueuePending,
        COALESCE(q.QueueProcessing, 0) AS QueueProcessing,
        COALESCE(q.QueueError, 0) AS QueueError,
        lr.Stato AS StatoUltimoRun,
        ma.MaxAggiornatoIlUtc
    FROM Enti e
    LEFT JOIN RunData rd ON rd.IdEnte = e.IdEnte
    LEFT JOIN ScopeTeorico st ON st.IdEnte = e.IdEnte
    LEFT JOIN ScopeRun sr ON sr.IdEnte = e.IdEnte
    LEFT JOIN ResultRun rr ON rr.IdEnte = e.IdEnte
    LEFT JOIN ResultPeriodoDashboard rp ON rp.IdEnte = e.IdEnte
    LEFT JOIN SettoriAttivi sa ON sa.IdEnte = e.IdEnte
    LEFT JOIN SettoriSenzaVigneti ssv ON ssv.IdEnte = e.IdEnte
    LEFT JOIN Queue q ON q.IdEnte = e.IdEnte
    LEFT JOIN LatestRun lr ON lr.IdEnte = e.IdEnte AND lr.rn = 1
    LEFT JOIN MaxAgg ma ON ma.IdEnte = e.IdEnte
)
SELECT
    b.IdEnte,
    b.IdRunCorrente,
    b.DataDa,
    b.DataA,
    b.StatoRun,
    b.PubblicatoIlUtc,
    b.ScopeTeoricoRighe,
    b.ScopeTeoricoSettori,
    b.ScopeTeoricoVigneti,
    b.ScopeRunRighe,
    b.ScopeRunSettori,
    b.ScopeRunVigneti,
    b.ResultRunRighe,
    b.ResultRunSettori,
    b.ResultRunVigneti,
    CAST(CASE
        WHEN b.IdRunCorrente IS NULL THEN 0
        WHEN b.SettoriAttivi > 0 AND b.ScopeTeoricoRighe = 0 THEN 1
        WHEN b.SettoriSenzaVigneti > 0 THEN 1
        WHEN b.ScopeRunRighe < b.ScopeTeoricoRighe THEN 1
        WHEN b.ResultRunRighe < b.ScopeTeoricoRighe THEN 1
        WHEN b.ResultPeriodoDashboardRighe < (b.ScopeTeoricoRighe * 7) THEN 1
        ELSE 0
    END AS bit) AS FlagRunParziale,
    CAST(CASE WHEN b.IdRunCorrente IS NULL THEN 1 ELSE 0 END AS bit) AS FlagNessunRunPubblicato,
    CAST(CASE
        WHEN b.IdRunCorrente IS NULL THEN 0
        WHEN b.SettoriAttivi > 0 AND b.ScopeTeoricoRighe = 0 THEN 1
        WHEN b.SettoriSenzaVigneti > 0 THEN 1
        WHEN b.ScopeRunRighe < b.ScopeTeoricoRighe THEN 1
        WHEN b.ResultRunRighe < b.ScopeTeoricoRighe THEN 1
        WHEN b.ResultPeriodoDashboardRighe < (b.ScopeTeoricoRighe * 7) THEN 1
        WHEN b.DataDa > CAST(GETDATE() AS date) THEN 1
        WHEN b.DataA < DATEADD(day, 6, CAST(GETDATE() AS date)) THEN 1
        ELSE 0
    END AS bit) AS FlagDatiIncompleti,
    b.QueuePending,
    b.QueueProcessing,
    b.QueueError,
    b.MaxAggiornatoIlUtc,
    CAST(CASE
        WHEN b.IdRunCorrente IS NULL THEN N'NESSUN_RUN_PUBBLICATO'
        WHEN b.StatoUltimoRun = N'FALLITO' OR b.QueueError > 0 THEN N'ERRORE_CALCOLO'
        WHEN b.QueueProcessing > 0 THEN N'RICALCOLO_IN_CORSO'
        WHEN b.SettoriAttivi > 0 AND b.ScopeTeoricoRighe = 0 THEN N'DATI_INCOMPLETI'
        WHEN b.SettoriSenzaVigneti > 0 THEN N'DATI_INCOMPLETI'
        WHEN b.ScopeRunRighe < b.ScopeTeoricoRighe THEN N'DATI_INCOMPLETI'
        WHEN b.ResultRunRighe < b.ScopeTeoricoRighe THEN N'DATI_INCOMPLETI'
        WHEN b.ResultPeriodoDashboardRighe < (b.ScopeTeoricoRighe * 7) THEN N'DATI_INCOMPLETI'
        WHEN b.DataDa > CAST(GETDATE() AS date) THEN N'DATI_INCOMPLETI'
        WHEN b.DataA < DATEADD(day, 6, CAST(GETDATE() AS date)) THEN N'DATI_INCOMPLETI'
        WHEN b.PubblicatoIlUtc < DATEADD(hour, -24, sysutcdatetime()) THEN N'DATI_OBSOLETI'
        ELSE N'AGGIORNATO'
    END AS nvarchar(40)) AS DashboardStatus
FROM Base b;

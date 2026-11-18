
/* ============================================================================
   Procedure: diagnostica stabile copertura run dashboard/tecnico
============================================================================ */

CREATE   PROCEDURE dbo.SP_BI_DiagnoseDashboardRunCoverage
    @IdEnte int
AS
BEGIN
    SET NOCOUNT ON;

    WITH PubTecnica AS
    (
        SELECT p.IdEnte, p.IdRunCorrente
        FROM dbo.BI_RunPublication p
        WHERE p.IdEnte = @IdEnte
    ),
    PubDashboard AS
    (
        SELECT p.IdEnte, p.IdRunCorrenteDashboard
        FROM dbo.BI_RunPublicationDashboard p
        WHERE p.IdEnte = @IdEnte
    ),
    Pub AS
    (
        SELECT
            e.IdEnte,
            pt.IdRunCorrente AS IdRunCorrenteTecnico,
            pd.IdRunCorrenteDashboard,
            COALESCE(pd.IdRunCorrenteDashboard, pt.IdRunCorrente) AS IdRunAnalizzato
        FROM (SELECT @IdEnte AS IdEnte) e
        LEFT JOIN PubTecnica pt ON pt.IdEnte = e.IdEnte
        LEFT JOIN PubDashboard pd ON pd.IdEnte = e.IdEnte
    ),
    RunDate AS
    (
        SELECT r.IdRun, r.DataDa, r.DataA,
               DATEDIFF(day, r.DataDa, r.DataA) + 1 AS GiorniRun
        FROM dbo.BI_Run r
        JOIN Pub p ON p.IdRunAnalizzato = r.IdRun
    ),
    ScopeTeorico AS
    (
        SELECT s.IdEnte, sv.IdSettore, sv.IdVigneto
        FROM dbo.BI_Settore s
        JOIN dbo.BI_SettoreVigneto sv ON sv.IdSettore = s.IdSettore
        WHERE s.IdEnte = @IdEnte
          AND s.Attivo = 1
    ),
    SettoriAttivi AS
    (
        SELECT s.IdSettore
        FROM dbo.BI_Settore s
        WHERE s.IdEnte = @IdEnte
          AND s.Attivo = 1
    ),
    ScopeRun AS
    (
        SELECT rs.IdRun, rs.IdEnte, rs.IdSettore, rs.IdVigneto
        FROM dbo.BI_RunScope rs
        JOIN Pub p ON p.IdRunAnalizzato = rs.IdRun
    ),
    ResultRun AS
    (
        SELECT DISTINCT r.IdRun, r.IdEnte, r.IdSettore, r.IdVigneto
        FROM dbo.BI_ResultSettoreVignetoGiornaliero r
        JOIN Pub p ON p.IdRunAnalizzato = r.IdRun
    ),
    PeriodoDashboard AS
    (
        SELECT CAST(GETDATE() AS date) AS DataDa,
               DATEADD(day, 6, CAST(GETDATE() AS date)) AS DataA
    ),
    ResultPeriodoDashboard AS
    (
        SELECT r.IdRun, r.IdEnte, r.IdSettore, r.IdVigneto, r.[Data]
        FROM dbo.BI_ResultSettoreVignetoGiornaliero r
        JOIN Pub p ON p.IdRunAnalizzato = r.IdRun
        CROSS JOIN PeriodoDashboard pe
        WHERE r.[Data] BETWEEN pe.DataDa AND pe.DataA
    ),
    CurrentTecnico AS
    (
        SELECT DISTINCT r.IdRun, r.IdEnte, r.IdSettore, r.IdVigneto
        FROM dbo.VistaBI_ResultSettoreVignetoCurrent r
        WHERE r.IdEnte = @IdEnte
    )
    SELECT
        p.IdRunCorrenteTecnico,
        p.IdRunCorrenteDashboard,
        p.IdRunAnalizzato,
        rd.DataDa,
        rd.DataA,
        rd.GiorniRun,
        (SELECT COUNT(*) FROM SettoriAttivi) AS SettoriAttivi,
        (SELECT COUNT(*) FROM ScopeTeorico) AS ScopeTeoricoRighe,
        (SELECT COUNT(DISTINCT IdSettore) FROM ScopeTeorico) AS ScopeTeoricoSettori,
        (SELECT COUNT(DISTINCT IdVigneto) FROM ScopeTeorico) AS ScopeTeoricoVigneti,
        (SELECT COUNT(*) FROM ScopeRun) AS ScopeRunRighe,
        (SELECT COUNT(DISTINCT IdSettore) FROM ScopeRun) AS ScopeRunSettori,
        (SELECT COUNT(DISTINCT IdVigneto) FROM ScopeRun) AS ScopeRunVigneti,
        (SELECT COUNT(*) FROM ResultRun) AS ResultRunRighe,
        (SELECT COUNT(DISTINCT IdSettore) FROM ResultRun) AS ResultRunSettori,
        (SELECT COUNT(DISTINCT IdVigneto) FROM ResultRun) AS ResultRunVigneti,
        CAST(GETDATE() AS date) AS DataDaDashboardDefault,
        DATEADD(day, 6, CAST(GETDATE() AS date)) AS DataADashboardDefault,
        (SELECT COUNT(*) FROM ScopeTeorico) * 7 AS ExpectedPeriodoDashboardRighe,
        (SELECT COUNT(*) FROM ResultPeriodoDashboard) AS ResultPeriodoDashboardRighe,
        (SELECT COUNT(*) FROM CurrentTecnico) AS VistaCurrentTecnicoRighe,
        (SELECT COUNT(DISTINCT IdSettore) FROM CurrentTecnico) AS VistaCurrentTecnicoSettori,
        (SELECT COUNT(DISTINCT IdVigneto) FROM CurrentTecnico) AS VistaCurrentTecnicoVigneti,
        CASE WHEN p.IdRunAnalizzato IS NULL THEN 1 ELSE 0 END AS FlagNessunRunPubblicato,
        CASE
            WHEN p.IdRunAnalizzato IS NULL THEN 0
            WHEN (SELECT COUNT(*) FROM SettoriAttivi) > 0 AND (SELECT COUNT(*) FROM ScopeTeorico) = 0 THEN 1
            WHEN (SELECT COUNT(*) FROM ScopeRun) < (SELECT COUNT(*) FROM ScopeTeorico) THEN 1
            WHEN (SELECT COUNT(*) FROM ResultRun) < (SELECT COUNT(*) FROM ScopeTeorico) THEN 1
            ELSE 0
        END AS FlagRunCorrenteParziale,
        CASE
            WHEN p.IdRunAnalizzato IS NULL THEN 0
            WHEN (SELECT COUNT(*) FROM ResultPeriodoDashboard) < ((SELECT COUNT(*) FROM ScopeTeorico) * 7) THEN 1
            ELSE 0
        END AS FlagDatiPeriodoDashboardIncompleti
    FROM Pub p
    LEFT JOIN RunDate rd ON rd.IdRun = p.IdRunAnalizzato;
END;

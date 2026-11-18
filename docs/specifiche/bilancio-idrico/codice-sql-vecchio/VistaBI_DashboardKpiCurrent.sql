
/* ============================================================================
   View: KPI dashboard-safe, periodo default oggi..oggi+6
============================================================================ */

CREATE   VIEW dbo.VistaBI_DashboardKpiCurrent
AS
WITH Periodo AS
(
    SELECT CAST(GETDATE() AS date) AS DataDa,
           DATEADD(day, 6, CAST(GETDATE() AS date)) AS DataA
),
Pub AS
(
    SELECT IdEnte, IdRunCorrenteDashboard AS IdRun
    FROM dbo.BI_RunPublicationDashboard
),
R AS
(
    SELECT r.*
    FROM dbo.BI_ResultSettoreVignetoGiornaliero r
    JOIN Pub p ON p.IdEnte = r.IdEnte AND p.IdRun = r.IdRun
    CROSS JOIN Periodo pe
    WHERE r.[Data] BETWEEN pe.DataDa AND pe.DataA
),
S AS
(
    SELECT s.*
    FROM dbo.BI_ResultShiftGiornaliero s
    JOIN Pub p ON p.IdEnte = s.IdEnte AND p.IdRun = s.IdRun
    CROSS JOIN Periodo pe
    WHERE s.[Data] BETWEEN pe.DataDa AND pe.DataA
),
AggR AS
(
    SELECT
        r.IdEnte,
        COUNT(DISTINCT CASE WHEN COALESCE(r.IrrigazioneSuggeritaMm, 0) > 0 THEN r.IdSettore END) AS SettoriDaIrrigare,
        COUNT(DISTINCT CASE WHEN r.FlagCriticitaPdlwp = 1 OR r.FlagCriticitaDeficit = 1 THEN r.IdVigneto END) AS VignetiInStress,
        SUM(CASE
            WHEN r.AWCmm IS NULL
              OR r.FlagMeteoMancante = 1
              OR r.FlagFenologiaMancante = 1
              OR r.FlagSuoloMancante = 1
              OR r.FlagConfigMancante = 1
            THEN 1 ELSE 0
        END) AS ProblemiWarning,
        SUM(COALESCE(r.PioggiaMm, 0)) AS PioggiaMmPeriodo,
        SUM(COALESCE(r.IrrigazioneMm, 0)) AS IrrigazioneMmPeriodo
    FROM R r
    GROUP BY r.IdEnte
),
AggS AS
(
    SELECT
        s.IdEnte,
        COUNT(DISTINCT CASE WHEN COALESCE(s.OreComandoShift, 0) > 0 THEN s.IdShift END) AS ShiftDaAttivare
    FROM S s
    GROUP BY s.IdEnte
),
Problemi AS
(
    SELECT
        p.IdEnte,
        SUM(CASE WHEN p.Severity >= 3 THEN 1 ELSE 0 END) AS ProblemiBloccanti,
        SUM(CASE WHEN p.Severity = 2 THEN 1 ELSE 0 END) AS ProblemiWarning
    FROM dbo.VistaBI_DashboardProblemiCurrent p
    CROSS JOIN Periodo pe
    WHERE p.[Data] IS NULL OR p.[Data] BETWEEN pe.DataDa AND pe.DataA
    GROUP BY p.IdEnte
)
SELECT
    st.IdEnte,
    pe.DataDa,
    pe.DataA,
    COALESCE(ar.SettoriDaIrrigare, 0) AS SettoriDaIrrigare,
    COALESCE(asf.ShiftDaAttivare, 0) AS ShiftDaAttivare,
    COALESCE(ar.VignetiInStress, 0) AS VignetiInStress,
    COALESCE(pr.ProblemiBloccanti, 0) AS ProblemiBloccanti,
    COALESCE(pr.ProblemiWarning, ar.ProblemiWarning, 0) AS ProblemiWarning,
    COALESCE(ar.PioggiaMmPeriodo, 0) AS PioggiaMmPeriodo,
    COALESCE(ar.IrrigazioneMmPeriodo, 0) AS IrrigazioneMmPeriodo,
    st.FlagRunParziale,
    st.FlagDatiIncompleti,
    st.DashboardStatus
FROM dbo.VistaBI_RunDashboardStatus st
CROSS JOIN Periodo pe
LEFT JOIN AggR ar ON ar.IdEnte = st.IdEnte
LEFT JOIN AggS asf ON asf.IdEnte = st.IdEnte
LEFT JOIN Problemi pr ON pr.IdEnte = st.IdEnte;


CREATE   VIEW dbo.VistaBI_RunAudit
AS
SELECT
    r.IdRun,
    r.IdEnte,
    r.DataDa,
    r.DataA,
    r.Stato,
    r.DryRun,
    r.AvviatoIlUtc,
    r.ConclusoIlUtc,
    r.DurataMs,
    r.RigheProcessate,
    r.TriggerOrigine,
    CASE WHEN p.IdRunCorrente = r.IdRun THEN 1 ELSE 0 END AS FlagRunCorrente
FROM dbo.BI_Run r
LEFT JOIN dbo.BI_RunPublication p
  ON p.IdEnte = r.IdEnte;

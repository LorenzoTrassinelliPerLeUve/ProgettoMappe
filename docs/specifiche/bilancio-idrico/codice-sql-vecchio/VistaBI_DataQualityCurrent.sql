
CREATE   VIEW dbo.VistaBI_DataQualityCurrent
AS
SELECT q.*
FROM dbo.BI_DataQualityIssue q
LEFT JOIN dbo.BI_RunPublication p
  ON p.IdEnte = q.IdEnte
WHERE q.IdRun IS NULL
   OR q.IdRun = p.IdRunCorrente;

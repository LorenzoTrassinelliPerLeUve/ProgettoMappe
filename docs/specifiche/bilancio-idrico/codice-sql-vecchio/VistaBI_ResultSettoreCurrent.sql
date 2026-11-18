
CREATE   VIEW dbo.VistaBI_ResultSettoreCurrent
AS
SELECT r.*
FROM dbo.BI_ResultSettoreGiornaliero r
JOIN dbo.BI_RunPublication p
  ON p.IdEnte = r.IdEnte
 AND p.IdRunCorrente = r.IdRun;

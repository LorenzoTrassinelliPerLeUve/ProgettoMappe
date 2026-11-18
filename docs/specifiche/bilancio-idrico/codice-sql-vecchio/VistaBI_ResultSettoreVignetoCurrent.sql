
CREATE   VIEW dbo.VistaBI_ResultSettoreVignetoCurrent
AS
SELECT r.*
FROM dbo.BI_ResultSettoreVignetoGiornaliero r
JOIN dbo.BI_RunPublication p
  ON p.IdEnte = r.IdEnte
 AND p.IdRunCorrente = r.IdRun;

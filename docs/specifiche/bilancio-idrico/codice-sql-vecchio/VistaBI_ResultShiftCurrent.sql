
CREATE   VIEW dbo.VistaBI_ResultShiftCurrent
AS
SELECT r.*
FROM dbo.BI_ResultShiftGiornaliero r
JOIN dbo.BI_RunPublication p
  ON p.IdEnte = r.IdEnte
 AND p.IdRunCorrente = r.IdRun;



CREATE   VIEW [dbo].[VistaBilancioIdricoFenologiaDaModelloBBCH]
AS
SELECT
    mb.Id AS IdModelloBBCH,
    mb.IdVigneto,
    CAST(mb.[Data] AS date) AS [Data],
    mb.IdBBCH_UltimaSettimana AS BBCH,
    dbo.FnTBilancioIdricoMappaBBCHaFase(mb.IdBBCH_UltimaSettimana) AS CodiceFase
FROM [4g].[dbo].[ModelloBBCH] mb
WHERE mb.IdVigneto IS NOT NULL
  AND mb.[Data] IS NOT NULL
  AND YEAR(Data) > 2025;


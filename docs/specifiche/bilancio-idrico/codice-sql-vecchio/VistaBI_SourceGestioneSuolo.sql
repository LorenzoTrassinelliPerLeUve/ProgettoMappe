
CREATE   VIEW dbo.VistaBI_SourceGestioneSuolo
AS
WITH UltimoRilievo AS
(
    SELECT
        r.Vigneto_IdVigneto AS IdVigneto,
        r.IdTipoGestioneSottoLeFile AS IdGestioneSuolo,
        r.InizioRilievo AS DataRilievo,
        ROW_NUMBER() OVER (PARTITION BY r.Vigneto_IdVigneto ORDER BY r.InizioRilievo DESC) AS rn
    FROM dbo.Rilievo r
    WHERE r.TipoRilievo_idTipoRilievo = 140
      AND r.Vigneto_IdVigneto IS NOT NULL
)
SELECT
    vigna.Ente_IdEnte AS IdEnte,
    v.IdVigneto,
    CAST(COALESCE(ur.IdGestioneSuolo, 2) AS tinyint) AS IdGestioneSuoloEffettiva,
    CAST(COALESCE(ur.DataRilievo, sysutcdatetime()) AS datetime2(0)) AS DataRilievo,
    CAST(CASE WHEN ur.IdGestioneSuolo IS NULL THEN N'default' ELSE N'rilievo140' END AS nvarchar(40)) AS FonteDato
FROM dbo.Vigneto v
JOIN dbo.Vigna vigna ON vigna.IdVigna = v.Vigna_IdVigna
LEFT JOIN UltimoRilievo ur
       ON ur.IdVigneto = v.IdVigneto
      AND ur.rn = 1;

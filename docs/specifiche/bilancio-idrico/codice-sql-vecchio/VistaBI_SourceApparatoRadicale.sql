
CREATE   VIEW dbo.VistaBI_SourceApparatoRadicale
AS
WITH UltimoRilievo AS
(
    SELECT
        r.Vigneto_IdVigneto AS IdVigneto,
        r.ProfonditaRadicaleM,
        r.LarghezzaZonaRadicaleM,
        r.NoteApparatoRadicale,
        r.InizioRilievo AS DataRilievo,
        r.IdRilievo AS IdRilievoOrigine,
        ROW_NUMBER() OVER
        (
            PARTITION BY r.Vigneto_IdVigneto
            ORDER BY r.InizioRilievo DESC, r.IdRilievo DESC
        ) AS rn
    FROM dbo.Rilievo r
    WHERE r.TipoRilievo_idTipoRilievo = 145
      AND r.Vigneto_IdVigneto IS NOT NULL
      AND (r.ProfonditaRadicaleM IS NOT NULL OR r.LarghezzaZonaRadicaleM IS NOT NULL)
)
SELECT
    vev.IdEnte,
    v.IdVigneto,
    CAST(v.TraFila AS decimal(8,3)) AS DistanzaFileM,
    ur.ProfonditaRadicaleM,
    ur.LarghezzaZonaRadicaleM,
    ur.DataRilievo,
    ur.IdRilievoOrigine,
    ur.NoteApparatoRadicale,
    CAST(CASE WHEN ur.IdVigneto IS NULL THEN 1 ELSE 0 END AS bit) AS FlagApparatoRadicaleMancante
FROM dbo.Vigneto v
JOIN dbo.VistaEnteVigneto vev
  ON vev.IdVigneto = v.IdVigneto
LEFT JOIN UltimoRilievo ur
       ON ur.IdVigneto = v.IdVigneto
      AND ur.rn = 1;

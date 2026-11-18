
/* ============================================================================
   BI - PATCH AWC CON FATTORE APPARATO RADICALE

   Da lanciare dopo:
   - 16_BI_PATCH_APPARATO_RADICALE_20260527.sql

   Formula Excel applicata:
   Plant available water max mm =
   ((Actual AWC) * ((1 / distance between rows) * root zone width * 10) * root deepening) / 10

   Forma semplificata:
   AWC corretta = AWC base * (LarghezzaZonaRadicaleM / TraFila) * ProfonditaRadicaleM

   Dove:
   - TraFila = Vigneto.TraFila
   - LarghezzaZonaRadicaleM = Rilievo.LarghezzaZonaRadicaleM, TipoRilievo 145
   - ProfonditaRadicaleM = Rilievo.ProfonditaRadicaleM, TipoRilievo 145
============================================================================ */

CREATE   VIEW dbo.VistaBI_SourceSuolo
AS
WITH TessituraAgg AS
(
    SELECT
        t.IdTessituraSuolo,
        MIN(t.IdTessitura) AS IdTessitura,
        AVG(CAST(t.AcquaDisponibile_mm_m AS decimal(12,4))) AS AcquaDisponibileMmM
    FROM dbo.Tessitura t
    GROUP BY t.IdTessituraSuolo
),
UltimaAnalisi AS
(
    SELECT
        r.Vigneto_IdVigneto AS IdVigneto,
        COALESCE(r.IdTessitura, ta.IdTessitura, r.IdTessituraSuolo) AS IdTessitura,
        r.IdPietrositaSuolo AS IdPietrositaSuolo,
        r.IdProfonditaSuolo AS IdProfonditaSuolo,
        ps.Punteggio AS PietrositaSuoloPerc,
        pr.Punteggio AS ProfonditaSuoloM,
        COALESCE(ts.AcquaDisponibile_mm_m, ta.AcquaDisponibileMmM) AS AcquaDisponibileMmM,
        r.InizioRilievo AS DataRilievo,
        ROW_NUMBER() OVER (PARTITION BY r.Vigneto_IdVigneto ORDER BY r.InizioRilievo DESC) AS rn
    FROM dbo.Rilievo r
    LEFT JOIN dbo.PietrositaSuolo ps ON ps.Id = r.IdPietrositaSuolo
    LEFT JOIN dbo.ProfonditaSuolo pr ON pr.Id = r.IdProfonditaSuolo
    LEFT JOIN dbo.TessituraSuolo ts ON ts.Id = r.IdTessituraSuolo
    LEFT JOIN TessituraAgg ta ON ta.IdTessituraSuolo = r.IdTessituraSuolo
    WHERE r.TipoRilievo_idTipoRilievo = 144
      AND r.Vigneto_IdVigneto IS NOT NULL
)
SELECT
    vigna.Ente_IdEnte AS IdEnte,
    v.IdVigneto,
    ua.IdTessitura,
    CAST(ua.PietrositaSuoloPerc AS decimal(8,3)) AS ScheletroPerc,
    CAST(
        CASE
            WHEN ua.AcquaDisponibileMmM IS NULL THEN NULL
            ELSE
                (
                    ua.AcquaDisponibileMmM
                    * COALESCE(NULLIF(ua.ProfonditaSuoloM, 0), 1)
                    * (100 - COALESCE(ua.PietrositaSuoloPerc, 0)) / 100.0
                )
                * CASE
                    WHEN ar.ProfonditaRadicaleM IS NOT NULL
                     AND ar.LarghezzaZonaRadicaleM IS NOT NULL
                     AND ar.DistanzaFileM IS NOT NULL
                     AND ar.DistanzaFileM > 0
                    THEN (ar.LarghezzaZonaRadicaleM / ar.DistanzaFileM) * ar.ProfonditaRadicaleM
                    ELSE 1
                  END
        END
    AS decimal(12,4)) AS CapacitaAcquaDisponibileCorrettaMm,
    ua.DataRilievo,
    CAST(CASE WHEN ua.IdVigneto IS NULL THEN 1 ELSE 0 END AS bit) AS FlagSuoloMancante
FROM dbo.Vigneto v
JOIN dbo.Vigna vigna ON vigna.IdVigna = v.Vigna_IdVigna
LEFT JOIN UltimaAnalisi ua
       ON ua.IdVigneto = v.IdVigneto
      AND ua.rn = 1
OUTER APPLY
(
    SELECT TOP (1) x.*
    FROM dbo.VistaBI_SourceApparatoRadicale x
    WHERE x.IdEnte = vigna.Ente_IdEnte
      AND x.IdVigneto = v.IdVigneto
    ORDER BY x.DataRilievo DESC
) ar;

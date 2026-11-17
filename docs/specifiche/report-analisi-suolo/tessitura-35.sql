-- Task 323 dba: terne dell'ente 35, ricavate come Mappe ha fatto per 1862 e 425 (verificato il 17/11: con questo filtro
-- escono 132 Id per 1862 e 24 per 425, gli stessi delle sue liste): le IdTessitura dei rilievi di azienda-35.sql.
SELECT IdTessitura, Sabbia, Limo, Argilla, Descrizione, PuntoDiAppassimentoPermanente, CapacitaDiCampo,
       AcquaDisponibile_mm_m, PesoSpecificoApparente, ConducibilitaIdraulicaSaturazione, MinSO, MaxSO, MinN, MaxN, MinP, MaxP
FROM dbo.Tessitura WHERE IdTessitura IN (
    SELECT r.IdTessitura FROM dbo.Rilievo r
    JOIN dbo.Vigneto v ON v.IdVigneto = r.Vigneto_IdVigneto
    JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
    JOIN dbo.Ente e ON e.IdEnte = vg.Ente_IdEnte
    WHERE r.TipoRilievo_idTipoRilievo = 144 AND e.IdEnte > 0 AND e.IdEnte <> 10003 AND v.IdVigneto > 0
      AND (r.Suolo_ph_h20 IS NOT NULL OR r.Suolo_SO_perc IS NOT NULL OR r.Suolo_CO_perc IS NOT NULL OR r.Suolo_CSC IS NOT NULL)
      AND e.IdEnte = 35)
ORDER BY IdTessitura

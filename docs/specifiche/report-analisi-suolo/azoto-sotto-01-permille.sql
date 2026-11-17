-- Task 323: rilievi di analisi del suolo (tipo 144) con azoto totale sotto 0,1 per mille (= sotto 0,01 %), per azienda e anno.
-- L'azoto sta quasi sempre in Suolo_N_totale_perc (3.332 rilievi) e quasi mai in Suolo_N_totale_permille (13): qui si usa
-- COALESCE(permille, perc * 10) come valore in per mille.
SELECT e.IdEnte, e.RagioneSociale AS Azienda, YEAR(COALESCE(r.DataPrelievoCampione, r.InizioRilievo)) AS Anno,
       COUNT(*) AS rilievi_azoto_sotto_01_permille,
       MIN(COALESCE(r.Suolo_N_totale_permille, r.Suolo_N_totale_perc * 10)) AS azoto_min_permille,
       MAX(COALESCE(r.Suolo_N_totale_permille, r.Suolo_N_totale_perc * 10)) AS azoto_max_permille,
       SUM(CASE WHEN COALESCE(r.Suolo_N_totale_permille, r.Suolo_N_totale_perc) = 0 THEN 1 ELSE 0 END) AS di_cui_zero,
       MIN(r.Suolo_C_N) AS CN_min_nel_4g, MAX(r.Suolo_C_N) AS CN_max_nel_4g
FROM dbo.Rilievo r
JOIN dbo.Vigneto v ON v.IdVigneto = r.Vigneto_IdVigneto
JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna
JOIN dbo.Ente e ON e.IdEnte = vg.Ente_IdEnte
WHERE r.TipoRilievo_idTipoRilievo = 144
  AND COALESCE(r.Suolo_N_totale_permille, r.Suolo_N_totale_perc * 10) < 0.1
GROUP BY e.IdEnte, e.RagioneSociale, YEAR(COALESCE(r.DataPrelievoCampione, r.InizioRilievo))
ORDER BY rilievi_azoto_sotto_01_permille DESC

# Parità del portale in Mappe, tappa 1 (letture): i numeri attesi (dba, 16/11/2026, task #247)

Riscontro indipendente per le 5 sezioni di sola lettura (27 Home, 5 Export rilievi, 19 Asportazioni, 10 Galleria,
3 Indicatori aziendali). La logica l'ho presa dai controller del portale vecchio (ControllersPortale) e i numeri li ho
calcolati con query dirette sul 4g, in sola lettura (registrate in letture.jsonl). Valgono **al 16/11/2026, ore 16**: i
dati 2026 crescono, quindi confrontateli lo stesso giorno o rilanciate le query (sotto).
Programma: `~/progetti/dba/uscite/parita-247/calcola.py`; risultati grezzi: `risultati.json` nella stessa cartella.

Le 3 aziende (tipo 1, molti rilievi e foto nel 2026):
- **Casa E. Mirafiore** (IdEnte 425), grande: 105 vigneti in carico
- **Tenuta Ficuzza** (IdEnte 2253), media: 42 vigneti
- **Mazzaporro** (IdEnte 628), piccola: 19 vigneti

Visibilità: i numeri sono quelli che vede un **MasterUser** (o un utente abbinato all'azienda con tutte le sue aziende).
Un utente normale vede solo le aziende di Utente_Ente (StatoEntita = 1) e, in Galleria, solo i vigneti di
VistaUtentiVigneti.

## Riepilogo

- **Casa E. Mirafiore**: Export 4014 righe (4 rilevatori); Asportazioni 2025: 75 righe; Galleria 2026: 1304 foto; Indicatori: 104 vigneti in 13 comuni, probabilità il 15/06 peronospora 0.41 e oidio 8.20, mio monitoraggio peronospora 0.0453 e oidio 0.0568.
- **Tenuta Ficuzza**: Export 4205 righe (4 rilevatori); Asportazioni 2026: 42 righe; Galleria 2026: 1698 foto; Indicatori: 42 vigneti in 3 comuni, probabilità il 15/06 peronospora 2.51 e oidio 20.60, mio monitoraggio peronospora 0.0000 e oidio 0.0000.
- **Mazzaporro**: Export 2514 righe (3 rilevatori); Asportazioni 2024: 1 righe; Galleria 2026: 1554 foto; Indicatori: 19 vigneti in 2 comuni, probabilità il 15/06 peronospora 2.68 e oidio 25.96, mio monitoraggio peronospora 1.0494 e oidio 6.0144.

## 27 Home
Non mostra numeri: smista l'utente secondo le sue abilitazioni (VistaAbilitazioniUtente). Con DASHBOARD va alla Dashboard;
altrimenti con PREVISIONI_METEO al Meteo; altrimenti con RILIEVI all'Export rilievi; altrimenti torna al login.
Da confrontare: per un utente SysAdmin o MasterUser il primo passo è la Dashboard; per un utente solo AppEnabled
(ha PREVISIONI_METEO) è il Meteo.

## 5 Export rilievi
«Scarica rilievi dell'azienda», anno 2026, lingua IT: righe del file Excel. Le altre due voci («miei» e «condivisi con me») dipendono dall'utente collegato: aggiungono `_IdUtente = idUtente` oppure `_IdUtente IN (sorgenti di Condivisione verso l'utente)`.

**Casa E. Mirafiore (425)**
- righe = 4014, utenti = 4

**Tenuta Ficuzza (2253)**
- righe = 4205, utenti = 4

**Mazzaporro (628)**
- righe = 2514, utenti = 3

Query (per Mirafiore; per le altre cambia IdEnte e, nelle Asportazioni, l'anno):
```sql
SELECT COUNT(*) AS righe, COUNT(DISTINCT _IdUtente) AS utenti FROM dbo.VistaExportRilievi_IT WHERE _IdAzienda = 425 AND _Anno = 2026;
```

## 19 Asportazioni
Lista con filtri Azienda e Anno (VistaAsportazioni). Nel 2026 hanno dati solo le aziende con la produzione caricata: per Mirafiore e Mazzaporro uso l'ultimo anno disponibile. **Attenzione**: la colonna Produzione_t_ha della vista vale migliaia (6.000-10.000): sono kg/ha con un nome sbagliato. Il portale la mostra così: Mappe deve fare lo stesso, o correggere l'etichetta per tutti e due.

**Casa E. Mirafiore (425)**
- anni = 2025, 2024, 2023, 2022
- righe = 75, vigneti = 75, somma_N = 1275.00, somma_P2O5 = 329.00, somma_K2O = 1417.00, media_t_ha = 6305.31
- NomeVigneto = Barbera sopra strada, t_ha = 10759.00, N = 29.00, P2O5 = 8.00, K2O = 32.00
- NomeVigneto = Biancardi Est Alba, t_ha = 7294.00, N = 20.00, P2O5 = 5.00, K2O = 22.00
- NomeVigneto = Biancardi Triangolo, t_ha = 3372.00, N = 9.00, P2O5 = 2.00, K2O = 10.00
- NomeVigneto = Bricco Biancardi, t_ha = 7755.00, N = 21.00, P2O5 = 5.00, K2O = 23.00
- NomeVigneto = Bricco la Rosa, t_ha = 10873.00, N = 29.00, P2O5 = 8.00, K2O = 33.00

**Tenuta Ficuzza (2253)**
- anni = 2026, 2025, 2024
- righe = 42, vigneti = 42, somma_N = 710.00, somma_P2O5 = 181.00, somma_K2O = 788.00, media_t_ha = 6257.52
- NomeVigneto = Chardonnay 1998 53_Ficuzza Est, t_ha = 6071.00, N = 16.00, P2O5 = 4.00, K2O = 18.00
- NomeVigneto = chardonnay 25_ficuzza nord  mirto, t_ha = 6399.00, N = 17.00, P2O5 = 4.00, K2O = 19.00
- NomeVigneto = Chardonnay 29/34_Ficuzza nord, t_ha = 6662.00, N = 18.00, P2O5 = 5.00, K2O = 20.00
- NomeVigneto = Chardonnay 33_Ficuzza nord, t_ha = 5170.00, N = 14.00, P2O5 = 4.00, K2O = 16.00
- NomeVigneto = Chardonnay 4_ficuzza est, t_ha = 4462.00, N = 12.00, P2O5 = 3.00, K2O = 13.00

**Mazzaporro (628)**
- anni = 2024, 2023, 2021
- righe = 1, vigneti = 1, somma_N = 16.00, somma_P2O5 = 4.00, somma_K2O = 18.00, media_t_ha = 6018.00
- NomeVigneto = Genovese Pinot Grigio, t_ha = 6018.00, N = 16.00, P2O5 = 4.00, K2O = 18.00

Query (per Mirafiore; per le altre cambia IdEnte e, nelle Asportazioni, l'anno):
```sql
SELECT STRING_AGG(CAST(Anno AS varchar(4)), ', ') WITHIN GROUP (ORDER BY Anno DESC) AS anni FROM (SELECT DISTINCT Anno FROM dbo.VistaAsportazioni WHERE IdEnte = 425) x;
SELECT COUNT(*) AS righe, COUNT(DISTINCT IdVigneto) AS vigneti, CAST(SUM(Asport_N_kg_ha_anno) AS decimal(12,2)) AS somma_N, CAST(SUM(Asport_P2O5_kg_ha_anno) AS decimal(12,2)) AS somma_P2O5, CAST(SUM(Asport_K2O_kg_ha_anno) AS decimal(12,2)) AS somma_K2O, CAST(AVG(Produzione_t_ha) AS decimal(12,2)) AS media_t_ha FROM dbo.VistaAsportazioni WHERE IdEnte = 425 AND Anno = 2025;
SELECT TOP 5 NomeVigneto, CAST(Produzione_t_ha AS decimal(10,2)) AS t_ha, CAST(Asport_N_kg_ha_anno AS decimal(10,2)) AS N, CAST(Asport_P2O5_kg_ha_anno AS decimal(10,2)) AS P2O5, CAST(Asport_K2O_kg_ha_anno AS decimal(10,2)) AS K2O FROM dbo.VistaAsportazioni WHERE IdEnte = 425 AND Anno = 2025 ORDER BY NomeVigneto;
```

## 10 Galleria
Filtri Anno 2026 + Azienda, gli altri vuoti: il portale mostra tutte le foto, dalla più recente (DataOraInserimento). Contare le foto, non i rilievi: un rilievo può avere più foto.

**Casa E. Mirafiore (425)**
- foto = 1304, rilievi = 1139, vigneti = 86, ultima = 2026-11-09 11:57
- TipoRilievo = FASE FENOLOGICA (BBCH), foto = 378
- TipoRilievo = SUPERFICIE FOGLIARE ESPOSTA (m²/vite), foto = 171
- TipoRilievo = STATO IDRICO DELLA PIANTA (classi), foto = 111

**Tenuta Ficuzza (2253)**
- foto = 1698, rilievi = 1505, vigneti = 41, ultima = 2026-11-10 09:52
- TipoRilievo = FASE FENOLOGICA (BBCH), foto = 618
- TipoRilievo = STATO IDRICO DELLA PIANTA (classi), foto = 299
- TipoRilievo = DESCRIZIONE DEL INTERFILA, foto = 124

**Mazzaporro (628)**
- foto = 1554, rilievi = 1251, vigneti = 19, ultima = 2026-09-24 17:00
- TipoRilievo = FASE FENOLOGICA (BBCH), foto = 448
- TipoRilievo = OIDIO Erysiphe Necator - Oidium Tuckeri, foto = 299
- TipoRilievo = PERONOSPORA - Plasmopara viticola, foto = 169

Query (per Mirafiore; per le altre cambia IdEnte e, nelle Asportazioni, l'anno):
```sql
SELECT COUNT(*) AS foto, COUNT(DISTINCT IdRilievo) AS rilievi, COUNT(DISTINCT IdVigneto) AS vigneti, CONVERT(varchar(16), MAX(DataOraInserimento), 120) AS ultima FROM dbo.VistaGalleriaFotografica WHERE IdAzienda = 425 AND Anno = 2026;
SELECT TOP 3 TipoRilievo, COUNT(*) AS foto FROM dbo.VistaGalleriaFotografica WHERE IdAzienda = 425 AND Anno = 2026 GROUP BY TipoRilievo ORDER BY COUNT(*) DESC, TipoRilievo;
```

## 3 Indicatori aziendali
Data scelta: 15/06/2026. Vigneti considerati: in carico, non estirpati, azienda di tipo 1, con il comune (VignetoMaps.Area3). «Probabilità» = massimo di ModelloInfezione.PercInfezione fra i vigneti dell'azienda in quel giorno (12 = peronospora, 11 = oidio). «Mio monitoraggio» (per la pressione) = rilievi dell'anno fino al giorno, tipo 12 o 11, foglia o grappolo, diffusione e intensità non vuote; valore = radice cubica(% vigneti con infezione × media dei massimi di diffusione × media dei massimi di intensità) × 0,5. La pressione finale aggiunge «Intorno a me» (× 0,3, media dei comuni) e «Fenologia» (× 0,2, dalla BBCH del modello): sono calcoli su tutti i vigneti del comune, qui non rifatti.

**Casa E. Mirafiore (425)**
- vigneti = 104, comuni = 13
- avversita = 12, max_infezione = 0.41, vigneti_con_modello = 89
- avversita = 11, max_infezione = 8.20, vigneti_con_modello = 89
- avversita = 12, vigneti_controllati = 71, con_infezione = 20, perc_presenza = 28.17, media_max_diff = 0.0375, media_max_int = 0.0007, mio_monitoraggio = 0.0453
- avversita = 11, vigneti_controllati = 62, con_infezione = 16, perc_presenza = 25.81, media_max_diff = 0.0587, media_max_int = 0.0010, mio_monitoraggio = 0.0568

**Tenuta Ficuzza (2253)**
- vigneti = 42, comuni = 3
- avversita = 12, max_infezione = 2.51, vigneti_con_modello = 41
- avversita = 11, max_infezione = 20.60, vigneti_con_modello = 41
- avversita = 12, vigneti_controllati = 34, con_infezione = 1, perc_presenza = 2.94, media_max_diff = 0.0059, media_max_int = 0.0000, mio_monitoraggio = 0.0000
- avversita = 11, vigneti_controllati = 34, con_infezione = 0, perc_presenza = 0.00, media_max_diff = 0.0000, media_max_int = 0.0000, mio_monitoraggio = 0.0000

**Mazzaporro (628)**
- vigneti = 19, comuni = 2
- avversita = 12, max_infezione = 2.68, vigneti_con_modello = 19
- avversita = 11, max_infezione = 25.96, vigneti_con_modello = 19
- avversita = 12, vigneti_controllati = 18, con_infezione = 17, perc_presenza = 94.44, media_max_diff = 2.2028, media_max_int = 0.0444, mio_monitoraggio = 1.0494
- avversita = 11, vigneti_controllati = 18, con_infezione = 18, perc_presenza = 100.00, media_max_diff = 17.1667, media_max_int = 1.0139, mio_monitoraggio = 6.0144

Query (per Mirafiore; per le altre cambia IdEnte e, nelle Asportazioni, l'anno):
```sql
SELECT COUNT(*) AS vigneti, COUNT(DISTINCT m.Area3) AS comuni FROM dbo.Vigneto v JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna JOIN dbo.Ente e ON e.IdEnte = vg.Ente_IdEnte JOIN dbo.VignetoMaps m ON m.IdVigneto = v.IdVigneto WHERE v.Carico = 1 AND v.Estirpo = 0 AND e.TipoEnte_idTipoEnte = 1 AND m.Area3 IS NOT NULL AND vg.Ente_IdEnte = 425;
SELECT mi.IdAvversitaDaTrattare AS avversita, CAST(MAX(mi.PercInfezione) AS decimal(10,2)) AS max_infezione, COUNT(DISTINCT mi.IdVigneto) AS vigneti_con_modello FROM dbo.ModelloInfezione mi JOIN dbo.Vigneto v ON v.IdVigneto = mi.IdVigneto JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna JOIN dbo.Ente e ON e.IdEnte = vg.Ente_IdEnte JOIN dbo.VignetoMaps m ON m.IdVigneto = v.IdVigneto WHERE mi.Data >= '2026-06-15' AND mi.Data < DATEADD(day, 1, '2026-06-15') AND v.Carico = 1 AND v.Estirpo = 0 AND e.TipoEnte_idTipoEnte = 1 AND m.Area3 IS NOT NULL AND vg.Ente_IdEnte = 425 AND mi.IdAvversitaDaTrattare IN (11, 12) GROUP BY mi.IdAvversitaDaTrattare ORDER BY 1 DESC;
WITH c AS (SELECT r.TipoRilievo_idTipoRilievo AS t, r.Vigneto_IdVigneto AS IdVigneto, SUM(CASE WHEN r.PercDiffusione > 0 THEN 1 ELSE 0 END) AS ConInfezione, MAX(CAST(r.PercDiffusione AS decimal(10,4))) AS MaxDiff, MAX(CAST(r.[PercIntensità] AS decimal(10,4))) AS MaxInt FROM dbo.Rilievo r WHERE r.TipoRilievo_idTipoRilievo IN (11, 12) AND r.PercDiffusione IS NOT NULL AND r.[PercIntensità] IS NOT NULL AND YEAR(r.InizioRilievo) = 2026 AND r.InizioRilievo < DATEADD(day, 1, '2026-06-15') AND r.Organo_idOrgano IN (1, 2) GROUP BY r.TipoRilievo_idTipoRilievo, r.Vigneto_IdVigneto), p AS (SELECT v.IdVigneto FROM dbo.Vigneto v JOIN dbo.Vigna vg ON vg.IdVigna = v.Vigna_IdVigna JOIN dbo.Ente e ON e.IdEnte = vg.Ente_IdEnte JOIN dbo.VignetoMaps m ON m.IdVigneto = v.IdVigneto WHERE v.Carico = 1 AND v.Estirpo = 0 AND e.TipoEnte_idTipoEnte = 1 AND m.Area3 IS NOT NULL AND vg.Ente_IdEnte = 425) SELECT c.t AS avversita, COUNT(*) AS vigneti_controllati, SUM(CASE WHEN c.ConInfezione > 0 THEN 1 ELSE 0 END) AS con_infezione, CAST(100.0 * SUM(CASE WHEN c.ConInfezione > 0 THEN 1 ELSE 0 END) / COUNT(*) AS decimal(10,2)) AS perc_presenza, CAST(AVG(c.MaxDiff) AS decimal(10,4)) AS media_max_diff, CAST(AVG(c.MaxInt) AS decimal(10,4)) AS media_max_int, CAST(POWER(100.0 * SUM(CASE WHEN c.ConInfezione > 0 THEN 1 ELSE 0 END) / COUNT(*) * AVG(c.MaxDiff) * AVG(c.MaxInt), 1.0/3) * 0.5 AS decimal(10,4)) AS mio_monitoraggio FROM c JOIN p ON p.IdVigneto = c.IdVigneto GROUP BY c.t ORDER BY 1 DESC;
```

## Da sapere per il confronto
- **Mirafiore** ha 105 vigneti in carico, ma negli Indicatori ne conta 104: uno non ha il comune in VignetoMaps (101695 «Nebbiolo Peggi 2026», verificato).
- **Ficuzza, mio monitoraggio**: 34 vigneti controllati e quasi nessuna infezione, quindi il valore è 0. È un buon caso
  per vedere che Mappe non divida per zero.
- **Mazzaporro**: quasi tutti i vigneti con infezione, valori alti. È il caso opposto.
- Le probabilità sono numeri del modello del giorno: se Dario rielabora i modelli, cambiano.

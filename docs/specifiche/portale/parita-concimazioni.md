# Parità delle CONCIMAZIONI (portale in Mappe, tappa 3): i numeri attesi (dba, 16/11/2026, task #268)

Anno **2026**, filtri della pagina: Anno + Azienda, nessun target, nessuna epoca, «solo con target» spento (come all'apertura).
Logica presa da `ControllersPortale/ConcimazioniController.cs` (UnitaConcimazione, DownloadUnitaConcimazione, FattoriConcimazione,
Resoconto); numeri calcolati in sola lettura sul 4g (letture.jsonl). Programma: `~/progetti/dba/uscite/parita-268/calcola.py`,
risultati grezzi `risultati.json`. N, P, K, MG, CA, S sono in kg/ha come nelle funzioni.

## Le funzioni FnT_* restituiscono solo l'azienda passata? **Sì**
Per tutte e tre le aziende, ognuna delle 5 funzioni restituisce **una sola azienda, quella passata, e zero righe di altre**:
FnT_FattoriConcimazione e _ByEnte filtrano `Vigna.Ente_IdEnte = @IdEnte`; _PerVigne e _PerAziende raggruppano _ByEnte;
FnT_DiffConcimazione_Elementi filtra `@IdEnte`. **Attenzione**: _DiffConcimazione con @IdEnte NULL restituisce **zero righe**
(non «tutte»): va sempre passata l'azienda. Il filtro in più di Mappe resta utile e non cambia niente.

- Botromagno (2269): FattoriConcimazione 19 righe / 1 azienda / 0 altre, ByEnte 19 righe / 1 azienda / 0 altre, PerVigne 8 righe / 1 azienda / 0 altre, PerAziende 3 righe / 1 azienda / 0 altre, DiffConcimazione 19 righe / 1 azienda / 0 altre
- Michele Satta (1173): FattoriConcimazione 31 righe / 1 azienda / 0 altre, ByEnte 31 righe / 1 azienda / 0 altre, PerVigne 14 righe / 1 azienda / 0 altre, PerAziende 4 righe / 1 azienda / 0 altre, DiffConcimazione 31 righe / 1 azienda / 0 altre
- Masciarelli - Provincia di Chieti (2279): FattoriConcimazione 42 righe / 1 azienda / 0 altre, ByEnte 42 righe / 1 azienda / 0 altre, PerVigne 12 righe / 1 azienda / 0 altre, PerAziende 3 righe / 1 azienda / 0 altre, DiffConcimazione 42 righe / 1 azienda / 0 altre

## Riepilogo per azienda
### Botromagno (IdEnte 2269)
- **Unità concimazione**: aziende 3 righe, vigne 8, vigneti 19 (di cui 0 senza target).
  Totali N/P/K/MG/CA/S: aziende 199/135/263/88/0/0; vigne 522/372/741/241/0/0; vigneti 1253/873/1745/578/0/0.
  Righe per azienda: VINO BIANCO STRUTTURATO 68/48/90/32/0/0; VINO ROSE' 72/45/63/25/0/0; VINO ROSSO STRUTTURATO 59/42/110/31/0/0.
- **Export Excel (3 fogli)**: Vigneti 19 righe, Vigne 8, Aziende 3. Le viste di export **non** contano i vigneti e le righe senza target.
- **Fattori concimazione**: 19 righe (vigneti non estirpati), 19 con target; superficie totale 348101.39 (unità della colonna Superficie, m²); media vigoria 3.778, media acidità 7.620; 18 con dati del suolo.
- **Target enologico** (UnitaBaseConcimazione 2026): 13 vigneti VINO BIANCO STRUTTURATO, 4 VINO ROSSO STRUTTURATO, 2 VINO ROSE'.
- **Resoconto**: 19 righe (una per vigneto di _ByEnte), 19 con le differenze. Somme delle differenze N/P/K/MG = -303.00/-873.00/-1745.00/-578.00 (CA e S vuote); concime dato N/P2O5/K2O = 950.00/0.00/0.00.
  Concimazione reale (VistaConcimazioneReale): 32 righe su 19 vigneti, N/P2O5/K2O/MgO = 950.00/0.00/0.00/0.00; per target (VistaConcimazioneRealePerTarget): 4 righe, 150.00/0.00/0.00/0.00.

### Michele Satta (IdEnte 1173)
- **Unità concimazione**: aziende 4 righe, vigne 14, vigneti 31 (di cui 3 senza target).
  Totali N/P/K/MG/CA/S: aziende 111/83/210/61/0/0; vigne 410/302/871/194/0/0; vigneti 1121/712/2227/457/0/0.
  Righe per azienda: None -/-/-/-/-/-; VINO BIANCO PRONTA BEVA 58/25/63/14/0/0; VINO BIANCO STRUTTURATO 21/32/59/30/0/0; VINO ROSSO STRUTTURATO 32/26/88/17/0/0.
- **Export Excel (3 fogli)**: Vigneti 28 righe, Vigne 11, Aziende 3. Le viste di export **non** contano i vigneti e le righe senza target.
- **Fattori concimazione**: 31 righe (vigneti non estirpati), 28 con target; superficie totale 260709.55 (unità della colonna Superficie, m²); media vigoria 6.296, media acidità 5.246; 26 con dati del suolo.
- **Target enologico** (UnitaBaseConcimazione 2026): 7 VINO BIANCO PRONTA BEVA, 1 VINO BIANCO STRUTTURATO, 23 VINO ROSSO STRUTTURATO.
- **Resoconto**: 31 righe (una per vigneto di _ByEnte), 31 con le differenze. Somme delle differenze N/P/K/MG = 55.00/-124.00/-1051.00/-289.00 (CA e S vuote); concime dato N/P2O5/K2O = 1176.00/588.00/1176.00.
  Concimazione reale (VistaConcimazioneReale): 31 righe su 31 vigneti, N/P2O5/K2O/MgO = 1302.00/651.00/1302.00/186.00; per target (VistaConcimazioneRealePerTarget): 3 righe, 126.00/63.00/126.00/18.00.

### Masciarelli - Provincia di Chieti (IdEnte 2279)
- **Unità concimazione**: aziende 3 righe, vigne 12, vigneti 42 (di cui 4 senza target).
  Totali N/P/K/MG/CA/S: aziende 100/89/226/51/0/0; vigne 463/392/995/235/0/0; vigneti 1936/1701/4283/958/0/0.
  Righe per azienda: None -/-/-/-/-/-; VINO BIANCO STRUTTURATO 59/46/110/28/0/0; VINO ROSSO STRUTTURATO 41/43/116/23/0/0.
- **Export Excel (3 fogli)**: Vigneti 38 righe, Vigne 9, Aziende 2. Le viste di export **non** contano i vigneti e le righe senza target.
- **Fattori concimazione**: 42 righe (vigneti non estirpati), 38 con target; superficie totale 516441.91 (unità della colonna Superficie, m²); media vigoria 4.590, media acidità 8.300; 41 con dati del suolo.
- **Target enologico** (UnitaBaseConcimazione 2026): 21 VINO BIANCO STRUTTURATO, 17 VINO ROSSO STRUTTURATO.
- **Resoconto**: 42 righe (una per vigneto di _ByEnte), 42 con le differenze. Somme delle differenze N/P/K/MG = -1936.00/-1701.00/-4283.00/-958.00 (CA e S vuote); concime dato N/P2O5/K2O = 0.00/0.00/0.00.
  Concimazione reale (VistaConcimazioneReale): 20 righe su 20 vigneti, N/P2O5/K2O/MgO = 0.00/0.00/0.00/0.00; per target (VistaConcimazioneRealePerTarget): 1 righe, 0.00/0.00/0.00/0.00.

## Da sapere per il confronto
- **Righe senza target**: nella pagina compaiono (filtro «solo con target» spento), nell'export Excel no. Michele Satta ha 3 vigneti
  senza target, Masciarelli 4: è la differenza fra le righe della pagina e quelle del foglio Vigneti.
- **Differenze negative** = concime dato meno del fabbisogno. Masciarelli ha 20 concimazioni reali registrate ma con N, P2O5, K2O e
  MgO tutti a zero: le differenze sono quindi pari al fabbisogno con il segno meno. Da far notare all'agronomo, non è un errore di Mappe.
- CA e S valgono 0 in tutti i target di queste aziende; le differenze per CA e S sono vuote (la funzione non le calcola qui).
- I numeri della pagina Unità e del Resoconto sono gli stessi (_ByEnte), il Resoconto aggiunge le differenze per vigneto.

## Query (per Botromagno, 2269; per le altre cambia l'IdEnte)
**isolamento**
```sql
SELECT 'FattoriConcimazione' f, COUNT(*) righe, COUNT(DISTINCT IdEnte) aziende, SUM(CASE WHEN IdEnte <> 2269 THEN 1 ELSE 0 END) altre FROM dbo.FnT_FattoriConcimazione(2026,2269) UNION ALL SELECT 'ByEnte', COUNT(*), COUNT(DISTINCT IdEnte), SUM(CASE WHEN IdEnte <> 2269 THEN 1 ELSE 0 END) FROM dbo.FnT_FattoriConcimazione_ConVariazione_ByEnte(2026,2269) UNION ALL SELECT 'PerVigne', COUNT(*), COUNT(DISTINCT IdEnte), SUM(CASE WHEN IdEnte <> 2269 THEN 1 ELSE 0 END) FROM dbo.FnT_FattoriConcimazione_ConVariazione_PerVigne(2026,2269) UNION ALL SELECT 'PerAziende', COUNT(*), COUNT(DISTINCT IdEnte), SUM(CASE WHEN IdEnte <> 2269 THEN 1 ELSE 0 END) FROM dbo.FnT_FattoriConcimazione_ConVariazione_PerAziende(2026,2269) UNION ALL SELECT 'DiffConcimazione', COUNT(*), COUNT(DISTINCT IdEnte), SUM(CASE WHEN IdEnte <> 2269 THEN 1 ELSE 0 END) FROM dbo.FnT_DiffConcimazione_Elementi(2026,2269,NULL,NULL);
```
**unita_aziende**
```sql
SELECT COUNT(*) righe, SUM(N_var) N, SUM(P_var) P, SUM(K_var) K, SUM(MG_var) MG, SUM(CA_var) CA, SUM(S_var) S FROM dbo.FnT_FattoriConcimazione_ConVariazione_PerAziende(2026,2269) WHERE IdEnte = 2269;
```
**unita_vigne**
```sql
SELECT COUNT(*) righe, SUM(N_var) N, SUM(P_var) P, SUM(K_var) K, SUM(MG_var) MG, SUM(CA_var) CA, SUM(S_var) S FROM dbo.FnT_FattoriConcimazione_ConVariazione_PerVigne(2026,2269) WHERE IdEnte = 2269;
```
**unita_vigneti**
```sql
SELECT COUNT(*) righe, SUM(CASE WHEN IdTarget IS NULL THEN 1 ELSE 0 END) senza_target, SUM(N_var) N, SUM(P_var) P, SUM(K_var) K, SUM(MG_var) MG, SUM(CA_var) CA, SUM(S_var) S FROM dbo.FnT_FattoriConcimazione_ConVariazione_ByEnte(2026,2269) WHERE IdEnte = 2269;
```
**export**
```sql
SELECT (SELECT COUNT(*) FROM dbo.VistaExportFattoriConcimazioneVigneti_IT WHERE _IdAzienda = 2269 AND _Anno = 2026) foglio_vigneti, (SELECT COUNT(*) FROM dbo.VistaExportFattoriConcimazioneVigne_IT WHERE _IdAzienda = 2269 AND _Anno = 2026) foglio_vigne, (SELECT COUNT(*) FROM dbo.VistaExportFattoriConcimazioneAziende_IT WHERE _IdAzienda = 2269 AND _Anno = 2026) foglio_aziende;
```
**fattori**
```sql
SELECT COUNT(*) righe, SUM(CASE WHEN IdTargetEnologico IS NOT NULL THEN 1 ELSE 0 END) con_target, CAST(SUM(Superficie) AS decimal(12,2)) superficie, CAST(AVG(CAST(Vigoria AS float)) AS decimal(10,3)) media_vigoria, CAST(AVG(CAST(AciditaTotale AS float)) AS decimal(10,3)) media_acidita, SUM(CASE WHEN SO IS NOT NULL THEN 1 ELSE 0 END) con_suolo FROM dbo.FnT_FattoriConcimazione(2026,2269) WHERE IdEnte = 2269;
```
**resoconto_righe**
```sql
SELECT COUNT(*) righe, SUM(CASE WHEN d.IdVigneto IS NOT NULL THEN 1 ELSE 0 END) con_differenza, CAST(SUM(d.Diff_N) AS decimal(12,2)) Diff_N, CAST(SUM(d.Diff_P) AS decimal(12,2)) Diff_P, CAST(SUM(d.Diff_K) AS decimal(12,2)) Diff_K, CAST(SUM(d.Diff_MG) AS decimal(12,2)) Diff_MG, CAST(SUM(d.Diff_CA) AS decimal(12,2)) Diff_CA, CAST(SUM(d.Diff_S) AS decimal(12,2)) Diff_S, CAST(SUM(d.N_Calc_Somma) AS decimal(12,2)) N_dato, CAST(SUM(d.P2O5_Calc_Somma) AS decimal(12,2)) P2O5_dato, CAST(SUM(d.K2O_Calc_Somma) AS decimal(12,2)) K2O_dato FROM dbo.FnT_FattoriConcimazione_ConVariazione_ByEnte(2026,2269) b LEFT JOIN (SELECT * FROM dbo.FnT_DiffConcimazione_Elementi(2026,2269,NULL,NULL) WHERE IdEnte = 2269) d ON d.IdVigneto = b.IdVigneto;
```
**concimazione_reale**
```sql
SELECT COUNT(*) righe, COUNT(DISTINCT IdVigneto) vigneti, CAST(SUM(N_Calc) AS decimal(12,2)) N, CAST(SUM(P2O5_Calc) AS decimal(12,2)) P2O5, CAST(SUM(K2O_Calc) AS decimal(12,2)) K2O, CAST(SUM(MGO_Calc) AS decimal(12,2)) MGO FROM dbo.VistaConcimazioneReale WHERE Anno = 2026 AND IdEnte = 2269;
```
**concimazione_reale_target**
```sql
SELECT COUNT(*) righe, CAST(SUM(N_Calc) AS decimal(12,2)) N, CAST(SUM(P2O5_Calc) AS decimal(12,2)) P2O5, CAST(SUM(K2O_Calc) AS decimal(12,2)) K2O, CAST(SUM(MGO_Calc) AS decimal(12,2)) MGO FROM dbo.VistaConcimazioneRealePerTarget WHERE Anno = 2026 AND IdEnte = 2269;
```
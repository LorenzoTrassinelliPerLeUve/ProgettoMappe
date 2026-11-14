# REPORT SETTIMANALE - LAORE: le formule raggruppate in schemi

40 misure nelle slide del report IT, 38 formule diverse, raggruppate in 6 schemi. Il portale implementa gli **schemi** (una funzione ciascuno, con parametri), non le formule una per una.
Classificazione automatica per regole (genera_struttura.py, lista SCHEMI): l'ordine delle regole conta; i casi dubbi finiscono in S99 e vanno guardati a mano.

Parametri comuni a quasi tutti gli schemi meteo: **fase** (`ID_FASE_FENOLOGICA` 1-4, date fisse: vedi regole-script.md), **anno** (`Max(Anno_MeteoGiornaliero)` o annata agraria), **finestra storica** (ultimi 10 anni: `Anno >= Max-10 <= Max-1`), **confronto «a oggi»** (`Meteo_Confronto_AdOggi=1`: solo i giorni fino alla data di oggi anche negli anni passati), vigneti con storico meteo (`DatiMeteoStorico=1`). Aggregazione: prima per **vigneto e anno** (`Aggr(…, IdVigneto, Anno)`), poi **media sui vigneti** dell'azienda.

## S24 — Malattie NON pesate: massimo dell'anno per vigneto (organo, tipo rilievo), poi media semplice sui vigneti (/100)
9 misure (9 formule diverse). Esempi:
- ×1 `If(avg(aggr(max({<idTipoRilievo={'13'},Anno={"$(=Max(Anno))"}>}PercDiffusione),[IdVigneto],[Anno]))>0, 1, 0) `
- ×1 `AVG(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}[PercIntensità]),[IdVigneto],[Anno]))/100 `
- ×1 `AVG(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}PercDiffusione),[IdVigneto],[Anno]))/100 `

## S25 — Trappole: catture medie per trappola per settimana (Lobesia 148, Cryptoblabes 153)
2 misure (2 formule diverse). Esempi:
- ×1 `avg(aggr(avg({<idTipoRilievo={'153'},Anno={"$(=Max(Anno))"}>}CatturePerTrappola),WEEK_REF,[IdVigneto])) //153 CRYPTOBLABES `
- ×1 `avg(aggr(avg({<idTipoRilievo={'148'},Anno={"$(=Max(Anno))"}>}CatturePerTrappola),WEEK_REF,[IdVigneto])) //153 CRYPTOBLABES //148 Lobesia `

## S26 — Indicatore di presenza (1/0): c'è almeno un rilievo o un valore > 0 di quel tipo nell'anno
1 misure (1 formule diverse). Esempi:
- ×1 `If(Count({<idTipoRilievo={"9"},Anno={"$(=Max(Anno))"}>}IdRilievo)>0, 1, 0)`

## S14 — Fenologia: fase o data dal modello BBCH / dai rilievi BBCH
8 misure (7 formule diverse). Esempi:
- ×2 `avg(aggr(Avg({<[Anno]={"$(=Max(Anno))"},BBCH_Rilievo={'53','55','57','60','61','62','63','64','65','66','67','68','69','70','72','73','75','77','79','80','81','82','83','84','85','89'}>}BBCH_Rilievo),WEEK_REF,IdVigneto,Anno)) `
- ×1 `avg(aggr(Avg({<[Anno]={"$(=Max(Anno))"},BBCH_Rilievo={'10','11','12','13','14','15','16','17','18','19','20','21','22','23','24','25'}>}BBCH_Rilievo),WEEK_REF,IdVigneto,Anno)) `
- ×1 ` avg(aggr(Avg({<[Anno]={"$(=Max(Anno))"},BBCH_Rilievo={'10','11','12','13','14','15','16','17','18','19','20','21','22','23','24','25'}>}BBCH_Rilievo),WEEK_REF,IdVigneto,Anno)) `

## S15 — Conteggi di rilievi, organi o vigneti
19 misure (18 formule diverse). Esempi:
- ×2 `Count({<Anno={"$(=Max(Anno))"}>} distinct IdRilievo)`
- ×1 `sum({<Anno={"$(=Max(Anno))"}>} NrOrganiControllati)`
- ×1 `Count({<Anno={"$(=Max(Anno))"}>} distinct IdVignetoDaApp)`

## S99 — Altro (semplice: media/somma di un campo con filtro)
1 misure (1 formule diverse). Esempi:
- ×1 `maxstring({<Anno={"$(=Max(Anno))"}>} WEEK_REF)`


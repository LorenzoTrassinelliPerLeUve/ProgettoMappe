# REPORT SETTIMANALE: le formule raggruppate in schemi

192 misure nelle slide del report IT, 179 formule diverse, raggruppate in 13 schemi. Il portale implementa gli **schemi** (una funzione ciascuno, con parametri), non le formule una per una.
Classificazione automatica per regole (genera_struttura.py, lista SCHEMI): l'ordine delle regole conta; i casi dubbi finiscono in S99 e vanno guardati a mano.

Parametri comuni a quasi tutti gli schemi meteo: **fase** (`ID_FASE_FENOLOGICA` 1-4, date fisse: vedi regole-script.md), **anno** (`Max(Anno_MeteoGiornaliero)` o annata agraria), **finestra storica** (ultimi 10 anni: `Anno >= Max-10 <= Max-1`), **confronto «a oggi»** (`Meteo_Confronto_AdOggi=1`: solo i giorni fino alla data di oggi anche negli anni passati), vigneti con storico meteo (`DatiMeteoStorico=1`). Aggregazione: prima per **vigneto e anno** (`Aggr(…, IdVigneto, Anno)`), poi **media sui vigneti** dell'azienda.

## S01 — Testo dinamico: titolo o frase composta (='testo' & valori)
16 misure (12 formule diverse). Esempi:
- ×2 `='Sanità Grappoli' `
- ×2 `='Sanità Foglie' `
- ×2 `='Fenologia - Confronto Vitigni' `

## S02 — Classe testuale da soglie (IF a cascata su un valore medio)
6 misure (6 formule diverse). Esempi:
- ×1 `IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria))"}>}Punteggio),IdVigneto))=10,('Vigoria Eccessiva'), IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria))"}>}Punteggio),IdVigneto))>=8.8,('Vigoria Molto Alta'), IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria))"}>}Punteggio),IdVigneto))>=7.5,('Vigoria Alta'), IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria))"}>}Punteggio),IdVigneto)`
- ×1 `IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria)-1)"}>}Punteggio),IdVigneto))=10,('Vigoria Eccessiva'), IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria)-1)"}>}Punteggio),IdVigneto))>=8.8,('Vigoria Molto Alta'), IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria)-1)"}>}Punteggio),IdVigneto))>=7.5,('Vigoria Alta'), IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria)-1)"}>}Punteggio),Id`
- ×1 `IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria)-2)"}>}Punteggio),IdVigneto))=10,('Vigoria Eccessiva'), IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria)-2)"}>}Punteggio),IdVigneto))>=8.8,('Vigoria Molto Alta'), IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria)-2)"}>}Punteggio),IdVigneto))>=7.5,('Vigoria Alta'), IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria)-2)"}>}Punteggio),Id`

## S18b — Tignole per generazione (vedi S18)
6 misure (6 formule diverse). Esempi:
- ×1 `if( (Avg(aggr(Max({<idEtaEpocaGenerazione={"1330","430"},Anno={"$(=Max(Anno))"}>}PercDiffusione ),IdVigneto))) >0, (Avg(aggr(Max({<idEtaEpocaGenerazione={"1330","430"},Anno={"$(=Max(Anno))"}>}PercDiffusione ),IdVigneto))), 0) `
- ×1 `if( (Avg(aggr(Max({<idEtaEpocaGenerazione={"1320","420"},Anno={"$(=Max(Anno))"}>}PercDiffusione ),IdVigneto))) >0, (Avg(aggr(Max({<idEtaEpocaGenerazione={"1320","420"},Anno={"$(=Max(Anno))"}>}PercDiffusione ),IdVigneto))), 0) `
- ×1 `if( (Avg(aggr(Max({<idEtaEpocaGenerazione={"1310","410"},Anno={"$(=Max(Anno))"}>}PercDiffusione ),IdVigneto))) >0, (Avg(aggr(Max({<idEtaEpocaGenerazione={"1310","410"},Anno={"$(=Max(Anno))"}>}PercDiffusione ),IdVigneto))), 0) `

## S21 — Copertura dei rilievi: % di superficie dell'azienda rilevata nell'anno per tipo di rilievo (ettari rilevati / ettari totali)
19 misure (13 formule diverse). Esempi:
- ×2 `(sum(Aggr(avg({<idTipoRilievo={'73'},Anno={"$(=Max(Anno))"}>}Superficie),IdVigneto))/10000) / (sum(Aggr(avg(Superficie),IdVigneto))/10000)`
- ×2 `(sum(Aggr(avg({<idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}Superficie),IdVigneto))/10000) / (sum(Aggr(avg(Superficie),IdVigneto))/10000)`
- ×2 `(sum(Aggr(avg({<idTipoRilievo={'30'},Anno={"$(=Max(Anno))"}>}Superficie),IdVigneto))/10000) / (sum(Aggr(avg(Superficie),IdVigneto))/10000)`

## S22 — % di vigneti senza avversità per tipo di rilievo (conteggio con PresenzaAvversitaCarenza=0 / conteggio rilievi)
19 misure (19 formule diverse). Esempi:
- ×1 `sum(Aggr(count({<idOrgano={'2','5'},idTipoRilievo={'73'},Anno={"$(=Max(Anno))"},PresenzaAvversitaCarenza={'0'}>}NomeVigneto),IdVigneto)) / sum(Aggr(count({<idTipoRilievo={'73'},Anno={"$(=Max(Anno))"}>}NomeVigneto),IdVigneto)) `
- ×1 `sum(Aggr(count({<idOrgano={'2','5'},idTipoRilievo={'12'},Anno={"$(=Max(Anno))"},PresenzaAvversitaCarenza={'0'}>}NomeVigneto),IdVigneto)) / sum(Aggr(count({<idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}NomeVigneto),IdVigneto)) `
- ×1 `sum(Aggr(count({<idOrgano={'2','5'},idTipoRilievo={'86'},Anno={"$(=Max(Anno))"},PresenzaAvversitaCarenza={'0'}>}NomeVigneto),IdVigneto)) / sum(Aggr(count({<idTipoRilievo={'86'},Anno={"$(=Max(Anno))"}>}NomeVigneto),IdVigneto)) `

## S23 — Danno o forme mobili/nidi pesati sulla superficie (massimo per vigneto × superficie / superficie)
2 misure (2 formule diverse). Esempi:
- ×1 `(sum(aggr(max([PercDanno]*Superficie),[IdVigneto])) / (sum(Aggr(avg(Superficie_Ampelopatie),IdVigneto)))) /100 `
- ×1 `if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'13'},Anno={"$(=Max(Anno))"}>}[NrFormeMobiliNidi]*Superficie),[IdVigneto],[Anno])) / (sum(Aggr(avg(Superficie),IdVigneto))) ) >0, (sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'13'},Anno={"$(=Max(Anno))"}>}[NrFormeMobiliNidi]*Superficie),[IdVigneto],[Anno])) / (sum(Aggr(avg(Superficie),IdVigneto))) ),0) *100`

## S03 — Malattie: intensità o diffusione media pesata sulla superficie dei vigneti rilevati (organo, tipo rilievo, anno)
57 misure (56 formule diverse). Esempi:
- ×2 `if((sum(aggr(max({<idTipoRilievo={'168'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno])) / (sum(Aggr(avg(Superficie),IdVigneto))) /100) >0, (sum(aggr(max({<idTipoRilievo={'168'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno])) / (sum(Aggr(avg(Superficie),IdVigneto))) /100),0)`
- ×1 `1- //BOTRITE ( if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'73'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno])) / (sum(Aggr(avg(Superficie),IdVigneto))) /100) >0, (sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'73'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno])) / (sum(Aggr(avg(Superficie),IdVigneto))) /100),0) //MARCIUMI + if((sum(`
- ×1 `if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno])) / (sum(Aggr(avg(Superficie),IdVigneto))) /100) >0, (sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno])) / (sum(Aggr(avg(Superficie),IdVigneto))) /100),0)`

## S10 — Indice Bigot e parametri (punteggi/valori dell'anno, vigneti con 9 parametri)
4 misure (4 formule diverse). Esempi:
- ×1 `avg(aggr(avg(Punteggio),IdVigneto,Anno_Vigoria)) `
- ×1 `avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria))"}>}Punteggio),IdVigneto))`
- ×1 `avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria)-2)"}>}Punteggio),IdVigneto))`

## S12 — Stato idrico e potenziale idrico (valutazioni dei rilievi, pre-alba)
4 misure (4 formule diverse). Esempi:
- ×1 `avg(aggr(avg({<idTipoRilievo={'146'},Anno_stress={"$(=Max(Anno_stress))"},Mese={'Mag','Giu','Lug','Ago','Set'}>}IdValutazioneStatoIdrico),IdVigneto,WEEK_REF_STRESS)) * -1 `
- ×1 `avg(aggr(max({<idTipoRilievo={'146'},Anno_stress={"$(=Max(Anno_stress))"},Mese={'Mag','Giu','Lug','Ago','Set'}>}IdValutazioneStatoIdrico),IdVigneto)) `
- ×1 `avg(aggr(max({<idTipoRilievo={'146'},Anno_stress={"$(=Max(Anno_stress)-1)"},Mese={'Mag','Giu','Lug','Ago','Set'}>}IdValutazioneStatoIdrico),IdVigneto))`

## S13 — Produzione e vendemmia (kg, quintali/ha, pesi, date di vendemmia)
5 misure (5 formule diverse). Esempi:
- ×1 `avg(aggr(Avg(Day_Ref_Vendemmia),Anno_Vendemmia,IdVigneto)) `
- ×1 `avg(aggr(Avg({<Anno_Vendemmia={"$(=Max(Anno_Vendemmia))"}>}Day_Ref_Vendemmia),Anno_Vendemmia,IdVigneto))`
- ×1 `avg(aggr(Avg({<Anno_Vendemmia={"$(=Max(Anno_Vendemmia)-1)"}>}Day_Ref_Vendemmia),Anno_Vendemmia,IdVigneto))`

## S14 — Fenologia: fase o data dal modello BBCH / dai rilievi BBCH
23 misure (22 formule diverse). Esempi:
- ×2 `avg(aggr(Avg({<[Anno]={"$(=Max(Anno))"},BBCH_Rilievo={'53','55','57','60','61','62','63','64','65','66','67','68','69','70','72','73','75','77','79','80','81','82','83','84','85'}>}BBCH_Rilievo),WEEK_REF,IdVigneto,Anno)) `
- ×1 `//avg(aggr(Avg({<BBCH_Modello={'7','8','9'}>} Day_Ref_ModelloBBCH),Anno_BBCH,IdVigneto)) avg( aggr( if( Count({<BBCH_Modello={'9'}>} Day_Ref_ModelloBBCH) > 0, Avg({<BBCH_Modello={'9'}>} Day_Ref_ModelloBBCH), if( Count({<BBCH_Modello={'8'}>} Day_Ref_ModelloBBCH) > 0, Avg({<BBCH_Modello={'8'}>} Day_Ref_ModelloBBCH), Avg({<BBCH_Modello={'7'}>} Day_Ref_ModelloBBCH) ) ), Anno_BBCH, IdVigneto ) ) //23/0`
- ×1 `//avg(aggr(Avg({<BBCH_Modello={'63','64','65','66','67','68'}>} Day_Ref_ModelloBBCH),Anno_BBCH,IdVigneto)) avg( aggr( if( Count({<BBCH_Modello={'68'}>} Day_Ref_ModelloBBCH) > 0, Avg({<BBCH_Modello={'68'}>} Day_Ref_ModelloBBCH), if( Count({<BBCH_Modello={'67'}>} Day_Ref_ModelloBBCH) > 0, Avg({<BBCH_Modello={'67'}>} Day_Ref_ModelloBBCH), if( Count({<BBCH_Modello={'66'}>} Day_Ref_ModelloBBCH) > 0, Av`

## S15 — Conteggi di rilievi, organi o vigneti
21 misure (20 formule diverse). Esempi:
- ×2 `Count({<Anno={"$(=Max(Anno))"}>} distinct IdRilievo)`
- ×1 `sum({<Anno={"$(=Max(Anno))"}>} NrOrganiControllati)`
- ×1 ` Count({<Anno={"$(=Max(Anno))"}>} distinct IdVignetoDaApp)`

## S99 — Altro (semplice: media/somma di un campo con filtro)
10 misure (10 formule diverse). Esempi:
- ×1 `max(Anno)-1`
- ×1 `max(Anno)`
- ×1 `max(Anno)-2`


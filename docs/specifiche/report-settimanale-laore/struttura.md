# REPORT SETTIMANALE - LAORE: struttura foglio per foglio

Task #216, tappa 1 · generato da `genera_struttura.py` dalle proprietà delle app (Engine API, senza dati).
Nessuna automazione PowerPoint usa questa app: ordine = ordine dei fogli nell'app (17 fogli). Nessuna versione ENG. Per ogni oggetto: tipo e id, titoli e testi IT (e ENG se diversi, stesso id: l'app ENG è una copia di quella IT), dimensioni, misure con lo **schema** [Sxx] (vedi schemi.md) e il formato, contachilometri con scala e fasce.

**Selezioni che il report presuppone**: quelle dell'utente al clic su «CREA REPORT» (segnalibro passato all'automazione): **un'azienda** (RagioneSociale) e **la settimana** (`Week`), più a scelta vigneto, comune/provincia/regione/stato, tecnico (`Nome Cognome`), anno d'impianto, descrizione, data (foglio «Filtri»). Le formule usano la settimana scelta e le settimane di riferimento WEEK_REF* (vedi regole-script.md).

**Variabili dell'app (IT)**:  · ENG: 

## Slide 1. Filtri
- **table** `tVdyY` — «Nome Report - Filtri - Week Ref»
  - dimensione: `AdministrativeAreaLevel2` (Provincia)
  - misura [S26] «MalEsca» · formato automatico: `If(Count({<idTipoRilievo={"9"},Anno={"$(=Max(Anno))"}>}IdRilievo)>0, 1, 0)`
  - misura [S24] «Tignole» · formato automatico: `If(avg(aggr(max({<idTipoRilievo={'13'},Anno={"$(=Max(Anno))"}>}PercDiffusione),[IdVigneto],[Anno]))>0, 1, 0)



`
  - misura [S99] «WeekRef» · formato automatico: `maxstring({<Anno={"$(=Max(Anno))"}>} WEEK_REF)`

## Slide 2. Dashboard
- **barchart** `b8f73493-725b-447d-953f-3bdf0406059f` — «=='Numerosità di rilievi eseguiti per tipologia'»
  - dimensione: `=TipoRilevo`
  - misura [S15] «n° rilievi» · formato automatico: `Count({<Anno={"$(=Max(Anno))"}>} distinct IdRilievo)`
- **kpi** `KycFP`
  - misura [S15] «N° Rilievi» · formato automatico: `Count({<Anno={"$(=Max(Anno))"}>} distinct IdRilievo)`
- **kpi** `PQHHAry`
  - misura [S15] «N° organi controllati» · formato automatico: `sum({<Anno={"$(=Max(Anno))"}>}  NrOrganiControllati)`
- **kpi** `yDrqWJR`
  - misura [S15] «N° Vigneti Monitorati» · formato automatico: `Count({<Anno={"$(=Max(Anno))"}>} distinct IdVignetoDaApp)`

## Slide 3. Confronto BBCH  Anno in  Corso - Foglie
- **linechart** `eXwSs` — «Fenologia - Foglie»
  - dimensione: `=WEEK_REF` (Settimana)
  - dimensione: `=AdministrativeAreaLevel2` (Provincia)
  - misura [S14] «BBCH» · formato automatico: `avg(aggr(Avg({<[Anno]={"$(=Max(Anno))"},BBCH_Rilievo={'10','11','12','13','14','15','16','17','18','19','20','21','22','23','24','25'}>}BBCH_Rilievo),WEEK_REF,IdVigneto,Anno))
`

## Slide 4. Confronto BBCH  Anno in  Corso - Grappoli
- **linechart** `ba0f2f43-0125-4584-a408-2636db088fa5` — «Fenologia - Grappoli»
  - dimensione: `=WEEK_REF` (Settimana)
  - dimensione: `AdministrativeAreaLevel2` (Provincia)
  - misura [S14] «BBCH» · formato automatico: `avg(aggr(Avg({<[Anno]={"$(=Max(Anno))"},BBCH_Rilievo={'53','55','57','60','61','62','63','64','65','66','67','68','69','70','72','73','75','77','79','80','81','82','83','84','85','89'}>}BBCH_Rilievo),WEEK_REF,IdVigneto,Anno))
`

## Slide 5. Confronto BBCH  Storico - Foglie
- **linechart** `vhrtBq` — «Fenologia - Foglie»
  - dimensione: `=//week_Ref_ModelloBBCH
WEEK_REF` (Settimana)
  - misura [S14] «BBCH Anno in Corso» · formato automatico: `
avg(aggr(Avg({<[Anno]={"$(=Max(Anno))"},BBCH_Rilievo={'10','11','12','13','14','15','16','17','18','19','20','21','22','23','24','25'}>}BBCH_Rilievo),WEEK_REF,IdVigneto,Anno))
`
  - misura [S14] «BBCH Anno Precedente» · formato automatico: `avg(aggr(Avg({<[Anno]={"$(=Max(Anno)-1)"},BBCH_Rilievo={'10','11','12','13','14','15','16','17','18','19','20','21','22','23','24','25'}>}BBCH_Rilievo),WEEK_REF,IdVigneto,Anno))`
  - misura [S14] «BBCH Media Storica» · formato automatico: `avg(aggr(Avg({<[Anno]={">=$(=Max(Anno)-5)		<=$(=Max(Anno)-1)"},BBCH_Rilievo={'10','11','12','13','14','15','16','17','18','19','20','21','22','23','24','25'}>}BBCH_Rilievo),WEEK_REF,IdVigneto,Anno))

`

## Slide 6. Confronto BBCH  Storico - Grappoli
- **linechart** `96503fe8-3fa9-4d22-93f0-3665d4ef4f26` — «Fenologia - Grappoli»
  - dimensione: `=WEEK_REF` (Settimana)
  - misura [S14] «BBCH Anno in Corso» · formato automatico: `avg(aggr(Avg({<[Anno]={"$(=Max(Anno))"},BBCH_Rilievo={'53','55','57','60','61','62','63','64','65','66','67','68','69','70','72','73','75','77','79','80','81','82','83','84','85','89'}>}BBCH_Rilievo),WEEK_REF,IdVigneto,Anno))
`
  - misura [S14] «BBCH Anno Precedente» · formato automatico: `avg(aggr(Avg({<[Anno]={"$(=Max(Anno)-1)"},BBCH_Rilievo={'53','55','57','60','61','62','63','64','65','66','67','68','69','70','72','73','75','77','79','80','81','82','83','84','85','89'}>}BBCH_Rilievo),WEEK_REF,IdVigneto,Anno))`
  - misura [S14] «BBCH Media Storica» · formato automatico: `avg(aggr(Avg({<[Anno]={">=$(=Max(Anno)-5)		<=$(=Max(Anno)-1)"},BBCH_Rilievo={'53','55','57','60','61','62','63','64','65','66','67','68','69','70','72','73','75','77','79','80','81','82','83','84','85','89'}>}BBCH_Rilievo),WEEK_REF,IdVigneto,Anno))
`

## Slide 7. Peronospora Intentistà Grafico Foglia
- **barchart** `15823e33-903a-49c8-9dcb-21b296a63af5` — «=='Danno da Peronospora su Foglia'&' '&[Anno]»
  - dimensione: `=AdministrativeAreaLevel2` (Provincia)
  - misura [S24] «Peronospora» · formato automatico: `AVG(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}[PercIntensità]),[IdVigneto],[Anno]))/100
`

## Slide 8. Peronospora Diffusione Grafico Foglia
- **barchart** `5e69da73-df99-4b71-a6da-2ee0862f27b4` — «=='Diffusione Peronospora su Foglia'&' '&[Anno]»
  - dimensione: `=AdministrativeAreaLevel2` (Provincia)
  - misura [S24] «Peronospora» · formato automatico: `AVG(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}PercDiffusione),[IdVigneto],[Anno]))/100
`

## Slide 9. Oidio Intentistà Grafico Foglia
- **barchart** `3974a772-1d08-4f35-9024-cbae48075839` — «=='Danno da Oidio su Foglia'&' '&[Anno]»
  - dimensione: `=AdministrativeAreaLevel2` (Provincia)
  - misura [S24] «Oidio» · formato automatico: `AVG(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'11'},Anno={"$(=Max(Anno))"}>}[PercIntensità]),[IdVigneto],[Anno]))/100
`

## Slide 10. Oidio Diffusione Grafico Foglia
- **barchart** `f11c5c21-f4e0-4a7c-a790-3e017940f2a1` — «=='Diffusione Oidio su Foglia'&' '&[Anno]»
  - dimensione: `=AdministrativeAreaLevel2` (Provincia)
  - misura [S24] «Oidio» · formato automatico: `AVG(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'11'},Anno={"$(=Max(Anno))"}>}PercDiffusione),[IdVigneto],[Anno]))/100
`

## Slide 11. Peronospora Intentistà Grafico Grappolo
- **barchart** `bb568447-5b1b-464c-a35f-1d997502c1ed` — «=='Danno da Peronospora su Grappolo'&' '&[Anno]»
  - dimensione: `=AdministrativeAreaLevel2` (Provincia)
  - misura [S24] «Peronospora» · formato automatico: `AVG(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}[PercIntensità]),[IdVigneto],[Anno]))/100
`

## Slide 12. Peronospora Diffusione Grafico Grappolo
- **barchart** `b32daa65-b4bd-4007-a662-9121abe09e9a` — «=='Diffusione Peronospora su Grappolo'&' '&[Anno]»
  - dimensione: `=AdministrativeAreaLevel2` (Provincia)
  - misura [S24] «Peronospora» · formato automatico: `AVG(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}PercDiffusione),[IdVigneto],[Anno]))/100
`

## Slide 13. Oidio Intentistà Grafico Grappolo
- **barchart** `4b1b034f-da17-4f79-8ca2-82ec9253edc6` — «=='Danno da Oidio su Grappolo'&' '&[Anno]»
  - dimensione: `=AdministrativeAreaLevel2` (Provincia)
  - misura [S24] «Oidio» · formato automatico: `AVG(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'11'},Anno={"$(=Max(Anno))"}>}[PercIntensità]),[IdVigneto],[Anno]))/100
`

## Slide 14. Oidio Diffusione Grafico Grappolo
- **barchart** `917cbb56-7035-4899-b6f4-6a77228097fd` — «=='Diffusione Oidio su Grappolo'&' '&[Anno]»
  - dimensione: `=AdministrativeAreaLevel2` (Provincia)
  - misura [S24] «Oidio» · formato automatico: `AVG(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'11'},Anno={"$(=Max(Anno))"}>}PercDiffusione),[IdVigneto],[Anno]))/100
`

## Slide 15. Tignole
- **linechart** `pJVPYkk` — «='Catture per trappola - CRYPTOBLABES'»
  - dimensione: `=WEEK_REF` (Settima di Riferimento)
  - dimensione: `=AdministrativeAreaLevel2` (Provincia)
  - misura [S25] «Catture Totali» · formato automatico: `avg(aggr(avg({<idTipoRilievo={'153'},Anno={"$(=Max(Anno))"}>}CatturePerTrappola),WEEK_REF,[IdVigneto]))


//153 CRYPTOBLABES


`

## Slide 16. Tignole (1)
- **linechart** `c0aa669c-b8d8-45e5-9ec8-324a303d59d8` — «=='Catture per trappola - Lobesia Botrana'»
  - dimensione: `=WEEK_REF` (Settima di Riferimento)
  - dimensione: `=AdministrativeAreaLevel2` (Provincia)
  - misura [S25] «Catture Totali» · formato automatico: `avg(aggr(avg({<idTipoRilievo={'148'},Anno={"$(=Max(Anno))"}>}CatturePerTrappola),WEEK_REF,[IdVigneto]))


//153 CRYPTOBLABES
//148 Lobesia

`

## Slide 17. Mal dell'Esca
- **barchart** `QwstNv` — «=='Diffusione Mal dell Esca '&max(Anno)»
  - dimensione: `AdministrativeAreaLevel2` (Comune)
  - misura [S15] «Colpo apoplettico» · formato automatico: `(Count({<idTipoRilievo={"9"}, ValoreConteggiato={"100"},Anno={"$(=Max(Anno))"}>} ([Collezione.IdRow])))
/
(sum({<idTipoRilievo={"9"},Anno={"$(=Max(Anno))"} >} ([NrOrganiControllati])))`
  - misura [S15] «Tigratura Estesa» · formato automatico: `(Count({<idTipoRilievo={"9"}, ValoreConteggiato={"50"},Anno={"$(=Max(Anno))"}>} ([Collezione.IdRow])))
/
(sum({<idTipoRilievo={"9"},Anno={"$(=Max(Anno))"} >} ([NrOrganiControllati])))`
  - misura [S15] «Tigratura poche foglie» · formato automatico: `(Count({<idTipoRilievo={"9"}, ValoreConteggiato={"10"},Anno={"$(=Max(Anno))"}>} ([Collezione.IdRow])))
/
(sum({<idTipoRilievo={"9"},Anno={"$(=Max(Anno))"} >} ([NrOrganiControllati])))`
  - misura [S15] «Tigratura + tralci disseccati o avvizziti» · formato automatico: `(Count({<idTipoRilievo={"9"}, ValoreConteggiato={"60"},Anno={"$(=Max(Anno))"}>} ([Collezione.IdRow])))
/
(sum({<idTipoRilievo={"9"},Anno={"$(=Max(Anno))"} >} ([NrOrganiControllati])))`
  - misura [S15] «Tralci disseccati o avvizziti» · formato automatico: `(Count({<idTipoRilievo={"9"}, ValoreConteggiato={"40"},Anno={"$(=Max(Anno))"}>} ([Collezione.IdRow])))
/
(sum({<idTipoRilievo={"9"},Anno={"$(=Max(Anno))"} >} ([NrOrganiControllati])))`
  - misura [S15] «Punti vegetativi inattivi o stentati» · formato automatico: `(Count({<idTipoRilievo={"9"}, ValoreConteggiato={"30"},Anno={"$(=Max(Anno))"}>} ([Collezione.IdRow])))
/
(sum({<idTipoRilievo={"9"},Anno={"$(=Max(Anno))"} >} ([NrOrganiControllati])))`
- **kpi** `npK`
  - misura [S15] «Numero Rilievi» · formato automatico: `Count(distinct{<idTipoRilievo={"9"},Anno={"$(=Max(Anno))"}>}IdRilievo)`
- **kpi** `tphFcY`
  - misura [S15] «Numero di Piante Controllate» · formato automatico: `Sum({<idTipoRilievo={"9"},Anno={"$(=Max(Anno))"}>}NrOrganiControllati)`
- **kpi** `QRVRF`
  - misura [S15] «Sintomi Assenti» · formato automatico: `Count({<idTipoRilievo={"9"}, ValoreConteggiato={"0"},Anno={"$(=Max(Anno))"}>} ([Collezione.IdRow]))`
- **kpi** `NAJyUP`
  - misura [S15] «Tigratura poche foglie» · formato automatico: `Count({<idTipoRilievo={"9"}, ValoreConteggiato={"10"},Anno={"$(=Max(Anno))"}>} ([Collezione.IdRow]))`
- **kpi** `QPReVAF`
  - misura [S15] «Punti vegetativi inattivi o stentati» · formato automatico: `Count({<idTipoRilievo={"9"}, ValoreConteggiato={"30"},Anno={"$(=Max(Anno))"}>} ([Collezione.IdRow]))
`
- **kpi** `mSffDh`
  - misura [S15] «Tralci disseccati o avvizziti» · formato automatico: `Count({<idTipoRilievo={"9"}, ValoreConteggiato={"40"},Anno={"$(=Max(Anno))"}>} ([Collezione.IdRow]))`
- **kpi** `jXzJKWz`
  - misura [S15] «Tigratura Estesa» · formato automatico: `Count({<idTipoRilievo={"9"}, ValoreConteggiato={"50"},Anno={"$(=Max(Anno))"}>} ([Collezione.IdRow]))`
- **kpi** `yKMuEj`
  - misura [S15] «=='Tigratura + tralci dissecati o avvizziti'» · formato automatico: `Count({<idTipoRilievo={"9"}, ValoreConteggiato={"60"},Anno={"$(=Max(Anno))"}>} ([Collezione.IdRow]))`
- **kpi** `WxphWa`
  - misura [S15] «Colpo Apopletico» · formato automatico: `Count({<idTipoRilievo={"9"}, ValoreConteggiato={"100"},Anno={"$(=Max(Anno))"}>} ([Collezione.IdRow]))`

## Differenze nel report ENG
- Slide ENG: 17 (IT 17).
- Fogli nel report ENG e non in quello IT: nessuno.
- Fogli ripetuti nel report ENG: nessuno.
- Fogli del report IT che mancano nel report ENG: nessuno.

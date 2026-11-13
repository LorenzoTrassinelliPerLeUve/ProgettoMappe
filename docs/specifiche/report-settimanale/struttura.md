# REPORT SETTIMANALE: struttura foglio per foglio

Task #211, tappa 1 · generato da `genera_struttura.py` dalle proprietà delle app (Engine API, senza dati).
Ordine = ordine delle slide dell'automazione **PPT_REPORT_SETTIAMANALE** (18 slide). Per ogni oggetto: tipo e id, titoli e testi IT (e ENG se diversi, stesso id: l'app ENG è una copia di quella IT), dimensioni, misure con lo **schema** [Sxx] (vedi schemi.md) e il formato, contachilometri con scala e fasce.

**Selezioni che il report presuppone**: quelle dell'utente al clic su «CREA REPORT» (segnalibro passato all'automazione): **un'azienda** (RagioneSociale) e **la settimana** (`Week`), più a scelta vigneto, comune/provincia/regione/stato, tecnico (`Nome Cognome`), anno d'impianto, descrizione, data (foglio «Filtri»). Le formule usano la settimana scelta e le settimane di riferimento WEEK_REF* (vedi regole-script.md).

**Variabili dell'app (IT)**:  · ENG: 

## Slide 1. Report
- **text-image** `TPTTmWR`
- **text-image** `uSEjyBA` — «=='$(vRagioneSociale)'»
  - testo IT: =[]({"index":15,"color":"#000000"})
  - testo ENG: =[](AAmhM)
  - misura [S01] «» · formato automatico: `=RagioneSociale&chr(10)&
chr(10)&
'Report del '&' ' &date(((DayNumberOfYear(Today())+(Yearend(Today())))-1),'DD/MM')`

## Slide 2. Dashboard
- **barchart** `EDguZU` — «=='Numerosità di rilievi eseguiti per tipologia'»
  - ENG titolo: «=='Number of observations per type'»
  - dimensione: `=TipoRilevo`
  - misura [S15] «n° rilievi» · formato automatico: `Count({<Anno={"$(=Max(Anno))"}>} distinct IdRilievo)`
- **text-image** `jDppzPu`
- **qlik-multi-kpi** `jfPeLJ`
  - misura [S15] «N° rilievi» · formato automatico: `Count({<Anno={"$(=Max(Anno))"}>} distinct IdRilievo)`
  - misura [S15] «N° organi controllati» · formato F #.##0 (2 dec.): `sum({<Anno={"$(=Max(Anno))"}>}  NrOrganiControllati)`
  - misura [S15] «N° vigneti Monitorati» · formato automatico: `
Count({<Anno={"$(=Max(Anno))"}>} distinct IdVignetoDaApp)`
  - misura [S15] «N° vigneti totale» · formato automatico: `Count(distinct{<Anno={"$(=Max(Anno))"}>} idVigneto_totale)`
  - misura [S15] «% vigneti monitorati» · formato automatico: `Count({<Anno={"$(=Max(Anno))"}>} distinct IdVignetoDaApp)/Count(distinct{<Anno={"$(=Max(Anno))"}>} idVigneto_totale)*100`
- **text-image** `eTJWY`
  - testo IT: =[]({"index":15,"color":"#000000"})
  - testo ENG: =[](YDCCM)
  - misura [S01] «» · formato automatico: `'RILIEVI FATTI al '&date(((DayNumberOfYear(Today())+(Yearend(Today())))-1),'DD/MM')`

## Slide 3. Sanità Grappoli KPI  ·  ENG: Health KPI
- **text-image** `RaLWA`
  - testo IT: OIDIO
- **text-image** `aapXUq`
  - testo IT: FITOTOSSICITA'
- **kpi** `e39ad1f8-2af6-42b6-882e-1c47830083ee` — «sanità dei grappoli da Ampelopatie»
  - misura [S03] «=='Sanità Grappolo'
» · formato automatico: `1-
//BOTRITE
(
if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'73'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'73'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)
//MARCIUMI
+
if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'86'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'86'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)
//OIDIO
+
if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'11'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
`
  - misura [S23] «=='Riduzione ponderata della Produzione'&chr(10)&
'sui vigneti rilevati'» · formato automatico: `(sum(aggr(max([PercDanno]*Superficie),[IdVigneto]))
/
(sum(Aggr(avg(Superficie_Ampelopatie),IdVigneto))))
/100
`
- **text-image** `96c3ee14-af89-4851-9a03-2fb66b6989c0`
  - testo IT: =[](AAmhM)
  - misura [S01] «=='Andamento termo pluviometrico'&' '& 
min({<DatiMeteoStorico={'1'},Mese_Meteo» · formato automatico: `='Sanità Grappoli'
`
- **text-image** `byjqpk`
  - testo IT: GRANDINE
- **kpi** `SUdmLW`
  - misura [S22] «Vigneti con Danno» · formato automatico: `sum(Aggr(count({<idOrgano={'2','5'},idTipoRilievo={'73'},Anno={"$(=Max(Anno))"},PresenzaAvversitaCarenza={'0'}>}NomeVigneto),IdVigneto))
/
sum(Aggr(count({<idTipoRilievo={'73'},Anno={"$(=Max(Anno))"}>}NomeVigneto),IdVigneto))

`
  - misura [S21] «Vigneti monitorati» · formato automatico: `(sum(Aggr(avg({<idTipoRilievo={'73'},Anno={"$(=Max(Anno))"}>}Superficie),IdVigneto))/10000)
/
(sum(Aggr(avg(Superficie),IdVigneto))/10000)`
- **kpi** `rJbkm`
  - misura [S22] «Vigneti con Danno» · formato automatico: `sum(Aggr(count({<idOrgano={'2','5'},idTipoRilievo={'12'},Anno={"$(=Max(Anno))"},PresenzaAvversitaCarenza={'0'}>}NomeVigneto),IdVigneto))
/
sum(Aggr(count({<idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}NomeVigneto),IdVigneto))

`
  - misura [S21] «Vigneti monitorati» · formato automatico: `(sum(Aggr(avg({<idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}Superficie),IdVigneto))/10000)
/
(sum(Aggr(avg(Superficie),IdVigneto))/10000)`
- **kpi** `0dd77e40-8ee0-4161-8d2d-ad8c7dd2c512`
  - misura [S03] «Intensità» · formato automatico: `if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `xbhcLp`
  - misura [S22] «Vigneti con Danno» · formato automatico: `sum(Aggr(count({<idOrgano={'2','5'},idTipoRilievo={'86'},Anno={"$(=Max(Anno))"},PresenzaAvversitaCarenza={'0'}>}NomeVigneto),IdVigneto))
/
sum(Aggr(count({<idTipoRilievo={'86'},Anno={"$(=Max(Anno))"}>}NomeVigneto),IdVigneto))

`
  - misura [S21] «Vigneti monitorati» · formato automatico: `(sum(Aggr(avg({<idTipoRilievo={'86'},Anno={"$(=Max(Anno))"}>}Superficie),IdVigneto))/10000)
/
(sum(Aggr(avg(Superficie),IdVigneto))/10000)`
- **text-image** `EhNeQZ`
  - testo IT: SCOTTATURE
- **kpi** `VRqBAj`
  - misura [S22] «Vigneti con Danno» · formato automatico: `sum(Aggr(count({<idOrgano={'2','5'},idTipoRilievo={'30'},Anno={"$(=Max(Anno))"},PresenzaAvversitaCarenza={'0'}>}NomeVigneto),IdVigneto))
/
sum(Aggr(count({<idTipoRilievo={'30'},Anno={"$(=Max(Anno))"}>}NomeVigneto),IdVigneto))

`
  - misura [S21] «Vigneti monitorati» · formato automatico: `(sum(Aggr(avg({<idTipoRilievo={'30'},Anno={"$(=Max(Anno))"}>}Superficie),IdVigneto))/10000)
/
(sum(Aggr(avg(Superficie),IdVigneto))/10000)`
- **kpi** `vkDms`
  - misura [S03] «Intensità» · formato automatico: `if((sum(aggr(max({<idTipoRilievo={'28'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idTipoRilievo={'28'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `XbncNZP`
  - misura [S22] «Vigneti con Danno» · formato automatico: `sum(Aggr(count({<idTipoRilievo={'28'},Anno={"$(=Max(Anno))"},PresenzaAvversitaCarenza={'0'}>}NomeVigneto),IdVigneto))
/
sum(Aggr(count({<idTipoRilievo={'28'},Anno={"$(=Max(Anno))"}>}NomeVigneto),IdVigneto))

`
  - misura [S21] «Vigneti monitorati» · formato automatico: `(sum(Aggr(avg({<idTipoRilievo={'28'},Anno={"$(=Max(Anno))"}>}Superficie),IdVigneto))/10000)
/
(sum(Aggr(avg(Superficie),IdVigneto))/10000)`
- **text-image** `cd5552f7-e038-4995-bde3-d3c97ba2a864`
- **text-image** `jeHfrvW`
  - testo IT: PERONOSPORA
- **kpi** `HFgJ`
  - misura [S03] «Diffusione» · formato automatico: `if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `NmCaFP`
  - misura [S03] «Diffusione» · formato automatico: `if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'11'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'11'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `KyaRf`
  - misura [S03] «Intensità» · formato automatico: `if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'11'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'11'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `dRgJmTF`
  - misura [S22] «Vigneti con Danno» · formato automatico: `sum(Aggr(count({<idOrgano={'2','5'},idTipoRilievo={'11'},Anno={"$(=Max(Anno))"},PresenzaAvversitaCarenza={'0'}>}NomeVigneto),IdVigneto))
/
sum(Aggr(count({<idTipoRilievo={'11'},Anno={"$(=Max(Anno))"}>}NomeVigneto),IdVigneto))

`
  - misura [S21] «Vigneti monitorati» · formato automatico: `(sum(Aggr(avg({<idTipoRilievo={'11'},Anno={"$(=Max(Anno))"}>}Superficie),IdVigneto))/10000)
/
(sum(Aggr(avg(Superficie),IdVigneto))/10000)`
- **text-image** `pbeKweg`
  - testo IT: BOTRITE
- **kpi** `KKPMbP`
  - misura [S03] «Diffusione» · formato automatico: `if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'73'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'73'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `XbDKTp`
  - misura [S03] «Intensità» · formato automatico: `if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'73'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'73'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **text-image** `YSCcHET`
  - testo IT: MARCIUMI SECONDARI
- **kpi** `BEkE`
  - misura [S03] «Diffusione» · formato automatico: `if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'86'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'86'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)
+
if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'131'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'131'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `CdxKjg`
  - misura [S03] «Intensità» · formato automatico: `if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'86'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'86'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)
+
if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'131'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'131'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **text-image** `NrJm`
  - testo IT: BLACK ROT
- **kpi** `LPYshX`
  - misura [S03] «Diffusione» · formato automatico: `if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'84'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'84'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `EtYakF`
  - misura [S03] «Intensità» · formato automatico: `if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'84'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'84'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `JzZpge`
  - misura [S22] «Vigneti con Danno» · formato automatico: `sum(Aggr(count({<idOrgano={'2','5'},idTipoRilievo={'84'},Anno={"$(=Max(Anno))"},PresenzaAvversitaCarenza={'0'}>}NomeVigneto),IdVigneto))
/
sum(Aggr(count({<idTipoRilievo={'84'},Anno={"$(=Max(Anno))"}>}NomeVigneto),IdVigneto))

`
  - misura [S21] «Vigneti monitorati» · formato automatico: `(sum(Aggr(avg({<idTipoRilievo={'84'},Anno={"$(=Max(Anno))"}>}Superficie),IdVigneto))/10000)
/
(sum(Aggr(avg(Superficie),IdVigneto))/10000)`
- **kpi** `FjVNNn`
  - misura [S03] «Diffusione» · formato automatico: `if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'32'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'32'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `mrBDNm`
  - misura [S03] «Diffusione» · formato automatico: `if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'29'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'29'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `QygP`
  - misura [S03] «Intensità» · formato automatico: `if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'32'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'32'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `YZqhW`
  - misura [S03] «Intensità» · formato automatico: `if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'29'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'29'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `uVYFg`
  - misura [S22] «Vigneti con Danno» · formato automatico: `sum(Aggr(count({<idOrgano={'2','5'},idTipoRilievo={'32'},Anno={"$(=Max(Anno))"},PresenzaAvversitaCarenza={'0'}>}NomeVigneto),IdVigneto))
/
sum(Aggr(count({<idTipoRilievo={'32'},Anno={"$(=Max(Anno))"}>}NomeVigneto),IdVigneto))

`
  - misura [S21] «Vigneti monitorati» · formato automatico: `(sum(Aggr(avg({<idTipoRilievo={'32'},Anno={"$(=Max(Anno))"}>}Superficie),IdVigneto))/10000)
/
(sum(Aggr(avg(Superficie),IdVigneto))/10000)`
- **kpi** `rNrPaHk`
  - misura [S22] «Vigneti con Danno» · formato automatico: `sum(Aggr(count({<idOrgano={'2','5'},idTipoRilievo={'29'},Anno={"$(=Max(Anno))"},PresenzaAvversitaCarenza={'0'}>}NomeVigneto),IdVigneto))
/
sum(Aggr(count({<idTipoRilievo={'29'},Anno={"$(=Max(Anno))"}>}NomeVigneto),IdVigneto))

`
  - misura [S21] «Vigneti monitorati» · formato automatico: `(sum(Aggr(avg({<idTipoRilievo={'29'},Anno={"$(=Max(Anno))"}>}Superficie),IdVigneto))/10000)
/
(sum(Aggr(avg(Superficie),IdVigneto))/10000)`
- **kpi** `tJqesqW`
  - misura [S03] «Diffusione» · formato automatico: `if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'30'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'30'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **text-image** `jZkWcGG`
  - testo IT: GELATA (gemme)
- **text-image** `xVLhLcz`
  - testo IT: TIGNOLE
- **kpi** `vpJN`
  - misura [S03] «Intensità» · formato automatico: `if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'30'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'30'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `YMpCTJ`
  - misura [S03] «Diffusione» · formato automatico: `if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'13'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'13'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `drQpktR`
  - misura [S23] «Numero di Nidi/100 grappoli» · formato automatico: `if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'13'},Anno={"$(=Max(Anno))"}>}[NrFormeMobiliNidi]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'13'},Anno={"$(=Max(Anno))"}>}[NrFormeMobiliNidi]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
),0)
*100`
- **kpi** `PAnWH`
  - misura [S22] «Vigneti con Danno» · formato automatico: `sum(Aggr(count({<idOrgano={'2','5'},idTipoRilievo={'13'},Anno={"$(=Max(Anno))"},PresenzaAvversitaCarenza={'0'}>}NomeVigneto),IdVigneto))
/
sum(Aggr(count({<idTipoRilievo={'13'},Anno={"$(=Max(Anno))"}>}NomeVigneto),IdVigneto))

`
  - misura [S21] «Vigneti monitorati» · formato automatico: `(sum(Aggr(avg({<idTipoRilievo={'13'},Anno={"$(=Max(Anno))"}>}Superficie),IdVigneto))/10000)
/
(sum(Aggr(avg(Superficie),IdVigneto))/10000)`
- **kpi** `GCRAK`
  - misura [S03] «Diffusione» · formato automatico: `if((sum(aggr(max({<idTipoRilievo={'28'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idTipoRilievo={'28'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `SsVVvmr`
  - misura [S03] «Diffusione» · formato automatico: `if((sum(aggr(max({<idTipoRilievo={'164'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idTipoRilievo={'164'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `fqfdJv`
  - misura [S03] «Intensità» · formato automatico: `if((sum(aggr(max({<idTipoRilievo={'164'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idTipoRilievo={'164'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `qBPtZ`
  - misura [S22] «Vigneti con Danno» · formato automatico: `sum(Aggr(count({<idTipoRilievo={'164'},Anno={"$(=Max(Anno))"},PresenzaAvversitaCarenza={'0'}>}NomeVigneto),IdVigneto))
/
sum(Aggr(count({<idTipoRilievo={'164'},Anno={"$(=Max(Anno))"}>}NomeVigneto),IdVigneto))

`
  - misura [S21] «Vigneti monitorati» · formato automatico: `(sum(Aggr(avg({<idTipoRilievo={'164'},Anno={"$(=Max(Anno))"}>}Superficie),IdVigneto))/10000)
/
(sum(Aggr(avg(Superficie),IdVigneto))/10000)`
- **text-image** `dRbCKW`
  - testo IT: FILATURA
- **text-image** `DhzKqT`
  - testo IT: ACINELLATURA
- **kpi** `hxmKsH`
  - misura [S03] «Diffusione» · formato automatico: `if((sum(aggr(max({<idTipoRilievo={'168'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idTipoRilievo={'168'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `CbERUs`
  - misura [S03] «Intensità» · formato automatico: `if((sum(aggr(max({<idTipoRilievo={'168'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idTipoRilievo={'168'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `vFbUSa`
  - misura [S22] «Vigneti con Danno» · formato automatico: `sum(Aggr(count({<idTipoRilievo={'168'},Anno={"$(=Max(Anno))"},PresenzaAvversitaCarenza={'0'}>}NomeVigneto),IdVigneto))
/
sum(Aggr(count({<idTipoRilievo={'168'},Anno={"$(=Max(Anno))"}>}NomeVigneto),IdVigneto))

`
  - misura [S21] «Vigneti monitorati» · formato automatico: `(sum(Aggr(avg({<idTipoRilievo={'168'},Anno={"$(=Max(Anno))"}>}Superficie),IdVigneto))/10000)
/
(sum(Aggr(avg(Superficie),IdVigneto))/10000)`

## Slide 4. Grafico Grappoli  ·  ENG: diseases
- **text-image** `HsApqR`
  - testo IT: =[](AAmhM)
  - misura [S01] «=='Andamento termo pluviometrico'&' '& 
min({<DatiMeteoStorico={'1'},Mese_Meteo» · formato automatico: `='Sanità Grappoli'
`
- **text-image** `qwRvQj`
- **barchart** `gKVmQ` — «=='Danno da Ampelopatie su Grappolo'&' '&[Anno]»
  - ENG titolo: «=='Damages on the cluster'&' '&[Anno]»
  - dimensione: `=NomeVigneto`
  - misura [S03] «Peronospora» · formato automatico: `if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'2','5'},idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'2','5'},idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100),0)
`
  - misura [S03] «Oidio» · formato automatico: `if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'11'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'2','5'},idTipoRilievo={'11'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'11'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'2','5'},idTipoRilievo={'11'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100),0)
`
  - misura [S03] «Botrite» · formato automatico: `if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'73'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'2','5'},idTipoRilievo={'73'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'73'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'2','5'},idTipoRilievo={'73'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100),0)`
  - misura [S03] «Marciumi Secondorai» · formato automatico: `if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'86'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'2','5'},idTipoRilievo={'86'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'86'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'2','5'},idTipoRilievo={'86'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100),0)`
  - misura [S03] «Black Rot» · formato automatico: `if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'84'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'2','5'},idTipoRilievo={'84'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'84'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'2','5'},idTipoRilievo={'84'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100),0)`
  - misura [S03] «Scottature» · formato automatico: `if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'32'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'2','5'},idTipoRilievo={'32'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'32'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'2','5'},idTipoRilievo={'32'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100),0)`
  - misura [S03] «Fitotossicità» · formato automatico: `if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'30'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'2','5'},idTipoRilievo={'30'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'30'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'2','5'},idTipoRilievo={'30'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100),0)`
  - misura [S03] «Grandine» · formato automatico: `if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'29'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'2','5'},idTipoRilievo={'29'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'29'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'2','5'},idTipoRilievo={'29'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100),0)`
  - misura [S03] «Tignole» · formato automatico: `if((sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'13'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'2','5'},idTipoRilievo={'13'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100)
>0,
(sum(aggr(max({<idOrgano={'2','5'}, idTipoRilievo={'13'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'2','5'},idTipoRilievo={'13'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100),0)
`
  - misura [S03] «Acinellatura» · formato automatico: `if((sum(aggr(max({<idTipoRilievo={'168'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idTipoRilievo={'168'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`

## Slide 5. Sanità Foglia KPI  ·  ENG: Leaves health
- **kpi** `7d1314cd-c61a-45f7-8b12-56e49389be9a`
  - misura [S03] «Intensità» · formato automatico: `if((sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'11'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'11'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `208d0649-caec-45e3-b5cc-f6e6a6c6ab24`
  - misura [S03] «Intensità» · formato automatico: `if((sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'73'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'73'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `c8e352fc-2f86-4c83-9ca5-0059966f8690`
  - misura [S03] «Intensità» · formato automatico: `if((sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'29'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'29'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **text-image** `790b29ff-0df6-4b43-88ca-38b5f97ab34f`
  - testo IT: =[](AAmhM)
  - misura [S01] «=='Andamento termo pluviometrico'&' '& 
min({<DatiMeteoStorico={'1'},Mese_Meteo» · formato automatico: `='Sanità Foglie'
`
- **kpi** `5211594a-76d0-4566-901a-653f878c3051`
  - misura [S03] «Intensità» · formato automatico: `if((sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'32'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'32'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `26367bcd-f5cc-40b1-94f5-8dcf4880ba54`
  - misura [S03] «Intensità» · formato automatico: `if((sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'84'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'84'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `0b52b93e-0faa-4f9e-aeee-a53f04bc4546`
  - misura [S03] «Intensità» · formato automatico: `if((sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **text-image** `c7974308-c14d-4573-aa12-1044fb41cdda`
- **kpi** `aTsWJWA`
  - misura [S03] «Intensità» · formato automatico: `if((sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'30'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'30'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **text-image** `EThJr`
  - testo IT: PERONOSPORA
  - testo ENG: DOWNEY MILDEW
- **text-image** `QsWLV`
  - testo IT: OIDIO
  - testo ENG: POWDERY MILDEW
- **text-image** `ZArmxrZ`
  - testo IT: BOTRITE
  - testo ENG: BOTRITIS
- **text-image** `ZxRNRd`
  - testo IT: BLACK ROT
- **text-image** `mUyvKm`
  - testo IT: SCOTTATURE
  - testo ENG: SUN BURNS
- **text-image** `KUTA`
  - testo IT: GRANDINE
  - testo ENG: HAIL
- **text-image** `bPpvjM`
  - testo IT: FITOTOSSICITA'
  - testo ENG: FITOTOXICITY
- **kpi** `sQCmg`
  - misura [S03] «Diffusione» · formato automatico: `if((sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}PercDiffusione*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `JPBXH`
  - misura [S03] «Diffusione» · formato automatico: `if((sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'11'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'11'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `dCpkyy`
  - misura [S03] «Diffusione» · formato automatico: `if((sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'73'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'73'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `mVLCFJZ`
  - misura [S03] «Diffusione» · formato automatico: `if((sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'84'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'84'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `tHjLcbt`
  - misura [S03] «Diffusione» · formato automatico: `if((sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'32'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'32'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `zJHxg`
  - misura [S03] «Diffusione» · formato automatico: `if((sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'29'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'29'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `zSaKJ`
  - misura [S03] «Diffusione» · formato automatico: `if((sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'30'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'30'},Anno={"$(=Max(Anno))"}>}[PercDiffusione]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)`
- **kpi** `fHGCbn`
  - misura [S22] «Vigneti con Danno» · formato automatico: `sum(Aggr(count({<idOrgano={'1','3','4','10'},idTipoRilievo={'12'},Anno={"$(=Max(Anno))"},PresenzaAvversitaCarenza={'0'}>}NomeVigneto),IdVigneto))
/
sum(Aggr(count({<idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}NomeVigneto),IdVigneto))

`
  - misura [S21] «Vigneti monitorati» · formato automatico: `(sum(Aggr(avg({<idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}Superficie),IdVigneto))/10000)
/
(sum(Aggr(avg(Superficie),IdVigneto))/10000)`
- **kpi** `QjLGSd`
  - misura [S22] «Vigneti con Danno» · formato automatico: `sum(Aggr(count({<idOrgano={'1','3','4','10'},idTipoRilievo={'11'},Anno={"$(=Max(Anno))"},PresenzaAvversitaCarenza={'0'}>}NomeVigneto),IdVigneto))
/
sum(Aggr(count({<idTipoRilievo={'11'},Anno={"$(=Max(Anno))"}>}NomeVigneto),IdVigneto))`
  - misura [S21] «Vigneti monitorati» · formato automatico: `(sum(Aggr(avg({<idTipoRilievo={'11'},Anno={"$(=Max(Anno))"}>}Superficie),IdVigneto))/10000)
/
(sum(Aggr(avg(Superficie),IdVigneto))/10000)`
- **kpi** `yJwMMbV`
  - misura [S22] «Vigneti con Danno» · formato automatico: `sum(Aggr(count({<idOrgano={'1','3','4','10'},idTipoRilievo={'73'},Anno={"$(=Max(Anno))"},PresenzaAvversitaCarenza={'0'}>}NomeVigneto),IdVigneto))
/
sum(Aggr(count({<idTipoRilievo={'73'},Anno={"$(=Max(Anno))"}>}NomeVigneto),IdVigneto))
`
  - misura [S21] «Vigneti monitorati» · formato automatico: `(sum(Aggr(avg({<idTipoRilievo={'73'},Anno={"$(=Max(Anno))"}>}Superficie),IdVigneto))/10000)
/
(sum(Aggr(avg(Superficie),IdVigneto))/10000)`
- **kpi** `DzGGpVG`
  - misura [S22] «Vigneti con Danno» · formato automatico: `sum(Aggr(count({<idTipoRilievo={'84'},Anno={"$(=Max(Anno))"},PresenzaAvversitaCarenza={'0'}>}NomeVigneto),IdVigneto))
/
sum(Aggr(count({<idTipoRilievo={'84'},Anno={"$(=Max(Anno))"}>}NomeVigneto),IdVigneto))

`
  - misura [S21] «Vigneti monitorati» · formato automatico: `(sum(Aggr(avg({<idTipoRilievo={'84'},Anno={"$(=Max(Anno))"}>}Superficie),IdVigneto))/10000)
/
(sum(Aggr(avg(Superficie),IdVigneto))/10000)`
- **kpi** `uhHPpPs`
  - misura [S22] «Vigneti con Danno» · formato automatico: `sum(Aggr(count({<idOrgano={'1','3','4','10'},idTipoRilievo={'32'},Anno={"$(=Max(Anno))"},PresenzaAvversitaCarenza={'0'}>}NomeVigneto),IdVigneto))
/
sum(Aggr(count({<idTipoRilievo={'32'},Anno={"$(=Max(Anno))"}>}NomeVigneto),IdVigneto))

`
  - misura [S21] «Vigneti monitorati» · formato automatico: `(sum(Aggr(avg({<idTipoRilievo={'32'},Anno={"$(=Max(Anno))"}>}Superficie),IdVigneto))/10000)
/
(sum(Aggr(avg(Superficie),IdVigneto))/10000)`
- **kpi** `tkvuEwh`
  - misura [S22] «Vigneti con Danno» · formato automatico: `sum(Aggr(count({<idOrgano={'1','3','4','10'},idTipoRilievo={'29'},Anno={"$(=Max(Anno))"},PresenzaAvversitaCarenza={'0'}>}NomeVigneto),IdVigneto))
/
sum(Aggr(count({<idTipoRilievo={'29'},Anno={"$(=Max(Anno))"}>}NomeVigneto),IdVigneto))

`
  - misura [S21] «Vigneti monitorati» · formato automatico: `(sum(Aggr(avg({<idTipoRilievo={'29'},Anno={"$(=Max(Anno))"}>}Superficie),IdVigneto))/10000)
/
(sum(Aggr(avg(Superficie),IdVigneto))/10000)


`
- **kpi** `ykjdcp`
  - misura [S22] «Vigneti con Danno» · formato automatico: `sum(Aggr(count({<idOrgano={'1','3','4','10'},idTipoRilievo={'30'},Anno={"$(=Max(Anno))"},PresenzaAvversitaCarenza={'0'}>}NomeVigneto),IdVigneto))
/
sum(Aggr(count({<idTipoRilievo={'30'},Anno={"$(=Max(Anno))"}>}NomeVigneto),IdVigneto))

`
  - misura [S21] «Vigneti monitorati» · formato automatico: `(sum(Aggr(avg({<idTipoRilievo={'30'},Anno={"$(=Max(Anno))"}>}Superficie),IdVigneto))/10000)
/
(sum(Aggr(avg(Superficie),IdVigneto))/10000)`
- **text-image** `PdgEsQj`
  - testo IT: RIDUZIONE % SFE
- **kpi** `HjEKPC`
  - misura [S03] «==' ' » · formato automatico: `//PERONOSPORA
if((sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)
//OIDIO
+
if((sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'11'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100)
>0,
(sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'11'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
(sum(Aggr(avg(Superficie),IdVigneto)))
/100),0)
//BOTRITE
+
if((sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'73'},Anno={"$(=Max(Anno))"}>}[PercI`

## Slide 6. Grafico Foglia  ·  ENG: (non nell'app ENG)
- **text-image** `d72d5857-b256-4082-83af-d8408f0be25c`
- **text-image** `2c5bb71c-19a9-478a-94f6-d54ae94caa91`
  - testo IT: =[](AAmhM)
  - misura [S01] «=='Andamento termo pluviometrico'&' '& 
min({<DatiMeteoStorico={'1'},Mese_Meteo» · formato automatico: `='Sanità Foglie'
`
- **barchart** `15823e33-903a-49c8-9dcb-21b296a63af5` — «=='Danno da Ampelopatie su Foglia'&' '&[Anno]»
  - dimensione: `=NomeVigneto`
  - misura [S03] «Peronospora» · formato automatico: `if((sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'1','3','4','10'},idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100)
>0,
(sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'1','3','4','10'},idTipoRilievo={'12'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100),0)
`
  - misura [S03] «Oidio» · formato automatico: `if((sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'11'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'1','3','4','10'},idTipoRilievo={'11'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100)
>0,
(sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'11'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'1','3','4','10'},idTipoRilievo={'11'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100),0)
`
  - misura [S03] «Botrite» · formato automatico: `if((sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'73'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'1','3','4','10'},idTipoRilievo={'73'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100)
>0,
(sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'73'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'1','3','4','10'},idTipoRilievo={'73'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100),0)`
  - misura [S03] «Marciumi Secondorai» · formato automatico: `if((sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'86'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'1','3','4','10'},idTipoRilievo={'86'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100)
>0,
(sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'86'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'1','3','4','10'},idTipoRilievo={'86'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100),0)`
  - misura [S03] «Black Rot» · formato automatico: `if((sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'84'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'1','3','4','10'},idTipoRilievo={'84'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100)
>0,
(sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'84'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'1','3','4','10'},idTipoRilievo={'84'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100),0)`
  - misura [S03] «Scottature» · formato automatico: `if((sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'32'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'1','3','4','10'},idTipoRilievo={'32'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100)
>0,
(sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'32'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'1','3','4','10'},idTipoRilievo={'32'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100),0)`
  - misura [S03] «Fitotossicità» · formato automatico: `if((sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'30'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'1','3','4','10'},idTipoRilievo={'30'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100)
>0,
(sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'30'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'1','3','4','10'},idTipoRilievo={'30'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100),0)`
  - misura [S03] «Grandine» · formato automatico: `if((sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'29'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'1','3','4','10'},idTipoRilievo={'29'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100)
>0,
(sum(aggr(max({<idOrgano={'1','3','4','10'}, idTipoRilievo={'29'},Anno={"$(=Max(Anno))"}>}[PercIntensità]*Superficie),[IdVigneto],[Anno]))
/
sum(aggr(avg({<idOrgano={'1','3','4','10'},idTipoRilievo={'29'},Anno={"$(=Max(Anno))"}>}Superficie),[IdVigneto],[Anno]))
/100),0)`

## Slide 7. Fenologia  ·  ENG: PHENOLOGY
- **distributionplot** `EnCYpP` — «Confronto BBCH - germogliamento»
  - ENG titolo: «Comparison BBCH - budburst»
  - dimensione: `Vitigno`
  - dimensione: `=Anno_BBCH` (anno)
  - misura [S14] «Data» · formato D DD/MM (2 dec.): `//avg(aggr(Avg({<BBCH_Modello={'7','8','9'}>} Day_Ref_ModelloBBCH),Anno_BBCH,IdVigneto))


avg(
  aggr(
    if(
      Count({<BBCH_Modello={'9'}>} Day_Ref_ModelloBBCH) > 0,
      Avg({<BBCH_Modello={'9'}>} Day_Ref_ModelloBBCH),
      if(
        Count({<BBCH_Modello={'8'}>} Day_Ref_ModelloBBCH) > 0,
        Avg({<BBCH_Modello={'8'}>} Day_Ref_ModelloBBCH),
        Avg({<BBCH_Modello={'7'}>} Day_Ref_ModelloBBCH)
      )
    ),
    Anno_BBCH,
    IdVigneto
  )
)

//23/03/2026`
- **distributionplot** `sfPzK` — «Confronto BBCH - fioritura»
  - ENG titolo: «Comparison BBCH - flowering»
  - dimensione: `Vitigno`
  - dimensione: `=Anno_BBCH` (anno)
  - misura [S14] «Data» · formato D DD/MM (2 dec.): `//avg(aggr(Avg({<BBCH_Modello={'63','64','65','66','67','68'}>} Day_Ref_ModelloBBCH),Anno_BBCH,IdVigneto))


avg(
  aggr(
    if(
      Count({<BBCH_Modello={'68'}>} Day_Ref_ModelloBBCH) > 0,
      Avg({<BBCH_Modello={'68'}>} Day_Ref_ModelloBBCH),
      if(
        Count({<BBCH_Modello={'67'}>} Day_Ref_ModelloBBCH) > 0,
        Avg({<BBCH_Modello={'67'}>} Day_Ref_ModelloBBCH),
        if(
          Count({<BBCH_Modello={'66'}>} Day_Ref_ModelloBBCH) > 0,
          Avg({<BBCH_Modello={'66'}>} Day_Ref_ModelloBBCH),
          if(
            Count({<BBCH_Modello={'65'}>} Day_Ref_ModelloBBCH) > 0,
            Avg({<BBCH_Modello={'65'}>} Day_Ref_ModelloBBCH),
            if(
              Count({<BBCH_Modello={'64'}>} Day_Ref_ModelloBBCH) > 0,
              Avg({<BBCH_Modello={'64'}>} Day_Ref_ModelloBBCH),
              Avg({<BBCH_Modello={'63'}>} Day_Ref_ModelloBBCH)
            )
          `
- **distributionplot** `LtcDg` — «Confronto BBCH - vendemmia»
  - ENG titolo: «Comparison BBCH - harvest»
  - dimensione: `Vitigno`
  - dimensione: `=Anno_Vendemmia` (Anno)
  - misura [S13] «Data» · formato D DD/MM (2 dec.): `avg(aggr(Avg(Day_Ref_Vendemmia),Anno_Vendemmia,IdVigneto))


`
- **distributionplot** `WpPNKDz` — «Confronto BBCH - invaiatura»
  - ENG titolo: «Comparison BBCH - veraison»
  - dimensione: `Vitigno`
  - dimensione: `=Anno_BBCH` (anno)
  - misura [S14] «Data» · formato D DD/MM (2 dec.): `//avg(aggr(Avg({<BBCH_Modello={'81','82','83'}>} Day_Ref_ModelloBBCH),Anno_BBCH,IdVigneto))


avg(
  aggr(
    if(
      Count({<BBCH_Modello={'83'}>} Day_Ref_ModelloBBCH) > 0,
      Avg({<BBCH_Modello={'83'}>} Day_Ref_ModelloBBCH),
      if(
        Count({<BBCH_Modello={'82'}>} Day_Ref_ModelloBBCH) > 0,
        Avg({<BBCH_Modello={'82'}>} Day_Ref_ModelloBBCH),
        Avg({<BBCH_Modello={'81'}>} Day_Ref_ModelloBBCH)
      )
    ),
    Anno_BBCH,
    IdVigneto
  )
)

//23/03/2026`
- **kpi** `zFwGep`
  - misura [S14] «=='ANNATA '& (max(Anno_BBCH))» · formato D DD/MM (2 dec.): `//avg(aggr(Avg({<BBCH_Modello={'7','8','9'},Anno_BBCH={"$(=Max(Anno_BBCH))"}>} Day_Ref_ModelloBBCH),IdVigneto))


avg(
  aggr(
    if(
      Count({<BBCH_Modello={'9'},Anno_BBCH={"$(=Max(Anno_BBCH))"}>} Day_Ref_ModelloBBCH) > 0,
      Avg({<BBCH_Modello={'9'},Anno_BBCH={"$(=Max(Anno_BBCH))"}>} Day_Ref_ModelloBBCH),
      if(
        Count({<BBCH_Modello={'8'},Anno_BBCH={"$(=Max(Anno_BBCH))"}>} Day_Ref_ModelloBBCH) > 0,
        Avg({<BBCH_Modello={'8'},Anno_BBCH={"$(=Max(Anno_BBCH))"}>} Day_Ref_ModelloBBCH),
        Avg({<BBCH_Modello={'7'},Anno_BBCH={"$(=Max(Anno_BBCH))"}>} Day_Ref_ModelloBBCH)
      )
    ),
    IdVigneto
  )
)

//23/03/2026


`
- **kpi** `JnLqdJ`
  - misura [S14] «=='ANNATA '& (max(Anno_BBCH)-1)» · formato D DD/MM (2 dec.): `//avg(aggr(Avg({<BBCH_Modello={'7','8','9'},Anno_BBCH={"$(=Max(Anno_BBCH)-1)"}>} Day_Ref_ModelloBBCH),IdVigneto))

avg(
  aggr(
    if(
      Count({<BBCH_Modello={'9'},Anno_BBCH={"$(=Max(Anno_BBCH)-1)"}>} Day_Ref_ModelloBBCH) > 0,
      Avg({<BBCH_Modello={'9'},Anno_BBCH={"$(=Max(Anno_BBCH)-1)"}>} Day_Ref_ModelloBBCH),
      if(
        Count({<BBCH_Modello={'8'},Anno_BBCH={"$(=Max(Anno_BBCH)-1)"}>} Day_Ref_ModelloBBCH) > 0,
        Avg({<BBCH_Modello={'8'},Anno_BBCH={"$(=Max(Anno_BBCH)-1)"}>} Day_Ref_ModelloBBCH),
        Avg({<BBCH_Modello={'7'},Anno_BBCH={"$(=Max(Anno_BBCH)-1)"}>} Day_Ref_ModelloBBCH)
      )
    ),
    IdVigneto
  )
)

//23/03/2026
`
- **kpi** `UEnDJHL`
  - misura [S14] «STORICO 5 ANNI» · formato D DD/MM (2 dec.): `//avg(aggr(Avg({<BBCH_Modello={'7','8','9'},Anno_BBCH={">=$(=Max(Anno_BBCH)-5)		<=$(=Max(Anno_BBCH-1))"}	>} Day_Ref_ModelloBBCH),IdVigneto,Anno_BBCH))


avg(
  aggr(
    if(
      Count({<BBCH_Modello={'9'},Anno_BBCH={">=$(=Max(Anno_BBCH)-5)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH) > 0,
      Avg({<BBCH_Modello={'9'},Anno_BBCH={">=$(=Max(Anno_BBCH)-5)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH),
      if(
        Count({<BBCH_Modello={'8'},Anno_BBCH={">=$(=Max(Anno_BBCH)-5)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH) > 0,
        Avg({<BBCH_Modello={'8'},Anno_BBCH={">=$(=Max(Anno_BBCH)-5)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH),
        Avg({<BBCH_Modello={'7'},Anno_BBCH={">=$(=Max(Anno_BBCH)-5)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH)
      )
    ),
    IdVigneto
  )
)

//23/03/2026`
- **kpi** `PceZKXt`
  - misura [S14] «STORICO 10 ANNI» · formato D DD/MM (2 dec.): `//avg(aggr(Avg({<BBCH_Modello={'7','8','9'},Anno_BBCH={">=$(=Max(Anno_BBCH)-10)		<=$(=Max(Anno_BBCH-1))"}	>} Day_Ref_ModelloBBCH),IdVigneto,Anno_BBCH))

avg(
  aggr(
    if(
      Count({<BBCH_Modello={'9'},Anno_BBCH={">=$(=Max(Anno_BBCH)-10)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH) > 0,
      Avg({<BBCH_Modello={'9'},Anno_BBCH={">=$(=Max(Anno_BBCH)-10)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH),
      if(
        Count({<BBCH_Modello={'8'},Anno_BBCH={">=$(=Max(Anno_BBCH)-10)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH) > 0,
        Avg({<BBCH_Modello={'8'},Anno_BBCH={">=$(=Max(Anno_BBCH)-10)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH),
        Avg({<BBCH_Modello={'7'},Anno_BBCH={">=$(=Max(Anno_BBCH)-10)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH)
      )
    ),
    IdVigneto
  )
)

//23/03/2026`
- **kpi** `neFcLCP`
  - misura [S14] «=='ANNATA '& (max(Anno_BBCH))» · formato D DD/MM (2 dec.): `//avg(aggr(Avg({<BBCH_Modello={'63','64','65','66','67','68'},Anno_BBCH={"$(=Max(Anno_BBCH))"}>} Day_Ref_ModelloBBCH),IdVigneto))


Avg(
  aggr(
    if(
      Count({<BBCH_Modello={'68'},Anno_BBCH={"$(=Max(Anno_BBCH))"}>} Day_Ref_ModelloBBCH) > 0,
      Avg({<BBCH_Modello={'68'},Anno_BBCH={"$(=Max(Anno_BBCH))"}>} Day_Ref_ModelloBBCH),
      if(
        Count({<BBCH_Modello={'67'},Anno_BBCH={"$(=Max(Anno_BBCH))"}>} Day_Ref_ModelloBBCH) > 0,
        Avg({<BBCH_Modello={'67'},Anno_BBCH={"$(=Max(Anno_BBCH))"}>} Day_Ref_ModelloBBCH),
        if(
          Count({<BBCH_Modello={'66'},Anno_BBCH={"$(=Max(Anno_BBCH))"}>} Day_Ref_ModelloBBCH) > 0,
          Avg({<BBCH_Modello={'66'},Anno_BBCH={"$(=Max(Anno_BBCH))"}>} Day_Ref_ModelloBBCH),
          if(
            Count({<BBCH_Modello={'65'},Anno_BBCH={"$(=Max(Anno_BBCH))"}>} Day_Ref_ModelloBBCH) > 0,
            Avg({<BBCH_Modello={'65'},Anno_BB`
- **kpi** `wvTzEUL`
  - misura [S14] «=='ANNATA '& (max(Anno_BBCH)-1)» · formato D DD/MM (2 dec.): `//avg(aggr(Avg({<BBCH_Modello={'63','64','65','66','67','68'},Anno_BBCH={"$(=Max(Anno_BBCH)-1)"}>} Day_Ref_ModelloBBCH),IdVigneto))

Avg(
  aggr(
    if(
      Count({<BBCH_Modello={'68'},Anno_BBCH={"$(=Max(Anno_BBCH)-1)"}>} Day_Ref_ModelloBBCH) > 0,
      Avg({<BBCH_Modello={'68'},Anno_BBCH={"$(=Max(Anno_BBCH)-1)"}>} Day_Ref_ModelloBBCH),
      if(
        Count({<BBCH_Modello={'67'},Anno_BBCH={"$(=Max(Anno_BBCH)-1)"}>} Day_Ref_ModelloBBCH) > 0,
        Avg({<BBCH_Modello={'67'},Anno_BBCH={"$(=Max(Anno_BBCH)-1)"}>} Day_Ref_ModelloBBCH),
        if(
          Count({<BBCH_Modello={'66'},Anno_BBCH={"$(=Max(Anno_BBCH)-1)"}>} Day_Ref_ModelloBBCH) > 0,
          Avg({<BBCH_Modello={'66'},Anno_BBCH={"$(=Max(Anno_BBCH)-1)"}>} Day_Ref_ModelloBBCH),
          if(
            Count({<BBCH_Modello={'65'},Anno_BBCH={"$(=Max(Anno_BBCH)-1)"}>} Day_Ref_ModelloBBCH) > 0,
            Avg({<BBCH_Modello`
- **kpi** `jXkAYKH`
  - misura [S14] «STORICO 5 ANNI» · formato D DD/MM (2 dec.): `//avg(aggr(Avg({<BBCH_Modello={'63','64','65','66','67','68'},Anno_BBCH={">=$(=Max(Anno_BBCH)-5)		<=$(=Max(Anno_BBCH-1))"}	>} Day_Ref_ModelloBBCH),IdVigneto,Anno_BBCH))

Avg(
  aggr(
    if(
      Count({<BBCH_Modello={'68'},Anno_BBCH={">=$(=Max(Anno_BBCH)-5)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH) > 0,
      Avg({<BBCH_Modello={'68'},Anno_BBCH={">=$(=Max(Anno_BBCH)-5)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH),
      if(
        Count({<BBCH_Modello={'67'},Anno_BBCH={">=$(=Max(Anno_BBCH)-5)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH) > 0,
        Avg({<BBCH_Modello={'67'},Anno_BBCH={">=$(=Max(Anno_BBCH)-5)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH),
        if(
          Count({<BBCH_Modello={'66'},Anno_BBCH={">=$(=Max(Anno_BBCH)-5)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH) > 0,
          Avg({<BBCH_Modello={'66'},Anno_BBCH={">=$(=Max(Anno_BBCH)-5)		<=$`
- **kpi** `LhZPVg`
  - misura [S14] «STORICO 10 ANNI» · formato D DD/MM (2 dec.): `//avg(aggr(Avg({<BBCH_Modello={'63','64','65','66','67','68'},Anno_BBCH={">=$(=Max(Anno_BBCH)-10)		<=$(=Max(Anno_BBCH-1))"}	>} Day_Ref_ModelloBBCH),IdVigneto,Anno_BBCH))


Avg(
  aggr(
    if(
      Count({<BBCH_Modello={'68'},Anno_BBCH={">=$(=Max(Anno_BBCH)-10)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH) > 0,
      Avg({<BBCH_Modello={'68'},Anno_BBCH={">=$(=Max(Anno_BBCH)-10)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH),
      if(
        Count({<BBCH_Modello={'67'},Anno_BBCH={">=$(=Max(Anno_BBCH)-10)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH) > 0,
        Avg({<BBCH_Modello={'67'},Anno_BBCH={">=$(=Max(Anno_BBCH)-10)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH),
        if(
          Count({<BBCH_Modello={'66'},Anno_BBCH={">=$(=Max(Anno_BBCH)-10)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH) > 0,
          Avg({<BBCH_Modello={'66'},Anno_BBCH={">=$(=Max(Anno_BBCH)-`
- **kpi** `fjbGmQ`
  - misura [S14] «=='ANNATA '& (max(Anno_BBCH))» · formato D DD/MM (2 dec.): `//avg(aggr(Avg({<BBCH_Modello={'81','82','83'},Anno_BBCH={"$(=Max(Anno_BBCH))"}>} Day_Ref_ModelloBBCH),IdVigneto))


avg(
  aggr(
    if(
      Count({<BBCH_Modello={'83'},Anno_BBCH={"$(=Max(Anno_BBCH))"}>} Day_Ref_ModelloBBCH) > 0,
      Avg({<BBCH_Modello={'83'},Anno_BBCH={"$(=Max(Anno_BBCH))"}>} Day_Ref_ModelloBBCH),
      if(
        Count({<BBCH_Modello={'82'},Anno_BBCH={"$(=Max(Anno_BBCH))"}>} Day_Ref_ModelloBBCH) > 0,
        Avg({<BBCH_Modello={'82'},Anno_BBCH={"$(=Max(Anno_BBCH))"}>} Day_Ref_ModelloBBCH),
        Avg({<BBCH_Modello={'81'},Anno_BBCH={"$(=Max(Anno_BBCH))"}>} Day_Ref_ModelloBBCH)
      )
    ),
    IdVigneto
  )
)

//23/03/2026`
- **kpi** `dxxqYNR`
  - misura [S14] «=='ANNATA '& (max(Anno_BBCH)-1)» · formato D DD/MM (2 dec.): `//avg(aggr(Avg({<BBCH_Modello={'81','82','83'},Anno_BBCH={"$(=Max(Anno_BBCH)-1)"}>} Day_Ref_ModelloBBCH),IdVigneto))



avg(
  aggr(
    if(
      Count({<BBCH_Modello={'83'},Anno_BBCH={"$(=Max(Anno_BBCH)-1)"}>} Day_Ref_ModelloBBCH) > 0,
      Avg({<BBCH_Modello={'83'},Anno_BBCH={"$(=Max(Anno_BBCH)-1)"}>} Day_Ref_ModelloBBCH),
      if(
        Count({<BBCH_Modello={'82'},Anno_BBCH={"$(=Max(Anno_BBCH)-1)"}>} Day_Ref_ModelloBBCH) > 0,
        Avg({<BBCH_Modello={'82'},Anno_BBCH={"$(=Max(Anno_BBCH)-1)"}>} Day_Ref_ModelloBBCH),
        Avg({<BBCH_Modello={'81'},Anno_BBCH={"$(=Max(Anno_BBCH)-1)"}>} Day_Ref_ModelloBBCH)
      )
    ),
    IdVigneto
  )
)

//23/03/2026
`
- **kpi** `GTJa`
  - misura [S14] «STORICO 10 ANNI» · formato D DD/MM (2 dec.): `//avg(aggr(Avg({<BBCH_Modello={'81','82','83'},Anno_BBCH={">=$(=Max(Anno_BBCH)-10)		<=$(=Max(Anno_BBCH)-1)"}	>} Day_Ref_ModelloBBCH),Anno_BBCH,IdVigneto))

avg(
  aggr(
    if(
      Count({<BBCH_Modello={'83'},Anno_BBCH={">=$(=Max(Anno_BBCH)-10)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH) > 0,
      Avg({<BBCH_Modello={'83'},Anno_BBCH={">=$(=Max(Anno_BBCH)-10)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH),
      if(
        Count({<BBCH_Modello={'82'},Anno_BBCH={">=$(=Max(Anno_BBCH)-10)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH) > 0,
        Avg({<BBCH_Modello={'82'},Anno_BBCH={">=$(=Max(Anno_BBCH)-10)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH),
        Avg({<BBCH_Modello={'81'},Anno_BBCH={">=$(=Max(Anno_BBCH)-10)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH)
      )
    ),
    IdVigneto
  )
)

//23/03/2026`
- **kpi** `xrS`
  - misura [S14] «STORICO 5 ANNI» · formato D DD/MM (2 dec.): `//avg(aggr(Avg({<BBCH_Modello={'81','82','83'},Anno_BBCH={">=$(=Max(Anno_BBCH)-5)		<=$(=Max(Anno_BBCH)-1)"}	>} Day_Ref_ModelloBBCH),Anno_BBCH,IdVigneto))

avg(
  aggr(
    if(
      Count({<BBCH_Modello={'83'},Anno_BBCH={">=$(=Max(Anno_BBCH)-5)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH) > 0,
      Avg({<BBCH_Modello={'83'},Anno_BBCH={">=$(=Max(Anno_BBCH)-5)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH),
      if(
        Count({<BBCH_Modello={'82'},Anno_BBCH={">=$(=Max(Anno_BBCH)-5)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH) > 0,
        Avg({<BBCH_Modello={'82'},Anno_BBCH={">=$(=Max(Anno_BBCH)-5)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH),
        Avg({<BBCH_Modello={'81'},Anno_BBCH={">=$(=Max(Anno_BBCH)-5)		<=$(=Max(Anno_BBCH-1))"}>} Day_Ref_ModelloBBCH)
      )
    ),
    IdVigneto
  )
)

//23/03/2026`
- **kpi** `ATZzQXJ`
  - misura [S13] «=='ANNATA '& (max(Anno_BBCH))» · formato D DD/MM (2 dec.): `avg(aggr(Avg({<Anno_Vendemmia={"$(=Max(Anno_Vendemmia))"}>}Day_Ref_Vendemmia),Anno_Vendemmia,IdVigneto))`
- **kpi** `dfzagMZ`
  - misura [S13] «=='ANNATA '& (max(Anno_BBCH)-1)» · formato D DD/MM (2 dec.): `avg(aggr(Avg({<Anno_Vendemmia={"$(=Max(Anno_Vendemmia)-1)"}>}Day_Ref_Vendemmia),Anno_Vendemmia,IdVigneto))`
- **kpi** `LUndK`
  - misura [S13] «STORICO 5 ANNI» · formato D DD/MM (2 dec.): `avg(aggr(Avg({<Anno_Vendemmia={">=$(=Max(Anno_Vendemmia)-5)		<=$(=Max(Anno_Vendemmia)-1)"}	>}Day_Ref_Vendemmia),Anno_Vendemmia,IdVigneto))`
- **kpi** `dxpB`
  - misura [S13] «STORICO 10 ANNI» · formato D DD/MM (2 dec.): `avg(aggr(Avg({<Anno_Vendemmia={">=$(=Max(Anno_Vendemmia)-10)		<=$(=Max(Anno_Vendemmia)-1)"}	>}Day_Ref_Vendemmia),Anno_Vendemmia,IdVigneto))`
- **text-image** `LjxzgtC`
- **text-image** `EVRvYbC`
  - testo IT: =[](AAmhM)
  - misura [S01] «=='Andamento termo pluviometrico'&' '& 
min({<DatiMeteoStorico={'1'},Mese_Meteo» · formato automatico: `='Fenologia'
`

## Slide 8. Confronto BBCH  - Anno in Corso - Vitigno  ·  ENG: (non nell'app ENG)
- **linechart** `eXwSs` — «Fenologia - Foglie»
  - dimensione: `=WEEK_REF` (Settimana)
  - dimensione: `Vitigno`
  - misura [S14] «BBCH» · formato automatico: `avg(aggr(Avg({<[Anno]={"$(=Max(Anno))"},BBCH_Rilievo={'10','11','12','13','14','15','16','17','18','19','20','21','22','23','24','25'}>}BBCH_Rilievo),WEEK_REF,IdVigneto,Anno))
`
- **text-image** `HKSakF`
- **text-image** `Kycq`
  - testo IT: =[](AAmhM)
  - misura [S01] «» · formato automatico: `='Fenologia - Confronto Vitigni'
`

## Slide 9. Confronto BBCH  - Anno in Corso - Vitigno (1)  ·  ENG: (non nell'app ENG)
- **linechart** `ba0f2f43-0125-4584-a408-2636db088fa5` — «Fenologia - Grappoli»
  - dimensione: `=WEEK_REF` (Settimana)
  - dimensione: `Vitigno`
  - misura [S14] «BBCH» · formato automatico: `avg(aggr(Avg({<[Anno]={"$(=Max(Anno))"},BBCH_Rilievo={'53','55','57','60','61','62','63','64','65','66','67','68','69','70','72','73','75','77','79','80','81','82','83','84','85'}>}BBCH_Rilievo),WEEK_REF,IdVigneto,Anno))
`
- **text-image** `4631c304-ea3a-44cb-b6d6-4c7915b893f6`
- **text-image** `68cc6cfb-b640-4c9d-a72f-3c082d14b718`
  - testo IT: =[](AAmhM)
  - misura [S01] «» · formato automatico: `='Fenologia - Confronto Vitigni'
`

## Slide 10. Confronto BBCH  - Storico  ·  ENG: (non nell'app ENG)
- **linechart** `vhrtBq` — «Fenologia - Foglie (media tutti i vitigni)»
  - dimensione: `=//week_Ref_ModelloBBCH
WEEK_REF` (Settimana)
  - misura [S14] «BBCH Anno in Corso» · formato automatico: `
avg(aggr(Avg({<[Anno]={"$(=Max(Anno))"},BBCH_Rilievo={'10','11','12','13','14','15','16','17','18','19','20','21','22','23','24','25'}>}BBCH_Rilievo),WEEK_REF,IdVigneto,Anno))
`
  - misura [S14] «BBCH Anno Precedente» · formato automatico: `avg(aggr(Avg({<[Anno]={"$(=Max(Anno)-1)"},BBCH_Rilievo={'10','11','12','13','14','15','16','17','18','19','20','21','22','23','24','25'}>}BBCH_Rilievo),WEEK_REF,IdVigneto,Anno))`
  - misura [S14] «BBCH Media Storica» · formato automatico: `avg(aggr(Avg({<[Anno]={">=$(=Max(Anno)-5)		<=$(=Max(Anno)-1)"},BBCH_Rilievo={'10','11','12','13','14','15','16','17','18','19','20','21','22','23','24','25'}>}BBCH_Rilievo),WEEK_REF,IdVigneto,Anno))

`
- **text-image** `UTuJ`
  - testo IT: =[](AAmhM)
  - misura [S01] «» · formato automatico: `='Fenologia - Confronto Storico'
`
- **text-image** `RwYjdN`

## Slide 11. Confronto BBCH  - Storico (1)  ·  ENG: (non nell'app ENG)
- **linechart** `96503fe8-3fa9-4d22-93f0-3665d4ef4f26` — «Fenologia - Grappoli (media tutti i vitigni)»
  - dimensione: `=WEEK_REF` (Settimana)
  - misura [S14] «BBCH Anno in Corso» · formato automatico: `avg(aggr(Avg({<[Anno]={"$(=Max(Anno))"},BBCH_Rilievo={'53','55','57','60','61','62','63','64','65','66','67','68','69','70','72','73','75','77','79','80','81','82','83','84','85'}>}BBCH_Rilievo),WEEK_REF,IdVigneto,Anno))
`
  - misura [S14] «BBCH Anno Precedente» · formato automatico: `avg(aggr(Avg({<[Anno]={"$(=Max(Anno)-1)"},BBCH_Rilievo={'53','55','57','60','61','62','63','64','65','66','67','68','69','70','72','73','75','77','79','80','81','82','83','84','85'}>}BBCH_Rilievo),WEEK_REF,IdVigneto,Anno))`
  - misura [S14] «BBCH Media Storica» · formato automatico: `avg(aggr(Avg({<[Anno]={">=$(=Max(Anno)-5)		<=$(=Max(Anno)-1)"},BBCH_Rilievo={'53','55','57','60','61','62','63','64','65','66','67','68','69','70','72','73','75','77','79','80','81','82','83','84','85'}>}BBCH_Rilievo),WEEK_REF,IdVigneto,Anno))
`
- **text-image** `b290d0c9-8de4-4dd6-b51f-7a85fe4d5aa3`
  - testo IT: =[](AAmhM)
  - misura [S01] «» · formato automatico: `='Fenologia - Confronto Storico'
`
- **text-image** `69d6ec25-7841-4e5c-a403-8be09b67bd92`

## Slide 12. Vigoria  ·  ENG: Vigour
- **text-image** `bsgkcf`
  - testo IT: =[](PJdcW)
  - misura [S99] «» · formato automatico: `max(Anno)-1`
- **text-image** `PzCNaF`
  - testo IT: =[](AAmhM)
  - misura [S01] «=='Andamento termo pluviometrico'&' '& 
min({<DatiMeteoStorico={'1'},Mese_Meteo» · formato automatico: `='Vigoria'
`
- **kpi** `BUeLmmS`
  - misura [S02] «=='Valutazione Vigoria'» · formato automatico: `IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria))"}>}Punteggio),IdVigneto))=10,('Vigoria Eccessiva'),
IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria))"}>}Punteggio),IdVigneto))>=8.8,('Vigoria Molto Alta'),
IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria))"}>}Punteggio),IdVigneto))>=7.5,('Vigoria Alta'),
IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria))"}>}Punteggio),IdVigneto))>=6.2,('Vigoria Medio/Alta'),
IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria))"}>}Punteggio),IdVigneto))>=4.9,('Vigoria Media'),
IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria))"}>}Punteggio),IdVigneto))>=3.6,('Vigoria Medio/Bassa'),
IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria))"}>}Punteggio),IdVigneto))>=2.3,('Vigoria Bassa'),
IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria))"}>}Punteggio),IdVigneto))>=1,('Vigoria Molto Bassa'),
))))))))





`
- **kpi** `gARuuHH`
  - misura [S02] «=='Valutazione Vigoria'» · formato automatico: `IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria)-1)"}>}Punteggio),IdVigneto))=10,('Vigoria Eccessiva'),
IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria)-1)"}>}Punteggio),IdVigneto))>=8.8,('Vigoria Molto Alta'),
IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria)-1)"}>}Punteggio),IdVigneto))>=7.5,('Vigoria Alta'),
IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria)-1)"}>}Punteggio),IdVigneto))>=6.2,('Vigoria Medio/Alta'),
IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria)-1)"}>}Punteggio),IdVigneto))>=4.9,('Vigoria Media'),
IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria)-1)"}>}Punteggio),IdVigneto))>=3.6,('Vigoria Medio/Bassa'),
IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria)-1)"}>}Punteggio),IdVigneto))>=2.3,('Vigoria Bassa'),
IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria)-1)"}>}Punteggio),IdVigneto))>=1,('Vigoria Molto Bassa'),
))))))))


`
- **distributionplot** `mhwRzF` — «=='Vigoria media '&max(Anno_Indice)»
  - ENG titolo: «=='average vigour '&max(Anno_Indice)»
  - dimensione: `=NomeVigneto`
  - dimensione: `=Anno_Vigoria`
  - misura [S10] «Classe Vigoria» · formato automatico: `avg(aggr(avg(Punteggio),IdVigneto,Anno_Vigoria))
`
- **kpi** `aZezns`
  - misura [S02] «=='Valutazione Vigoria'» · formato automatico: `IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria)-2)"}>}Punteggio),IdVigneto))=10,('Vigoria Eccessiva'),
IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria)-2)"}>}Punteggio),IdVigneto))>=8.8,('Vigoria Molto Alta'),
IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria)-2)"}>}Punteggio),IdVigneto))>=7.5,('Vigoria Alta'),
IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria)-2)"}>}Punteggio),IdVigneto))>=6.2,('Vigoria Medio/Alta'),
IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria)-2)"}>}Punteggio),IdVigneto))>=4.9,('Vigoria Media'),
IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria)-2)"}>}Punteggio),IdVigneto))>=3.6,('Vigoria Medio/Bassa'),
IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria)-2)"}>}Punteggio),IdVigneto))>=2.3,('Vigoria Bassa'),
IF(avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria)-2)"}>}Punteggio),IdVigneto))>=1,('Vigoria Molto Bassa'),
))))))))


`
- **gauge** `DuRP` — «=='Vigoria media '&max(Anno_Indice)»
  - ENG titolo: «=='Average vigour '&max(Anno_Indice)»
  - misura [S10] «classe» · formato automatico: `avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria))"}>}Punteggio),IdVigneto))`
  - contachilometri: min 0 · max 10 · fasce True · limiti [0.86, 2.24, 3.325, 5.229, 6.7] · colori ['#f8981d', '#ffcf02', '#46c646', '#276e27', '#ffcf02', '#f8981d']
- **gauge** `kGhLpDP` — «=='Vigoria media '&max(Anno_Indice)»
  - misura [S10] «classe» · formato automatico: `avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria)-2)"}>}Punteggio),IdVigneto))`
  - contachilometri: min 0 · max 10 · fasce True · limiti [0.86, 2.24, 3.325, 5.229, 6.7] · colori ['#f8981d', '#ffcf02', '#46c646', '#276e27', '#ffcf02', '#f8981d']
- **text-image** `NhmsA`
  - testo IT: =[](PJdcW)
  - misura [S99] «» · formato automatico: `max(Anno)`
- **gauge** `cAZJn` — «=='Vigoria media '&max(Anno_Indice)»
  - misura [S10] «classe» · formato automatico: `avg(aggr(avg({<Anno_Vigoria={"$(=Max(Anno_Vigoria)-1)"}>}Punteggio),IdVigneto))`
  - contachilometri: min 0 · max 10 · fasce True · limiti [0.86, 2.24, 3.325, 5.229, 6.7] · colori ['#f8981d', '#ffcf02', '#46c646', '#276e27', '#ffcf02', '#f8981d']
- **text-image** `JEWjejq`
- **text-image** `aLQNMr`
  - testo IT: =[](PJdcW)
  - misura [S99] «» · formato automatico: `max(Anno)-2`

## Slide 13. Lunghezza Germogli  ·  ENG: Shoot length
- **linechart** `pVRH` — «Andamento lunghezza germogli (cm)»
  - ENG titolo: «=='Shoot lenght trend (cm)'»
  - dimensione: `=//=WEEK_REF_GERMOGLIO
Week_germoglio&' - '&DAY_WEEK_REF_GERMOGLIO` (Settimana di RIferimento)
  - dimensione: `=NomeVigneto_germoglio`
  - misura [S99] «Lunghezza Germoglio (cm)» · formato automatico: `avg(aggr(avg({<Anno={"$(=Max(Anno))"}>}LungMediaGermogliPerVite),IdVigneto,Week_germoglio))

`
- **linechart** `XuT` — «Confronto Storico - Lunghezza Germogli»
  - ENG titolo: «=='Historical comparison - shoot length'»
  - dimensione: `=//=WEEK_REF

Week_germoglio&' - '&DAY_WEEK_REF_GERMOGLIO` (Settimana di Riferimento)
  - dimensione: `=Anno_germoglio` (Anno)
  - misura [S99] «Lunghezza Germoglio (cm)» · formato automatico: `

avg(aggr(avg(LungMediaGermogliPerVite),IdVigneto,Week_germoglio,Anno))`
- **text-image** `QVWUBhY`
- **text-image** `PaMLbq`
  - testo IT: =[](AAmhM)
  - misura [S01] «=='Andamento termo pluviometrico'&' '& 
min({<DatiMeteoStorico={'1'},Mese_Meteo» · formato automatico: `='Lunghezza Germogli'
`

## Slide 14. Deficit Idrico  ·  ENG: Water stress
- **linechart** `f0ee1f90-f912-4724-89d8-bead6cd7ef30` — «=//='ANDAMENTO DEFICIT IDRICO'& ' '&max(Anno_stress) ='VALUTAZIONE DELLO STRESS IDRICO - METODO DEGLI APICI - CONFRONTO »
  - ENG titolo: «ANDAMENTO DEFICIT IDRICO»
  - dimensione: `=//=Week_stress&' - '&DAY_WEEK_REF_STRESS
WEEK_REF_STRESS` (Settimana di Riferimento)
  - dimensione: `Vigna`
  - misura [S12] «Valutazione Stato Idrico» · formato automatico: `avg(aggr(avg({<idTipoRilievo={'146'},Anno_stress={"$(=Max(Anno_stress))"},Mese={'Mag','Giu','Lug','Ago','Set'}>}IdValutazioneStatoIdrico),IdVigneto,WEEK_REF_STRESS))

*
-1

`
- **gauge** `64bf466e-8d51-444e-af96-c52a42aab4e0` — «Stato Idrico»
  - misura [S12] «Stato Idrico» · formato automatico: `avg(aggr(max({<idTipoRilievo={'146'},Anno_stress={"$(=Max(Anno_stress))"},Mese={'Mag','Giu','Lug','Ago','Set'}>}IdValutazioneStatoIdrico),IdVigneto))

`
  - contachilometri: min 0 · max 4 · fasce True · limiti [0.5, 1.04, 2.18, 3.175, 3.875] · colori ['#ffcf02', '#46c646', '#276e27', '#276e27', '#276e27', '#ffcf02']
- **kpi** `888e943b-c6b4-491f-b3a8-c93a2ab15364`
  - misura [S02] «=='Valutazione Stato Idrico'» · formato automatico: `IF(avg(aggr(max({<idTipoRilievo={'146'},Anno_stress={"$(=Max(Anno_stress))"},Mese={'Mag','Giu','Lug','Ago','Set'}>}IdValutazioneStatoIdrico),IdVigneto))=4,('Stress idrico Molto severo'),
IF(avg(aggr(max({<idTipoRilievo={'146'},Anno_stress={"$(=Max(Anno_stress))"},Mese={'Mag','Giu','Lug','Ago','Set'}>}IdValutazioneStatoIdrico),IdVigneto))>=3,('Stress idrico Severo'),
IF(avg(aggr(max({<idTipoRilievo={'146'},Anno_stress={"$(=Max(Anno_stress))"},Mese={'Mag','Giu','Lug','Ago','Set'}>}IdValutazioneStatoIdrico),IdVigneto))>=2,('Stress idrico Medio'),
IF(avg(aggr(max({<idTipoRilievo={'146'},Anno_stress={"$(=Max(Anno_stress))"},Mese={'Mag','Giu','Lug','Ago','Set'}>}IdValutazioneStatoIdrico),IdVigneto))>=1,('Stress idrico Lieve'),
IF(avg(aggr(max({<idTipoRilievo={'146'},Anno_stress={"$(=Max(Anno_stress))"},Mese={'Mag','Giu','Lug','Ago','Set'}>}IdValutazioneStatoIdrico),IdVigneto))>=0,('Stress idr`
- **kpi** `1ff2e0b9-8547-46c4-acf1-019cc0924e87`
  - misura [S02] «=='Valutazione Stato Idrico'» · formato automatico: `IF(avg(aggr(max({<idTipoRilievo={'146'},Anno_stress={"$(=Max(Anno_stress)-1)"},Mese={'Mag','Giu','Lug','Ago','Set'}>}IdValutazioneStatoIdrico),IdVigneto))=4,('Stress idrico Molto severo'),
IF(avg(aggr(max({<idTipoRilievo={'146'},Anno_stress={"$(=Max(Anno_stress)-1)"},Mese={'Mag','Giu','Lug','Ago','Set'}>}IdValutazioneStatoIdrico),IdVigneto))>=3,('Stress idrico Severo'),
IF(avg(aggr(max({<idTipoRilievo={'146'},Anno_stress={"$(=Max(Anno_stress)-1)"},Mese={'Mag','Giu','Lug','Ago','Set'}>}IdValutazioneStatoIdrico),IdVigneto))>=2,('Stress idrico Medio'),
IF(avg(aggr(max({<idTipoRilievo={'146'},Anno_stress={"$(=Max(Anno_stress)-1)"},Mese={'Mag','Giu','Lug','Ago','Set'}>}IdValutazioneStatoIdrico),IdVigneto))>=1,('Stress idrico Lieve'),
IF(avg(aggr(max({<idTipoRilievo={'146'},Anno_stress={"$(=Max(Anno_stress)-1)"},Mese={'Mag','Giu','Lug','Ago','Set'}>}IdValutazioneStatoIdrico),IdVigneto))>=0,('`
- **text-image** `mspUNY`
  - testo IT: =[](AAmhM)
  - misura [S01] «=='Andamento termo pluviometrico'&' '& 
min({<DatiMeteoStorico={'1'},Mese_Meteo» · formato automatico: `='Valutazione dello Stress Idrico'&chr(10)&
'Metodo degli apici'&chr(10)&
'Confronto tra vigne'
`
- **kpi** `be2a984d-e984-4c67-87c8-4b0059e10b5f`
  - misura [S02] «=='Valutazione Stato Idrico'» · formato automatico: `IF(avg(aggr(max({<idTipoRilievo={'146'},Anno_stress={"$(=Max(Anno_stress)-2)"},Mese={'Mag','Giu','Lug','Ago','Set'}>}IdValutazioneStatoIdrico),IdVigneto))=4,('Stress idrico Molto severo'),
IF(avg(aggr(max({<idTipoRilievo={'146'},Anno_stress={"$(=Max(Anno_stress)-2)"},Mese={'Mag','Giu','Lug','Ago','Set'}>}IdValutazioneStatoIdrico),IdVigneto))>=3,('Stress idrico Severo'),
IF(avg(aggr(max({<idTipoRilievo={'146'},Anno_stress={"$(=Max(Anno_stress)-2)"},Mese={'Mag','Giu','Lug','Ago','Set'}>}IdValutazioneStatoIdrico),IdVigneto))>=2,('Stress idrico Medio'),
IF(avg(aggr(max({<idTipoRilievo={'146'},Anno_stress={"$(=Max(Anno_stress)-2)"},Mese={'Mag','Giu','Lug','Ago','Set'}>}IdValutazioneStatoIdrico),IdVigneto))>=1,('Stress idrico Lieve'),
IF(avg(aggr(max({<idTipoRilievo={'146'},Anno_stress={"$(=Max(Anno_stress)-2)"},Mese={'Mag','Giu','Lug','Ago','Set'}>}IdValutazioneStatoIdrico),IdVigneto))>=0,('`
- **gauge** `KvkJfwb` — «Stato Idrico»
  - misura [S12] «Stato Idrico» · formato automatico: `avg(aggr(max({<idTipoRilievo={'146'},Anno_stress={"$(=Max(Anno_stress)-1)"},Mese={'Mag','Giu','Lug','Ago','Set'}>}IdValutazioneStatoIdrico),IdVigneto))`
  - contachilometri: min 0 · max 4 · fasce True · limiti [0.5, 1.04, 2.18, 3.175, 3.875] · colori ['#ffcf02', '#46c646', '#276e27', '#276e27', '#276e27', '#ffcf02']
- **text-image** `7050c24f-7011-4cb4-afea-5ebf28141e97`
  - testo IT: =[](PJdcW)
  - misura [S99] «» · formato automatico: `Max(Anno_stress)`
- **gauge** `ppxrSms` — «Stato Idrico»
  - misura [S12] «Stato Idrico» · formato automatico: `avg(aggr(max({<idTipoRilievo={'146'},Anno_stress={"$(=Max(Anno_stress)-2)"},Mese={'Mag','Giu','Lug','Ago','Set'}>}IdValutazioneStatoIdrico),IdVigneto))`
  - contachilometri: min 0 · max 4 · fasce True · limiti [0.5, 1.04, 2.18, 3.175, 3.875] · colori ['#ffcf02', '#46c646', '#276e27', '#276e27', '#276e27', '#ffcf02']
- **text-image** `jdmqyvt`
  - testo IT: =[](PJdcW)
  - misura [S99] «» · formato automatico: `max(Anno_stress)-1`
- **text-image** `MkMchu`
- **text-image** `teTCJa`
  - testo IT: =[](PJdcW)
  - misura [S99] «» · formato automatico: `max(Anno_stress)-2`

## Slide 15. Tignole  ·  ENG: Grape moths
- **linechart** `pJVPYkk` — «Catture per trappola»
  - ENG titolo: «Catches per trap»
  - dimensione: `=WEEK_REF` (Settima di Riferimento)
  - dimensione: `=If([idTipoRilievo] = '148' or [idTipoRilievo]='149' or [idTipoRilievo]='153' or [idTipoRilievo]='155', [TipoRilevo])` (Tipo Rilievo)
  - misura [S99] «Catture Totali» · formato automatico: `avg(aggr(avg({<idTipoRilievo={'148','149','153','155'},Anno={"$(=Max(Anno))"}>}CatturePerTrappola),WEEK_REF,[IdVigneto], idTipoRilievo))


`
- **text-image** `JXmpaS`
  - testo IT: =[](AAmhM)
  - misura [S01] «=='Andamento termo pluviometrico'&' '& 
min({<DatiMeteoStorico={'1'},Mese_Meteo» · formato automatico: `='Andamento Volo Tignole'
`
- **text-image** `ZFytg`

## Slide 16. Tignole (1)  ·  ENG: Grape moths (1)
- **gauge** `9247289f-e348-475e-a1e6-d0eed0af1a92` — «Tignole larve di terza generazione»
  - ENG titolo: «Third generation larvae»
  - misura [S18b] «nidi larvali ogni 100 grappoli» · formato automatico: `if(
(Avg(aggr(Max({<idEtaEpocaGenerazione={"1330","430"},Anno={"$(=Max(Anno))"}>}PercDiffusione ),IdVigneto)))
>0,
(Avg(aggr(Max({<idEtaEpocaGenerazione={"1330","430"},Anno={"$(=Max(Anno))"}>}PercDiffusione ),IdVigneto))),
0)

`
  - contachilometri: min 0 · max 100 · fasce True · limiti [1, 4, 10] · colori ['#46c646', '#ffcf02', '#f93f17', '#8e477d']
- **text-image** `66a1cc9f-dc42-4cea-9850-b3a8cec89dca`
- **gauge** `9f8e07e3-0f61-41c4-b7bf-021f50f95783` — «Tignole larve di seconda generazione»
  - ENG titolo: «Second generation larve»
  - misura [S18b] «nidi larvali ogni 100 grappoli» · formato automatico: `if(
(Avg(aggr(Max({<idEtaEpocaGenerazione={"1320","420"},Anno={"$(=Max(Anno))"}>}PercDiffusione ),IdVigneto)))
>0,
(Avg(aggr(Max({<idEtaEpocaGenerazione={"1320","420"},Anno={"$(=Max(Anno))"}>}PercDiffusione ),IdVigneto))),
0)

`
  - contachilometri: min 0 · max 100 · fasce True · limiti [5, 15, 20] · colori ['#46c646', '#ffcf02', '#f93f17', '#8e477d']
- **barchart** `5e9dd138-2181-441b-af89-d3384dbb82d0` — «=='Diffusione Tignole massima rilevata durante la stagione'»
  - ENG titolo: «=='Moths incidence»
  - dimensione: `Vigna`
  - dimensione: `NomeVigneto` (Vigneto)
  - misura [S99] «=='diffusione % '» · formato automatico: `//avg(aggr(max({<idTipoRilievo={'13'},Anno={"$(=Max(Anno))"}>}PercDiffusione),[IdVigneto]))
sum(aggr(max({<idTipoRilievo={'13'},Anno={"$(=Max(Anno))"}>}PercDiffusione),[IdVigneto]))`
- **text-image** `2e00a19d-169e-42c1-8b6a-a97f78a19c05`
  - testo IT: =[](AAmhM)
  - testo ENG: Grape moths
  - misura [S01] «=='Andamento termo pluviometrico'&' '& 
min({<DatiMeteoStorico={'1'},Mese_Meteo» · formato automatico: `='Presenza Tignole (valutazione nidi larvali)'
`
- **gauge** `SMeujc` — «Tignole larve di prima generazione»
  - ENG titolo: «First generation larvae»
  - misura [S18b] «nidi larvali ogni 100 grappoli» · formato automatico: `if(
(Avg(aggr(Max({<idEtaEpocaGenerazione={"1310","410"},Anno={"$(=Max(Anno))"}>}PercDiffusione ),IdVigneto)))
>0,
(Avg(aggr(Max({<idEtaEpocaGenerazione={"1310","410"},Anno={"$(=Max(Anno))"}>}PercDiffusione ),IdVigneto))),
0)

`
  - contachilometri: min 0 · max 100 · fasce True · limiti [20, 45.4, 75.4] · colori ['#46c646', '#ffcf02', '#f93f17', '#8e477d']
- **kpi** `fSQbvN`
  - misura [S18b] «Vigneti controllati prima generazione» · formato automatico: `(sum(Aggr(avg({<idEtaEpocaGenerazione={"1310","410"},Anno={"$(=Max(Anno))"}>}Superficie),IdVigneto))/10000)
/
(sum(Aggr(avg(Superficie),IdVigneto))/10000)`
- **kpi** `VYmJYWp`
  - misura [S18b] «Vigneti controllati seconda generazione» · formato automatico: `(sum(Aggr(avg({<idEtaEpocaGenerazione={"1320","420"},Anno={"$(=Max(Anno))"}>}Superficie),IdVigneto))/10000)
/
(sum(Aggr(avg(Superficie),IdVigneto))/10000)`
- **kpi** `Uaxxyh`
  - misura [S18b] «Vigneti controllati terza generazione» · formato automatico: `(sum(Aggr(avg({<idEtaEpocaGenerazione={"1330","430"},Anno={"$(=Max(Anno))"}>}Superficie),IdVigneto))/10000)
/
(sum(Aggr(avg(Superficie),IdVigneto))/10000)`

## Slide 17. Mal dell'Esca  ·  ENG: Esca disease
- **barchart** `QwstNv` — «=='Diffusione Mal dell Esca '&max(Anno)»
  - ENG titolo: «=='Esca disease incidence '&max(Anno)»
  - dimensione: `=NomeVigneto`
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
  - misura [S15] «Numero Rilievi» · formato automatico: `Count({<idTipoRilievo={"9"},Anno={"$(=Max(Anno))"}>}IdRilievo)`
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
- **text-image** `mWqsc`
- **text-image** `pmmsm`
  - testo IT: MAL DELL'ESCA
  - testo ENG: ESCA DISEASE

## Slide 18. CONSIDERAZIONI  ·  ENG: (non nell'app ENG)
- **text-image** `9a0928f1-3e77-4ebb-910d-fd5644ab0529`
  - testo IT: CONSIDERAZIONI
- **text-image** `52a1a642-eada-4a08-a3a8-0b1d27e43e7f`

## Differenze nel report ENG
- Slide ENG: 12 (IT 18).
- Fogli nel report ENG e non in quello IT: nessuno.
- Fogli ripetuti nel report ENG: nessuno.
- Fogli del report IT che mancano nel report ENG: ['Grafico Foglia', 'Confronto BBCH  - Anno in Corso - Vitigno', 'Confronto BBCH  - Anno in Corso - Vitigno (1)', 'Confronto BBCH  - Storico', 'Confronto BBCH  - Storico (1)', 'CONSIDERAZIONI'].

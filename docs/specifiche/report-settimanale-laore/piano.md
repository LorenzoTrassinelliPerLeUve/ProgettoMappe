# Report settimanale LAORE (task #217)

Fonte: `dati-app-REPORT-SETTIMANALE-LAORE.json` (app Qlik «REPORT SETTIMANALE - LAORE», 17 fogli). Struttura e schemi
del principale (task #216) in `~/progetti/lavori/2026-11-13-report-settimanale/laore/`: **nessuna automazione PowerPoint**
usa l'app, nessuna versione ENG; ordine delle slide = ordine dei fogli.

## Decisioni (boss, 14/11 13:05, provvisorie «tale e quale a Qlik», da confermare col proprietario)
1. Perimetro come lo script (dba, #218): aziende `IdTipoEnte = 1` con vigneti in Sardegna
   (`VignetoMaps.AdministrativeAreaLevel1 = 'Sardegna'`), senza filtri su gruppo, estirpo, carico: 173 enti, 1.104
   vigneti (SS 544, SU 210, NU 210, OR 103, CA 32, OT 5). **Non** `Perimetro:Gruppi` dell'Api e non il gruppo 32.
2. La pagina sceglie **Provincia e Comune** (come i filtri di Qlik) e la settimana; niente azienda.
3. Il dba ha aggiunto il perimetro alle tabelle RS_ dello snapshot (task #219, `dati-dba.md`): ogni tabella ha le colonne
   0/1 `VTS` e `LAORE` (una riga può valere per tutti e due), `RS_RiepilogoSettimana` la colonna `Perimetro`. Il portale
   legge `LAORE = 1` per questo report e `VTS = 1` / `Perimetro = 'VTS'` per il settimanale (verificato: parità del
   settimanale invariata). Con uno snapshot senza le colonne il LAORE ricostruisce il perimetro (tipo 1 + Sardegna).
4. **Estirpati inclusi come Qlik** (lo script non filtra Estirpo/Carico; `RS_Vigneto` ha le colonne se il proprietario
   deciderà di escluderli).

## Com'è fatto
- Api `ReportSettimanaleLaoreController`: `GET api/report-settimanale-laore/luoghi` (vigneti del perimetro con
  provincia e comune) e `GET …/rilievi?da=&a=&provincia=&comune=` (stesso `RilieviSettimanaDto` del settimanale, con
  l'esca); 204 senza snapshot. Lettura in `SnapshotReportFineStagione.LeggiLuoghiLaoreAsync`/`LeggiRilieviLaoreAsync`,
  stessa query di rilievi ed esca del settimanale (`RilieviEscaRsAsync`, cambia solo il filtro).
- Web `ReportSettimanaleLaore/`: `CalcoloLaore` (schemi S24, S25, S26, S14, S15 per provincia; dove la formula è la
  stessa usa `CalcoloSettimanale`), `SezioniLaore` (17 fogli), `TestiLaore` (solo IT; i testi comuni da
  `TestiSettimanale`), `PowerPointLaore`. Pagina `/report-settimanale-laore`, voce di menu, rotte
  `/report-settimanale-laore/powerpoint|pdf?provincia=&comune=&settimana=` (PDF dalla coda comune, chiave `rsl-…`).
- Test `ReportSettimanaleLaoreTests` (formule, 17 slide valide anche senza dati, nome file, perimetro dello snapshot).

## Differenze volute (sezione LAORE di decisioni.md)
- **Nome del file**: nessuna automazione da copiare → «{AAMMGG} - LAORE {territorio} - Report settimanale» come il
  settimanale IT (territorio = «Sardegna», la provincia o «Comune (PR)»).
- **Settimana**: KPI e barre sulla sola settimana scelta, linee dall'inizio dell'anno a quella settimana (in Qlik, con la
  `Week` selezionata, un punto solo): stessa scelta del settimanale (#214).
- **Vigneti monitorati**: vigneti distinti dei rilievi (`IdVigneto`), non `IdVignetoDaApp`; il cruscotto mostra in più i
  vigneti del perimetro nel territorio.
- **Controlli in cima** (non ci sono in Qlik): `RS_RiepilogoSettimana` con `Perimetro = 'LAORE'` delle aziende con almeno
  un vigneto nel territorio, sommato per settimana (un'azienda a cavallo di due province conta intera in tutte e due).
- **Meteo**: slide in più come nel settimanale («Andamento termo pluviometrico», ultima), media dei punti meteo del
  territorio (`RS_MeteoSettimana` con `LAORE = 1`).
- **Mal dell'esca**: Qlik raggruppa per `AdministrativeAreaLevel2` con l'etichetta «Comune»: il portale scrive «Provincia».
- **Nome del file**: confermato dal boss il 14/11.

## Limiti
- **Meteo quasi sempre vuoto**: 165 aziende LAORE su 173 non hanno un punto meteo (dba #219, controllo S4); la slide
  meteo dice «nessun punto meteo» per quasi tutti i territori.
- A novembre i rilievi sono pochissimi (4 in tutta la Sardegna nella settimana 45): per provare si sceglie una settimana
  della stagione (es. 2026-23: 844 rilievi su 142 vigneti).

## Parità (task #220): 102 numeri su 102
- `MisureLaore.Calcola`: ogni numero con l'id dell'oggetto Qlik di `struttura.md` («id» KPI, «id|provincia» barre,
  «id|provincia|n» grafico dell'esca, «b8f73493…|tipo» rilievi per tipo, «tVdyY|provincia|n» foglio Filtri, linee = il punto
  della settimana scelta, «id|n» per le tre linee dei confronti storici).
- Test `Parita_SullaVeritaDiQlik` (`ParitaLaoreQlik`): la verità è per **azienda** (RagioneSociale + Week); il test prende i
  vigneti LAORE dell'azienda, l'anno che Qlik ha mostrato e i rilievi fino al 28/07/2026 (Qlik non ha quelli dal 29/07).
- **5 selezioni su 9 cadute** in Qlik: l'azienda non aveva rilievi in quella settimana (46 per tutte e tre, 45 per Jerzu e
  Monastir) e Qlik mostra i rilievi di altre aziende (3 rilievi in settimana 46, 21 in provincia CA in settimana 45, uguali per
  tutte). Si confrontano le altre 4 (Podere Guardia Grande 45 e 30, Jerzu 30, Monastir 30): **tutti i 102 numeri tornano**, e
  nessun numero del portale dove Qlik mostra «-». Nessuna differenza voluta nei numeri.
- Trovato: la colonna `Week` del dba conta il giorno su 365 (22/7/2026: dba 29, Qlik 30). Il portale usa sempre la settimana
  di Qlik dalla data (`SettimanaQlik.Di`); la parità del settimanale migliora (1.698 numeri su 1.736 invece di 1.696).
  Da dire al dba: anche `Week` di `RS_RiepilogoSettimana` (controlli) e `RS_MeteoSettimana` (slide meteo) va rifatta così.

# Report settimanale LAORE (task #217)

Fonte: `dati-app-REPORT-SETTIMANALE-LAORE.json` (app Qlik «REPORT SETTIMANALE - LAORE», 17 fogli). Struttura e schemi
del principale (task #216) in `~/progetti/lavori/2026-11-13-report-settimanale/laore/`: **nessuna automazione PowerPoint**
usa l'app, nessuna versione ENG; ordine delle slide = ordine dei fogli.

## Decisioni (boss, 14/11 13:05, provvisorie «tale e quale a Qlik», da confermare col proprietario)
1. Perimetro come lo script (dba, #218): aziende `IdTipoEnte = 1` con vigneti in Sardegna
   (`VignetoMaps.AdministrativeAreaLevel1 = 'Sardegna'`), senza filtri su gruppo, estirpo, carico: 173 enti, 1.104
   vigneti (SS 544, SU 210, NU 210, OR 103, CA 32, OT 5). **Non** `Perimetro:Gruppi` dell'Api e non il gruppo 32.
2. La pagina sceglie **Provincia e Comune** (come i filtri di Qlik) e la settimana; niente azienda.
3. Il dba aggiunge il perimetro alle tabelle RS_ dello snapshot (task #219). Fino ad allora lo snapshot ha in Sardegna
   solo Siddura e Podere Guardia Grande (73 vigneti): la pagina funziona ma mostra solo quelli.

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
- **Controlli in cima**: per ora solo «settimana senza rilievi»; `RS_RiepilogoSettimana` è per azienda e va riassunto
  per territorio quando il dba lo avrà per il perimetro LAORE.
- **Mal dell'esca**: Qlik raggruppa per `AdministrativeAreaLevel2` con l'etichetta «Comune»: il portale scrive «Provincia».

## Mancano
- Lo snapshot col perimetro LAORE (dba #219), poi la prova sui dati veri.
- La verità di Qlik per la parità (principale #216): senza, nessun confronto dei numeri.

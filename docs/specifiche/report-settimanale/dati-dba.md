# Report settimanale nel portale: i dati (dba, 13/11/2026, task #212)

Fonte: script Qlik «REPORT SETTIMANALE» (~/progetti/qlik/script/REPORT SETTIMANALE/2026-11-12-1715.qvs) e
~/progetti/qlik/docs/dati-app/REPORT SETTIMANALE.json. Solo letture sul DB (agente_lettura, tutto in letture.jsonl).

## Cosa cambia rispetto al Report fine stagione
- **Altre aziende**: gruppi 1, 3, 4, 27, 31, 33, 34 (RFS: 1, 3, 4, 24, 34). In più TIKVES (27, 11 aziende),
  Calzedonia (31, 3), Cusumano (33, 5); in meno il gruppo 24. Totale **78 aziende, 2.801 vigneti attivi**.
- **Rilievi legati all'azienda dell'app** (Rilievo.IdAziendaDaApp), non all'azienda del vigneto.
- **La settimana di Qlik** non è la settimana ISO: è `Week(DayNumberOfYear(data))`, cioè il giorno dell'anno
  portato su una settimana fissa (tabella DOY_WEEK, 366 righe). Le etichette sono in WEEK_REF («45 - 07 Nov»).
- Per questo le fonti del report settimanale sono **tabelle nuove con prefisso RS_** nello stesso rfs.sqlite: le tabelle
  del fine stagione non cambiano.

## Fonti che mancavano, ora nello snapshot (righe al 13/11/2026)
Query pulite in dati/misure/sql/, misure in dati/misure/misure_rs.json.
- RS_Rilievo: 328.589 (dal 2015; 31 campi usati dal report più la colonna Week di Qlik)
- RS_Collezione: 381.646 (mal dell'esca, tipo 9, dal 2025: una riga per organo con ValoreConteggiato)
- RS_Utente: 186 (solo id, nome e cognome: **l'email (LoginName) non si copia**, il report non la usa nelle formule)
- RS_VistaDanniDaAmpelopatie: 2.602 (dal 2025, PercDanno)
- RS_ModelloBBCHSettimana: 286.755 (storico 4g_Storico dal 2017 + attuale, senza origine REGIONE, già per vigneto/anno/settimana:
  BBCH dell'ultimo giorno della settimana e massimo; 1,7 milioni di righe giornaliere ridotte a 287 mila)
- RS_Vendemmia: 4.229 (rilievi con BBCH 89, tutti gli anni)
- RS_VistaVigoria: 6.892 (gli 11 campi del report, dal 2015)
- RS_Documento: 85.978 (foto dei rilievi dal 2025: UrlMedium, UrlThumbnail)
- RS_MeteoSettimana: 4.521 (punti meteo, per settimana, dal 2025: serve al controllo del meteo)
- Anagrafiche del nuovo universo: RS_Ente 78, RS_Componente 94, RS_Gruppo 7, RS_Vigna 1.018, RS_Vigneto 2.801,
  RS_VistaEntiVigneti 2.801, RS_VignetoMaps 2.710, RS_Vitigno 180, RS_SubVigneto 1.997
- Decodifiche: Organo 14, AvversitaCarenza 180, StatoVegetativo 8 (id 1,2,3,6,7,8,9,10 come lo script),
  CategoriaRilievo 9, Sporulazione 12
- Settimane dai CSV del principale: WEEK_REF, WEEK_REF_GERMOGLIO, WEEK_REF_STRESS (53 righe), DOY_WEEK (366)

Non servono tabelle a parte per: VignetiRilevati (= vigneti distinti di RS_Rilievo dell'anno), Vigneto germoglio
(= RS_Rilievo con IdTipoRilievo 113), stress (= tipo 146).

## Controlli per settimana (dati/controlli_rs.py, girano a ogni snapshot)
- RS_RiepilogoSettimana: per azienda e settimana dell'anno (fino a oggi): vigneti attivi, rilevati, rilievi, punti meteo,
  giorni di meteo mancanti o in previsione. Il portale sceglie la settimana.
- _controlli_rs, per l'ultima settimana conclusa (oggi: settimana 45, «45 - 07 Nov»):
  - S1 aziende senza nessun rilievo nella settimana: 67 (normale a novembre)
  - S2 vigneti senza rilievo nelle aziende che hanno rilevato: 9 aziende (es. Casa E. Mirafiore 4 vigneti su 105)
  - S3 meteo mancante o in previsione: 4 aziende (Casa E. Mirafiore 35 giorni mancanti su 8 punti, Sandrone 14, Nebo 7,
    Case Basse - Soldera 7)
  - S4 aziende senza nessun punto meteo: 12
  - S5 rilievi su vigneti di un'altra azienda o non attivi: 0
  - S6 vigneti senza fase BBCH del modello nella settimana: 74 aziende (a novembre il modello è fermo)

## Da sapere
- Lo script **non carica il meteo**, ma l'app ha 130 riferimenti a MeteoGiornaliero (grafico «Andamento termo pluviometrico»):
  in Qlik quei grafici restano vuoti. Nel portale si possono usare le tabelle meteo del fine stagione (Meteo, solo
  gruppi RFS) o RS_MeteoSettimana (tutti i punti meteo del report, dal 2025).
- La fenologia del modello in Qlik usa come dimensione WEEK_REF (week_Ref_ModelloBBCH è commentato): RS_ModelloBBCHSettimana
  usa la stessa settimana dei rilievi.
- Snapshot: 501 MB (era 358), 6 minuti. Programmi: snapshot-rfs.py, dati/misura_rs.py, dati/controlli_rs.py nella
  cartella del fine stagione.

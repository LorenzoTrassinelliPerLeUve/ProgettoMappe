# Report settimanale LAORE: piano (task #217)

Fonte: `dati-app-REPORT-SETTIMANALE-LAORE.json` (app Qlik «REPORT SETTIMANALE - LAORE», 17 fogli, 32 oggetti).
Struttura, regole dello script, automazione e verità arrivano dal principale (task #216).

## Cosa fa l'app di Qlik
- **Non ragiona per azienda**: filtri Provincia (`VignetoMaps.AdministrativeAreaLevel2`) e Comune
  (`AdministrativeAreaLevel3`); quasi tutti i grafici hanno una barra o una linea **per provincia**.
- Tutte le misure sono su `Anno = Max(Anno)` della selezione (niente settimana nei KPI, a parte `WEEK_REF` nelle linee).
- Fogli: Filtri (MalEsca/Tignole presenti, WeekRef) · Dashboard (rilievi per tipo, n. rilievi, organi controllati,
  vigneti monitorati = `IdVignetoDaApp` distinti) · BBCH foglie (10–25) e grappoli (53–89) per settimana e provincia ·
  BBCH storico (anno, anno prima, media 5 anni) foglie e grappoli · peronospora (tipo 12) e oidio (tipo 11),
  intensità e diffusione, su foglia (organi 1, 3, 4, 10) e grappolo (2, 5): media per provincia del massimo per
  vigneto e anno · catture per trappola Cryptoblabes (153) e Lobesia (148) per settimana e provincia · mal dell'esca
  (tipo 9): quota per classe della collezione (0, 10, 30, 40, 50, 60, 100) su `NrOrganiControllati`, più i KPI.

## Perimetro (dba, 14/11)
Lo script di Qlik carica **TipoEnte = 1 con vigneti in Sardegna** (`AdministrativeAreaLevel1 = 'Sardegna'`): 173 enti,
1.104 vigneti, province SS 544, SU 210, NU 210, OR 103, CA 32, OT 5. Non è il gruppo 32 «Laore» (82 enti di quei 173;
86 non sono in nessun gruppo). Rilievi: 2021 21.261 · 2022 18.855 · 2023 16.643 · 2024 23.717 · 2025 13.992 · 2026 14.054.
Lo snapshot `rfs.sqlite` oggi ha solo i gruppi 1, 3, 4, 27, 31, 33, 34 (in Sardegna solo Siddura e Podere Guardia Grande).

## Riuso del settimanale (senza duplicare)
- Dati: `RilievoSettimanaDto` ha già organo, diffusione, intensità, organi controllati, catture, BBCH, IdRilievo, Week;
  `EscaSettimanaDto` la collezione dell'esca. Manca il luogo del vigneto: `RS_VignetoMaps` (Provincia, Comune) da
  leggere nello snapshot, oggi mai usata.
- Lettura: `SnapshotReportFineStagione.LeggiRilieviRsAsync` filtra per `IdAziendaDaApp`; per LAORE va parametrizzato il
  filtro (vigneti del perimetro LAORE, eventualmente provincia/comune) invece di copiare la query.
- Settimane ed etichette: `SettimanaQlik`/`SettimanaReport`; PowerPoint: `PresentazionePptx.GraficoSerie` (colonne per
  provincia, linee per settimana con una serie per provincia); PDF: `ConvertitorePdf` con chiave `rsl-…`.
- Pagina nuova `/report-settimanale-laore` e voce di menu in `MainLayout.razor` (desktop e mobile).

## Da decidere (proprietario)
1. Perimetro: come Qlik (TipoEnte 1 in Sardegna, 173 enti) o solo il gruppo 32 «Laore» (82 enti)? Oggi l'app mostra
   solo i gruppi VTS: è un allargamento dei dati visibili.
2. Selezione della pagina: Provincia/Comune come Qlik, oppure azienda come il settimanale (la task dice «azienda»)?
3. Via libera al dba per aggiungere il perimetro LAORE alle tabelle RS_ dello snapshot.

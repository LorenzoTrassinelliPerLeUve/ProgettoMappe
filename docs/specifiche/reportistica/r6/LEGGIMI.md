# Tappa R6: obiettivi dell'Indice Bigot e «A che punto sono» (task #419)

Progetto: [../PROGETTO.md](../PROGETTO.md) §5. Foto: `giro.sh` (copia dello snapshot `indice-bigot.db` con l'azienda 62 sull'id 1).

## Cosa c'è (giro 1)

- **Obiettivi** (`/indice-bigot/obiettivi?azienda=&anno=`, voce «Obiettivi dei parametri» del menù Report): per parametro e annata
  minimo, obiettivo, massimo; livelli predefinito (tutte le aziende, ente 0) → azienda → vigneto, vince il più specifico.
  Tabella `obiettivi_bigot` nel SQLite della reportistica (`ArchivioReportistica.Obiettivi`), una riga per salvataggio (storico),
  tutti vuoti = tolto. Api `api/indice-bigot/obiettivi` (GET, `storico`, PUT, POST `copia` = «Copia dall'annata scorsa», solo
  quelli che mancano). Controlli in `Contracts/ObiettiviBigot` (scala del parametro, minimo ≤ obiettivo ≤ massimo).
  **Predefiniti solo dagli interni** (Api: `X-Mappe-Utente` con esperienza Interno); clienti in consulenza: niente pagina.
- **Proposte di partenza** (`Web/IndiceBigot/ObiettiviIndice.Proposta`) dalle curve di `dbo.ParametroIndice` (non concessa: righe
  copiate dal dba, sotto-task #424): obiettivo = valore col punteggio migliore, fascia = punteggio ≥ 80 % del migliore; morfologia e
  stress idrico sulle classi intere; sanità = 100 − danno. **Vigoria senza proposta** (4Grapes passa la scala 1–10 a 1–7 con
  `FnS_MapValue`, non nota); **età senza obiettivo** (non si governa in stagione).
- **A che punto sono** (`/indice-bigot/cruscotto?azienda=&vigneto=&anno=`, `Web/IndiceBigot/CruscottoIndice`): dallo snapshot
  notturno. Per parametro valore medio, obiettivo che vale, esito, vigneti in linea / fuori / senza dato, valore finale dell'anno
  prima; «Cosa manca» con il rilievo che serve (dba #424); per vigneto n/9, indice **provvisorio** (somma dei punteggi presenti)
  sul massimo possibile, parametri fuori obiettivo e mancanti.

## Limiti noti

- Confronto con l'anno prima sul valore **finale**, non «allo stesso giorno»: lo snapshot non ha le date dei rilievi.
- Sanità: la proposta è sulla curva del danno di 4Grapes (somma dei massimi), la pagina mostra 100 − media dei massimi.
- Biodiversità: la proposta (≥ 5,8) mette «sotto» quasi tutti i vigneti con i dati di oggi (valore 0): da decidere.

## Prossimi giri

Fascia dell'obiettivo sui quadranti e colonna «obiettivo» nelle tabelle del report Indice Bigot e del fine stagione; storico delle
versioni nella pagina; classi a scelta per vigoria/morfologia/stress idrico.

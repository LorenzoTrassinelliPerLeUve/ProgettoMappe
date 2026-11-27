# Configurazione del bilancio idrico intorno al vigneto (task #405, prototipo)

Pagina nuova `/bilancio-idrico/configurazione-nuova?azienda=` (la vecchia `/bilancio-idrico/configurazione` resta finché il
proprietario non sceglie). Il nuovo flusso, in 8 righe:

1. In cima **«Configurazione completa: N vigneti su M»** con la barra e **«Cosa ti manca per avere i consigli»** in parole semplici, ognuna con «Fallo ora».
2. **Una tabella «I vigneti dell'azienda»**, una riga per vigneto: Stile del vino · Suolo · Radici · Irrigato · Stato (✓ pronto / manca …); celle mancanti in giallo.
3. Si sceglie **nella cella** (tendine) e si salva subito: «Salvato ✓» sulla riga. Righe spuntate → **«Riempi giù da …»** o **«Copia da un vigneto»**.
4. **Suolo senza analisi**: «Descrivi il suolo» → tessitura (le 12 classi di 4Grapes), fin dove arrivano le radici, pietre; la riserva d'acqua (mm) la calcola il programma.
5. **«L'azienda irriga?»**: no → niente altro. Sì → 3 passi: quali vigneti si irrigano insieme (nome proposto dalla vigna), com'è l'impianto (mm all'ora calcolati, con il controllo «normale / troppo / poco»), aprite più gruppi insieme (turno, facoltativo).
6. A schermo «gruppo irriguo» e «turno», mai settore, quota o shift. Un vigneto sta in un gruppo: si cambia anche dalla colonna «Irrigato».
7. **«Ho irrigato»**: dalla pagina del bilancio (pulsante su ogni gruppo) si arriva al modulo con ore/mm del consiglio già scritti; sotto, il diario delle irrigazioni con «Cambia» e «Togli».
8. **Per esperti** (parametri del modello, correzioni fini del suolo) solo per gli interni Perleuve, in fondo; sul telefono la tabella diventa una scheda per vigneto.

Foto: `prima/` (pagina di prima: Satta, Rainoldi, Mirafiore) e `dopo/` (stesse aziende più suolo guidato, riempi giù, percorso
dell'irrigazione, impianto di Satta, «Ho irrigato», pagina del bilancio). Rifarle: `giro.sh dopo dopo` (Web in Development con
`BilancioIdrico:AutoreDiProva`, solo per vedere i comandi accesi). Rainoldi e Mirafiore non hanno gruppi irrigui: sono l'esempio
di azienda senza irrigazione. Logica in `Web/BilancioIdrico/ConfigurazioneVigneti.cs`, test `ConfigurazioneNuovaTests`.

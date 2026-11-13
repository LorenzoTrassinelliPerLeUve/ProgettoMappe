# PDF dei report (LibreOffice)

Il PDF del report fine stagione è il suo PowerPoint convertito da LibreOffice
(`ConvertitorePdf`). Non si converte mai dentro la richiesta: coda da uno, la
pagina «PDF in preparazione» si ricarica ogni 30 secondi, il PDF finito resta in
cache. Se una conversione fallisce, per 10 minuti la stessa richiesta risponde
503 («scarica il PowerPoint») invece di ritentare.

## A pezzi (task #204)

Il PowerPoint intero di Mirafiore (97 slide) porta LibreOffice oltre 2 GB e non si
converte su questa macchina (3,8 GB in tutto). Per questo il file si divide in
blocchi di `SlidePerPezzo` slide (copie del file con le altre slide tolte
dall'elenco della presentazione), LibreOffice ne converte uno alla volta con un
profilo temporaneo comune e `pdfunite` (pacchetto `poppler-utils`) li unisce.
Misure del boss su Mirafiore, blocchi da 30: 2 s, 5 s, 14 s, memoria bassa. Con il codice
(blocchi da 25, 13/11): IT 185 s, 183 pagine, 3,6 MB; ENG 185 s, 191 pagine, 3,8 MB; LibreOffice al
massimo ~970 MB.

## Impostazioni (sezione `Report` di appsettings.json del Web)

| Chiave | Valore consigliato | Cosa fa |
|---|---|---|
| `Soffice` | `soffice` | Riga di comando di LibreOffice. Vuota = PDF spento (503). |
| `Pdfunite` | `pdfunite` | Riga di comando che unisce i PDF dei pezzi. |
| `SlidePerPezzo` | `25` | Slide per blocco. Più piccolo = meno memoria, un po' più lento. |
| `CartellaPdf` | `/var/lib/mappe/dati/pdf` | Cache dei PDF finiti (PDF più vecchi di 3 giorni cancellati). Se non è scrivibile (anteprima, utente `agente`) si usa la cartella temporanea, con un Warning. |

Il tempo massimo è 10 minuti per tutto il PDF (tutti i pezzi): oltre, LibreOffice
viene fermato e la pagina dice di scaricare il PowerPoint.

**Anteprima e produzione: `soffice`.** Con i pezzi basta il tempo massimo: ogni
blocco resta lontano dal gigabyte. Lo stesso appsettings.json vale per tutte e due.

Limite di memoria, solo se servisse ancora: `Soffice` diventa
`systemd-run --user --scope -q -p MemoryMax=1500M soffice`. Funziona solo se
l'utente che fa girare il Web ha il suo gestore dei servizi acceso (linger):
`agente` (anteprima) sì; per `mappe` (produzione) va controllato prima, altrimenti
`systemd-run --user` fallisce e il PDF non si fa mai.

LibreOffice ha bisogno di `/tmp` scrivibile (ci crea il suo canale): nel recinto
dell'agente automatico non c'è, quindi lì le prove vere del PDF non si possono fare
(test `Pdf_UnaPaginaPerSlide` con `MAPPE_PROVA_PDF=1`).

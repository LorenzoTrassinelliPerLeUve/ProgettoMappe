# Notifiche degli avvisi nuovi (task #147)

Ogni mattina alle 7:00 `avvisi_notifica.py` legge il file degli avvisi che l'Api di produzione
riscrive a ogni calcolo (`Avvisi:File` = `/srv/mappe-avvisi/avvisi.json`, ogni ora), lo
confronta con la foto del giorno prima e, se ci sono avvisi **nuovi**, manda:

- una notifica nell'app delle task (`~/progetti/task/notifiche.py`, `manda()`, etichetta
  `mappe-avvisi`): «Mappe: 3 avvisi nuovi in 2 aziende», testo «Collio (2), Brda (1)», il tocco
  apre `/avvisi` (o `/avvisi?azienda=ID` se l'azienda è una sola);
- un messaggio al bot Telegram del proprietario (token `~/segreti/telegram.txt`, chat
  `~/progetti/telegram/utente.txt`, come `strumenti/controllo.py`), una riga per azienda con i
  conteggi per regola e il link `/avvisi?azienda=ID` (pagina con solo quell'azienda, già aperta).

Regole:

- **Nuovo** = la `chiave` dell'avviso (regola + vigneto, stazione o azienda) non c'era il giorno prima.
- **Un messaggio al giorno** al massimo: un secondo giro nello stesso giorno non manda niente, e
  la foto resta quella dell'invio (chi compare dopo è «nuovo» il giorno dopo).
- **Fuori stagione** niente: gli avvisi delle regole con `ferma` non contano (l'Api già non li produce).
- Regola **non disponibile** (4Grapes non ha risposto): i suoi avvisi non contano e la foto tiene
  quelli di prima, così il giorno dopo non sembrano tutti nuovi.
- **Primo giro**: salva la foto e non manda niente.
- **File vecchio** (il campo `giorno` non è oggi, ora italiana): non manda niente, foto invariata.
- Se un canale non funziona si prova l'altro; l'uscita è 1 (il servizio risulta fallito).

Foto: `~/progetti/mappe/avvisi-notifica-stato.json` (`giorno`, `base` = chiavi del giorno prima,
`chiavi` = quelle di oggi, `inviato`).

## Prova

    python3 avvisi_notifica.py --prova                       # stampa il messaggio, non manda, non salva
    python3 avvisi_notifica.py --file altro.json --stato /tmp/s.json --prova
    python3 -m unittest discover -s tests                    # test senza rete

## Installazione (servizio dell'utente, fuori dal rilascio)

Gira dalla copia `~/progetti/mappe/codice` con il Python dell'app delle task:

    cp systemd/mappe-avvisi-notifica.* ~/.config/systemd/user/
    systemctl --user daemon-reload
    systemctl --user enable --now mappe-avvisi-notifica.timer
    systemctl --user start mappe-avvisi-notifica.service      # primo giro: salva la foto

#!/usr/bin/env python3
"""Avvisi nuovi della mappa → una notifica nell'app delle task e un messaggio Telegram (task #147).

Ogni mattina (timer dell'utente alle 7:00) legge il file degli avvisi che scrive l'Api
(`Avvisi:File`, /srv/mappe-avvisi/avvisi.json), lo confronta con quello del giorno prima e,
se ci sono avvisi NUOVI, manda UN messaggio solo, raggruppato per azienda, con il link alla
pagina /avvisi filtrata sull'azienda.

- «Nuovo» = la sua `chiave` (regola + vigneto o stazione) non c'era nella foto del giorno prima.
- Al massimo un messaggio al giorno, anche se il programma gira più volte.
- Regole ferme per la stagione: i loro avvisi non contano (l'Api già non ne produce).
- Regola «non disponibile» (4Grapes non ha risposto): i suoi avvisi non contano e si tiene la
  foto del giorno prima, così domani non sembrano tutti nuovi.
- Il primo giro (nessuna foto) salva la foto e non manda niente.
- File vecchio (giorno diverso da oggi): non manda niente e non tocca la foto.

    avvisi_notifica.py [--file F] [--stato S] [--prova]

`--prova` stampa il messaggio senza mandarlo e senza salvare la foto. Gira con il Python
dell'app delle task (~/progetti/task/.venv), che ha le librerie di `notifiche.py`. Il token
del bot si legge da ~/segreti/telegram.txt solo al momento dell'invio.
"""

import argparse
import json
import os
import sys
import urllib.parse
import urllib.request
from datetime import datetime
from pathlib import Path
from zoneinfo import ZoneInfo

FILE = Path("/srv/mappe-avvisi/avvisi.json")
STATO = Path.home() / "progetti/mappe/avvisi-notifica-stato.json"
INDIRIZZO = "https://mappe.37-27-212-47.sslip.io/"   # se i link del file sono relativi
CARTELLA_TASK = Path.home() / "progetti/task"
AZIENDE_MAX = 15        # aziende elencate nel messaggio Telegram, poi «e altre N»
ETICHETTA = "mappe-avvisi"


def oggi():
    return datetime.now(ZoneInfo("Europe/Rome")).date().isoformat()


def da_contare(dati):
    """Gli avvisi che contano (regole in stagione e disponibili) e le regole da tenere com'erano."""
    regole = {r["id"]: r for r in dati.get("regole") or []}
    sospese = {i for i, r in regole.items() if r.get("ferma") is None and not r.get("disponibile", True)}
    ferme = {i for i, r in regole.items() if r.get("ferma") is not None}
    avvisi = [a for a in dati.get("avvisi") or [] if a.get("regola") not in ferme | sospese]
    return avvisi, sospese


def confronta(dati, stato, giorno):
    """(avvisi nuovi, stato nuovo). Lo stato è la foto: le chiavi di oggi e quelle del giorno prima."""
    avvisi, sospese = da_contare(dati)
    chiavi = {a["chiave"] for a in avvisi}
    if stato is None:
        return [], {"giorno": giorno, "base": None, "chiavi": sorted(chiavi), "inviato": None}
    if stato.get("giorno") == giorno and stato.get("inviato") == giorno:
        # Già mandato oggi: la foto resta quella dell'invio, così chi è comparso dopo è nuovo domani.
        return [], dict(stato)
    base = stato.get("base") if stato.get("giorno") == giorno else stato.get("chiavi")
    precedenti = set(stato.get("chiavi") or [])
    # Regola non disponibile oggi: si tengono le sue chiavi di prima («regola:oggetto»).
    chiavi |= {c for c in precedenti if c.split(":", 1)[0] in sospese}
    nuovi = [] if base is None else [a for a in avvisi if a["chiave"] not in set(base)]
    return nuovi, {"giorno": giorno, "base": base, "chiavi": sorted(chiavi), "inviato": None}


def indirizzo_base(dati, indirizzo):
    """L'indirizzo della mappa, dai link assoluti del file; se sono relativi, quello predefinito."""
    for azienda in dati.get("aziende") or []:
        link = azienda.get("link") or ""
        fine = f"mappa/{azienda.get('id')}"
        if link.startswith("http") and link.endswith(fine):
            return link[: -len(fine)]
    return indirizzo.rstrip("/") + "/"


def per_azienda(nuovi, dati, base):
    """[(nome, link avvisi filtrati, [(breve regola, quanti)])], prima le aziende con più avvisi nuovi."""
    brevi = {r["id"]: r.get("breve") or r.get("nome") or r["id"] for r in dati.get("regole") or []}
    link = {a["id"]: a.get("linkAvvisi") for a in dati.get("aziende") or []}
    gruppi = {}
    for avviso in nuovi:
        gruppi.setdefault(avviso["aziendaId"], []).append(avviso)
    risultato = []
    for azienda, avvisi in gruppi.items():
        conteggi = {}
        for a in avvisi:
            breve = brevi.get(a["regola"], a["regola"])
            conteggi[breve] = conteggi.get(breve, 0) + 1
        indirizzo = link.get(azienda) or f"{base}avvisi?azienda={azienda}"
        if not indirizzo.startswith("http"):
            indirizzo = base + indirizzo
        risultato.append((avvisi[0].get("azienda") or f"Azienda {azienda}", indirizzo, list(conteggi.items()), len(avvisi)))
    risultato.sort(key=lambda g: (-g[3], g[0].lower()))
    return [(nome, indirizzo, conteggi) for nome, indirizzo, conteggi, _ in risultato]


def messaggio(nuovi, dati, base):
    """(titolo, testo Telegram, testo breve per la notifica, link della notifica)."""
    gruppi = per_azienda(nuovi, dati, base)
    n, m = len(nuovi), len(gruppi)
    titolo = f"Mappe: {n} {'avviso nuovo' if n == 1 else 'avvisi nuovi'}" + (f" in {m} aziende" if m > 1 else f" ({gruppi[0][0]})")
    righe = [titolo, ""]
    for nome, indirizzo, conteggi in gruppi[:AZIENDE_MAX]:
        righe.append(f"• {nome}: " + ", ".join(f"{breve} {quanti}" for breve, quanti in conteggi))
        righe.append(f"  {indirizzo}")
    if m > AZIENDE_MAX:
        righe.append(f"…e altre {m - AZIENDE_MAX} aziende")
    righe += ["", f"Tutti gli avvisi: {base}avvisi"]
    breve = ", ".join(f"{nome} ({sum(q for _, q in conteggi)})" for nome, _, conteggi in gruppi)
    link = gruppi[0][1] if m == 1 else f"{base}avvisi"
    return titolo, "\n".join(righe), breve, link


def telegram(testo):
    token = (Path.home() / "segreti/telegram.txt").read_text().strip()
    chat = (Path.home() / "progetti/telegram/utente.txt").read_text().strip()
    dati = urllib.parse.urlencode({"chat_id": chat, "text": testo, "disable_web_page_preview": "true"}).encode()
    urllib.request.urlopen(f"https://api.telegram.org/bot{token}/sendMessage", data=dati, timeout=30)


def notifica(titolo, testo, link):
    sys.path.insert(0, str(CARTELLA_TASK))
    import notifiche
    return notifiche.manda(titolo, testo, link, etichetta=ETICHETTA)


def salva(percorso, stato):
    provvisorio = percorso.with_name(percorso.name + ".nuovo")
    provvisorio.write_text(json.dumps(stato, ensure_ascii=False, indent=1))
    os.replace(provvisorio, percorso)


def main(argomenti=None, manda_notifica=notifica, manda_telegram=telegram, giorno=None):
    p = argparse.ArgumentParser(description="Notifica gli avvisi nuovi della mappa (task #147).")
    p.add_argument("--file", type=Path, default=FILE)
    p.add_argument("--stato", type=Path, default=STATO)
    p.add_argument("--indirizzo", default=INDIRIZZO, help="indirizzo della mappa se i link del file sono relativi")
    p.add_argument("--prova", action="store_true", help="stampa il messaggio, non manda niente e non salva")
    a = p.parse_args(argomenti)
    giorno = giorno or oggi()

    try:
        dati = json.loads(a.file.read_text())
    except (OSError, ValueError) as errore:
        print(f"Non riesco a leggere {a.file}: {type(errore).__name__}: {errore}", file=sys.stderr)
        return 1
    if dati.get("giorno") != giorno:
        print(f"Il file degli avvisi è del {dati.get('giorno')}, non di oggi ({giorno}): non mando niente.", file=sys.stderr)
        return 0

    stato = json.loads(a.stato.read_text()) if a.stato.exists() else None
    nuovi, stato_nuovo = confronta(dati, stato, giorno)
    if stato is None:
        print("Prima volta: salvo la foto degli avvisi di oggi, da domani notifico i nuovi.")
    elif not nuovi:
        print("Già mandato oggi." if stato_nuovo["inviato"] == giorno else "Nessun avviso nuovo.")

    esito = 0
    if nuovi:
        titolo, testo, breve, link = messaggio(nuovi, dati, indirizzo_base(dati, a.indirizzo))
        if a.prova:
            print(testo)
            print(f"\n[notifica] {titolo} — {breve} → {link}")
            return 0
        riusciti = 0
        for nome, invia in (("notifica", lambda: manda_notifica(titolo, breve, link)), ("Telegram", lambda: manda_telegram(testo))):
            try:
                invia()
                riusciti += 1
            except Exception as errore:   # un canale rotto non ferma l'altro
                print(f"{nome} non mandata: {type(errore).__name__}: {errore}", file=sys.stderr)
                esito = 1
        if riusciti:
            stato_nuovo["inviato"] = giorno
            print(f"Mandati {len(nuovi)} avvisi nuovi.")
    if not a.prova:
        salva(a.stato, stato_nuovo)
    return esito


if __name__ == "__main__":
    sys.exit(main())

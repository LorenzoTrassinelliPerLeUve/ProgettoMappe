"""Configurazione: valori predefiniti, sovrascritti da ndvi/config.json (se c'è)."""

import json
from dataclasses import dataclass, fields
from pathlib import Path


@dataclass
class Configurazione:
    # Api locale di produzione (elenco aziende e geometrie dei vigneti).
    api: str = "http://127.0.0.1:5201"
    # File con la chiave condivisa dell'Api (X-Mappe-Chiave): usata solo se il file esiste.
    chiave_api_file: str = "~/segreti/mappe-chiave-api.txt"
    # Righe CLIENT_ID=… e CLIENT_SECRET=… (letto a ogni avvio, mai copiato altrove).
    credenziali_file: str = "~/segreti/copernicus.txt"
    dati: str = "~/progetti/mappe/ndvi/dati"
    url_token: str = (
        "https://identity.dataspace.copernicus.eu/auth/realms/CDSE/protocol/openid-connect/token"
    )
    url_catalogo: str = "https://sh.dataspace.copernicus.eu/api/v1/catalog/1.0.0/search"
    url_process: str = "https://sh.dataspace.copernicus.eu/api/v1/process"
    collezione: str = "sentinel-2-l2a"
    giorni_prima_volta: int = 60
    richieste_al_secondo: float = 4
    tentativi: int = 3
    risoluzione_m: float = 10
    max_pixel: int = 2500
    margine_m: float = 30
    # Oltre questa percentuale di pixel nuvolosi sui vigneti il passaggio non è «pulito».
    nuvole_max_pct: float = 60
    # Giorni con tutte le scene del catalogo (eo:cloud_cover) oltre questa % si saltano senza
    # chiamare il Process API: il passaggio resta in SQLite come scartato dal catalogo.
    nuvole_catalogo_max_pct: float = 80
    # Vigneti a meno di questa distanza (m) stanno nello stesso riquadro. 0 = un riquadro
    # per azienda (spezzato solo oltre max_pixel).
    distanza_gruppi_m: float = 2000
    # Tetto per giro (anche lo storico, salvo --senza-tetto): raggiunto, il giro si ferma pulito
    # e riprende la volta dopo. Richieste = Catalog + Process API (il token non conta).
    pu_max_per_giro: float = 300
    richieste_max_per_giro: int = 400
    # A fine giro si cancellano i GeoTIFF dei passaggi più vecchi di N giorni (le righe in
    # SQLite restano, `passaggi.file` diventa NULL). 0 = non cancellare mai.
    conserva_raster_giorni: int = 90
    # Statistiche calcolate in memoria senza salvare il GeoTIFF (lo storico: --senza-raster).
    senza_raster: bool = False

    def percorso(self, valore: str) -> Path:
        return Path(valore).expanduser()

    @property
    def cartella_dati(self) -> Path:
        return self.percorso(self.dati)

    @property
    def database(self) -> Path:
        return self.cartella_dati / "ndvi.db"


def carica(file: Path | None) -> Configurazione:
    """Legge il file JSON (chiavi sconosciute = errore, così un refuso non passa muto)."""
    if file is None or not file.exists():
        return Configurazione()
    valori = json.loads(file.read_text(encoding="utf-8"))
    note = {f.name for f in fields(Configurazione)}
    ignote = sorted(set(valori) - note)
    if ignote:
        raise ValueError(f"{file}: chiavi sconosciute {', '.join(ignote)}")
    return Configurazione(**valori)


def leggi_credenziali(file: Path) -> tuple[str, str]:
    """CLIENT_ID e CLIENT_SECRET dal file KEY=VALORE (righe vuote e # ignorate)."""
    valori = {}
    for riga in file.read_text(encoding="utf-8").splitlines():
        riga = riga.strip()
        if not riga or riga.startswith("#") or "=" not in riga:
            continue
        chiave, valore = riga.split("=", 1)
        valori[chiave.strip()] = valore.strip()
    mancanti = [k for k in ("CLIENT_ID", "CLIENT_SECRET") if not valori.get(k)]
    if mancanti:
        raise ValueError(f"{file}: manca {', '.join(mancanti)}")
    return valori["CLIENT_ID"], valori["CLIENT_SECRET"]

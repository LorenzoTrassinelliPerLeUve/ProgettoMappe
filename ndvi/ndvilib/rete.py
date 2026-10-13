"""Richieste HTTP: Copernicus (token, limite di frequenza, tentativi) e Api locale."""

import logging
import time
from datetime import date, datetime, timedelta, timezone

import requests

log = logging.getLogger("ndvi")

# Scene Classification Layer: nodata, saturati, ombre scure, ombre di nuvole, nuvole, cirri, neve.
SCL_SCARTATI = (0, 1, 2, 3, 7, 8, 9, 10, 11)

EVALSCRIPT = """//VERSION=3
function setup() {
  return {
    input: [{ bands: ["B04", "B08", "SCL"] }],
    output: { bands: 1, sampleType: "FLOAT32" }
  };
}
const scartati = [%s];
function evaluatePixel(s) {
  if (scartati.includes(s.SCL)) return [NaN];
  const somma = s.B08 + s.B04;
  if (somma === 0) return [NaN];
  return [(s.B08 - s.B04) / somma];
}
""" % ", ".join(str(c) for c in SCL_SCARTATI)


class ErroreRete(Exception):
    """Una richiesta non è andata nemmeno dopo i tentativi: si passa oltre."""


class ErroreQuota(ErroreRete):
    """HTTP 403 «Insufficient processing units or requests available»: la quota Copernicus è
    finita. Non si ritenta e il giro si ferma (anche le altre aziende fallirebbero)."""


def _quota_esaurita(risposta):
    testo = (risposta.text or "").lower()
    return risposta.status_code == 403 and ("insufficient" in testo or "quota" in testo)


class Limitatore:
    """Al massimo N richieste al secondo (intervallo minimo fra due richieste)."""

    def __init__(self, al_secondo, orologio=time.monotonic, dormi=time.sleep):
        self.intervallo = 1.0 / al_secondo
        self.orologio = orologio
        self.dormi = dormi
        self.ultima = None

    def aspetta(self):
        adesso = self.orologio()
        if self.ultima is not None:
            mancano = self.ultima + self.intervallo - adesso
            if mancano > 0:
                self.dormi(mancano)
                adesso += mancano
        self.ultima = adesso


def _ritentabile(risposta):
    return risposta.status_code == 429 or risposta.status_code >= 500


def richiesta(sessione, metodo, url, tentativi, dormi=time.sleep, prima=None, **kwargs):
    """Una richiesta con `tentativi` ripetizioni su errori di rete, 429 e 5xx.

    `prima` è chiamata prima di ogni invio (limite di frequenza, intestazioni aggiornate):
    restituisce le intestazioni da usare. Altri 4xx: ErroreRete subito (ripetere non serve).
    """
    ultimo_errore = None
    for prova in range(tentativi + 1):
        if prova:
            dormi(2 ** prova)
        intestazioni = prima() if prima else kwargs.get("headers")
        if intestazioni is not None:
            kwargs["headers"] = intestazioni
        try:
            risposta = sessione.request(metodo, url, **kwargs)
        except (requests.ConnectionError, requests.Timeout) as e:
            ultimo_errore = f"{type(e).__name__}: {e}"
            log.warning("%s %s: %s (tentativo %d)", metodo, url, ultimo_errore, prova + 1)
            continue
        if _ritentabile(risposta):
            ultimo_errore = f"HTTP {risposta.status_code}"
            log.warning("%s %s: %s (tentativo %d)", metodo, url, ultimo_errore, prova + 1)
            continue
        if _quota_esaurita(risposta):
            raise ErroreQuota(f"{metodo} {url}: HTTP 403 {risposta.text[:300]}")
        if risposta.status_code >= 400:
            raise ErroreRete(f"{metodo} {url}: HTTP {risposta.status_code} {risposta.text[:300]}")
        return risposta
    raise ErroreRete(f"{metodo} {url}: {ultimo_errore} dopo {tentativi + 1} prove")


class Copernicus:
    """Token OAuth (client_credentials), Catalog API e Process API di Copernicus Data Space."""

    def __init__(self, conf, credenziali, sessione=None, orologio=time.time, dormi=time.sleep,
                 limitatore=None):
        self.conf = conf
        self.client_id, self.client_secret = credenziali
        self.sessione = sessione or requests.Session()
        self.orologio = orologio
        self.dormi = dormi
        self.limitatore = limitatore or Limitatore(conf.richieste_al_secondo)
        self.token = None
        self.scadenza = 0.0
        self.richieste = 0
        # Richieste a Catalog e Process API (tentativi compresi): contano nella quota, il token no.
        self.richieste_quota = 0

    def _invia(self, metodo, url, autenticata=True, **kwargs):
        def prima():
            intestazioni = dict(kwargs.get("headers") or {})
            if autenticata:
                intestazioni["Authorization"] = f"Bearer {self._token_valido()}"
                self.richieste_quota += 1
            self.limitatore.aspetta()
            self.richieste += 1
            return intestazioni

        return richiesta(self.sessione, metodo, url, self.conf.tentativi, self.dormi, prima,
                         **kwargs)

    def _token_valido(self):
        # Rinnovato un minuto prima della scadenza, così non scade a metà di un passaggio.
        if self.token is None or self.orologio() >= self.scadenza - 60:
            risposta = self._invia("POST", self.conf.url_token, autenticata=False, timeout=60,
                                   data={"grant_type": "client_credentials",
                                         "client_id": self.client_id,
                                         "client_secret": self.client_secret})
            corpo = risposta.json()
            self.token = corpo["access_token"]
            self.scadenza = self.orologio() + float(corpo.get("expires_in", 300))
            log.debug("token rinnovato, scade fra %s s", corpo.get("expires_in"))
        return self.token

    def date_passaggi(self, bbox, dal: date, al: date) -> dict[str, float | None]:
        """Giorni (AAAA-MM-GG, crescenti) con almeno una scena sul bbox fra `dal` e `al` compresi,
        con la copertura nuvolosa (eo:cloud_cover) della scena meno nuvolosa del giorno: è quella
        che il Process API mette davanti (leastCC). None se il catalogo non la dà."""
        corpo = {
            "bbox": list(bbox),
            "datetime": f"{dal.isoformat()}T00:00:00Z/{al.isoformat()}T23:59:59Z",
            "collections": [self.conf.collezione],
            "limit": 100,
            "fields": {"include": ["properties.datetime", "properties.eo:cloud_cover"],
                       "exclude": ["assets", "links"]},
        }
        giorni = {}
        while True:
            risposta = self._invia("POST", self.conf.url_catalogo, json=corpo, timeout=120).json()
            for scena in risposta.get("features", []):
                proprieta = scena.get("properties", {})
                istante = proprieta.get("datetime")
                if not istante:
                    continue
                nuvole = proprieta.get("eo:cloud_cover")
                giorno = istante[:10]
                if giorno not in giorni:
                    giorni[giorno] = nuvole
                elif nuvole is None or giorni[giorno] is None:
                    giorni[giorno] = None  # una scena senza copertura: il giorno non si salta
                else:
                    giorni[giorno] = min(giorni[giorno], nuvole)
            prossima = (risposta.get("context") or {}).get("next")
            if prossima is None:
                return dict(sorted(giorni.items()))
            corpo["next"] = prossima

    def geotiff_ndvi(self, bbox, giorno: str, larghezza: int, altezza: int) -> bytes:
        """GeoTIFF float32 (WGS84) con l'NDVI del giorno sul bbox; NaN dove SCL è scartato."""
        corpo = {
            "input": {
                "bounds": {"bbox": list(bbox),
                           "properties": {"crs": "http://www.opengis.net/def/crs/OGC/1.3/CRS84"}},
                "data": [{
                    "type": self.conf.collezione,
                    "dataFilter": {
                        "timeRange": {"from": f"{giorno}T00:00:00Z", "to": f"{giorno}T23:59:59Z"},
                        "mosaickingOrder": "leastCC",
                    },
                }],
            },
            "output": {
                "width": larghezza,
                "height": altezza,
                "responses": [{"identifier": "default", "format": {"type": "image/tiff"}}],
            },
            "evalscript": EVALSCRIPT,
        }
        return self._invia("POST", self.conf.url_process, json=corpo, timeout=300,
                           headers={"Accept": "image/tiff"}).content


class ApiMappe:
    """Api locale dell'app: elenco aziende e geometrie dei vigneti (export GeoJSON)."""

    def __init__(self, conf, sessione=None, dormi=time.sleep):
        self.conf = conf
        self.sessione = sessione or requests.Session()
        self.dormi = dormi
        self.intestazioni = {}
        file_chiave = conf.percorso(conf.chiave_api_file)
        if file_chiave.is_file():
            self.intestazioni["X-Mappe-Chiave"] = file_chiave.read_text(encoding="utf-8").strip()

    def _get(self, percorso, **parametri):
        return richiesta(self.sessione, "GET", self.conf.api.rstrip("/") + percorso,
                         self.conf.tentativi, self.dormi, headers=dict(self.intestazioni),
                         params=parametri or None, timeout=180)

    def aziende(self) -> list[dict]:
        return self._get("/api/aziende").json()

    def vigneti_geojson(self, id_azienda: int) -> dict:
        return self._get(f"/api/aziende/{id_azienda}/vigneti/esporta", formato="geojson").json()


def oggi_utc() -> date:
    return datetime.now(timezone.utc).date()


def giorno_dopo(giorno: str) -> date:
    return date.fromisoformat(giorno) + timedelta(days=1)

#!/usr/bin/env python3
"""Verità di Qlik del REPORT SETTIMANALE - LAORE (task #216, tappa 2; adattato da leggi_verita_rs.py). Solo letture: le selezioni vivono solo nella sessione
dell'API e non si salvano. Per ogni azienda e annata: ClearAll, seleziona RagioneSociale e AnnataAgraria.MeteoGiornaliero
(come fanno i colleghi nel foglio «0 - FILITRI» prima di «CREA REPORT»), poi legge il layout di ogni oggetto delle slide
del report (ordine dell'automazione PPT_REPORT_FINE_STAGIONE): KPI, contachilometri, testi con misure → i valori delle
misure; grafici e tabelle → tutte le righe (dimensioni + misure). Chiavi: «Slide N. titolo foglio» → «tipo id titolo».
  python3 leggi_verita_rfs.py            (scrive verita-qlik.json)
"""
import json
import sys
from datetime import datetime
from pathlib import Path

sys.path.insert(0, str(Path.home() / "progetti" / "qlik"))
sys.argv = [sys.argv[0]]
import qlik  # noqa: E402
sys.path.insert(0, str(Path(__file__).resolve().parent))
import genera_struttura as g  # noqa: E402

APP = "29279fdf-b791-4908-bf7a-bd75b2356fd4"   # REPORT SETTIMANALE - LAORE
AZIENDE = ["Podere Guardia Grande", "JERZU_LO_OR_04_PASSA", "MONASTIR_CO_EN_01_MZ"]   # LAORE: per nome (più rilievi 2025-2026)
ANNATE = [46, 45, 30]   # settimane richieste + la 30 (ultima con rilievi 2026: dati fino al 31/07)
C = Path(__file__).resolve().parent
SALTA = {"filterpane", "listbox", "action-button", "sheet"}


def cella(c):
    n = c.get("qNum")
    num = None if n in (None, "NaN") or c.get("qIsNull") else round(float(n), 6)
    return {"testo": c.get("qText", ""), "num": num}


def valori(chiama, doc, qid):
    h = chiama("GetObject", doc, {"qId": qid})["qReturn"]["qHandle"]
    lay = chiama("GetLayout", h, {})["qLayout"]
    hc = lay.get("qHyperCube")
    out = {"tipo": lay.get("visualization") or lay["qInfo"]["qType"], "titolo": g.pulisci(g.testo(lay.get("title") or ""))}
    if not hc:
        return out
    teste = [d.get("qFallbackTitle") for d in hc.get("qDimensionInfo", [])] + [m.get("qFallbackTitle") for m in hc.get("qMeasureInfo", [])]
    out["colonne"] = teste
    if hc.get("qMode") == "P":                      # pivot: si leggono le righe «piatte» del cubo
        out["nota"] = "tabella pivot: valori dalla pagina pivot"
        pag = chiama("GetHyperCubePivotData", h, {"qPath": "/qHyperCubeDef", "qPages": [{"qTop": 0, "qLeft": 0, "qWidth": 50, "qHeight": 200}]})
        out["pivot"] = [[cella(c) for c in r] for r in pag["qDataPages"][0]["qData"]]
        return out
    ncol, nrig = hc["qSize"]["qcx"], hc["qSize"]["qcy"]
    if ncol == 0:
        return out
    righe, passo = [], max(1, 9000 // ncol)
    for top in range(0, min(nrig, 5000), passo):
        pag = chiama("GetHyperCubeData", h, {"qPath": "/qHyperCubeDef", "qPages": [{"qTop": top, "qLeft": 0, "qWidth": ncol, "qHeight": min(passo, nrig - top)}]})
        righe += [[cella(c) for c in r] for r in pag["qDataPages"][0]["qMatrix"]]
    if not hc.get("qDimensionInfo") and len(righe) == 1:   # KPI, contachilometri, testi: una riga di misure
        out["misure"] = dict(zip(teste, righe[0]))
    else:
        out["righe"] = righe
        out["righe_totali"] = nrig
    return out


def seleziona(chiama, doc, campo, testo=None, numero=None):
    h = chiama("GetField", doc, {"qFieldName": campo})["qReturn"]["qHandle"]
    v = {"qText": testo} if numero is None else {"qText": str(numero), "qIsNumeric": True, "qNumber": numero}
    return chiama("SelectValues", h, {"qFieldValues": [v], "qToggleMode": False})["qReturn"]


def leggi(chiama, doc):
    _, ordine = g.ordine_slide("automazione-PPT_REPORT_SETTIAMANALE.json", g.IT_FOGLI)
    ev = lambda e: chiama("Evaluate", doc, {"qExpression": e})["qReturn"]
    out = {"letto_il": datetime.now().isoformat(timespec="seconds"), "app": "REPORT SETTIMANALE - LAORE", "app_id": APP,
           "selezioni": "RagioneSociale + Week (come nel foglio «Filtri»)", "aziende": {}}
    precedente = C / "verita-qlik.json"
    fatti = json.loads(precedente.read_text())["aziende"] if precedente.exists() else {}
    for e in AZIENDE:
        chiama("ClearAll", doc, {"qLockedAlso": False})
        if fatti.get(str(e), {}).get("annate", {}).keys() >= {str(a) for a in ANNATE}:   # già letta in un giro precedente
            out["aziende"][str(e)] = fatti[str(e)]
            continue
        nome = e
        out["aziende"][str(e)] = {"nome": nome, "annate": {}}
        for a in ANNATE:
            chiama("ClearAll", doc, {"qLockedAlso": False})
            ok = seleziona(chiama, doc, "RagioneSociale", testo=nome) and seleziona(chiama, doc, "Week", numero=a)
            risolti = {k: ev(f"Max({k})") for k in ("Anno", "Week", "Week_germoglio", "Week_stress", "Anno_BBCH")}
            slide = {}
            for n, sid in enumerate(ordine, 1):
                f = g.IT_FOGLI[sid]
                chiave_foglio = f"Slide {n}. {g.testo(f['qProperty']['qMetaDef'].get('title'))}"
                oggetti = {}
                for nodo in f.get("qChildren", []):
                    for x in g.nodi(nodo):
                        p = x["qProperty"]
                        tipo = p.get("visualization") or p["qInfo"]["qType"]
                        if tipo in SALTA or not (p.get("qHyperCubeDef") or {}).get("qMeasures"):
                            continue
                        try:
                            v = valori(chiama, doc, p["qInfo"]["qId"])
                        except RuntimeError as err:
                            v = {"errore": str(err)[:200]}
                        oggetti[f"{tipo} {p['qInfo']['qId']} {g.pulisci(g.testo(p.get('title')))[:60]}".strip()] = v
                if oggetti:
                    slide[chiave_foglio] = oggetti
            out["aziende"][str(e)]["annate"][str(a)] = {"selezione_riuscita": ok, "anni_risolti": risolti, "slide": slide}
            (C / "verita-qlik.json").write_text(json.dumps(out, ensure_ascii=False))   # salva a ogni passo
            print(f"{nome} {a}: selezione {'ok' if ok else 'NON riuscita'}, anni {risolti}, {sum(len(x) for x in slide.values())} oggetti", flush=True)
    chiama("ClearAll", doc, {"qLockedAlso": False})
    return out


if __name__ == "__main__":
    dati = qlik.con_dati(APP, leggi, tentativi=40)
    (C / "verita-qlik.json").write_text(json.dumps(dati, ensure_ascii=False))
    print("verita-qlik.json:", len((C / "verita-qlik.json").read_text()) // 1024, "KB")

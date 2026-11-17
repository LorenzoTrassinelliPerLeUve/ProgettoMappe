"""Riassunto leggibile di qlik-struttura.json: fogli in ordine, oggetti, dimensioni e misure.

Uso: python3 riassunto_struttura.py qlik-struttura.json > struttura.txt
"""
import json
import sys

d = json.load(open(sys.argv[1]))


def dim(o):
    return [(x.get('etichetta') or '') + '|' + ','.join(x.get('campi', [])) for x in o.get('dimensioni', [])]


def mis(o):
    return [(x.get('etichetta') or '') + '|' + (x.get('formula') or x.get('espressione') or json.dumps(x, ensure_ascii=False))
            for x in o.get('misure', [])]


def stampa(o, ind):
    altro = {k: v for k, v in o.items() if k not in ('id', 'tipo', 'titolo', 'dimensioni', 'misure', 'figli') and v}
    print(' ' * ind + f"[{o['tipo']}] {o.get('titolo', '')!r} D={dim(o)} M={mis(o)} {json.dumps(altro, ensure_ascii=False) if altro else ''}")
    for f in o.get('figli', []):
        stampa(f, ind + 2)


print({k: (len(v) if isinstance(v, (list, dict)) else v) for k, v in d.items()})
for f in sorted(d['fogli'], key=lambda f: f.get('rank', 0)):
    print(f"\n== {f.get('rank')} {f['titolo']} ({len(f['oggetti'])} oggetti)")
    for o in f['oggetti']:
        stampa(o, 2)
for k, v in d.items():
    if k != 'fogli' and isinstance(v, (list, dict)):
        print('\n##', k)
        print(json.dumps(v, ensure_ascii=False, indent=1))

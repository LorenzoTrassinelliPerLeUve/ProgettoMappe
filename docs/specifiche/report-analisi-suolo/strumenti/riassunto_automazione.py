"""Blocchi dell'automazione nell'ordine di esecuzione (catena childId), con gli input significativi.

Uso: python3 riassunto_automazione.py qlik-automazione.json
"""
import json
import sys

d = json.load(open(sys.argv[1]))
blocchi = {b['id']: b for b in d['workspace']['blocks']}
figli = {b.get('childId') for b in blocchi.values()} | {b.get('loopBlockId') for b in blocchi.values()}
for b in blocchi.values():
    for i in b.get('inputs', []):
        pass
radici = [b for b in blocchi.values() if b['type'] == 'StartBlock']


def ingressi(b):
    out = []
    for i in b.get('inputs', []):
        v = i.get('displayValue') or i.get('value')
        if isinstance(v, (list, dict)):
            v = json.dumps(v, ensure_ascii=False)[:300]
        if v not in (None, '', [], {}) and i['id'] not in ('report_id',):
            out.append(f"{i['id'][:14]}={v}")
    return out


visti = set()


def segui(bid, ind):
    while bid and bid not in visti:
        visti.add(bid)
        b = blocchi[bid]
        print(' ' * ind + f"{b['type']} «{b.get('displayName')}» " + ' ; '.join(ingressi(b)))
        if b.get('loopBlockId'):
            print(' ' * ind + '  [ciclo]')
            segui(b['loopBlockId'], ind + 4)
        bid = b.get('childId')


for r in radici:
    segui(r['id'], 0)
if len(sys.argv) > 2:
    # fogli aggiunti al report → titolo del foglio in qlik-struttura.json
    titoli = {f['id']: f['titolo'] for f in json.load(open(sys.argv[2]))['fogli']}
    print('\nFogli del PowerPoint (ordine dell\'automazione):')
    bid, n = radici[0]['id'], 0
    ordine = []
    visti2 = set()
    while bid and bid not in visti2:
        visti2.add(bid)
        b = blocchi[bid]
        if b['type'] == 'QlikReportingAddSheetToReportBlock':
            s = next(i for i in b['inputs'] if i['id'] == 'sheet')
            n += 1
            ordine.append(s['value'])
            print(f"{n:2} {s['value']:38} {s.get('displayValue')!r:40} -> {titoli.get(s['value'], '???')!r}")
        bid = b.get('childId')
    print('fogli dell\'app non nel PowerPoint:', [t for k, t in titoli.items() if k not in ordine])
rimasti = [b for k, b in blocchi.items() if k not in visti]
print('\nnon raggiunti:', [(b['type'], b.get('displayName')) for b in rimasti])

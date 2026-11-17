"""Proprietà dei grafici e dei KPI dei fogli estratti (qlik-qvf/fogli/*.json): orientamento, ordinamento, colori,
linee di riferimento, etichette, formato dei numeri, tipo di serie nei combo, testi degli text-image.

Uso: python3 grafici_qvf.py qlik-qvf/fogli > qlik-qvf/grafici.md
"""
import json
import os
import sys

cartella = sys.argv[1]


def fmt(nf):
    if not nf:
        return ''
    return f"{nf.get('qType', '')}:{nf.get('qFmt', '')}" if nf.get('qType') not in (None, 'U') else ''


def oggetto(c):
    p = c['qProperty']
    v = p.get('visualization')
    righe = []
    hc = p.get('qHyperCubeDef', {})
    if v in ('barchart', 'combochart', 'kpi', 'table'):
        dims = [(d.get('qDef', {}).get('qFieldDefs') or [''])[0] + (
            f" (ordina {d['qDef'].get('qSortCriterias', [{}])[0]})" if v != 'kpi' else '') for d in hc.get('qDimensions', [])]
        mis = []
        for m in hc.get('qMeasures', []):
            d = m.get('qDef', {})
            serie = m.get('series', {})
            mis.append(f"{d.get('qLabel', '')} = {d.get('qDef', '')} [{fmt(d.get('qNumFormat'))}]"
                       + (f" serie={serie.get('type')}" if serie else '')
                       + (f" colore={json.dumps(d.get('coloring', {}).get('baseColor'))}" if d.get('coloring', {}).get('baseColor') else ''))
        righe.append(f"- **{v}** dims={dims}")
        for m in mis:
            righe.append(f"  - misura: {m}")
        extra = {k: p.get(k) for k in ('orientation', 'barGrouping', 'dataPoint', 'refLine', 'color', 'sorting', 'scrollStartPos',
                                         'dimensionAxis', 'measureAxis', 'legend', 'gridlines')
                 if p.get(k) not in (None, {}, [])}
        if hc.get('qInterColumnSortOrder') is not None:
            extra['qInterColumnSortOrder'] = hc['qInterColumnSortOrder']
        if 'refLine' in extra:
            extra['refLine'] = [{k: r.get(k) for k in ('label', 'refLineExpr', 'paletteColor', 'style')}
                                for r in extra['refLine'].get('refLines', [])]
        if 'color' in extra:
            col = extra['color']
            extra['color'] = {k: col.get(k) for k in ('auto', 'mode', 'singleColor', 'paletteColor', 'byDimDef', 'byMeasureDef', 'useBaseColors') if k in col}
        if v == 'kpi':
            extra = {'showTitles': p.get('showTitles'), 'textAlign': p.get('textAlign'), 'fontSize': p.get('fontSize')}
        righe.append('  - ' + json.dumps(extra, ensure_ascii=False)[:1500])
    elif v == 'text-image':
        md = p.get('markdown', '')
        righe.append(f"- **text-image** {md[:200]!r}")
    for f in c.get('qChildren', []):
        righe += oggetto(f)
    return righe


if '--compatto' in sys.argv:
    # una riga per grafico: foglio | tipo | dimensioni | raggruppamento | linee di riferimento | colore | asse
    def grafici(c):
        p = c['qProperty']
        if p.get('visualization') in ('barchart', 'combochart'):
            dims = [(d['qDef'].get('qFieldDefs') or [''])[0] for d in p['qHyperCubeDef'].get('qDimensions', [])]
            ref = [r.get('refLineExpr', {}).get('value') for r in p.get('refLine', {}).get('refLines', [])]
            ax = p.get('measureAxis', {})
            asse = f"{ax.get('min')}–{ax.get('max')}" if ax.get('autoMinMax') is False or 'min' in ax else 'auto'
            serie = [m.get('series', {}).get('type', '') for m in p['qHyperCubeDef'].get('qMeasures', [])]
            yield (p['visualization'], ' / '.join(dims), p.get('barGrouping', {}).get('grouping'), ref,
                   p.get('color', {}).get('mode'), asse, serie)
        for f in c.get('qChildren', []):
            yield from grafici(f)
    for nome in sorted(n for n in os.listdir(cartella) if n.endswith('.json')):
        for c in json.load(open(os.path.join(cartella, nome)))['qRoot'].get('qChildren', []):
            for g in grafici(c):
                print(nome[:-5], '|', ' | '.join(str(x) for x in g))
    sys.exit()

for nome in sorted(n for n in os.listdir(cartella) if n.endswith('.json')):
    j = json.load(open(os.path.join(cartella, nome)))
    print(f"\n## {nome[:-5]}")
    for c in j['qRoot'].get('qChildren', []):
        for r in oggetto(c):
            print(r)

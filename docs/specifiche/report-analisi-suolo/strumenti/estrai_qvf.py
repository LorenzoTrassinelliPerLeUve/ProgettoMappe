"""Estrae dalla copia .qvf dell'app Qlik (solo lettura) i fogli completi (JSON con gli oggetti figli: testi, colori,
ordinamenti, linee di riferimento) e le immagini incorporate.

Uso: python3 estrai_qvf.py FILE.qvf CARTELLA-USCITA
Scrive CARTELLA/fogli/<titolo>.json, CARTELLA/media/<n>.<png|jpg> e CARTELLA/testi.md (i testi di ogni foglio).
"""
import json
import os
import re
import sys
import zlib

dati = open(sys.argv[1], 'rb').read()
uscita = sys.argv[2]
os.makedirs(os.path.join(uscita, 'fogli'), exist_ok=True)
os.makedirs(os.path.join(uscita, 'media'), exist_ok=True)

blocchi = []
for m in re.finditer(rb'\x78[\x01\x5e\x9c\xda]', dati):
    try:
        testo = zlib.decompressobj().decompress(dati[m.start():m.start() + 4_000_000])
    except zlib.error:
        continue
    if len(testo) > 20:
        blocchi.append(testo)

fogli = []
n_media = 0
for t in blocchi:
    if t.startswith(b'{'):
        try:
            j = json.loads(t[:t.rfind(b'}') + 1].decode('utf-8'))
        except ValueError:
            continue
        if j.get('qMetaData', {}).get('qType') == 'sheet':
            fogli.append(j)
    elif b'mediaitem' in t[:20]:
        for firma, est in ((b'\x89PNG', 'png'), (b'\xff\xd8\xff', 'jpg')):
            i = t.find(firma)
            if 0 <= i < 30:
                n_media += 1
                open(os.path.join(uscita, 'media', f'{n_media:02}.{est}'), 'wb').write(t[i:])


def testi(o, out):
    """Raccoglie i testi visibili di un oggetto e dei figli (markdown, titoli, sottotitoli, note)."""
    if isinstance(o, dict):
        for k in ('markdown', 'title', 'subtitle', 'footnote', 'content'):
            v = o.get(k)
            if isinstance(v, str) and v.strip():
                out.append((k, v))
            elif isinstance(v, dict) and v.get('qStringExpression'):
                out.append((k, '=' + v['qStringExpression'].get('qExpr', '')))
        v = o.get('text')
        if isinstance(v, str) and v.startswith('{"root"'):
            # sn-text: testo «lexical», paragrafi e titoli; le misure dinamiche come {misura}
            def piatto(n):
                if n.get('type') == 'text':
                    return n.get('text', '')
                return ''.join(piatto(c) for c in n.get('children', []))
            par = [piatto(c) for c in json.loads(v)['root']['children']]
            out.append(('testo', ' / '.join(p for p in par if p.strip())))
        url = o.get('qStaticContentUrlDef', {}).get('qUrl') if isinstance(o.get('qStaticContentUrlDef'), dict) else None
        if url:
            out.append(('immagine', url))
        for k, v in o.items():
            if k not in ('markdown', 'title', 'subtitle', 'footnote', 'text'):
                testi(v, out)
    elif isinstance(o, list):
        for v in o:
            testi(v, out)


righe = []
for j in sorted(fogli, key=lambda j: j['qRoot']['qProperty'].get('rank', 0)):
    p = j['qRoot']['qProperty']
    titolo = p.get('qMetaDef', {}).get('title', '?')
    nome = re.sub(r'[^\w\-() %]+', '_', titolo).strip()
    json.dump(j, open(os.path.join(uscita, 'fogli', f'{nome}.json'), 'w'), ensure_ascii=False, indent=1)
    righe.append(f'\n## {titolo}  (id {p["qInfo"]["qId"]})\n')
    out = []
    testi(j['qRoot'].get('qChildren', j['qRoot']), out)
    for k, v in out:
        righe.append(f'- {k}: {v}')
open(os.path.join(uscita, 'testi.md'), 'w').write('# Testi dei fogli (estratti dalla copia .qvf)\n' + '\n'.join(righe) + '\n')
print('fogli', len(fogli), 'immagini', n_media)

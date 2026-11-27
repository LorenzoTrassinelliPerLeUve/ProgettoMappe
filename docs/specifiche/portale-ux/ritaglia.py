"""Foto del Portale (task #387). Uso:
  ritaglia.py CARTELLA              tutte le foto a 256 colori (pesano un terzo) e l'elenco con le altezze
  ritaglia.py FOTO Y0 Y1 USCITA     un pezzo verticale di una foto (per guardarla da vicino)
"""
import sys
from pathlib import Path
from PIL import Image

if len(sys.argv) == 5:
    im = Image.open(sys.argv[1])
    im.crop((0, int(sys.argv[2]), im.width, min(int(sys.argv[3]), im.height))).save(sys.argv[4])
    sys.exit()

for f in sorted(Path(sys.argv[1]).glob("*.png")):
    im = Image.open(f)
    if im.mode != "P":
        im.convert("RGB").quantize(256).save(f, optimize=True)
    if not f.stem.endswith("-alto"):
        print(f.stem, im.size[1])

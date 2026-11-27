"""Foto della configurazione (task #384). Uso:
  ritaglia.py CARTELLA              la prima schermata di ogni foto in *-alto.png, poi tutte a 256 colori (pesano un terzo)
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
    if f.stem.endswith("-alto"):
        continue
    im = Image.open(f)
    alto = 1688 if "telefono" in f.stem else 768  # una schermata: 844 px × 2 sul telefono, 768 px sul computer
    im.crop((0, 0, im.width, min(alto, im.height))).save(f.with_name(f.stem + "-alto.png"))
for f in sorted(Path(sys.argv[1]).glob("*.png")):
    Image.open(f).convert("RGB").quantize(256).save(f, optimize=True)
    print(f.name)

"""Ritaglia la parte alta delle foto intere (la prima schermata, quella che si vede aprendo la pagina). Uso: ritaglia.py CARTELLA"""
import sys
from pathlib import Path
from PIL import Image

for f in sorted(Path(sys.argv[1]).glob("*-pagina-*.png")):
    if f.stem.endswith("-alto"):
        continue
    im = Image.open(f)
    alto = 1688 if "telefono" in f.stem else 768  # una schermata: 844 px × 2 sul telefono, 768 px sul computer
    im.crop((0, 0, im.width, min(alto, im.height))).save(f.with_name(f.stem + "-alto.png"))
    print(f.stem)

# Le foto vanno nel repository: 256 colori bastano per delle schermate e pesano un terzo.
for f in sorted(Path(sys.argv[1]).glob("*.png")):
    Image.open(f).convert("RGB").quantize(256).save(f, optimize=True)

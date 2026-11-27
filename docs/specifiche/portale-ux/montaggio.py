"""Mette più foto in un foglio solo, rimpicciolite (task #387). Uso: montaggio.py USCITA LARGHEZZA-CELLA COLONNE FOTO…"""
import sys
from PIL import Image, ImageDraw

uscita, cella, colonne, foto = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4:]
immagini = []
for f in foto:
    im = Image.open(f).convert("RGB")
    immagini.append((f.rsplit("/", 1)[-1], im.resize((cella, round(im.height * cella / im.width)))))
righe = [immagini[i:i + colonne] for i in range(0, len(immagini), colonne)]
alte = [max(im.height for _, im in r) + 18 for r in righe]
foglio = Image.new("RGB", (cella * colonne + 8 * (colonne - 1), sum(alte) + 8 * len(righe)), "white")
d = ImageDraw.Draw(foglio)
y = 0
for r, h in zip(righe, alte):
    for i, (nome, im) in enumerate(r):
        x = i * (cella + 8)
        d.text((x, y), nome, fill="black")
        foglio.paste(im, (x, y + 16))
    y += h + 8
foglio.save(uscita)

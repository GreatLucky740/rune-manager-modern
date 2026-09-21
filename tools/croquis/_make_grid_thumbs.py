"""Miniatures 140px pour la grille. Originaux restent dans croquis/sets."""
import os
from PIL import Image

SRC = r"C:\Users\Great-Lucky\Documents\Rune_Manager_Modern\Donnees\assets\croquis\sets"
DST = r"C:\Users\Great-Lucky\Documents\Rune_Manager_Modern\Donnees\assets"
n = 0
for fn in os.listdir(SRC):
    if not fn.endswith("-legendaire.png"):
        continue
    name = fn[: -len("-legendaire.png")]
    im = Image.open(os.path.join(SRC, fn)).convert("RGBA")
    im.thumbnail((140, 140), Image.Resampling.LANCZOS)
    im.save(os.path.join(DST, "croquis-rune-3d-%s.png" % name), "PNG", optimize=True)
    n += 1
print("thumbs", n)

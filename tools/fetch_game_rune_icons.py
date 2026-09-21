# -*- coding: utf-8 -*-
"""Download in-game rune textures (set emblems + 6 slot frames + quality BGs)
and compose every set x slot icon locally. No runtime dependency on any website.
"""
from __future__ import print_function
import io
import os
import ssl
import sys
import urllib.request

try:
    from PIL import Image
except ImportError:
    os.system(sys.executable + " -m pip install pillow")
    from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LIVE = r"C:\Users\Great-Lucky\Documents\Rune_Manager_Modern\Donnees"
SRC_APP = os.path.join(ROOT, "rune_manager_app", "assets")
RELEASE = os.path.join(ROOT, "outputs", "rune_manager_release", "Donnees", "assets")
GH = "https://raw.githubusercontent.com/swarfarm/swarfarm/master/herders/static/herders/images/runes/"

SETS = [
    "energy", "guard", "swift", "blade", "rage", "focus", "endure", "fatal",
    "despair", "vampire", "violent", "nemesis", "will", "shield", "revenge",
    "destroy", "fight", "determination", "enhance", "accuracy", "tolerance",
    "seal", "intangible",
]
SLOTS = [1, 2, 3, 4, 5, 6]
# Official in-game layout (50x50 canvas, 30x30 emblem), from game UI compositing.
SYMBOL_POS = {1: (9, 12), 2: (7, 9), 3: (7, 9), 4: (9, 6), 5: (11, 9), 6: (10, 9)}
QUALITIES = ["normal", "magic", "rare", "hero", "legend"]
CTX = ssl.create_default_context()
UA = {"User-Agent": "RuneManagerModern/1.0 (local asset pack)"}


def fetch(url):
    req = urllib.request.Request(url, headers=UA)
    with urllib.request.urlopen(req, context=CTX, timeout=30) as r:
        data = r.read()
    if len(data) < 200 or data[:8] != b"\x89PNG\r\n\x1a\n":
        raise RuntimeError("not a png: " + url)
    return data


def save_png(path, data):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "wb") as f:
        f.write(data)


def load_png(data):
    img = Image.open(io.BytesIO(data)).convert("RGBA")
    return img


def compose(bg, slot_img, emblem, slot, size=128):
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    canvas.alpha_composite(bg.resize((size, size), Image.LANCZOS))
    canvas.alpha_composite(slot_img.resize((size, size), Image.LANCZOS), (0, 0))
    scale = size / 50.0
    emblem_size = int(round(30 * scale))
    x = int(round(SYMBOL_POS[slot][0] * scale))
    y = int(round(SYMBOL_POS[slot][1] * scale))
    emblem_r = emblem.resize((emblem_size, emblem_size), Image.LANCZOS)
    canvas.alpha_composite(emblem_r, (x, y))
    return canvas


def copy_into(src_dir, dest_root):
    if not dest_root or not os.path.isdir(os.path.dirname(dest_root)) and not os.path.isdir(dest_root):
        parent = os.path.dirname(dest_root)
        if parent and not os.path.isdir(parent):
            return
    os.makedirs(dest_root, exist_ok=True)
    for dirpath, _, files in os.walk(src_dir):
        rel = os.path.relpath(dirpath, src_dir)
        target_dir = dest_root if rel == "." else os.path.join(dest_root, rel)
        os.makedirs(target_dir, exist_ok=True)
        for name in files:
            with open(os.path.join(dirpath, name), "rb") as f:
                data = f.read()
            with open(os.path.join(target_dir, name), "wb") as f:
                f.write(data)


def main():
    out_src = os.path.join(SRC_APP, "runes")
    layers = os.path.join(out_src, "_layers")
    os.makedirs(layers, exist_ok=True)

    print("Downloading in-game rune layers...")
    slot_imgs = {}
    for n in SLOTS:
        data = fetch(GH + "rune%d.png" % n)
        save_png(os.path.join(layers, "slot%d.png" % n), data)
        slot_imgs[n] = load_png(data)
        print("  slot", n, len(data), "bytes")

    bgs = {}
    for q in QUALITIES:
        for suffix, key in [("", q), ("_immemorial", q + "_ancient")]:
            name = "bg_%s%s.png" % (q, suffix)
            data = fetch(GH + name)
            save_png(os.path.join(layers, name), data)
            bgs[key] = load_png(data)
            print("  ", name, len(data), "bytes")

    emblems = {}
    sets_dir = os.path.join(SRC_APP, "sets")
    os.makedirs(sets_dir, exist_ok=True)
    for s in SETS:
        data = fetch(GH + s + ".png")
        save_png(os.path.join(sets_dir, s + ".png"), data)
        emblems[s] = load_png(data)
        print("  set", s, len(data), "bytes")

    print("Composing set x slot icons...")
    count = 0
    legend = bgs["legend"]
    for s in SETS:
        for n in SLOTS:
            img = compose(legend, slot_imgs[n], emblems[s], n, 128)
            path = os.path.join(out_src, "%s_%d.png" % (s, n))
            img.save(path, "PNG")
            count += 1
    print("Wrote", count, "composed icons")

    # Preview sheet: 23 rows x 6 cols
    cell = 96
    sheet = Image.new("RGBA", (cell * 6, cell * len(SETS)), (12, 18, 28, 255))
    for r, s in enumerate(SETS):
        for c, n in enumerate(SLOTS):
            icon = Image.open(os.path.join(out_src, "%s_%d.png" % (s, n))).convert("RGBA")
            icon = icon.resize((cell - 8, cell - 8), Image.LANCZOS)
            sheet.alpha_composite(icon, (c * cell + 4, r * cell + 4))
    preview = os.path.join(out_src, "_preview_all_sets_slots.png")
    sheet.save(preview, "PNG")
    print("Preview", preview)

    targets = [
        os.path.join(RELEASE, "runes"),
        os.path.join(LIVE, "assets", "runes"),
        os.path.join(LIVE, "assets", "sets"),
    ]
    copy_into(out_src, os.path.join(RELEASE, "runes"))
    copy_into(out_src, os.path.join(LIVE, "assets", "runes"))
    copy_into(sets_dir, os.path.join(RELEASE, "sets"))
    copy_into(sets_dir, os.path.join(LIVE, "assets", "sets"))
    copy_into(sets_dir, os.path.join(SRC_APP, "sets"))
    print("Copied into release + live install")
    print("OK", count, "icons")


if __name__ == "__main__":
    main()

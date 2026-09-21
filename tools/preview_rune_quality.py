# -*- coding: utf-8 -*-
from PIL import Image, ImageEnhance
import os

ROOT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "rune_manager_app", "assets")
LAYERS = os.path.join(ROOT, "runes", "_layers")
SETS = os.path.join(ROOT, "sets")
OUT = os.path.join(ROOT, "runes", "_preview_quality.png")
POS = {1: (9, 12), 2: (7, 9), 3: (7, 9), 4: (9, 6), 5: (11, 9), 6: (10, 9)}
FILLS = {
    5: (198, 152, 56, 255),
    4: (168, 72, 186, 255),
    3: (48, 156, 220, 255),
    2: (42, 176, 88, 255),
    1: (118, 110, 106, 255),
}
RIM = (210, 168, 64, 255)


def tint(src, color, size):
    img = src.resize((size, size), Image.LANCZOS).convert("RGBA")
    px = img.load()
    tr, tg, tb, _ = color
    for y in range(size):
        for x in range(size):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            lum = max(10, int(r * 0.299 + g * 0.587 + b * 0.114))
            nr = min(255, tr * lum // 132)
            ng = min(255, tg * lum // 132)
            nb = min(255, tb * lum // 132)
            px[x, y] = (nr, ng, nb, a)
    return img


def compose(set_name, slot, grade, size=128):
    slot_img = Image.open(os.path.join(LAYERS, "slot%d.png" % slot)).convert("RGBA")
    emblem = Image.open(os.path.join(SETS, set_name + ".png")).convert("RGBA")
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    canvas.alpha_composite(tint(slot_img, RIM, size))
    inner = int(size * 0.86)
    pad = (size - inner) // 2
    fill = tint(slot_img, FILLS[grade], size).resize((inner, inner), Image.LANCZOS)
    canvas.alpha_composite(fill, (pad, pad))
    scale = size / 50.0
    es = int(round(30 * scale))
    x = int(round(POS[slot][0] * scale))
    y = int(round(POS[slot][1] * scale))
    canvas.alpha_composite(emblem.resize((es, es), Image.LANCZOS), (x, y))
    return canvas


hero = compose("intangible", 2, 4)
legend = compose("seal", 2, 5)
sheet = Image.new("RGBA", (220, 128), (42, 32, 18, 255))
sheet.alpha_composite(hero, (8, 0))
sheet.alpha_composite(legend, (84, 0))
sheet.save(OUT, "PNG")
print("wrote", OUT)

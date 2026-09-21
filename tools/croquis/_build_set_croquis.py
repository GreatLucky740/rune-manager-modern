"""6 pierres 3D pleines (pas de creux), puis creux = forme du set sur copie."""
from PIL import Image, ImageFilter
import numpy as np
import os

BASE = r"C:\Users\Great-Lucky\Documents\Rune_Manager_Modern\Donnees\assets"
CROQUIS = os.path.join(BASE, "croquis")
LAYER_DIR = os.path.join(BASE, "runes", "_layers")
BLANK_DIR = os.path.join(CROQUIS, "blanks")
OUT_DIR = os.path.join(CROQUIS, "sets")
SETS_DIR = os.path.join(BASE, "sets")

HI = np.array([255, 214, 92], np.float32)
MID = np.array([255, 158, 28], np.float32)
LO = np.array([168, 42, 8], np.float32)
WALL = np.array([255, 78, 12], np.float32)
WALL_DK = np.array([120, 28, 6], np.float32)

# charcoal like Despair stone
DARK = np.array([42.0, 44.0, 46.0], np.float32)
MIDG = np.array([86.0, 88.0, 90.0], np.float32)
LITE = np.array([158.0, 154.0, 146.0], np.float32)
WEAR = np.array([196.0, 118.0, 52.0], np.float32)

CANVAS = {1: (754, 898), 2: (772, 564), 3: (866, 654), 4: (828, 898), 5: (869, 670), 6: (739, 579)}
# opaque box from Despair croquis so slot size/placement match
OPAQUE = {
    1: (27, 8, 703, 882),
    2: (10, 10, 753, 545),
    3: (13, 26, 840, 601),
    4: (8, 8, 703, 882),
    5: (14, 26, 841, 617),
    6: (23, 23, 693, 548),
}

SETS = [
    "energy", "guard", "swift", "blade", "rage", "focus", "endure", "fatal",
    "despair", "vampire", "violent", "nemesis", "will", "shield", "revenge",
    "destroy", "fight", "determination", "enhance", "accuracy", "tolerance",
    "seal", "intangible",
]


def dilate(mask, r):
    if r <= 0:
        return mask.copy()
    img = Image.fromarray((mask.astype(np.uint8) * 255), "L")
    left = int(r)
    while left > 0:
        step = 4 if left > 4 else left
        img = img.filter(ImageFilter.MaxFilter(step * 2 + 1))
        left -= step
    return np.array(img) > 127


def erode(mask, r):
    if r <= 0:
        return mask.copy()
    img = Image.fromarray((mask.astype(np.uint8) * 255), "L")
    left = int(r)
    while left > 0:
        step = 4 if left > 4 else left
        img = img.filter(ImageFilter.MinFilter(step * 2 + 1))
        left -= step
    return np.array(img) > 127


def edt(mask):
    h, w = mask.shape
    inf = 1e6
    d = np.where(mask, inf, 0.0).astype(np.float32)
    for y in range(h):
        for x in range(w):
            if d[y, x] == 0:
                continue
            v = d[y, x]
            if y:
                v = min(v, d[y - 1, x] + 1)
                if x:
                    v = min(v, d[y - 1, x - 1] + 1.414)
                if x + 1 < w:
                    v = min(v, d[y - 1, x + 1] + 1.414)
            if x:
                v = min(v, d[y, x - 1] + 1)
            d[y, x] = v
    for y in range(h - 1, -1, -1):
        for x in range(w - 1, -1, -1):
            if d[y, x] == 0:
                continue
            v = d[y, x]
            if y + 1 < h:
                v = min(v, d[y + 1, x] + 1)
                if x:
                    v = min(v, d[y + 1, x - 1] + 1.414)
                if x + 1 < w:
                    v = min(v, d[y + 1, x + 1] + 1.414)
            if x + 1 < w:
                v = min(v, d[y, x + 1] + 1)
            d[y, x] = v
    d = np.where(mask, d, 0.0)
    d[d > 1e5] = 0
    return d


def dist_inside(mask):
    step = 2
    d = edt(mask[::step, ::step])
    h, w = mask.shape
    mx = float(d.max()) if d.size else 1.0
    scale = 255.0 / max(mx, 1e-3)
    im = Image.fromarray(np.clip(d * scale, 0, 255).astype(np.uint8), "L")
    im = im.resize((w, h), Image.Resampling.BILINEAR)
    return np.array(im).astype(np.float32) / scale * step


def fill_holes(mask):
    h, w = mask.shape
    seen = np.zeros_like(mask)
    stack = []
    for x in range(w):
        if not mask[0, x]:
            stack.append((x, 0))
        if not mask[h - 1, x]:
            stack.append((x, h - 1))
    for y in range(h):
        if not mask[y, 0]:
            stack.append((0, y))
        if not mask[y, w - 1]:
            stack.append((w - 1, y))
    while stack:
        x, y = stack.pop()
        if x < 0 or y < 0 or x >= w or y >= h or seen[y, x] or mask[y, x]:
            continue
        seen[y, x] = True
        stack.extend(((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)))
    return mask | ~seen


def slot_hull(slot, W, H, ox, oy, ow, oh):
    src = Image.open(os.path.join(LAYER_DIR, "slot%d.png" % slot)).convert("RGBA")
    a = np.array(src)
    if a[..., 3].max() > 40 and a[..., 3].mean() < 250:
        m = a[..., 3]
    else:
        m = ((a[..., :3].mean(2) > 28) * 255).astype(np.uint8)
        m = Image.fromarray(m, "L")
        return _place_hull(m, W, H, ox, oy, ow, oh)
    m = Image.fromarray(m, "L")
    return _place_hull(m, W, H, ox, oy, ow, oh)


def _place_hull(m, W, H, ox, oy, ow, oh):
    m = m.resize((ow * 2, oh * 2), Image.Resampling.LANCZOS)
    m = m.filter(ImageFilter.GaussianBlur(2.4))
    arr = np.array(m).astype(np.float32) / 255.0
    hard = (arr > 0.42).astype(np.uint8) * 255
    m = Image.fromarray(hard, "L").resize((ow, oh), Image.Resampling.LANCZOS)
    m = m.filter(ImageFilter.GaussianBlur(0.8))
    canvas = Image.new("L", (W, H), 0)
    canvas.paste(m, (ox, oy))
    return np.array(canvas).astype(np.float32) / 255.0


def _blur_l(ch, r):
    im = Image.fromarray(np.clip(ch, 0, 255).astype(np.uint8), "L")
    return np.array(im.filter(ImageFilter.GaussianBlur(r))).astype(np.float32)


def fill_creux_with_charcoal(arr):
    """Keep Despair 3D stone 100%. Replace only orange S with inner charcoal."""
    a = arr[..., 3] >= 40
    d = dist_inside(a)
    r, g, b = arr[..., 0].astype(np.int16), arr[..., 1].astype(np.int16), arr[..., 2].astype(np.int16)
    lum = arr[..., :3].astype(np.float32).mean(2)
    sat = arr[..., :3].max(2).astype(np.int16) - arr[..., :3].min(2).astype(np.int16)
    orange = a & (r >= 145) & (r >= g + 10) & (b <= 140) & (r >= b + 18)
    s_orange = orange & (d > 12)
    hole = dilate(s_orange, 4) & a
    out = arr.astype(np.float32)
    known = a & ~hole & (sat < 30) & (lum >= 60) & (lum <= 140)
    still = hole & ~known
    for _ in range(50):
        ring = dilate(known, 1) & still
        if not ring.any():
            break
        m = known.astype(np.float32)
        rgb_m = out[..., :3] * m[..., None]
        bm = _blur_l(m * 255.0, 1.4) / 255.0
        acc = np.stack([_blur_l(rgb_m[..., c], 1.4) for c in range(3)], -1)
        fill = acc / np.maximum(bm[..., None], 0.08)
        out[ring, :3] = fill[ring]
        out[ring, 3] = 255
        known |= ring
        still &= ~ring
    leftover = hole & ~known
    if leftover.any() and known.any():
        med = np.median(out[known][:, :3], axis=0)
        out[leftover, :3] = med
        out[leftover, 3] = 255
    return np.clip(out, 0, 255).astype(np.uint8)


def render_blank(slot):
    """Same 3D charcoal Despair stone. No S. Inner face filled gris-noir."""
    src = os.path.join(BASE, "croquis-rune-3d-slot%d.png" % slot)
    arr = np.array(Image.open(src).convert("RGBA"))
    return fill_creux_with_charcoal(arr)


def make_blanks():
    os.makedirs(BLANK_DIR, exist_ok=True)
    paths = []
    for slot in range(1, 7):
        blank = render_blank(slot)
        path = os.path.join(BLANK_DIR, "croquis-blank-slot%d.png" % slot)
        Image.fromarray(blank, "RGBA").save(path)
        paths.append(path)
        print("blank", slot, path)
    return paths


def extract_glyph(path, size):
    im = Image.open(path).convert("RGBA")
    a = np.array(im)
    rgb = a[..., :3].astype(np.float32)
    lum = rgb.mean(2)
    sat = rgb.max(2) - rgb.min(2)
    core = (a[..., 3] > 80) & (lum > 148) & (sat < 100)
    if core.sum() < 30:
        core = (a[..., 3] > 80) & (lum > 120) & (sat < 130)
    ys, xs = np.where(core)
    if len(ys) < 8:
        raise RuntimeError("no glyph " + path)
    pad = 2
    y0, y1 = max(0, ys.min() - pad), min(core.shape[0], ys.max() + 1 + pad)
    x0, x1 = max(0, xs.min() - pad), min(core.shape[1], xs.max() + 1 + pad)
    crop = Image.fromarray((core[y0:y1, x0:x1].astype(np.uint8) * 255), "L")
    crop = crop.resize((crop.width * 14, crop.height * 14), Image.Resampling.LANCZOS)
    crop = crop.filter(ImageFilter.GaussianBlur(1.8))
    arr = np.array(crop)
    hard = fill_holes(arr > 88)
    hard = erode(dilate(hard, 1), 1)
    ys, xs = np.where(hard)
    pad = 20
    y0, y1 = max(0, ys.min() - pad), min(hard.shape[0], ys.max() + 1 + pad)
    x0, x1 = max(0, xs.min() - pad), min(hard.shape[1], xs.max() + 1 + pad)
    hard = hard[y0:y1, x0:x1]
    ch, cw = hard.shape
    scale = size / float(max(ch, cw))
    nw, nh = max(8, int(round(cw * scale))), max(8, int(round(ch * scale)))
    hard_im = Image.fromarray((hard.astype(np.uint8) * 255), "L").resize((nw, nh), Image.Resampling.LANCZOS)
    hard_im = hard_im.filter(ImageFilter.GaussianBlur(0.8))
    alpha = np.array(hard_im).astype(np.float32) / 255.0
    hard = alpha > 0.45
    return hard, alpha


def carve_set_creux(stone, glyph, alpha, y0, x0, wall=14):
    h, w = glyph.shape
    fill = erode(glyph, 2)
    fill_a = alpha.copy()
    fill_a[~dilate(fill, 1)] = 0
    fill_a = np.array(Image.fromarray((np.clip(fill_a, 0, 1) * 255).astype(np.uint8), "L").filter(ImageFilter.GaussianBlur(0.9))).astype(np.float32) / 255.0
    dist_in = edt(fill)
    well = dilate(glyph, wall)
    dist_out = edt(~fill)
    gy, gx = np.gradient(np.maximum(dist_in, 0.01))
    nlen = np.sqrt(gx * gx + gy * gy + 0.28)
    nx, ny = gx / nlen, gy / nlen
    lx, ly, lz = -0.52, -0.68, 0.55
    ndot = np.clip(-nx * lx - ny * ly + lz, 0, 1)
    t = np.clip(ndot ** 0.8, 0, 1)
    body = LO * (1 - t)[..., None] + HI * t[..., None]
    body = body * 0.34 + MID * 0.66
    edge = np.exp(-np.maximum(dist_in, 0) / 3.0)
    body = body * (1 - 0.78 * edge)[..., None] + WALL * (0.78 * edge)[..., None]
    rng = np.random.RandomState(5)
    body = np.clip(body + rng.normal(0, 3.0, body.shape), 0, 255)

    out = stone.astype(np.float32)
    H, W = out.shape[:2]
    yy = np.arange(h)[:, None] + y0
    xx = np.arange(w)[None, :] + x0
    valid = (yy >= 0) & (yy < H) & (xx >= 0) & (xx < W)

    lip = dilate(well, 1) & ~well & valid
    ys, xs = np.where(lip)
    if len(ys):
        out[ys + y0, xs + x0, :3] *= 0.88

    walls = well & ~fill & valid
    ys, xs = np.where(walls)
    yys, xxs = ys + y0, xs + x0
    from_g = dist_out[ys, xs]
    wgy, wgx = np.gradient(dist_out)
    wn = np.sqrt(wgx[ys, xs] ** 2 + wgy[ys, xs] ** 2 + 1e-6)
    wnx, wny = wgx[ys, xs] / wn, wgy[ys, xs] / wn
    wlit = np.clip(-wnx * lx - wny * ly + 0.25, 0, 1)
    inner = np.clip(1.0 - (from_g / (wall * 0.70)), 0, 1) ** 0.65
    wall_col = WALL_DK * (1 - wlit)[:, None] + WALL * (0.55 + 0.45 * wlit)[:, None]
    mix = np.clip(0.55 + 0.45 * inner, 0, 1)
    base = out[yys, xxs, :3]
    out[yys, xxs, :3] = base * (1 - mix[:, None]) + wall_col * mix[:, None]

    ys, xs = np.where((fill_a > 0.05) & valid)
    yys, xxs = ys + y0, xs + x0
    fa = fill_a[ys, xs][:, None]
    out[yys, xxs, :3] = out[yys, xxs, :3] * (1 - fa) + body[ys, xs] * fa
    out[yys, xxs, 3] = np.maximum(out[yys, xxs, 3], fa[:, 0] * 255)
    return np.clip(out, 0, 255).astype(np.uint8)


def inner_bbox(arr):
    a = arr[..., 3] >= 40
    d = dist_inside(a)
    bevel = max(22.0, 0.118 * min(a.shape[0], a.shape[1]))
    inner = a & (d > bevel)
    ys, xs = np.where(inner if inner.any() else a)
    return xs.min(), ys.min(), xs.max() + 1, ys.max() + 1


def carve_all(only=None):
    os.makedirs(OUT_DIR, exist_ok=True)
    names = SETS if only is None else list(only)
    for slot in range(1, 7):
        blank_path = os.path.join(BLANK_DIR, "croquis-blank-slot%d.png" % slot)
        blank = np.array(Image.open(blank_path).convert("RGBA"))
        x0, y0, x1, y1 = inner_bbox(blank)
        iw, ih = x1 - x0, y1 - y0
        cx, cy = (x0 + x1) * 0.5, (y0 + y1) * 0.5
        target = int(min(iw, ih) * 0.70)
        wall = max(8, int(round(min(iw, ih) * 0.026)))
        for name in names:
            gpath = os.path.join(SETS_DIR, name + ".png")
            glyph, alpha = extract_glyph(gpath, target)
            mh, mw = glyph.shape
            gx0 = int(round(cx - mw / 2.0))
            gy0 = int(round(cy - mh / 2.0))
            out = carve_set_creux(blank, glyph, alpha, gy0, gx0, wall=wall)
            dest = os.path.join(OUT_DIR, "%s-slot%d-legendaire.png" % (name, slot))
            Image.fromarray(out, "RGBA").save(dest)
            print("carved", name, "slot", slot)


if __name__ == "__main__":
    import sys
    stage = sys.argv[1] if len(sys.argv) > 1 else "blanks"
    if stage == "blanks":
        make_blanks()
    elif stage == "violent1":
        carve_all(only=["violent"])
    elif stage == "all":
        carve_all()
    else:
        raise SystemExit("blanks | violent1 | all")

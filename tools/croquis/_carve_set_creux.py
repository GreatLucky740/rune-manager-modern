"""Creuse le logo de chaque set dans les 6 bases. Creux = forme du set. Orange seulement dans le creux."""
import math
import os
import cv2
import numpy as np
from PIL import Image, ImageDraw

BASE = r"C:\Users\Great-Lucky\Documents\Rune_Manager_Modern\Donnees\assets"
BLANK_DIR = os.path.join(BASE, "croquis", "blanks")
SETS_DIR = os.path.join(BASE, "sets")
OUT_DIR = os.path.join(BASE, "croquis", "sets")
SCALE = 40

LIGHT = np.array([-0.42, -0.78, 0.46], np.float32)
LIGHT /= np.linalg.norm(LIGHT)

HI = np.array([255.0, 214.0, 92.0], np.float32)
MID = np.array([255.0, 158.0, 28.0], np.float32)
LO = np.array([168.0, 42.0, 8.0], np.float32)
WALL = np.array([255.0, 78.0, 12.0], np.float32)
WALL_DK = np.array([120.0, 28.0, 6.0], np.float32)

SETS = [
    "energy", "guard", "swift", "blade", "rage", "focus", "endure", "fatal",
    "despair", "vampire", "violent", "nemesis", "will", "shield", "revenge",
    "destroy", "fight", "determination", "enhance", "accuracy", "tolerance",
    "seal", "intangible",
]

BEVEL = 43.0
INNER_MARGIN = 8.0


def dilate(mask, r):
    if r <= 0:
        return mask.copy()
    k = int(round(r)) * 2 + 1
    ker = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (k, k))
    return cv2.dilate((mask.astype(np.uint8) * 255), ker) > 127


def erode(mask, r):
    if r <= 0:
        return mask.copy()
    k = int(round(r)) * 2 + 1
    ker = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (k, k))
    return cv2.erode((mask.astype(np.uint8) * 255), ker) > 127


def dist(mask):
    return cv2.distanceTransform((mask.astype(np.uint8) * 255), cv2.DIST_L2, 5)


def _drop_hud_junk(mask):
    n, lab, stats, cents = cv2.connectedComponentsWithStats(mask.astype(np.uint8), 8)
    if n <= 1:
        return mask
    h, w = mask.shape
    areas = stats[1:, cv2.CC_STAT_AREA]
    mx = float(areas.max())
    tot = float(areas.sum())
    gx = float((cents[1:, 0] * areas).sum() / tot)
    gy = float((cents[1:, 1] * areas).sum() / tot)
    diag = float(np.hypot(h, w))
    out = np.zeros_like(mask, bool)
    for i in range(1, n):
        ar = float(stats[i, cv2.CC_STAT_AREA])
        d = float(np.hypot(cents[i, 0] - gx, cents[i, 1] - gy))
        x, y = int(stats[i, cv2.CC_STAT_LEFT]), int(stats[i, cv2.CC_STAT_TOP])
        bw, bh = int(stats[i, cv2.CC_STAT_WIDTH]), int(stats[i, cv2.CC_STAT_HEIGHT])
        near_c = d < 0.16 * diag
        near_b = x <= 1 or y <= 1 or x + bw >= w - 1 or y + bh >= h - 1
        aspect = max(bw, bh) / max(1.0, min(bw, bh))
        compact = ar / max(1.0, bw * bh)
        # HUD Swift dot + hole highlights: small round blobs
        if ar < 0.45 * mx and aspect < 1.45 and compact > 0.52:
            continue
        if ar >= 0.30 * mx:
            keep = True
        elif near_c or near_b:
            keep = False
        else:
            keep = ar >= 0.12 * mx
        if keep:
            out |= lab == i
    return out


def _guard_ring(mask):
    """U + barre du bas = anneau, sans pont plein dans le trou (screen Guard)."""
    ys, xs = np.where(mask)
    if len(ys) < 8:
        return mask
    pts = np.column_stack((xs, ys)).astype(np.int32)
    hull = cv2.convexHull(pts)
    filled = np.zeros(mask.shape, np.uint8)
    cv2.fillConvexPoly(filled, hull, 1)
    filled = filled > 0
    # epaisseur ~ barre du bas
    n, lab, stats, _ = cv2.connectedComponentsWithStats(mask.astype(np.uint8), 8)
    thick = 7.0
    if n > 1:
        hs = [float(stats[i, cv2.CC_STAT_HEIGHT]) for i in range(1, n)]
        thick = max(5.0, min(hs) * 0.92)
    hole = erode(filled, thick)
    return filled & ~hole


def _bridge_large_gaps(mask, max_dist=6.3, min_frac=0.18):
    n, lab, stats, _ = cv2.connectedComponentsWithStats(mask.astype(np.uint8), 8)
    if n <= 2:
        return mask
    mx = float(stats[1:, cv2.CC_STAT_AREA].max())
    out = mask.copy()
    for i in range(1, n):
        if float(stats[i, cv2.CC_STAT_AREA]) < min_frac * mx:
            continue
        di = cv2.distanceTransform((lab != i).astype(np.uint8), cv2.DIST_L2, 5)
        for j in range(i + 1, n):
            if float(stats[j, cv2.CC_STAT_AREA]) < min_frac * mx:
                continue
            d = float(di[lab == j].min())
            if d <= max_dist:
                r = int(np.ceil(d / 2.0)) + 1
                out |= dilate(lab == i, r) & dilate(lab == j, r)
    return out


def _drop_hole_highlights(mask):
    n, lab, stats, _ = cv2.connectedComponentsWithStats(mask.astype(np.uint8), 8)
    if n <= 1:
        return mask
    mx = float(stats[1:, cv2.CC_STAT_AREA].max())
    keep = np.zeros_like(mask)
    for i in range(1, n):
        ar = float(stats[i, cv2.CC_STAT_AREA])
        bw, bh = int(stats[i, cv2.CC_STAT_WIDTH]), int(stats[i, cv2.CC_STAT_HEIGHT])
        aspect = max(bw, bh) / max(1.0, min(bw, bh))
        compact = ar / max(1.0, bw * bh)
        if ar < 0.22 * mx and aspect < 1.85 and compact > 0.45:
            continue
        if ar >= 0.10 * mx:
            keep |= lab == i
    return keep


def _crop_ring(m):
    ys, xs = np.where(m > 0.08)
    return np.clip(m[int(ys.min()) - 8:int(ys.max()) + 9, int(xs.min()) - 8:int(xs.max()) + 9], 0, 1)


def _rr(size, box, r):
    im = Image.new("L", size, 0)
    ImageDraw.Draw(im).rounded_rectangle(box, radius=max(1, int(round(r))), fill=255)
    return np.array(im) > 0


def _guard_logo():
    """Preset Guard creme: casque n, barre bas, point milieu."""
    s = 800
    sc = 16.0
    hw, hh, bh, gap = 35 * sc, 30 * sc, 7 * sc, 5 * sc
    stroke = 6.0 * sc
    total_h = hh + gap + bh
    x0 = (s - hw) / 2.0
    y0 = (s - total_h) / 2.0
    x1, y1 = x0 + hw, y0 + hh
    r_out = 0.18 * hw
    r_in = 0.16 * (hw - 2 * stroke)
    helmet = _rr((s, s), [x0, y0, x1, y1], r_out)
    hole = _rr((s, s), [x0 + stroke, y0 + stroke, x1 - stroke, y1 + stroke], r_in)
    helmet = helmet & ~hole
    by0 = y1 + gap
    bar = _rr((s, s), [x0, by0, x1, by0 + bh], bh * 0.48)
    canvas = np.zeros((s, s), np.uint8)
    canvas[helmet | bar] = 255
    cx = x0 + 17.3 * sc
    cy = y0 + 19.5 * sc
    cv2.circle(canvas, (int(round(cx)), int(round(cy))), int(round(4.2 * sc)), 255, -1, lineType=cv2.LINE_AA)
    m = canvas.astype(np.float32) / 255.0
    return _crop_ring(cv2.GaussianBlur(m, (0, 0), 0.45))


def _shield_logo():
    """Losange creux + point haut-gauche + point bas-droite (PNG Preset)."""
    s = 800
    cx = cy = s / 2.0
    hh = 260.0
    hw = (30.0 / 37.0) * hh
    canvas = np.zeros((s, s), np.uint8)
    outer_pts = np.array(
        [[cx, cy - hh], [cx + hw, cy], [cx, cy + hh], [cx - hw, cy]], np.int32
    )
    inner_pts = np.array(
        [
            [cx, cy - hh * (22.0 / 37.0)],
            [cx + hw * (16.0 / 30.0), cy],
            [cx, cy + hh * (22.0 / 37.0)],
            [cx - hw * (16.0 / 30.0), cy],
        ],
        np.int32,
    )
    cv2.fillConvexPoly(canvas, outer_pts, 255, lineType=cv2.LINE_AA)
    cv2.fillConvexPoly(canvas, inner_pts, 0, lineType=cv2.LINE_AA)
    r = int(round((4.0 / 15.0) * hw))
    nw = (int(round(cx - 1.087 * hw)), int(round(cy - 0.789 * hh)))
    se = (int(round(cx + 1.047 * hw)), int(round(cy + 0.778 * hh)))
    cv2.circle(canvas, nw, r, 255, -1, lineType=cv2.LINE_AA)
    cv2.circle(canvas, se, r, 255, -1, lineType=cv2.LINE_AA)
    m = canvas.astype(np.float32) / 255.0
    return _crop_ring(cv2.GaussianBlur(m, (0, 0), 0.45))


def _smooth_contour(pts):
    pts = pts.astype(np.float64)
    n = len(pts)
    if n < 16:
        return pts
    k = max(9, (n // 80) | 1)
    if k % 2 == 0:
        k += 1
    kern = np.hanning(k)
    kern /= kern.sum()
    pad = k // 2
    for _ in range(2):
        x = np.concatenate([pts[-pad:, 0], pts[:, 0], pts[:pad, 0]])
        y = np.concatenate([pts[-pad:, 1], pts[:, 1], pts[:pad, 1]])
        pts[:, 0] = np.convolve(x, kern, mode="valid")
        pts[:, 1] = np.convolve(y, kern, mode="valid")
    return pts


def _smooth_mask(mask):
    """Lisse iso-contour. Garde trous."""
    pad = 8
    src = cv2.copyMakeBorder(
        (mask.astype(np.uint8) * 255), pad, pad, pad, pad, cv2.BORDER_CONSTANT, 0
    )
    cnts, hier = cv2.findContours(src, cv2.RETR_CCOMP, cv2.CHAIN_APPROX_NONE)
    canvas = np.zeros_like(src)
    if hier is None or len(cnts) == 0:
        return mask
    hier = hier[0]
    def poly(i):
        pts = _smooth_contour(cnts[i].reshape(-1, 2))
        return np.round(pts).astype(np.int32).reshape(-1, 1, 2)
    for i in range(len(cnts)):
        if hier[i][3] < 0 and len(cnts[i]) >= 12:
            cv2.fillPoly(canvas, [poly(i)], 255)
    for i in range(len(cnts)):
        if hier[i][3] >= 0 and len(cnts[i]) >= 12:
            cv2.fillPoly(canvas, [poly(i)], 0)
    return canvas[pad:-pad, pad:-pad] > 127


def extract_glyph(name):
    """Couverture AA du PNG Preset, LANCZOS. Pas de DP (fond les #)."""
    path = os.path.join(SETS_DIR, name + ".png")
    a = np.array(Image.open(path).convert("RGBA"))
    rgb = a[..., :3].astype(np.float32)
    lum = rgb.mean(2)
    al = a[..., 3].astype(np.float32) / 255.0
    vis = al > 0.15
    cream = vis & (lum > 150) & (rgb[..., 1] > 130)
    if name != "focus":
        cream = _drop_hole_highlights(cream)
    if name == "guard":
        return _guard_logo()
    if name == "shield":
        return _shield_logo()
    ys, xs = np.where(cream)
    if len(ys) < 8:
        raise RuntimeError("empty glyph " + name)
    pad = 3
    y0, y1 = max(0, int(ys.min()) - pad), min(cream.shape[0], int(ys.max()) + 1 + pad)
    x0, x1 = max(0, int(xs.min()) - pad), min(cream.shape[1], int(xs.max()) + 1 + pad)
    gate = dilate(cream, 1)[y0:y1, x0:x1]
    cov = np.clip((lum[y0:y1, x0:x1] - 135.0) / 90.0, 0, 1) * al[y0:y1, x0:x1]
    crop = np.where(gate, cov, 0.0).astype(np.float32)
    d = cv2.distanceTransform((cream[y0:y1, x0:x1].astype(np.uint8) * 255), cv2.DIST_L2, 5)
    thick = float(np.median(d[d > 0])) if (d > 0).any() else 4.0
    sig = float(np.clip(0.12 * thick, 0.50, 0.95))
    crop = cv2.GaussianBlur(crop, (0, 0), sig)
    bh, bw = crop.shape
    up = 8
    return cv2.resize(crop, (bw * up, bh * up), interpolation=cv2.INTER_LANCZOS4)


def rotate_cov(g, ang):
    if abs(ang) < 0.2:
        return g
    h, w = g.shape
    pad = int(max(h, w) * 0.25) + 8
    canvas = np.pad(g.astype(np.float32), pad)
    H, W = canvas.shape
    M = cv2.getRotationMatrix2D((W / 2.0, H / 2.0), float(ang), 1.0)
    rot = cv2.warpAffine(canvas, M, (W, H), flags=cv2.INTER_LINEAR)
    ys, xs = np.where(rot > 0.04)
    if len(ys) < 8:
        return g
    return rot[int(ys.min()): int(ys.max()) + 1, int(xs.min()): int(xs.max()) + 1]


def tip_vec(inner):
    ys, xs = np.where(inner)
    cx = (float(xs.min()) + float(xs.max())) * 0.5
    cy = (float(ys.min()) + float(ys.max())) * 0.5
    d2 = (xs.astype(np.float32) - cx) ** 2 + (ys.astype(np.float32) - cy) ** 2
    i = int(d2.argmax())
    return float(xs[i] - cx), float(ys[i] - cy)


# frac = taille vs min(inner). Centre = centroid du plateau (partie centrale).
SLOT_FRAC = {1: 0.88, 2: 0.86, 3: 0.86, 4: 0.78, 5: 0.86, 6: 0.86}


def place_glyph(glyph, inner, wall, slot=1):
    ys, xs = np.where(inner)
    iw = int(xs.max() - xs.min()) + 1
    ih = int(ys.max() - ys.min()) + 1
    cx = float(xs.mean())
    cy = float(ys.mean())
    frac = SLOT_FRAC.get(int(slot), 0.86)
    gh, gw = glyph.shape
    scale = (frac * min(iw, ih)) / float(max(gh, gw))
    canvas = np.zeros(inner.shape, bool)
    for _ in range(12):
        nw = max(8, int(round(gw * scale)))
        nh = max(8, int(round(gh * scale)))
        g = cv2.resize(glyph.astype(np.float32), (nw, nh), interpolation=cv2.INTER_LANCZOS4)
        cnts, hier = cv2.findContours((g > 0.47).astype(np.uint8) * 255, cv2.RETR_CCOMP, cv2.CHAIN_APPROX_NONE)
        has_hole = hier is not None and any(int(hier[0][i][3]) >= 0 for i in range(len(cnts)))
        if has_hole:
            hard = g > 0.42
        else:
            g = cv2.GaussianBlur(g, (0, 0), 1.0)
            hard = g > 0.47
            hard = _smooth_mask(hard)
        g = hard
        gx0 = int(round(cx - nw / 2.0))
        gy0 = int(round(cy - nh / 2.0))
        canvas[:] = False
        y_a, y_b = max(0, gy0), min(inner.shape[0], gy0 + nh)
        x_a, x_b = max(0, gx0), min(inner.shape[1], gx0 + nw)
        gy_a, gx_a = y_a - gy0, x_a - gx0
        canvas[y_a:y_b, x_a:x_b] = g[gy_a:gy_a + (y_b - y_a), gx_a:gx_a + (x_b - x_a)]
        well = dilate(canvas, wall)
        if (well & ~inner).sum() < 40:
            return canvas
        scale *= 0.92
    return canvas


def shade_fill(fill, bevel):
    din = dist(fill)
    mx = float(din.max()) if fill.any() else 1.0
    bevel = max(6.0, min(bevel, 0.42 * mx))
    hmap = np.clip(din / max(bevel, 1e-3), 0, 1).astype(np.float32)
    gy, gx = np.gradient(hmap)
    nx, ny, nz = -gx, -gy, np.full_like(gx, 0.72)
    nlen = np.sqrt(nx * nx + ny * ny + nz * nz) + 1e-6
    lx, ly, lz = LIGHT
    ndot = np.clip((nx * lx + ny * ly + nz * lz) / nlen, 0, 1)
    t = ndot ** 0.75
    body = LO * (1.0 - t)[..., None] + HI * t[..., None]
    body = body * 0.12 + MID * 0.88
    return body, din


def carve(stone, glyph, wall, bevel, seed):
    out = stone.astype(np.float32)
    fill = glyph
    well = dilate(glyph, wall)
    body, din = shade_fill(fill, bevel)
    rng = np.random.RandomState(seed)
    body = np.clip(body + rng.normal(0, 3.2, body.shape), 0, 255)

    stone_a = stone[..., 3] > 40
    valid = well & stone_a

    walls = valid & ~fill
    if walls.any():
        dout = dist(~fill)
        wgy, wgx = np.gradient(dout)
        ys, xs = np.where(walls)
        wn = np.sqrt(wgx[ys, xs] ** 2 + wgy[ys, xs] ** 2 + 1e-6)
        wnx, wny = wgx[ys, xs] / wn, wgy[ys, xs] / wn
        lx, ly, _ = LIGHT
        wlit = np.clip(-wnx * lx - wny * ly + 0.22, 0, 1)
        from_g = dout[ys, xs]
        inner = np.clip(1.0 - (from_g / (wall * 0.72)), 0, 1) ** 0.62
        wall_col = MID * (0.55 + 0.45 * wlit)[:, None]
        mix = np.clip(0.42 + 0.38 * inner, 0, 1)
        base = out[ys, xs, :3]
        out[ys, xs, :3] = base * (1 - mix[:, None]) + wall_col * mix[:, None]

    fa = (cv2.GaussianBlur(fill.astype(np.uint8) * 255, (0, 0), 0.7).astype(np.float32) / 255.0)
    hit = fa > 0.05
    if hit.any():
        a = fa[hit][:, None]
        out[hit, :3] = out[hit, :3] * (1 - a) + body[hit] * a
        out[hit, 3] = np.maximum(out[hit, 3], a[:, 0] * 255)
    return np.clip(out, 0, 255).astype(np.uint8)


def inner_mask(arr):
    stone = arr[..., 3] > 40
    d = dist(stone)
    return d > (BEVEL + INNER_MARGIN)


def load_glyphs(names):
    glyphs = {}
    for name in names:
        glyphs[name] = extract_glyph(name)
        print("glyph", name, glyphs[name].shape)
    return glyphs


def carve_all(only=None, slots=None):
    os.makedirs(OUT_DIR, exist_ok=True)
    names = SETS if only is None else list(only)
    slots = list(range(1, 7)) if slots is None else list(slots)
    glyphs = load_glyphs(names)
    blanks = {}
    for slot in slots:
        blank = np.array(Image.open(os.path.join(BLANK_DIR, "croquis-blank-slot%d.png" % slot)).convert("RGBA"))
        inner = inner_mask(blank)
        stone = blank[..., 3] > 40
        iy, ix = np.where(inner)
        wall = max(8, int(round(min(ix.max() - ix.min(), iy.max() - iy.min()) * 0.016)))
        letter_bevel = max(7, int(round(wall * 0.55)))
        blanks[slot] = (blank, inner, wall, letter_bevel)
    for slot in slots:
        print("slot", slot)
        blank, inner, wall, letter_bevel = blanks[slot]
        for i, name in enumerate(names):
            w = wall
            bev = letter_bevel
            if name in ("guard", "shield"):
                w = max(5, int(round(wall * 0.55)))
                bev = max(4, int(round(w * 0.50)))
            gmask = place_glyph(glyphs[name], inner, w, slot=slot)
            out = carve(blank, gmask, w, bev, seed=11 + i * 17 + slot)
            dest = os.path.join(OUT_DIR, "%s-slot%d-legendaire.png" % (name, slot))
            Image.fromarray(out, "RGBA").save(dest)
            print("carved", name, "slot", slot, "wall", w)


if __name__ == "__main__":
    import sys
    args = sys.argv[1:]
    if not args:
        carve_all()
    elif args[0] == "one":
        name = args[1] if len(args) > 1 else "violent"
        slot = int(args[2]) if len(args) > 2 else 1
        carve_all(only=[name], slots=[slot])
    else:
        carve_all(only=args)

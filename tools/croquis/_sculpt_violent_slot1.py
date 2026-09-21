from PIL import Image, ImageFilter, ImageDraw
import numpy as np, os

BASE = r"C:\Users\Great-Lucky\Documents\Rune_Manager_Modern\Donnees\assets"
SRC = os.path.join(BASE, "croquis-rune-3d-slot1.png")
GLYPH = os.path.join(BASE, "sets", "violent.png")
OUT = os.path.join(BASE, "croquis-violent-slot1-legendaire.png")
DESK = r"C:\Users\Great-Lucky\Desktop\croquis-violent-slot1-legendaire.png"

HI = np.array([255, 214, 92], np.float32)
MID = np.array([255, 158, 28], np.float32)
LO = np.array([168, 42, 8], np.float32)
WALL = np.array([255, 78, 12], np.float32)
WALL_DK = np.array([120, 28, 6], np.float32)

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

def dilate(mask, r):
    if r <= 0:
        return mask.copy()
    img = Image.fromarray((mask.astype(np.uint8) * 255), "L")
    img = img.filter(ImageFilter.MaxFilter(r * 2 + 1))
    return np.array(img) > 127

def erode(mask, r):
    if r <= 0:
        return mask.copy()
    img = Image.fromarray((mask.astype(np.uint8) * 255), "L")
    img = img.filter(ImageFilter.MinFilter(r * 2 + 1))
    return np.array(img) > 127

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

def s_bbox(arr):
    r, g, b, a = arr[..., 0].astype(np.int16), arr[..., 1].astype(np.int16), arr[..., 2].astype(np.int16), arr[..., 3]
    core = (a >= 80) & (r >= 200) & (r >= g + 25) & (b <= 90)
    h, w = core.shape
    seen = np.zeros_like(core)
    comps = []
    for y in range(h):
        for x in range(w):
            if not core[y, x] or seen[y, x]:
                continue
            stack = [(x, y)]
            seen[y, x] = True
            xs, ys = [x], [y]
            n = 0
            while stack:
                cx, cy = stack.pop()
                n += 1
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = cx + dx, cy + dy
                    if 0 <= nx < w and 0 <= ny < h and core[ny, nx] and not seen[ny, nx]:
                        seen[ny, nx] = True
                        stack.append((nx, ny))
                        xs.append(nx)
                        ys.append(ny)
            comps.append((n, min(xs), min(ys), max(xs), max(ys)))
    comps.sort(reverse=True)
    keep = [c for c in comps if c[0] >= comps[0][0] * 0.22][:4]
    return min(c[1] for c in keep), min(c[2] for c in keep), max(c[3] for c in keep) + 1, max(c[4] for c in keep) + 1

def _blur_l(ch, r):
    im = Image.fromarray(np.clip(ch, 0, 255).astype(np.uint8), "L")
    return np.array(im.filter(ImageFilter.GaussianBlur(r))).astype(np.float32)


def restore_plateau(arr, hole, plateau):
    """Grow inner-face charcoal into the S zone. Do not sample the old well."""
    out = arr.astype(np.float32)
    known = plateau.copy()
    if not known.any():
        known = (arr[..., 3] > 80) & ~hole
    h = hole.copy()
    for _ in range(70):
        ring = dilate(known, 2) & h & ~known
        if not ring.any():
            break
        m = known.astype(np.float32)
        rgb_m = out[..., :3] * m[..., None]
        bm = _blur_l(m * 255.0, 1.6) / 255.0
        acc = np.stack([_blur_l(rgb_m[..., c], 1.6) for c in range(3)], -1)
        fill = acc / np.maximum(bm[..., None], 0.05)
        out[ring, :3] = fill[ring]
        out[ring, 3] = 255
        known |= ring
    still = h & ~known
    if still.any():
        med = np.median(arr[plateau][:, :3].astype(np.float32), axis=0) if plateau.any() else np.array([76.0, 77.0, 78.0])
        out[still, :3] = med
        out[still, 3] = 255
    return np.clip(out, 0, 255).astype(np.uint8)

def extract_b(path, size):
    im = Image.open(path).convert("RGBA")
    im = im.resize((im.width * 16, im.height * 16), Image.Resampling.LANCZOS)
    a = np.array(im)
    rgb = a[..., :3].astype(np.float32)
    lum = rgb.mean(2)
    sat = rgb.max(2) - rgb.min(2)
    core = (a[..., 3] > 80) & (lum > 148) & (sat < 95)
    core = fill_holes(core)
    ys, xs = np.where(core)
    pad = 16
    y0, y1 = max(0, ys.min() - pad), min(core.shape[0], ys.max() + 1 + pad)
    x0, x1 = max(0, xs.min() - pad), min(core.shape[1], xs.max() + 1 + pad)
    crop = Image.fromarray((core[y0:y1, x0:x1].astype(np.uint8) * 255), "L")
    ch, cw = crop.size[1], crop.size[0]
    scale = size / float(max(ch, cw))
    nw, nh = max(8, int(round(cw * scale))), max(8, int(round(ch * scale)))
    crop = crop.resize((nw, nh), Image.Resampling.LANCZOS).filter(ImageFilter.GaussianBlur(1.6))
    arr = np.array(crop)
    hard = fill_holes(arr > 90)
    # slight smooth silhouette
    hard = erode(dilate(hard, 1), 1)
    alpha = np.array(Image.fromarray((hard.astype(np.uint8) * 255), "L").filter(ImageFilter.GaussianBlur(1.1))).astype(np.float32) / 255.0
    return hard, alpha

def carve_set_creux(stone, glyph, alpha, y0, x0, wall=16):
    """Creux = dilated glyph. Walls orange-red. Fill = 3D set logo. Not the Despair S well."""
    h, w = glyph.shape
    # fill slightly inset so the B-shaped orange wall stays visible (like Despair S)
    fill = erode(glyph, 2)
    fill_a = alpha.copy()
    fill_a[~dilate(fill, 1)] *= 0.0
    fill_a = np.array(Image.fromarray((np.clip(fill_a, 0, 1) * 255).astype(np.uint8), "L").filter(ImageFilter.GaussianBlur(0.8))).astype(np.float32) / 255.0
    dist_in = edt(fill)
    well = dilate(glyph, wall)
    dist_well = edt(well)
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

    # thin dark lip only, then B-shaped orange walls (no dark blob around)
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
    a = fill_a[ys, xs][:, None]
    out[yys, xxs, :3] = out[yys, xxs, :3] * (1 - a) + body[ys, xs] * a
    out[yys, xxs, 3] = np.maximum(out[yys, xxs, 3], a[:, 0] * 255)
    return np.clip(out, 0, 255).astype(np.uint8)

def main():
    arr = np.array(Image.open(SRC).convert("RGBA"))
    x0, y0, x1, y1 = s_bbox(arr)
    sw, sh = x1 - x0, y1 - y0
    cx, cy = (x0 + x1) * 0.5, (y0 + y1) * 0.5
    print("S", x0, y0, sw, sh)
    r, g, b, a = arr[..., 0].astype(np.int16), arr[..., 1].astype(np.int16), arr[..., 2].astype(np.int16), arr[..., 3]
    orange = (a >= 40) & (r >= 145) & (r >= g + 10) & (b <= 140) & (r >= b + 18)
    lum = arr[..., :3].astype(np.float32).mean(2)
    face = erode(a >= 80, 14)
    s_region = np.zeros(orange.shape, bool)
    pad = 12
    s_region[max(0, y0 - pad):min(arr.shape[0], y1 + pad), max(0, x0 - pad):min(arr.shape[1], x1 + pad)] = True
    plateau = face & ~s_region & (lum >= 70) & (lum <= 115)
    hole = s_region & face
    print("hole px", int(hole.sum()), "plat", int(plateau.sum()))
    blank = restore_plateau(arr, hole, plateau)
    # leftover warm pixels only in old S area -> charcoal
    leftover = (blank[..., 0].astype(np.int16) > blank[..., 1].astype(np.int16) + 12) & (blank[..., 0].astype(np.int16) > blank[..., 2].astype(np.int16) + 16) & hole & (blank[..., 0] > 110)
    gray = (blank[..., 0].astype(np.float32) + blank[..., 1] + blank[..., 2]) / 3.0
    blank[..., 0][leftover] = np.clip(gray[leftover] + 3, 0, 255).astype(np.uint8)
    blank[..., 1][leftover] = np.clip(gray[leftover] + 1, 0, 255).astype(np.uint8)
    blank[..., 2][leftover] = np.clip(gray[leftover] + 2, 0, 255).astype(np.uint8)

    target = int(max(sw, sh) * 0.90)
    glyph, alpha = extract_b(GLYPH, target)
    mh, mw = glyph.shape
    gx0 = int(round(cx - mw / 2.0))
    gy0 = int(round(cy - mh / 2.0))
    print("B creux", mw, mh, "at", gx0, gy0)
    out = carve_set_creux(blank, glyph, alpha, gy0, gx0, wall=12)
    im = Image.fromarray(out, "RGBA")
    im.save(OUT)
    im.save(DESK)
    print("saved", OUT, DESK)

if __name__ == "__main__":
    main()

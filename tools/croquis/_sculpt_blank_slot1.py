"""Slot blanks: same 3D as in-game (faceted chamfer tablet). Charcoal. No set. No orange."""
import os
import cv2
import numpy as np
from PIL import Image

BASE = r"C:\Users\Great-Lucky\Documents\Rune_Manager_Modern\Donnees\assets"
SRC = os.path.join(BASE, "croquis-rune-3d-slot1.png")
OUT = os.path.join(BASE, "croquis", "blanks", "croquis-blank-slot1.png")

# in-game light: top-left
LIGHT = np.array([-0.42, -0.78, 0.46], np.float32)
LIGHT /= np.linalg.norm(LIGHT)

DARK = np.array([38.0, 39.0, 41.0], np.float32)
MID = np.array([78.0, 79.0, 81.0], np.float32)
LIT = np.array([132.0, 131.0, 128.0], np.float32)


def hull_verts(src_path):
    im = np.array(Image.open(src_path).convert("RGBA"))
    H, W = im.shape[:2]
    stone = im[..., 3] > 40
    cnt, _ = cv2.findContours(stone.astype(np.uint8) * 255, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    c = max(cnt, key=cv2.contourArea)
    peri = cv2.arcLength(c, True)
    best = None
    for e in (0.008, 0.01, 0.012, 0.015, 0.02, 0.025, 0.03):
        approx = cv2.approxPolyDP(c, e * peri, True).reshape(-1, 2).astype(np.float32)
        n = len(approx)
        if 4 <= n <= 6:
            best = approx
            if n == 5:
                break
    if best is None:
        best = cv2.approxPolyDP(c, 0.02 * peri, True).reshape(-1, 2).astype(np.float32)
    return W, H, best


def raster_poly(verts, W, H):
    m = np.zeros((H, W), np.uint8)
    pts = np.round(verts).astype(np.int32).reshape(-1, 1, 2)
    cv2.fillPoly(m, [pts], 255)
    return m > 0


def edge_faces(verts):
    n = len(verts)
    faces = []
    for i in range(n):
        a = verts[i]
        b = verts[(i + 1) % n]
        d = b - a
        ln = float(np.hypot(d[0], d[1])) + 1e-6
        # clockwise pentagon -> outward
        nxy = np.array([d[1] / ln, -d[0] / ln], np.float32)
        # point a test vertex away from centroid to confirm outward
        faces.append((a, b, nxy, ln))
    return faces


def assign_face(ys, xs, faces):
    # nearest edge (point-to-segment)
    best = np.zeros(len(ys), np.int32)
    bestd = np.full(len(ys), 1e9, np.float32)
    p = np.stack([xs, ys], 1).astype(np.float32)
    for i, (a, b, _, _) in enumerate(faces):
        ab = b - a
        t = np.clip(((p - a) * ab).sum(1) / (np.dot(ab, ab) + 1e-6), 0, 1)
        proj = a + t[:, None] * ab
        dist = np.hypot(p[:, 0] - proj[:, 0], p[:, 1] - proj[:, 1])
        hit = dist < bestd
        best[hit] = i
        bestd[hit] = dist[hit]
    return best


def render_slot(slot):
    src = os.path.join(BASE, "croquis-rune-3d-slot%d.png" % slot)
    out_path = os.path.join(BASE, "croquis", "blanks", "croquis-blank-slot%d.png" % slot)
    W, H, verts = hull_verts(src)
    c = verts.mean(0)
    faces = edge_faces(verts)
    for i, (a, b, nxy, ln) in enumerate(faces):
        mid = (a + b) * 0.5
        if np.dot(nxy, mid - c) < 0:
            faces[i] = (a, b, -nxy, ln)

    stone = raster_poly(verts, W, H)
    dist = cv2.distanceTransform((stone.astype(np.uint8) * 255), cv2.DIST_L2, 5)
    bevel = 43.0
    chamfer = stone & (dist < bevel)
    inner = stone & (dist >= bevel)

    ys, xs = np.where(chamfer)
    fid = assign_face(ys, xs, faces)

    nx = np.zeros((H, W), np.float32)
    ny = np.zeros((H, W), np.float32)
    nz = np.zeros((H, W), np.float32)
    nz[inner] = 1.0
    iy, ix = np.where(inner)
    ic = np.array([c[0], c[1]], np.float32)
    vx = (ix - ic[0]) / (W * 0.35)
    vy = (iy - ic[1]) / (H * 0.35)
    nx[iy, ix] = np.clip(vx * 0.12, -0.2, 0.2)
    ny[iy, ix] = np.clip(vy * 0.12, -0.2, 0.2)
    nzi = np.sqrt(np.clip(1.0 - nx[iy, ix] ** 2 - ny[iy, ix] ** 2, 0.4, 1))
    nz[iy, ix] = nzi

    slope = 0.58
    for i, (_, _, nxy, _) in enumerate(faces):
        m = fid == i
        if not m.any():
            continue
        yy, xx = ys[m], xs[m]
        nx[yy, xx] = nxy[0] * slope
        ny[yy, xx] = nxy[1] * slope
        nz[yy, xx] = np.sqrt(max(1.0 - slope * slope, 0.12))

    nlen = np.sqrt(nx * nx + ny * ny + nz * nz + 1e-8)
    nx, ny, nz = nx / nlen, ny / nlen, nz / nlen
    ndot = np.clip(nx * LIGHT[0] + ny * LIGHT[1] + nz * LIGHT[2], 0, 1)
    shade = 0.16 + 0.92 * (ndot ** 1.05)

    col = DARK[None, None, :] * (1 - shade)[..., None] + LIT[None, None, :] * shade[..., None]
    col = col * 0.42 + MID[None, None, :] * 0.58
    col[inner] = col[inner] * 0.55 + (DARK * 0.45 + MID * 0.55) * 0.45
    col[inner] = col[inner] * (0.88 + 0.18 * ndot[inner][:, None])

    rng = np.random.RandomState(12 + slot)
    grain = rng.normal(0, 1, (H, W)).astype(np.float32)
    grain = cv2.GaussianBlur(grain, (0, 0), 0.7)
    col = col + grain[..., None] * np.where(inner[..., None], 2.1, 2.8)

    lip = stone & (dist < 2.5)
    col[lip] *= 0.62
    crease = inner & (dist < bevel + 2.0)
    w = np.clip((bevel + 2.0 - dist) / 2.0, 0, 1)
    col[crease] *= (1.0 - 0.18 * w[crease])[:, None]
    col[stone] *= (0.94 + 0.06 * ndot[stone])[:, None]

    col = np.clip(col * 0.92, 0, 255)
    hull = cv2.GaussianBlur((stone.astype(np.uint8) * 255), (0, 0), 0.6)
    alpha = hull.astype(np.float32) / 255.0
    out = np.zeros((H, W, 4), np.uint8)
    out[..., :3] = (col * alpha[..., None]).astype(np.uint8)
    out[..., 3] = np.clip(hull, 0, 255)
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    Image.fromarray(out, "RGBA").save(out_path)
    print("slot", slot, "verts", len(verts), "saved", out_path)


if __name__ == "__main__":
    for s in range(1, 7):
        render_slot(s)

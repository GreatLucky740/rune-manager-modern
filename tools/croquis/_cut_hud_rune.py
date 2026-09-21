"""Croquis: HUD Energy slot 1 et 2 coupes au liseré noir. Sans etoiles ni +15."""
import os
import cv2
import numpy as np

OUT = r"C:\Users\Great-Lucky\Documents\Rune_Manager_Modern\Donnees\assets\croquis\hud-energy"
SCALE = 8

# verts x8 sur le liseré noir EXTERIEUR (grille green 80px)
JOBS = [
    (
        1,
        r"C:\Users\Great-Lucky\Documents\Set de rune SW\Energy\{C01B4CEE-216B-4E10-96DB-73524AB2445F}.png",
        [(364, 228), (140, 448), (364, 680), (588, 448)],
    ),
    (
        2,
        r"C:\Users\Great-Lucky\Documents\Set de rune SW\Energy\{3051EC63-95A6-4055-966C-F43618271433}.png",
        [(208, 244), (108, 456), (192, 608), (464, 608), (600, 248)],
    ),
]


def punch_stars_and_plus(up, mask):
    hsv = cv2.cvtColor(up, cv2.COLOR_BGR2HSV)
    H, S, V = hsv[:, :, 0], hsv[:, :, 1], hsv[:, :, 2]
    mag = ((H >= 135) & (H <= 175) & (S >= 70) & (V >= 60)).astype(np.uint8) * 255
    white = ((V >= 220) & (S <= 90)).astype(np.uint8) * 255
    mag = cv2.dilate(mag, np.ones((7, 7), np.uint8), iterations=1)
    white_bin = (white > 0).astype(np.uint8)
    plus_box = np.zeros_like(white_bin)
    if white_bin.any():
        num, _labels, stats, _ = cv2.connectedComponentsWithStats(white_bin, 8)
        Himg = up.shape[0]
        best = -1
        best_area = 0
        for i in range(1, num):
            _x, y, _bw, _bh, area = stats[i]
            if y > Himg * 0.45 and area > best_area:
                best_area = area
                best = i
        if best > 0:
            x, y, bw, bh, _area = stats[best]
            padn = 8
            plus_box[
                max(0, y - padn) : y + bh + padn,
                max(0, x - padn) : x + bw + padn,
            ] = 255
    top = np.zeros_like(mag)
    top[: int(up.shape[0] * 0.38)] = 255
    mag = cv2.bitwise_and(mag, top)
    drop = cv2.bitwise_or(mag, plus_box)
    keep = mask.copy()
    keep[drop > 0] = 0
    dt = cv2.distanceTransform(keep, cv2.DIST_L2, 5)
    gold_edge = (keep > 0) & (dt < 14) & (V >= 145) & (S >= 100) & (H <= 28)
    keep[gold_edge] = 0
    return keep


def cut(path, verts8):
    bgr = cv2.imread(path)
    h, w = bgr.shape[:2]
    up = cv2.resize(bgr, (w * SCALE, h * SCALE), interpolation=cv2.INTER_NEAREST)
    mask = np.zeros(up.shape[:2], np.uint8)
    cv2.fillPoly(mask, [np.array(verts8, np.int32)], 255)
    keep = punch_stars_and_plus(up, mask)
    rgba = cv2.cvtColor(up, cv2.COLOR_BGR2BGRA)
    rgba[keep == 0, 3] = 0
    ys, xs = np.where(keep > 0)
    crop = rgba[ys.min() : ys.max() + 1, xs.min() : xs.max() + 1]
    debug = up.copy()
    cv2.polylines(debug, [np.array(verts8, np.int32)], True, (0, 255, 255), 2, cv2.LINE_AA)
    return crop, debug


os.makedirs(OUT, exist_ok=True)
crops = {}
for slot, path, verts in JOBS:
    crop, debug = cut(path, verts)
    crops[slot] = crop
    cv2.imwrite(os.path.join(OUT, "cut-energy-slot%d.png" % slot), crop)
    cv2.imwrite(os.path.join(OUT, "cut-energy-slot%d-debug.png" % slot), debug)
    print("slot", slot, crop.shape)

c1, c2 = crops[1], crops[2]
pad = 48
H = max(c1.shape[0], c2.shape[0]) + pad * 2
W = c1.shape[1] + c2.shape[1] + pad * 3
canvas = np.zeros((H, W, 4), np.uint8)
canvas[:, :, 3] = 255


def blit(dst, src, x, y):
    h, w = src.shape[:2]
    roi = dst[y : y + h, x : x + w]
    a = src[:, :, 3:4].astype(np.float32) / 255.0
    roi[:, :, :3] = (src[:, :, :3] * a).astype(np.uint8)
    roi[:, :, 3] = 255


blit(canvas, c1, pad, pad + (H - 2 * pad - c1.shape[0]) // 2)
blit(canvas, c2, pad * 2 + c1.shape[1], pad + (H - 2 * pad - c2.shape[0]) // 2)
cv2.imwrite(os.path.join(OUT, "croquis-cut-energy-slot1-2.png"), canvas)
print("croquis", canvas.shape)

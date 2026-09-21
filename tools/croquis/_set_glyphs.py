"""Logos set vectoriels, formes in-game. Masque 0/1 recadre."""
import cv2
import numpy as np

S = 900
AA = cv2.LINE_AA


def _blank():
    return np.zeros((S, S), np.uint8)


def _p(x, y):
    return (int(round(x * S)), int(round(y * S)))


def _r(v):
    return max(2, int(round(v * S)))


def _pts(seq):
    return np.array([_p(x, y) for x, y in seq], np.int32)


def capsule(m, a, b, rad):
    p1, p2 = _p(*a), _p(*b)
    rr = _r(rad)
    cv2.line(m, p1, p2, 255, rr * 2, AA)
    cv2.circle(m, p1, rr, 255, -1, AA)
    cv2.circle(m, p2, rr, 255, -1, AA)


def poly(m, seq, color=255):
    cv2.fillPoly(m, [_pts(seq)], color, AA)


def stroke_poly(m, seq, rad, closed=False):
    pts = _pts(seq)
    rr = _r(rad)
    cv2.polylines(m, [pts], closed, 255, rr * 2, AA)
    for x, y in pts:
        cv2.circle(m, (int(x), int(y)), rr, 255, -1, AA)


def round_rect(m, x0, y0, x1, y1, rad, color=255):
    a, b = _p(x0, y0), _p(x1, y1)
    r = _r(rad)
    x0i, y0i = a
    x1i, y1i = b
    if x1i < x0i:
        x0i, x1i = x1i, x0i
    if y1i < y0i:
        y0i, y1i = y1i, y0i
    r = min(r, (x1i - x0i) // 2, (y1i - y0i) // 2)
    cv2.rectangle(m, (x0i + r, y0i), (x1i - r, y1i), color, -1)
    cv2.rectangle(m, (x0i, y0i + r), (x1i, y1i - r), color, -1)
    cv2.circle(m, (x0i + r, y0i + r), r, color, -1, AA)
    cv2.circle(m, (x1i - r, y0i + r), r, color, -1, AA)
    cv2.circle(m, (x0i + r, y1i - r), r, color, -1, AA)
    cv2.circle(m, (x1i - r, y1i - r), r, color, -1, AA)


def ellipse_fill(m, cx, cy, ax, ay, color=255):
    cv2.ellipse(m, _p(cx, cy), (_r(ax), _r(ay)), 0, 0, 360, color, -1, AA)


def crop(m):
    m = cv2.GaussianBlur(m, (0, 0), 1.15)
    hard = m > 88
    ys, xs = np.where(hard)
    if len(ys) < 20:
        raise RuntimeError("empty glyph")
    pad = 18
    y0, y1 = max(0, ys.min() - pad), min(S, ys.max() + 1 + pad)
    x0, x1 = max(0, xs.min() - pad), min(S, xs.max() + 1 + pad)
    return hard[y0:y1, x0:x1]


def energy():
    m = _blank()
    w = 0.095
    stroke_poly(m, [(0.84, 0.18), (0.20, 0.18), (0.58, 0.50), (0.20, 0.82), (0.84, 0.82)], w)
    return crop(m)


def guard():
    m = _blank()
    round_rect(m, 0.16, 0.16, 0.84, 0.84, 0.10, 255)
    round_rect(m, 0.36, 0.36, 0.64, 0.64, 0.055, 0)
    return crop(m)


def swift():
    m = _blank()
    xs = np.linspace(0.14, 0.86, 56)
    t = (xs - 0.14) / 0.72
    y1 = 0.36 + 0.07 * np.sin(2.0 * np.pi * t)
    y2 = 0.64 + 0.07 * np.sin(2.0 * np.pi * t)
    stroke_poly(m, list(zip(xs, y1)), 0.052)
    stroke_poly(m, list(zip(xs, y2)), 0.052)
    return crop(m)


def blade():
    m = _blank()
    r = 0.062
    capsule(m, (0.32, 0.14), (0.38, 0.86), r)
    capsule(m, (0.62, 0.14), (0.68, 0.86), r)
    capsule(m, (0.16, 0.34), (0.84, 0.40), 0.055)
    capsule(m, (0.16, 0.60), (0.84, 0.66), 0.055)
    return crop(m)


def rage():
    m = _blank()
    poly(m, [(0.50, 0.10), (0.90, 0.50), (0.50, 0.90), (0.10, 0.50)])
    poly(m, [(0.50, 0.32), (0.68, 0.50), (0.50, 0.68), (0.32, 0.50)], 0)
    return crop(m)


def focus():
    m = _blank()
    ellipse_fill(m, 0.50, 0.50, 0.34, 0.34)
    ellipse_fill(m, 0.50, 0.50, 0.16, 0.16, 0)
    return crop(m)


def endure():
    m = _blank()
    r = 0.055
    capsule(m, (0.22, 0.34), (0.66, 0.78), r)
    capsule(m, (0.34, 0.22), (0.78, 0.66), r)
    capsule(m, (0.22, 0.66), (0.66, 0.22), r)
    capsule(m, (0.34, 0.78), (0.78, 0.34), r)
    return crop(m)


def fatal():
    m = _blank()
    poly(m, [
        (0.08, 0.88), (0.32, 0.88), (0.50, 0.46), (0.68, 0.88), (0.92, 0.88),
        (0.58, 0.12), (0.42, 0.12),
    ])
    poly(m, [(0.50, 0.26), (0.40, 0.50), (0.60, 0.50)], 0)
    return crop(m)


def despair():
    m = _blank()
    stroke_poly(m, [
        (0.80, 0.16), (0.26, 0.20), (0.26, 0.40),
        (0.74, 0.60), (0.74, 0.82), (0.20, 0.86),
    ], 0.100)
    return crop(m)


def vampire():
    m = _blank()
    poly(m, [(0.18, 0.12), (0.82, 0.12), (0.50, 0.50)])
    poly(m, [(0.18, 0.88), (0.82, 0.88), (0.50, 0.50)])
    poly(m, [(0.40, 0.20), (0.60, 0.20), (0.50, 0.38)], 0)
    poly(m, [(0.40, 0.80), (0.60, 0.80), (0.50, 0.62)], 0)
    return crop(m)


def violent():
    m = _blank()
    round_rect(m, 0.16, 0.12, 0.42, 0.88, 0.035)
    tmp = _blank()
    ellipse_fill(tmp, 0.50, 0.32, 0.32, 0.22)
    ellipse_fill(tmp, 0.52, 0.68, 0.34, 0.24)
    tmp[:, :_p(0.34, 0)[0]] = 0
    m = np.maximum(m, tmp)
    holes = _blank()
    ellipse_fill(holes, 0.50, 0.32, 0.13, 0.09)
    ellipse_fill(holes, 0.54, 0.68, 0.14, 0.10)
    m[holes > 0] = 0
    round_rect(m, 0.16, 0.12, 0.40, 0.88, 0.035)
    return crop(m)


def nemesis():
    m = _blank()
    poly(m, [(0.16, 0.14), (0.88, 0.50), (0.16, 0.86)])
    return crop(m)


def will():
    m = _blank()
    r = 0.078
    capsule(m, (0.50, 0.40), (0.50, 0.88), r)
    capsule(m, (0.50, 0.42), (0.16, 0.14), r)
    capsule(m, (0.50, 0.42), (0.84, 0.14), r)
    return crop(m)


def shield():
    m = _blank()
    poly(m, [(0.50, 0.10), (0.88, 0.50), (0.50, 0.90), (0.12, 0.50)])
    poly(m, [(0.50, 0.32), (0.68, 0.50), (0.50, 0.68), (0.32, 0.50)], 0)
    return crop(m)


def revenge():
    m = _blank()
    r = 0.082
    capsule(m, (0.20, 0.16), (0.80, 0.84), r)
    capsule(m, (0.80, 0.16), (0.20, 0.84), r)
    poly(m, [(0.50, 0.34), (0.66, 0.50), (0.50, 0.66), (0.34, 0.50)])
    return crop(m)


def destroy():
    m = _blank()
    stroke_poly(m, [(0.10, 0.22), (0.28, 0.84), (0.50, 0.34), (0.72, 0.84), (0.90, 0.22)], 0.078)
    return crop(m)


def fight():
    m = _blank()
    w = 0.070
    stroke_poly(m, [
        (0.26, 0.22), (0.52, 0.12), (0.78, 0.24), (0.78, 0.40),
        (0.48, 0.50), (0.78, 0.60), (0.78, 0.76), (0.52, 0.88), (0.26, 0.78),
    ], w)
    return crop(m)


def determination():
    m = _blank()
    r = 0.062
    capsule(m, (0.20, 0.12), (0.20, 0.88), r)
    capsule(m, (0.80, 0.12), (0.80, 0.88), r)
    capsule(m, (0.20, 0.14), (0.80, 0.50), r)
    capsule(m, (0.80, 0.14), (0.20, 0.50), r)
    poly(m, [(0.50, 0.22), (0.64, 0.38), (0.50, 0.50), (0.36, 0.38)], 0)
    return crop(m)


def enhance():
    m = _blank()
    r = 0.078
    capsule(m, (0.18, 0.26), (0.82, 0.26), r)
    capsule(m, (0.28, 0.26), (0.28, 0.86), r)
    capsule(m, (0.72, 0.26), (0.72, 0.86), r)
    return crop(m)


def accuracy():
    m = _blank()
    poly(m, [
        (0.50, 0.12), (0.82, 0.34), (0.82, 0.66),
        (0.50, 0.88), (0.18, 0.66), (0.18, 0.34),
    ])
    poly(m, [(0.50, 0.32), (0.68, 0.50), (0.50, 0.68), (0.32, 0.50)], 0)
    return crop(m)


def tolerance():
    m = _blank()
    poly(m, [
        (0.50, 0.10), (0.86, 0.30), (0.86, 0.70),
        (0.50, 0.90), (0.14, 0.70), (0.14, 0.30),
    ])
    poly(m, [(0.50, 0.34), (0.68, 0.50), (0.50, 0.66), (0.32, 0.50)], 0)
    capsule(m, (0.22, 0.50), (0.78, 0.50), 0.055)
    return crop(m)


def seal():
    m = _blank()
    ellipse_fill(m, 0.50, 0.50, 0.30, 0.38)
    ellipse_fill(m, 0.50, 0.50, 0.14, 0.20, 0)
    capsule(m, (0.50, 0.08), (0.50, 0.92), 0.055)
    return crop(m)


def intangible():
    m = _blank()
    r = 0.090
    capsule(m, (0.18, 0.16), (0.82, 0.84), r)
    capsule(m, (0.82, 0.16), (0.18, 0.84), r)
    return crop(m)


GLYPHS = {
    "energy": energy,
    "guard": guard,
    "swift": swift,
    "blade": blade,
    "rage": rage,
    "focus": focus,
    "endure": endure,
    "fatal": fatal,
    "despair": despair,
    "vampire": vampire,
    "violent": violent,
    "nemesis": nemesis,
    "will": will,
    "shield": shield,
    "revenge": revenge,
    "destroy": destroy,
    "fight": fight,
    "determination": determination,
    "enhance": enhance,
    "accuracy": accuracy,
    "tolerance": tolerance,
    "seal": seal,
    "intangible": intangible,
}


def make(name):
    return GLYPHS[name]()

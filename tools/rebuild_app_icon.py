from PIL import Image
import struct, io, os, shutil

SRC = r"C:\Users\Great-Lucky\Documents\Rune_Manager_Modern\Donnees\assets\app_icon.png"
OUT_ICO = r"C:\Users\Great-Lucky\Documents\Codex\2026-08-09\referenced-chatgpt-conversation-this-is-an-2\rune_manager_app\assets\app_icon.ico"
DON_ICO = r"C:\Users\Great-Lucky\Documents\Rune_Manager_Modern\Donnees\assets\app_icon.ico"

def content_bbox(im):
    px = im.load()
    w, h = im.size
    minx, miny, maxx, maxy = w, h, -1, -1
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a < 30 or r + g + b < 45:
                continue
            if x < minx: minx = x
            if y < miny: miny = y
            if x > maxx: maxx = x
            if y > maxy: maxy = y
    if maxx < 0:
        return im.getbbox() or (0, 0, w, h)
    return (minx, miny, maxx + 1, maxy + 1)

def fit(im, size):
    # Keep the full slot-1 diamond. Small sizes fill the canvas a bit more.
    fill = 1.0 if size <= 48 else 0.96
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    crop = im.crop(content_bbox(im))
    max_side = max(1, int(round(size * fill)))
    scale = min(max_side / float(crop.width), max_side / float(crop.height))
    nw = max(1, int(round(crop.width * scale)))
    nh = max(1, int(round(crop.height * scale)))
    resized = crop.resize((nw, nh), Image.Resampling.LANCZOS)
    canvas.paste(resized, ((size - nw) // 2, (size - nh) // 2), resized)
    return canvas

def save_ico(path, images):
    blobs = []
    for im in images:
        bio = io.BytesIO()
        im.save(bio, format="PNG")
        blobs.append(bio.getvalue())
    count = len(images)
    header = struct.pack("<HHH", 0, 1, count)
    offset = 6 + 16 * count
    entries = b""
    for im, blob in zip(images, blobs):
        w = 0 if im.width >= 256 else im.width
        h = 0 if im.height >= 256 else im.height
        entries += struct.pack("<BBBBHHII", w, h, 0, 0, 1, 32, len(blob), offset)
        offset += len(blob)
    with open(path, "wb") as f:
        f.write(header + entries)
        for b in blobs:
            f.write(b)

src = Image.open(SRC).convert("RGBA")
print("bbox", content_bbox(src), "src", src.size)
sizes = [16, 20, 24, 32, 40, 48, 64, 128, 256]
imgs = [fit(src, s) for s in sizes]
save_ico(OUT_ICO, imgs)
shutil.copy2(OUT_ICO, DON_ICO)
print("wrote", OUT_ICO, os.path.getsize(OUT_ICO))

from pathlib import Path
import json
import sys

import numpy as np
from PIL import Image


def feature(image):
    image = image.resize((64, 64), Image.Resampling.LANCZOS).convert("RGB")
    data = np.asarray(image, dtype=np.float32) / 255.0
    # Stars/checkmarks cover the top and the level covers the bottom-right.
    data[:18] = 0
    data[51:, 44:] = 0
    return data


def hungarian(cost):
    n, m = cost.shape
    u = np.zeros(n + 1); v = np.zeros(m + 1)
    p = np.zeros(m + 1, dtype=int); way = np.zeros(m + 1, dtype=int)
    for i in range(1, n + 1):
        p[0] = i; j0 = 0; minimum = np.full(m + 1, np.inf); used = np.zeros(m + 1, dtype=bool)
        while True:
            used[j0] = True; i0 = p[j0]; delta = np.inf; j1 = 0
            for j in range(1, m + 1):
                if not used[j]:
                    cur = cost[i0 - 1, j - 1] - u[i0] - v[j]
                    if cur < minimum[j]: minimum[j] = cur; way[j] = j0
                    if minimum[j] < delta: delta = minimum[j]; j1 = j
            for j in range(m + 1):
                if used[j]: u[p[j]] += delta; v[j] -= delta
                else: minimum[j] -= delta
            j0 = j1
            if p[j0] == 0: break
        while True:
            j1 = way[j0]; p[j0] = p[j1]; j0 = j1
            if j0 == 0: break
    result = np.empty(n, dtype=int)
    for j in range(1, m + 1):
        if p[j]: result[p[j] - 1] = j - 1
    return result


def main():
    if len(sys.argv) < 3:
        raise SystemExit("usage: match_worldboss_icons.py ASSET_DIR SCREEN...")
    asset_dir = Path(sys.argv[1])
    allowed = None
    slots = None
    if "--plan" in sys.argv:
        marker = sys.argv.index("--plan")
        with open(sys.argv[marker + 1], encoding="utf-8-sig") as handle:
            rows = json.load(handle).get("Rows", [])
            allowed = {str(row["MasterId"]) for row in rows}
            slots = [str(row["MasterId"]) for row in rows]
        del sys.argv[marker:marker + 2]
    assets = []
    for path in asset_dir.glob("*.png"):
        if allowed is not None and path.stem not in allowed:
            continue
        assets.append((path.stem, feature(Image.open(path))))
    names = [item[0] for item in assets]
    references = np.stack([item[1] for item in assets])

    rank = 0
    all_errors = []
    for screen_path in map(Path, sys.argv[2:]):
        screen = Image.open(screen_path).convert("RGB")
        cell_w = screen.width / 10.0
        cell_h = screen.height / 2.0
        for row in range(2):
            for col in range(10):
                rank += 1
                # Portrait area inside the gold frame. Test nearby crops because
                # the three captures differ by a few pixels.
                candidates = []
                left0 = round(col * cell_w) + 9
                top0 = round(row * cell_h) + 8
                for dx in range(-3, 4):
                    for dy in range(-3, 4):
                        crop = screen.crop((left0 + dx, top0 + dy, left0 + dx + 100, top0 + dy + 100))
                        candidates.append(feature(crop))
                errors = np.full(len(names), np.inf, dtype=np.float32)
                for candidate in candidates:
                    errors = np.minimum(errors, np.mean((references - candidate) ** 2, axis=(1, 2, 3)))
                best = np.argsort(errors)[:4]
                print(f"{rank:02d}\t" + "\t".join(f"{names[i]}:{errors[i]:.5f}" for i in best), flush=True)
                all_errors.append(errors)
    if slots is not None and len(slots) == len(all_errors):
        columns = [names.index(master_id) for master_id in slots]
        assignment = hungarian(np.stack(all_errors)[:, columns])
        print("ASSIGNMENT")
        for index, column in enumerate(assignment):
            print(f"{index + 1:02d}\t{slots[column]}\t{all_errors[index][columns[column]]:.5f}")


if __name__ == "__main__":
    main()

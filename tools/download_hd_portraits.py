import concurrent.futures
import json
import pathlib
import re
import urllib.request

from PIL import Image

base = pathlib.Path("outputs/rune_manager_app/assets/monsters")
catalog = json.loads((base / "catalog.json").read_text(encoding="utf-8"))
by_id = {str(row["id"]): row for row in catalog}


def needs_hd(path):
    try:
        with Image.open(path) as image:
            return image.width < 80 or image.height < 80
    except Exception:
        return False


def download(path):
    row = by_id.get(path.stem)
    if not row or not row.get("icon"):
        return False, path.stem
    filename = row["icon"].rsplit("/", 1)[-1]
    filename = re.sub(r"_swgt[^.]*", "", filename)
    url = "https://swarfarm.com/static/herders/images/monsters/" + filename
    try:
        data = urllib.request.urlopen(url, timeout=25).read()
        tmp = path.with_suffix(".tmp")
        tmp.write_bytes(data)
        with Image.open(tmp) as image:
            if image.width < 80 or image.height < 80:
                tmp.unlink(missing_ok=True)
                return False, path.stem
        tmp.replace(path)
        return True, path.stem
    except Exception:
        return False, path.stem


targets = [path for path in base.glob("*.png") if needs_hd(path)]
with concurrent.futures.ThreadPoolExecutor(max_workers=16) as pool:
    results = list(pool.map(download, targets))

print("PORTRAITS_HD=" + str(sum(ok for ok, _ in results)))
print("ECHECS=" + str(sum(not ok for ok, _ in results)))

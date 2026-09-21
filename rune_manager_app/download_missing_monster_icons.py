import html
import json
import re
import sys
import urllib.request
from pathlib import Path


ELEMENTS = {
    "water": "water",
    "fire": "fire",
    "wind": "wind",
    "light": "light",
    "dark": "dark",
}


def normalized(value):
    return re.sub(r"[^a-z0-9]+", " ", html.unescape(value).lower()).strip()


def main():
    output = Path(sys.argv[1])
    catalog = json.loads((output / "catalog.json").read_text(encoding="utf-8"))
    present = {int(path.stem) for path in output.glob("*.png") if path.stem.isdigit()}
    request = urllib.request.Request(
        "https://summonerswarskyarena.info/monster-stat-database/",
        headers={"User-Agent": "Mozilla/5.0"},
    )
    page = urllib.request.urlopen(request, timeout=60).read().decode("utf-8", "ignore")
    rows = []
    pattern = re.compile(
        r'<tr class="searchable"[^>]*data-element="([^"]+)"[\s\S]*?'
        r'<img[^>]*data-src="([^"]+)"[\s\S]*?'
        r'<span class="element">([^<]+)</span>[\s\S]*?<h3>([^<]+)</h3>',
        re.I,
    )
    for element, url, display, monster_name in pattern.findall(page):
        rows.append((ELEMENTS.get(element.lower(), element.lower()), normalized(display), normalized(monster_name), html.unescape(url)))
    print("SITE_ROWS=" + str(len(rows)))

    downloaded = []
    unresolved = []
    for monster in catalog:
        monster_id = int(monster.get("id", 0))
        if not monster_id or monster_id in present:
            continue
        element = str(monster.get("element", "")).lower()
        name = normalized(str(monster.get("name", "")))
        base_name = name[len(element) + 1 :] if name.startswith(element + " ") else name
        exact = normalized(element + " " + base_name)
        matches = [row for row in rows if row[0] == element and (row[1] == exact or row[1].endswith(" " + base_name) or row[2] == base_name)]
        if len(matches) != 1:
            unresolved.append((monster_id, monster.get("name", ""), element))
            continue
        target = output / (str(monster_id) + ".png")
        image_request = urllib.request.Request(matches[0][3], headers={"User-Agent": "Mozilla/5.0"})
        data = urllib.request.urlopen(image_request, timeout=60).read()
        if not data.startswith(b"\x89PNG\r\n\x1a\n"):
            unresolved.append((monster_id, monster.get("name", ""), element))
            continue
        target.write_bytes(data)
        downloaded.append((monster_id, monster.get("name", ""), matches[0][3]))

    print("DOWNLOADED=" + str(len(downloaded)))
    print("UNRESOLVED=" + str(len(unresolved)))
    for item in downloaded:
        print("OK", item[0], item[1])
    for item in unresolved:
        print("MISSING", item[0], item[1], item[2])


if __name__ == "__main__":
    main()

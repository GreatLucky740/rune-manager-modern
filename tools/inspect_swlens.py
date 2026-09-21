import re
import ssl
import urllib.request
from urllib.parse import urljoin
from pathlib import Path

BASE = "https://www.swlens.io"
ctx = ssl.create_default_context()
req = urllib.request.Request(BASE + "/webapp/bestiary", headers={"User-Agent": "Mozilla/5.0"})
html = urllib.request.urlopen(req, timeout=30, context=ctx).read().decode("utf-8", "replace")
scripts = sorted(set(re.findall(r'src="([^"]+\.js[^\"]*)"', html)))
print("scripts", len(scripts))
patterns = re.compile(r"(?:https?://[^\"']+|/[A-Za-z0-9_?=&./{}:-]+)")
for src in scripts:
    try:
        req = urllib.request.Request(urljoin(BASE, src), headers={"User-Agent": "Mozilla/5.0"})
        body = urllib.request.urlopen(req, timeout=30, context=ctx).read().decode("utf-8", "replace")
    except Exception:
        continue
    out = Path("tools") / ("swlens_" + src.split("/")[-1].split("?")[0])
    out.write_text(body, encoding="utf-8")
    if re.search(r"usage.?score|overall.?pvp|siege.?def|bestiary", body, re.I):
        print("MATCH", src, len(body))
        urls = sorted(set(x for x in patterns.findall(body) if re.search(r"api|bestiary|monster|score|usage", x, re.I)))
        for value in urls[:100]:
            print(" ", value[:500])
        for match in re.finditer(r"usage.?score|overall.?pvp|siege.?def|bestiary", body, re.I):
            print("  CTX", body[max(0, match.start()-180):match.start()+350].replace("\n", " ")[:530].encode("ascii", "replace").decode())
            if match.start() > 0 and match.start() > 20000:
                break

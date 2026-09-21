import pathlib
import re
import urllib.request

base = "https://www.swlens.io"
html = urllib.request.urlopen(base + "/webapp/bestiary?tab=all", timeout=30).read().decode("utf-8")
urls = [base + value for value in re.findall(r'src="(/_next/static/chunks/[^"]+\.js[^"]*)', html)]
target = pathlib.Path("swlens_chunks")
target.mkdir(exist_ok=True)
for url in dict.fromkeys(urls):
    name = url.rsplit("/", 1)[-1].split("?", 1)[0]
    (target / name).write_bytes(urllib.request.urlopen(url, timeout=30).read())
print(len(set(urls)))

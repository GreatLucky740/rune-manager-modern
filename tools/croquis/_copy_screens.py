import os
import re
import shutil

src = r"C:\Users\Great-Lucky\.cursor\projects\c-Users-Great-Lucky-Documents-Rune-Manager-Modern\agent-transcripts\9f50e4a4-c67d-4e6a-898f-7a495cc62002\9f50e4a4-c67d-4e6a-898f-7a495cc62002.jsonl"
out = r"C:\Users\Great-Lucky\Documents\Rune_Manager_Modern\Donnees\assets\croquis\_screens"
assets = r"C:\Users\Great-Lucky\.cursor\projects\c-Users-Great-Lucky-Documents-Rune-Manager-Modern\assets"
os.makedirs(out, exist_ok=True)
pat = re.compile(r"c__Users_Great-Lucky_AppData_Roaming_Cursor_User_workspaceStorage_bbc93fecd1b30cb2ac876d030bda9d68_images__[A-Za-z0-9_.-]+\.png")
names = []
with open(src, "r", encoding="utf-8") as f:
    for line in f:
        if "63D27D8F-1528-4EC0-8CCF-EB94E529921D" in line:
            names = pat.findall(line)
            break
print("names", len(names))
if names:
    print("sample", names[0][:70])
seen = set()
uniq = []
for n in names:
    if n not in seen:
        seen.add(n)
        uniq.append(n)
print("uniq", len(uniq))
for i, n in enumerate(uniq[:38], 1):
    p = os.path.join(assets, n)
    dest = os.path.join(out, "%02d.png" % i)
    shutil.copy2(p, dest)
    print(i, os.path.getsize(p))

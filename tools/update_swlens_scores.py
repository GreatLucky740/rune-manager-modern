import json
import urllib.request
import urllib.error
from pathlib import Path

URL = "https://jljnwnydzodbisihlqcj.supabase.co/rest/v1/rpc/get_all_monster_usage_scores"
KEY = "sb_publishable_orn9Kf_ygbbjina8jLsdag_tnh1cwVS"
DATA = Path("outputs/artifact_manager_app/Artifact_Manager_Data.json")

rows = []
offset = 0
while True:
    request = urllib.request.Request(
        f"{URL}?offset={offset}&limit=1000",
        data=b"{}",
        headers={"apikey": KEY, "Authorization": "Bearer " + KEY, "Content-Type": "application/json"},
    )
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            page = json.loads(response.read())
    except urllib.error.HTTPError as error:
        if error.code in (416, 500) and offset > 0:
            break
        raise
    rows.extend(page)
    if len(page) < 1000:
        break
    offset += 1000

db = json.loads(DATA.read_text(encoding="utf-8"))
unit_names = {str(k): str(v) for k, v in db["unit_names"].items()}
scores = {}
for row in rows:
    name = unit_names.get(str(row["unit_master_id"]))
    if not name:
        continue
    content = str(row["content_type"])
    score = float(row["score"])
    item = scores.setdefault(name, {"best_score": 0.0, "best_content": "", "scores": {}})
    item["scores"][content] = max(score, float(item["scores"].get(content, 0)))
    if score > item["best_score"]:
        item["best_score"] = score
        item["best_content"] = content

db["monster_usage_scores"] = scores
db["monster_usage_scores_source"] = "SWLens get_all_monster_usage_scores"
DATA.write_text(json.dumps(db, ensure_ascii=False, indent=2), encoding="utf-8")
print(f"rows={len(rows)} monsters={len(scores)}")

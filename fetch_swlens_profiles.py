import collections
import json
import pathlib
import urllib.parse
import urllib.request

BASE = "https://jljnwnydzodbisihlqcj.supabase.co/rest/v1"
KEY = "sb_publishable_orn9Kf_ygbbjina8jLsdag_tnh1cwVS"
OUT = pathlib.Path(__file__).resolve().parent


def fetch_all(table, columns):
    rows = []
    offset = 0
    while True:
        query = urllib.parse.urlencode({"select": columns, "limit": 1000, "offset": offset})
        request = urllib.request.Request(
            f"{BASE}/{table}?{query}", headers={"apikey": KEY, "Accept": "application/json"}
        )
        page = json.loads(urllib.request.urlopen(request, timeout=60).read())
        rows.extend(page)
        if len(page) < 1000:
            return rows
        offset += len(page)


EFFECT_NAMES = {
    200: "ATK+ Prop. to Lost HP",
    201: "DEF+ Prop. to Lost HP",
    202: "SPD+ Prop. to Lost HP",
    203: "SPD Under Inability +",
    204: "ATK UP Effect +",
    205: "DEF UP Effect +",
    206: "SPD UP Effect +",
    207: "CRIT Rate Increasing Effect +",
    208: "Counterattack DMG +",
    209: "Co-op Attack DMG +",
    210: "Bomb DMG +",
    211: "Damage Dealt by Reflect DMG +",
    212: "Crushing Hit DMG +",
    213: "Damage Received Under Inability -",
    214: "CRIT DMG Taken -",
    215: "Life Drain +",
    216: "HP when Revived +",
    217: "Attack Bar when Revived +",
    218: "Add'l DMG Prop. to HP",
    219: "Add'l DMG Prop. to ATK",
    220: "Add'l DMG Prop. to DEF",
    221: "Add'l DMG Prop. to SPD",
    222: "CD+ as Enemy HP is More",
    223: "CD+ as Enemy HP is Less",
    224: "Own Turn 1-target CD+",
    225: "Counterattack/Co-op Attack DMG +",
    226: "ATK/DEF UP Effect +",
    300: "DMG dealt on Fire +",
    301: "DMG dealt on Water +",
    302: "DMG dealt on Wind +",
    303: "DMG dealt on Light +",
    304: "DMG dealt on Dark +",
    305: "DMG taken from Fire -",
    306: "DMG taken from Water -",
    307: "DMG taken from Wind -",
    308: "DMG taken from Light -",
    309: "DMG taken from Dark -",
    400: "[Skill 1] CRIT DMG +",
    401: "[Skill 2] CRIT DMG +",
    402: "[Skill 3] CRIT DMG +",
    403: "[Skill 4] CRIT DMG +",
    404: "S1 Recovery+",
    405: "S2 Recovery+",
    406: "S3 Recovery+",
    407: "S1 ACC+",
    408: "S2 ACC+",
    409: "S3 ACC+",
    410: "S3/S4 CRIT DMG+",
    411: "First Attack CD+",
}


def normalize_stat(value, effect_id=None):
    return EFFECT_NAMES.get(effect_id, str(value or "").strip())


def make_mode(primary_rows, sub_rows, percentages_are_stored):
    by_monster = collections.defaultdict(lambda: {"primary": [], "substats": []})

    primary_totals = collections.Counter()
    sub_totals = collections.Counter()
    if not percentages_are_stored:
        for row in primary_rows:
            primary_totals[(row["unit_master_id"], row["slot_id"])] += row.get("usage_count") or 0
        for row in sub_rows:
            sub_totals[(row["unit_master_id"], row["slot_id"])] += row.get("usage_count") or 0

    for row in primary_rows:
        key = (row["unit_master_id"], row["slot_id"])
        count = row.get("usage_count") or 0
        pct = row.get("usage_percentage")
        if pct is None:
            total = primary_totals[key]
            pct = 100 * count / total if total else 0
        by_monster[row["unit_master_id"]]["primary"].append(
            {"slot": row["slot_id"], "stat": normalize_stat(row.get("effect_name")), "pct": round(pct, 1), "count": count}
        )

    for row in sub_rows:
        key = (row["unit_master_id"], row["slot_id"])
        count = row.get("usage_count") or 0
        pct = row.get("usage_percentage")
        if pct is None:
            total = sub_totals[key]
            pct = 100 * count / total if total else 0
        by_monster[row["unit_master_id"]]["substats"].append(
            {"slot": row["slot_id"], "stat": normalize_stat(row.get("effect_name"), row.get("effect_id")), "pct": round(pct, 1), "count": count}
        )

    result = {}
    for monster_id, data in by_monster.items():
        slots = sorted({row["slot"] for row in data["primary"] + data["substats"]})
        # Current SWLens uses 1=Element and 2=Type. Some legacy rows use 0;
        # if only 0 exists, preserve it as Element instead of dropping the monster.
        element_slot = 1 if 1 in slots else (0 if 0 in slots else None)
        type_slot = 2 if 2 in slots else None
        mode = {}
        for label, slot in (("element", element_slot), ("type", type_slot)):
            if slot is None:
                continue
            prim = sorted((x for x in data["primary"] if x["slot"] == slot), key=lambda x: (-x["count"], x["stat"]))[:3]
            subs = sorted((x for x in data["substats"] if x["slot"] == slot), key=lambda x: (-x["count"], x["stat"]))[:4]
            if prim or subs:
                mode[label] = {
                    "primary": [{"stat": x["stat"], "pct": x["pct"]} for x in prim],
                    "substats": [{"stat": x["stat"], "pct": x["pct"]} for x in subs],
                }
        if mode:
            result[monster_id] = mode
    return result


def main():
    monsters = fetch_all("monsters", "id,name,element,family_name,image_filename,archetype")
    rta_primary = fetch_all("monster_community_artifact_primary", "unit_master_id,slot_id,effect_name,usage_count,usage_percentage,rank")
    rta_sub = fetch_all("monster_community_artifact_substats", "unit_master_id,slot_id,effect_id,effect_name,usage_count,usage_percentage,rank")
    siege_primary = fetch_all("siege_monster_community_artifact_primary", "unit_master_id,slot_id,effect_name,usage_count")
    siege_sub = fetch_all("siege_monster_community_artifact_substats", "unit_master_id,slot_id,effect_id,effect_name,usage_count")

    rta = make_mode(rta_primary, rta_sub, True)
    siege = make_mode(siege_primary, siege_sub, False)
    monster_by_id = {row["id"]: row for row in monsters}
    # Ignore orphaned historical rows whose monster no longer exists in the
    # current All Monsters bestiary (their public detail page returns Not Found).
    ids = sorted((set(rta) | set(siege)) & set(monster_by_id))
    profiles = []
    for monster_id in ids:
        monster = monster_by_id.get(monster_id, {})
        profiles.append(
            {
                "id": monster_id,
                "name": monster.get("name") or f"Monster {monster_id}",
                "element": monster.get("element"),
                "family": monster.get("family_name"),
                "image": monster.get("image_filename"),
                "role": monster.get("archetype"),
                "rta": rta.get(monster_id),
                "siege": siege.get(monster_id),
            }
        )

    (OUT / "swlens_artifact_profiles.json").write_text(json.dumps(profiles, ensure_ascii=False, indent=2), encoding="utf-8")
    (OUT / "monster_ids.json").write_text(
        json.dumps([{"id": row["id"], "name": row["name"], "element": row.get("element")} for row in monsters], ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    print(json.dumps({
        "monsters_in_database": len(monsters),
        "profiles": len(profiles),
        "rta_profiles": len(rta),
        "siege_profiles": len(siege),
        "rta_primary_rows": len(rta_primary),
        "rta_substat_rows": len(rta_sub),
        "siege_primary_rows": len(siege_primary),
        "siege_substat_rows": len(siege_sub),
    }, ensure_ascii=False))


if __name__ == "__main__":
    main()

import json
import re

with open("artifact_seed.json", encoding="utf-8") as f:
    seed = json.load(f)
with open("swlens_artifact_profiles.json", encoding="utf-8") as f:
    swlens = json.load(f)

stat_map = {
    "ATK/DEF Buff+": "ATK/DEF UP Effect +",
    "Addl. DMG by ATK": "Add'l DMG Prop. to ATK",
    "Addl. DMG by DEF": "Add'l DMG Prop. to DEF",
    "Addl. DMG by HP": "Add'l DMG Prop. to HP",
    "Addl. DMG by SPD": "Add'l DMG Prop. to SPD",
    "Add'l DMG by ATK%": "Add'l DMG Prop. to ATK",
    "Add'l DMG by DEF%": "Add'l DMG Prop. to DEF",
    "Add'l DMG by HP%": "Add'l DMG Prop. to HP",
    "Add'l DMG by SPD+": "Add'l DMG Prop. to SPD",
    "Bomb DMG+": "Bomb DMG +",
    "CD+ Enemy HP Bad": "CD+ as Enemy HP is Less",
    "CD+ Enemy HP Good": "CD+ as Enemy HP is More",
    "Counter/Co-op DMG+": "Counterattack/Co-op Attack DMG +",
    "Counter DMG+": "Counterattack DMG +",
    "Revenge DMG+": "Counterattack DMG +",
    "Co-op DMG+": "Co-op Attack DMG +",
    "Crit DMG Received-": "CRIT DMG Taken -",
    "Crit DMG Reduction+": "CRIT DMG Taken -",
    "DMG from Dark-": "DMG taken from Dark -",
    "DMG from Fire-": "DMG taken from Fire -",
    "DMG from Light-": "DMG taken from Light -",
    "DMG from Water-": "DMG taken from Water -",
    "DMG from Wind-": "DMG taken from Wind -",
    "DMG to Dark+": "DMG dealt on Dark +",
    "DMG to Fire+": "DMG dealt on Fire +",
    "DMG to Fire": "DMG dealt on Fire +",
    "DMG to Light+": "DMG dealt on Light +",
    "DMG to Water+": "DMG dealt on Water +",
    "DMG to Wind+": "DMG dealt on Wind +",
    "First Attack CD+": "First Attack CRIT DMG +",
    "Life Drain+": "Life Drain +",
    "Reflect DMG+": "Damage Dealt by Reflect DMG +",
    "S1 ACC+": "[Skill 1] Accuracy +",
    "S1 CRIT DMG+": "[Skill 1] CRIT DMG +",
    "S1 Recovery+": "[Skill 1] Recovery +",
    "S2 ACC+": "[Skill 2] Accuracy +",
    "S2 CRIT DMG+": "[Skill 2] CRIT DMG +",
    "S2 Recovery+": "[Skill 2] Recovery +",
    "S3 ACC+": "[Skill 3] Accuracy +",
    "S3 Recovery+": "[Skill 3] Recovery +",
    "S3/S4 CRIT DMG+": "[Skill 3/4] CRIT DMG +",
    "SPD Buff Effect+": "SPD UP Effect +",
    "Single CD on Turn": "Own Turn 1-target CD+",
}

def key(name):
    name = (name or "").split("/")[0].strip().casefold()
    return re.sub(r"[^a-z0-9]", "", name)

element_labels = {
    "water": "Eau",
    "fire": "Feu",
    "wind": "Vent",
    "light": "Lumière",
    "dark": "Ténèbres",
}

# SWLens contient plusieurs monstres portant exactement le même nom selon
# l'élément. Dans ce cas uniquement, rendre le nom visible non ambigu dans
# les presets et dans le menu de la feuille Monstre.
elements_by_name = {}
for mon in swlens:
    mon_key = key(mon.get("name"))
    element_key = str(mon.get("element") or "").strip().lower()
    if mon_key and element_key:
        elements_by_name.setdefault(mon_key, set()).add(element_key)

ambiguous_names = {name for name, elements in elements_by_name.items() if len(elements) > 1}

def display_name(mon):
    name = str(mon.get("name") or "").strip()
    if key(name) not in ambiguous_names:
        return name
    element_key = str(mon.get("element") or "").strip().lower()
    label = element_labels.get(element_key, str(mon.get("element") or "").strip())
    return f"{name} {label}".strip()

base_by_name = {}
for p in seed["profiles"]:
    base_by_name.setdefault(key(p["monster"]), p)

def weights(items):
    # SWLens definit l'ordre des priorites. Les pourcentages ci-dessous
    # reprennent le systeme lisible de l'Excel runes : P1=100 %, P2=90 %,
    # P3=85 %, P4=80 %. L'echelle de base 3 conserve un score /10 utile.
    best_by_stat = {}
    for item in items:
        if item["stat"] not in stat_map:
            continue
        name = stat_map[item["stat"]]
        best_by_stat[name] = max(best_by_stat.get(name, 0), float(item["pct"]))
    ranked = sorted(best_by_stat.items(), key=lambda x: (-x[1], x[0]))[:4]
    factors = (1.0, 0.90, 0.85, 0.80)
    return {name: round(3 * factors[index], 4) for index, (name, _) in enumerate(ranked)}

def preferred(items):
    if not items:
        return ""
    top = max(items, key=lambda x: x["pct"])["stat"]
    return {"ATK+": "Attack", "HP+": "HP", "DEF+": "Defense"}.get(top, "")

def required_main(items):
    """Impose la stat principale uniquement quand SWLens depasse strictement 75 %."""
    if not items:
        return ""
    top = max(items, key=lambda x: x["pct"])
    if float(top.get("pct", 0)) <= 75:
        return ""
    return {"ATK+": "Attack", "HP+": "HP", "DEF+": "Defense"}.get(top["stat"], "")

profiles = []
missing_role = []
for mon in swlens:
    base = base_by_name.get(key(mon["name"]))
    visible_name = display_name(mon)
    role_raw = str(mon.get("role") or "").strip().lower()
    role = {"attack": "Attack", "defense": "Defense", "hp": "HP", "support": "Support"}.get(role_raw)
    if not role and base:
        role = base.get("role", "")
    if not role:
        missing_role.append(mon["name"])
        role = "Support"
    element = mon.get("element") or (base.get("element", "") if base else "")
    element_id = {"Water": 1, "Fire": 2, "Wind": 3, "Light": 4, "Dark": 5}.get(element, 0)
    style_id = {"Attack": 1, "Defense": 2, "HP": 3, "Support": 4}.get(role, 4)
    for mode_key, mode_name in (("rta", "RTA"), ("siege", "Siege")):
        data = mon.get(mode_key)
        if not data:
            continue
        element_data = data.get("element", {"primary": [], "substats": []})
        type_data = data.get("type", {"primary": [], "substats": []})
        profiles.append({
            "family": mon.get("family") or (base.get("family", "") if base else ""), "monster": visible_name,
            "monster_id": mon["id"], "element": element,
            "element_id": element_id, "role": role,
            "style_id": style_id, "mode": mode_name,
            "preset": f'{visible_name} {mode_name}', "source": "SWLens",
            "preferred_flat_element": preferred(element_data["primary"]),
            "preferred_flat_type": preferred(type_data["primary"]),
            "required_main_element": required_main(element_data["primary"]),
            "required_main_type": required_main(type_data["primary"]),
            "weights_element": weights(element_data["substats"]),
            "weights_type": weights(type_data["substats"]),
            "swlens_element": element_data, "swlens_type": type_data,
        })

seed["profiles"] = profiles
seed["swlens_summary"] = {
    "checked": len(swlens), "with_data": sum(bool(x.get("rta") or x.get("siege")) for x in swlens),
    "rta": sum(bool(x.get("rta")) for x in swlens), "siege": sum(bool(x.get("siege")) for x in swlens),
    "profiles_matched": len(profiles), "missing_role_count": len(missing_role),
    "missing_role_examples": missing_role[:30], "collected": "2026-08-20",
}
with open("artifact_seed_swlens.json", "w", encoding="utf-8") as f:
    json.dump(seed, f, ensure_ascii=False)
print(json.dumps(seed["swlens_summary"], ensure_ascii=False))

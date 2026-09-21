import fs from "node:fs/promises";

const BASE = "https://jljnwnydzodbisihlqcj.supabase.co/rest/v1";
const KEY = "sb_publishable_orn9Kf_ygbbjina8jLsdag_tnh1cwVS";
const mappingsText = (await fs.readFile("artifact_mappings.json", "utf8")).replace(/^\uFEFF/, "");
const effectNames = new Map(JSON.parse(mappingsText.split(/\r?\n/, 1)[0]).filter(x => typeof x[0] === "number"));

async function fetchAll(table, columns) {
  const rows = [];
  for (let offset = 0;; offset += 1000) {
    const url = `${BASE}/${table}?select=${encodeURIComponent(columns)}&limit=1000&offset=${offset}`;
    const response = await fetch(url, { headers: { apikey: KEY, Accept: "application/json" } });
    if (!response.ok) throw new Error(`${table}: HTTP ${response.status} ${await response.text()}`);
    const page = await response.json();
    rows.push(...page);
    if (page.length < 1000) return rows;
  }
}

function makeMode(primaryRows, subRows, storedPct) {
  const byMonster = new Map();
  const primaryTotals = new Map(), subTotals = new Map();
  const bucket = id => { if (!byMonster.has(id)) byMonster.set(id, { primary: [], substats: [] }); return byMonster.get(id); };
  const addTotal = (map, row) => { const k = `${row.unit_master_id}|${row.slot_id}`; map.set(k, (map.get(k) || 0) + (row.usage_count || 0)); };
  if (!storedPct) { primaryRows.forEach(x => addTotal(primaryTotals, x)); subRows.forEach(x => addTotal(subTotals, x)); }
  const add = (rows, field, totals, isSub) => rows.forEach(row => {
    const count = row.usage_count || 0, key = `${row.unit_master_id}|${row.slot_id}`;
    const pct = row.usage_percentage ?? (totals.get(key) ? 100 * count / totals.get(key) : 0);
    const stat = isSub ? (effectNames.get(Number(row.effect_id)) || String(row.effect_name || "").trim()) : String(row.effect_name || "").trim();
    bucket(row.unit_master_id)[field].push({ slot: row.slot_id, stat, pct: Math.round(pct * 10) / 10, count });
  });
  add(primaryRows, "primary", primaryTotals, false); add(subRows, "substats", subTotals, true);
  const result = new Map();
  for (const [id, data] of byMonster) {
    const slots = new Set([...data.primary, ...data.substats].map(x => x.slot));
    const elementSlot = slots.has(1) ? 1 : slots.has(0) ? 0 : null;
    const typeSlot = slots.has(2) ? 2 : null;
    const mode = {};
    for (const [label, slot] of [["element", elementSlot], ["type", typeSlot]]) {
      if (slot == null) continue;
      const top = (field, n) => data[field].filter(x => x.slot === slot).sort((a,b) => b.count-a.count || a.stat.localeCompare(b.stat)).slice(0,n).map(({stat,pct}) => ({stat,pct}));
      const primary = top("primary", 3), substats = top("substats", 4);
      if (primary.length || substats.length) mode[label] = { primary, substats };
    }
    if (Object.keys(mode).length) result.set(id, mode);
  }
  return result;
}

const [monsters, rp, rs, sp, ss] = await Promise.all([
  fetchAll("monsters", "id,name,element,family_name,image_filename,archetype"),
  fetchAll("monster_community_artifact_primary", "unit_master_id,slot_id,effect_name,usage_count,usage_percentage,rank"),
  fetchAll("monster_community_artifact_substats", "unit_master_id,slot_id,effect_id,effect_name,usage_count,usage_percentage,rank"),
  fetchAll("siege_monster_community_artifact_primary", "unit_master_id,slot_id,effect_name,usage_count"),
  fetchAll("siege_monster_community_artifact_substats", "unit_master_id,slot_id,effect_id,effect_name,usage_count"),
]);
const rta = makeMode(rp, rs, true), siege = makeMode(sp, ss, false), byId = new Map(monsters.map(x => [x.id, x]));
const ids = [...new Set([...rta.keys(), ...siege.keys()])].filter(id => byId.has(id)).sort((a,b)=>a-b);
const profiles = ids.map(id => { const m=byId.get(id); return {id,name:m.name,element:m.element,family:m.family_name,image:m.image_filename,role:m.archetype,rta:rta.get(id),siege:siege.get(id)}; });
await fs.writeFile("swlens_artifact_profiles.json", JSON.stringify(profiles, null, 2), "utf8");
await fs.writeFile("monster_ids.json", JSON.stringify(monsters.map(x => ({id:x.id,name:x.name,element:x.element})), null, 2), "utf8");
console.log(JSON.stringify({profiles:profiles.length,rta:rta.size,siege:siege.size,rtaSub:rs.length,siegeSub:ss.length}));

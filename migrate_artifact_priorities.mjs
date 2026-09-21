import fs from "node:fs/promises";

const path = "artifact_seed_swlens.json";
const seed = JSON.parse(await fs.readFile(path, "utf8"));
const aliases = {
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
  "DMG to Dark": "DMG dealt on Dark +",
  "DMG to Fire+": "DMG dealt on Fire +",
  "DMG to Fire": "DMG dealt on Fire +",
  "DMG to Light+": "DMG dealt on Light +",
  "DMG to Light": "DMG dealt on Light +",
  "DMG to Water+": "DMG dealt on Water +",
  "DMG to Water": "DMG dealt on Water +",
  "DMG to Wind+": "DMG dealt on Wind +",
  "DMG to Wind": "DMG dealt on Wind +",
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
};
const factors = [3, 2.7, 2.55, 2.4];
function priorityWeights(items = []) {
  const best = new Map();
  for (const item of items) {
    const name = aliases[item.stat];
    if (!name) continue;
    best.set(name, Math.max(best.get(name) ?? 0, Number(item.pct) || 0));
  }
  return Object.fromEntries([...best].sort((a, b) => b[1] - a[1] || a[0].localeCompare(b[0])).slice(0, 4).map(([name], index) => [name, factors[index]]));
}
for (const profile of seed.profiles) {
  profile.weights_element = priorityWeights(profile.swlens_element?.substats);
  profile.weights_type = priorityWeights(profile.swlens_type?.substats);
}
seed.config = { ...(seed.config ?? {}), conversion_factor: 0.2 };
seed.priority_system = { P1: 1, P2: 0.9, P3: 0.85, P4: 0.8, conversion_gain: 0.2 };
await fs.writeFile(path, JSON.stringify(seed), "utf8");
console.log(JSON.stringify({ profiles: seed.profiles.length, prioritySystem: seed.priority_system }));

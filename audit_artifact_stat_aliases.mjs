import fs from "node:fs";

const seed = JSON.parse(fs.readFileSync("artifact_seed_swlens.json", "utf8"));
const source = fs.readFileSync("apply_swlens_profiles.py", "utf8");
const block = (source.match(/stat_map\s*=\s*\{([\s\S]*?)\n\}/) ?? [])[1] ?? "";
const mapped = new Set([...block.matchAll(/^\s*"([^"]+)"\s*:/gm)].map((match) => match[1]));
const raw = new Set();
for (const profile of seed.profiles) {
  for (const side of ["swlens_element", "swlens_type"]) {
    for (const item of profile[side]?.substats ?? []) raw.add(item.stat);
  }
}
const missing = [...raw].filter((name) => !mapped.has(name)).sort();
console.log(JSON.stringify({ rawCount: raw.size, mappedKeys: mapped.size, missingCount: missing.length, missing, raw: [...raw].sort() }, null, 2));

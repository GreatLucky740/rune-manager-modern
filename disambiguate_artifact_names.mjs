import fs from "node:fs/promises";

const file = "artifact_seed_swlens.json";
const seed = JSON.parse(await fs.readFile(file, "utf8"));
const labels = {
  water: "Eau",
  fire: "Feu",
  wind: "Vent",
  light: "Lumière",
  dark: "Ténèbres",
};
const normalize = (value) => String(value ?? "").trim().toLocaleLowerCase("en-US");
const elementsByName = new Map();
for (const profile of seed.profiles) {
  const name = normalize(profile.monster);
  const element = normalize(profile.element);
  if (!name || !element) continue;
  if (!elementsByName.has(name)) elementsByName.set(name, new Set());
  elementsByName.get(name).add(element);
}
const ambiguous = new Set(
  [...elementsByName].filter(([, elements]) => elements.size > 1).map(([name]) => name),
);
let changedProfiles = 0;
const changedMonsters = new Set();
for (const profile of seed.profiles) {
  const original = String(profile.monster ?? "").trim();
  if (!ambiguous.has(normalize(original))) continue;
  const element = normalize(profile.element);
  const suffix = labels[element] ?? String(profile.element ?? "").trim();
  const visible = `${original} ${suffix}`.trim();
  profile.monster = visible;
  profile.preset = `${visible} ${profile.mode}`;
  changedProfiles += 1;
  changedMonsters.add(visible);
}
seed.swlens_summary = {
  ...(seed.swlens_summary ?? {}),
  disambiguated_profiles: changedProfiles,
  disambiguated_monsters: changedMonsters.size,
};
await fs.writeFile(file, JSON.stringify(seed), "utf8");
console.log(JSON.stringify({ changedProfiles, changedMonsters: changedMonsters.size }));

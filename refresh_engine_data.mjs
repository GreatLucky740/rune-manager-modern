import fs from 'node:fs';

const seed = JSON.parse(fs.readFileSync('artifact_seed_swlens.json', 'utf8'));
const out = 'outputs/artifact_manager_modern/Artifact_Manager_Data.json';
const current = JSON.parse(fs.readFileSync(out, 'utf8'));
const payload = {
  profiles: seed.profiles,
  rolls: seed.rolls,
  stat_names: seed.stat_names,
  unit_names: current.unit_names,
  config: { ...(current.config ?? {}), ...(seed.config ?? {}) }
};
fs.writeFileSync(out, JSON.stringify(payload), 'utf8');
console.log(`${payload.profiles.length} profils ecrits.`);

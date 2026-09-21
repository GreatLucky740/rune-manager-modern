import fs from 'node:fs';

const path = 'swlens_artifact_profiles.json';
const data = JSON.parse(fs.readFileSync(path, 'utf8'));
const lushen = data.find(x => x.name === 'Lushen');
if (!lushen) throw new Error('Lushen introuvable');
lushen.siege = {
  element: {
    primary: [{stat:'ATK+',pct:87.9},{stat:'HP+',pct:7.4},{stat:'DEF+',pct:4.7}],
    substats: [{stat:'Addl. DMG by ATK',pct:11.8},{stat:'CD+ Enemy HP Good',pct:6.8},{stat:'DMG to Water+',pct:6.4},{stat:'Addl. DMG by SPD',pct:5.6}]
  },
  type: {
    primary: [{stat:'ATK+',pct:78.6},{stat:'HP+',pct:12.5},{stat:'DEF+',pct:8.8}],
    substats: [{stat:'Addl. DMG by ATK',pct:9.6},{stat:'S3/S4 CRIT DMG+',pct:6.6},{stat:'CD+ Enemy HP Good',pct:6.6},{stat:'CD+ Enemy HP Bad',pct:6.4}]
  }
};
fs.writeFileSync(path, JSON.stringify(data, null, 2), 'utf8');
console.log('Lushen Siege ajoute.');

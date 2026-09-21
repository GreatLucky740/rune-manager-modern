import fs from "node:fs/promises";
import path from "node:path";

const app="outputs/artifact_manager_app";
const dataPath=path.join(app,"Artifact_Manager_Data.json");
const monsterDir=path.join(app,"assets","monsters");
const catalogPath=path.join(monsterDir,"catalog.json");
const data=JSON.parse(await fs.readFile(dataPath,"utf8"));
const seed=JSON.parse(await fs.readFile("artifact_seed_swlens.json","utf8"));
const swlens=JSON.parse(await fs.readFile("swlens_artifact_profiles.json","utf8"));
const catalog=JSON.parse(await fs.readFile(catalogPath,"utf8"));
const byId=new Map(catalog.map(x=>[Number(x.id),x]));
const profileNameById=new Map();
for(const p of seed.profiles)if(!profileNameById.has(Number(p.monster_id)))profileNameById.set(Number(p.monster_id),p.monster);

data.profiles=seed.profiles;
data.rolls=seed.rolls;
data.stat_names=seed.stat_names;
data.config={...(data.config||{}),...(seed.config||{})};
data.unit_names=data.unit_names||{};
for(const [id,name] of profileNameById)data.unit_names[String(id)]=name;
await fs.writeFile(dataPath,JSON.stringify(data),"utf8");

let added=0,downloaded=0,failed=0;
for(const m of swlens){
  const id=Number(m.id);let entry=byId.get(id);
  if(!entry){entry={id,name:m.name,family:m.family||m.name,element:String(m.element||"").toLowerCase(),icon:m.image?`https://swarfarm.com/static/herders/images/monsters/${m.image}`:"",stars:0,skillups:0,familyid:Math.floor(id/100)*100,skillgroup:Math.floor(id/100)*100};catalog.push(entry);byId.set(id,entry);added++;}
  else {entry.name=m.name||entry.name;entry.family=m.family||entry.family;entry.element=String(m.element||entry.element||"").toLowerCase();if(m.image)entry.icon=`https://swarfarm.com/static/herders/images/monsters/${m.image}`;}
  const portrait=path.join(monsterDir,`${id}.png`);
  try{await fs.access(portrait);}catch{
    if(entry.icon){try{const response=await fetch(entry.icon);if(!response.ok)throw new Error(`HTTP ${response.status}`);await fs.writeFile(portrait,new Uint8Array(await response.arrayBuffer()));downloaded++;}catch{failed++;}}
  }
}
catalog.sort((a,b)=>Number(a.id)-Number(b.id));
await fs.writeFile(catalogPath,JSON.stringify(catalog,null,2),"utf8");
console.log(JSON.stringify({swlens:swlens.length,profiles:seed.profiles.length,catalog:catalog.length,added,downloaded,failed}));

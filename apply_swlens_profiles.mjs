import fs from "node:fs/promises";

const seed = JSON.parse((await fs.readFile("artifact_seed.json", "utf8")).replace(/^\uFEFF/, ""));
const swlens = JSON.parse(await fs.readFile("swlens_artifact_profiles.json", "utf8"));
const factors = [3, 2.7, 2.55, 2.4];
const elementLabels = { water:"Eau", fire:"Feu", wind:"Vent", light:"Lumière", dark:"Ténèbres" };
const key = s => String(s || "").split("/")[0].trim().toLowerCase().normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/[^a-z0-9]/g, "");
const byName = new Map();
for (const p of seed.profiles) if (!byName.has(key(p.monster))) byName.set(key(p.monster), p);
const elementsByName = new Map();
for (const m of swlens) { const k=key(m.name), e=String(m.element||"").toLowerCase(); if (!elementsByName.has(k)) elementsByName.set(k,new Set()); if(e) elementsByName.get(k).add(e); }
const ambiguous = new Set([...elementsByName].filter(([,v])=>v.size>1).map(([k])=>k));
const displayName = m => ambiguous.has(key(m.name)) ? `${m.name} ${elementLabels[String(m.element||"").toLowerCase()] || m.element}` : m.name;
const preferred = items => ({"ATK+":"Attack","HP+":"HP","DEF+":"Defense"})[[...(items||[])].sort((a,b)=>b.pct-a.pct)[0]?.stat] || "";
const required = items => { const x=[...(items||[])].sort((a,b)=>b.pct-a.pct)[0]; return x && Number(x.pct)>75 ? preferred([x]) : ""; };
const weights = items => Object.fromEntries([...(items||[])].sort((a,b)=>b.pct-a.pct || a.stat.localeCompare(b.stat)).slice(0,4).map((x,i)=>[x.stat,factors[i]]));
const profiles=[]; const missingRole=[];
for (const m of swlens) {
  const base=byName.get(key(m.name));
  let role=({attack:"Attack",defense:"Defense",hp:"HP",support:"Support"})[String(m.role||"").toLowerCase()] || base?.role;
  if(!role){role="Support";missingRole.push(m.name);}
  const element=m.element||base?.element||"", visible=displayName(m);
  for(const [modeKey,modeName] of [["rta","RTA"],["siege","Siege"]]){
    const data=m[modeKey]; if(!data) continue;
    const el=data.element||{primary:[],substats:[]}, ty=data.type||{primary:[],substats:[]};
    profiles.push({family:m.family||base?.family||"",monster:visible,monster_id:m.id,element,element_id:({Water:1,Fire:2,Wind:3,Light:4,Dark:5})[element]||0,role,style_id:({Attack:1,Defense:2,HP:3,Support:4})[role]||4,mode:modeName,preset:`${visible} ${modeName}`,source:"SWLens",preferred_flat_element:preferred(el.primary),preferred_flat_type:preferred(ty.primary),required_main_element:required(el.primary),required_main_type:required(ty.primary),weights_element:weights(el.substats),weights_type:weights(ty.substats),swlens_element:el,swlens_type:ty});
  }
}
seed.profiles=profiles;
seed.config={...(seed.config||{}),conversion_factor:0.2};
seed.priority_system={P1:1,P2:0.9,P3:0.85,P4:0.8,conversion_gain:0.2};
seed.swlens_summary={checked:swlens.length,with_data:swlens.filter(x=>x.rta||x.siege).length,rta:swlens.filter(x=>x.rta).length,siege:swlens.filter(x=>x.siege).length,profiles_matched:profiles.length,missing_role_count:missingRole.length,missing_role_examples:missingRole.slice(0,30),collected:"2026-08-20"};
await fs.writeFile("artifact_seed_swlens.json",JSON.stringify(seed),"utf8");
console.log(JSON.stringify(seed.swlens_summary));

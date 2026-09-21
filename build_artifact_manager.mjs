import fs from 'node:fs/promises';
import { Workbook, SpreadsheetFile } from '@oai/artifact-tool';

const seed = JSON.parse(await fs.readFile('artifact_seed_swlens.json','utf8'));
const outDir = 'outputs/artifact_manager_modern';
await fs.mkdir(outDir,{recursive:true});

const wb = Workbook.create();
const home = wb.worksheets.add('Home');
const arts = wb.worksheets.add('Artifacts');
const presets = wb.worksheets.add('Presets');
const rolls = wb.worksheets.add('Rolls');
const cfg = wb.worksheets.add('Configuration');
const monsters = wb.worksheets.add('Monstre');
const src = wb.worksheets.add('Sources');

const bg='#090D14', panel='#111827', head='#0F766E', cyan='#22D3EE', white='#E2E8F0', muted='#94A3B8', orange='#F97316', purple='#A855F7', red='#3F1111', green='#22C55E';
const usedAreas = new Map([[home,'A1:H20'],[arts,'A1:R2400'],[presets,`A1:BD${seed.profiles.length+5}`],[rolls,'A1:D60'],[cfg,'A1:B12'],[src,'A1:B8']]);
for (const sh of [home,arts,presets,rolls,cfg,monsters,src]) { sh.showGridLines=false; sh.getRange('A1:AZ5000').format={fill:bg,font:{color:white,name:'Aptos',size:10}}; }

const roll = seed.rolls;
const profilesByCompat = new Map();
for (const p of seed.profiles) {
  const key = p.element_id ? `E${p.element_id}` : `S${p.style_id}`;
  if (!profilesByCompat.has(key)) profilesByCompat.set(key,[]);
  profilesByCompat.get(key).push(p);
}
const flatPref={"HP flat":"HP","ATK flat":"Attack","DEF flat":"Defense"};
function evalProfile(a,p){
  const weights=a.type===1?(p.weights_element||{}):(p.weights_type||{});
  const preferred=a.type===1?(p.preferred_flat_element||''):(p.preferred_flat_type||'');
  let total=0; const contrib=[]; const present=new Set();
  for(const s of a.substats){ const rr=roll[s.name]?.avg||1; const w=weights[s.name]||0; const c=(s.value/rr)*w; total+=c; contrib.push({name:s.name,c,value:s.value}); present.add(s.name); }
  if (preferred.includes(flatPref[a.primary_name]||'__')) total+=0.75;
  let source=contrib.slice().sort((x,y)=>x.c-y.c)[0]||{name:'-',c:0};
  let best=null;
  for(const [name,w] of Object.entries(weights)){
    if(present.has(name) || !roll[name]) continue;
    const c=(roll[name].max/roll[name].avg)*w;
    const gain=c-source.c;
    if(!best||gain>best.gain) best={name,gain,max:roll[name].max};
  }
  const conversionFactor=seed.config?.conversion_factor??0.2;
  const potential=total+Math.max(0,best?.gain||0)*conversionFactor;
  return {score:total/2.5,potential:potential/2.5,reco:best&&best.gain>0.05?`${source.name} → ${best.name} ${best.max}`:'Déjà optimal'};
}
function labelArtifact(a){const type=a.type===1?'Élément':'Type';const restriction=a.type===1?({1:'Eau',2:'Feu',3:'Vent',4:'Lumière',5:'Ténèbres',98:'Intangible'}[a.attribute]||a.attribute):({1:'Attaque',2:'Défense',3:'PV',4:'Support'}[a.style]||a.style);return `${a.grade===5?'Légendaire':'Héroïque'} ${type} ${restriction} +${a.level}`}
function statText(s){if(!s)return '';const v=Math.trunc(Number(s.value)*100)/100;return `${s.converted?'↻ ':''}${s.name} +${v}`}

const artifactRows=[];
for(const a of seed.artifacts){
  const key=a.type===1?`E${a.attribute}`:`S${a.style}`;
  const candidates=profilesByCompat.get(key)||seed.profiles;
  let best=null, bestP=null;
  for(const p of candidates){const e=evalProfile(a,p);if(!best||e.potential>best.potential){best=e;bestP=p;}}
  const action=best.potential>=6?'Keep':'Sell';
  artifactRows.push([labelArtifact(a),a.date_add||a.rid,a.type===1?'Élément':'Type',a.type===1?({1:'Eau',2:'Feu',3:'Vent',4:'Lumière',5:'Ténèbres',98:'Intangible'}[a.attribute]||a.attribute):({1:'Attaque',2:'Défense',3:'PV',4:'Support'}[a.style]||a.style),a.grade===5?'Légendaire':'Héroïque',a.level,`${a.primary_name} +${a.primary_value}`,statText(a.substats[0]),statText(a.substats[1]),statText(a.substats[2]),statText(a.substats[3]),action,Number(best.potential.toFixed(3)),best.reco,bestP?.preset||'',bestP?.mode||'',a.owner,a.locked?'Oui':'Non']);
}
artifactRows.sort((a,b)=>b[12]-a[12]);

home.getRange('A1:H2').merge(); home.getRange('A1').values=[['ARTIFACT MANAGER — MODERN EDITION']]; home.getRange('A1:H2').format={fill:'#071C2A',font:{color:cyan,bold:true,size:20},verticalAlignment:'center'};
home.getRange('A4:B8').values=[['INDICATEUR','VALEUR'],['Artefacts importés',artifactRows.length],['Presets monstres',seed.profiles.length],['Keep',artifactRows.filter(r=>r[11]==='Keep').length],['Sell',artifactRows.filter(r=>r[11]==='Sell').length]];
home.getRange('A4:B4').format={fill:head,font:{bold:true,color:'#FFFFFF'}}; home.getRange('D4:H8').values=[['COMMANDES','','','',''],['IMPORTER JSON','','','',''],['TRIER POTENTIAL VALUE','','','',''],['TRIER OBTAINED','','','',''],['RECALCULER','','','','']]; home.getRange('D4:H4').format={fill:head,font:{bold:true,color:'#FFFFFF'}}; home.getRange('D5:H8').format={fill:'#0E7490',font:{bold:true,color:'#FFFFFF'}};
home.getRange('A11:H14').merge(); home.getRange('A11').values=[['Le moteur compare chaque artefact aux presets RTA et Siège compatibles, mesure les rolls utiles, puis simule une conversion théorique de la statistique la moins rentable. Les profils peuvent être ajustés dans la feuille Presets.']]; home.getRange('A11:H14').format={fill:panel,font:{color:muted,italic:true},wrapText:true,verticalAlignment:'center'};
home.getRange('A1:H20').format.columnWidth=18; home.getRange('A:A').format.columnWidth=24;

const ah=['Artefact','Obtained','Catégorie','Restriction','Rareté','Niveau','Stat principale','Stat 1','Stat 2','Stat 3','Stat 4','Action','Potential Value','Conversion conseillée','Meilleur preset','Mode','Équipé sur','Verrouillé'];
arts.getRangeByIndexes(0,0,1,ah.length).values=[ah]; arts.getRangeByIndexes(1,0,artifactRows.length,ah.length).values=artifactRows;
arts.getRange(`A1:R1`).format={fill:head,font:{bold:true,color:'#FFFFFF'},wrapText:true}; arts.freezePanes.freezeRows(1); arts.getRange('A1:R2396').format.rowHeight=22;
const widths=[30,16,11,13,13,9,19,36,36,36,36,9,14,55,20,10,20,10]; widths.forEach((w,i)=>arts.getRangeByIndexes(0,i,artifactRows.length+1,1).format.columnWidth=w);
for(const col of ['B:B','E:E','F:F','P:P','Q:Q','R:R']) arts.getRange(col).format.columnWidth=0;
arts.getRange(`M2:M${artifactRows.length+1}`).format.numberFormat='0.000';
arts.getRange(`A2:R${artifactRows.length+1}`).conditionalFormats.addCustom('=$L2="Sell"',{fill:red,font:{color:'#FFFFFF'}});
arts.getRange(`L2:L${artifactRows.length+1}`).conditionalFormats.add('containsText',{text:'Keep',format:{font:{color:green,bold:true}}});
arts.getRange(`M2:M${artifactRows.length+1}`).conditionalFormats.add('colorScale',{colors:['#7F1D1D','#F59E0B','#16A34A']});

function listPct(xs){return (xs||[]).map(x=>`${x.stat} ${x.pct}%`).join(' | ')}
const ph=['Preset','Monstre','ID SWLens','Mode','Élément','Type','Source','Primaire élément','Sous-stats élément','Primaire type','Sous-stats type'];
const pr=seed.profiles.map(p=>[p.preset,p.monster,p.monster_id,p.mode,p.element,p.role,p.source,listPct(p.swlens_element?.primary),listPct(p.swlens_element?.substats),listPct(p.swlens_type?.primary),listPct(p.swlens_type?.substats)]);
presets.getRangeByIndexes(0,0,1,ph.length).values=[ph]; presets.getRangeByIndexes(1,0,pr.length,ph.length).values=pr; presets.getRange('A1:K1').format={fill:head,font:{bold:true,color:'#FFFFFF'}}; presets.freezePanes.freezeRows(1); presets.freezePanes.freezeColumns(2); presets.getRange('A:B').format.columnWidth=24; presets.getRange('C:G').format.columnWidth=14; presets.getRange('H:K').format.columnWidth=42; presets.getRange(`A1:K${pr.length+1}`).format.wrapText=true;

const rr=[['Propriété','Min','Max','Moyenne'],...Object.entries(roll).sort().map(([k,v])=>[k,v.min,v.max,v.avg])]; rolls.getRangeByIndexes(0,0,rr.length,4).values=rr; rolls.getRange('A1:D1').format={fill:head,font:{bold:true,color:'#FFFFFF'}}; rolls.getRange('A:A').format.columnWidth=34; rolls.getRange('B:D').format.columnWidth=12;
cfg.getRange('A1:B14').values=[['PARAMÈTRE','VALEUR'],['Seuil Keep',6],['Décisions','Keep / Sell'],['Bonus stat principale compatible',0.75],['Diviseur du score',2.5],['Priorité 1','100 %'],['Priorité 2','90 %'],['Priorité 3','85 %'],['Priorité 4','80 %'],['Gain conversion ajouté au score','20 %'],['Modes','RTA + Siège'],['Fiches SWLens contrôlées',seed.swlens_summary.checked],['Profils SWLens utilisés',seed.swlens_summary.profiles_matched],['Version','2026-08-20 V3 Priorités']]; cfg.getRange('A1:B1').format={fill:head,font:{bold:true,color:'#FFFFFF'}}; cfg.getRange('A:A').format.columnWidth=38; cfg.getRange('B:B').format.columnWidth=20;
const monsterNames=[...new Set(seed.profiles.map(p=>p.monster).filter(Boolean))].sort((a,b)=>a.localeCompare(b,'fr'));
monsters.getRange('A1:O1').merge(); monsters.getRange('A1').values=[['MEILLEURS ARTEFACTS PAR MONSTRE']]; monsters.getRange('A1:O1').format={fill:'#071C2A',font:{color:cyan,bold:true,size:18},verticalAlignment:'center'};
monsters.getRange('A2').values=[[monsterNames[0]||'']]; monsters.getRange('A2').format={fill:panel,font:{color:'#FFFFFF',bold:true}};
monsters.getRange('D2:G2').merge(); monsters.getRange('D2').values=[['ARTEFACTS ÉLÉMENT RTA']]; monsters.getRange('D2:G2').format={fill:'#0E7490',font:{bold:true,color:'#FFFFFF'},horizontalAlignment:'center'};
monsters.getRange('H2:K2').merge(); monsters.getRange('H2').values=[['ARTEFACTS ÉLÉMENT SIÈGE']]; monsters.getRange('H2:K2').format={fill:'#0F766E',font:{bold:true,color:'#FFFFFF'},horizontalAlignment:'center'};
monsters.getRange('D3:G3').merge(); monsters.getRange('D3').values=[['ARTEFACTS TYPE RTA']]; monsters.getRange('D3:G3').format={fill:'#0284C7',font:{bold:true,color:'#FFFFFF'},horizontalAlignment:'center'};
monsters.getRange('H3:K3').merge(); monsters.getRange('H3').values=[['ARTEFACTS TYPE SIÈGE']]; monsters.getRange('H3:K3').format={fill:'#7C3AED',font:{bold:true,color:'#FFFFFF'},horizontalAlignment:'center'};
monsters.getRange('A4:R4').values=[ah]; monsters.getRange('A4:R4').format={fill:head,font:{bold:true,color:'#FFFFFF'},wrapText:true}; monsters.freezePanes.freezeRows(4);
monsterNames.forEach((name,i)=>monsters.getRange(`Z${i+2}`).values=[[name]]); monsters.getRange('Z:Z').format.columnWidth=0;
monsters.getRange('A2').dataValidation={rule:{type:'list',formula1:`Monstre!$Z$2:$Z$${monsterNames.length+1}`}};
const monsterWidths=[30,16,11,13,13,9,19,36,36,36,36,9,14,55,20,10,20,10]; monsterWidths.forEach((w,i)=>monsters.getRangeByIndexes(3,i,1,1).format.columnWidth=w);
for(const col of ['B:B','E:E','F:F','P:P','Q:Q','R:R']) monsters.getRange(col).format.columnWidth=0;
src.getRange('A1:B5').values=[['SOURCE','UTILISATION'],['Artifact tool 3.7.2022 -Public.xlsm','Types et valeurs de rolls'],['SWEX JSON','Inventaire des artefacts et propriétaires'],['https://www.swlens.io/webapp/bestiary?tab=all','Pourcentages RTA et Siège relevés sur chaque fiche monstre'],['Collecte',`20 août 2026 — ${seed.swlens_summary.checked} monstres avec données, ${seed.swlens_summary.profiles_matched} profils RTA/Siège exploitables`]]; src.getRange('A1:B1').format={fill:head,font:{bold:true,color:'#FFFFFF'}}; src.getRange('A:A').format.columnWidth=48; src.getRange('B:B').format.columnWidth=70; src.getRange('A1:B5').format.wrapText=true;

const out=await SpreadsheetFile.exportXlsx(wb); await out.save(`${outDir}/Artifact_Manager_Modern_V2_SWLens.xlsx`);
const inspect=await wb.inspect({kind:'table',range:'Artifacts!A1:R8',include:'values,formulas',tableMaxRows:8,tableMaxCols:18,maxChars:8000}); console.log(inspect.ndjson);
const errors=await wb.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A',options:{useRegex:true,maxResults:50},summary:'formula errors'}); console.log(errors.ndjson);
const previews={Home:'A1:H16',Artifacts:'A1:R25',Monstre:'A1:O15',Presets:'A1:Z20',Rolls:'A1:D52',Configuration:'A1:B14',Sources:'A1:B6'};
for(const [sheetName,range] of Object.entries(previews)){const png=await wb.render({sheetName,range,scale:1,format:'png'});await fs.writeFile(`${outDir}/preview_${sheetName}.png`,new Uint8Array(await png.arrayBuffer()));}

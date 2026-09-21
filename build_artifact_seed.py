import json, sys, openpyxl

old_xlsm, swex_json, out = sys.argv[1:]
w = openpyxl.load_workbook(old_xlsm, read_only=False, data_only=True)

# Translation tables and monster names.
tr = w['JSON translate']
stat_names = {}
unit_names = {}
for r in range(1, tr.max_row + 1):
    a, b, d, e = tr.cell(r,1).value, tr.cell(r,2).value, tr.cell(r,4).value, tr.cell(r,5).value
    if isinstance(a, (int,float)) and isinstance(b, str) and a >= 100:
        stat_names[int(a)] = b
    if isinstance(d, (int,float)) and isinstance(e, str) and e.strip():
        unit_names[int(d)] = e.strip()

# Artifact roll ranges.
skills = w['Skills']
rolls = {}
for r in range(6, skills.max_row + 1):
    for base in (1, 6):
        name, mn, mx, avg = [skills.cell(r, base+i).value for i in range(4)]
        if isinstance(name, str) and isinstance(avg, (int,float)):
            rolls[name] = {'min': float(mn), 'max': float(mx), 'avg': float(avg)}

# Per-monster property preferences already contained in the public workbook.
um = w['User monsters']
headers = [um.cell(4,c).value for c in range(1, um.max_column+1)]
profiles = []
element_id = {'Water':1,'Fire':2,'Wind':3,'Light':4,'Dark':5}
style_id = {'Attack':1,'Defense':2,'HP':3,'Support':4}
for r in range(5, um.max_row + 1):
    family, element, name, role, preferred = [um.cell(r,c).value for c in range(1,6)]
    if not isinstance(name,str) or not name.strip(): continue
    weights = {}
    for c in range(7, um.max_column+1):
        h, v = headers[c-1], um.cell(r,c).value
        if isinstance(h,str) and isinstance(v,(int,float)) and v > 0:
            weights[h] = float(v)
    base = {'family':family or '', 'monster':name.strip(), 'element':element or '',
            'element_id':element_id.get(element,0), 'role':role or 'Support',
            'style_id':style_id.get(role,4), 'preferred_flat':preferred or '', 'weights':weights}
    for mode in ('RTA','Siege'):
        p = dict(base); p['mode']=mode; p['preset']=f"{name.strip()} {mode}"
        adjusted = dict(weights)
        for k in list(adjusted):
            if mode == 'RTA' and ("Add'l DMG" in k or 'DMG taken' in k or 'Accuracy' in k): adjusted[k] *= 1.12
            if mode == 'Siege' and ('Bomb DMG' in k or 'CRIT DMG +' in k or 'Recovery' in k or 'DMG dealt on' in k): adjusted[k] *= 1.12
        p['weights']=adjusted; profiles.append(p)

with open(swex_json,'r',encoding='utf-8-sig') as f: data=json.load(f)
units = data.get('unit_list',[])
owner = {int(u.get('unit_id',0)): {'master_id':int(u.get('unit_master_id',0)), 'name':unit_names.get(int(u.get('unit_master_id',0)),str(u.get('unit_master_id','')))} for u in units}
arts = list(data.get('artifacts',[]))
for u in units: arts.extend(u.get('artifacts',[]))
artifact_rows=[]
for a in arts:
    o=owner.get(int(a.get('occupied_id',0)),{})
    secs=[]
    for sec in a.get('sec_effects',[]):
        code=int(sec[0]); name=stat_names.get(code,str(code)); secs.append({'code':code,'name':name,'value':float(sec[1]),'converted':bool(sec[4])})
    pri=a.get('pri_effect',[0,0])
    artifact_rows.append({'rid':str(a.get('rid','')),'type':int(a.get('type',0)),'attribute':int(a.get('attribute',0)),
      'style':int(a.get('unit_style',0)),'grade':int(a.get('natural_rank',0)),'level':int(a.get('level',0)),
      'primary_code':int(pri[0]),'primary_name':stat_names.get(int(pri[0]),str(pri[0])),'primary_value':float(pri[1]),
      'locked':int(a.get('locked',0)),'owner':o.get('name',''),'owner_master_id':o.get('master_id',0),'substats':secs})

payload={'profiles':profiles,'rolls':rolls,'stat_names':stat_names,'artifacts':artifact_rows,
         'sources':{'old_workbook':old_xlsm,'swex':swex_json,'swlens':'https://www.swlens.io/webapp/bestiary'}}
with open(out,'w',encoding='utf-8') as f: json.dump(payload,f,ensure_ascii=False)
print(json.dumps({'profiles':len(profiles),'rolls':len(rolls),'artifacts':len(artifact_rows)},ensure_ascii=False))

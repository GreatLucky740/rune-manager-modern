import json, sys, collections
p=sys.argv[1]
with open(p,'r',encoding='utf-8-sig') as f:d=json.load(f)
arts=list(d.get('artifacts',[]))
for u in d.get('unit_list',[]): arts.extend(u.get('artifacts',[]))
print('artifacts',len(arts))
print('top_keys', sorted(d.keys()))
print('artifact_keys', sorted(set().union(*(a.keys() for a in arts))) if arts else [])
print('type',collections.Counter(a.get('type') for a in arts))
print('attribute',collections.Counter(a.get('attribute') for a in arts))
print('level',collections.Counter(a.get('level') for a in arts))
print('natural_rank',collections.Counter(a.get('natural_rank') for a in arts))
for a in arts[:3]: print(json.dumps(a,ensure_ascii=False))

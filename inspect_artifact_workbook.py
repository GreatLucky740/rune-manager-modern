import json, sys, openpyxl

p = sys.argv[1]
w = openpyxl.load_workbook(p, read_only=False, data_only=False, keep_vba=True)
out = {}
for name in ["Stats", "Skills", "User settings", "JSON translate", "JSON import", "User Artifacts", "Best mon for artifact", "Best artifact for mon"]:
    ws = w[name]
    rows = []
    for row in ws.iter_rows(min_row=1, max_row=min(ws.max_row, 60), min_col=1, max_col=min(ws.max_column, 20)):
        vals = [c.value for c in row]
        if any(v not in (None, "") for v in vals):
            rows.append(vals)
    out[name] = rows[:30]
print(json.dumps(out, ensure_ascii=False, default=str))

import json, sys
def interp(rows, day, key):
    rows = sorted(rows, key=lambda r: r['day'])
    for a, b in zip(rows, rows[1:]):
        if a['day'] <= day <= b['day']:
            f = (day - a['day']) / max(b['day'] - a['day'], 1e-9)
            return a[key] + f * (b[key] - a[key])
    return rows[-1][key]
c = json.load(open(sys.argv[1])); s = json.load(open(sys.argv[2]))
names = ['Ana', 'Ben', 'Cleo', 'Dev']
print('took', round(c['took']))
for day in [int(x) for x in (sys.argv[3].split(",") if len(sys.argv) > 3 else "1,2,3,5,7,10,13".split(","))]:
    crew = sum(interp([r[n] for r in c['rows']], day, 'inc') for n in names)
    solo = sum(interp(s[n], day, 'inc') for n in names)
    ce = sum(interp([r[n] for r in c['rows']], day, 'earned') for n in names)
    se = sum(interp(s[n], day, 'earned') for n in names)
    trade = sum(interp([r[n] for r in c['rows']], day, 'crew') for n in names)
    print(f"day {day:2d}: crew income {crew:9.0f} solo {solo:9.0f} = {crew/solo-1:+5.0%}   earned {ce/1e6:8.1f}M vs {se/1e6:8.1f}M = {ce/se-1:+5.0%}   paid trade {trade/crew:5.1%} of income")

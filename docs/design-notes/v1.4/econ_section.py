# The worked-out economy: figures and tables generated from the model's runs (sim/out/*.json)
import json, math, os
import figs2
from figs2 import load, line_chart, stacked_bars, base_map, short

KINDS = figs2.TYPE_KIND
NAME = {
    'solarPanel': 'Solar Panel', 'windMoily': 'Wind Moily', 'hydroWheel': 'Hydro Wheel', 'coalPlant': 'Coal Plant',
    'ironMine': 'Iron Mine', 'copperMine': 'Copper Mine', 'coalPit': 'Coal Pit', 'quarry': 'Quarry',
    'sandDredger': 'Sand Dredger', 'lumberCamp': 'Lumber Camp', 'waterPump': 'Water Pump',
    'smallFactory': 'Small Factory', 'bigFactory': 'Big Factory', 'smelter': 'Smelter', 'brickworks': 'Brickworks',
    'glassworks': 'Glassworks', 'sawmill': 'Sawmill', 'bigTurbine': 'Big Turbine', 'steamTurbine': 'Steam Turbine',
    'bauxitePit': 'Bauxite Pit', 'deepMine': 'Deep Mine', 'deepMineCopper': 'Deep Mine', 'deepMineCoal': 'Deep Mine', 'steelMill': 'Steel Mill', 'wireMill': 'Wire Mill',
    'cementKiln': 'Cement Kiln', 'aluminiumWorks': 'Aluminium Works',
}


def plural(name):
    if name.endswith('y') and name[-2] not in 'aeiou':
        return name[:-1] + 'ies'
    if name.endswith('s'):
        return name
    return name + 's'


def g(v):
    """gold, written short"""
    return short(v) + ' g' if v >= 1000 else f'{v:.0f} g'


def row_at(rows, day):
    return min(rows, key=lambda r: abs(r['day'] - day))


def brow_at(rows, day):
    return min((r for r in rows if r['buildings']), key=lambda r: abs(r['day'] - day))


def first_evening():
    rows = load('first.json')
    o = ['<div class="scroll"><table class="wide">',
         '<tr><th>Minute</th><th class="num">Income a minute</th><th>Level</th><th>Buildings</th><th>What happened</th></tr>']
    notes = {0: 'Four Wind Moilies on the hill, piped to the trading post',
             10: 'Solar Panels fill the gaps; Levels 2 and 3', 20: 'Level 4: the Lab',
             30: 'Copper pipes and the Power track\'s first tier, one on each of the Lab\'s lines; the first Iron Mines',
             40: 'Level 5: the first Small Factory, the first chain', 60: 'Water Pumps and Lumber Camps',
             80: 'Copper Mines, Sawmills, more Small Factories',
             90: 'Level 6; the trading post goes to level 4: more of every Hub Order',
             120: 'Smelters; the trading post at level 8; the evening ends'}
    for r in rows:
        if r['minute'] not in notes:
            continue
        n = sum(r['buildings'].values())
        o.append(f'<tr><td>{r["minute"]}</td><td class="num">{r["income"]:,}</td><td>{r["level"]}</td><td class="num">{n}</td><td>{notes[r["minute"]]}</td></tr>')
    o.append('</table></div>')
    return '\n'.join(o)


def base_card(r, day):
    b = r['buildings'] or []
    by = {}
    for t, lvl, m, *_ in b:
        by.setdefault(NAME[t], []).append(lvl)
    top = sorted(by.items(), key=lambda kv: -len(kv[1]))[:6]
    spent = r['spent']
    tot = sum(spent.values()) or 1
    lines = [
        ('Income', f'{r["inc"]:,.0f} g a minute'),
        ('Company Level', f'{r["L"]}'),
        ('Land', f'{4 + r["parcels"]} of 16 parcels' + (f' and {r["outskirts"]} of 9 outskirts' if r['outskirts'] else '')),
        ('Buildings', f'{len(b)}, average level {sum(x[1] for x in b) / max(len(b), 1):.1f}'),
        ('Most of them', ', '.join(f'{len(ls)} {plural(t) if len(ls) > 1 else t}' for t, ls in top[:4])),
        ('Research', f'{sum(r["tiers"].values())} tiers; Iron at {r["tiers"].get("iron", 0)}' + (f', Steel at {r["tiers"].get("steel", 0)}' if r['tiers'].get('steel') else '')),
        ('Earned so far', g(r['earned'])),
        ('Spent so far', f'{sum(spent.values()) / max(r["earned"], 1):.0%} of it'),
    ]
    if r.get('monument'):
        lines.append(('Monument', f'stage {r["monument"]}'))
    return '<table class="facts">' + ''.join(f'<tr><th>{a}</th><td>{v}</td></tr>' for a, v in lines) + '</table>'


TRACK_PLOTS = {'power': 'a rich coal deposit, no copper ore', 'iron': 'a rich iron deposit, no sand',
               'copper': 'a rich copper deposit, no sand', 'stone': 'a rich stone deposit, no sand',
               'sand': 'a rich sand deposit, no stone', 'wood': 'a rich iron deposit, no sand'}


def track_gain(track, day):
    a = load(f'tracks_{track}_deep.json')['rows']
    b = load(f'tracks_{track}_wide.json')['rows']
    return row_at(a, day)['earned'] / row_at(b, day)['earned'] - 1


def tracks_table():
    """Every Era-1 track mastered against the same plot played wide"""
    days = [7, 14, 21, 35]
    o = ['<div class="scroll"><table class="wide">',
         '<tr><th>Track mastered</th><th>On a plot with</th>' + ''.join(f'<th class="num">Day {d}</th>' for d in days) + '</tr>']
    for t in ['power', 'iron', 'copper', 'stone', 'sand', 'wood']:
        o.append(f'<tr><td><b>{t.capitalize()}</b></td><td>{TRACK_PLOTS[t]}</td>'
                 + ''.join(f'<td class="num">{track_gain(t, d):+.0%}</td>' for d in days) + '</tr>')
    o.append('</table></div>')
    o.append('<p class="small muted">Gold earned by a player who researches the first two tiers of everything and '
             'then masters one track, against the same plot researching every track a tier at a time. Both do the '
             'Era-2 tracks after that, from day 12.</p>')
    return '\n'.join(o)


def tracks_spread(day):
    v = [track_gain(t, day) + 1 for t in ['power', 'iron', 'copper', 'stone', 'sand', 'wood']]
    return max(v) / min(v)


def summary_table():
    """The base at a glance, for the first page"""
    r = load('show_regular.json')['rows']
    o = ['<div class="scroll"><table>',
         '<tr><th>Day</th><th class="num">Buildings</th><th class="num">Income a minute</th><th class="num">Company Level</th><th>Land</th></tr>']
    for d in [1, 7, 14, 34]:
        x = row_at(r, d)
        land = f'{4 + x["parcels"]} of 16 parcels' + (f', {x["outskirts"]} of 9 outskirts' if x['outskirts'] else '')
        inc = f'{x["inc"]:,.0f} g' if x['inc'] < 100_000 else g(x['inc'])
        o.append(f'<tr><td>{d}</td><td class="num">{x["n"]}</td><td class="num">{inc}</td><td class="num">{x["L"]}</td><td>{land}</td></tr>')
    o.append('</table></div>')
    return '\n'.join(o)


def days_table(r):
    days = [1, 2, 3, 5, 7, 10, 14, 17, 21, 28, 34]
    o = ['<div class="scroll"><table class="wide">',
         '<tr><th>Day</th><th class="num">Income a minute</th><th>Level</th><th class="num">Buildings</th><th class="num">Avg level</th><th>Parcels</th><th class="num">Research tiers</th><th class="num">Spent of earned</th><th class="num">Bank</th><th>Gold went to</th></tr>']
    prev = None
    for d in days:
        x = row_at(r['rows'], d)
        sp = x['spent']
        diff = {k: v - (prev['spent'].get(k, 0) if prev else 0) for k, v in sp.items()}
        tot = sum(v for v in diff.values() if v > 0) or 1
        top = [kv for kv in sorted(((k, v) for k, v in diff.items() if v > 0), key=lambda kv: -kv[1])[:3] if kv[1] / tot >= 0.01]
        label = {'levels': 'levels', 'buildings': 'buildings', 'research': 'research', 'parcels': 'parcels',
                 'monument': 'Monument', 'wonder': 'Wonder', 'outskirts': 'outskirts'}
        went = ', '.join(f'{label.get(k, k)} {v / tot:.0%}' for k, v in top)
        n = None
        o.append(f'<tr{" class=\"hl\"" if d in (7, 14, 34) else ""}><td>{d}</td><td class="num">{x["inc"]:,.0f}</td><td>{x["L"]}</td><td class="num">{x["n"]}</td><td class="num">{x["avgL"]:.1f}</td>'
                 f'<td>{4 + x["parcels"]}{" + " + str(x["outskirts"]) if x["outskirts"] else ""}</td><td class="num">{sum(x["tiers"].values())}</td>'
                 f'<td class="num">{sum(sp.values()) / max(x["earned"], 1):.0%}</td><td class="num">{x["gold"] / max(x["inc"], 1) / 60:.1f} h</td><td>{went}</td></tr>')
        prev = x
    o.append('</table></div>')
    return '\n'.join(o)


def income_chart():
    series = []
    for name, col, f in [('keen (4 visits a day)', '#a12f1f', 'show_keen.json'), ('regular (3 a day)', '#236b69', 'show_regular.json'),
                         ('once a day', '#c9963a', 'show_casual.json')]:
        r = load(f)
        series.append((name, col, [(x['day'], x['inc']) for x in r['rows'] if x['inc'] > 0]))
    return line_chart(series, 'income, gold a minute (each line up is ten times the one below)', days=35, log=True, ymin=100,
                      marks=[(12, 35, 'Era 2 (the town finished its Wonder on day 12)')], label='Income over 35 days for three ways of playing')


def payback_chart():
    r = load('show_regular.json')
    pts = []
    for s in r['sessions']:
        pb = min(s['pb'], 400)
        pts.append((s['t'], max(pb, 0.05)))
    return line_chart([('best purchase', '#236b69', pts)], 'hours the best purchase takes to pay for itself, at the end of each visit',
                      days=35, log=True, ymin=0.1, ymax=400, yfmt=lambda v: f'{v:g} h' if v < 1000 else short(v),
                      marks=[(12, 35, 'Era 2')], label='How long the best purchase takes to pay back')


def spend_chart():
    r = load('show_regular.json')
    rows = []
    prev = {}
    for d in range(1, 35):
        x = row_at(r['rows'], d)
        diff = {k: max(0, v - prev.get(k, 0)) for k, v in x['spent'].items()}
        rows.append((d, diff))
        prev = x['spent']
    cats = [('buildings', 'new buildings', '#5a7fa8'), ('levels', 'levels', '#236b69'), ('research', 'research', '#8a5a9e'),
            ('parcels', 'parcels', '#6b8e3a'), ('outskirts', 'outskirts', '#a7c26a'), ('monument', 'the Monument', '#c9963a'),
            ('wonder', 'the Wonder', '#a12f1f')]
    return stacked_bars(rows, cats, label='Where a regular player\'s gold went, day by day')


def crew_chart():
    c = load('crew9.json')
    s = load('solo9.json')
    names = ['Ana', 'Ben', 'Cleo', 'Dev']
    crew = [(row['day'], sum(row[n]['inc'] for n in names) / 4) for row in c['rows']]
    solo = []
    days = sorted({r['day'] for n in names for r in s[n]})
    for d in days:
        vals = []
        for n in names:
            rr = min(s[n], key=lambda r: abs(r['day'] - d))
            vals.append(rr['inc'])
        solo.append((d, sum(vals) / 4))
    return line_chart([('a crew of four', '#236b69', crew), ('the same four alone', '#c9963a', solo, True)],
                      'income a minute per player', days=35, log=True, ymin=1000, marks=[(12, 35, 'Era 2')],
                      label='A crew of four against the same four players alone')


def crew_table():
    c = load('crew9.json')
    s = load('solo9.json')
    names = ['Ana', 'Ben', 'Cleo', 'Dev']

    def interp(rows, day, key):
        rows = sorted(rows, key=lambda r: r['day'])
        for a, b in zip(rows, rows[1:]):
            if a['day'] <= day <= b['day']:
                f = (day - a['day']) / max(b['day'] - a['day'], 1e-9)
                return a[key] + f * (b[key] - a[key])
        return rows[-1][key]
    o = ['<div class="scroll"><table class="wide">',
         '<tr><th>Day</th><th class="num">Earned by the crew</th><th class="num">By the same four alone</th><th class="num">Crew ahead</th><th class="num">Paid trade, share of income</th><th class="num">Monument stage, crew · alone</th></tr>']
    for d in [3, 7, 10, 14, 21, 28, 34]:
        ce = sum(interp([r[n] for r in c['rows']], d, 'earned') for n in names)
        se = sum(interp(s[n], d, 'earned') for n in names)
        inc = sum(interp([r[n] for r in c['rows']], d, 'inc') for n in names)
        trade = sum(interp([r[n] for r in c['rows']], d, 'crew') for n in names)
        mon = row_at([r['Ana'] | {'day': r['day']} for r in c['rows']], d)['monument']
        solo_mon = row_at(load('show_regular.json')['rows'], d)['monument']
        o.append(f'<tr><td>{d}</td><td class="num">{short(ce)}</td><td class="num">{short(se)}</td><td class="num">{ce / se - 1:+.0%}</td><td class="num">{trade / inc:.0%}</td><td class="num">{mon} · {solo_mon}</td></tr>')
    o.append('</table></div>')
    return '\n'.join(o)


def wonder_table():
    r = load('show_era1only.json')
    G = [(x['day'], x['gift_goods'] + x['gift_gold']) for x in r['rows']]

    def at(d):
        for (a, ga), (b, gb) in zip(G, G[1:]):
            if a <= d <= b:
                return ga + (gb - ga) * (d - a) / max(b - a, 1e-9)
        return G[-1][1]

    def when(t):
        for (a, ga), (b, gb) in zip(G, G[1:]):
            if ga <= t <= gb:
                return a + (b - a) * (t - ga) / max(gb - ga, 1e-9)
        return None
    W1, W2 = sizes()
    o = ['<div class="scroll"><table>', '<tr><th>Active players in the town</th>' + ''.join(f'<th class="num">{n}</th>' for n in [1, 2, 4, 6, 8, 12, 16]) + '</tr>',
         '<tr><td>Era 2 on day</td>']
    for n in [1, 2, 4, 6, 8, 12, 16]:
        d = when(W1 / n)
        o.append(f'<td class="num">{d:.0f}</td>' if d else '<td class="num">—</td>')
    o.append('</tr></table></div>')
    return '\n'.join(o)


def styles_table():
    o = ['<div class="scroll"><table class="wide">', '<tr><th>How they play</th><th class="num">Day 3</th><th class="num">Day 7</th><th class="num">Day 14</th><th class="num">Day 28</th><th>Level on day 7 · 14 · 28</th></tr>']
    for name, f in [('Keen: four visits a day', 'show_keen.json'), ('Regular: three a day', 'show_regular.json'), ('Once a day, with Max and Grow', 'show_casual.json'), ('Once a day, two things a minute', 'show_casual_apm2.json')]:
        r = load(f)['rows']
        o.append(f'<tr><td>{name}</td>' + ''.join(f'<td class="num">{row_at(r, d)["inc"]:,.0f}</td>' for d in [3, 7, 14, 28]) +
                 f'<td>{row_at(r, 7)["L"]} · {row_at(r, 14)["L"]} · {row_at(r, 28)["L"]}</td></tr>')
    o.append('</table></div>')
    return '\n'.join(o)


def maps():
    r = load('show_regular.json')
    out = {}
    for d in [1, 7, 14, 34]:
        x = brow_at(r['rows'], d)
        out[d] = (base_map(x['buildings'], x['parcels'], x['outskirts'], 'sand', 'oil', 'ironOre', title=f'The base on day {d}', legend=False), base_card(x, d))
    return out


LEVEL_UNLOCKS = {
    1: 'HQ, trading post, Solar Panel, Wind Moily, Storage, grey pipe, your stall',
    2: 'Iron Mine, Small Factory', 3: 'Trading in the Yard and your prices; daily orders', 4: 'The Lab: research, the tracks and the Era tree (copper pipes)',
    5: '5th parcel; Copper Mine, Coal Pit, Sand Dredger, Lumber Camp, Water Pump, Sawmill; the Monument',
    6: 'Smelter, Brickworks, Hydro Wheel', 7: 'Glassworks; 6th parcel', 8: 'Copper pipes for everyone', 9: '7th parcel',
    10: 'Quarry, Warehouse, Engineer\'s Workshop; Yard workshops and the Depot; first manager', 11: '8th parcel',
    12: 'Links, the Exchange, Cargo Depot, Big Factory', 13: '9th parcel', 14: 'Coal Plant', 15: '10th parcel; Recycler',
    16: 'Market Hall', 17: '11th parcel', 18: 'Weather Station', 19: '12th parcel',
    20: 'Expedition Office; second manager', 21: '13th parcel', 22: 'Blueprint Office', 23: '14th parcel',
    25: '15th parcel', 27: '16th parcel: the whole plot', 30: 'Third manager', 45: 'Fourth manager',
}


def level_gold(L):
    if L <= 1:
        return 0
    if L <= 10:
        return 500 * 3.96 ** (L - 2)
    if L <= 20:
        return 30e6 * 1.44 ** (L - 10)
    if L <= 25:
        return 30e6 * 1.44 ** 10 * 1.35 ** (L - 20)
    return 30e6 * 1.44 ** 10 * 1.35 ** 5 * 1.5 ** (L - 25)


def levels_table():
    r = load('show_regular.json')['rows']
    first = load('first.json')

    def day_of(L):
        for x in first:
            if x['level'] >= L:
                return f'minute {x["minute"]}'
        for x in r:
            if x['L'] >= L:
                return f'day {x["day"]:.0f}' if x['day'] >= 1 else 'day 1'
        return 'after day 35'
    o = ['<div class="scroll"><table class="wide">', '<tr><th>Level</th><th class="num">Gold earned in all</th><th>Typical day</th><th>Unlocks</th></tr>']
    for L, what in LEVEL_UNLOCKS.items():
        o.append(f'<tr><td>{L}</td><td class="num">{short(level_gold(L)) if L > 1 else "—"}</td><td>{day_of(L) if L > 1 else "the start"}</td><td>{what}</td></tr>')
    o.append('</table></div>')
    return '\n'.join(o)


def hub_orders_table():
    from build_names import NAMES
    P = {'energy': 1, 'water': 0.5, 'ironOre': 3, 'wood': 1.5, 'copperOre': 3.5, 'coal': 2.5, 'stone': 1.5, 'sand': 1.2,
         'ironIngot': 14, 'copperIngot': 18, 'bricks': 5, 'glass': 14, 'planks': 3.5,
         'bauxite': 8, 'steel': 60, 'copperWire': 40, 'concrete': 20, 'aluminium': 80}
    era2 = {'bauxite', 'steel', 'copperWire', 'concrete', 'aluminium'}
    o = ['<div class="scroll"><table class="wide">',
         '<tr><th>Good</th><th class="num">Base price</th><th class="num">Hub Order, trading post level 1</th><th class="num">…level 10</th><th class="num">…level 25</th><th class="num">In an Era-2 town, level 25</th></tr>',
         '<tr class="era"><td colspan="6">Era 1 · 1,000 g a minute of each good at level 1</td></tr>']
    first2 = True
    for g, p in P.items():
        if g in era2 and first2:
            o.append('<tr class="era"><td colspan="6">Era 2 · 4,000 g a minute of each good at level 1</td></tr>')
            first2 = False
        base = (4000 if g in era2 else 1000) / p
        e2 = base * (1 if g in era2 else 1.5)
        f = lambda v: f'{v:,.0f}' if v >= 10 else f'{v:.1f}'
        o.append(f'<tr><td>{NAMES[g]}</td><td class="num">{p:g} g</td><td class="num">{f(base)}</td><td class="num">{f(base * 1.1 ** 9)}</td><td class="num">{f(base * 1.1 ** 24)}</td><td class="num">{f(e2 * 1.1 ** 24)}</td></tr>')
    o.append('</table></div>')
    o.append('<p class="small muted">Units a minute. From tier 5, research multiplies them again for the goods of your own tracks (§12).</p>')
    return '\n'.join(o)


def wonders_table():
    W1, W2 = sizes()
    k = W2 / W1
    rows = [
        ('The Great Dynamo', 'Era 1 → 2. The power plant of a new age.', W1,
         ['bricks, planks, iron ingots', 'copper ingots, iron ingots, glass', 'bricks, glass, copper ingots, planks'],
         ['+5% energy', '+5% energy', 'Era 2']),
        ('The Copper Cathedral', 'Era 2 → 3. A cathedral of copper and steel.', W2,
         ['steel, concrete', 'copper wire, aluminium, glass', 'steel, concrete, copper wire, aluminium'],
         ['Hub Orders +10%', 'Hub Orders +10%', 'Era 3']),
        ('The Grand Refinery Tower', 'Era 3 → 4. Oil becomes everything.', _round2(W2 * k),
         ['plastic, steel', 'gears, motors, concrete', 'plastic, motors, aluminium, gears'],
         ['factories +5%', 'factories +5%', 'Era 4']),
        ('The Thinking Machine', 'Era 4 → 5. A machine that thinks.', _round2(W2 * k * k),
         ['circuits, batteries', 'robots, circuits, copper wire', 'robots, batteries, motors, circuits'],
         ['research 5% faster', 'research 5% faster', 'Era 5']),
        ('The Space Elevator', 'Era 5. The town\'s way to the stars.', _round2(W2 * k ** 3),
         ['titanium, superconductors, steel', 'spaceframes, robots', 'spaceframes, superconductors, titanium'],
         ['+5% everything', '+5% everything', 'the Launch']),
    ]
    o = ['<div class="scroll"><table class="wide">', '<tr><th>Wonder</th><th>Stage 1 · ⅙</th><th>Stage 2 · ⅓</th><th>Stage 3 · ½</th><th class="num">In all</th></tr>']
    for i, (name, sub, total, goods, perks) in enumerate(rows):
        cells = []
        for j, frac in enumerate([1 / 6, 1 / 3, 1 / 2]):
            pill = 'info' if j == 2 else 'good'
            cells.append(f'<td>{goods[j]}<br><span class="small">{short(total * frac)} g</span> <span class="pill {pill}">{perks[j]}</span></td>')
        note = '' if i < 2 else '<br><span class="small muted">provisional, phase C</span>'
        o.append(f'<tr{" class=\"hl\"" if i == 4 else ""}><td><b>{name}</b><br><span class="small muted">{sub}</span></td>{"".join(cells)}<td class="num">{short(total)} g{note}</td></tr>')
    o.append('</table></div>')
    return '\n'.join(o)


def era_pace_table():
    r1 = load('show_era1only.json')['rows']
    r2 = load('show_regular.json')['rows']
    G1 = [(x['day'], x['gift_goods'] + x['gift_gold']) for x in r1]
    g12 = [x['gift_goods'] + x['gift_gold'] for x in r2 if x['day'] >= 12][0]
    G2 = [(x['day'] - 12, x['gift_goods'] + x['gift_gold'] - g12) for x in r2 if x['day'] >= 12]

    def when(G, t):
        for (a, ga), (b, gb) in zip(G, G[1:]):
            if ga <= t <= gb:
                return a + (b - a) * (t - ga) / max(gb - ga, 1e-9)
        return None
    W1, W2 = sizes()
    o = ['<div class="scroll"><table>', '<tr><th>Active players in the town</th>' + ''.join(f'<th class="num">{n}</th>' for n in [1, 2, 4, 8, 16]) + '</tr>']
    e1 = {n: when(G1, W1 / n) for n in [1, 2, 4, 8, 16]}
    o.append('<tr><td>Era 2 begins on day</td>' + ''.join(f'<td class="num">{max(3, e1[n]):.0f}</td>' if e1[n] else '<td class="num">—</td>' for n in [1, 2, 4, 8, 16]) + '</tr>')
    row = '<tr><td>Era 2 lasts</td>'
    for n in [1, 2, 4, 8, 16]:
        d = when(G2, W2 / n)
        row += f'<td class="num">{max(3, d):.0f} days</td>' if d else '<td class="num">more than 3 weeks</td>'
    o.append(row + '</tr>')
    o.append('</table></div>')
    return '\n'.join(o)


def together_html():
    c = load('crew9.json')
    names = ['Ana', 'Ben', 'Cleo', 'Dev']
    x = row_at([r['Ana'] | {'day': r['day']} for r in c['rows']], 10)
    return crew_table()


def _curves():
    r1 = load('show_era1only.json')['rows']
    r2 = load('show_regular.json')['rows']
    G1 = [(x['day'], x['gift_goods'] + x['gift_gold']) for x in r1]
    g12 = [x['gift_goods'] + x['gift_gold'] for x in r2 if x['day'] >= 12][0]
    G2 = [(x['day'] - 12, x['gift_goods'] + x['gift_gold'] - g12) for x in r2 if x['day'] >= 12]
    return G1, G2


def _at(G, d):
    for (a, ga), (b, gb) in zip(G, G[1:]):
        if a <= d <= b:
            return ga + (gb - ga) * (d - a) / max(b - a, 1e-9)
    return G[-1][1]


def _when(G, t):
    for (a, ga), (b, gb) in zip(G, G[1:]):
        if ga <= t <= gb:
            return a + (b - a) * (t - ga) / max(gb - ga, 1e-9)
    return None


def _round2(v):
    e = 10 ** (math.floor(math.log10(v)) - 1)
    return round(v / e) * e


def sizes():
    """The Great Dynamo: what eight regular players give by day 12; the Copper Cathedral: by three weeks into Era 2"""
    G1, G2 = _curves()
    W1 = _round2(8 * _at(G1, 12))
    W2 = _round2(8 * _at(G2, 21))
    return W1, W2


def era2_day(n):
    G1, _ = _curves()
    W1, _ = sizes()
    d = _when(G1, W1 / n)
    return max(3, round(d)) if d else None


def words(v):
    if v >= 1e12:
        return f'{v / 1e12:g} trillion'
    if v >= 1e9:
        return f'{v / 1e9:g} billion'
    return f'{v / 1e6:g} million'

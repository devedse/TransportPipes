# Assembles docs/v1.4-design.html from the parts, the model's tables and the figures
import os
import figs
from model import P, B, val, cost

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', 'v1.4-design.html')

NAMES = {
    'energy': 'Energy', 'water': 'Water', 'ironOre': 'Iron ore', 'wood': 'Wood', 'copperOre': 'Copper ore', 'coal': 'Coal',
    'stone': 'Stone', 'sand': 'Sand', 'ironIngot': 'Iron ingots', 'copperIngot': 'Copper ingots', 'bricks': 'Bricks',
    'glass': 'Glass', 'planks': 'Planks', 'bauxite': 'Bauxite', 'steel': 'Steel', 'copperWire': 'Copper wire',
    'concrete': 'Concrete', 'aluminium': 'Aluminium', 'oil': 'Oil', 'plastic': 'Plastic', 'gears': 'Gears',
    'motors': 'Motors', 'rareEarth': 'Rare earth', 'circuits': 'Circuits', 'batteries': 'Batteries', 'robots': 'Robots',
    'titaniumOre': 'Titanium ore', 'titanium': 'Titanium', 'superconductors': 'Superconductors', 'spaceframes': 'Spaceframes',
}
ERA_OF = {
    'energy': 1, 'water': 1, 'ironOre': 1, 'wood': 1, 'copperOre': 1, 'coal': 1, 'stone': 1, 'sand': 1,
    'ironIngot': 1, 'copperIngot': 1, 'bricks': 1, 'glass': 1, 'planks': 1,
    'bauxite': 2, 'steel': 2, 'copperWire': 2, 'concrete': 2, 'aluminium': 2,
    'oil': 3, 'plastic': 3, 'gears': 3, 'motors': 3,
    'rareEarth': 4, 'circuits': 4, 'batteries': 4, 'robots': 4,
    'titaniumOre': 5, 'titanium': 5, 'superconductors': 5, 'spaceframes': 5,
}
WHERE = {
    'energy': 'made by power buildings', 'water': 'shores, on every plot', 'ironOre': 'deposits, on every plot',
    'wood': 'forests, on every plot', 'copperOre': 'deposits, on 3 of 4 plots', 'coal': 'deposits, on 3 of 4 plots',
    'stone': 'hills and deposits, on 3 of 4 plots', 'sand': 'beaches, on 3 of 4 plots',
    'bauxite': 'red hills, on 3 of 4 plots', 'oil': 'deserts, on 3 of 4 plots', 'rareEarth': 'hills, on 3 of 4 plots',
    'titaniumOre': 'mountain feet, on 3 of 4 plots',
}
ERA_NAMES = {1: 'Era 1 · Steam & Iron', 2: 'Era 2 · Electric', 3: 'Era 3 · Oil & Machines', 4: 'Era 4 · Electronics',
             5: 'Era 5 · Space Age'}
UNLOCK = {'Solar Panel': 'Level 1', 'Wind Moily': 'Level 1', 'Hydro Wheel': 'Level 6', 'Coal Plant': 'Level 14',
          'Iron Mine': 'Level 2', 'Copper Mine': 'Level 5', 'Coal Pit': 'Level 5', 'Quarry': 'Level 10',
          'Sand Dredger': 'Level 5', 'Lumber Camp': 'Level 5', 'Water Pump': 'Level 5', 'Small Factory': 'Level 2',
          'Big Factory': 'Level 12 + research', 'Smelter': 'Level 6', 'Brickworks': 'Level 6', 'Glassworks': 'Level 7',
          'Sawmill': 'Level 5', 'Deep Mine': 'Era 2 + research'}
TRACK = {'Solar Panel': 'Power', 'Wind Moily': 'Power', 'Hydro Wheel': 'Power', 'Coal Plant': 'Power',
         'Big Turbine': 'Power', 'Steam Turbine': 'Power', 'Solar Farm': 'Power', 'Geothermal Plant': 'Power',
         'Fusion Reactor': 'Power', 'Water Pump': 'Power', 'Iron Mine': 'Iron', 'Small Factory': 'Iron',
         'Big Factory': 'Iron', 'Deep Mine': 'its ore', 'Copper Mine': 'Copper', 'Smelter': 'Copper', 'Coal Pit': 'Coal',
         'Quarry': 'Stone', 'Brickworks': 'Stone', 'Sand Dredger': 'Sand', 'Glassworks': 'Sand', 'Lumber Camp': 'Wood',
         'Sawmill': 'Wood', 'Steel Mill': 'Steel', 'Wire Mill': 'Wire', 'Cement Kiln': 'Concrete',
         'Bauxite Pit': 'Aluminium', 'Aluminium Works': 'Aluminium', 'Oil Derrick': 'Oil', 'Refinery': 'Oil',
         'Machine Shop': 'Gears', 'Motor Works': 'Motors', 'Rare Earth Excavator': 'Rare Earth',
         'Circuit Fab': 'Circuits', 'Battery Works': 'Batteries', 'Robot Works': 'Robots',
         'Titanium Mine': 'Titanium', 'Titanium Works': 'Titanium', 'Superconductor Lab': 'Superconductors',
         'Spaceframe Works': 'Spaceframes'}
NOTE = {'Solar Panel': ' ×sun', 'Wind Moily': ' ×wind', 'Big Turbine': ' ×wind', 'Solar Farm': ' ×sun',
        'Hydro Wheel': ' (shore)', 'Deep Mine': ' (any ★3+ metal or coal)'}
SIZE = {1: '1×1', 4: '2×2', 9: '3×3'}


def num(x):
    if x >= 1_000_000:
        v = x / 1_000_000
        return (f'{v:.0f}' if v >= 10 else f'{v:.1f}'.rstrip('0').rstrip('.')) + ' million'
    return f'{x:,.0f}' if x >= 100 or x == int(x) else f'{x:g}'


SINGULAR = {'superconductors': 'superconductor', 'robots': 'robot', 'motors': 'motor', 'circuits': 'circuit',
            'batteries': 'battery', 'spaceframes': 'spaceframe', 'gears': 'gear'}


def amounts(d):
    return ', '.join(f'{num(v)} {SINGULAR.get(k, NAMES[k].lower()) if v == 1 else NAMES[k].lower()}' for k, v in d.items())


def mat(x):
    if x < 100:
        return round(x, -1)
    if x < 1000:
        return round(x / 50) * 50
    from model import nice
    return nice(x)


def resources_table():
    o = ['<div class="scroll"><table>', '<tr><th>Good</th><th class="num">Base price</th><th>Where it comes from</th></tr>']
    made = {}
    for name, era, size, kind, i, out in B:
        for k in out:
            made.setdefault(k, name)
    for era in range(1, 6):
        o.append(f'<tr class="era"><td colspan="3">{ERA_NAMES[era]}</td></tr>')
        for k, p in P.items():
            if ERA_OF[k] != era:
                continue
            src = WHERE.get(k) or made[k]
            if k not in WHERE:
                for name, e, size, kind, i, out in B:
                    if k in out and name == made[k]:
                        src = f'{name}, from {amounts({a: b for a, b in i.items() if a != "energy"}) or "energy"}'
                        break
            o.append(f'<tr><td>{NAMES[k]}</td><td class="num">{num(p)} g</td><td>{src}</td></tr>')
    o.append('</table></div>')
    return '\n'.join(o)


def buildings_table():
    o = ['<div class="scroll"><table>',
         '<tr><th>Building</th><th>Size</th><th>Unlock</th><th>Takes → makes, a minute</th><th class="num">Adds a tile</th><th>Cost</th><th>Track</th></tr>']
    for era in range(1, 6):
        o.append(f'<tr class="era"><td colspan="7">{ERA_NAMES[era]}</td></tr>')
        for b in B:
            name, e, size, kind, i, out = b
            if e != era:
                continue
            va = (val(out) - val(i)) / size
            gold, mats = cost(b)
            c = f'{num(gold)} g' + (''.join(f' + {num(mat(v))} {NAMES[k].lower()}' for k, v in mats.items()))
            flow = f'{amounts(i)} → {amounts(out)}' if i else f'makes {amounts(out)}'
            o.append(f'<tr><td><b>{name}</b>{NOTE.get(name, "")}</td><td>{SIZE[size]}</td><td>{UNLOCK.get(name, f"Era {e}")}</td>'
                     f'<td>{flow}</td><td class="num">{va:,.0f} g</td><td>{c}</td><td>{TRACK[name]}</td></tr>')
    o.append('</table></div>')
    return '\n'.join(o)


html = ''.join(open(os.path.join(HERE, f'part{n}.html')).read() for n in range(1, 5))
for name in ['world', 'town', 'group', 'yard', 'ladder', 'track', 'robot']:
    html = html.replace('{{FIG:' + name + '}}', getattr(figs, name + '_svg')())
import base64
ALT = {
    'lanes-cutaway': 'The big grey pipe opened up: power cables, a small conveyor belt and a slim water pipe run inside it',
    'lanes-windmill': 'Copper cables on tiny posts run from the pipe to a Wind Moily',
    'lanes-mine': 'A small conveyor belt carries iron ore from a mine into the pipe',
    'lanes-pump': 'A slim blue pipe with a valve wheel branches from the big pipe to a water pump',
    'lanes-coalplant': 'A coal plant with a coal belt, a water pipe and power cables coming out of the pipe',
}
for key, alt in ALT.items():
    data = base64.b64encode(open(os.path.join(HERE, 'img', key + '.webp'), 'rb').read()).decode()
    html = html.replace('{{IMG:' + key + '}}', f'<img src="data:image/webp;base64,{data}" alt="{alt}" loading="lazy">')
html = html.replace('{{TABLE:resources}}', resources_table())

# ---- the worked-out economy: everything below comes from the model's runs in sim/out
import figs2
import econ_section as ec
W1, W2 = ec.sizes()
reg = ec.load('show_regular.json')['rows']
crew = ec.load('crew9.json')
solo = ec.load('solo9.json')
names4 = ['Ana', 'Ben', 'Cleo', 'Dev']


def interp(rows, day, key):
    rows = sorted(rows, key=lambda r: r['day'])
    for a, b in zip(rows, rows[1:]):
        if a['day'] <= day <= b['day']:
            f = (day - a['day']) / max(b['day'] - a['day'], 1e-9)
            return a[key] + f * (b[key] - a[key])
    return rows[-1][key]


def crew_ahead(day):
    ce = sum(interp([r[n] for r in crew['rows']], day, 'earned') for n in names4)
    se = sum(interp(solo[n], day, 'earned') for n in names4)
    return ce / se - 1


def first_day(pred):
    for x in reg:
        if pred(x):
            return x['day']
    return None


lv = [first_day(lambda x, L=L: x['L'] >= L) for L in (10, 20, 25, 30)]
master = first_day(lambda x: x['tiers'].get('iron', 0) >= 9)
r5 = min(reg, key=lambda x: abs(x['day'] - 5)); r34 = min(reg, key=lambda x: abs(x['day'] - 34))
spent_late = (sum(r34['spent'].values()) - sum(r5['spent'].values())) / (r34['earned'] - r5['earned'])
banks = [x['gold'] / max(x['inc'], 1) / 60 for x in reg if x['day'] >= 5 and abs(x['day'] - round(x['day'])) < 0.01]
trade_shares = []
for r in crew['rows']:
    if r['day'] < 3:
        continue
    inc = sum(r[n]['inc'] for n in names4)
    trade_shares.append(sum(r[n]['crew'] for n in names4) / max(inc, 1))
pace = {
    'levels': ' · '.join(f'day {d:.0f}' if d else 'later' for d in lv),
    'master': f'day {master:.1f}' if master else 'later',
    'era2': 'day 12, which is how the Great Dynamo is sized',
    'spent': f'{spent_late:.0%} of everything earned from day 5 to day 34',
    'bank': f'{min(banks):.1f}–{max(banks):.1f} hours of income from day 5',
    'crew': f'{crew_ahead(7):+.0%} by day 7, {crew_ahead(14):+.0%} by day 14, {crew_ahead(28):+.0%} by day 28',
}
for k, v in pace.items():
    html = html.replace('{{PACE:' + k + '}}', v)
html = html.replace('{{W1TEXT}}', ec.words(W1))
for n in (1, 2, 4, 8, 16):
    html = html.replace('{{E2:' + str(n) + '}}', str(ec.era2_day(n)))
html = html.replace('{{FIG:curve}}', figs2.curve_svg())
html = html.replace('{{TABLE:hubOrders}}', ec.hub_orders_table())
html = html.replace('{{TABLE:wonders}}', ec.wonders_table())
html = html.replace('{{TABLE:levels}}', ec.levels_table())
html = html.replace('{{TABLE:erapace}}', ec.era_pace_table())
lo, hi = max(0.01, min(trade_shares)), max(trade_shares)
html = html.replace('{{TRADEWORTH}}', f"""<p>In the model (§17), paid trade between four friends in a Yard is {lo:.0%} to {hi:.0%} of their income:
    a neighbour's coal or sand in the first days, goods for levels from the second week, Era 2's chains after. The gold
    that changes hands is small next to what the market pays. What it buys is not: a Smelter that loses 27 g a minute on
    market copper ore earns 20 g on a friend's, and the glass for a level costs a plot without sand less than half as
    much from a neighbour (1.15×) as from the market (2.5×). Together with the Yard's Monument it puts a crew
    {crew_ahead(14):.0%} ahead of the same players alone by day 14 and {crew_ahead(28):.0%} by day 28.</p>""")
html = html.replace('{{TOGETHER}}', ec.crew_table())
maps = ec.maps()
for d, (svg, card) in maps.items():
    html = html.replace('{{EC:map' + str(d) + '}}', svg).replace('{{EC:card' + str(d) + '}}', card)
html = html.replace('{{EC:legend}}', figs2.map_legend())
for k, f in [('first', ec.first_evening), ('income', ec.income_chart), ('payback', ec.payback_chart), ('spend', ec.spend_chart),
             ('crew', ec.crew_chart), ('crewtable', ec.crew_table), ('wonder', ec.wonder_table), ('styles', ec.styles_table)]:
    html = html.replace('{{EC:' + k + '}}', f())
html = html.replace('{{EC:days}}', ec.days_table(ec.load('show_regular.json')))
html = html.replace('{{TABLE:buildings}}', buildings_table())
assert '{{' not in html, html[html.index('{{') - 100:html.index('{{') + 100]
open(OUT, 'w').write(html)
print(os.path.normpath(OUT), len(html))

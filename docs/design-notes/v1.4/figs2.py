# Charts and base maps for the worked-out economy, drawn from the model's runs (sim/out/*.json)
import json, math, os
from figs import text, rect, INK, TEAL, BRASS, MUTED, FONT

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, 'sim', 'out')
GRID = '#e6dcc4'
COLORS = ['#236b69', '#c9963a', '#a12f1f', '#5a7fa8', '#6b8e3a', '#8a5a9e']


def load(name):
    return json.load(open(os.path.join(OUT, name)))


def short(v):
    a = abs(v)
    if a >= 1e15:
        return f'{v / 1e15:,.0f} quadrillion'
    if a >= 1e12:
        return f'{v / 1e12:.0f}T' if a >= 1e13 else f'{v / 1e12:.1f}T'.replace('.0T', 'T')
    if a >= 1e9:
        return f'{v / 1e9:.0f}B' if a >= 1e10 else f'{v / 1e9:.1f}B'.replace('.0B', 'B')
    if a >= 1e6:
        return f'{v / 1e6:.0f}M' if a >= 1e7 else f'{v / 1e6:.1f}M'.replace('.0M', 'M')
    if a >= 1e3:
        return f'{v / 1e3:.0f}k'
    return f'{v:.0f}'


def line_chart(series, title_y, days=35, log=True, ymin=None, ymax=None, width=900, height=330, marks=None, yfmt=short, label=None):
    """series: [(name, colour, [(day, value)], dashed)]"""
    L, R, T, B = 64, 150, 18, 40
    pw, ph = width - L - R, height - T - B
    vals = [v for _, _, pts, *_ in series for _, v in pts if v > 0 or not log]
    lo = ymin if ymin is not None else (min(vals) if vals else 1)
    hi = ymax if ymax is not None else max(vals)
    if log:
        lo, hi = math.log10(max(lo, 1)), math.log10(hi)
    X = lambda d: L + pw * d / days
    Y = lambda v: T + ph * (1 - ((math.log10(max(v, 1e-9)) if log else v) - lo) / (hi - lo))
    o = [f'<svg viewBox="0 0 {width} {height}" role="img" aria-label="{label or title_y}">']
    o.append(rect(0, 0, width, height, '#fbf5e7'))
    # grid
    if log:
        e = math.floor(lo)
        while e <= hi:
            for m in (1, 3):
                v = m * 10 ** e
                if lo <= math.log10(v) <= hi:
                    y = Y(v)
                    o.append(f'<line x1="{L}" y1="{y:.1f}" x2="{L + pw}" y2="{y:.1f}" stroke="{GRID}" stroke-width="1"/>')
                    o.append(text(L - 6, y + 4, yfmt(v), 10, 700, MUTED, 'end'))
            e += 1
    else:
        steps = 5
        for i in range(steps + 1):
            v = lo + (hi - lo) * i / steps
            y = Y(v)
            o.append(f'<line x1="{L}" y1="{y:.1f}" x2="{L + pw}" y2="{y:.1f}" stroke="{GRID}" stroke-width="1"/>')
            o.append(text(L - 6, y + 4, yfmt(v), 10, 700, MUTED, 'end'))
    for d in range(0, days + 1, 7 if days > 14 else 1):
        x = X(d)
        o.append(f'<line x1="{x:.1f}" y1="{T}" x2="{x:.1f}" y2="{T + ph}" stroke="{GRID}" stroke-width="1"/>')
        o.append(text(x, T + ph + 16, f'day {d}', 10, 700, MUTED, 'middle'))
    for (d0, d1, name) in (marks or []):
        o.append(rect(X(d0), T, X(d1) - X(d0), ph, '#e4efe9', extra='opacity="0.7"'))
        o.append(text((X(d0) + X(d1)) / 2, T + ph - 8, name, 10, 800, TEAL, 'middle'))
    o.append(f'<line x1="{L}" y1="{T + ph}" x2="{L + pw}" y2="{T + ph}" stroke="{MUTED}" stroke-width="1.2"/>')
    o.append(f'<line x1="{L}" y1="{T}" x2="{L}" y2="{T + ph}" stroke="{MUTED}" stroke-width="1.2"/>')
    o.append(text(L, T + ph + 32, title_y, 10.5, 800, TEAL))
    # lines and labels at their ends, kept apart
    ends = []
    for i, (name, col, pts, *rest) in enumerate(series):
        dashed = rest[0] if rest else False
        pts = [(d, v) for d, v in pts if d <= days and (v > 0 or not log)]
        if not pts:
            continue
        path = 'M' + ' L'.join(f'{X(d):.1f} {Y(v):.1f}' for d, v in pts)
        o.append(f'<path d="{path}" fill="none" stroke="{col}" stroke-width="2.4" stroke-linejoin="round"{" stroke-dasharray=\"6 4\"" if dashed else ""}/>')
        ends.append([Y(pts[-1][1]), name, col])
    ends.sort()
    for i in range(1, len(ends)):
        if ends[i][0] - ends[i - 1][0] < 14:
            ends[i][0] = ends[i - 1][0] + 14
    for y, name, col in ends:
        o.append(text(L + pw + 8, y + 4, name, 11, 800, col))
    o.append('</svg>')
    return '\n'.join(o)


def stacked_bars(rows, cats, width=900, height=300, label='Where the gold went, day by day'):
    """rows: [(day, {cat: gold})]; shares per day"""
    L, R, T, B = 64, 150, 14, 40
    pw, ph = width - L - R, height - T - B
    n = len(rows)
    bw = pw / n
    o = [f'<svg viewBox="0 0 {width} {height}" role="img" aria-label="{label}">']
    o.append(rect(0, 0, width, height, '#fbf5e7'))
    for i in range(0, 101, 25):
        y = T + ph * (1 - i / 100)
        o.append(f'<line x1="{L}" y1="{y:.1f}" x2="{L + pw}" y2="{y:.1f}" stroke="{GRID}" stroke-width="1"/>')
        o.append(text(L - 6, y + 4, f'{i}%', 10, 700, MUTED, 'end'))
    for j, (day, spent) in enumerate(rows):
        tot = sum(spent.get(c, 0) for c, _, _ in cats) or 1
        y = T + ph
        for c, name, col in cats:
            h = ph * spent.get(c, 0) / tot
            o.append(rect(L + j * bw + 1, y - h, bw - 2, h, col))
            y -= h
        if day % 7 == 0 or n <= 14:
            o.append(text(L + j * bw + bw / 2, T + ph + 16, f'day {day}', 10, 700, MUTED, 'middle'))
    for i, (c, name, col) in enumerate(reversed(cats)):
        y = T + 14 + i * 18
        o.append(rect(L + pw + 10, y - 9, 11, 11, col, extra='rx="2"'))
        o.append(text(L + pw + 26, y, name, 11, 700, INK))
    o.append('</svg>')
    return '\n'.join(o)


# ---- base maps
KIND = {
    'power': '#e9b93a', 'mine': '#9a6a45', 'factory': '#3f8f8a', 'era2': '#6e5aa8', 'post': '#c9963a', 'hq': '#173c3f',
}
TYPE_KIND = {
    'solarPanel': 'power', 'windMoily': 'power', 'hydroWheel': 'power', 'coalPlant': 'power', 'bigTurbine': 'era2',
    'steamTurbine': 'era2', 'ironMine': 'mine', 'copperMine': 'mine', 'coalPit': 'mine', 'quarry': 'mine',
    'sandDredger': 'mine', 'lumberCamp': 'mine', 'waterPump': 'mine', 'bauxitePit': 'era2', 'deepMine': 'era2', 'deepMineCopper': 'era2', 'deepMineCoal': 'era2',
    'smallFactory': 'factory', 'bigFactory': 'factory', 'smelter': 'factory', 'brickworks': 'factory',
    'glassworks': 'factory', 'sawmill': 'factory', 'steelMill': 'era2', 'wireMill': 'era2', 'cementKiln': 'era2',
    'aluminiumWorks': 'era2',
}
TERRAIN = {'general': '#cfe0b0', 'hill': '#c2c98a', 'forest': '#7fa65a', 'shore': '#8fc4dc', 'desert': '#ead9a2',
           'beach': '#efe1b0'}
DEP = {'ironOre': '#c0603a', 'copperOre': '#e8893a', 'coal': '#3a3a40', 'stone': '#9aa1a8', 'sand': '#e6cf8f',
       'bauxite': '#b5523a'}


def base_map(buildings, parcels_owned, outskirts_owned, lacks, lacks2, strength, scale=11, title='', legend=True):
    """A plot drawn tile by tile: owned parcels, the terrain they hold, the buildings on them by kind"""
    import sys
    sys.path.insert(0, os.path.join(HERE, 'sim'))
    from plot import make_land, make_outskirts, PARCEL
    from content import B
    start, parcels = make_land(lacks, lacks2, strength)
    outs = make_outskirts(lacks, lacks2)
    # where the parcels lie on the 4x4 grid (start: the bottom middle, against the Yard), in buying order
    order = [(1, 2), (2, 2), (1, 3), (2, 3)]
    grow = [(0, 2), (3, 2), (1, 1), (2, 1), (0, 3), (3, 3), (0, 1), (3, 1), (1, 0), (2, 0), (0, 0), (3, 0)]
    owned = [('start', start, order)]
    for i in range(parcels_owned):
        owned.append((parcels[i][0], parcels[i][1], [grow[i]]))
    cells = {}

    def pools_of(d):
        out = {}
        for k, v in d.items():
            out[k] = sum(e[0] for e in v) if isinstance(v, list) else v
        return out

    # the start area's pools spread over its four parcels: a deposit, the pond, the forest, the hill
    sp = pools_of(start)
    dep_keys = [k for k in sp if k.startswith('dep:')]
    quarters = [{}, {}, {}, {}]
    quarters[0][dep_keys[0]] = sp[dep_keys[0]]
    if len(dep_keys) > 1:
        quarters[1][dep_keys[1]] = sp[dep_keys[1]]
    quarters[1]['shore'] = sp.get('shore', 0)
    quarters[2]['forest'] = sp.get('forest', 0)
    quarters[3]['hill'] = sp.get('hill', 0)
    gen = sp.get('general', 0)
    for q in quarters:
        q['general'] = max(0, 49 - sum(q.values()))
    owned = [(f'start{i}', quarters[i], [order[i]]) for i in range(4)]
    for i in range(parcels_owned):
        owned.append((parcels[i][0], pools_of(parcels[i][1]), [grow[i]]))
    ogrid = [(-1, 3), (-1, 2), (-2, 3), (-2, 2), (-1, 1), (-2, 1), (-3, 3), (-3, 2), (-3, 1)]
    for i in range(outskirts_owned):
        owned.append((outs[i][0], pools_of(outs[i][1]), [ogrid[i]]))
    # in a parcel: a cross of pipes, and each pool as a patch, deposits and water first
    local = []
    for qx0, qx1, qy0, qy1 in [(0, 3, 0, 3), (4, 8, 0, 3), (0, 3, 4, 8), (4, 8, 4, 8)]:
        quad = [(x, y) for y in range(qy0, qy1) for x in range(qx0, qx1)]
        cx = 0 if qx0 == 0 else 7
        cy = 0 if qy0 == 0 else 7
        quad.sort(key=lambda t: abs(t[0] - cx) + abs(t[1] - cy))
        local += quad
    rank = lambda k: ['dep:', 'shore', 'forest', 'hill', 'desert', 'general'].index(next((p for p in ['dep:', 'shore', 'forest', 'hill', 'desert', 'general'] if k.startswith(p)), 'general'))
    for name, pools, spots in owned:
        (px, py) = spots[0]
        kinds = []
        for k, v in sorted(pools.items(), key=lambda kv: rank(kv[0])):
            kinds += [k] * v
        kinds = (kinds + ['general'] * 49)[:49]
        for (lx, ly), k in zip(local, kinds):
            cells[(px * 8 + lx, py * 8 + ly)] = k
        for i in range(8):
            cells[(px * 8 + 3, py * 8 + i)] = 'pipe'
            cells[(px * 8 + i, py * 8 + 3)] = 'pipe'
    # place buildings onto cells of their pool
    free = {}
    for t, k in cells.items():
        free.setdefault(k, []).append(t)
    for k in free:
        free[k].sort(key=lambda t: (abs(t[0] % 8 - 3) + abs(t[1] % 8 - 3), t[1], t[0]))
    placed = []
    pref = {'wind': ['hill', 'general', 'desert'], 'sun': ['desert', 'general', 'hill'], 'any': ['general', 'desert', 'hill'],
            'shore': ['shore'], 'forest': ['forest']}
    for t, lvl, m, *_ in sorted(buildings, key=lambda b: -B[b[0]]['size']):
        d = B[t]
        pools = [d['place']] if d['place'].startswith('dep:') else pref[d['place']]
        size = d['size']
        done = False
        for pool in pools:
            cand = free.get(pool, [])
            if size == 1 and cand:
                placed.append((cand.pop(0), 1, t, lvl))
                done = True
                break
            if size > 1:
                s = 2 if size == 4 else 3
                for c in list(cand):
                    block = [(c[0] + dx, c[1] + dy) for dx in range(s) for dy in range(s)]
                    if all(b in cand for b in block):
                        for b in block:
                            if b in cand:
                                cand.remove(b)
                            for k in free:
                                if b in free[k]:
                                    free[k].remove(b)
                        placed.append((c, s, t, lvl))
                        done = True
                        break
                if done:
                    break
        if not done:
            for pool in ['general', 'desert', 'hill']:
                cand = free.get(pool, [])
                if cand:
                    placed.append((cand.pop(0), 1, t, lvl))
                    break
    s = scale
    OX = 24 * s if outskirts_owned else 0
    W, H = OX + 32 * s + (230 if legend else 6), 32 * s + 8 * s + 6
    o = [f'<svg viewBox="0 0 {W} {H}" role="img" aria-label="{title}">']
    o.append(rect(0, 0, W, H, '#fbf5e7'))
    o.append(f'<g transform="translate({OX},0)">')
    o.append(rect(0, 0, 32 * s, 32 * s, '#e6ecd8'))
    if outskirts_owned:
        o.append(rect(-24 * s, 8 * s, 24 * s, 24 * s, '#efe6c8', '#d9c99a', 1))
        o.append(text(-12 * s, 6 * s, 'your outskirts (Era 2)', 11, 800, BRASS, 'middle'))
    for (x, y), k in cells.items():
        col = TERRAIN.get(k, '#cfe0b0') if not k.startswith('dep:') else DEP.get(k[4:], '#999')
        if k == 'pipe':
            col = '#d9d2c0'
        o.append(rect(x * s, y * s, s, s, col))
    for (x, y), size, t, lvl in placed:
        col = KIND[TYPE_KIND[t]]
        pad = 1.2 if size == 1 else 1.6
        op = 0.35 + 0.65 * min(lvl, 25) / 25
        o.append(rect(x * s + pad, y * s + pad, size * s - 2 * pad, size * s - 2 * pad, col, '#2b2418', 0.4, f'rx="1.5" opacity="{op:.2f}"'))
    # unowned parcels
    ownedcells = set(cells)
    for i in range(outskirts_owned, 9):
        cx, cy = ogrid[i]
        o.append(rect(cx * 8 * s, cy * 8 * s, 8 * s, 8 * s, '#efe8d6', '#d8ccae', 0.8))
    for cy in range(4):
        for cx in range(4):
            if (cx * 8, cy * 8) not in ownedcells:
                o.append(rect(cx * 8 * s, cy * 8 * s, 8 * s, 8 * s, '#efe8d6', '#d8ccae', 0.8))
    for i in range(5):
        o.append(f'<line x1="{i * 8 * s}" y1="0" x2="{i * 8 * s}" y2="{32 * s}" stroke="#b8a985" stroke-width="0.8"/>')
        o.append(f'<line x1="0" y1="{i * 8 * s}" x2="{32 * s}" y2="{i * 8 * s}" stroke="#b8a985" stroke-width="0.8"/>')
    o.append(rect(0, 32 * s, 32 * s, 8 * s, '#e3cf9e', '#94692a', 1))
    o.append(text(16 * s, 32 * s + 4.5 * s, 'the Yard', 11, 800, TEAL, 'middle'))
    o.append('</g>')
    if not legend:
        o.append('</svg>')
        return '\n'.join(o)
    # legend
    lx = OX + 32 * s + 18
    ly = 14
    for k, name in [('power', 'power'), ('mine', 'mines, pumps, camps'), ('factory', 'factories'), ('era2', 'Era 2 buildings')]:
        o.append(rect(lx, ly - 9, 11, 11, KIND[k], extra='rx="2"'))
        o.append(text(lx + 16, ly, name, 11, 700, INK))
        ly += 18
    ly += 4
    o.append(text(lx, ly, 'darker = higher level', 10, 700, MUTED))
    ly += 22
    for k, name in [('hill', 'hills'), ('forest', 'forest'), ('shore', 'water and shore'), ('desert', 'desert')]:
        o.append(rect(lx, ly - 9, 11, 11, TERRAIN[k], extra='rx="2"'))
        o.append(text(lx + 16, ly, name, 11, 700, INK))
        ly += 18
    for g in ['ironOre', 'copperOre', 'coal', 'stone', 'sand', 'bauxite']:
        if g == lacks:
            continue
        o.append(rect(lx, ly - 9, 11, 11, DEP[g], extra='rx="2"'))
        o.append(text(lx + 16, ly, {'ironOre': 'iron ore', 'copperOre': 'copper ore'}.get(g, g) + ' deposit', 11, 700, INK))
        ly += 18
    o.append(rect(lx, ly - 9, 11, 11, '#efe8d6', '#d8ccae', 0.8))
    o.append(text(lx + 16, ly, 'not bought yet', 11, 700, INK))
    o.append('</svg>')
    return '\n'.join(o)


def curve_svg():
    """What the market pays against sales, both in units of the Hub Order"""
    W, H = 900, 320
    L, R, T, B = 64, 40, 18, 46
    pw, ph = W - L - R, H - T - B
    xmax, ymax = 20, 6
    X = lambda v: L + pw * v / xmax
    Y = lambda v: T + ph * (1 - v / ymax)
    pay = lambda s: s if s <= 1 else (math.sqrt(s) if s <= 16 else 4 + 0.25 * (s - 16))
    o = [f'<svg viewBox="0 0 {W} {H}" role="img" aria-label="What the market pays against how much you sell">']
    o.append(rect(0, 0, W, H, '#fbf5e7'))
    for v in range(0, ymax + 1):
        o.append(f'<line x1="{L}" y1="{Y(v):.1f}" x2="{L + pw}" y2="{Y(v):.1f}" stroke="{GRID}"/>')
        o.append(text(L - 6, Y(v) + 4, f'{v}×', 10, 700, MUTED, 'end'))
    for v in [0, 1, 2, 4, 8, 12, 16, 20]:
        o.append(f'<line x1="{X(v):.1f}" y1="{T}" x2="{X(v):.1f}" y2="{T + ph}" stroke="{GRID}"/>')
        o.append(text(X(v), T + ph + 16, f'{v}×', 10, 700, MUTED, 'middle'))
    o.append(rect(X(0), T, X(1) - X(0), ph, '#e4efe9', extra='opacity="0.8"'))
    o.append(rect(X(16), T, X(20) - X(16), ph, '#f6d3cc', extra='opacity="0.5"'))
    o.append(text((X(0) + X(1)) / 2, T + 14, 'full', 10, 800, TEAL, 'middle'))
    o.append(text((X(1) + X(16)) / 2, T + 14, 'square root', 10, 800, TEAL, 'middle'))
    o.append(text((X(16) + X(20)) / 2, T + 14, 'a quarter', 10, 800, '#a12f1f', 'middle'))
    full = 'M' + ' L'.join(f'{X(s):.1f} {Y(min(s, ymax)):.1f}' for s in [0, ymax])
    o.append(f'<path d="{full}" fill="none" stroke="{MUTED}" stroke-width="1.5" stroke-dasharray="5 4"/>')
    o.append(text(X(4.4) - 6, Y(4.7), '- - - if the market paid', 10, 700, MUTED, 'end'))
    o.append(text(X(4.4) - 6, Y(4.7) + 13, 'the full price for all', 10, 700, MUTED, 'end'))
    pts = [i / 20 for i in range(0, 401)]
    path = 'M' + ' L'.join(f'{X(s):.1f} {Y(pay(s)):.1f}' for s in pts)
    o.append(f'<path d="{path}" fill="none" stroke="#236b69" stroke-width="3"/>')
    for s, lab in [(1, '1×'), (4, '2×'), (16, '4×')]:
        o.append(f'<circle cx="{X(s):.1f}" cy="{Y(pay(s)):.1f}" r="4" fill="#c9963a" stroke="{TEAL}"/>')
        o.append(text(X(s) + 7, Y(pay(s)) - 7, f'sell {s}× → paid {lab}', 10.5, 800, TEAL))
    o.append(text(L, T + ph + 34, 'what you sell a minute, in Hub Orders → what the market pays, in Hub Orders', 10.5, 800, TEAL))
    o.append('</svg>')
    return '\n'.join(o)


def map_legend():
    """One legend for all the base maps, as text that wraps on a phone"""
    items = [(KIND['power'], 'power'), (KIND['mine'], 'mines, pumps, camps'), (KIND['factory'], 'factories'),
             (KIND['era2'], 'Era 2 buildings'), (TERRAIN['hill'], 'hills'), (TERRAIN['forest'], 'forest'),
             (TERRAIN['shore'], 'water and shore'), (DEP['ironOre'], 'iron ore'), (DEP['copperOre'], 'copper ore'),
             (DEP['coal'], 'coal'), (DEP['stone'], 'stone'), (DEP['bauxite'], 'bauxite'), ('#efe8d6', 'not bought yet')]
    return '<div class="legend">' + ''.join(f'<span><i style="background:{c}"></i>{n}</span>' for c, n in items) + \
        '<span class="muted">· darker = higher level</span></div>'

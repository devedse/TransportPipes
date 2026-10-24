# SVG figures for the v1.4 design, drawn to scale from the layout rules
import math

INK = '#2b2418'; TEAL = '#173c3f'; BRASS = '#94692a'; MUTED = '#6b5c43'
WILD = '#a9c48a'; ROAD = '#b9ab8e'; PLOT = '#cfe0b0'; START = '#7fae5a'; YARD = '#e3cf9e'; HUB = '#cdb68a'
FONT = 'font-family="Nunito, sans-serif"'; SERIF = 'font-family="Fraunces, Georgia, serif"'


def text(x, y, s, size=11, weight=700, fill=INK, anchor='start', serif=False):
    return (f'<text x="{x:.1f}" y="{y:.1f}" {SERIF if serif else FONT} font-size="{size}" font-weight="{weight}" '
            f'fill="{fill}" text-anchor="{anchor}">{s}</text>')


def rect(x, y, w, h, fill, stroke=None, sw=1, extra=''):
    st = f' stroke="{stroke}" stroke-width="{sw}"' if stroke else ''
    return f'<rect x="{x:.1f}" y="{y:.1f}" width="{w:.1f}" height="{h:.1f}" fill="{fill}"{st} {extra}/>'


HATCH = ('<pattern id="{id}" width="7" height="7" patternUnits="userSpaceOnUse" patternTransform="rotate(45)">'
         '<rect width="7" height="7" fill="#efe6c8"/><line x1="0" y1="0" x2="0" y2="7" stroke="#d9c99a" stroke-width="3"/></pattern>')
PAVE = ('<pattern id="{id}" width="{s}" height="{s}" patternUnits="userSpaceOnUse"><rect width="{s}" height="{s}" fill="#e3cf9e"/>'
        '<path d="M{s} 0H0V{s}" fill="none" stroke="#d2bb85" stroke-width="1"/></pattern>')


# A Yard group in tiles, relative to its corner: (x, y, w, h)
def group_rects(gx, gy):
    return {
        'N': (gx + 32, gy + 0, 32, 32), 'S': (gx + 32, gy + 48, 32, 32),
        'W': (gx + 0, gy + 24, 32, 32), 'E': (gx + 64, gy + 24, 32, 32),
        'yard': (gx + 32, gy + 32, 32, 16),
    }


# The start area of each plot (16x16), against the Yard
START_OFF = {'N': (8, 16), 'S': (8, 0), 'W': (16, 8), 'E': (0, 8)}
GROUPS = [(0, 0), (100, 0), (0, 84), (100, 84)]
NAMES = ['Ana', 'Ben', 'Cleo', 'Dev', 'Eli', 'Fay', 'Gus', 'Hal', 'Ivy', 'Jo', 'Kai', 'Lin', 'Mo', 'Nia', 'Oz', 'Pia']


def town_svg():
    s = 3.0; m = 12  # px per tile, wilderness margin in tiles
    W = (196 + 2 * m) * s; H = (164 + 2 * m) * s + 54
    o = []
    o.append(f'<svg viewBox="0 0 {W:.0f} {H:.0f}" role="img" aria-label="A town of four Yard groups drawn to scale">')
    o.append('<defs>' + HATCH.format(id='hatchT') + '</defs>')
    o.append(rect(0, 0, W, H - 54, WILD))
    X = lambda t: (t + m) * s
    # outskirts (everything inside the town that isn't plot, Yard, hub or road)
    o.append(rect(X(0), X(0), 196 * s, 164 * s, 'url(#hatchT)', BRASS, 1))
    # roads
    o.append(rect(X(96), X(0), 4 * s, 164 * s, ROAD))
    o.append(rect(X(0), X(80), 196 * s, 4 * s, ROAD))
    # road stubs into the wilderness and the four town gates
    o.append(rect(X(96), X(-m), 4 * s, m * s, ROAD))
    o.append(rect(X(96), X(164), 4 * s, m * s, ROAD))
    o.append(rect(X(-m), X(80), m * s, 4 * s, ROAD))
    o.append(rect(X(196), X(80), m * s, 4 * s, ROAD))
    for (gx, gy, w, h) in [(94, -2, 8, 4), (94, 162, 8, 4), (-2, 78, 4, 8), (194, 78, 4, 8)]:
        o.append(rect(X(gx), X(gy), w * s, h * s, '#c9963a', TEAL, 1.5, 'rx="2"'))
    o.append(text(X(104), X(-4) + 2, 'town gate', 10, 800, TEAL))
    # hub
    o.append(rect(X(64), X(56), 68 * s, 52 * s, HUB, '#7a5a2a', 2))
    for k in range(1, 4):
        o.append(f'<line x1="{X(64):.1f}" y1="{X(56 + 13 * k):.1f}" x2="{X(132):.1f}" y2="{X(56 + 13 * k):.1f}" stroke="#b89d6c" stroke-width="1.2"/>')
        o.append(f'<line x1="{X(64 + 17 * k):.1f}" y1="{X(56):.1f}" x2="{X(64 + 17 * k):.1f}" y2="{X(108):.1f}" stroke="#b89d6c" stroke-width="1.2"/>')
    o.append(rect(X(91), X(73), 14 * s, 18 * s, '#f4ead3', TEAL, 1.5))
    o.append(text(X(98), X(80) + 2, 'Wonder', 9, 800, TEAL, 'middle'))
    o.append(text(X(98), X(80) + 12, 'site', 9, 800, TEAL, 'middle'))
    o.append(text(X(75), X(62) + 2, 'the hub', 12, 800, TEAL, 'start', True))
    o.append(text(X(75), X(102), 'market', 9.5, 700, TEAL))
    # plots, start areas, Yards, gates
    n = 0
    for gi, (gx, gy) in enumerate(GROUPS):
        r = group_rects(gx, gy)
        for side in ['N', 'W', 'E', 'S']:
            x, y, w, h = r[side]
            o.append(rect(X(x), X(y), w * s, h * s, PLOT, '#8aa86a', 1))
            sx, sy = START_OFF[side]
            o.append(rect(X(x + sx), X(y + sy), 16 * s, 16 * s, START))
            lx = X(x + 16); ly = X(y + (6 if side != 'N' else 6))
            if side == 'S':
                ly = X(y + 28)
            o.append(text(lx, ly + 2, NAMES[n], 10, 800, INK, 'middle'))
            n += 1
        x, y, w, h = r['yard']
        o.append(rect(X(x), X(y), w * s, h * s, YARD, BRASS, 1.5))
        o.append(text(X(x + 16), X(y + 8) + 4, 'Yard', 11, 800, BRASS, 'middle', True))
        g = 1.5 * s
        for (px, py) in [(x + 16, y), (x + 16, y + 16), (x, y + 8), (x + 32, y + 8)]:
            o.append(rect(X(px) - g / 2, X(py) - g / 2, g, g, TEAL))
    # labels for the outskirts and road
    o.append(text(X(16), X(12) + 3, 'outskirts', 9.5, 800, BRASS, 'middle'))
    o.append(text(X(80), X(12) + 3, 'outskirts', 9.5, 800, BRASS, 'middle'))
    o.append(text(X(16), X(68) + 3, 'outskirts', 9.5, 800, BRASS, 'middle'))
    o.append(text(X(98) + 4, X(140), 'road', 9, 700, '#5a4a30', 'start'))
    o.append(text(X(-10), X(-4) + 1, 'wilderness: 128 tiles to the next town', 10, 700, '#2f4a1f'))
    # legend
    rows = [[(START, 'start area 16×16'), (PLOT, 'parcels to buy'), (YARD, 'the Yard 32×16')],
            [('url(#hatchT)', 'outskirts (Era 2)'), (HUB, 'the hub'), (ROAD, 'road')]]
    for r_, items in enumerate(rows):
        ly = H - 46 + r_ * 22
        x = 8
        for fill, label in items:
            o.append(rect(x, ly, 12, 12, fill, '#8a7a5a', 0.8))
            o.append(text(x + 17, ly + 10, label, 10.5, 600))
            x += 150
    ly = H - 24
    o.append(f'<line x1="{W - 8 - 32 * s:.1f}" y1="{ly}" x2="{W - 8:.1f}" y2="{ly}" stroke="{INK}" stroke-width="2"/>')
    o.append(text(W - 8 - 16 * s, ly + 15, '32 tiles', 10, 700, INK, 'middle'))
    o.append('</svg>')
    return '\n'.join(o)


LAND = {
    'N': ('Ana', 'copper ore', 'titanium ore'), 'W': ('Ben', 'stone', 'oil'),
    'E': ('Cleo', 'sand', 'bauxite'), 'S': ('Dev', 'coal', 'rare earth'),
}
OWNC = {'N': '#e9c46a', 'W': '#8ecae6', 'E': '#f4a3a8', 'S': '#b5a1e0'}


def group_svg():
    s = 5.0; pad = 8
    W = 96 * s + 2 * pad; H = 80 * s + 2 * pad
    X = lambda t: pad + t * s
    o = [f'<svg viewBox="0 0 {W:.0f} {H:.0f}" role="img" aria-label="One Yard group: four plots, what each lacks, and their outskirts">']
    o.append(rect(0, 0, W, H, '#fbf5e7'))
    # outskirts allotments: (patch x, y) parcels -> owner
    allot = {}
    for c in range(4):
        for r_ in range(3):
            allot[(0 + 8 * c, 0 + 8 * r_)] = 'N' if c >= 2 else 'W'          # corner patch
            allot[(64 + 8 * c, 0 + 8 * r_)] = 'N' if c == 0 else 'E'         # top-middle patch
            allot[(0 + 8 * c, 56 + 8 * r_)] = 'W' if c == 0 else 'S'         # left-middle patch
    for (x, y), who in allot.items():
        o.append(rect(X(x) + 1, X(y) + 1, 8 * s - 2, 8 * s - 2, OWNC[who], None, 1, 'opacity="0.55"'))
    o.append(rect(X(64), X(56), 32 * s, 24 * s, HUB, '#7a5a2a', 1.5))
    o.append(text(X(80), X(68) + 4, 'the hub', 12, 800, TEAL, 'middle', True))
    r = group_rects(0, 0)
    for side in ['N', 'W', 'E', 'S']:
        x, y, w, h = r[side]
        o.append(rect(X(x), X(y), w * s, h * s, PLOT, '#6f8f52', 1.2))
        for k in range(1, 4):
            o.append(f'<line x1="{X(x + 8 * k):.1f}" y1="{X(y):.1f}" x2="{X(x + 8 * k):.1f}" y2="{X(y + 32):.1f}" stroke="#b6cc98" stroke-width="0.8"/>')
            o.append(f'<line x1="{X(x):.1f}" y1="{X(y + 8 * k):.1f}" x2="{X(x + 32):.1f}" y2="{X(y + 8 * k):.1f}" stroke="#b6cc98" stroke-width="0.8"/>')
        sx, sy = START_OFF[side]
        o.append(rect(X(x + sx), X(y + sy), 16 * s, 16 * s, START, None, 1, 'opacity="0.9"'))
        name, lack1, lack2 = LAND[side]
        cx = X(x + 16)
        ty = X(y + (3 if side != 'S' else 22)) + 6
        if side == 'N':
            ty = X(y + 3) + 6
        o.append(rect(cx - 66, ty - 11, 132, 46, '#fffaf0', OWNC[side], 3, 'rx="6" opacity="0.96"'))
        o.append(text(cx, ty + 3, name, 12, 800, TEAL, 'middle', True))
        o.append(text(cx, ty + 16, f'lacks {lack1}', 10, 700, '#a12f1f', 'middle'))
        o.append(text(cx, ty + 28, f'and {lack2}', 10, 700, '#a12f1f', 'middle'))
    x, y, w, h = r['yard']
    o.append(rect(X(x), X(y), w * s, h * s, YARD, BRASS, 2))
    o.append(text(X(x + 16), X(y + 8) + 5, 'Yard', 13, 800, BRASS, 'middle', True))
    g = 2.2 * s
    for (px, py) in [(x + 16, y), (x + 16, y + 16), (x, y + 8), (x + 32, y + 8)]:
        o.append(rect(X(px) - g / 2, X(py) - g / 2, g, g, TEAL))
    o.append(text(X(16), X(12) + 3, 'outskirts', 10, 800, BRASS, 'middle'))
    o.append(text(X(80), X(12) + 3, 'outskirts', 10, 800, BRASS, 'middle'))
    o.append(text(X(16), X(68) + 3, 'outskirts', 10, 800, BRASS, 'middle'))
    o.append('</svg>')
    return '\n'.join(o)


def yard_svg():
    s = 17.0; padx = 2; pady = 2  # tiles of plot shown around the Yard
    W = (32 + 2 * padx) * s; H = (16 + 2 * pady) * s
    X = lambda t: (t + padx) * s
    Y = lambda t: (t + pady) * s
    o = [f'<svg viewBox="0 0 {W:.0f} {H:.0f}" role="img" aria-label="The Yard close up: stalls, Monument sites, the Depot and workshop space">']
    o.append('<defs>' + PAVE.format(id='paveY', s=int(s)) + HATCH.format(id='hatchY') + '</defs>')
    o.append(rect(0, 0, W, H, PLOT))
    o.append(rect(X(0), Y(0), 32 * s, 16 * s, 'url(#paveY)', BRASS, 2.5))
    for x0 in (0, 24):
        o.append(rect(X(x0) + 3, Y(0) + 3, 8 * s - 6, 16 * s - 6, 'none', '#7a5a2a', 1.5, 'stroke-dasharray="6 5" rx="6"'))
        o.append(text(X(x0 + 4), Y(1.6), 'workshop space', 11, 800, '#7a5a2a', 'middle'))
    for (x, y, w, h, c, lab) in [(1.5, 3, 2, 2, '#c0603a', 'Ben’s mill'), (4.5, 11, 2, 2, '#3a78a8', 'Dev’s kiln'),
                                 (25.5, 3, 2, 2, '#8e6ab0', 'Ana’s wire'), (28.5, 11, 2, 2, '#6f9a3a', 'Cleo’s smelter')]:
        o.append(rect(X(x), Y(y), w * s, h * s, c, INK, 1, 'rx="3"'))
        o.append(text(X(x + w / 2), Y(y + h + 0.8), lab, 9.5, 700, INK, 'middle'))
    # gates, pipes from the end gates to the stalls
    g = 1.2 * s
    for (gx, gy) in [(16, 0), (16, 16), (0, 8), (32, 8)]:
        o.append(rect(X(gx) - g / 2, Y(gy) - g / 2, g, g, TEAL))
    o.append(f'<path d="M{X(0.6):.1f} {Y(8):.1f} H{X(8.4):.1f}" stroke="#6b5c43" stroke-width="5" stroke-linecap="round"/>')
    o.append(f'<path d="M{X(31.4):.1f} {Y(8):.1f} H{X(23.6):.1f}" stroke="#6b5c43" stroke-width="5" stroke-linecap="round"/>')
    stalls = {'N': (15, 0.6), 'S': (15, 13.4), 'W': (8.4, 7), 'E': (21.6, 7)}
    who = {'N': ('Ana', 'energy 1.15', '#e9c46a'), 'S': ('Dev', 'copper ore 1.2', '#b5a1e0'),
           'W': ('Ben', 'iron ingots 1.1', '#8ecae6'), 'E': ('Cleo', 'glass 1.25', '#f4a3a8')}
    for k, (sx, sy) in stalls.items():
        name, sell, col = who[k]
        o.append(rect(X(sx), Y(sy), 2 * s, 2 * s, col, INK, 1.2, 'rx="4"'))
        o.append(f'<path d="M{X(sx):.1f} {Y(sy) + 6:.1f} h{2 * s:.1f}" stroke="{INK}" stroke-width="1" />')
        lab = f'{name} · {sell}'
        if k == 'N':
            lx, ly, anc = X(sx + 2.4), Y(sy + 1.3), 'start'
        elif k == 'S':
            lx, ly, anc = X(sx - 0.4), Y(sy + 1.3), 'end'
        elif k == 'W':
            lx, ly, anc = X(sx + 1), Y(sy - 0.45), 'middle'
        else:
            lx, ly, anc = X(sx + 1), Y(sy - 0.45), 'middle'
        wtxt = len(lab) * 5.9 + 12
        bx = lx - (wtxt / 2 if anc == 'middle' else (6 if anc == 'start' else wtxt - 6))
        o.append(rect(bx, ly - 12, wtxt, 17, '#fffaf0', None, 1, 'rx="8" opacity="0.92"'))
        o.append(text(lx, ly, lab, 10, 800, INK, anc))
    # the first Monument in the middle, four more sites
    o.append(rect(X(14), Y(5.5), 4 * s, 4 * s, '#f4ead3', TEAL, 2, 'rx="6"'))
    o.append(text(X(16), Y(7.4), 'Grand', 10.5, 800, TEAL, 'middle', True))
    o.append(text(X(16), Y(8.4), 'Fountain', 10.5, 800, TEAL, 'middle', True))
    for (x, y) in [(9.5, 2.5), (9.5, 10.5), (19.5, 2.5), (19.5, 10.5)]:
        o.append(rect(X(x), Y(y), 3 * s, 3 * s, 'url(#hatchY)', BRASS, 1.2, 'stroke-dasharray="4 3" rx="4"'))
    o.append(text(X(11), Y(12.3), 'next', 9.5, 800, BRASS, 'middle'))
    o.append(text(X(11), Y(13.1), 'Monument', 9.5, 800, BRASS, 'middle'))
    # the Depot
    o.append(rect(X(19.5), Y(13.6), 2 * s, 2 * s, '#9aa1a8', INK, 1.2, 'rx="3"'))
    o.append(text(X(22), Y(14.9), 'Depot', 10, 800, INK, 'start'))
    for (x, y) in [(13.2, 4.6), (18.8, 4.6), (13.2, 10.4), (18.8, 10.4)]:
        o.append(f'<circle cx="{X(x):.1f}" cy="{Y(y):.1f}" r="5.5" fill="#33761f"/>')
    for (x, y) in [(14, 10.6), (18, 10.6), (14, 5.0), (18, 5.0)]:
        o.append(f'<circle cx="{X(x):.1f}" cy="{Y(y):.1f}" r="3" fill="#c9963a"/>')
    o.append(text(X(-1.8), Y(-0.9), 'Ana’s plot', 10, 700, '#4b6a2f'))
    o.append(text(X(-1.8), Y(16.95), 'Dev’s plot', 10, 700, '#4b6a2f'))
    o.append(text(X(-1.7), Y(8) - 14, 'Ben', 10, 700, '#4b6a2f'))
    o.append(text(X(33.7), Y(8) - 14, 'Cleo', 10, 700, '#4b6a2f', 'end'))
    o.append('</svg>')
    return '\n'.join(o)



ERAS = [
    ('1', 'Steam & Iron', 25, 'seven basics', 'Great Dynamo'),
    ('2', 'Electric', 100, 'bauxite', 'Copper Cathedral'),
    ('3', 'Oil & Machines', 400, 'oil', 'Grand Refinery Tower'),
    ('4', 'Electronics', 1600, 'rare earth', 'Thinking Machine'),
    ('5', 'Space Age', 6400, 'titanium ore', 'Space Elevator'),
]


def ladder_svg():
    W, H = 680, 318
    o = [f'<svg viewBox="0 0 {W} {H}" role="img" aria-label="The five Eras as steps, each about four times the last">']
    base = 250; step = 40; bw = 118
    for i, (n, name, va, raw, wonder) in enumerate(ERAS):
        x = 16 + i * (bw + 14)
        h = 46 + i * step
        y = base - h
        col = ['#7fae5a', '#3a78a8', '#5a3a6e', '#2f8a5a', '#24575a'][i]
        o.append(rect(x, y, bw, h, col, None, 1, 'rx="8"'))
        o.append(text(x + 10, y + 20, f'Era {n}', 13, 800, '#fff', 'start', True))
        o.append(text(x + 10, y + 36, name, 10.5, 700, '#f4ead3'))
        o.append(text(x + bw / 2, base + 18, f'{va:,} g', 12, 800, INK, 'middle'))
        o.append(text(x + bw / 2, base + 33, f'new: {raw}', 10, 600, MUTED, 'middle'))
        o.append(text(x + bw / 2, y - 8, wonder, 10, 800, BRASS, 'middle'))
        if i:
            o.append(text(x - 7, base - h + 60, '×4', 12, 800, '#a12f1f', 'middle'))
    o.append(text(16, 308, 'Value a factory adds per tile per minute at level 1 (g), the Era’s new raw resource, and the Wonder that leads out of it', 10.5, 600, MUTED))
    o.append('</svg>')
    return '\n'.join(o)


TIERS = [  # minutes, label, multiplier after
    (10, 'Faster ×1.25', 1.25), (60, 'Leaner −20%', 1.25), (240, 'Faster ×1.5', 1.875), (720, 'Complexes', 1.875),
    (1440, 'Faster ×2', 3.75), (2160, 'Leaner −30%', 3.75), (2880, 'Faster ×2', 7.5), (2880, 'Mega complexes', 7.5),
    (3600, 'Master ×2', 15)]


def track_svg():
    W, H = 680, 290
    L, R, T, B = 52, 660, 26, 220
    days = 10
    X = lambda m: L + (m / 1440) / days * (R - L)
    Ylog = lambda v: B - math.log(v, 15) * (B - T)
    o = [f'<svg viewBox="0 0 {W} {H}" role="img" aria-label="One research track: nine tiers over about ten days">']
    o.append(rect(L, T, R - L, B - T, '#fffaf0', '#e2d6b8', 1))
    for d in range(days + 1):
        x = L + d / days * (R - L)
        o.append(f'<line x1="{x:.1f}" y1="{T}" x2="{x:.1f}" y2="{B}" stroke="#eadfc4" stroke-width="1"/>')
        o.append(text(x, B + 16, f'{d}', 10.5, 700, MUTED, 'middle'))
    o.append(text((L + R) / 2, B + 32, 'days of research', 11, 700, MUTED, 'middle'))
    for v, lab in [(1, '×1'), (1.875, '×1.9'), (3.75, '×3.75'), (7.5, '×7.5'), (15, '×15')]:
        y = Ylog(v)
        o.append(f'<line x1="{L}" y1="{y:.1f}" x2="{R}" y2="{y:.1f}" stroke="#eadfc4" stroke-width="1"/>')
        o.append(text(L - 8, y + 4, lab, 10.5, 700, MUTED, 'end'))
    # step line
    t = 0; mult = 1; pts = [(X(0), Ylog(1))]
    marks = []
    for i, (mins, lab, after) in enumerate(TIERS):
        t += mins
        pts.append((X(t), Ylog(mult)))
        mult = after
        pts.append((X(t), Ylog(mult)))
        marks.append((X(t), Ylog(mult), i + 1, lab))
    pts.append((X(days * 1440), Ylog(mult)))
    o.append('<polyline fill="none" stroke="#24575a" stroke-width="3" points="' + ' '.join(f'{x:.1f},{y:.1f}' for x, y in pts) + '"/>')
    for x, y, n, lab in marks:
        o.append(f'<circle cx="{x:.1f}" cy="{y:.1f}" r="5" fill="#c9963a" stroke="#fff" stroke-width="1.5"/>')
    # labels for the big tiers
    for x, y, n, lab in marks[4:]:
        o.append(text(x - 6, y - 9, f'{n} · {lab}', 10, 800, TEAL, 'end'))
    o.append(text(X(20), Ylog(1.875) - 12, 'tiers 1–4: day one', 10, 800, TEAL))
    o.append(text(L, H - 6, 'The track’s buildings make this many times more (log scale). Halfway (×7.5) after about five days, master (×15) after about ten.', 10.5, 600, MUTED))
    o.append('</svg>')
    return '\n'.join(o)


def robot_svg():
    W, H = 700, 360
    cols = [16, 132, 248, 364, 480, 590]
    bw, bh = 100, 24
    o = [f'<svg viewBox="0 0 {W} {H}" role="img" aria-label="What one robot needs: every land resource">']
    nodes = {
        'iron ore': (0, 30, 'raw'), 'coal': (0, 64, 'raw'), 'copper ore': (0, 98, 'raw'), 'sand': (0, 132, 'raw'),
        'bauxite': (0, 166, 'raw'), 'oil': (0, 200, 'raw'), 'water': (0, 234, 'raw'), 'rare earth': (0, 268, 'raw'),
        'iron ingots': (1, 40, 1), 'copper ingots': (1, 98, 1), 'glass': (1, 150, 1),
        'steel': (2, 40, 2), 'copper wire': (2, 98, 2), 'aluminium': (2, 166, 2),
        'gears': (3, 40, 3), 'plastic': (3, 217, 3),
        'motors': (4, 70, 3), 'circuits': (4, 150, 4), 'batteries': (4, 240, 4),
        'robots': (5, 150, 4),
    }
    edges = [('iron ore', 'iron ingots'), ('iron ingots', 'steel'), ('coal', 'steel'), ('steel', 'gears'),
             ('copper ore', 'copper ingots'), ('coal', 'copper ingots'), ('copper ingots', 'copper wire'),
             ('bauxite', 'aluminium'), ('oil', 'plastic'), ('water', 'plastic'), ('sand', 'glass'),
             ('gears', 'motors'), ('copper wire', 'motors'), ('aluminium', 'motors'),
             ('copper wire', 'circuits'), ('plastic', 'circuits'), ('glass', 'circuits'),
             ('rare earth', 'batteries'), ('aluminium', 'batteries'),
             ('motors', 'robots'), ('circuits', 'robots'), ('batteries', 'robots')]
    colr = {'raw': '#efe6c8', 1: '#d6eccd', 2: '#dde9f3', 3: '#eadcf3', 4: '#d3efe2'}
    pos = {k: (cols[c], y) for k, (c, y, _) in nodes.items()}
    for a, b in edges:
        ax, ay = pos[a]; bx, by = pos[b]
        x1 = ax + bw; y1 = ay + bh / 2; x2 = bx; y2 = by + bh / 2
        mx = (x1 + x2) / 2
        o.append(f'<path d="M{x1:.1f} {y1:.1f} C{mx:.1f} {y1:.1f} {mx:.1f} {y2:.1f} {x2:.1f} {y2:.1f}" fill="none" stroke="#b9a77f" stroke-width="1.6"/>')
    for k, (c, y, kind) in nodes.items():
        x = cols[c]
        lacking = kind == 'raw' and k not in ('iron ore', 'water')
        stroke = '#a12f1f' if lacking else '#8a7a5a'
        o.append(rect(x, y, bw, bh, colr[kind], stroke, 1.6 if lacking else 1, 'rx="7"'))
        o.append(text(x + bw / 2, y + 16, k, 11, 800 if k == 'robots' else 700, INK, 'middle'))
    o.append(text(cols[0] + bw / 2, 16, 'land', 10.5, 800, BRASS, 'middle'))
    lx = 16
    for kind, lab in [(1, 'Era 1 good'), (2, 'Era 2'), (3, 'Era 3'), (4, 'Era 4')]:
        o.append(rect(lx, 306, 14, 14, colr[kind], '#8a7a5a', 0.8, 'rx="3"'))
        o.append(text(lx + 20, 317, lab, 10.5, 700, INK))
        lx += 110
    o.append(text(16, 342, 'Red: the six land resources one plot or another lacks. A robot needs all of them, so no plot makes', 10.5, 600, MUTED))
    o.append(text(16, 356, 'one alone: it takes a Yard, the market or the outskirts.', 10.5, 600, MUTED))
    o.append('</svg>')
    return '\n'.join(o)


def world_svg():
    s = 0.7; M = 64; G = 128; TW, TH = 196, 164
    W = (M * 2 + TW * 3 + G * 2) * s; H = (M * 2 + TH * 2 + G) * s + 48
    X = lambda t: t * s
    o = [f'<svg viewBox="0 0 {W:.0f} {H:.0f}" role="img" aria-label="Six towns to scale with 128 tiles of wilderness between them">']
    o.append('<defs>' + HATCH.format(id='hatchW') + '</defs>')
    o.append(rect(0, 0, W, H - 48, WILD))
    # a river, mountains and forests in the wilderness
    o.append(f'<path d="M{X(0):.1f} {X(420):.1f} C{X(180):.1f} {X(380):.1f} {X(250):.1f} {X(470):.1f} {X(420):.1f} {X(430):.1f} '
             f'S{X(560):.1f} {X(300):.1f} {X(650):.1f} {X(330):.1f} S{X(800):.1f} {X(250):.1f} {X(972):.1f} {X(280):.1f}" '
             'fill="none" stroke="#6ec6ff" stroke-width="5" stroke-linecap="round" opacity="0.9"/>')
    for (mx, my) in [(668, 60), (690, 40), (712, 66), (735, 48), (700, 95), (745, 88)]:
        o.append(f'<path d="M{X(mx - 14):.1f} {X(my + 12):.1f} L{X(mx):.1f} {X(my - 14):.1f} L{X(mx + 14):.1f} {X(my + 12):.1f} Z" fill="#9aa1a8" stroke="#5b6168" stroke-width="1"/>')
        o.append(f'<path d="M{X(mx - 4):.1f} {X(my - 6):.1f} L{X(mx):.1f} {X(my - 14):.1f} L{X(mx + 4):.1f} {X(my - 6):.1f} Z" fill="#fff"/>')
    for i in range(40):
        fx = (37 + i * 83) % 972; fy = (21 + i * 157) % 584
        o.append(f'<circle cx="{X(fx):.1f}" cy="{X(fy):.1f}" r="{3 + i % 3}" fill="#5f8f45" opacity="0.8"/>')
    towns = [('Capital', 3), ('Ironvale', 2), ('Copperbrook', 1), ('Saltmere', 2), ('Ashford', 4), ('Newgate', 1)]
    gates = {}
    labels = []
    for k, (name, era) in enumerate(towns):
        c, r_ = k % 3, k // 3
        tx = M + c * (TW + G); ty = M + r_ * (TH + G)
        o.append(rect(X(tx), X(ty), TW * s, TH * s, 'url(#hatchW)', BRASS, 1))
        o.append(rect(X(tx + 96), X(ty), 4 * s, TH * s, ROAD))
        o.append(rect(X(tx), X(ty + 80), TW * s, 4 * s, ROAD))
        o.append(rect(X(tx + 64), X(ty + 56), 68 * s, 52 * s, HUB))
        for (gx, gy) in GROUPS:
            rr = group_rects(tx + gx, ty + gy)
            for side in ['N', 'W', 'E', 'S']:
                x, y, w, h = rr[side]
                o.append(rect(X(x), X(y), w * s, h * s, PLOT, '#8aa86a', 0.5))
            x, y, w, h = rr['yard']
            o.append(rect(X(x), X(y), w * s, h * s, YARD))
        gates[name] = {'N': (tx + 98, ty), 'S': (tx + 98, ty + TH), 'W': (tx, ty + 82), 'E': (tx + TW, ty + 82)}
        for (gx, gy) in gates[name].values():
            o.append(rect(X(gx) - 3.5, X(gy) - 3.5, 7, 7, '#c9963a', TEAL, 1.2, 'rx="1.5"'))
        labels.append(rect(X(tx) + 2, X(ty) - 17, len(name) * 6.4 + 52, 15, '#fffaf0', None, 1, 'rx="7"'))
        labels.append(text(X(tx) + 8, X(ty) - 6, f'{name} · Era {era}', 10, 800, TEAL))
    # a trunk line (1.4, Era 3): Capital east gate to Ironvale west gate
    (ax, ay), (bx, by) = gates['Capital']['E'], gates['Ironvale']['W']
    o.append(f'<path d="M{X(ax) + 4:.1f} {X(ay):.1f} H{X(bx) - 4:.1f}" stroke="#4a4f57" stroke-width="4" stroke-linecap="round"/>')
    o.append(text(X((ax + bx) / 2), X(ay) - 7, 'trunk line', 9.5, 800, '#2b2418', 'middle'))
    o.append(text(X((ax + bx) / 2), X(ay) + 15, '(a Yard’s own, Era 3)', 9, 700, '#2b2418', 'middle'))
    # a carrier line (idea): Ironvale south gate, around the mountains, to Copperbrook's west gate and on to Ashford's north gate
    (sx, sy) = gates['Ironvale']['S']; (cx, cy) = gates['Copperbrook']['W']; (nx, ny) = gates['Ashford']['N']
    path = f'M{X(sx):.1f} {X(sy) + 4:.1f} V{X(sy + 40):.1f} H{X(cx - 50):.1f} V{X(cy):.1f} H{X(cx) - 4:.1f}'
    o.append(f'<path d="{path}" fill="none" stroke="#7a3fd0" stroke-width="4" stroke-dasharray="9 5" stroke-linecap="round"/>')
    o.append(f'<path d="M{X(sx):.1f} {X(sy + 40):.1f} V{X(ny) - 4:.1f}" fill="none" stroke="#7a3fd0" stroke-width="4" stroke-dasharray="9 5"/>')
    for (px, py) in [(sx, sy + 40), (cx - 50, sy + 40), (cx - 50, cy + 30)]:
        o.append(f'<circle cx="{X(px):.1f}" cy="{X(py):.1f}" r="4.5" fill="#fffaf0" stroke="#7a3fd0" stroke-width="2"/>')
    o.append(rect(X(sx + 8), X(sy + 46), 168, 30, '#fffaf0', '#7a3fd0', 1.2, 'rx="8" opacity="0.95"'))
    o.append(text(X(sx + 8) + 8, X(sy + 46) + 13, 'a carrier line (idea): one player’s', 9.5, 800, '#5a2ea0'))
    o.append(text(X(sx + 8) + 8, X(sy + 46) + 25, 'lossless line, rented by others', 9.5, 800, '#5a2ea0'))
    # outposts on rich deposits (Era 4) and a caravan camp (idea)
    for (ox, oy) in [(296, 520), (318, 500), (640, 470), (930, 300)]:
        o.append(rect(X(ox), X(oy), 8 * s + 2, 8 * s + 2, '#c0603a', INK, 1))
        o.append(f'<circle cx="{X(ox) + 3:.1f}" cy="{X(oy) - 5:.1f}" r="2.4" fill="#f6c744" stroke="{INK}" stroke-width="0.6"/>')
    o.append(text(X(334), X(512) + 3, 'outposts (Era 4)', 9.5, 800, '#2b2418'))
    o.append(f'<path d="M{X(784) - 8:.1f} {X(300) + 7:.1f} L{X(784):.1f} {X(300) - 8:.1f} L{X(784) + 8:.1f} {X(300) + 7:.1f} Z" fill="#e9c46a" stroke="{INK}" stroke-width="1"/>')
    o.append(text(X(784), X(300) + 20, 'caravan camp (idea)', 9.5, 800, '#2b2418', 'middle'))
    o.extend(labels)
    # the distance between two towns
    y = X(M + TH + G / 2) + 26
    x1 = X(M + TW); x2 = X(M + TW + G)
    o.append(f'<path d="M{x1:.1f} {y:.1f} H{x2:.1f}" stroke="{INK}" stroke-width="1.5" marker-start="url(#tick)"/>')
    o.append(f'<path d="M{x1:.1f} {y - 5:.1f} v10 M{x2:.1f} {y - 5:.1f} v10" stroke="{INK}" stroke-width="1.5"/>')
    o.append(text((x1 + x2) / 2, y - 6, '128 tiles', 10, 800, INK, 'middle'))
    # legend
    ly = H - 34
    items = [('#c9963a', 'town gate'), ('#4a4f57', 'trunk line (Era 3)'), ('#7a3fd0', 'carrier line (idea)'), ('#c0603a', 'outpost (Era 4)')]
    x = 8
    for fill, label in items:
        o.append(rect(x, ly, 12, 12, fill, INK, 0.8, 'rx="2"'))
        o.append(text(x + 17, ly + 10, label, 10.5, 600))
        x += 160
    o.append(text(8, H - 6, 'Six towns to scale (0.7 px a tile). The towns are 1.4; the carrier line and the camp are ideas.', 10.5, 600, MUTED))
    o.append('</svg>')
    return '\n'.join(o)


if __name__ == '__main__':
    import sys
    for name, f in [('world', world_svg), ('town', town_svg), ('group', group_svg), ('yard', yard_svg), ('ladder', ladder_svg), ('track', track_svg), ('robot', robot_svg)]:
        open(f'/tmp/claude-1000/-home-remoteman-work-admin-transportpipes/6e7119ed-0f1b-4a6b-9bc9-e2a691323912/scratchpad/v14/fig-{name}.svg', 'w').write(
            f.__call__().replace('<svg ', '<svg xmlns="http://www.w3.org/2000/svg" ', 1))
    print('ok')

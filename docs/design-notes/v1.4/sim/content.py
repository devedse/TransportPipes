# The 1.4 content for the balance model: Eras 1 and 2. Every number here is a design number the doc states.
import math

PRICE = {
    'energy': 1, 'water': 0.5, 'ironOre': 3, 'wood': 1.5, 'copperOre': 3.5, 'coal': 2.5, 'stone': 1.5, 'sand': 1.2,
    'ironIngot': 14, 'copperIngot': 18, 'bricks': 5, 'glass': 14, 'planks': 3.5,
    'bauxite': 8, 'steel': 60, 'copperWire': 40, 'concrete': 20, 'aluminium': 80,
}
ERA_OF_GOOD = {g: (2 if g in ('bauxite', 'steel', 'copperWire', 'concrete', 'aluminium') else 1) for g in PRICE}

# name: era, size (tiles), gold, materials, in, out, unlock level, track, place
# place: wind | sun | shore | dep:<good> | forest | any
B = {
    'solarPanel':   dict(era=1, size=1, gold=30,    inp={}, out={'energy': 5}, unlock=1, track='power', place='sun'),
    'windMoily':    dict(era=1, size=1, gold=200,   inp={}, out={'energy': 30}, unlock=1, track='power', place='wind'),
    'hydroWheel':   dict(era=1, size=1, gold=1200,  inp={}, out={'energy': 40}, unlock=6, track='power', place='shore'),
    'coalPlant':    dict(era=1, size=4, gold=6000,  inp={'coal': 10, 'water': 20}, out={'energy': 200}, unlock=14, track='power', place='any'),
    'ironMine':     dict(era=1, size=1, gold=600,   inp={'energy': 10}, out={'ironOre': 12}, unlock=2, track='iron', place='dep:ironOre'),
    'copperMine':   dict(era=1, size=1, gold=900,   inp={'energy': 12}, out={'copperOre': 10}, unlock=5, track='copper', place='dep:copperOre'),
    'coalPit':      dict(era=1, size=1, gold=700,   inp={'energy': 8}, out={'coal': 12}, unlock=5, track='coal', place='dep:coal'),
    'quarry':       dict(era=1, size=4, gold=3000,  inp={'energy': 25}, out={'stone': 80}, unlock=10, track='stone', place='dep:stone'),
    'sandDredger':  dict(era=1, size=1, gold=500,   inp={'energy': 6}, out={'sand': 25}, unlock=5, track='sand', place='dep:sand'),
    'lumberCamp':   dict(era=1, size=1, gold=400,   inp={'energy': 6}, out={'wood': 20}, unlock=5, track='wood', place='forest'),
    'waterPump':    dict(era=1, size=1, gold=300,   inp={'energy': 3}, out={'water': 50}, unlock=5, track='power', place='shore'),
    'smallFactory': dict(era=1, size=1, gold=1500,  inp={'energy': 20, 'ironOre': 12}, out={'ironIngot': 6}, unlock=2, track='iron', place='any'),
    'bigFactory':   dict(era=1, size=4, gold=12000, inp={'energy': 90, 'ironOre': 60}, out={'ironIngot': 32}, unlock=12, track='iron', place='any'),
    'smelter':      dict(era=1, size=1, gold=1800,  inp={'energy': 20, 'copperOre': 10, 'coal': 4}, out={'copperIngot': 5}, unlock=6, track='copper', place='any'),
    'brickworks':   dict(era=1, size=1, gold=1400,  inp={'energy': 10, 'stone': 10, 'coal': 5}, out={'bricks': 12}, unlock=6, track='stone', place='any'),
    'glassworks':   dict(era=1, size=1, gold=1600,  inp={'energy': 25, 'sand': 15}, out={'glass': 5}, unlock=7, track='sand', place='any'),
    'sawmill':      dict(era=1, size=1, gold=800,   inp={'energy': 6, 'wood': 10}, out={'planks': 12}, unlock=5, track='wood', place='any'),
    # Era 2
    'bigTurbine':     dict(era=2, size=4, gold=36000,  inp={}, out={'energy': 400}, unlock=0, track='power', place='wind'),
    'steamTurbine':   dict(era=2, size=4, gold=30000,  inp={'coal': 30, 'water': 40}, out={'energy': 500}, unlock=0, track='power', place='any'),
    'bauxitePit':     dict(era=2, size=1, gold=8000,   inp={'energy': 20}, out={'bauxite': 15}, unlock=0, track='aluminium', place='dep:bauxite'),
    'deepMine':       dict(era=2, size=4, gold=40000,  inp={'energy': 60}, out={'ironOre': 160}, unlock=0, track='iron', place='dep:ironOre'),
    'steelMill':      dict(era=2, size=4, gold=46000,  inp={'energy': 120, 'ironIngot': 20, 'coal': 10}, out={'steel': 14}, unlock=0, track='steel', place='any'),
    'wireMill':       dict(era=2, size=1, gold=12000,  inp={'energy': 40, 'copperIngot': 6}, out={'copperWire': 6}, unlock=0, track='wire', place='any'),
    'cementKiln':     dict(era=2, size=4, gold=40000,  inp={'energy': 50, 'stone': 20, 'sand': 20, 'water': 10}, out={'concrete': 25}, unlock=0, track='concrete', place='any'),
    'aluminiumWorks': dict(era=2, size=4, gold=45000,  inp={'energy': 300, 'bauxite': 30}, out={'aluminium': 12}, unlock=0, track='aluminium', place='any'),
}

TRACKS1 = ['power', 'iron', 'copper', 'coal', 'stone', 'sand', 'wood']
TRACKS2 = ['steel', 'wire', 'concrete', 'aluminium']
BASICS = ['copperOre', 'coal', 'stone', 'sand']

# A track: nine tiers. time in hours; effect; gold at Era 1 (x4 per Era)
TIERS = [
    (1 / 6, ('faster', 1.25), 500),
    (1, ('leaner', 0.8), 3000),
    (4, ('faster', 1.5), 15000),
    (12, ('complex', 1.25), 50000),
    (24, ('faster', 2.0), 120000),
    (36, ('leaner', 0.7), 300000),
    (48, ('faster', 2.0), 600000),
    (48, ('complex', 4 / 3), 1200000),
    (60, ('faster', 2.0), 2500000),
]


def level_mult(level):
    return 1.1 ** (level - 1)


def step_cost(gold, level):
    """Gold for the step from level to level + 1"""
    return gold * 1.15 ** (level - 1)


def copy_cost(gold, copies):
    return gold * 1.07 ** copies


def xp_for_level(n, xp_base=100, exp=1.7, ramp=4, ramp_level=9, late=4, late_from=22, late_to=30):
    if n <= 1:
        return 0
    early = 1 + (ramp - 1) * min(max((n - 2) / max(1, ramp_level - 2), 0), 1)
    lat = 1 + (late - 1) * min(max((n - late_from) / max(1, late_to - late_from), 0), 1)
    return round(xp_base * (n - 1) ** exp * early * lat)


PARCEL_LEVELS = [5, 7, 12, 14, 18, 25, 30, 30, 35, 40, 45, 50]


def parcel_cost(n, first=5000, factor=2):
    return first * factor ** n

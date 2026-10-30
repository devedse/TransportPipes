# The 1.4 content for the balance model: Eras 1 and 2. Every number here is a design number the doc states.
import math

PRICE = {
    'energy': 1, 'water': 0.5, 'ironOre': 3, 'wood': 1.5, 'copperOre': 3.5, 'coal': 2.5, 'stone': 1.5, 'sand': 1.2,
    'ironIngot': 14, 'copperIngot': 18, 'bricks': 5, 'glass': 14, 'planks': 3.5,
    'bauxite': 8, 'steel': 60, 'copperWire': 40, 'concrete': 20, 'aluminium': 80,
    # Eras 3 to 5 (phase C: the model plays them to set their numbers)
    'oil': 25, 'plastic': 150, 'gears': 290, 'motors': 700,
    'rareEarth': 300, 'circuits': 1500, 'batteries': 1800, 'robots': 7700,
    'titaniumOre': 1300, 'titanium': 4800, 'superconductors': 10000, 'spaceframes': 34000,
}
ERA_GOODS = {2: ('bauxite', 'steel', 'copperWire', 'concrete', 'aluminium'), 3: ('oil', 'plastic', 'gears', 'motors'),
             4: ('rareEarth', 'circuits', 'batteries', 'robots'), 5: ('titaniumOre', 'titanium', 'superconductors', 'spaceframes')}
ERA_OF_GOOD = {g: next((e for e, gs in ERA_GOODS.items() if g in gs), 1) for g in PRICE}

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
    # the Deep Mine on copper ore and on coal: the same building, the same value a minute, its ore's track
    'deepMineCopper': dict(era=2, size=4, gold=40000,  inp={'energy': 60}, out={'copperOre': 137}, unlock=0, track='copper', place='dep:copperOre'),
    'deepMineCoal':   dict(era=2, size=4, gold=40000,  inp={'energy': 60}, out={'coal': 192}, unlock=0, track='coal', place='dep:coal'),
    'steelMill':      dict(era=2, size=4, gold=46000,  inp={'energy': 120, 'ironIngot': 20, 'coal': 10}, out={'steel': 14}, unlock=0, track='steel', place='any'),
    'wireMill':       dict(era=2, size=1, gold=12000,  inp={'energy': 40, 'copperIngot': 6}, out={'copperWire': 6}, unlock=0, track='wire', place='any'),
    'cementKiln':     dict(era=2, size=4, gold=40000,  inp={'energy': 50, 'stone': 20, 'sand': 20, 'water': 10}, out={'concrete': 25}, unlock=0, track='concrete', place='any'),
    'aluminiumWorks': dict(era=2, size=4, gold=45000,  inp={'energy': 300, 'bauxite': 30}, out={'aluminium': 12}, unlock=0, track='aluminium', place='any'),
    # Era 3 (buildings.yaml; a later Era's building pays for its materials too: build_cost's x1.25)
    'solarFarm':      dict(era=3, size=9, gold=650000,  inp={}, out={'energy': 3600}, unlock=0, track='power', place='sun'),
    'oilDerrick':     dict(era=3, size=1, gold=76000,   inp={'energy': 30}, out={'oil': 18}, unlock=0, track='oil', place='dep:oil'),
    'refinery':       dict(era=3, size=9, gold=640000,  inp={'energy': 600, 'oil': 60, 'water': 40}, out={'plastic': 38}, unlock=0, track='oil', place='any'),
    'machineShop':    dict(era=3, size=4, gold=290000,  inp={'energy': 400, 'steel': 20}, out={'gears': 11}, unlock=0, track='gears', place='any'),
    'motorWorks':     dict(era=3, size=4, gold=280000,  inp={'energy': 500, 'copperWire': 10, 'aluminium': 6, 'gears': 2}, out={'motors': 5}, unlock=0, track='motors', place='any'),
    # the Deep Level's magma (the Deep Lift, Era 3)
    'magmaTap':       dict(era=3, size=4, gold=300000,  inp={}, out={'energy': 1600}, unlock=0, track='power', place='dep:magma'),
    # Era 4
    'geothermalPlant':    dict(era=4, size=4, gold=2300000, inp={'water': 20}, out={'energy': 6400}, unlock=0, track='power', place='dep:geothermal'),
    'rareEarthExcavator': dict(era=4, size=4, gold=2300000, inp={'energy': 200}, out={'rareEarth': 22}, unlock=0, track='rareEarth', place='dep:rareEarth'),
    'circuitFab':         dict(era=4, size=4, gold=2400000, inp={'energy': 1500, 'copperWire': 20, 'plastic': 10, 'glass': 10}, out={'circuits': 7}, unlock=0, track='circuits', place='any'),
    'batteryWorks':       dict(era=4, size=4, gold=2400000, inp={'energy': 1500, 'rareEarth': 6, 'aluminium': 10}, out={'batteries': 6}, unlock=0, track='batteries', place='any'),
    'robotWorks':         dict(era=4, size=9, gold=5200000, inp={'energy': 4000, 'motors': 4, 'circuits': 4, 'batteries': 2}, out={'robots': 4}, unlock=0, track='robots', place='any'),
    # Era 5
    'fusionReactor':      dict(era=5, size=9, gold=41000000, inp={'superconductors': 1, 'water': 100}, out={'energy': 67000}, unlock=0, track='power', place='any'),
    'titaniumMine':       dict(era=5, size=4, gold=18000000, inp={'energy': 400}, out={'titaniumOre': 20}, unlock=0, track='titanium', place='dep:titaniumOre'),
    'titaniumWorks':      dict(era=5, size=4, gold=18000000, inp={'energy': 6000, 'titaniumOre': 20}, out={'titanium': 12}, unlock=0, track='titanium', place='any'),
    'superconductorLab':  dict(era=5, size=4, gold=17000000, inp={'energy': 6000, 'rareEarth': 10, 'circuits': 4, 'copperWire': 20}, out={'superconductors': 4}, unlock=0, track='superconductors', place='any'),
    'spaceframeWorks':    dict(era=5, size=9, gold=41000000, inp={'energy': 15000, 'titanium': 6, 'robots': 2, 'superconductors': 2}, out={'spaceframes': 4}, unlock=0, track='spaceframes', place='any'),
}

TRACKS1 = ['power', 'iron', 'copper', 'coal', 'stone', 'sand', 'wood']
TRACKS2 = ['steel', 'wire', 'concrete', 'aluminium']
TRACKS3 = ['oil', 'gears', 'motors']
TRACKS4 = ['rareEarth', 'circuits', 'batteries', 'robots']
TRACKS5 = ['titanium', 'superconductors', 'spaceframes']
TRACK_ERA = {t: e for e, ts in enumerate([TRACKS1, TRACKS2, TRACKS3, TRACKS4, TRACKS5], 1) for t in ts}
BASICS = ['copperOre', 'coal', 'stone', 'sand']
DEEP_MINES = {'deepMine', 'deepMineCopper', 'deepMineCoal'}

# The Era tree (the second research line): (id, era, hours, gold, what it opens)
TREE = [
    ('copperPipes', 1, 1 / 6, 300, 'pipe:copper'),
    ('valves', 1, 0.25, 500, None),
    ('pumpStations', 1, 1 / 3, 800, None),
    ('bigFactory', 1, 0.5, 2000, 'build:bigFactory'),
    ('steelPipes', 2, 1, 5e6, 'pipe:steel'),
    ('survey', 2, 2, 30e6, 'outskirts'),
    ('deepMining', 2, 2, 20e6, 'build:deep'),
    ('powerLines', 2, 1, 10e6, None),
    ('silos', 2, 1, 10e6, None),
    # Eras 3 to 5 (research.yaml): what the model plays opens pipes and land; the rest is only its price
    # (about an hour or two of a regular player's income when the Era begins: 2.8M, 12M and 35M g a minute)
    ('compositePipes', 3, 4, 200e6, 'pipe:composite'),
    ('deepLift', 3, 8, 400e6, 'deep'),
    ('trains', 3, 4, 200e6, None),
    ('trunkLines', 3, 6, 300e6, None),
    ('terraforming', 3, 12, 600e6, None),
    ('glassConduit', 4, 12, 800e6, 'pipe:glass'),
    ('outposts', 4, 12, 1e9, 'outposts'),
    ('automation', 4, 8, 800e6, None),
    ('quantumLink', 4, 16, 1.2e9, None),
    ('superconductorPipes', 5, 24, 3e9, 'pipe:superconductor'),
    ('orbitalLogistics', 5, 24, 3e9, None),
]
# Re-laying a plot's pipes in a better tier: gold and goods a tile (the goods at the market's price)
PIPE_RELAY = {'copper': 10 + 18 * 2.5, 'steel': 40 + 60 * 2.5, 'composite': 200 + 150 * 2.5, 'glass': 1000 + 1500 * 2.5,
              'superconductor': 5000 + 10000 * 2.5}
PIPE_TILES_PER_PARCEL = 13

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

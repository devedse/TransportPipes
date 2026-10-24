# A player's plot: land pools, buildings, levels, research, gold, XP
import copy
from content import B, TIERS, TRACKS1, TRACKS2, BASICS, DEEP_MINES, level_mult, step_cost, copy_cost, xp_for_level, PARCEL_LEVELS, parcel_cost, PRICE

PIPES = 0.8  # share of a parcel's 64 tiles left for buildings once pipes are laid
PARCEL = int(64 * PIPES)  # 51

# Where each kind of building may stand, best spot first
POOLS = {
    'wind': ['hill', 'general', 'desert', 'beach'],
    'sun': ['desert', 'general', 'beach', 'hill'],
    'any': ['general', 'desert', 'beach', 'hill'],
    'shore': ['shore'],
    'forest': ['forest'],
}
MULT = {'hill': {'wind': 1.4}, 'desert': {'sun': 1.45}, 'beach': {'wind': 1.15}}


def make_land(lacks, lacks2, strength):
    """The plot of the 1.4 design: the start area and twelve parcels, buildable tiles after pipes"""
    has = [b for b in BASICS if b != lacks]
    first = has[0]
    start = {'general': 205 - 8 - 12 - 10 - 6 - 16 - 30, 'dep:ironOre': [(12, 1.0)], f'dep:{first}': [(10, 1.0)],
             'shore': 6, 'forest': 16, 'hill': 30}
    parcels = []
    rich = 1.6
    # the rich (strength) deposit, next to the start area
    parcels.append(('rich', {f'dep:{strength}': [(12, rich)], 'general': PARCEL - 12}))
    parcels.append(('hill', {'hill': 40, 'general': PARCEL - 40} | ({'dep:stone': [(12, 1.0)]} if 'stone' in has else {})))
    parcels.append(('iron', {'dep:ironOre': [(12, 1.3)], 'general': PARCEL - 12}))
    parcels.append(('basicB', {f'dep:{has[1]}': [(12, 1.0)], 'general': PARCEL - 12}))
    parcels.append(('basicC', {f'dep:{has[2]}': [(12, 1.0)], 'general': PARCEL - 12}))
    parcels.append(('forest', {'forest': 35, 'general': PARCEL - 35}))
    parcels.append(('lake', {'shore': 12, 'general': PARCEL - 12 - 8} | ({'dep:sand': [(8, 1.0)]} if 'sand' in has else {})))
    parcels.append(('redhills', ({'dep:bauxite': [(12, 1.0)]} if lacks2 != 'bauxite' else {}) | {'hill': 20, 'general': PARCEL - 32}))
    parcels.append(('basicA2', {f'dep:{first}': [(12, 1.3)], 'general': PARCEL - 12}))
    parcels.append(('desert', {'desert': PARCEL}))
    parcels.append(('grass', {'general': PARCEL}))
    parcels.append(('grass2', {'general': PARCEL}))
    return start, parcels


def make_outskirts(lacks, lacks2):
    """Nine outskirts parcels: every land resource at two or three stars, the plot's missing ones first"""
    out = []
    out.append(('o-lacks', {f'dep:{lacks}': [(12, 1.0)], 'general': PARCEL - 12}))
    out.append(('o-bauxite', {'dep:bauxite': [(12, 1.3)], 'hill': 20, 'general': PARCEL - 32}))
    out.append(('o-iron', {'dep:ironOre': [(12, 1.3)], 'general': PARCEL - 12}))
    out.append(('o-coal', {'dep:coal': [(12, 1.0)], 'general': PARCEL - 12}))
    out.append(('o-forest', {'forest': 30, 'shore': 10, 'general': PARCEL - 40}))
    out.append(('o-stone', {'dep:stone': [(12, 1.0)], 'hill': 20, 'general': PARCEL - 32}))
    out.append(('o-sand', {'dep:sand': [(12, 1.0)], 'shore': 8, 'general': PARCEL - 20}))
    out.append(('o-copper', {'dep:copperOre': [(12, 1.3)], 'general': PARCEL - 12}))
    out.append(('o-grass', {'general': PARCEL}))
    return out


class Plot:
    def __init__(self, name, lacks, lacks2='oil', strength='ironOre', plan=None, schedule='regular'):
        self.name = name
        self.lacks = lacks
        self.lacks2 = lacks2
        self.strength = strength
        self.plan = plan or []
        self.schedule = schedule
        start, self.parcels_left = make_land(lacks, lacks2, strength)
        self.pools = {}
        self.add_pools(start)
        self.parcels = 0
        self.outskirts_left = make_outskirts(lacks, lacks2)
        self.outskirts = 0
        self.give = 0.0
        self.gift_goods = 0.0
        self.gift_gold = 0.0
        self.buildings = []  # [type, level, mult]
        self.posts = [1]
        self.post_copies = 0
        self.gold = 2000.0
        self.xp = 0.0
        self.level = 1
        self.tiers = {}
        self.research = None  # (track, done_at)
        self.queue_index = 0
        self.earned = 0.0
        self.spent = {}
        self.copies = {}
        self.given = 0.0
        self.era = 1
        self.pipe = 'grey'
        self.tree = set()
        self.tree_line = None  # (item, done_at)
        self.need_tree = False

    def add_pools(self, d):
        for k, v in d.items():
            if isinstance(v, list):
                self.pools.setdefault(k, [])
                self.pools[k] = sorted(self.pools[k] + [list(e) for e in v], key=lambda e: -e[1])
            else:
                self.pools.setdefault(k, 0)
                self.pools[k] += v

    def units(self):
        u = {}
        for t, lvl, m, *_ in self.buildings:
            u[t] = u.get(t, 0) + level_mult(lvl) * m
        return u

    def post_units(self):
        return sum(level_mult(l) for l in self.posts)

    # ---- placing
    def spot(self, t):
        """The best free spot for a building: (pool, mult) or None"""
        d = B[t]
        size = d['size']
        place = d['place']
        if place.startswith('dep:'):
            for e in self.pools.get(place, []):
                if e[0] >= size:
                    return place, e[1]
            return None
        for pool in POOLS[place]:
            if self.pools.get(pool, 0) >= size:
                return pool, MULT.get(pool, {}).get(place, 1.0)
        return None

    def take(self, t, spot):
        pool, mult = spot
        size = B[t]['size']
        if pool.startswith('dep:'):
            for e in self.pools[pool]:
                if e[0] >= size and e[1] == mult:
                    e[0] -= size
                    break
        else:
            self.pools[pool] -= size

    def unlocked(self, t):
        d = B[t]
        allowed = getattr(self, 'allowed', None)
        if allowed is not None and d['track'] not in allowed:
            return False
        if d['era'] > self.era:
            return False
        if self.need_tree and ((t == 'bigFactory' and 'bigFactory' not in self.tree)
                               or (t in DEEP_MINES and 'deepMining' not in self.tree)):
            return False
        return d['era'] > 1 or self.level >= d['unlock']

    def build_cost(self, t, rules=None):
        f = rules.copy_factor if rules else 1.07
        scale = (rules.cost_scale.get(t, 1) * rules.all_costs) if rules else 1
        return B[t]['gold'] * scale * f ** self.copies.get(t, 0) * (1.25 if B[t]['era'] >= 2 else 1)

    def build(self, t, spot, rules=None):
        cost = self.build_cost(t, rules)
        self.take(t, spot)
        self.buildings.append([t, 1, spot[1], cost])
        self.copies[t] = self.copies.get(t, 0) + 1
        return cost

    # ---- XP
    def gain_xp(self, xp):
        if getattr(self, 'level_gold', None):
            return
        self.xp += xp
        while self.level < 60 and self.xp >= xp_for_level(self.level + 1):
            self.level += 1

    def gain_earned(self, gold):
        """One XP per 25 x level gold earned; or, with a level table, the level comes straight from the gold earned"""
        lg = getattr(self, 'level_gold', None)
        if lg:
            self.lifetime = getattr(self, 'lifetime', 0.0) + gold
            while self.level < len(lg) + 1 and self.lifetime >= lg[self.level - 1]:
                self.level += 1
            return
        while gold > 1e-9:
            rate = 25 * self.level
            to_next = xp_for_level(self.level + 1) - self.xp if self.level < 60 else 9e18
            if gold / rate < to_next:
                self.xp += gold / rate
                break
            self.xp += to_next
            gold -= to_next * rate
            self.level += 1

    def spend_gold(self, what, gold):
        self.gold -= gold
        self.spent[what] = self.spent.get(what, 0) + gold

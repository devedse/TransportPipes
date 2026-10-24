# The economy of one plot: what its buildings make and use, what it sells and buys, and its income a minute
import math
from content import B, PRICE, ERA_OF_GOOD, TIERS, level_mult

GOOD_TRACK = {'energy': 'power', 'water': 'power', 'ironOre': 'iron', 'ironIngot': 'iron', 'copperOre': 'copper',
              'copperIngot': 'copper', 'coal': 'coal', 'stone': 'stone', 'bricks': 'stone', 'sand': 'sand', 'glass': 'sand',
              'wood': 'wood', 'planks': 'wood', 'steel': 'steel', 'copperWire': 'wire', 'concrete': 'concrete',
              'bauxite': 'aluminium', 'aluminium': 'aluminium'}
POWER_FREE = {'windMoily', 'solarPanel', 'hydroWheel', 'bigTurbine'}  # power from wind, sun, water: leaner = more output


class Rules:
    """Design numbers the balance turns: one object so a run can try another set"""

    def __init__(self, **kw):
        self.ho_value = 1000.0          # a Hub Order in gold a minute of each good's base price
        self.ho_era_growth = 1.5        # x per town Era above the good's Era
        self.ho_era_value = 4.0         # a later Era's goods: this many times the Hub Order value
        self.ho_track = 1.0             # a track's tiers raise its goods' Hub Orders: x this per tier
        self.track_strength = {}        # track -> share of each faster tier's boost (power: 0.5 halves it)
        self.out_first = 5e6            # the first outskirts parcel (Era 2)
        self.out_factor = 1.6
        self.ho_floor = 0.25            # the lowest share of the price the market pays
        self.import_mult = 2.5          # the market sells at
        self.crew_price = 1.15          # what crew members pay each other
        self.post_units = 600           # trading post units a minute at level 1
        self.post_value = None          # or: gold a minute (at base prices) a level-1 post sells
        self.post_ho = None             # Hub Orders x this per trading post level above 1 (and no throughput cap)
        self.post_cost = 2500           # the step from post level L to L + 1: post_cost x post_factor^(L - 1)
        self.post_factor = 1.15
        self.max_level = 25
        self.pipe_cap = None            # units a minute one building can push (None = no cap)
        self.upgrade_mats = False       # level steps above 10 cost goods
        self.mats_share = 0.3           # goods worth this share of the step's gold
        self.perk_output = 1.0          # Monuments, Wonder stages
        self.copy_levels = True         # a copy's levels cost from what that copy cost
        self.copy_factor = 1.07
        self.level_factor = 1.15
        self.parcel_first = 5000
        self.parcel_factor = 2.0
        self.parcel_levels = [5, 7, 12, 14, 18, 25, 30, 30, 35, 40, 45, 50]
        self.research_scale = 1.0
        self.cost_scale = {}            # building -> x on its gold
        self.all_costs = 1.0            # x on every building, level, parcel, research and post price
        self.level_gold = None          # Company Level from gold earned: [gold for level 2, 3, ...]
        self.tier_gold = None           # research gold per tier (Era 1), x4 per Era
        self.monument = None            # (first stage gold, x per stage, production per stage)
        self.endgame_split = 0.5        # share of the spare gold a player puts in the Monument (the rest: the Wonder)
        self.start_gold = 2000
        self.pipe_caps = None           # units a minute one connection carries, by pipe tier ({'grey': 100, ...})
        self.era_tree = False           # the Era tree on the second research line: costs, and what it opens
        self.ho_track_from = 1          # the first tier that raises its goods' Hub Orders
        self.coal_in = None             # tracks that also cover coal (coal pits follow the best of them), no Coal track
        self.recipes = {}               # building -> {'inp': ..., 'out': ...} replacing its recipe
        self.__dict__.update(kw)


TRACK_STRENGTH = {}
COAL_IN = []


def tiers_of(player, track):
    """How many tiers of a track a player has; with coal in other tracks, coal follows the best of them"""
    if track == 'coal' and COAL_IN:
        return max(player.tiers.get(t, 0) for t in COAL_IN)
    return player.tiers.get(track, 0)


def track_mults(tiers_done, track=None):
    """faster, leaner, complex for a track with this many tiers done"""
    faster, leaner, complex_ = 1.0, 1.0, 1.0
    k = TRACK_STRENGTH.get(track, 1.0)
    for i in range(tiers_done):
        kind, v = TIERS[i][1]
        if kind == 'faster':
            faster *= 1 + (v - 1) * k
        elif kind == 'leaner':
            leaner *= v
        else:
            complex_ = v
    return faster, leaner, complex_


def capped_units(player, cap):
    """A building's units, but none that would push more than one pipe connection carries: a building whose pipe is
    full runs at what the pipe takes"""
    perk = getattr(player, 'perk', 1.0)
    out = {}
    per = {}
    for t, lvl, m, *_ in player.buildings:
        if t not in per:
            d = B[t]
            tiers = tiers_of(player, d['track'])
            faster, leaner, complex_ = track_mults(tiers, d['track'])
            top = max(d['out'].values()) * faster * complex_ * perk
            if t in POWER_FREE:
                top *= (1.2 if tiers >= 2 else 1) * (1.3 if tiers >= 6 else 1)
            per[t] = top
        u = level_mult(lvl) * m
        out[t] = out.get(t, 0) + min(u, cap / per[t])
    return out


def rates(player, rules):
    """Per building type: (inputs a minute, outputs a minute) at full speed"""
    out = {}
    caps = rules.pipe_caps
    units_of = capped_units(player, caps[player.pipe]) if caps else player.units()
    for t, units in units_of.items():
        d = B[t]
        tiers = tiers_of(player, d['track'])
        faster, leaner, complex_ = track_mults(tiers, d['track'])
        speed = units * faster * complex_ * rules.perk_output * getattr(player, 'perk', 1.0)
        o = {g: v * speed for g, v in d['out'].items()}
        if t in POWER_FREE:
            bonus = (1.2 if tiers >= 2 else 1) * (1.3 if tiers >= 6 else 1)
            o = {g: v * bonus for g, v in o.items()}
            i = {}
        else:
            i = {g: v * speed * leaner for g, v in d['inp'].items()}
        out[t] = (i, o)
    return out


def market_revenue(units, ho, price, floor):
    if units <= ho:
        return units * price
    if units <= ho / floor ** 2:
        return price * math.sqrt(ho * units)
    return units * price * floor


def hub_order(good, rules, town_era):
    era = ERA_OF_GOOD[good]
    return rules.ho_value * rules.ho_era_value ** (era - 1) / PRICE[good] * rules.ho_era_growth ** max(0, town_era - era)


MATS = {1: [(11, ['planks', 'bricks']), (16, ['planks', 'bricks', 'ironIngot', 'glass']), (21, ['planks', 'bricks', 'ironIngot', 'glass', 'copperIngot'])],
        2: [(11, ['steel', 'concrete']), (16, ['steel', 'concrete', 'copperWire']), (21, ['steel', 'concrete', 'copperWire', 'aluminium'])]}


def materials(era, level, gold, share):
    """The goods a level step from this level needs: worth share x its gold at base prices, split evenly"""
    goods = []
    for frm, gs in MATS.get(era, MATS[1]):
        if level + 1 >= frm:
            goods = gs
    if not goods:
        return {}
    each = gold * share / len(goods)
    return {g: each / PRICE[g] for g in goods}


def solve(player, rules, town_era=1, crew_offer=None, crew_need=None, give_beyond_ho=0.0, extra_ho=None):
    """
    The flows of a plot in one minute. crew_offer: what neighbours can sell it (good -> (units, price)), crew_need:
    what neighbours want of its spare (good -> units). Returns a dict with income and the flows.
    """
    crew_offer = crew_offer or {}
    crew_need = crew_need or {}
    r = rates(player, rules)
    types = list(r)
    x = {t: 1.0 for t in types}
    buy = {}
    # Which goods the player lets the market sell it: only where every building using it still earns at 2.5x
    allow_import = {}
    mv = getattr(player, '_mv', None) or {}
    for t in types:
        i, o = r[t]
        val_o = sum(mv.get(g, PRICE[g]) * v for g, v in o.items())
        for g in i:
            val_i = sum(PRICE[h] * v * (rules.import_mult if h == g else 1) for h, v in i.items())
            allow_import[g] = allow_import.get(g, True) and val_o > val_i
    for _ in range(3):
        for _ in range(12):
            prod = {}
            dem = {}
            for t in types:
                i, o = r[t]
                for g, v in o.items():
                    prod[g] = prod.get(g, 0) + v * x[t]
                for g, v in i.items():
                    dem[g] = dem.get(g, 0) + v
            # buying: the crew first, then the market
            buy = {}
            for g, d in dem.items():
                short = d - prod.get(g, 0)
                if short <= 1e-9:
                    continue
                units, price = crew_offer.get(g, (0, 0))
                take = min(short, units)
                if take > 0:
                    buy[g] = [take, take * price * PRICE[g], 0]
                    short -= take
                if short > 1e-9 and allow_import.get(g):
                    buy.setdefault(g, [0, 0, 0])
                    buy[g][2] = short
            ratio = {}
            for g, d in dem.items():
                have = prod.get(g, 0) + sum(buy.get(g, [0, 0, 0])[k] for k in (0, 2))
                ratio[g] = min(1.0, have / d) if d > 0 else 1.0
            nx = {}
            for t in types:
                i, o = r[t]
                nx[t] = min([ratio[g] for g in i] + [1.0])
            if all(abs(nx[t] - x[t]) < 1e-6 for t in types):
                x = nx
                break
            x = nx
        # a second pass hands what limited buildings leave over to the others
        used = {}
        for t in types:
            for g, v in r[t][0].items():
                used[g] = used.get(g, 0) + v * x[t]
        spare_in = {g: prod.get(g, 0) + sum(buy.get(g, [0, 0, 0])[k] for k in (0, 2)) - used.get(g, 0) for g in used}
        changed = False
        for t in types:
            i, o = r[t]
            if not i or x[t] >= 1 - 1e-9:
                continue
            room = min([(spare_in[g] / v if v > 0 else 9e9) for g, v in i.items()])
            extra = min(1 - x[t], room)
            if extra > 1e-6:
                x[t] += extra
                for g, v in i.items():
                    spare_in[g] -= v * extra
                changed = True
        if not changed:
            break
    want = {g: d - prod.get(g, 0) for g, d in dem.items()} if types else {}
    prod, used = {}, {}
    for t in types:
        i, o = r[t]
        for g, v in o.items():
            prod[g] = prod.get(g, 0) + v * x[t]
        for g, v in i.items():
            used[g] = used.get(g, 0) + v * x[t]
    imports = {}
    cost_crew = 0.0
    bought_crew = {}
    for g, (cu, cg, mu) in buy.items():
        need = max(0.0, used.get(g, 0) - prod.get(g, 0))
        cu = min(cu, need)
        mu = min(mu, need - cu)
        if cu > 0:
            bought_crew[g] = cu
            cost_crew += cu * crew_offer[g][1] * PRICE[g]
        if mu > 0:
            imports[g] = mu
    spare = {g: prod.get(g, 0) - used.get(g, 0) for g in prod}
    spare = {g: v for g, v in spare.items() if v > 1e-9}
    # materials for the levels being bought: own spare first, then the crew, then the market
    mats_own, mats_crew, mats_market = {}, {}, {}
    for g, want_m in getattr(player, 'mat_rate', {}).items():
        if want_m <= 1e-9:
            continue
        own = min(spare.get(g, 0), want_m)
        if own > 0:
            spare[g] -= own
            mats_own[g] = own
        rest = want_m - own
        units, price = crew_offer.get(g, (0, 0))
        units = max(0.0, units - bought_crew.get(g, 0))
        c = min(rest, units)
        if c > 0:
            mats_crew[g] = c
            cost_crew += c * price * PRICE[g]
            bought_crew[g] = bought_crew.get(g, 0) + c
            rest -= c
        if rest > 1e-9:
            mats_market[g] = rest
            imports[g] = imports.get(g, 0) + rest
    # spare: the crew's needs first, at the crew price
    sold_crew = {}
    gold_crew = 0.0
    for g, v in spare.items():
        n = crew_need.get(g, 0)
        if n > 0:
            take = min(v, n)
            sold_crew[g] = take
            gold_crew += take * rules.crew_price * PRICE[g]
            spare[g] = v - take
    # then the market, through the trading posts; the best gold a unit first
    by_value = rules.post_value is not None
    cap = (rules.post_value if by_value else rules.post_units) * player.post_units()
    hos = {g: hub_order(g, rules, town_era) for g in spare}
    if rules.post_ho:
        reach = rules.post_ho ** (player.posts[0] - 1)
        hos = {g: v * reach for g, v in hos.items()}
        cap = 9e18
    if rules.ho_track != 1.0:
        k0 = rules.ho_track_from - 1
        hos = {g: v * rules.ho_track ** max(0, tiers_of(player, GOOD_TRACK.get(g, '')) - k0) for g, v in hos.items()}
    own_hos = dict(hos)
    if extra_ho:
        hos = {g: v + extra_ho.get(g, 0) for g, v in hos.items()}
    chunks = []
    for g, v in spare.items():
        if v <= 1e-9:
            continue
        ho = hos[g]
        n = 24
        prev_rev = 0.0
        prev_u = 0.0
        for k in range(1, n + 1):
            u = v * k / n
            rev = market_revenue(u, ho, PRICE[g], rules.ho_floor)
            chunks.append(((rev - prev_rev) / (u - prev_u), g, u - prev_u, rev - prev_rev))
            prev_rev, prev_u = rev, u
    chunks.sort(key=lambda c: -c[0])
    sold = {}
    gold_market = 0.0
    gifts = {}
    for per, g, u, rev in chunks:
        if cap <= 1e-9:
            break
        if by_value:
            take = min(u, cap / PRICE[g])
            cap -= take * PRICE[g]
        else:
            take = min(u, cap)
            cap -= take
        sold[g] = sold.get(g, 0) + take
    # recompute the market's gold on what was sold (sorting by marginal price keeps each good's chunks in order)
    for g, u in sold.items():
        if give_beyond_ho > 0 and u > hos[g]:
            gifts[g] = (u - hos[g]) * give_beyond_ho
            u = u - gifts[g]
        gold_market += market_revenue(u, hos[g], PRICE[g], rules.ho_floor)
    unsold = {g: spare.get(g, 0) - sold.get(g, 0) for g in spare}
    cost_market = sum(u * PRICE[g] * rules.import_mult for g, u in imports.items())
    income = gold_market + gold_crew - cost_crew - cost_market
    return dict(income=income, gold_market=gold_market, gold_crew=gold_crew, cost_crew=cost_crew,
                cost_market=cost_market, prod=prod, used=used, sold=sold, sold_crew=sold_crew, bought_crew=bought_crew,
                imports=imports, unsold=unsold, x=x, hos=hos, own_hos=own_hos, gifts=gifts,
                mats_own=mats_own, mats_crew=mats_crew, mats_market=mats_market,
                short={g: max(0.0, used.get(g, 0) - prod.get(g, 0)) for g in used},
                want={g: max(0.0, v) for g, v in want.items()},
                earned=gold_market + gold_crew)

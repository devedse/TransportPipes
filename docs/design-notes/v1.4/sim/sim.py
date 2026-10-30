# Plays plots session by session: gold comes in all the time, the player spends it when they play
import math
from content import B, TIERS, TRACKS1, TRACKS2, TRACK_ERA, level_mult, step_cost, copy_cost, PARCEL_LEVELS, parcel_cost, xp_for_level
from econ import solve, Rules
from plot import Plot

DAY = 1440
SCHEDULES = {
    'keen': [(7 * 60 + 30, 20), (12 * 60 + 30, 15), (18 * 60, 45), (21 * 60 + 30, 30)],
    'regular': [(8 * 60, 10), (19 * 60 + 30, 45), (22 * 60 + 30, 10)],
    'casual': [(20 * 60, 30)],
}


def sessions(schedule, days):
    """(start minute, length) for every session; the first evening is a long one at 19:00 on day 0"""
    out = [(19 * 60, 120)]
    for d in range(1, days):
        for start, length in SCHEDULES[schedule]:
            out.append((d * DAY + start, length))
    return out


class Sim:
    def __init__(self, rules, plots, town_era_at=None, max_payback_h=72, save_h=8, crew=False, give=0.0):
        self.rules = rules
        self.plots = plots
        self.town_era_at = town_era_at or {}  # era -> minute
        self.max_payback = max_payback_h * 60
        self.save = save_h * 60
        self.crew = crew
        self.give = give
        self.t = 0.0
        import econ
        econ.TRACK_STRENGTH.clear()
        econ.TRACK_STRENGTH.update(rules.track_strength)
        econ.COAL_IN[:] = rules.coal_in or []
        from content import B
        for t, r in rules.recipes.items():
            B[t].update(r)
        self.mon = {'stage': 0, 'paid': 0.0}
        self.wonder = {'given': 0.0}
        for p in plots:
            p.gold = rules.start_gold
            p.need_tree = rules.era_tree
            if rules.level_gold:
                p.level_gold = rules.level_gold
        self.log = []
        self.state = {}
        self.offers = {p.name: ({}, {}) for p in plots}

    def era(self):
        e = 1
        for era, at in self.town_era_at.items():
            if self.t >= at:
                e = max(e, era)
        return e

    # ---- the economy of each plot, with the crew's offers
    def econ(self, p, extra=None):
        offer, need = self.offers[p.name]
        return solve(p, self.rules, self.era(), offer, need, max(self.give, p.give), extra_ho=getattr(self, 'extra', {}).get(p.name))

    def crew_round(self):
        """The Yard: what each plot lacks is filled from the others' spare at the crew price"""
        if not self.crew:
            for p in self.plots:
                self.state[p.name] = solve(p, self.rules, self.era(), give_beyond_ho=max(self.give, p.give))
            return
        for _ in range(3):
            alone = {p.name: solve(p, self.rules, self.era(), *self.offers[p.name], give_beyond_ho=self.give) for p in self.plots}
            for p in self.plots:
                offer, need = {}, {}
                for q in self.plots:
                    if q is p:
                        continue
                    s = alone[q.name]
                    # the Yard comes before the market: a neighbour's whole spare is on offer, and what it already
                    # sells to this plot stays on offer to it
                    for g in set(s['unsold']) | set(s['sold']) | set(s['sold_crew']):
                        v = s['unsold'].get(g, 0) + s['sold'].get(g, 0) + s['sold_crew'].get(g, 0) * 0.25
                        if v > 1e-6:
                            offer[g] = (offer.get(g, (0, 0))[0] + v, self.rules.crew_price)
                    for g, v in s['want'].items():
                        if v > 1e-6:
                            need[g] = need.get(g, 0) + v
                self.offers[p.name] = (offer, need)
        self.share_room()
        for p in self.plots:
            self.state[p.name] = solve(p, self.rules, self.era(), *self.offers[p.name], give_beyond_ho=max(self.give, p.give), extra_ho=self.extra.get(p.name))

    def share_room(self):
        """The Yard's market room: what a neighbour doesn't sell of its Hub Order, the others' overflow may use"""
        self.extra = {}
        if not self.crew or not getattr(self, 'pool_ho', False):
            return
        st = {p.name: solve(p, self.rules, self.era(), *self.offers[p.name], give_beyond_ho=self.give) for p in self.plots}
        goods = set()
        for s in st.values():
            goods |= set(s['own_hos'])
        from econ import hub_order
        for g in goods:
            unused = 0.0
            over = {}
            for p in self.plots:
                s = st[p.name]
                ho = s['own_hos'].get(g, hub_order(g, self.rules, self.era()) * (self.rules.post_ho ** (p.posts[0] - 1) if self.rules.post_ho else 1))
                sold = s['sold'].get(g, 0) + s['unsold'].get(g, 0)
                if sold < ho:
                    unused += ho - sold
                else:
                    over[p.name] = sold - ho
            tot = sum(over.values())
            if tot <= 0 or unused <= 0:
                continue
            for name, o in over.items():
                self.extra.setdefault(name, {})[g] = unused * o / tot

    # ---- spending
    def candidates(self, p):
        out = []
        for t in B:
            if not p.unlocked(t):
                continue
            spot = p.spot(t)
            if spot:
                out.append(('build', t, spot, p.build_cost(t, self.rules)))
            elif B[t]['era'] >= 3 and not B[t]['place'].startswith('dep:'):
                room = self.make_room(p, t)
                if room:
                    out.append(('replace', t, room, p.build_cost(t, self.rules)))
        lowest = {}
        for i, (t, lvl, m, paid, *_) in enumerate(p.buildings):
            if lvl >= self.rules.max_level:
                continue
            key = t
            # the cheapest step of each type: what a copy cost sets the price of its levels
            cost = self.up_cost(p, i)
            score = cost / (m * level_mult(lvl))
            if key not in lowest or score < lowest[key][1]:
                lowest[key] = (i, score, cost)
        for t, (i, _, cost) in lowest.items():
            out.append(('up', t, i, cost))
        if self.rules.post_ho:
            if p.posts[0] < self.rules.max_level:
                out.append(('post', None, 0, self.rules.all_costs * self.rules.post_cost * self.rules.post_factor ** (p.posts[0] - 1)))
            return out
        lp = min(range(len(p.posts)), key=lambda i: p.posts[i])
        if p.posts[lp] < self.rules.max_level:
            out.append(('post', None, lp, 2500 * self.rules.copy_factor ** max(0, lp - 1) * self.rules.level_factor ** (p.posts[lp] - 1) * self.mats(p.posts[lp])))
        if p.pools.get('general', 0) >= 1:
            pc = getattr(self, 'post_copy', None) or self.rules.copy_factor
            out.append(('newpost', None, None, 2500 * pc ** p.post_copies))
        return out

    def value(self, p, i, research, mv):
        """What building i adds a minute now, at what its goods are worth"""
        t, lvl, m, *_ = p.buildings[i]
        d = B[t]
        f, l, cx = research.get(d['track'], (1, 1, 1))
        return (sum(mv.get(g, 0) * v for g, v in d['out'].items()) - sum(mv.get(g, 0) * v * l for g, v in d['inp'].items())) * m * level_mult(lvl) * f * cx

    def make_room(self, p, t):
        """Land for a later Era's building on a full plot (phase C): the older buildings that add least a tile, knocked
        down in the pool where that loses least; (pool, mult, indices) or None"""
        from plot import POOLS, MULT
        from econ import track_mults, tiers_of
        mv = getattr(p, '_mv', None)
        if mv is None:
            return None
        research = {k: track_mults(tiers_of(p, k), k) for k in set(p.tiers) | {'coal'}}
        size = B[t]['size']
        place = B[t]['place']
        best = None
        for pool in POOLS[place]:
            have = p.pools.get(pool, 0)
            olds = sorted(((self.value(p, i, research, mv) / B[b[0]]['size'], i) for i, b in enumerate(p.buildings)
                           if len(b) > 4 and b[4] == pool and B[b[0]]['era'] < B[t]['era']), key=lambda e: e[0])
            idx, lost = [], 0.0
            for per, i in olds:
                if have >= size:
                    break
                idx.append(i)
                have += B[p.buildings[i][0]]['size']
                lost += per * B[p.buildings[i][0]]['size']
            if have >= size and (best is None or lost < best[0]):
                best = (lost, (pool, MULT.get(pool, {}).get(place, 1.0), idx))
        return best[1] if best else None

    def up_gold(self, p, i):
        t, lvl, m, paid, *_ = p.buildings[i]
        base = paid if self.rules.copy_levels else B[t]['gold']
        return base * self.rules.level_factor ** (lvl - 1)

    def up_mats(self, p, i):
        from econ import materials
        if not self.rules.upgrade_mats:
            return {}
        t, lvl, m, paid, *_ = p.buildings[i]
        return materials(B[t]['era'], lvl, self.up_gold(p, i), self.rules.mats_share)

    def up_cost(self, p, i):
        """Gold, plus the goods at what they cost this plot now: its own spare at what the market would pay, a
        neighbour's at the crew price, the market's at 2.5x"""
        from content import PRICE
        cost = self.up_gold(p, i)
        st = self.state.get(p.name)
        mv = getattr(p, '_mv', None)
        for g, u in self.up_mats(p, i).items():
            spare = (st['sold'].get(g, 0) + st['unsold'].get(g, 0)) if st else 0
            if spare > u / 120:
                cost += u * (mv.get(g, PRICE[g]) if mv else PRICE[g])
            elif g in self.offers[p.name][0]:
                cost += u * PRICE[g] * self.rules.crew_price
            else:
                cost += u * PRICE[g] * self.rules.import_mult
        return cost

    def mats(self, lvl):
        return 1

    def apply(self, p, c):
        kind, t, arg, cost = c
        if kind == 'build':
            p.build(t, arg, self.rules)
            return ('build', t)
        if kind == 'replace':
            pool, mult, idx = arg
            for i in sorted(idx, reverse=True):
                p.free(i)
            p.build(t, (pool, mult), self.rules)
            return ('build', t)
        if kind == 'up':
            p.buildings[arg][1] += 1
            return ('up', t)
        if kind == 'newpost':
            p.posts.append(1)
            p.post_copies += 1
            p.pools['general'] -= 1
            return ('post', None)
        p.posts[arg] += 1
        return ('post', None)

    def undo(self, p, c, snap):
        p.buildings, p.posts, p.pools, p.copies, p.post_copies = snap

    def snapshot(self, p):
        return ([list(b) for b in p.buildings], list(p.posts), {k: ([list(e) for e in v] if isinstance(v, list) else v) for k, v in p.pools.items()}, dict(p.copies), p.post_copies)

    def marginal(self, p, st):
        """What one more unit of each good is worth to this plot now"""
        from content import PRICE
        from econ import hub_order
        mv = {}
        rules = self.rules
        for g in PRICE:
            price = PRICE[g]
            short = st['short'].get(g, 0)
            if st['unsold'].get(g, 0) > 1e-6:
                mv[g] = 0.0
            elif g in st['imports'] and st['imports'][g] > 1e-6:
                mv[g] = price * rules.import_mult
            elif short > 1e-6:
                mv[g] = price * 2.0
            elif g in st['sold']:
                s = st['sold'][g]
                ho = st['hos'].get(g, hub_order(g, rules, self.era()))
                if s <= ho:
                    mv[g] = price
                elif s <= ho / rules.ho_floor ** 2:
                    mv[g] = price * 0.5 * (ho / s) ** 0.5
                else:
                    mv[g] = price * rules.ho_floor
            else:
                mv[g] = price
        return mv

    def rough(self, p, c, research, mv):
        """Value added a minute by one more unit of this building, at what the goods are worth now, over its cost"""
        kind, t, arg, cost = c
        if kind in ('post', 'newpost'):
            return 9e9
        d = B[t]
        f, l, cx = research.get(d['track'], (1, 1, 1))
        lost = 0.0
        if kind == 'build':
            du = arg[1]
        elif kind == 'replace':
            du = arg[1]
            lost = sum(self.value(p, i, research, mv) for i in arg[2])
        else:
            tt, lvl, m, paid, *_ = p.buildings[arg]
            du = m * (level_mult(lvl + 1) - level_mult(lvl))
        va = (sum(mv[g] * v for g, v in d['out'].items()) - sum(mv[g] * v * l for g, v in d['inp'].items())) * du * f * cx
        return (va - lost) / cost

    def best(self, p, base, keep=10):
        from econ import track_mults
        from econ import tiers_of
        research = {k: track_mults(tiers_of(p, k), k) for k in set(p.tiers) | {'coal'}}
        mv = self.marginal(p, self.econ(p))
        p._mv = mv
        # what the market would pay feeds which imports pay: solve again with it, so every candidate compares alike
        base = self.econ(p)['income']
        cands = self.candidates(p)
        cands.sort(key=lambda c: -self.rough(p, c, research, mv))
        best = None
        for c in cands[:keep]:
            snap = self.snapshot(p)
            self.apply(p, c)
            inc = self.econ(p)['income']
            self.undo(p, c, snap)
            gain = inc - base
            if gain <= 1e-9:
                continue
            payback = c[3] / gain
            if best is None or payback < best[0]:
                best = (payback, c, gain)
        return best

    def next_research(self, p):
        import econ
        for track, upto in p.plan:
            n = p.tiers.get(track, 0)
            if n >= upto or (track == 'coal' and econ.COAL_IN):
                continue
            if TRACK_ERA.get(track, 1) > p.era:
                continue
            return track, n
        return None

    def research_cost(self, track, n):
        era = TRACK_ERA.get(track, 1)
        base = self.rules.tier_gold[n] if self.rules.tier_gold else TIERS[n][2]
        gold = base * 4 ** (era - 1) * self.rules.research_scale * self.rules.all_costs
        return gold * (1.2 if n >= 2 else 1)

    def try_research(self, p):
        if p.level < 4 or (p.research and p.research[1] > self.t):
            return 'busy'
        nxt = self.next_research(p)
        if not nxt:
            return 'none'
        cost = self.research_cost(*nxt)
        if p.gold >= cost:
            p.spend_gold('research', cost)
            p.research = (nxt, self.t + TIERS[nxt[1]][0] * 60)
            return 'started'
        inc = self.state[p.name]['income']
        return 'save' if cost <= self.save * max(inc, 1) else 'far'

    def try_tree(self, p):
        """The Era tree on the second research line: one item at a time, in order, as soon as the gold is there"""
        from content import TREE, PIPE_RELAY, PIPE_TILES_PER_PARCEL
        if not self.rules.era_tree or p.level < 4 or p.tree_line:
            return 'busy'
        for item, era, hours, gold, opens in TREE:
            if item in p.tree or era > self.era():
                continue
            if item == 'copperPipes' and p.level >= 8:
                gold, hours = 0, 0  # everyone gets copper pipes at Level 8
            cost = gold * self.rules.all_costs
            if opens and opens.startswith('pipe:'):
                tiles = (4 + p.parcels + p.outskirts) * PIPE_TILES_PER_PARCEL
                cost += tiles * PIPE_RELAY[opens[5:]]
            if p.gold >= cost:
                p.spend_gold('research', cost)
                p.tree_line = (item, self.t + hours * 60)
                return 'started'
            inc = self.state[p.name]['income']
            return 'save' if cost <= self.save * max(inc, 1) else 'far'
        return 'none'

    def finish_tree(self, p):
        from content import TREE
        if p.tree_line and p.tree_line[1] <= self.t:
            item = p.tree_line[0]
            p.tree.add(item)
            p.tree_line = None
            opens = next(t[4] for t in TREE if t[0] == item)
            if opens and opens.startswith('pipe:'):
                p.pipe = opens[5:]
            if opens == 'deep':
                from plot import DEEP
                p.add_pools(DEEP)
            return True
        return False

    def try_parcel(self, p):
        if p.parcels >= 12 or p.level < self.rules.parcel_levels[p.parcels] or not p.parcels_left:
            return 'no'
        cost = self.rules.all_costs * self.rules.parcel_first * self.rules.parcel_factor ** p.parcels
        inc = self.state[p.name]['income']
        if p.gold >= cost:
            name, pools = p.parcels_left.pop(0)
            p.add_pools(pools)
            p.parcels += 1
            p.spend_gold('parcels', cost)
            p.gain_xp(100)
            return 'bought'
        return 'save' if cost <= self.save * max(inc, 1) else 'far'

    def try_outskirts(self, p):
        if self.era() < 2 or not p.outskirts_left or (self.rules.era_tree and 'survey' not in p.tree):
            return 'no'
        cost = self.rules.out_first * self.rules.out_factor ** p.outskirts
        inc = self.state[p.name]['income']
        if p.gold >= cost:
            name, pools = p.outskirts_left.pop(0)
            p.add_pools(pools)
            p.outskirts += 1
            p.spend_gold('outskirts', cost)
            p.gain_xp(100)
            return 'bought'
        return 'save' if cost <= self.save * max(inc, 1) else 'far'

    def try_outpost(self, p):
        """Era 4: up to three outposts in the wilderness, bought once (20B x 2^n), each on a rich deposit"""
        from plot import OUTPOST_RICH, OUTPOST_LAND
        n = getattr(p, 'outposts', 0)
        if self.era() < 4 or 'outposts' not in p.tree or n >= 3:
            return 'no'
        cost = self.rules.outpost_first * 2 ** n
        inc = self.state[p.name]['income']
        if p.gold >= cost:
            # what the plot lacks first (a Discovery finds titanium ore), then geothermal heat, rare earth, oil
            kinds = ([p.lacks2] if p.lacks2 in ('oil', 'rareEarth', 'titaniumOre') else []) + ['geothermal', 'rareEarth', 'oil']
            p.add_pools({f'dep:{kinds[n % len(kinds)]}': [OUTPOST_RICH], 'general': OUTPOST_LAND})
            p.outposts = n + 1
            p.spend_gold('outposts', cost)
            return 'bought'
        return 'save' if cost <= self.save * max(inc, 1) else 'far'

    def pay_mats(self, p, i):
        """The goods of a level: what a neighbour sells goes to them at the crew price (counted for the record)"""
        from content import PRICE
        st = self.state.get(p.name)
        for g, u in self.up_mats(p, i).items():
            spare = (st['sold'].get(g, 0) + st['unsold'].get(g, 0)) if st else 0
            if spare > u / 120:
                p.mats_own = getattr(p, 'mats_own', 0) + u * PRICE[g]
                continue
            sellers = [(q, self.state[q.name]['sold'].get(g, 0) + self.state[q.name]['unsold'].get(g, 0)) for q in self.plots if q is not p and self.crew]
            sellers = [(q, v) for q, v in sellers if v > 1e-6]
            if sellers:
                tot = sum(v for q, v in sellers)
                gold = u * PRICE[g] * self.rules.crew_price
                for q, v in sellers:
                    q.gold += gold * v / tot
                    q.mats_sold = getattr(q, 'mats_sold', 0) + gold * v / tot
                p.mats_crew = getattr(p, 'mats_crew', 0) + gold
            else:
                p.mats_market = getattr(p, 'mats_market', 0) + u * PRICE[g] * self.rules.import_mult

    def add_mats(self, p, i):
        for g, u in self.up_mats(p, i).items():
            p.mat_backlog = getattr(p, 'mat_backlog', {})
            p.mat_backlog[g] = p.mat_backlog.get(g, 0) + u
        self.set_mat_rate(p)

    def set_mat_rate(self, p):
        p.mat_rate = {g: u / 120 for g, u in getattr(p, 'mat_backlog', {}).items() if u > 1e-6}

    def fund_wonder(self, p):
        """Nothing left worth buying: the gold goes to the Wonder (it buys the goods at the market's price), the spare
        beyond the Hub Orders too"""
        if self.state[p.name]['income'] <= 0:
            return
        keep = self.state[p.name]['income'] * 60 * 2
        g = max(0.0, p.gold - keep)
        if g <= 0:
            return
        if self.rules.monument:
            first, growth, perk = self.rules.monument
            m = g * self.rules.endgame_split
            p.spend_gold('monument', m)
            self.mon['paid'] += m
            while True:
                cost = first * growth ** self.mon['stage']
                if self.mon['paid'] < cost:
                    break
                self.mon['paid'] -= cost
                self.mon['stage'] += 1
                for q in self.plots:
                    q.perk = 1 + perk * self.mon['stage']
                    q.monument_stage = self.mon['stage']
            g -= m
        p.spend_gold('wonder', g)
        p.gift_gold += g / self.rules.import_mult
        self.wonder['given'] += g / self.rules.import_mult
        p.gain_xp(g / (25 * p.level))

    def spend(self, p, budget=None):
        """budget: actions this look may take (one building placed, one Max upgrade of one building, one research
        or parcel); None plays like a perfect bot"""
        self.crew_round()
        actions = 0
        for _ in range(400):
            if budget is not None and actions >= budget:
                p.stop = 'out of time'
                return
            st = self.state[p.name]
            tr = self.try_tree(p)
            if tr == 'started':
                actions += 1
                self.finish_tree(p)
                self.state[p.name] = self.econ(p)
                continue
            if tr == 'save':
                p.stop = 'save research'
                return
            r = self.try_research(p)
            if r == 'started':
                actions += 1
                continue
            q = self.try_parcel(p)
            if q == 'bought':
                actions += 1
                self.state[p.name] = self.econ(p)
                continue
            o = self.try_outskirts(p)
            if o == 'bought':
                actions += 1
                self.state[p.name] = self.econ(p)
                continue
            if o == 'save':
                q = 'save'
            w = self.try_outpost(p)
            if w == 'bought':
                actions += 1
                self.state[p.name] = self.econ(p)
                continue
            if w == 'save':
                q = 'save'
            if r == 'save' or q == 'save':
                p.stop = 'save research' if r == 'save' else 'save parcel'
                return
            base = self.econ(p)['income']
            b = self.best(p, base)
            if not b:
                p.stop = 'nothing to buy'
                p.last_payback = 9e9
                self.fund_wonder(p)
                return
            payback, c, gain = b
            p.last_payback = payback
            if payback > self.max_payback:
                p.stop = 'paybacks too long'
                self.fund_wonder(p)
                return
            if p.gold < c[3]:
                p.stop = 'saving'
                return
            if c[0] == 'up':
                gold = c[3]
                self.pay_mats(p, c[2])
            else:
                gold = c[3]
            what = self.apply(p, c)
            p.spend_gold('buildings' if c[0] in ('build', 'replace') else 'levels', gold)
            p.gain_xp(10 + 50 * (p.copies.get(c[1], 0) == 1) if c[0] == 'build' else 2)
            actions += 1
            # Max: an upgrade keeps going on the same building while each next step pays back in time
            if c[0] == 'up' and budget is not None:
                i = c[2]
                for _ in range(24):
                    if p.buildings[i][1] >= self.rules.max_level:
                        break
                    cost = self.up_cost(p, i)
                    gold = self.up_gold(p, i)
                    if p.gold < cost:
                        break
                    st0 = self.econ(p)['income']
                    p.buildings[i][1] += 1
                    gain = self.econ(p)['income'] - st0
                    if gain <= 0 or cost / gain > self.max_payback:
                        p.buildings[i][1] -= 1
                        break
                    p.buildings[i][1] -= 1
                    self.pay_mats(p, i)
                    p.buildings[i][1] += 1
                    p.spend_gold('levels', cost)
                    p.gain_xp(2)
            self.state[p.name] = self.econ(p)

    def finish_research(self, p):
        if p.research and p.research[1] <= self.t:
            (track, n), _ = p.research
            p.tiers[track] = n + 1
            p.research = None
            p.gain_xp(40)
            return True
        return False

    # ---- time
    def run(self, days, sample=None):
        evs = []
        for p in self.plots:
            for s, l in sessions(p.schedule, days):
                evs.append((s, l, p))
        evs.sort(key=lambda e: e[0])
        self.crew_round()
        end = days * DAY
        i = 0
        samples = []
        next_sample = 0
        while self.t < end:
            nxt = evs[i][0] if i < len(evs) else end
            # until the next visit: samples and research finishing offline (the next tier starts by itself if the gold
            # is there), in the order they happen
            while True:
                soon = [p.research[1] for p in self.plots if p.research and self.t < p.research[1] <= nxt]
                soon += [p.tree_line[1] for p in self.plots if p.tree_line and self.t < p.tree_line[1] <= nxt]
                t_r = min(soon) if soon else float('inf')
                if next_sample <= min(t_r, nxt) and next_sample < end:
                    self.advance(next_sample)
                    samples.append(self.sample())
                    next_sample += sample or DAY
                    continue
                if t_r <= nxt:
                    self.advance(t_r)
                    for p in self.plots:
                        if self.finish_research(p):
                            self.try_research(p)
                            self.crew_round()
                        if self.finish_tree(p):
                            self.try_tree(p)
                            self.crew_round()
                    continue
                break
            self.advance(min(nxt, end))
            if i >= len(evs) or self.t >= end:
                break
            start, length, p = evs[i]
            i += 1
            for p2 in self.plots:
                p2.era = max(p2.era, self.era())
            # within the session, the player looks every 2 minutes
            m = 0
            while m < length:
                for q in self.plots:
                    if self.finish_research(q):
                        self.try_research(q)
                    self.finish_tree(q)
                rate = getattr(p, 'actions_per_min', None)
                self.spend(p, None if rate is None else rate * 2)
                self.advance(self.t + 2)
                m += 2
            p.bank_end = getattr(p, 'bank_end', []) + [(self.t, p.gold, self.state[p.name]['income'], getattr(p, 'last_payback', 0), getattr(p, 'stop', ''), p.level)]
        return samples

    def advance(self, t):
        # while goods for levels are still coming in, the flows change: step in 20 minutes and solve again
        while t - self.t > 20 and any(getattr(p, 'mat_backlog', None) and sum(p.mat_backlog.values()) > 1e-3 for p in self.plots):
            self._advance(self.t + 20)
            self.crew_round()
        self._advance(t)

    def _advance(self, t):
        dt = t - self.t
        if dt <= 0:
            return
        for p in self.plots:
            st = self.state[p.name]
            p.gold += st['income'] * dt
            p.earned += st['earned'] * dt
            from content import PRICE
            if getattr(p, 'mat_backlog', None):
                for g in list(p.mat_backlog):
                    got = (st['mats_own'].get(g, 0) + st['mats_crew'].get(g, 0) + st['mats_market'].get(g, 0)) * dt
                    p.mat_backlog[g] = max(0.0, p.mat_backlog[g] - got)
                    p.mats_paid = getattr(p, 'mats_paid', 0) + (st['mats_crew'].get(g, 0) * self.rules.crew_price + st['mats_market'].get(g, 0) * self.rules.import_mult + st['mats_own'].get(g, 0)) * PRICE[g] * dt
                self.set_mat_rate(p)
                if sum(p.mat_backlog.values()) <= 1e-3:
                    p.mat_rate = {}
            p.gift_goods += sum(v * PRICE[g] for g, v in st['gifts'].items()) * dt
            p.gain_xp(sum(v * PRICE[g] for g, v in st['gifts'].items()) * dt / (25 * p.level))
            p.gain_earned(st['earned'] * dt)
        self.t = t

    def sample(self):
        out = {'day': self.t / DAY}
        for p in self.plots:
            st = self.state[p.name]
            lv = [b[1] for b in p.buildings]
            out[p.name] = dict(income=st['income'], gold=p.gold, level=p.level, n=len(p.buildings),
                               avg_level=sum(lv) / len(lv) if lv else 0, parcels=p.parcels, tiers=dict(p.tiers),
                               earned=p.earned, spent=dict(p.spent), market=st['gold_market'], crew=st['gold_crew'],
                               crew_cost=st['cost_crew'], imports=st['cost_market'], gift_goods=p.gift_goods, gift_gold=p.gift_gold,
                               outskirts=p.outskirts, buildings=[list(b) for b in p.buildings], post=p.posts[0],
                               monument=getattr(p, 'monument_stage', 0), era=self.era(), outposts=getattr(p, 'outposts', 0), mats_paid=getattr(p, 'mats_paid', 0),
                               mats_own=getattr(p, 'mats_own', 0), mats_crew=getattr(p, 'mats_crew', 0), mats_market=getattr(p, 'mats_market', 0), mats_sold=getattr(p, 'mats_sold', 0))
        return out

import sys, json, time
from econ import Rules
from plot import Plot
from sim import Sim, DAY
from run2 import wide

ALL = ['power', 'iron', 'copper', 'coal', 'stone', 'wood', 'sand']

ALL2 = ['steel', 'wire', 'concrete', 'aluminium']

def spec(main, quick, main2=None):
    # the first two tiers of everything, the main track to master, then the Era-2 track, then the rest wide; main2 may
    # list a main track for every later Era (phase C), and the later Eras' tracks go wide from their first tier (a
    # track of an Era the town hasn't reached is skipped, so Eras 1 and 2 play as before)
    from content import TRACKS3, TRACKS4, TRACKS5
    plan = [(t, 2) for t in ALL] + [(main, 9)]
    for m in ([main2] if isinstance(main2, str) else main2 or []):
        plan += [(m, 9)]
    plan += [(t, n) for n in range(3, 10) for t in ALL + ALL2]
    plan += [(t, n) for n in range(1, 10) for t in TRACKS3 + TRACKS4 + TRACKS5]
    return plan

def crew_plots(apm, schedule='regular', coordinated=False):
    ps = [Plot('Ana', lacks='copperOre', lacks2='rareEarth', strength='coal', plan=spec('power', ['iron', 'wood'], 'aluminium')),
          Plot('Ben', lacks='coal', lacks2='bauxite', strength='ironOre', plan=spec('iron', ['power', 'wood'], 'steel')),
          Plot('Cleo', lacks='stone', lacks2='oil', strength='sand', plan=spec('sand', ['power', 'iron'], 'concrete')),
          Plot('Dev', lacks='sand', lacks2='titaniumOre', strength='copperOre', plan=spec('copper', ['power', 'iron'], 'wire'))]
    if coordinated:
        ps[0].allowed = {'power', 'wood'}
        ps[1].allowed = {'power', 'iron', 'stone'}
        ps[2].allowed = {'power', 'sand', 'coal'}
        ps[3].allowed = {'power', 'copper', 'coal'}
    for p in ps:
        p.actions_per_min = apm
        p.schedule = schedule
    return ps

if __name__ == '__main__':
    cfg = json.loads(sys.argv[1]); out = sys.argv[2]
    rules = Rules(**cfg.get('rules', {}))
    days = cfg.get('days', 14)
    t0 = time.time()
    ps = crew_plots(cfg.get('apm'), coordinated=cfg.get('coordinated', False))
    if cfg.get('plan') == 'wide':
        for p in ps:
            p.plan = [(t, n) for n in range(1, 10) for t in ALL + ALL2]
    sim = Sim(rules, ps, max_payback_h=cfg.get('max_payback_h', 120), crew=cfg.get('trade', True), town_era_at={2: cfg['era2'] * 1440} if cfg.get('era2') else None)
    sim.pool_ho = cfg.get('pool', False)
    samples = sim.run(days)
    rows = []
    for s in samples:
        row = {'day': round(s['day'], 2)}
        for p in ps:
            d = s[p.name]
            row[p.name] = dict(day=row['day'], inc=round(d['income']), L=d['level'], n=d['n'], avgL=round(d['avg_level'], 1), crew=round(d['crew']), crew_cost=round(d['crew_cost']), imports=round(d['imports']), market=round(d['market']), earned=round(d['earned']), mats_sold=round(d['mats_sold']), mats_crew=round(d['mats_crew']), mats_market=round(d['mats_market']), monument=d['monument'], spent=round(sum(d['spent'].values())), tiers=d['tiers'])
        rows.append(row)
    json.dump(dict(cfg=cfg, rows=rows, took=time.time() - t0), open(out, 'w'))

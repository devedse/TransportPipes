# Every Era-1 track mastered against the same plot played wide: how much going deep is worth for each track
import sys, json, time
from econ import Rules
from plot import Plot
from sim import Sim, DAY
from crew import ALL, ALL2

# the plot each track is tried on: its own rich deposit, lacking a basic its chain doesn't use
PLOTS = {
    'power': dict(lacks='copperOre', strength='coal'),
    'iron': dict(lacks='sand', strength='ironOre'),
    'copper': dict(lacks='sand', strength='copperOre'),
    'coal': dict(lacks='sand', strength='coal'),
    'stone': dict(lacks='sand', strength='stone'),
    'sand': dict(lacks='stone', strength='sand'),
    'wood': dict(lacks='sand', strength='ironOre'),
}

if __name__ == '__main__':
    cfg = json.loads(sys.argv[1]); out = sys.argv[2]
    track, mode = cfg['track'], cfg['mode']
    rules = Rules(**cfg.get('rules', {}))
    ALL = [t for t in ALL if not (rules.coal_in and t == 'coal')]
    if mode == 'deep':
        plan = [(t, 2) for t in ALL] + [(track, 9)] + [(t, n) for n in range(3, 10) for t in ALL + ALL2]
    else:
        plan = [(t, n) for n in range(1, 10) for t in ALL + ALL2]
    p = Plot(track, lacks2='oil', plan=plan, **PLOTS[track])
    p.actions_per_min = cfg.get('apm', 6)
    sim = Sim(rules, [p], max_payback_h=120, town_era_at={2: cfg['era2'] * DAY} if cfg.get('era2') else None)
    t0 = time.time()
    samples = sim.run(cfg.get('days', 35))
    rows = [dict(day=round(s['day'], 2), inc=round(s[track]['income']), L=s[track]['level'], earned=round(s[track]['earned']),
                 tiers=s[track]['tiers']) for s in samples]
    json.dump(dict(cfg=cfg, rows=rows, took=time.time() - t0), open(out, 'w'))

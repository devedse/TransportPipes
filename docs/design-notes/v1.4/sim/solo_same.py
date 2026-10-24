# solo players on the same four plots, each playing alone
import sys, json
from econ import Rules
from sim import Sim
from crew import crew_plots
cfg = json.loads(sys.argv[1]); out = sys.argv[2]
rules = Rules(**cfg.get('rules', {}))
res = {}
for p in crew_plots(cfg.get('apm')):
    p.plan = [(t, n) for n in range(1, 10) for t in ['power', 'iron', 'copper', 'coal', 'stone', 'wood', 'sand']]
    sim = Sim(rules, [p], max_payback_h=cfg.get("max_payback_h", 120), town_era_at={2: cfg["era2"] * 1440} if cfg.get("era2") else None)
    samples = sim.run(cfg.get('days', 14))
    res[p.name] = [dict(day=round(s['day'], 2), inc=round(s[p.name]['income']), L=s[p.name]['level'], earned=round(s[p.name]['earned']), imports=round(s[p.name]['imports'])) for s in samples]
json.dump(res, open(out, 'w'))

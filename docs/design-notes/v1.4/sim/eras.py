# Phase C: one regular player through all five Eras, the town's Eras on the days the design aims at (Era 2 on day 12,
# then three to five weeks each: Era 3 on day 33, 4 on day 61, 5 on day 89, the Launch about day 120). What it gives
# in each Era, times eight, sizes that Era's Wonder for a town of eight (the Great Dynamo and the Copper Cathedral were
# sized this way in §17).
import sys, json, time
from econ import Rules
from plot import Plot
from sim import Sim, DAY
from crew import spec

cfg = json.loads(sys.argv[1]); out = sys.argv[2]
rules = Rules(**cfg['rules'])
mains = cfg.get('mains', ['iron', 'steel', 'oil', 'circuits', 'spaceframes'])
p = Plot('Jo', lacks=cfg.get('lacks', 'sand'), lacks2=cfg.get('lacks2', 'oil'), strength='ironOre', schedule=cfg.get('schedule', 'regular'),
         plan=spec(mains[0], [], mains[1:]))
p.actions_per_min = cfg.get('apm', 6)
eras = {int(k): v * DAY for k, v in cfg.get('eras', {'2': 12, '3': 33, '4': 61, '5': 89}).items()}
sim = Sim(rules, [p], max_payback_h=cfg.get('max_payback_h', 120), town_era_at=eras)
t0 = time.time()
samples = sim.run(cfg.get('days', 125), sample=DAY)
rows = []
for s in samples:
    d = s['Jo']
    rows.append(dict(day=round(s['day'], 3), era=d['era'], inc=d['income'], gold=d['gold'], L=d['level'], n=d['n'], avgL=d['avg_level'],
                     parcels=d['parcels'], outskirts=d['outskirts'], outposts=d['outposts'], tiers=d['tiers'], earned=d['earned'],
                     spent=d['spent'], gift_goods=d['gift_goods'], gift_gold=d['gift_gold'], monument=d['monument'],
                     kinds=sorted({b[0] for b in d['buildings']})))
json.dump(dict(cfg=cfg, rows=rows, took=time.time() - t0, tree=sorted(p.tree), pipe=p.pipe), open(out, 'w'))

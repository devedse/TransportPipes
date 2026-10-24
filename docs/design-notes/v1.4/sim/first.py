# The first evening, five minutes at a time
import json
from econ import Rules
from plot import Plot
from sim import Sim
from crew import spec
base = json.loads('{' + open('final_rules.txt').read() + '}')
rules = Rules(**base)
import econ
econ.TRACK_STRENGTH.update(rules.track_strength)
p = Plot('Jo', lacks='sand', lacks2='oil', strength='ironOre', schedule='regular', plan=spec('iron', [], 'steel'))
p.actions_per_min = 2
sim = Sim(rules, [p], max_payback_h=120)
rows = []
sim.crew_round()
sim.t = 19 * 60
for m in range(0, 121, 2):
    for q in sim.plots:
        sim.finish_research(q)
        sim.finish_tree(q)
    sim.spend(p, 4)
    if m % 10 == 0:
        st = sim.econ(p)
        from collections import Counter
        c = Counter(b[0] for b in p.buildings)
        rows.append(dict(minute=m, income=round(st['income']), level=p.level, pipe=p.pipe, tree=sorted(p.tree), gold=round(p.gold), earned=round(p.earned), buildings=dict(c), tiers=dict(p.tiers), post=p.posts[0]))
    sim.advance(sim.t + 2)
for r in rows: print(r)
json.dump(rows, open('out/first.json', 'w'))

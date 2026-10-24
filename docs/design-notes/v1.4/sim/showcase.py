# One player in detail: samples four times a day, with the whole base
import sys, json, time
from econ import Rules
from plot import Plot
from sim import Sim, DAY
from crew import spec

cfg = json.loads(sys.argv[1]); out = sys.argv[2]
rules = Rules(**cfg['rules'])
p = Plot('Jo', lacks=cfg.get('lacks', 'sand'), lacks2=cfg.get('lacks2', 'oil'), strength='ironOre', schedule=cfg.get('schedule', 'regular'),
         plan=spec(cfg.get('main', 'iron'), [], cfg.get('main2', 'steel')))
p.actions_per_min = cfg.get('apm', 2)
sim = Sim(rules, [p], max_payback_h=cfg.get('max_payback_h', 120), town_era_at={2: cfg['era2'] * DAY} if cfg.get('era2') else None)
t0 = time.time()
samples = sim.run(cfg.get('days', 35), sample=DAY / 4)
rows = []
for s in samples:
    d = s['Jo']
    rows.append(dict(day=round(s['day'], 3), inc=d['income'], gold=d['gold'], L=d['level'], n=d['n'], avgL=d['avg_level'],
                     parcels=d['parcels'], outskirts=d['outskirts'], tiers=d['tiers'], earned=d['earned'], spent=d['spent'],
                     market=d['market'], imports=d['imports'], gift_goods=d['gift_goods'], gift_gold=d['gift_gold'],
                     post=d['post'], monument=d['monument'], mats_paid=d['mats_paid'], mats_own=d['mats_own'], mats_market=d['mats_market'],
                     buildings=d['buildings'] if abs(s['day'] * 4 - round(s['day'] * 4)) < 0.01 and round(s['day'] * 4) % 4 == 0 else None))
sess = [dict(t=t / DAY, bank=g, inc=i, pb=pb / 60, stop=st, L=l) for (t, g, i, pb, st, l) in p.bank_end]
st = sim.econ(p)
json.dump(dict(cfg=cfg, rows=rows, sessions=sess, took=time.time() - t0, final=dict(prod=st['prod'], sold=st['sold'], hos=st['hos'], used=st['used'])), open(out, 'w'))

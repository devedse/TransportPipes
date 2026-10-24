import sys, time, json
from econ import Rules
from plot import Plot
from sim import Sim, DAY

def wide(tracks, top=9):
    return [(t, n) for n in range(1, top + 1) for t in tracks]

def report(p, samples, every=1):
    for s in samples:
        d = s[p.name]
        sp = sum(d['spent'].values())
        cats = ' '.join(f"{k[:5]} {v/max(sp,1):.0%}" for k, v in sorted(d['spent'].items()))
        print(f"d{s['day']:4.1f} inc {d['income']:8.0f} gold {d['gold']:11.0f} L{d['level']:2d} b{d['n']:3d} avgL {d['avg_level']:4.1f} par {d['parcels']:2d} earned {d['earned']/1e6:8.2f}M spent {sp/d['earned'] if d['earned'] else 0:4.0%} [{cats}] tiers {sum(d['tiers'].values())}")

def sessions_report(p, days=None):
    for (t, gold, inc, pb, stop, lvl) in p.bank_end:
        if days and int(t // DAY) not in days:
            continue
        h = int((t % DAY) // 60); m = int(t % 60)
        print(f"  day {int(t//DAY)} {h:02d}:{m:02d} L{lvl} bank {gold:11.0f} = {gold/max(inc,1)/60:5.1f} h of income; best payback {pb/60:6.1f} h; stop: {stop}")

if __name__ == '__main__':
    days = int(sys.argv[1]) if len(sys.argv) > 1 else 7
    rules = Rules()
    p = Plot('solo', lacks='sand', strength='ironOre', plan=wide(['power', 'iron', 'copper', 'coal', 'stone', 'wood', 'sand']))
    sim = Sim(rules, [p])
    t0 = time.time()
    samples = sim.run(days)
    print('took', round(time.time() - t0, 1), 's')
    report(p, samples)
    sessions_report(p)

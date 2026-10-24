import json, sys
for f in sys.argv[1:]:
    r = json.load(open(f))
    print('==', f, 'took', round(r['took']))
    for row in r['rows']:
        if abs(row['day'] - round(row['day'])) > 0.01 or round(row['day']) not in (1, 2, 3, 5, 7, 10, 12, 14, 17, 21, 25, 28, 31, 34): continue
        sp = sum(row['spent'].values())
        cats = ' '.join(f"{k[:4]} {v/max(sp,1):.0%}" for k, v in sorted(row['spent'].items()))
        print(f"  d{row['day']:5.1f} inc {row['inc']:10.0f} L{row['L']:2d} b{row['n']:3d} avgL {row['avgL']:4.1f} par {row['parcels']:2d} out {row['outskirts']} mon {row['monument']} tiers {sum(row['tiers'].values()):2d} bank {row['gold']/max(row['inc'],1)/60:5.1f}h spent {sp/max(row['earned'],1):4.0%} [{cats}] wonder {row['gift_goods']/1e9 + row['gift_gold']/1e9:6.2f}B mats own/mkt {row.get('mats_own',0)/1e6:.0f}M/{row.get('mats_market',0)/1e6:.0f}M")
    by = {}
    for s in r['sessions']:
        by.setdefault(int(s['t']), []).append(s)
    print('   best payback / bank (h):', ' '.join(f"d{d}:{min(max(x['pb'] for x in v), 9999):.0f}/{max(x['bank']/max(x['inc'],1)/60 for x in v):.0f}" for d, v in sorted(by.items())))

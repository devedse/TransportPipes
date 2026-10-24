# Round 3 · economy

I read the draft plan and the four other round-2 papers, and checked every constant in the plan against the run's
data, the content files and the balance runs. My verdict on the whole: **accept the plan with amendments to F1 and F4
and one test added to the verification.** Every amendment below is arithmetic, not taste, and each has its check in
the appendix.

## Verdict per item

- **F0 · accept.** Add two diagnostics, not pass marks (the researcher's point): the share of the bank spent per
  session, and income per building.
- **F1 · amend, three exact changes:**
  - **(a) The floor covers only hub-bound spare.** Storage-bound and idle-capacity spare (F1.4) is paid only when it is
    taken. The hub would have paid nothing for those units: a Storage was keeping them, or the building wasn't making
    them. Pay the floor on them and every false match mints gold.
  - **(b) The project pays for what it takes.** Units taken by the project are paid by `SettleProjects` at base and
    get no floor. Otherwise they are paid twice: hub value plus base.
  - **(c) The probe is 0.5 units a minute per buyer and good.** The conservation test becomes: sellers receive = player
    buyers pay + the project pays base × taken + the seller's hub price × untaken hub-bound units.

  I accept leaving the untaken part out of the Hub Order, with a trigger in numbers: revisit if any seller's untaken
  share exceeds 10% of its gate volume over 24 h.
- **F1.5 · amend.** A NeighbourInNeed House's missing good is one the station's humans make, chosen again daily.
  Otherwise its trading post fixes nothing for Mia: her House wanted sand, and she made energy.
- **F2 · accept.**
- **F3 · accept.** The effort constant is right (appendix). The strongest evidence for it is in `docs/balance/`: the
  bot that does exactly what the advisor says (`balance-typical-advisor.md`) banks 107M at 2,955 g/min on day 28,
  which is 25 days of income. Even an obedient player hoards under today's advisor.
- **F4 · amend, four exact changes:**
  - **(a) Fewer than two goods made: fill in from the horizon.** When active residents make fewer than two goods, the
    two "made" goods may include goods makeable with one building unlocked within the stage's horizon. F4's own
    Foundation fix now starts the Water Tower at about 20:15 on day 1. Then all four are at Level 2 and make only
    energy, and the rule as written gives a one-good tower. Iron ore fills in: every estate gets an iron deposit in its
    start parcels ("Guaranteed basics", WorldGenerator.cs), and the Iron Mine unlocks at Level 2.
  - **(b) The unlock horizon is per stage:** Water Tower Level 5, Workshop Level 7, Foundry Hall and later Level 10.
    With "Level 5" for every stage the Workshop has no keystone: bricks and copper ingots need Level 6, glass Level 7.
  - **(c) The cap covers the whole basket.** The whole basket is at most 1.5 × the stage's hours of summed I(c), so 6 h
    / 9 h / 13.5 h. Above the cap the made goods shrink and the keystone stays. As written ("each made good … capped at
    6 h") three goods could reach 18 h.
  - **(d) "Delivering" means delivering.** A resident counts as delivering if they delivered to this stage or the last
    within 3 days. A 900-energy Foundation is worth less than 30 min of anyone's I(c), so no one could ever qualify
    (engineer E8d).
- **F5 · accept.** The Invest line is VOTE A.
- **F6 · accept, with my dissent attached.** At 1.75, the neighbour-fed `ChainSteps` will offer almost nothing in Era 1
  except the Glassworks.
- **F7 · accept.**
- **F8 · accept.** 90% gives Cleo's Lab "Max +8 · 27.5k g" (level 9, 10.7 research a minute) instead of +34 for 1.53M.
- **F9 · accept.** Dropping the fee waiver is fine: a faucet nobody missed. A repair is 10% of the building's price, 20 g
  for a moily (Events.cs:445).
- **F10 · accept.**
- **F11 · accept.** The research queue stays low.
- **Verification · amend one mark.** "t ≥ 15% between humans by day 14" becomes "t measured; target set by the crew bot
  run" (my dissent). Every other mark stands, including mine: the Water Tower by day 8, the Workshop by day 14, and Mia
  at 85–110% of Dev per building.

## VOTE A

**As proposed: the line with "Show me" opening the best upgrade one building at a time, smart-Max level preselected.**
Cleo's harm came from an unseen spend, and the advisor bot shows that right-sized steps get followed one at a time. With
F3's effort term the big steps reach the top on their own, so a one-tap spend of half the bank isn't needed. I withdraw
my one-tap.

## My last dissent

**Decide the one player price with the crew bot run before run 2: 1.75 or 1.5 × base.**

Crew-wide, a trade between friends is a transfer: the crew earns the chain's margin at any price. What the price
decides is whether the *buyer* has a reason to build. At 1.75, the buyer of a neighbour's input gains little or loses
on the core Era-1 chains:

| Chain, its input bought from a neighbour | Margin own-made | at 1.75 | at 1.5 |
| --- | --- | --- | --- |
| Small Factory, iron ore | +28 g/min | **+1** | +10 |
| Smelter, copper ore | +25 | **−1.25** | +7.5 |
| Sawmill, wood | +7 | −4.25 | −0.5 |
| Brickworks, stone | +12.5 | +1.25 | +5 |
| Glassworks, sand | +27 | +13.5 | +18 |

(Level-1 rates from buildings.yaml, outputs and energy at base.)

Copper is the one Era-1 good most seats can't make, and at 1.75 its chain can't cross estates. So F6's
interdependence will offer the Glassworks and little else, and t stays near 0. That inverts the 85–90% promise:
solo with Houses earns 0.75 + 0.25 × 1.25 = 1.0625 × the hub, against 1 + 0.75t crewed, so they break even at
t = 8%. At 1.5 both sides gain on iron and copper. The price order still holds: hub 1.0 < House 1.25 < friend 1.5 <
House sells 2.0 < hub sells 2.5. And the most an alt can funnel per unit traded drops from 0.75 to 0.5 × base
(principle 7). It is still one price, as v1.2 decided.

I'm not asking to change it blind. Run the crew-of-four advisor-bot run at both prices, and ship the one that reaches
t ≥ 15% and parity 85–90%. The plan's "seat kits" lever comes second, and its consumer must be a chain that pays at the
chosen price: the Glassworks at 1.75, the Smelter only at 1.5.

## Top 8

1. **F1 The Yard is honest**, with the floor limited to hub-bound spare and the project paying its own. It took 3.5M
   from three friends and about 28 g/min from Mia, and every later screen shows its numbers.
2. **F2 Dead ends explain themselves.** The most pain, and the reason Ana's income sat flat for 32 days.
3. **F4 A project the station can reach**, with the four amendments. It is the only shared buyer in week 1, and most of
   week 3's growth (Foundry Hall ≈ +9%).
4. **F3 Next steps for everyone, with advice sized to the bank.** 4 of 5 players never saw them, and even the advisor
   bot banks 25 days of income.
5. **F5 One return card.** All five players, every return.
6. **F6 Trade with a face, and interdependence**, with the price decided by the bots.
7. **F7 The seat picker.** Mia's STUCK in minute one.
8. **F8 Smart Max.** Once, but 1.53M, and it set off the drain.

F0 comes before all of them and isn't ranked.

## Appendix: the numbers

Run figures are from `metrics.jsonl` (real gold growth), the digest and the content YAML. "Base" is the hub's full price.

### F1 · the Yard

| Constant | Final value | Worked check |
| --- | --- | --- |
| Floor | The seller keeps its hub price × hub-bound units sent to the gate. A player buyer pays the premium (1.75 × base − the seller's hub price, ≥ 0) for what it takes. The project pays base for what it takes. Untaken storage-bound or idle-capacity units: 0 | Lab week: Ana sent 194 energy/min and about 0.3 was taken. Her hub rate was 33.5, so the floor gives 33.5 + 194 × 1.0 ≈ 228; her rate after the Lab was 228–231. Ben 0 + 72 = 72 (pass ≥ 70; after the Lab, 76). Dev 57 + 95 = 152 (pass ≥ 120; after, 146) |
| Lack cap | lack_r ≤ need_r × min(f_out, f_other) − got_r. f_other = min over the other inputs q of min(1, (got_q on own links + station spare_q) ÷ need_q). f_out = 0 when stuck or taker stuck, the run fraction when full, else 1 | Lab: f_other = (6.1 + 0) ÷ 204 = 0.030, so its energy lack ≤ 1,022 × 0.030 − 30.5 ≈ 0.1/min (pass < 1). Ingots: f = (30.5 + 361) ÷ 1,022 = 0.38, so ≤ 72/min, but nobody had spare ingots, so nothing is matched. Mia's Glassworks: stuck (no post), so 0 |
| Feedback | A buyer that took < 80% is next matched ≤ 1.25 × taken + 0.5/min | Lab: ≤ 0.5/min. A real buyer ramps from 0.5 to 200/min in ln(400) ÷ ln(1.25) ≈ 27 rounds, about 27 minutes (rounds stay 1 min apart while terms move) |
| Hub Order | Untaken units are left out. Revisit if any seller's untaken share is > 10% of its gate volume over 24 h | With the cap and feedback, untaken is at most about 25% of taken plus the probe, for one round |

### F3 · the advisor

| Constant | Final value | Worked check |
| --- | --- | --- |
| Effort | max(200, 10 min of income, 2% of the bank). Worth = 1000 × gain ÷ (cost + effort) | Cleo on day 14 (4.8M, 477/min), effort 96k. "+16 for 353 g" scores 0.17; moily to level 10 (+106 for 3,357) scores 1.1; moily to 30 plus post to 10 (+1,760 for 114k) scores 8.4, and ranks first. Ana on day 1 (3k gold, 160/min): effort 1,600, the same as today. Ana on day 8 (1.41M, 78/min): effort 28k. The factory's pipe (+28 for 12 g, × 3 as a fix) scores 3.0 and beats +10 levels on a moily (+45 for 4k, 1.4) |

### F4 · the project

| Constant | Final value | Worked check |
| --- | --- | --- |
| Foundation | 900 energy | Houses bring ≤ 225. Ana alone at about 100 spare from 20:07 does the rest in about 7 min. With Ben (79) and Cleo (58) from 20:09, it stands about 20:12. Mia alone (about 70) stands about 20:21. Pass: by 20:30 |
| Done | ≥ 99.5% or < 1 unit left. The project takes all that's offered, up to what's left | Today 150 ore takes 60 × ln(1.5 × 10⁸) ≈ 19 h, observed twice (18.2 h, 18.1 h). After the fix, about 10 min of one mine |
| Water Tower | Made goods 4 h × the station's spare (floor: 4 h × one level-1 building). Keystone 6 h × one level-1 building. Whole basket ≤ 6 h of summed I(c). Horizon Level 5 | Crew at about 20:15 on day 1 (Ana 162, Ben 79, Cleo 58: summed 299). Energy 4 × 60 × 299 = 71.8k (71.8k g). Iron ore floor 4 × 60 × 12 = 2,880 (8.6k g). Keystone, e.g. copper ore 6 × 60 × 10 = 3,600 (12.6k g). Total 93k against a cap of 108k (today 329k). Allowances are 35.9k on day 1, 39k on day 2 and 64.5k on day 3, so the made goods are in by day 3. Ana's Copper Mine (Level 5 on day 2) fills the keystone in 6 h. Done about day 3 (pass: day 8). Mia: 16.8k + 8.6k + 5.4k = 30.8k, capped to 25.2k (6 h × 70), about 3 days with the Houses' quarter |
| Workshop | 6 h of made goods, keystone 6 h, cap 9 h of summed I(c), horizon Level 7 | Crew on day 3 (summed 553): ingots floor 2,160 (30.2k g), planks floor 2,880 (10k g), keystone glass 1,800 (25.2k g). That is 65k against a cap of 299k, and allowances of 66k a day. Done about day 6–9 (pass: day 14) |
| Foundry Hall | 9 h of made goods, cap 13.5 h of summed I(c), horizon Level 10, opens on day 14 | Crew on day 14 without Ben (summed 1,166): Cleo's spare copper ingots (5/min) give 2,700 (48.6k g), her iron ingots (6.1/min) 3,294 (46k g), plus a keystone of about 25k: 120k against a cap of 944k. The goods take at least 9 h to flow, so it is done in about a day (design: by day 18–19) |
| Keystone | 6 h × one level-1 building, exempt from the allowance, picked again when no active resident can make it | Sand 5,400, stone 7,200, copper ore 3,600, wood 3,600 (today 73–91k each) |
| I(c) | 24-h average of production gold a minute (hub sales + paid Yard + project pay), at least 60 | Ana on day 27: 213 (T(14) = 2,054) |
| Allowance | 2 h of I(c) a day, banked 3 days, shown only when it holds someone back | Ana on day 27: 25.6k a day, 76.7k banked (her card said 739.6k) |
| Delivering | Delivered to this stage or the last within 3 days | Everyone who fed the Foundation counts for the Water Tower |
| Shrink | A counted resident's part comes off after 3 days without a session | Ben: last session day 3 20:00, so from day 6 his 79/299 of the energy comes off |
| Landmark, Wonders | Half the summed I(c) for 6 h; 6 h of I(c) per active player | Crew at the end (1,215): Landmark 219k (by T(L), 1.63M) |
| Bonus shown | 3% of I(c), and the station's total | Ana +6.4 g/min, Cleo +24, station +36 (52k a day) |

### F8 · smart Max

| Constant | Final value | Worked check |
| --- | --- | --- |
| Useful level | The highest level at which own links plus what the gate brings now keep the building at ≥ 90% | Lab, multiplier at level L = 1.1^(L−1) × 2^(milestones). Level 9: 4.29 ingots ≤ 6.1, so 100%. Level 10: 9.43, so 65%. Max +8 for 27.5k. Today +34 cost 1.53M for the same 15 research/min as level 10 |

### What the balance runs must show after the fixes

1. **`typical` and `typical-together`:** level pacing unchanged (Level 10 on day 2, 20 on day 5, 30 on day 25). No price
   changes in F1–F4.
2. **`typical-advisor`:** a bank under 1 day of income at day 28 (today 107M ÷ 2,955 g/min = 25 days), with income no
   lower than today.
3. **A new crew-of-four advisor-bot run**, with neighbour-fed `ChainSteps`, at a player price of 1.75 and of 1.5. It
   must show:
   - t by day 14;
   - each stage's finish day (Water Tower ≤ day 3, Workshop ≤ day 9, Foundry Hall day 14–19);
   - no basket over 3 days of allowances;
   - conservation every sample (sellers = player buyers + project + hub × untaken hub-bound units).
4. **An equal-bot parity run** (solo in a House station against a crew of four) at both prices, with a target of
   85–90%. My prediction at 1.75 with t ≈ 0: solo about 106%.
5. **New gold from NPCs ≤ 6% of all faucets**, measured for the first time now that Houses pay.
6. **A Lab-trap check:** one bot Maxes a Lab beside three sellers, and their rates move by less than 1%.

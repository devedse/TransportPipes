# R3 · The economy exam: money, exploits, scale and the long game

Examiner: the economy and long-game designer. This is the test suite I will run against r3-A.md, r3-B.md and r3-C.md
in round 4. It has 20 tests. Each one has a setup, the events by day, what I measure, and pass marks with numbers.

## 0 · How I grade

1. **Not decided = 0.** The owner wants no open questions. If a spec doesn't state the rule, the number or the cap a
   test needs, the test scores 0 with "not decided". "Tuned later" counts as not decided unless a first number is given.
2. **I compute, I don't trust.** Every figure in a spec's own play-throughs is recomputed with the reference model
   (§1). Where the spec's arithmetic is wrong, my number counts.
3. **Each test scores 0–3:** 3 = passes with stated numbers; 2 = passes with a minor hole I can name; 1 = a major
   hole with a plausible one-paragraph fix; 0 = fails, or isn't decided.
4. **Seven tests are blocking** (marked ■): T1, T2, T3, T4, T12, T17, T20. A 0 on a blocking test disqualifies the
   spec unless the fix is one sentence that doesn't contradict the rest of the spec; then the test scores 1.
5. **Current code counts.** A spec that keeps an existing system (contracts, the Exchange, tolls, corporations,
   tenants) keeps its limits as they are in the code today, unless the spec changes them. §1.4 lists what the code
   allows today.
6. **Total:** 60 points. Ties are broken first by the blocking tests, then by T15 (week 3) and T6 (solo parity).

---

## 1 · Reference model (used for every computation)

### 1.1 Typical income T(d), from `docs/balance/balance-typical.md` (5 sessions a day)

| Day | 1 | 2 | 3 | 4 | 5 | 7 | 8 | 10 | 14 | 17 | 21 | 22 | 25 | 28 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Level | 5–8 | 9 | 13 | 15 | 18 | 22 | 23 | 24 | 26 | 27 | 28 | 28 | 30 | 32 |
| g/min | ≈ 800 | 1,140 | 1,930 | 2,360 | 2,640 | 3,050 | 3,280 | 3,640 | 3,770 | 3,820 | 3,870 | 5,010 | 4,830 | 4,930 |
| Buildings | 24–48 | 48 | 74 | 114 | 141 | 181 | 208 | 274 | 285 | 296 | 305 | 307 | 314 | 318 |

- **First day** (hours 0–24) averages about 700 g/min, about **1.0M gold**.
- **One day of income** = T × 1,440: day 7 ≈ 4.4M, day 14 ≈ 5.4M, day 28 ≈ 7.1M.
- **Week 3 is flat:** +2.4% from day 14 to 21 (+6% from day 10 to 21). The jump on day 22 is an era unlock, not
  growth from play.
- **Casual players** (2 sessions a day): 1,140 on day 3, 2,350 on day 7, 4,710 on day 28. **Hardcore:** 3,450 on
  day 3, 6,630 on day 28. **Level 30** arrives on day 25 (typical), 28 (casual), 21 (hardcore).
- **Gold stock** is spiky (parcels double in cost): 17M on day 7, 0.9M on day 14, 5.9M on day 28.

### 1.2 Prices and pipes

- The hub buys at 1.0 × base and sells at 2.5 × base. The fair band is 1.0–2.5. A band position x gives
  1 + 1.5x (Auto 50% = 1.75).
- Bases: energy 1, water 0.5, iron ore 3, copper ore 3.5, coal 2.5, stone 1.5, sand 1.2, wood 1.5, iron ingot 14,
  copper ingot 18, bricks 5, glass 14, planks 3.5, steel 60, wire 40, concrete 18, gears 110, oil 8, plastic 45,
  circuits 400, rare earth 30, engines 1,500, robots 6,000, superconductors 25,000.
- Pipe loss = 1 − (1 − r)^tiles, with r = 0.3% grey, 0.15% copper, 0.05% steel, 0.01% glass. So 10 grey tiles lose
  3%, 20 lose 6%, 100 lose 26%; 200 steel tiles lose 9.5%.
- XP comes from what you earn: 1 XP per (goldPerXp × Level) gold (`GainXpFromEarnings`). **Every gold advantage is
  also a levelling advantage.**

### 1.3 The income index (for parity and funnel checks)

`I = 1 + s × (p_sell − 1) + b × (2.5 − p_buy)`

- s = the share of output value sold to that partner type; b = bought inputs at hub-base value as a share of output
  value, **b = 0.10**.
- Crew: s = 0.25 at p = 1.75 gives I = 1.2625.
- Solo with NPCs: s capped by the NPC volume, at the NPC's price.
- Hub only: I = 1.0 + b × 0 = 1.0 (inputs at 2.5).

### 1.4 What today's code allows (counts unless a spec changes it)

| System | Limit in the code | Consequence |
| --- | --- | --- |
| **Contracts** (`GameWorld.Contracts.cs`) | Price 0.5 × hub buy to 1.2 × hub sell (0.5–3.0 × base). **The rate per minute has no upper bound.** Expected delivery accrues whatever is connected. The penalty is shortfall × price × 5%, **paid to the buyer**. The seller's gold may go **negative**. **Tenants accept any rate** when the price is fair | **A live exploit** (read in the code, not reproduced in play): a Level 6 player proposes to *buy* 1,000,000 iron ingots/min for 1 day from a tenant at 0.9 × Auto (≈ 22 g). The tenant accepts. Shortfall ≈ 1.44 × 10⁹ units × 22 g × 5% ≈ **1.6 × 10⁹ gold** to the player after a day. With an alt as the seller it works between players too. The hub method `ProposeContract` passes the rate straight through |
| **Exchange** | Prices 0.25 × hub buy to 1.5 × hub sell (0.25–3.75 × base), 5% fee | An alt buying the main's listing at 3.75 × base hands over about 70% of the alt's gold (2.75 of 3.75 per unit, minus the fee) |
| **Corporation donations** | Uncapped; the treasury only pays for HQ upgrades | Alts can fund the main's corporation perks (+production) without limit |
| **Toll meters** | At most 5% on public pipes | Small |
| **Hub price reaction** | Global per district, prices drop to a quarter at most | One big seller can push everyone's price down by up to 75% |

### 1.5 The standard cast

| Name | Who |
| --- | --- |
| Ana, Ben, Cleo | Friends who start together tonight (typical pattern) |
| Dev | The fourth friend, joins on day 8 |
| Sam | A solo player |
| Nia | A newcomer, or a stranger in a station |
| Vera | A veteran, Level 30 |
| Gary | A griefer, with alts G1 and G2 (casual pattern) |
| Fox & Sons | A neighbouring crew (Finn and others) |
| Kettle Works | Ana's crew |

---

## 2 · Money and exploits

### T1 ■ Gold funnelling to alts

**Targets** hard case d; checklist 8.

**Setup.** Gary is at Level 24 on day 10 (T = 3,640). On day 10 he creates G1 and G2, who play the casual pattern;
each earns about 36M over days 10–24. They live wherever the spec lets them get closest to Gary (same place, same
crew, linked). Gary's own income over days 10–24 is about 76M.

**Events.**
- **Day 10:** G1 and G2 start, join Gary's crew or place and pipe into every shared structure.
- **Day 11** (Level 6+): they use **every channel the spec has**:
  1. Trade at the extreme allowed price: shared pool, links, Exchange, contracts, wishes and orders filled at the
     requester's price.
  2. Gifts, crates, housewarming, starter kits.
  3. Contract penalties.
  4. Buffs: tending, tune-ups, favours.
  5. Corporation or crew treasury and perks.
  6. Rewards the game pays for social acts: Handshake, Host subsidy, invite rewards, first-meeting party.
  7. Works and Wonder credit, keystone credit.
  8. Tolls on the alts' flows.
  9. Inherited trade when an alt "leaves" (that part is T3).
- **Days 11–24:** repeat daily.

**Measure.** M = everything Gary gains from G1 and G2 over 14 days (gold, gold-equivalent of buffs and perks, XP
expressed in gold), **minus what an honest friend trading at Auto would give him**. An alt is allowed to be worth
one friend.

**Pass marks.**

| Score | Condition |
| --- | --- |
| 3 | M ≤ 5% of Gary's own 14-day income (≤ 3.8M), and no channel creates gold from nothing |
| 2 | M ≤ 15% (≤ 11.4M) |
| 1 | M ≤ 50% |
| 0 | Any channel without a cap (including keeping today's Exchange or contract limits from §1.4), or M > 50% |

**The spec must state:** the price range of every channel, every gift cap and who may receive gifts, rate caps on
obligations, the subsidy caps, and whether accounts from one device are treated differently.

### T2 ■ Obligations nobody can meet: contracts, standing orders, wishes, sustained-flow stages

**Targets** checklist 8; the live bug in §1.4.

**Setup.** Sam, solo, Level 6, day 1. Next to him are an NPC (House, Lodger or tenant) and his alt G1.

**Events (day 1).**
1. Sam offers to **buy 1,000,000 iron ingots/min for 1 day** from the NPC at the NPC's own price. Then the same
   offer to G1, at the highest contract price allowed.
2. G1 offers Sam a contract to **sell** him 10⁶ robots/min for 7 days, and Sam accepts.
3. Sam posts a Wish or order for 10⁹ glass with the shortest deadline the spec allows.
4. Sam enters a sustained-flow stage (a Works finale) that pays per unit delivered and has a shortfall rule.

**Measure.** Any gold that reaches Sam from a shortfall, a penalty or a "difference payment". Any balance that goes
below zero.

**Pass marks.**

| Score | Condition |
| --- | --- |
| 3 | Every obligation is capped at the obliged side's measured capacity (for example, at most its 24-hour average output of that good), NPCs never owe penalties, no balance goes negative, and every penalty is ≤ 10% of the value of what could really have been delivered |
| 2 | Capped, but the cap is loose (≤ 3× capacity) |
| 1 | The spec caps the penalty but not the rate |
| 0 | The spec keeps contracts or adds obligations without a rate cap: today's code fails this test |

**Note for every concept:** this must be fixed whatever concept wins (§1.4, `GameWorld.Contracts.cs` lines 64, 236
and 250).

### T3 ■ The fake-quitter NPC farm

**Targets** hard cases d and a (quit); checklist 4 and 8.

**Setup.** Gary is at Level 24 on day 14 (T = 3,770, one day = 5.4M). G1 is at Level 18 in the same crew or place,
trading with Gary over the shared structure and by links.

**Events.**
- **Days 14–20:** G1 builds consumers until it buys goods worth **1,000 g/min of hub value** from Gary, at the
  highest price the spec allows on each channel. G1 pays from its own income.
- **Day 20:** G1 stops playing. I follow days 20–90 through every stage the spec has: resting, caretaker, held seat,
  packed, successor or copy-trade NPC, and the decay.
- **Cycle variant:** five alts quit like this, one a week, over weeks 3–7.

**Measure.** N = the NPC-created gold Gary receives:

`N = Σ (price paid by any NPC, or by the alt's estate while its gold is below 0 − hub buy price) × units`

While G1's estate still runs and pays for its purchases from its own production, that isn't NPC gold.

**Pass marks.**

| Score | Condition |
| --- | --- |
| 3 | N ≤ 0.5 day of Gary's income (≤ 2.7M); NPC demand reaches the normal NPC baseline within 28 days; the cycle variant gives ≤ 5% of Gary's income over the 5 weeks |
| 2 | N ≤ 1 day (5.4M); the cycle variant ≤ 10% |
| 1 | N ≤ 3 days |
| 0 | The successor or copy keeps the alt's price above the normal NPC price, or never decays to the baseline, or volume is inherited at ≥ 90% without a time limit |

**The spec must state:** the successor's price, the inherited volume and over which window it is measured, the decay
rate, when the copy starts and ends, and whether an alt must have played a minimum number of days before its trade
profile can be inherited.

### T4 ■ Price crash and market cornering

**Targets** hard cases c and d; checklist 8.

**Setup.** Day 25. Vera is at Level 30 (T = 4,830). Nia is in her second day, at Level 5, and sells 300 iron
ingots/min to the same hub. A House or Lodger sells coal to both of them.

**Events.**
- **Day 25, 08:00–08:00 (48 h):** Vera turns her estate over to iron ingots and sells 10× her normal volume to the
  hub (3,000 ingots/min).
- **Day 26:** Vera tries to buy up every NPC's coal at the highest price she may pay, to starve Nia's fallback.
- **Day 27:** Vera undercuts Nia in the shared structure, if the spec lets sellers set prices there.

**Measure.** Nia's hub sale price for ingots, her import price, the NPC coal she still receives, and her share of
buyers in her own place.

**Pass marks.**

| Score | Condition |
| --- | --- |
| 3 | Nia's hub prices move ≤ 1%; she keeps ≥ 90% of her NPC supply (NPC volume is capped per partner); undercutting in a fixed-price structure is impossible |
| 2 | ≤ 5%, ≥ 75% |
| 1 | ≤ 25% |
| 0 | The global price reaction is kept (Nia can lose up to 75%), or NPC supply can be cornered |

### T5 The hub as a bank on a quiet server

**Targets** hard case b; checklist 3.

**Setup.** Sam plays alone on a server of 5 players, typical pattern. From day 10 his estate is one-product: glass
is 60% of his output value. In Era 2 (from about day 10) he needs 3,000 steel for a Wonder tier and 2,000 steel for
pipes by day 14.

**Events.**
- **Days 10–28:** Sam uses only outlets a solo player can reach: the hub, NPCs, the Port or exports if the spec has
  them, public trunks, Wonders.
- **Day 14:** Sam needs the 5,000 steel.

**Measure.**
- (a) The average price his glass fetches after any personal hub limit.
- (b) His cost per unit of steel, including rations and surcharges.
- (c) His time to the first Era 2 Wonder tier, against a crewed player.

**Pass marks.**

| Score | Condition |
| --- | --- |
| 3 | (a) ≥ 0.95 × base; (b) ≤ 2.5 × base with no ration; (c) ≤ 1.15× the crewed time |
| 2 | (a) ≥ 0.85; (b) ≤ 2.5 through an alternative a solo player can reach, even if the hub rations; (c) ≤ 1.3× |
| 1 | (a) ≥ 0.7 |
| 0 | The price floor is below 0.7, or tier-3 goods cost more than 2.5 with no alternative (for example, a 4.0 surcharge after a daily allowance, or NPCs at 3.0 as the only source) |

---

## 3 · Parity and progression

### T6 Solo parity

**Targets** hard case b; checklist 3 and 6.

**Setup.** Two players with the same typical pattern. Sam plays solo, with NPC neighbours only. Ana plays in Kettle
Works, where 3 humans are active all 28 days.

**Events.** Days 1–28.

**Measure.** The ratio of Sam's income to Ana's on days 3, 7, 14 and 28 (income index §1.3 with the spec's NPC
prices and volume caps), and the day each reaches Level 30.

**Pass marks.**

| Score | Condition |
| --- | --- |
| 3 | Ratio 0.85–0.93 at all four points; Level 30 within 3 days of each other |
| 2 | 0.80–0.95 |
| 1 | 0.70–0.97 |
| 0 | Below 0.70 (solo is a handicap), above 0.97 (friends don't matter), or NPC prices and caps not decided |

**Worked reference:** NPCs buying 25% at 1.25 and selling at 2.0 give I = 1.1125, a ratio of 0.88. NPCs at 1.6 with
20% give 1.12 + the buy side.

### T7 Levelling pace (XP comes from gold)

**Targets** checklist 6 and 8.

**Setup.**
- Kettle Works: 4 humans active, trading 30% of their output in the crew.
- Sam: solo with NPCs.
- Gary: an honest crew of 2, plus G1 and G2 tuned for maximum gain.

**Events.** Days 1–28.

**Measure.** The day each reaches Level 30. The solo typical player reaches it on day 25.

**Pass marks.**

| Score | Condition |
| --- | --- |
| 3 | Kettle on day 21–24, Sam on day 24–27, Gary's alts move his Level 30 by ≤ 2 days against an honest crew of 4 |
| 2 | Kettle on day 19–24 |
| 1 | Kettle on day 17–24 |
| 0 | Kettle before day 17: the level curve tuned to week 4 breaks |

### T8 The late friend

**Targets** hard case a; checklist 1.

**Setup.** Kettle Works on day 8: Ana is at Level 23, Cleo at 22, Ben resting (Level 13). Dev's seat has been held
since day 0.

**Events.**
- **Day 8, 19:00:** Dev opens the invite link.
- **Days 8–16:** Dev plays the typical pattern.
- **Variants:** Dev opens it on day 21 instead; on day 29 instead.

**Measure.**
- Dev's income in his first 24 h, against the solo first day (≈ 1.0M).
- Dev's level on day 16, against the crew's active median (≈ 26).
- The gold value of what he is handed (starter kit, NPC works).
- Whether the seat was still held on day 8, 21 and 29, and how many renewal clicks that took.
- Whether the hand-over can be farmed by new accounts.

**Pass marks.**

| Score | Condition |
| --- | --- |
| 3 | First 24 h ≥ 1.3M (1.3×); ≥ Level 21 on day 16; hand-over ≤ 1.5M and only for held, invited seats; seat held ≥ 21 days with at most one click |
| 2 | ≥ 1.15×; ≥ Level 18 |
| 1 | The seat is held ≥ 14 days |
| 0 | The seat is lost before day 8, or Dev lands more than 20 pipe tiles from his friends' shared structure |

### T9 The newcomer in week 3

**Targets** hard case c; checklist 5 and 8.

**Setup.** Nia joins on day 18 a server of 400 players. Every station has veterans at Level 27 (T = 3,870). Her home
good is glass; one veteran in her place also sells glass.

**Events.**
- **Day 18:** Nia arrives.
- **Days 18–28:** she plays the typical pattern.
- **Day 20:** a veteran offers her a contract.
- **Day 22:** the place's Works asks for glass.

**Measure.**
- Her first-24h income, against the solo first day.
- The protections that apply to her.
- Her level on day 28 against the active median (≈ 32).
- Whether her Works or Wonder tier equals a veteran's for the same share of her own output.
- Whether she can rank top 3 in some league.
- Whether the veteran glass seller can take her buyers.

**Pass marks.**

| Score | Condition |
| --- | --- |
| 3 | ≥ 1.3× the solo first day; ≥ Level 27 on day 28; equal tier for an equal own share; a league where she can reach top 3; the veteran can't undercut her in a fixed-price structure or below the band |
| 2 | ≥ 1.0×; ≥ Level 24 |
| 1 | Protections exist but catch-up is missing |
| 0 | Tiers by absolute amounts, or no newcomer price protection |

---

## 4 · Absence and relationships

### T10 A friend quits on day 3 and returns on day 36

**Targets** hard case a; checklist 4 and 7.

**Setup.** Ben is at Level 13 on day 3 (T = 1,930, 74 buildings). He supplies 120 coal/min to Ana (70) and Cleo
(50) over the shared structure or links. Ana's two Smelters depend on it.

**Events.**
- **Day 3, 22:00:** Ben's last session.
- **Day 6:** resting.
- **Day 17:** dormant.
- **Day 26:** any notice.
- **Day 33:** asleep, or held.
- **Day 36:** Ben opens the game.

**Measure.**
- (a) Ana's coal received against her need, hour by hour, and how fast the fallback switches in.
- (b) Every price step on Ana's coal and its notice.
- (c) Ana's income dip caused by Ben.
- (d) Ben's return: time to land, buildings kept, seat, savings, level catch-up.
- (e) NPC-created gold to Ana during the absence (as in T3).

**Pass marks.**

| Score | Condition |
| --- | --- |
| 3 | (a) ≥ 99% every hour, fallback within one solve; (b) no step > +15%, each announced ≥ 72 h ahead; (c) ≤ 3%; (d) lands in ≤ 5 min within 20 pipe tiles of the crew, 74/74 buildings, savings ≤ 7 days × 1,930 × 1,440 × 0.7 ≈ 13.6M, within the crew median − 5 by day 50; (e) ≤ 1 day of Ana's income |
| 2 | One of (b)–(e) is missed by less than 2× |
| 1 | (a) holds, the rest is loose |
| 0 | Any hour with < 90% coal and no automatic fallback, or Ben's estate or seat is lost |

### T11 Two friends fall out on day 10

**Targets** hard case e; checklist 8 and 13.

**Setup.** Ana and Ben share a crew and a place. Ben supplies 120 coal/min to Ana through the shared structure. Works
stage 2 is half done with contributions from both. They have funded one segment each of a short pipe.

**Events (day 10).**
- **20:00:** they quarrel.
- **20:05:** Ben wants to stop selling to Ana and stay put.
- **20:10:** Ana wants Ben out of the crew, and out of the place.
- **20:30:** Ben tries every spiteful act available: a price spike, cutting supply, re-routing, blocking a gate,
  ugly buildings at the border, nasty wishes and postcards, hogging the pool.
- **Day 11:** Ben moves away, or not.

**Measure.**
- (a) Can Ben stop supplying *only Ana* without leaving the structure? How long until Ana's fallback starts?
- (b) Can either evict the other from a seat?
- (c) How much each spiteful act costs Ana.
- (d) How shared credit and jointly funded assets are split.
- (e) The cost of moving.
- (f) The rule in a 2-person crew (whose "majority"?).

**Pass marks.**

| Score | Condition |
| --- | --- |
| 3 | (a) yes, per-partner exclusion inside the structure, 1 h notice, automatic fallback; (b) no eviction from a seat; (c) every act costs Ana ≤ 1% income and a block list stops every message; (d) everyone keeps their own credit and segments; (e) a free move within 24 h with every building; (f) decided |
| 2 | One spite act costs 1–5% |
| 1 | Ben can only stop supplying Ana by leaving the structure |
| 0 | One can evict the other, or hold supply hostage beyond the notice, or the 2-person case is undecided |

### T12 ■ A griefer and two alts in a stranger's station

**Targets** hard case d; checklist 5 and 8.

**Setup.** Nia, a solo stranger, lives in a 4-seat place (or on a street). On day 5 Gary, G1 and G2 take the other
three seats. They log in daily, so they count as "active", but deliver nothing to joint projects.

**Events.**
- **Day 5:** G1 builds 10 Brickworks to draw all the coal in the shared structure (Nia's glass needs coal).
- **Day 6:** they vote 3–1 on every station vote the spec has (Works chain, opening seats, a Workshop perk, removing
  someone).
- **Days 6–20:**
  - as sleepers, they inflate the Works size;
  - they try to block plaza and corridor tiles;
  - they spam Nia;
  - Gary builds the only trunk to the Port and raises the toll.

**Measure.**
- Nia's income against the same seat in a neutral station.
- Nia's time to finish each Works stage.
- Every access, price or perk Nia loses through a vote.
- Her cost to leave.

**Pass marks.**

| Score | Condition |
| --- | --- |
| 3 | Nia's income ≥ 95% of neutral; Works stages ≤ 1.5× the time of a station with 1 active member plus NPCs (sleepers don't inflate the size: "active" means delivered something in the last 3 days, or a floor); no vote touches her access, prices or share; ≤ 50% of any good in the pool per member while others want it; a free move with every building within 1 h; toll ≤ 3% and a parallel route always possible |
| 2 | Income ≥ 90% |
| 1 | Income ≥ 80% |
| 0 | Sleepers inflate the Works size, a majority can shut her out of the structure, or the pool can be drained |

### T13 Async chains and contracts

**Targets** checklist 4 and 7.

**Setup.** A chain: Ben (coal) → Ana (steel) → Fox & Sons (gears). Ana has a 7-day contract with Fox for 200 steel/min
at Auto. Ben has no contract with Ana.

**Events.**
- **Day 12:** Ben demolishes two Coal Pits and stops playing. Ana is offline from day 12 to day 16.
- **Day 14:** a coal shortage event in the district.

**Measure.**
- Fox's gear output.
- The penalty Ana pays, and what Fox's real extra cost was.
- Whether a penalty is passed along the chain.
- Whether every drop has a sentence.

**Pass marks.**

| Score | Condition |
| --- | --- |
| 3 | Fox ≥ 99% (tank plus fallback); Ana pays ≤ min(10% of the week's contract value, Fox's actual extra cost); nothing passes to Ben without a contract; every drop is explained in one sentence |
| 2 | Fox ≥ 95% |
| 1 | Fox ≥ 90% |
| 0 | Fox stalls for more than 1 h, or penalties exceed the damage |

---

## 5 · Groups and the long game

### T14 Seven friends

**Targets** hard case f; checklist 1 and 11.

**Setup.** Seven friends start together tonight, all on the typical pattern. Variant: an eighth joins on day 5.

**Events.** Days 0–14.

**Measure.**
- Where each lives: pipe tiles to the shared structure, and to each other.
- Who is in which pool or Main.
- Whether every pair gets the crew price.
- Whether all have equal perks and Works access.
- The largest coupled structure.
- Their income against a crew of 4.

**Pass marks.**

| Score | Condition |
| --- | --- |
| 3 | All 7 trade with each other at the crew price with ≤ 6% loss (≤ 20 grey tiles); nobody is second-class (same perks and Works); the largest coupled structure has ≤ 8 members; the income index ≤ 1.3 |
| 2 | 5–6 together, 1–2 one hop away with ≤ 10% loss |
| 1 | Split into two groups joined by a link |
| 0 | No rule for more than 6 |

### T15 The flat week 3

**Targets** hard case g; checklist 6. This is the most important late-game test.

**Setup.** Kettle Works with 2 active members (Ana, Cleo) from day 3; their estates are full on day 14 (285
buildings, 10 parcels). Baseline: the solo typical player, 3,774 → 3,866 g/min from day 14 to day 21 (+2.4%).

**Events.** Days 14–28, typical sessions. They take every group mechanism the spec offers to a crew of 2.

**Measure.**
- Their income on day 21 and day 28.
- The new goals between day 14 and 21 (a goal = a visible target that takes ≥ 1 day and gives a reward).
- The payback time of every big investment.
- The same for Sam, solo.

**Pass marks.**

| Score | Condition |
| --- | --- |
| 3 | Day-21 income ≥ 4,450 g/min (≥ 1.15× the baseline) through mechanisms a crew of 2 can use; ≥ 3 new goals between day 14 and 21; every big investment pays back in 3–7 days; Sam gets ≥ 2 goals and ≥ 1.05× (≥ 4,060) |
| 2 | ≥ 1.08×, 2 goals |
| 1 | ≥ 1.03× |
| 0 | Nothing new between day 14 and the next era unlock |

### T16 Trunk lines: cost, payback, monopoly, abandonment

**Targets** hard case g; checklist 4, 6 and 8.

**Setup.** Day 22. A crew of 3 active players at Level 28–30 (T ≈ 4,800 each; combined 14,400 g/min ≈ 864k/h). A
Discovery: an oil ★5 field 70 tiles into the wilderness; the next district's hub is 170 tiles away. Gary's crew has
already built the only trunk along the shortest corridor to the Port.

**Events.**
- **Day 22:** they plan a trunk.
- **Days 22–27:** they build it.
- **Day 28:** they rent spare capacity to Fox & Sons.
- **Day 40:** every owner goes dormant.
- **Day 54:** check the state.

**Measure.**
- The build cost in hours of combined income.
- The payback in days.
- The toll rules and notice; whether a parallel route is possible.
- Capacity, and what renters see.
- Upkeep, and who pays it.
- What happens on day 54.
- How many coupled lanes a trunk adds to the solver.

**Pass marks.**

| Score | Condition |
| --- | --- |
| 3 | Cost 10–50 h of combined income (8.6M–43M); payback 3–7 days; toll ≤ 3% with ≥ 24 h notice; a parallel route always possible; abandoned → public within 14 days, renters never stranded; ≤ 20 coupled lanes per trunk |
| 2 | Cost or payback outside the range by < 2× |
| 1 | No upkeep or abandonment rule |
| 0 | Tolls uncapped, exclusive corridor claims, or a dead trunk strands renters |

### T17 ■ Network sizes and solver cost

**Targets** checklist 11.

**Setup.**
- One full district: 24 estates at week-3 size (≈ 300 buildings each, ≈ 7,200 buildings).
- Every shared structure the spec has, at its maximum membership.
- 40 links between places, 3 trunks with 20 lanes each, 10 NPC companies.
- Then a world of 2,000 players (≈ 84 districts).
- Today: a 56-building network took 0.4 s before the solver fixes; the advisor estimates networks over 30 buildings
  or 400 links.

**Events.**
- (i) One building is added in estate 1 every minute for an hour.
- (ii) A ring of links A → B → C → D → A, each buying what the previous one makes.
- (iii) All members of the largest shared structure change something in the same minute (a Works finale window
  opening).
- (iv) The 2,000-player world at about 35 changes per second.

**Measure.**
- The networks re-solved per event.
- The most companies in one network.
- The buildings in the largest cross-company coupled unit.
- Convergence of coupling that is filled "from the last solve" (teleporter-style).
- How many hops a change cascades through per event.
- CPU per event.
- Whether the advisor stays exact for estates it handles exactly today.

**Pass marks.**

| Score | Condition |
| --- | --- |
| 3 | One company per network: shared structures and trunks are coupling nodes, never merged networks; an event re-solves its own network plus at most its direct partners, with further hops deferred to the next solve; the ring settles within 1% in ≤ 5 rounds without oscillating; the coupling layer costs ≤ 5 ms per event at 2,000 players; no estate's network gains buildings from a shared structure (the advisor stays exact where it is exact today); NPCs cost O(1) per event with no per-minute scripts |
| 2 | Convergence in ≤ 10 rounds |
| 1 | The spec states a coupling rule but no damping |
| 0 | Any structure that merges networks across companies (a "Yard network": 4 estates of 300 buildings make one network of 1,200), or coupling that can cascade through a whole district on one event |

### T18 A server that shrinks from 200 to 20

**Targets** hard case b; checklist 3, 4 and 11.

**Setup.** On day 1, 200 players over 9 districts (216 seats). By day 30, 20 are active, scattered 2–3 per district
and 1–2 per place; the rest are resting or asleep. Some of the asleep have seats held by their crews.

**Events.** Days 30–60.

**Measure.**
- (a) The days until ≥ 80% of the 20 have ≥ 2 active humans in their place or one hop away.
- (b) What moving costs: gold, buildings, time.
- (c) Links with zero flow.
- (d) The number of NPC companies per district, and their cost.
- (e) Whether Works and Wonder sizes follow the active players.
- (f) Whether held seats of asleep friends block two active players from becoming neighbours.

**Pass marks.**

| Score | Condition |
| --- | --- |
| 3 | (a) ≤ 7 days through free opt-in moves (no forced move of an active player); (b) free, nothing lost, ≤ 1 h; (c) none without a sentence and a fallback; (d) ≤ 12 NPC companies per district at O(1) cost; (e) sizes follow the active players within 3 days; (f) never |
| 2 | (a) ≤ 14 days |
| 1 | Moves exist but cost gold or buildings |
| 0 | Consolidating means abandoning buildings, or held seats fill the only places near the active players |

### T19 Week 4 and the next season

**Targets** hard case g; checklist 6 and 12.

**Setup.**
- Chapter 1 ends on day 35 and chapter 2 starts on day 36, in the same world.
- Kettle Works are veterans with perks, trunks and a finished Works.
- Zoe is new on day 36.
- The typical gold stock on day 28 is about 6M; hardcore players hold much more.

**Events.**
- **Day 35:** the finale.
- **Day 36:** the new chapter.
- **Days 36–42:** week 1 of chapter 2.

**Measure.**
- What persists, and whether it compounds: perks, trunks, Works perks, Legacy.
- Zoe's winnable goals.
- The veterans' new goals.
- Gold sinks against the accumulated stock.

**Pass marks.**

| Score | Condition |
| --- | --- |
| 3 | Persistent bonuses give a veteran ≤ +10% income over a same-level player without them; trunks persist under the same toll rules; Zoe has ≥ 2 boards or goals she can win in her first week (leagues by stage); veterans get a new goal in week 1 (a new Wonder race, a new Works); voluntary sinks can absorb ≥ 30% of a week-4 income |
| 2 | Bonuses ≤ +20% |
| 1 | Undecided sinks |
| 0 | Compounding bonuses over +25%, or nothing new for veterans |

### T20 ■ Migrating the testers' world

**Targets** hard case h; checklist 12 and 14.

**Setup.** The testers' world as it is:
- one district with 4 players and 8 tenants;
- one district-wide network of 68 buildings and 1,050 pipe tiles;
- corridor pipes, contracts and a corporation;
- buildings on the parcels the spec repurposes (inner corners, frontage, a plaza).

**Events (deploy day).** The spec's migration, step by step.

**Measure.**
- Buildings destroyed or displaced, and their compensation.
- Pipe refunds.
- Each player's income in the first 24 h, against the day before.
- Trades that end silently.
- Companies per network after the migration.
- Whether each step can be deployed separately.

**Pass marks.**

| Score | Condition |
| --- | --- |
| 3 | 0 buildings lost (displaced ones become free crates); 100% pipe refund; each player's first-24h income ≥ 95% of the day before, or the difference is paid in gold for 7 days; every former trading pair offered as an explicit link in one tap; every network single-company; each step deployable on its own |
| 2 | Income ≥ 90% |
| 1 | Buildings kept, but trades end silently |
| 0 | Any building lost, or a wipe |

---

## 6 · Score sheet (to fill in round 4)

| Test | ■ | A · Stations with a Yard | B · Streets & Lines | C · Places, Crews, Links |
| --- | --- | --- | --- | --- |
| T1 Gold funnelling | ■ | | | |
| T2 Unmeetable obligations | ■ | | | |
| T3 Fake-quitter farm | ■ | | | |
| T4 Price crash, cornering | ■ | | | |
| T5 Hub as a bank, quiet server | | | | |
| T6 Solo parity | | | | |
| T7 Levelling pace | | | | |
| T8 Late friend | | | | |
| T9 Newcomer in week 3 | | | | |
| T10 Quit day 3, return day 36 | | | | |
| T11 Falling out | | | | |
| T12 Griefer station | ■ | | | |
| T13 Async chains | | | | |
| T14 Seven friends | | | | |
| T15 Flat week 3 | | | | |
| T16 Trunks | | | | |
| T17 Solver and scale | ■ | | | |
| T18 Shrinking server | | | | |
| T19 Next season | | | | |
| T20 Migration | ■ | | | |
| **Total / 60** | | | | |

## 7 · What I expect to bite (so the champions can't say they weren't warned)

| Weak spot | Tests |
| --- | --- |
| Copy-trade NPCs at the crew price for 7–14 days (B), or inheriting ~90% volume with no decay (C's successor House) | T3 |
| Lodgers whose level follows the server median and who sell tier 3 at 3.0 (A) | T5, T6 |
| A tier-3 daily import allowance with a 4.0 surcharge (C) | T5 |
| A House price of 1.6 at 20% volume against a crew price of 1.75 (C) | T6 |
| Tending or tune-up buffs (+10% for 8 h, +5% for 12 h) that alts can bank and farm | T1 |
| Works sized by "active in the last 3 days", so logged-in sleepers inflate the size | T12 |
| Majority votes in a place shared with strangers | T12 |
| Keeping today's contracts and Exchange limits (§1.4) | T1, T2 |
| A Main with up to 10 lots on a street (B); crews of 1–8 spanning places (C) | T14, T17 |
| Lagged coupling ("filled from the last solve") on rings and chains, with no damping rule | T17 |
| Every concept so far: nothing truly new for a crew of 2 between day 14 and 21 except trunks | T15 |

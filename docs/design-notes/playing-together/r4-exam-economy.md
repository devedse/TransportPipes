# R4 · The economy exam, run against A, B and C

Examiner: the economy and long-game designer. Exam: `r3-exam-economy.md` (20 tests, 0–3 each, 60 points, seven
blocking tests marked ■). Specs: `r3-A.md` (Stations with a Yard), `r3-B.md` (Streets & Lines), `r3-C.md`
(Places, Crews, Links).

**How I treated changes since the exam was written:**
- **The contract fix (commit 9621766)** counts as current code. Sellers can promise at most 1.25× what their
  finished buildings make (open and offered contracts included), tenants decline what they can't make, and a
  penalty never takes gold below zero.
- **One more code fact I checked:** a broke buyer stops buying (`GameWorld.Networks.cs`: trades need
  `Gold > 0`). So the "alt's estate buys with gold below zero" clause of T3 can't happen, and T3 counts only gold
  that NPCs create.

## 0 · Scoreboard

| Test | ■ | A | B | C |
| --- | --- | --- | --- | --- |
| T1 Gold funnelling | ■ | 1 r | 1 r | 1 r |
| T2 Obligations nobody can meet | ■ | 3 | 3 | 3 |
| T3 Fake-quitter farm | ■ | 1 r | 1 | 2 |
| T4 Price crash, cornering | ■ | 2 | 2 | 3 |
| T5 Hub as a bank, quiet server | | 3 | 3 | 3 |
| T6 Solo parity | | 3 | 3 | 3 |
| T7 Levelling pace | | 2 | 2 | 2 |
| T8 Late friend | | 2 | 3 | 3 |
| T9 Newcomer in week 3 | | 2 | 2 | 2 |
| T10 Quit on day 3, back on day 36 | | 3 | 2 | 3 |
| T11 Falling out | | 3 | 3 | 3 |
| T12 Griefer station | ■ | 1 r | 1 r | 1 r |
| T13 Async chains | | 3 | 3 | 3 |
| T14 Seven friends | | 2 | 3 | 2 |
| T15 Flat week 3 | | 2 | 2 | 2 |
| T16 Trunks | | 3 | 3 | 3 |
| T17 Solver and scale | ■ | 1 | 2 | 1 |
| T18 Shrinking server | | 2 | 2 | 2 |
| T19 Next season | | 2 | 2 | 3 |
| T20 Migration | ■ | 2 | 2 | 3 |
| **Total / 60** | | **43** | **45** | **48** |

**r** = a blocking 0, rescued to 1 because the fix is one sentence that doesn't contradict the spec. A has three
rescued zeros (T1, T3, T12), B and C two each (T1, T12). Nobody is disqualified. **Winner: C**, ahead of B and then A.

Two of the three rescued failures are **common to all three specs**: gold funnelling through prices off the fair
point (T1), and logged-in "sleepers" inflating shared projects (T12). Whoever wins needs those two fixes.

---

## 1 · Test by test (my numbers recomputed with the reference model)

### T1 ■ Gold funnelling: A 1 r · B 1 r · C 1 r

**The channel that breaks everyone is price, not gifts.** All three keep link presets (Friend 1.375 · Fair 1.75 ·
Premium 2.125). They also keep the Exchange and contracts with today's price ranges, because no spec changes them:
the Exchange runs 0.25–3.75 × base and contracts 0.5–3.0 × base.

How the funnel works: an alt on another device buys Gary's goods above Fair and resells them at the hub (1.0),
spending its gold again and again. Gary's extra gain is `M = alt gold × (p − 1.75) / (p − 1.0)`. G1 and G2 have
72M of gold together over 14 days; Gary's own income over the same days is 76M.

| Highest price an alt can pay | M | Share of Gary's own income |
| --- | --- | --- |
| Exchange 3.75 (today's code) | 52M | **69% → 0** |
| The band top, 2.5 (the "1.5 × base per unit" all three state) | 36M | 47% → 1 |
| Premium preset, 2.125 | 24M | 32% → 1 |

**What each spec already gets right:**
- Same-device accounts are locked at Fair.
- There are no gold gifts; crates are limited to 1 h of the giver's own production, only to newcomers below Level
  10.
- Tending has no stat buff in A and B. **C doesn't decide this** (round 2 had +10% for 8 h, banked).
- Host subsidies are small and go to the newcomer (A: 3 h, C: 2 h of the newcomer's T(L); B has none).

**Not addressed anywhere:** corporation donations, uncapped today. 72M of alt gold buys HQ level 9 (≈ 66M): +9% on
one resource for Gary, forever.

**The smallest change, the same for all three (one sentence each):**
1. Exchange and contract prices use the same presets as links.
2. Friend and Premium apply only between companies that have each played on ≥ 7 distinct days and don't share a
   device; otherwise the price is Fair.
3. Corporation donations are capped at 1 h of the donor's T(L) per day.

With these, M ≈ 0 beyond an honest friend: **3** for A and B, and for C once tending is decided as "no stat buff".

### T2 ■ Obligations nobody can meet: A 3 · B 3 · C 3

With 9621766 every contract is capped near capacity (1.25×), NPCs decline what they can't make, and no balance goes
below zero.
- A adds "only an active seller pays the difference, capped at 10% of the week's value": a transfer, not a faucet.
- No sustained-flow stage has a shortfall rule; all of them pause, never fail.

**Note (not scored here):** B lets a Lodger fill an expired Wish at 2.0 (C has a House fill, A the hub price), and
nothing caps how much. A solo player could source everything at 2.0 through Wishes. Fix: "NPC Wish fills count
inside the NPC volume cap".

### T3 ■ The fake-quitter farm: A 1 r · B 1 · C 2

Setup: G1 buys goods worth 1,000 g/min of hub value from Gary, then quits on day 20. Gary's day-14 income is 5.4M a
day. The normal NPC volume is 25% of Gary's 1,000 = 250 g/min.

**A: a profile at Fair (1.75) on the 14-day median for 14 days, then Lodger terms from 100%, −10% a week.**
- The median is 1,000 g/min, because G1's dormant estate still runs at 100%.
- The 14 days at Fair: (0.75 × 1,000 − 0.25 × 250) × 20,160 min = **13.9M**.
- The decay at 1.25, from 1,000 down to 250: **10.6M**.
- **N = 24.5M ≈ 4.5 days of Gary's income.** The rule "keeps the alt's price above the normal NPC price" also
  applies, so this is a 0.
- "An alt must first run real buildings for two weeks" doesn't stop it: G1 qualifies.
- Fix (one sentence): "the profile trades at Lodger terms from day 1, with 80% of the 14-day median." That gives
  N ≈ 3.9M (0.7 day), a 2.

**B: a successor at Lodger prices, 80% of the median, −10% a week, not inside the Lodger cap.**
- N = **6.9M ≈ 1.3 days → 1.**
- Cycle variant (5 alts, one a week): 18% of Gary's income over 5 weeks.
- Fix: "the successor counts inside the 25% Lodger cap" (as C does). That gives N ≈ 0, a 3.

**C: the successor counts inside the all-Houses 25% cap, so N ≈ 0 and five quitters stack to nothing.** This is
the best rule of the three.
- **But** a crew-held asleep estate stays in place, fully simulated, for 60 + 60 days, with the autopilot
  reinvesting 30% by default.
- It is real production, so there's no NPC gold. But it makes an alt cost one week of play for **four months of a
  growing trading partner at 1.75**: a degenerate strategy the farm test exists to catch. Hence 2.
- Fix:
  - the autopilot only for owners with ≥ 14 distinct days played, and off by default;
  - the second 60-day hold only for such owners.

### T4 ■ Price crash and cornering: A 2 · B 2 · C 3

- All three replace the global price reaction with a personal Hub Order: Vera's dumping moves Nia's price 0% ✓.
- Fixed prices in the Yard, Main and crew make undercutting impossible there ✓.
- The difference is cornering NPC supply:
  - C caps Houses *per company on both sides* ("buy ≤ 25% of its output, sell ≤ 25% of its need") ✓.
  - A and B cap only what Lodgers buy, so Vera might buy up a Lodger's stock.
- Fix for A and B: "a Lodger's sales are capped per buyer at 25% of that buyer's need".

### T5 The hub as a bank on a quiet server: A 3 · B 3 · C 3

- (a) Sam's glass (≈ 0.6 T) sits at the Hub Order line, and grace (A ×4; B and C off at ≤ 5 lit seats) keeps it
  at 1.0 ✓.
- (b) No spec has a tier-3 ration in round 3; B's attack on C quotes C's round-2 file. Steel costs at most 2.5
  (A and C: Lodgers or Houses at 2.0; B: hub 2.5) ✓.
- (c) The first Era Wonder tier (Bronze, any part) is equally fast ✓.
- B is weakest on tier 3 (Lodgers are tier 1–2 only): Sam's steel costs 2.5 against a crew's 1.75.

### T6 Solo parity: A 3 · B 3 · C 3

All three use NPC prices of 1.25 / 2.0 and a 25% volume cap.
- Crew: I = 1 + 0.25 × 0.75 + 0.1 × 0.75 = 1.2625.
- Solo: I = 1.075–1.1125, depending on whether NPCs also cover only 25% of the inputs.
- **Ratio 0.85–0.88** at every point, Level 30 about 2.5 days apart (day ≈ 21 against ≈ 23.6) ✓. At the low edge
  of the band, so A's and C's triggers ("solo below 85% → House volume 35%") are the right safety valve.

The attacks on each other are outdated:
- C's attack on B (Lodgers at 1.75 worth +22%, +79% with alts) uses B's round-2 numbers. In round 3, B's Lodgers
  are at 1.25 with a total cap.
- B's attack on A ("a pool at 1.6, no cap on hogging") is also outdated: A is at 1.75, with a 50% cap.

### T7 Levelling pace: A 2 · B 2 · C 2

- XP follows gold. A crew of 4 trading 30% gives I = 1.30, so Level 30 on about day 20–21 (2 points: day 19–24) ✓.
- Gary's alts move his Level 30 by more than 2 days under the T1 funnel; that is the same fix as T1, and then it
  is a 3 for all three.

### T8 The late friend: A 2 · B 3 · C 3

- **B and C:** the seat is held 28 days with no click, a starter kit of 3–5 buildings at level ≤ 5 plus the NPC's
  links, and Tailwind ×1.57 on day 8 on top of selling at 1.75.
  - Dev's first 24 h come to about 1.45× a solo first day, and he passes Level 21 well before day 16 ✓.
- **A** never sizes its kit. Its Lodgers' works level is the station median − 3 (about Level 19 on day 8), and its
  Lodgers "hand their works to the next human", so Dev, or any account taking a held seat, may get a mid-game
  estate for free.
- Fix for A: "the starter kit is 3–5 buildings at level ≤ 5" (C's rule).

### T9 The newcomer in week 3: A 2 · B 2 · C 2

- All three pass on the first day (1.75 for what the veterans need, a waiting link, Host bonuses), catch-up
  (×2.3–2.5 to the median − 5 by about day 26), equal tiers for an equal own share, and rookie leagues.
- **All three copy the same broken newcomer band:** "across a 10-level gap the lower one sells at ≥ hub price and
  buys at ≤ hub price + 5%".
  - If "hub price" means 1.0, no veteran will ever sell to her at 1.05, so the band blocks her buying.
  - If it means 2.5, it protects nothing.
- Fix for all: "across a 10-level gap, for the lower company's first 14 days, links trade at Fair only".

### T10 A friend quits on day 3 and returns on day 36: A 3 · B 2 · C 3

- **A:** the estate runs at 100% until day 33, then becomes a profile at the old price (the Yard's 1.75) for 14 days.
  Ben is back on day 36, before any price step. Ana gets 100% of her coal with no step, and the NPC gold is about
  1M (0.2 day) ✓.
- **C:** Ana holds the seat on day 17, so the estate stays in place and runs. No step, no NPC gold ✓.
- **B:** on day 33 Old Wim keeps 80% at 2.0, and the missing 20% comes from the hub at 2.5.
  - Ana's effective coal price rises **+20%** (0.8 × 2.0 + 0.2 × 2.5 = 2.1 against 1.75), above the 15% mark.
  - Her income dips only 1.3%.
  - Fix: "a successor keeps 100% of the volume for its first 14 days".
- Whatever the spec, earnings while away never exceed playing: the estate doesn't grow, even with C's +5% a week.

### T11 Two friends fall out on day 10: A 3 · B 3 · C 3

Each spec decides:
- **Exclusion per partner:** A's "not with", B's pair-mute, C's pause with 1 h notice and the tank.
- **No eviction from a seat.**
- **Credit and segments stay personal.**
- **A free move.**
- **The 2-person case:**
  - A: nobody votes;
  - B: invited friends can never be removed;
  - C: removal needs at least 2 votes.

C is the only one with a notice period on the cut. A's "not with" and B's pair-mute cut at once, which costs Ana
about 3% of income until she finds coal. That is a legitimate act, not spite.

### T12 ■ A griefer and two alts in a stranger's station: A 1 r · B 1 r · C 1 r

**Every spec fails the same way:** shared projects are sized by residents "lit in the last 3 days". Gary's alts log
in daily and deliver nothing, so Nia's Works (A, C) or Crew Orders (B) are sized for 4 and take her **about 4× as
long** as in a station with one active member.
- A excludes same-device accounts from goal sizing, which only helps against the careless griefer.
- **B has a worse variant:** a crew of one has open lots by default, so the alts can join *Nia's own crew* on
  probation. They then sit on her Main and inflate her orders, and she can't remove them (a majority of the 4 active
  members is 3).
- Nia's income is hit only mildly in all three: the alts take seats that NPCs would otherwise fill, so about 92% of
  a station with NPCs. That is a 2 on income alone.
- **Fix for all (one sentence):** "goal sizing counts residents who delivered to the project or traded with a
  resident in the last 3 days".
- **And for B:** "the founder alone may remove a probation joiner".

### T13 Async chains and contracts: A 3 · B 3 · C 3

- Ana's mill falls back to NPCs and then the hub automatically, so Fox stays above 99%.
- Penalties stay at or below the real damage: A's difference payment is capped at 10%; B and C use the code's 5%,
  capped by the seller's gold.
- Nothing passes along the chain, and every link has a state sentence.

### T14 Seven friends: A 2 · B 3 · C 2

- **B** puts 4 + 3 lots on one lossless Main (≤ 8): equal and simple ✓.
- **A and C** use a station plus two halts. The crew or Street pipes run 34 and 68 tiles, and **neither spec says
  they are lossless.**
  - Over 68 grey tiles 18.5% is lost (9.7% on copper), so the seventh friend trades at a real handicap.
  - A at least puts the halts in the station's pool and Works; C sends halt members' deliveries to the station's
    Works.
- Fix for A and C: "crew and Street pipes are lossless and drawn by the game (B's Main rule)".

### T15 The flat week 3: A 2 · B 2 · C 2 (the most important late-game test)

Every spec offers a crew of 2 the same engine:
- **The Hub Order nudge:** one-product estates sit at 107–125% of their order.
- **A trunk to a second hub** with a fresh Hub Order and its weekly specialty: +18% for the member with the
  overflow, payback 3–5 days.
- **Works or Crew Order perks** (+5%).
- **A Discovery and Line Race.**

Recomputed, a crew of 2 reaches about +12–16% on day 21: about 1.08–1.15× the baseline of 3,866 g/min, so 2.

**None of the three aims high enough.** Their own triggers are A: +5% from day 14 to 21; B: +8%; C: 20% of gold
spent. My target is +15%.

What distinguishes them:
- **B:** the crew levels give a steady stream of goals, and Lines are open to a crew of one.
- **C:** the only one that decides how often Discoveries happen (weekly, every 4 days if spending is low).
- **A:** decides neither.

**The fix for the winner:**
- Discoveries every 4 days.
- The Works stage 3 perk lands in week 3, not week 4.
- A target of +15% from day 14 to 21 as the trigger.

### T16 Trunks: A 3 · B 3 · C 3

- **Cost and payback are the same in all three:** 2 h of T(L) per segment, 300 steel and 100 concrete. For a crew
  of 3 that is 17 segments ≈ 9.8M, **11.3 h of combined income**, with a payback of 3–5 days ✓.
- **Toll and upkeep:** a game-fixed 2% toll (no gouging) and no upkeep, so absent owners strand nobody. Lines go to
  the hub at 30 days asleep, later than my 14, but with no upkeep the delay costs renters nothing ✓.
- **Lanes:** at most 20 ✓.
- **None of them says that trunks, like links, never own corridor tiles.** One sentence each.

### T17 ■ Solver and scale: A 1 · B 2 · C 1

**What all three get right:**
- One company per network; the shared structures are teleporter-style coupling.
- The advisor stays exact for each estate.
- NPCs are cheap or frozen.

**What none of them deals with: feedback.** Today's teleporter re-solves the receiver whenever the amount sent
changes by more than 10⁻⁶ (`PassTeleported`).
- With bilateral demand and supply (pools, Mains, pair links, Junctions), a change ripples estate to estate
  through every linked company in a district, one 300-building re-solve per hop.
- With pro-rata sharing it can oscillate. This is the 500-round problem of commit 716d68f again, spread over
  estates.

How each spec scores on that:
- **B** is the only one with damping, a "1 min delay" and a stated fallback "allocate on 1-minute averages": 2.
- **A and C** state the coupling with no damping: 1.
- C adds useful bounds: at most 12 links per estate, Houses as supply tables every 15 min, and a 20 ms trigger.

**Fix for all:**
- a coupling term is re-sent at most once per minute per link, pool or lane, and only when it moves more than 1%;
- a pool allocates on 1-minute averages;
- hops are deferred to the next solve.

### T18 A server that shrinks from 200 to 20: A 2 · B 2 · C 2

- All three have the lone-human card, landing in the liveliest districts, free moves with crates, and frozen dark
  districts.
- **A and B** run a Lodger in every empty seat, with build queues: up to 22 per district, more than my 12.
- **C** has the cheapest NPCs (supply tables), but **keeps crew-held asleep estates fully simulated for up to 120
  days**, so the ghost stations cost CPU.
- C also says the 20 "drift together over two weeks", where 3 needs 7 days.
- Fix (for C):
  - held asleep estates beyond 30 days run as profiles (A's approach, but at House terms after a notice);
  - the lone-human card on day 5.

### T19 Week 4 and the next season: A 2 · B 2 · C 3

- **C** decides that the Works **restart with a new project** each chapter and that the finale gives a **chapter
  perk**. That is a fresh goal for veterans, and no bonus compounds ✓.
- **A and B** persist everything, but never say whether the Works or Gate Arch perks (+5%) last forever. Over a
  few chapters they could stack.
- Fix for A and B: "project perks last for the chapter; heirlooms are cosmetic".

### T20 ■ Migration: A 2 · B 2 · C 3

All three:
- keep every building (crates for the corner parcels in A and C; B carves nothing);
- refund corridor pipes;
- make every network single-company.

The difference is the tenants:
- Today they buy at the player's price. **A and B switch them to 1.25 at once**, so a tester who sold 30% of output
  to tenants at about 1.6 loses about 10% of income on day 1.
- **C announces the change with a 7-day notice**, runs the e2e and balance checks before and after, and offers
  station links by card ✓.
- Fix for A and B: "tenant prices change after a 7-day notice, and a 'what changed' screen lists every trade that
  ended" (A already has the screen).

---

## 2 · Ranking and verdict

1. **C · Places, Crews, Links: 48.** C has the soundest money rules:
   - **No fake-quitter farm:** successors count inside a 25% cap shared by all Houses.
   - **Explicit caps on both sides:** NPCs are capped on what they buy and what they sell, per company.
   - **A gentle migration:** tenant prices change only after notice.
   - **Projects reset per chapter**, so bonuses don't compound.
   - **Nothing shared to hog,** because only chosen pairs trade.

   Its weak spots: no damping on the coupling (T17), lossy crew pipes to the halts (T14), crew-held asleep estates
   simulated for up to 120 days, and the sleeper-sized Works shared by all three.
2. **B · Streets & Lines: 45.** B has:
   - the simplest money and the only damping;
   - a lossless Main that fits any group size;
   - Lines open to a crew of one.

   It loses on the successor outside the cap (T3), a +20% price step at handover (T10), open lots that let alts
   into a stranger's own crew (T12), and immediate tenant price changes (T20).
3. **A · Stations with a Yard: 43.** A is the warmest for the lonely player (the Yard with strangers, the profile
   for asleep friends), but has the most money holes:
   - **The profile at Fair for 14 days** is a farm worth 4.5 days of income per fake quit.
   - **The starter kit has no size**, while Lodgers' works sit at the median level − 3.
   - **No damping, and Lodgers in every empty seat.**

With the fixes below, C goes to about 57/60, B to about 55, A to about 54. The gap is real but not large. **C wins
because its holes are one-liners at the edges, while A's and B's sit in the NPC rules at the core of the economy.**

## 3 · Must-take changes so that C passes every blocking test

| Blocking test | Change for C | Taken from |
| --- | --- | --- |
| T1 funnelling | Exchange and contract prices use the link presets; Friend and Premium only between companies each with ≥ 7 distinct days played and different devices, otherwise Fair; corporation donations ≤ 1 h of the donor's T(L) a day; **tending has no stat buff** | B and A (tending), mine (the rest) |
| T3 fake quitter | Autopilot off by default, and only for owners with ≥ 14 distinct days played; the second 60-day hold only for such owners | B ("alts count only after 4 distinct days"), A (two weeks of real buildings) |
| T12 griefer station | Works baskets count residents who delivered to the Works or traded with a resident in the last 3 days; same-device accounts never count | A |
| T17 solver | A coupling term is re-sent at most once per minute and only when it moves more than 1%; pair links share a junction pipe by 1-minute averages; extra hops wait for the next solve; held asleep estates beyond 30 days become profiles, not simulations | B (1-min delay, averages), A (profiles) |
| T2, T4, T20 | Already pass. Add: NPC Wish fills count inside the House cap; a "what changed" screen at migration | A |

**Non-blocking changes worth taking:**
- **T14:** crew pipes to the halts are lossless and game-drawn (B's Main).
- **T9:** the newcomer band becomes "Fair only for the lower company's first 14 days across a 10-level gap".
- **T15:** Discoveries every 4 days, the Works stage 3 perk in week 3, a trigger at +15% from day 14 to 21.
- **T18:** the lone-human card on day 5.

## 4 · Decisions in C I'd overrule

1. **"A held estate stays in place, run by the caretaker with a 30% autopilot, for 60 + 60 days."**
   - **Overrule:** it stays in place and can be tended, but it is simulated for at most 30 days asleep. After that it
     becomes a profile at its own prices for 14 days, then House terms inside the cap. The autopilot only works if
     the owner opted in and has played ≥ 14 distinct days.
   - **Why:**
     - AFK alts: one week of play buys four months of partner trade;
     - the CPU spent on absent players;
     - stations that look full to every newcomer (B's and A's attacks are right here).
2. **"Strangers in a place trade only by a link card the other side accepts (offers wait 72 h)."**
   - **Overrule:** a station neighbour's link starts by itself 24 h after the card unless either side declines,
     still bilateral and with per-pair priority and caps. This is C's own change-it trigger, adopted at once.
   - **Why:** the lonely player's first human trade must not wait for a stranger's timing (A's attack C1). The
     economics are unchanged, because it stays bilateral and nobody can hog.
3. **Hub Order above the line pays 0.5.**
   - **Overrule:** a glide to 0.7 at twice the order, as in A and B.
   - **Why:** at 0.5 the home goods that C's own seats encourage are punished hardest. At 0.7 it still nudges
     specialists to trunks (+18% payback stays) without making the hub stop being a bank.
4. **The newcomer fair-price band (≤ hub price + 5%).**
   - **Overrule:** Fair only (T9).
   - **Why:** as written it either blocks newcomers from buying or protects nothing.
5. **Link presets Friend and Premium for everyone from minute one.**
   - **Overrule:** only between established accounts (T1).
   - **Why:** they are the only funnel left once gifts are gone, about 32% of a main's income with two alts.
6. **One junction pipe whose capacity all pair links share, without a rule.**
   - **Overrule:** shared by need on 1-minute averages, with the per-pair share shown in the manifest.
   - **Why:** "Ben's coal throttled by Ana's energy" (B's attack) is otherwise unexplained, and it is also the
     damping T17 needs.
7. **Kept, despite the attacks:**
   - the fixed crew price;
   - the 2% game-fixed toll with no upkeep;
   - the 25% cap for all Houses together;
   - the 7-day notice for tenants;
   - removal needing at least 2 votes;
   - Works perks per chapter.

   These are the parts of C that make its economy the safest of the three.

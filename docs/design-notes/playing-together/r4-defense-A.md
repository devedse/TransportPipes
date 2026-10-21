# Round 4 · Defending concept A (Stations with a Yard)

I read r3-B and r3-C in full, plus the economy exam's tests (T3 fake quitter, T11 falling out, T12 griefer station,
T17 solver, T20 migration), because they will grade all three of us. I also took into account commit 9621766: a
seller can promise only what its buildings make, and a penalty never takes gold below zero.

Several attacks quote A's round-2 numbers rather than r3-A: "a pool at 1.6", "no cap on hogging", "a seat opens 30
days after sleep". I answer them against r3-A, but I don't hide behind that. **Three of the six attacks land, and I
change the rules for them.**

| Attack | Verdict |
| --- | --- |
| B1 / C1 A stranger in the Yard takes a friend's or a couple's trade | **Rebut** with numbers, plus one rule made sharper (trust order) |
| B2 / C3 The copy-trade Lodger is a gold farm | **Concede.** r3-A still fails the exam's T3 |
| B3 Crews don't fit a table of four; every tester loses a parcel; 74% of stations are lonely | **Rebut** the crew size, **concede** the parcel, **partly concede** the 74% |
| C2 A rule hands a friend's seat to a stranger | **Concede in part:** crew stations are closed to strangers by default, and holds can be extended once |

---

## 1 · The attacks on A, one by one

### B1 and C1: "A stranger in your Yard takes a friend's or a couple's trade"

**The claims.**
- **B:** Vex takes Ben's lapsed seat, builds 12 Brickworks, and gets 75% of Dev's coal, so Cleo's Glassworks falls to
  a quarter.
- **C:** Vex builds 8 Brickworks and gets 70% of Max's coal, so Lea's Smelter gets 30%, and "Lea can't tell the pool
  my partner comes first".

**Rebuttal with numbers.** r3-A §2 serves a seller's surplus in this order: Wish standing orders, then **crew mates
before other residents**, then pro rata. On top of that, no member may take more than 50% of a good's supply while
others want it.
- Dev has 600 coal/min to spare. Cleo (his crew mate) needs 300, and Vex wants 2,000.
- Cleo gets her 300 first, and Vex gets the remaining 300.
- **Cleo is 100% fed.** Vex pays 1.75 for coal Dev would otherwise have sold to the hub at 1.0, so Dev is better off.

C's couple is a crew too, and in A a couple goes to a **halt** (2 seats, both theirs), so Vex isn't even in their
Yard. And if Lea and Max were strangers who simply traded, see the new trust order below.

**The bigger point.** B and C treat "a stranger can buy my surplus" as harm. It is only harm if it takes something a
friend needed, and the order prevents that. What remains is a stranger paying 1.75 for what would otherwise go to the
hub at 1.0. That is the warmth a lonely player needs in minute 12.

**What I sharpen anyway.** Inside the pool, after crew mates, a seller's surplus goes to **other residents in order
of trust** (days of unbroken trade), and pro rata only among equals. A newcomer to the Yard is served last until they
have traded for a day.
- So Lea and Max, even if they are strangers to each other, come before Vex.
- No setting is needed: established relationships are protected by default.

**On B's "no way out but leaving":** r3-A already has a private "not with" switch per company. It stops trade
between two companies only, and nobody else sees it.

### B2 and C3: "The copy-trade Lodger is a gold farm"

**Concede.** r3-A's profile keeps the leaver's volume "at the old price, capped at Fair, for 14 days". Here is the
exam's T3 run.
- **The setup:** G1 buys 1,000 g/min of hub value from Gary in the Yard at 1.75, then quits.
- **The new gold:** for 14 days the profile pays 0.75 above the hub price, so N = 0.75 × 1,000 × 1,440 × 14 ≈
  **15.1M**. That is about **2.8 days of Gary's income**: score 1, or 0 under "price above the normal NPC price".
- With honest friends it also freezes a last-week spike, as B says.

**The fix** (in §3):
- **The price:** the successor uses Lodger prices from the day of sleep.
- **The volume:** it inherits 80% of the 14-day median, and only if the leaver played on at least 7 distinct days of
  those 14. Same-device accounts inherit nothing.
- **The cap:** it sits inside C's company-wide cap (all Lodgers together buy at most 25% of a company's output of a
  good and sell at most 25% of its need). The excess above the cap shrinks to zero over 14 days.

**The re-run:**
- The cap is 250 and the successor starts at 800, so the excess is 550, falling linearly to 0 over 14 days (average
  275).
- N = 0.25 × 275 × 1,440 × 14 ≈ **1.4M ≈ 0.26 day** of Gary's income: score 3.
- Cycle variant (five alts, one a week): about 6.9M against about 190M of income over 5 weeks, **≈ 3.6%**: score 3.

**What honest Ana feels when Ben leaves.**
- Ben sold her 1,200 coal/h of her 1,800.
- After sleep, the successor sells her 960/h at 2.0, falling to 450/h at 2.0 by day 14 of his sleep. The rest comes
  from the Yard, links or the hub at up to 2.5.
- Coal's base price is 2.5 g, so the whole extra cost is about 28 g/min, **under 1% of her income**. She has 3 weeks between the
  notice and the cap.
- The lonely guardian's "cap cliff" fear is real in feeling but tiny in gold. The 7-day notice and the one-line state ("Old Pete ⚙
  keeps 960 coal/h coming, falling to 450 by 14 Nov; Fox sells coal next door") handle the feeling.

### B3: "The table for four doesn't fit people, costs every tester a parcel, and 74% of stations are lonely"

**Group of seven: rebut.** B quotes r2's cap of 6. In r3-A a crew is 1–8: a station plus both adjacent halts, joined
by Street pipes into one pool of 8 (decision 2, case f). Gus is inside the pool and the Works. That is the same 8 as B
and C.

**The parcel: concede.** r3-A carved the whole 8×8 inner-corner parcel from every estate in a station or halt.
- B is right that this is permanent for veterans, even though those parcels unlock only at Levels 35–50.
- **Fix:** the Yard shrinks to a **10×10 square around the corridor crossing**. The two corridors keep running
  straight through it, so link bundles still pass.
- Each estate gives only a **4×4 notch** of its corner parcel: 16 of its 1,024 tiles (1.6%). The other 48 tiles of
  that parcel stay its own and unlock as before.
- The four notches hold the Depot (2×2), the Works (3×3, growing upwards into the Landmark), the notice board and a
  bench.
- The rest of the plaza vanishes. The shared object stays, and it still sits at your corner, on screen.
- To check before building: the Works and Landmark art must stand on their 3×3 footprint (`fit.mjs`, then
  `showcase.mjs` next to the corridors).

**The 74% arithmetic: partly concede.** With 25% of players still active on day 14, placed independently, 74% of
4-seat places have at most one active human. But:
- It applies to B's stretches and C's stations alike. It is a fact about retention, not about Yards.
- A places people *dependently*. "Join neighbours" seats newcomers and returners next to lit seats first. A new
  district opens only at 60% lit. The lone-human card offers a free move after 7 days alone.
- A metric settles it. **Target: by day 14, at least 60% of active players live in a station with at least 2 active
  humans.** If that misses, the card becomes a weekly offer and returners land only in lit stations.
- Where a station really has one human, A's Yard still trades with the Lodgers, and the first newcomer who arrives
  trades with that human in minute 12, with no request to wait for.

### C2: "The game hands a friend's seat to a stranger, and nobody can undo it"

**Partly rebut.** C's Mara uses r2's 30-day hold. In r3-A the hold runs 60 days after sleep:
- she goes asleep on day 35;
- her seat is held until day 95;
- she returns on day 80 into her own seat.

**Concede the principle.** C is right that "it opens by itself" lets a rule make a social decision. Two changes:
1. **Crew stations are closed by default.** When one crew owns 3 or more seats of a station (members plus held
   seats), its free seats are never offered to strangers by "Join neighbours". The crew opens a seat with one tap,
   which is an act of welcome, not of rejection. A station with 2 or fewer crew seats stays open, so gravity still
   brings neighbours to a shrinking crew. (Close to B's rule.)
2. **Any active crew member can extend a hold once by 60 days**, so 120 days in all. Extending is a positive tap ("keep
   Mara's seat"), and not extending needs no tap at all. A lapsed seat in a closed station goes to a Lodger, not to a
   stranger.

### What the exams warned A about

The economy exam lists three weak spots for A:
- **"Lodgers following the server median and selling tier 3 at 3.0":** already fixed in r3-A (the median of the
  station's humans − 3; tier 3 at 2.0).
- **"Works sized by 'active in the last 3 days', so logged-in sleepers inflate it":** **concede**. Fixed below.
- **"Lagged coupling with no damping":** **concede**. Fixed below.

A has no votes at all, so T12's "3–1 votes" find nothing to vote on.

---

## 2 · My attacks on B and C, after reading their full specs

### B · Streets & Lines

| r3-A attack | Still stands? | Updated form |
| --- | --- | --- |
| B1 · Solos alone in streets of bots | **Changed, and worse in a new way** | See below |
| B2 · The Main turns a falling-out into a vote | **Fixed for friends**, open for strangers and Junctions | Folded into B1, plus a new B2 |
| B3 · A members-only pipe at strangers' doors | **Mostly answered** (a crew's stretch is contiguous, so it touches strangers only at its ends) | Downgraded to a smaller point (B3 below) |

**B1 (revised). Solos are seated inside strangers' crews as probationary, removable members.**
- **The rules:** r3-B opens a crew's lots to strangers while the crew has ≤ 2 active humans. A stranger who takes one
  **joins the crew on probation**:
  - away 5 days in a row within their first 14, they are packed to the Quiet Isles;
  - "only open-lot joiners can be removed, by a majority of the active members, weekly".
- **Scenario (exam T12):** Nia takes the open lot on Gary's street. Gary and two alts are the "active" majority. On
  day 9 they remove her from the Main, and her glass drops from 1.75 to the Lodgers' 1.25 and the hub's 1.0. Or, more
  innocently, Nia goes on a 5-day trip in her first week and comes back packed.
- **Why it fails:** absence punished (rule 2), and a vote that only one class of member is exposed to. In A there is
  no vote and no probation: a stranger's absence costs nobody, and a stranger who annoys you meets "not with" or your
  free move.

**B2 (new). A Junction merges two crews' pools by vote, which brings back the owner's complaint.**
- **The rule:** a Junction is "a link between two Mains, opened by a majority of each crew, shared out by need".
- **Scenario:** Kettle (8) and Fox (8) open one, so 16 companies now share their surplus by need. Cleo's coal goes to a
  Fox member she has never heard of. The Main panel's "ledger by pair" now has up to 120 pairs. Cleo voted against it
  and is in it anyway.
- **Why it fails:** this is "everyone trades with everyone, nobody chose anybody" at crew scale, plus a majority
  overruling a member. In A, everything beyond the station is a two-ended link (or a trunk lane) that each company
  opens itself.

**B3 (downgraded). The Main makes distance free and pipes passive.**
- **Scenario:** seven friends get "one lossless Main of about 136 tiles", laid by the game, while every link outside
  the crew pays 0.3% per tile.
- **Why it fails:** inside the crew, logistics is a valve, not a pipe you lay. The one decision pipes are about (how
  far, which tier) is gone exactly where friends play. In A the plaza pipe's tier is your own first upgrade.

### C · Places, Crews, Links

| r3-A attack | Still stands? |
| --- | --- |
| C1 · Strangers share nothing, so the first human contact waits | **Stands** |
| C2 · A crew spanning places splits its project | **Withdrawn.** r3-C attaches each halt to a station in a pinwheel, and halt members deliver to its Works |
| C3 · Held seats keep a full estate running for up to 120 days | **Stands, in a sharper form** |

**C1. Strangers share nothing, so the first human contact waits.**
- **The rule:** stranger links in a place need an accepted card (offers wait 72 h).
- **Scenario:** Mia joins Jo's place at 02:40 and her first partners are Houses. Jo accepts at 23:00 the next day. A
  4-seat place also needs up to 6 pair links, each with its own preset, priority, cap and manifest. All of them share
  one junction pipe, so (B's point) Ben's coal to Cleo is throttled by Ana's energy.
- C's own decision 1 admits the risk: "if fewer than 30% of profitable stranger cards are accepted within 72 h, links
  start automatically". That is A's rule, deferred by a fortnight of lonely first hours.

**C3 (sharper). An absent estate keeps earning and growing for up to four months.**
- **The rule:** a held estate "stays in place, caretaker-run", at 100%, with autopilot reinvest on by default, for 60
  + 60 days.
- **What's not wrong:** by the exam's definition this isn't NPC gold, because the estate pays from its own production.
- **What is wrong:**
  - Ben, away from day 3, earns and levels (XP comes from gold) for 120 days. He comes back above the casual friend
    who kept playing (B's point too).
  - The caretaker spends his gold on upgrades he never chose.
  - The station card shows four residents, two of them ivy-covered, so it looks full to every newcomer.
  - The server simulates full estates that nobody plays.
- **In A** the estate also stays in place, but it **stops earning** (storage-capped production accrues, as on the
  Quiet Isles), it isn't simulated, and its trades run on successor terms.

**C4 (new). Removal by majority inside a friends' crew.**
- **The rule:** "Removal needs a majority of the active members (at least 2 votes)".
- **Scenario:** exam T11, day 10. Ana and Cleo quarrel with Ben and vote him out 2–1. He keeps his seat but sits
  beside his ex-crew as "a neighbour", with his banner colour changed for the whole district to see.
- **Why it fails:** it costs him no gold, and all of the dignity: Hay Day's and Egg, Inc.'s kick drama. In A he can
  leave, or be quietly "not with"-ed, but nobody can vote anybody out.

---

## 3 · A, revised: only the rules that change

| # | Rule | r3-A | **Revised** |
| --- | --- | --- | --- |
| 1 | **Yard order** | Wish orders → crew → pro rata (50% cap) | Wish orders → crew → **other residents by trust (days of unbroken trade)** → pro rata among equals. A Yard newcomer is served last until 1 day of trade. The 50% cap stays |
| 2 | **Successor / profile** | The 14-day median at the old price (≤ 1.75) for 14 days, then Lodger terms −10%/week | **Lodger prices (1.25 buy / 2.0 sell) from the day of sleep.** Inherits 80% of the 14-day median, only if the leaver played ≥ 7 distinct days of those 14; same-device accounts inherit nothing. **All Lodgers together** buy ≤ 25% of a company's output of a good and sell ≤ 25% of its need; a successor's excess above that falls linearly to 0 in 14 days. 7-day notice (day 23) |
| 3 | **Asleep in place** (crew members) | Visible, tendable, trade as a profile | Same, and it **stops earning**: production accrues only up to storage, as on the Quiet Isles, computed at wake-up; no caretaker spending; the card shows "1 seat kept for a friend", not a resident |
| 4 | **Holds** | 60 days after sleep, by themselves | 60 days by themselves; **any active member may extend once by 60** (positive tap). A lapsed seat in a closed station goes to a Lodger |
| 5 | **Closed crew stations** | — | A station where one crew owns ≥ 3 seats (members + holds) is **closed to strangers by default**; the crew opens a seat with one tap. A station with ≤ 2 crew seats is open (gravity) |
| 6 | **Works sizing** | Active = played in the last 3 days | **Active = delivered something to the station** (Yard sale, Works, Wish) in the last 3 days; same-device accounts never count; size floor = 1 active member + Lodgers |
| 7 | **Plaza** | 18×18 from the four inner-corner parcels | **10×10 around the crossing.** Corridors run through; each estate gives a **4×4 notch** (16 tiles, 1.6%) of its corner parcel; Depot 2×2, Works 3×3, board, bench. A halt has two notches and a Depot, and attaches to one station in a pinwheel (from C), delivering to its Works |
| 8 | **Coupling** | "Filled from the last solve" | **1-minute averages and 60% damping** per tick: a ring settles to about 1% in 5 ticks (0.4⁵). An event re-solves its own network; direct partners re-solve next tick; never a cascade within a tick. Budget ≤ 5 ms per event at 2,000 players |
| 9 | **Obligations** | Active sellers pay the difference (≤ 10%) | Commit 9621766 as the base: you promise only what you make, and no penalty goes below zero. **A Wish is capped at 24 h of the poster's own use of that good**; the NPC fill at the deadline is capped at what the poster can pay. The Works finale has no penalty |
| 10 | **Social gifts** | Handshake gift | **Once per company lifetime** (no alt farming). Host subsidy unchanged (≤ 3 h of the newcomer's T(L), stars need 4 distinct days) |
| 11 | **Migration** | 9 steps | Plus: every former trading pair is **offered as a link in one tap**; pipe refunds 100%; **first-24-hour income guarantee** (if a tester earns < 95% of the day before, the difference is paid in gold for 7 days); plazas take only 4×4 notches, and a building found there becomes a free crate |
| 12 | **Metric for gravity** | — | **By day 14, ≥ 60% of active players live with ≥ 2 active humans**; otherwise the lone-human card becomes weekly and returners land only in lit stations |

Everything else in r3-A stands: the six rules, the 1.75 Yard price, the link presets, the Hub Order with
quiet-district grace, the Works, trunks at a fixed 2%, the absence timeline, the return in three screens, and the
build steps. With the notch and the damping work, the build is about **19 days** instead of 18.

---

## 4 · If A lost: I would back C, on conditions

**Why C over B.** C separates **place, people and trade**, and that matches what players feel: a shared address isn't
friendship, and a crew isn't a market.
- B binds place and people into the Main, then fixes the strangers problem with probation and removal votes, and the
  crews problem with Junction votes. Each of those is a social wound that A and C avoid.
- B's strongest card, a migration with nothing carved, loses most of its value once plazas are 4×4 notches.
- C also has the better pieces A already borrowed: seat codes, the pinwheel halts and the company-wide NPC cap.

**What C must take from A:**
1. **Strangers in a place trade automatically.** Pair links at Fair open by themselves when both pipe to the plaza,
   and either side can decline. This is C's own fallback made the default from day one, so the lonely player's first
   human trade happens in minute 12, not 72 h later.
2. **No removal votes.** Leave and pause, nothing more. Nobody presses a button that feels like kicking a friend.
3. **Held estates stop earning,** as on the Quiet Isles, with no autopilot spending and no simulation. Their trades run
   on successor terms.
4. **"Active" means delivered**, for every goal size.
5. **Trust-ordered priority** at the plaza, so established pairs are protected without settings.
6. **Keep the surveyors' ★2 patches in the start area.** C moved them into the Level-5 and Level-7 parcels. The first
   hour must be complete alone, and the role should appear at Levels 5–7, not at minute one. Put the home goods behind
   the first fence instead.
7. **4×4 notches instead of whole corner parcels.**
8. **A's six rules as the acceptance test** for every later feature, especially rule 6: every push towards other
   players switches off where there are none.

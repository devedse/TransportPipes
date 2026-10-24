# Round C · lonely: the exam for players alone and players who stop

I examined the three specs against the five cases the synthesis gave me, using each spec's own numbers:
- **c-economy:** the land and the money;
- **c-friends:** Send on top of the automatic Yard, answer (a);
- **c-wild:** friends give only by Send, strangers trade automatically, answer (b).

Friends and wild sit on economy's land and prices, so where they don't change a rule they inherit economy's result.
Prices are × the hub's base price. Margins are level-1 g/min from c-economy §4–§5 unless I compute them. Upgrade
multipliers come from `progression.yaml` (`outputPerLevel: 1.10`, doubling at the milestones 10 and 25).

## The verdict at a glance

| Case | c-economy | c-friends (a) | c-wild (b) |
| --- | --- | --- | --- |
| 1 · Mia with no free human seat | **pass** (93% alone; porch trade by week 1) | **pass**, one sentence missing | **fail**: on the porch Mia trades, but anonymously |
| 1b · Mia beside Jo | pass | pass | **fail** (same reason) |
| 2 · A solo player in week 3 against the House floor | **fail**: as written the floor stops her NPC-fed chains below the level-10 milestone | inherits | inherits |
| 3 · Ben's 33 days away | **fail** on days 3–30: the station's only sand is frozen, with no House or hub behind it | pass (with economy's fix) | **fail**: if Ben never tapped, his friends never get his sand |
| 4 · An alt farming the stand-in or a Send | **pass** (bounded) | pass, after one contradiction is fixed | pass, after the same fix |
| 5 · A friend who falls out on day 10 | **fail**: the only copper seat can hold Ana's Smelter hostage | inherits; its own end rules pass | inherits; its own rules pass |

Two fixes in economy's §5 (§§2 and 5 below) turn every economy fail into a pass. The one I insist on is the station
backstop (§5): no player may be the only door to a land good.

---

## 1 · Mia: no free human seat, and beside Jo

### Run 1's server: the crew took the only human station

**Economy, alone (case 1 of its §9).** Mia gets Capital N's three-good seat (coal, sand, wood) beside Bertha ⚙
(copper, stone), and a Coal Pit kit worth 100 g/min on day 1 (run 1: 78). Her Glassworks runs on her own sand (+27),
her Smelter and Brickworks on Bertha's copper and stone at 1.4 (+11, +6.5). No chain runs at a loss. Economy's day-14
parity is **93%**. **Pass**, for money.

**Economy, on the porch (its case 2).** Before day 8 her coal undercuts Coal Co ⚙ (1.3 against 1.4) for Ana's and
Cleo's Smelters (+3–6 g/min for her); after day 30 her sand feeds Ana while Ben sleeps. Her trade share goes from 0 to
5–10% in week 1. **Pass.**

**The money passes. The question is whether Mia knows she lives among people.** Run 1 measured that as "alive" 3/5 in
all four sessions. Her warmest line was a reply addressed to her (mia d08-2000 #105).

**Friends (a): pass, with one sentence missing.**
- Under (a), Mia's coal reaches Ana's Smelter automatically, and Ana's panel names her: *"coal · from Mia Studio, via
  the gate · 1.30 g"*, with a Thanks (c-friends §3, "An automatic flow, no Send").
- One Thanks from Ana puts *"Ana thanked you for your coal 💛"* on Mia's return card (§2). That is the addressed moment,
  with no tap from Mia and no invitation from the crew.
- Mia can also Send ▸ to the crew's Water Tower and be named in its ring ("only those who delivered are named").
- **The missing sentence:** c-friends lists "a bid from a stranger shown only after a Handshake" under "what the
  examiners should check" (§10), but its own bid rule (§5) has no such condition.
- **Fix:** add to §5: *"A bid between two companies outside one crew shows only after their gate pipes met (the
  Handshake), and at most once per session."*

**Wild (b): fail.**
- Under (b), Mia's flows with the crew are "the market", and the market is shown as "totals, no names" (c-wild §1 table
  and §5).
- Her automatic deliveries to the crew's Water Tower count for credit but show as *"via the market"* (§1).
- So Mia sells coal to Ana's Smelter for weeks, and neither ever sees the other's name; only Sends get a Thanks. She is
  economically inside the crew and socially invisible, unless *she* taps Send. In run 1 she made no social tap in four
  sessions, and nobody made one toward her. Wild's "first-comer is the host" doesn't help: Mia came after the crew.
- **Fix:** *"Market flows between two humans name both ends, on the receiver's input line and the giver's 'Where it
  goes', and take a Thanks. Totals stay for Houses and the hub."* The fix plan's privacy rule allows it: "a trade pair
  is shown only to its two ends".

**Missing from all three specs:** "Today here", or anything replacing the design's unbuilt lit windows and daily line.
Run 2's pass mark for Mia without Jo ("alive ≥ 4 twice") needs it. I assign it to F5 and F10 (S): three public district
events on the first screen and the return card.

### Mia beside Jo (run 2's scripted neighbour)

Their seats hold different land goods, so the automatic market carries Mia's goods to Jo under every spec. **Economy:**
pass. **Friends:** pass, since Jo's panel names Mia. **Wild:** fail, for the same reason: Jo is scripted and never
chats, so an anonymous market is all Mia has. The same fix applies.

---

## 2 · A solo player in week 3 against the House floor

**The rule** (c-economy §5): Houses sell land goods at 1.4, up to (2 + ⌊level ÷ 10⌋) × one building's need per good per
buyer. Then: *"A building's upgrades count in 'buildings' need' at level 1, so an upgraded Smelter counts as one."*

**That sentence can be built three ways:**

| Reading | What it means | What it does to a solo player |
| --- | --- | --- |
| **R0, units, real need** | Economy's own day-14 example, a Smelter at level 5 (×1.46) fully supplied, works only under this reading | She is held at a cap of 20–40 copper/min |
| **R1, one level-1 need per building** | The literal reading | An upgraded NPC-fed Smelter gets 10 of 47 copper at level 10 and runs at 21%. That is the Lab trap, for every solo player |
| **R2, count buildings** | "counts as one" | No depth cap |

**R0 is economy's intent. It fails at the level-10 milestone, where income doubles.**

| Smelter level | Copper need | Inside the cap? |
| --- | --- | --- |
| 9 | 10 × 1.1⁸ = 21.4/min | Yes, at every player Level |
| 10 | 10 × 1.1⁹ × 2 = 47.2/min | No: the cap is 30 at Levels 10–19 and 40 at 20–29 |

So a solo player's House-fed Smelter can't pass level 9 before player Level 30 (day 25 for the typical bot, later for
a light one). Smart Max (F8) will stop it there, honestly. The crewed player's friend supply has no cap.

**Week 3, an equal estate.** Two House-fed chains (a Smelter and a Brickworks); the crewed player runs them at level
10, as F3 advises once banks hold millions. B is base production; the trade premium is 0.3 × 25% × B crewed and 0.15 ×
25% × B solo (economy's own formula).

| B (base production) | Crewed | Solo, R0 (chains at level 9) | Parity |
| --- | --- | --- | --- |
| 200 g/min | 200 + 22.5 × 4.716 + 15 = **321** | 200 + 17.5 × 2.144 + 7.5 = **245** | **76%** |
| 400 g/min | **536** | **453** | **84%** |

**Under R0 that is 76–84%, below my veto's 85%.**

Under R2 the solo player's chains reach level 10 too: 200 + 17.5 × 4.716 + 7.5 = 290 against 321, **90%**; at B = 400,
**93%**. The friend's edge is then 0.1 a unit, breadth beyond the floor's building count, and the name, which is what
economy says it intends ("puts the friend's weight in the name and the Send").

**Verdict: economy fails case 2 as written.** It is ambiguous, and its intended reading fails at the milestone.

**Fix** (one sentence in §5): *"The floor counts buildings, not units: each building that uses the good counts as one,
at any level. A level-25 Smelter is one building's need."*
- **No faucet:** a House selling at 1.4 destroys gold. Buying at 1.4 to resell at the hub's 1.0 loses money.
- **Pass mark** 3 of §10 (solo against crew per building, 85–95% on days 7–28) stays. Add one profile: a light solo
  bot that follows F3 and F8, so the upgrade path is tested.

**Friends and wild** don't change the floor. Wild says it outright: *"Ana's third Glassworks (beyond the floor) …
loses money, so she doesn't build it"* (§6). Both inherit the fail and the fix.

---

## 3 · Ben's 33 days away under the land deal

**The station** (c-economy §9): Ana holds wood and stone, Ben sand, Cleo copper, Dev coal, so Ben is the only sand. His
last session is day 3, after 3 play days. He rests from day 6, is dormant from day 17, gets the notice on day 26, sleeps
in place on day 33 and returns on day 36.

### What his friends lose while he sleeps

**Economy, days 33–36: pass.**
- Ben has fewer than 10 play days, so the stand-in sells his sand on House terms, at 1.4. The seat is held, so it sells
  uncapped.
- Ana's Glassworks goes from +21.6 to +19.8 g/min: −1.8 g/min, about 1% of her ≈ 200. It never stalls.
- Economy's pass mark 6 says the same: her largest price step is ≤ +8%.

**Economy, days 3–30: fail.**
- His seat isn't held yet: it is his, and he is away.
- His dredger keeps sending 15 sand/min, frozen at day 3's size. Nobody else in Capital NW may sell sand:
  - There is no House in a full station of humans (Coal Co ⚙ leaves on day 8).
  - The hub's backstop applies only to "players whose station has no seat for that good left" (§5). Ana's station has
    one: Ben's.
- So Ana's second Glassworks can buy sand only from the hub at 2.5, where its margin is 27 − 18 × 1.5 = **0**. She won't
  build it.
- From her Level 7 on day 4–6 to day 33, that is about 19.8 g/min × 27 days ≈ **0.77M gold of her growth**, blocked by a
  friend who is simply away.
- v1.2's own rule says "absence costs nobody but the absent player's own growth". Here a resting friend costs Ana hers.
  The porch softens it (Mia's sand), but the rule can't rest on a stranger happening to live next door.

**Fix:** extend the held-seat guard to the whole absence. *"While the owner of a station's only seat for a land good has
been away 3+ days, or the seat is held, the station's House or the hub's market sells that good at 1.4 with no cap."*
Players pay for it, so it is a sink.

### His return

- **Friends (a): pass.** Ben never tapped Send in run 1 and wouldn't need to. His kit sand reaches Ana's Glassworks
  automatically for 30 days, then through Granny Ida ⚙. His card reads *"Your sand ran Ana's Glassworks for 33 days ·
  Granny Ida ⚙ from day 30"* (c-friends §5–§6): someone needed him, with zero taps. The seat is held in place, and the
  landing is the built one-minute way back.
- **Wild (b): fail.**
  - Wild's evening-one Send goes to the Water Tower (§7), because no friend's building uses sand yet.
  - From day 2, Ana's Glassworks needs a *second* Send, offered once as an optional bid on Ben's day-2 card. Ben opened
    the game on day 2 for three minutes and on day 3 for two (ben d02-0800, d03-2000).
  - If he doesn't tap, crew mates can't trade by the market (`YardTradeAllowed` gets "not the same crew", c-wild §2).
    His sand goes to Mia, to Houses or to the hub. Ana's Glassworks buys Mia's sand, or nothing.
  - His return card then has no line that says someone needed him. And his absence costs Ana her Glassworks' supply,
    which on-top never does.
  - Wild names this weakness itself (§12) and offers a switch, but it fires on day 8, after the canonical leaver has
    gone.
  - **Fix:** *"A crew mate who is away 3+ days trades with his crew by the market, named both ways, until he returns.
    His Sends stay Sends."* It costs wild no meaning: nobody is choosing anything in those weeks.

**The stand-in threshold.** Economy raises it from 5 to 10 play days (§6.3). At 1.3 it costs an honest friend about
1% (1.3 against 1.4, −1.8 g/min per Glassworks). I accept 10 and withdraw my round-5 objection.

---

## 4 · An alt farming the stand-in or a Send

**The fake quitter (stand-in): pass.**
An alt on another device plays 10 distinct days with a Glassworks buying the main's sand at 1.3, then quits. From day
30 the stand-in buys at the old price for 7 more days, then glides over 14 days to House terms, inside the 25% NPC cap
(`StandIns.cs`). The excess over the House baseline is (1.3 − 1.15) × 18 g = 2.7 g/min, falling to 0: about 0.07M once,
0.1–0.3 days of a 200–800 g/min main. It needs 10 real play days and 30 days away per round, and more alts don't stack
under one cap.

**A Send: pass.** A main sending to an alt's building moves at most 0.3 × base a unit, only for goods the alt really
uses (F1's lack cap). Same-device trades and storage-bound units are at the hub price (§6.1–6.2). The project pays 1.3
once per basket, and same-device accounts never size it. An alt is worth one honest partner.

**The contradiction that fails friends and wild until it is fixed.** Both say a Send to an asleep receiver *pauses*
(c-friends §1; c-wild §8). But the built stand-in keeps the sleeper's purchases going: `StandInMember` adds his `Buys`
as Lacks (`GameWorld.StandIns.cs:95–98`). Two programmers will build two different games. If the Send pauses, the
friend who sold to the sleeper loses the sale the day he sleeps, and under (b) no market catches it.

**Fix, in both specs:** *"A Send to an asleep receiver passes to his stand-in, at stand-in terms and inside the 25% NPC
cap. The giver's line reads 'Granny Ida ⚙ takes it while Ana is away'."* That keeps the bounded farm above and the
giver's income.

**Porch squatting: a gap in all three.** Probation applies to "a stranger who settles in a station a crew fills (two or
more of its seats)", counted per station (`GameWorld.Return.cs:377–383`, `CrewSeats`, `GameWorld.Crews.cs:463`). A
squatter on the porch is never on probation and blocks its seats to day 37. **Fix:** *"For probation, the porch counts
as part of the crew's big station."*

---

## 5 · A friend who falls out on day 10 (§17e)

**The case.** Ana and Cleo fall out on day 10. Cleo leaves the crew in one tap. Ana sets "not with Cleo", or Blocks
her. Under v1.2 that was harmless: everyone had copper patches.

**Under the land deal, Cleo is the station's only copper.**
- Ana's Smelter (copper from Cleo, coal from Dev) made +11.5 g/min at 1.3.
- After the 1-hour notice and the tank (both specs, §1 and §10), where does Ana get copper?
  - No House in Capital NW sells copper.
  - The hub's backstop doesn't apply: her station still has a copper seat.
  - That leaves the hub's 2.5: 25 − 35 × 1.5 − 10 × 0.3 = **−30.5 g/min**. The Smelter can't run.
- The same holds the other way for Cleo's Brickworks on Ana's stone.

**Neither of them loses much income in a day.** A level-5 Smelter is about 17 g/min, roughly 7% of 250, under my 10%
day bar. But the land deal has turned "not with" into a weapon. The friend you fall out with now holds the one door to
a good, and can close it whenever she likes. My exam's §17e test asked exactly this: "Neither can block the other's …
place's shared thing".

**Verdict:** economy fails case 5. Friends and wild pass on their own rules (Block ends Sends after an hour, no
"stopped" line, leaving never cuts a running supply) and inherit the land's fail.

**Fix:** the same backstop as case 3, with one more condition. *"Whenever no seat of your station will sell you a land
good (its owner is away 3+ days, has Blocked you, has set 'not with' you, or the seat is held), the station's House or
the hub's market sells it to you at 1.4, with no cap."*
- Ana's Smelter then runs at 25 − 35 × 0.4 − 10 × 0.3 = **+8 g/min** instead of +11.5: a 3.5 g/min cost for a
  quarrel, and no hostage.
- On the porch, Bertha ⚙ already sells copper to the big station. The rule makes that true everywhere.

---

## The stranger next door: by default, decided

**Decision:** in small mode (fewer than 50 active companies), "Find me neighbours" seats a stranger on the **porch**,
the small station attached to a crew's closed corner station, by default. Its seats join the crew's Yard and project as
residents. The crew's **invite** (friends' round A §4; wild §10: "her market flows with the crew become Sends, kept by
default") turns a porch neighbour into a crew mate. Any active crew mate may close the porch to *future* strangers;
nobody living there is moved or cut off.

**Why by default, and not by invitation with a 3-day fallback (the psychologist's alternative):**

1. **An act toward a stranger is the act that doesn't happen.** In run 1 the crew's Links tab listed Mia as their only
   possible partner all month, and nobody offered her anything (findings, "together"). An invite repeats that bet.
2. **The fallback arrives after the decision.** Mia's first two sessions were day 1 and day 2, with "alive" already
   3/5 on day 1. A 3-day fallback seats her among people after the sessions in which she decides whether to come back.
3. **Under the land deal, the porch is also a guard for the crew.** The big station holds every land good twice. In run
   1's story, Mia's sand covers Ana while Ben sleeps (case 3), and Bertha's copper covers a quarrel (case 5). The porch
   makes the land deal safer, not only the stranger happier.
4. **The crew loses nothing measurable, and consent holds.** Usual buyers are protected, a new Send phases in at 10% a
   week (both specs), and their own seats stay closed. Mia chose "Find me neighbours" from a card naming whose
   neighbour she'll be; one tap closes the porch to the next stranger.

**What comes with it:**
- probation counts the porch as part of the crew's station (§4);
- market flows between humans are named to both ends (§1, the fix for wild);
- wild's "first-comer is the host" (§11) for the empty server. One change: the text reads *"The next player who comes
  alone will live next to you"*. Crews found their own station, so a promise about "the next player" would be broken
  by the first crew.

---

## The fixes, by spec

| Spec | Fixes (texts above) |
| --- | --- |
| c-economy | The station backstop (cases 3, 5); the floor counts buildings, plus a light solo bot following F3 and F8 (case 2); probation covers the porch (case 4) |
| c-friends | A stranger's bid only after a Handshake (case 1); a Send to a sleeper passes to the stand-in (case 4) |
| c-wild | Human market flows named and thankable (case 1); an away crew mate trades by the market (case 3); the stand-in rule (case 4) |
| All three | "Today here" on the first screen and the return card, or Mia without Jo has only chat |

**Between (a) and (b):** from my seat (a) wins.
- It passes the leaver's case without a tap.
- Under (a), the stranger's case passes with one sentence; under (b) it needs two new rules to get back what (a) gives
  by default.
- Wild's strengths (Send is real, "stopped" never shown, a gift kept by the stand-in) carry into (a): c-friends shows
  Send only where it changes something.

## The one fix I insist on

**The station backstop: no player may be the only door to a land good.** "No copies" is the land deal's whole point,
and I support it. But it creates the one thing my veto rules never had to guard against, a monopoly inside a station.
It hurts exactly my two players:
- the friend who stays, when the only seller rests (case 3: ≈ 0.77M of Ana's growth blocked by a friend who is merely
  away);
- the friend who falls out, when the only seller can close the door (case 5: −30.5 g/min against +8 with the backstop).

One rule fixes both, costs no gold (players pay 1.4, a sink), and leaves the friend's edge intact: 1.3 against 1.4,
with no cap.

# Round 3 exam · The lonely, the leavers and the returners

Examiner: the guardian of the lonely and the leavers. This exam will be run against r3-A.md, r3-B.md and r3-C.md.
It has 6 hard gates and 18 tests. Each test has a setup, the events by day, and pass/fail criteria with numbers.

## How the exam is marked

- **Silence is a fail.** A criterion passes only if the spec has an explicit rule or number that produces the result.
  "In the spirit of" doesn't count. If a spec leaves a case open, that criterion fails. The owner asked for every
  question decided.
- **The examiner does the maths with the spec's own numbers.** Where a spec gives none, these reference values apply:

  | Reference | Value | Source |
  | --- | --- | --- |
  | Typical income T | day 1: 800 g/min · day 3: ≈1,500 · day 7: 3,000 · day 10: ≈3,350 · day 14–21: 3,800–3,870 · day 28: 4,900 | r2-4-economy (balance runs) |
  | Hub | buys at 1.0 × base, sells at 2.5 × base | today's game |
  | Share of output traded with other companies | 30% when the spec gives no figure | r2-3 assumption |
  | Activity stages today | resting from 3 d, dormant from 14 d (caretaker 10%), asleep from 30 d (packed to the Quiet Isles) | `GameWorld.Moving.cs` |

- **Marks:** each test scores *pass* 2, *partial* 1 (a majority of its criteria pass and none of its **[hard]**
  criteria fail), or *fail* 0. Tests marked ★ count double. With 8 ★ tests (4 marks each) and 10 others (2 marks each),
  the maximum is **52 marks**, with the 6 gates scored separately as passed or failed.
- **A failed gate fails the concept**, whatever it scores elsewhere, until the spec is changed. Gates are listed
  first and are re-checked inside the tests (as "Gate G1", etc.).

## Three facts from today's code that every spec must fix

| # | Code today | What a passing spec must say |
| --- | --- | --- |
| **C1** | `Sleep()` calls `EndTradeOf(company, cancelPipes: true)`: every contract with the sleeper ends ("they moved away"), and the partner's pipe leads to an empty estate | On sleep, every link and contract is **handed over** (to a successor NPC or an explicit fallback), never ended without a replacement |
| **C2** | `WakeUp()` lands the returner in `FindFreeEstate()`, the first free seat in district order: no choice, not next to friends, not the old spot | The returner **chooses** where to land, from cards that include their held seat or crew and a lively place |
| **C3** | `CheckQuietDistricts()` skips districts with fewer than 4 non-sleeping players, so the 1–3 loneliest players never get the "move to a busy district" offer | The quiet check counts **active humans (lit seats)**, and works with 1 player |

Each fact is tested in T1, T11 and T5/T6. A spec that doesn't name a fix for it fails those criteria.

---

## Part 1 · The six gates (hard pass/fail)

| Gate | Rule | Fails if the spec contains or allows… |
| --- | --- | --- |
| **G1 · No dead pipe** | Every link or trade relation with zero flow shows a reason in one sentence; no link or contract ends because a partner sleeps without a replacement or a fallback that feeds the buyer automatically | any state where a partner's absence leaves a buyer's input at 0 with no automatic replacement, or a pipe without a status line (C1) |
| **G2 · Solo is a full game** | A player with no human partners earns **≥ 85%** of a crewed player at the same Level (computed with the spec's own prices and volumes), and **no** building, tech, Level, recipe, Wonder tier or cosmetic needs a number of humans | a recipe or reward that needs N different humans; any head-count multiplier; solo parity < 85% |
| **G3 · Nothing needs two people online** | Every cooperative action completes with zero overlapping minutes; no reward depends on a window shorter than 24 h; nothing expires in less than 72 h (offers, invites) or 12 h (deadlines) | live-only rewards, shift or attendance bonuses, an hourly Night Market, a land rush window |
| **G4 · Absence costs nobody else** | Another player's absence lowers nobody's income by more than 10% on any single day, nor by more than 5% over the absence as a whole, and lowers no reward, multiplier or goal outcome; nothing built is ever lost; nobody is moved without their own tap (except into safety on the Quiet Isles after 30+ days) | a diversity multiplier; a crew goal that fails or shrinks its reward when a member is away; adoption of an absent estate; an active player able to push a dormant one out early (spot requests) |
| **G5 · NPCs are honest** | NPCs are labelled in every view, never appear on leaderboards, never deliver more than 25% of any shared goal, never rank above a human, and never take a seat a human wants (they move out within 1 h) | NPC levels that follow the *server* median (on a server of 5 that puts NPCs at the top); fake partners ("echoes") presented as people |
| **G6 · Nudges vanish when nobody is there** | Any mechanic that lowers a player's prices or income to push them towards other players (hub appetite, saturation, tier-3 import caps) is switched off or at least halved in effect where there are **≤ 5 active humans** in the district | a hub cap that halves a solo player's main product on a server of 5 |

---

## Part 2 · The 18 tests

### T1 ★ · The sleeping partner (C1)

**Setup.** Ana (Level 18, day 20) buys 1,200 coal/h from her neighbour Dara over a link or the shared thing, at the
spec's crew or Auto price. She also has a 7-day contract with Dara for 2,000 bricks a day. Dara is not in Ana's crew.
The coal feeds Ana's Smelter, which is 25% of her income.

**Events.** Day 20: Dara's last login. Day 23: resting. Day 34: dormant. Day 47 (−72 h): the notice. Day 50: asleep,
packed. Day 50–64: whatever succeeds her. Day 64: a human takes Dara's seat.

**Pass if:**
1. **[hard]** Every hour from day 20 to day 64, Ana's Smelter gets ≥ 95% of its usual coal, from Dara, a caretaker,
   a successor, a top-up or the fallback, *automatically* (no action by Ana). (G1)
2. **[hard]** Dara's sleep does not end the brick contract or the coal link without a replacement. The spec names
   who takes them over (C1).
3. Ana gets exactly 1 notice, ≥ 72 h before the handover, with a default that needs no action.
4. On the handover day, Ana's cost per unit of coal rises by ≤ 15% (e.g. 1.75 → 2.0 passes, 1.75 → 2.5 fails), and her
   income that day drops by ≤ 5%.
5. On day 64 the newcomer is offered Ana's link in their first session ("a pipe is waiting at your gate").
6. Ana's link panel shows a one-sentence state on every day of the 44 (Live · Resting · Caretaker · Handed over ·
   Capped).

### T2 ★ · The last friend standing (3 of 4 quit)

**Setup.** A crew of 4 in one place. On day 10 Ana sells 30% of her output to the other three and buys three inputs
from them. Ben stops on day 3, Cleo on day 5, Dev on day 7. Ana plays on normally until day 60.

**Events.** Days 6/8/10: resting. Days 17/19/21: dormant. Days 33/35/37: each friend's estate goes asleep, or stays
held in place (whatever the spec says). Day 40: a stranger looks at the free seats.

**Pass if:**
1. **[hard]** On each of days 33, 35 and 37, Ana's income drops ≤ 10% against the day before. (G4)
2. **[hard]** Over days 30–60, Ana's income is ≥ 90% of a "friends stay but freeze at their day-3 size" reference.
3. **[hard] Never worse than having played alone:** from day 33 on, Ana's income is ≥ the income of a solo player at
   the same Level with the spec's default NPC neighbours. Having had friends who left must not leave her poorer than
   never having had them.
4. Ana alone (the only active member) can keep the seats held or open them, and change anything shared. No decision
   waits for a quorum of sleepers or an absent founder.
5. The shared project of the place (Works, crew orders, Main levels) stays finishable by Ana alone: it is sized to the
   active members, and her personal reward is by her own share and not reduced.
6. Ana gets at most 1 prompt (with ≥ 2 choices) to consolidate (move next to active humans, keep the crew identity)
   within 14 days of being alone, and it never repeats more often than every 14 days.
7. Her daily view doesn't list her absent friends with "days since last seen". A cheerful marker (cat, balloon,
   banner) is allowed.

### T3 ★ · Case (a): three friends tonight, Ben quits on day 3 and returns on day 36, Dev joins on day 8

**Setup.** Ana, Ben and Cleo open the game at 21:00 on day 0. They invite Dev, who joins on day 8. Ben stops after
day 3 and opens the game again on day 36.

**Events.** Day 0, 21:00–21:20: founding, seats, first shared pipe. Day 3: Ben's last session. Day 6: Ben resting.
Day 8, 19:00: Dev arrives. Day 17: Ben dormant. Day 33: Ben's 30 days (asleep, or held in place). Day 36: Ben returns.

**Pass if:**
1. By minute 20 on day 0, all three have a working estate with income, **and** a visible shared pipe or flow between
   at least two of them. Nothing in the first 60 minutes needs another player to progress.
2. All three can be neighbours in the same place, chosen together, within the first 5 minutes.
3. **[hard]** Dev's seat is still reserved on day 8 with ≤ 1 tap by anyone (or none). A publicly posted invite link
   can't give the seat to a stranger.
4. Dev's first sale comes ≤ 15 min after arriving. He is within 5 Levels of the crew's median by day 18 (≤ 10 days).
5. **[hard]** From day 3 to day 36, no reward, multiplier or goal of Ana, Cleo or Dev is reduced by anything tied to
   Ben's absence. (G4)
6. Ben gets ≤ 3 messages during his absence, none of them with guilt, countdown or loss wording.
7. **[hard]** On day 36 Ben lands in his own seat, or (if the spec moved him) chooses a seat next to the crew, within
   2 min, with 100% of his buildings and levels. His crew trades are live again within 1 h.
8. Ben's income on day 37 is ≥ 90% of his day-3 income (≈ 1,500 g/min reference), and he gets a catch-up (XP or
   kit) because the crew is ~15 Levels ahead.

### T4 · The fake quitter: an NPC successor as a gold farm

**Setup** (r2-4-economy §1.3). Rex (main, Level 20) and alts R2 and R3 sit in his place or crew. From day 13 to
day 20 the alts set their buy prices to the maximum the spec allows and build cheap consumers, so that they really
use 800 coal/min each. Then they stop on day 20.

**Events.** Days 23–50: resting, dormant, asleep. Days 50–80: the successors.

**Pass if:**
1. **[hard]** From the end of the notice period on, a successor buys at the spec's standard NPC price, **not** the
   leaver's price, and at ≤ 80% of the leaver's 14-day median volume, decaying.
2. **[hard]** Over days 50–80, the gold Rex gets from all of his alts' successors is ≤ 110% of what the same goods
   would have earned at the hub. In other words, the farm adds ≤ 2.5% to his income.
3. A caretaker running a resting or dormant alt (days 23–50) doesn't buy more than the alt bought in its last 14 days,
   at the alt's price minus nothing extra. It doesn't escalate.
4. Doing this twice doesn't double the farm: one successor's volume follows its human partner's needs, not the
   leaver's peak.

### T5 ★ · Case (b1): a solo player on a server of 5 (C3)

**Setup.** The whole server is one district. Tom and Lee are friends who play evenings. Jo is solo and plays nights.
Kai played twice and stopped on day 2. Mia arrives on day 4 at 02:40 and plays only 02:00–03:00 local time. Jo stops
on day 12. Sam arrives on day 24.

**Pass if:**
1. **[hard]** On day 14 Mia earns ≥ 90% of a crewed player at the same Level (spec's numbers). ≥ 85% is required by
   G2 in any case. She also earns ≥ today's solo balance-run income for her day (≈ 3,800 g/min at day 14), so she is
   no worse off than in today's game.
2. **[hard]** No price of hers is cut for lack of partners: her main product's effective hub price stays ≥ 0.9 of the
   full price through day 28. (G6)
3. The default placement on day 4 puts her in a place with at least one other active human (Jo or Tom & Lee), with
   matching hours ranked above mismatched ones.
4. On ≥ 90% of her 03:00 sessions while another human is active, her first screen shows at least one sign that a
   human was there in the last 24 h (lit windows, a changed estate, a chronicle line). It never shows "0 online".
5. Every shared goal she can contribute to (place project stage 1, district Wonder stage 1) finishes by day 28 even if
   she is the only human contributing.
6. **[hard]** NPC neighbours never rank above her on any board, and an NPC's level never exceeds the highest human
   level in its place. (G5)
7. (C3) After Jo stops, a quiet-place or quiet-district rule fires for Mia's 1–2-human place by day 26. It offers one
   free move next to active humans, keeping her crew and progress, and doesn't repeat for 14 days.
8. On day 24, Sam is placed next to Mia (hours match) unless he chooses otherwise.
9. Her leaderboards are meaningful with 5 players. Leagues that need ~25 companies either pause or are replaced by
   personal bests or a per-active-member place cup.

### T6 · Case (b2): the server shrinks from 200 to 20

**Setup.** 9 districts open, 200 active players on day 1. By day 21 only 20 are active, spread at random, about 2 per
district. On day 25 three newcomers arrive, and on day 30 two players come back.

**Pass if:**
1. By day 35, ≥ 75% of the 20 have at least one other active human in their place or in a place they are linked to,
   reached **only** through offers they accepted (no forced moves).
2. **[hard]** No active player is moved without their own tap, and no active player loses a held seat or a shared
   project's progress because of the shrink.
3. The newcomers and returners are placed in a place with ≥ 1 active human (if any has a free seat) in 5 of 5 cases.
4. Districts or places with no active human cost ≤ 5% of a lit one to simulate, e.g. their NPCs are frozen.
5. The server Wonder and Era deadlines are sized to the active players (or have a deadline), so the season still
   advances with 10% of its players.
6. (C3) The quiet check fires in districts with 1–3 active humans.

### T7 ★ · Case (c): a newcomer in week 3 among veterans

**Setup.** A server of 200. On day 22 the median active Level is 24. Noor starts alone at 18:30.

**Pass if:**
1. Her default placement offers either a place with ≥ 1 active human within 10 Levels of her or a Host, or a
   frontier place where everyone started within the last 7 days. One tap.
2. Her first sale comes ≤ 15 min after arriving, and her seat is not empty: it is furnished with starter works or has
   a waiting link.
3. **[hard]** For a trade with a company 10+ Levels above her, she never sells below the hub price or buys above the
   hub price +5%. No contract offers with shortfall clauses reach her, and no offers at all from 10+ Levels above in her
   first 3 days.
4. By day 29 (7 days in) she reaches the Level a day-1 starter has on day 10. By day 40 she is within 5 Levels of her
   place's active median.
5. Delivering the same share of her own output as a Level 24 veteran puts her in the same personal Wonder or Works
   reward tier.
6. In her first week she is in the top half of at least one board she sees by default (league of peers, rookies,
   growth).
7. **[hard]** No veteran gains anything when Noor fails or leaves: no plunder, no exclusive claim, no adoption, no
   windfall.

### T8 ★ · Never online together (the 3 a.m. test)

**Setup.** Ana plays 07:00–07:05 and Ben 23:00–23:05 every day for 14 days. They are in one crew and never overlap.

**Pass if:**
1. **[hard]** Every cooperative feature of the spec completes in this setup: the invite and its acceptance, a link or
   Yard/Main connection, a Wish, a standing order, the place project, its sustained-flow finale, a trunk segment, a
   Wonder delivery. List each feature and check it. (G3)
2. **[hard]** No offer or invite expires in < 72 h, no deadline is < 12 h, and no reward depends on a window < 24 h.
3. A Wish Ana posts at 07:00 is closed by 07:00 the next day in 100% of cases, by Ben, an NPC or the hub, never
   "unfilled". The result shows who filled what.
4. The sustained-flow finale (e.g. 6 h) completes with both only filling tanks in their own 5-minute sessions. It
   pauses, never fails, when the flow drops.
5. Each gets ≤ 1 push a day, in a window they chose.
6. Favours or tending bank for ≥ 3 days, so a missed day loses nothing.

### T9 · Case (e): two friends fall out on day 10

**Setup.** Ana and Ben form a crew of 2 (variant: inside a crew of 4). Each sends 40% of their output to the other
through the shared thing or links. On day 10, after an argument, Ana leaves the crew. Ben tries to punish her for 7
days: he pauses links, raises prices, posts Wishes at her gate, sends gift crates with rude names, and tries to remove
her from the place.

**Pass if:**
1. Leaving takes 1 tap. Ana keeps her estate, seat, buildings, earned rewards and cosmetics, at 100%.
2. **[hard]** Neither can block the other's seat, gates, corridor access or the place's shared thing. If the shared
   thing is for crews only, the spec says what the leaver gets instead (e.g. normal links or the NPC market) within
   1 h.
3. **[hard]** Each one's income drops ≤ 10% in the first 24 h after the split and ≤ 5% after 7 days, through the
   fallbacks and other buyers.
4. With 2 members the spec says who is the "majority". Neither can remove the other from a *seat*, only from a crew.
5. Blocking works: one tap stops all of Ben's messages, Wishes, offers and gifts to Ana.
6. Neither has to move. If one of them wants to, the first move is free and takes ≤ 1 h.
7. Pause and price changes give the partner ≥ 1 h notice, and contract tanks or fallbacks cover it (no overnight
   starvation).

### T10 · Case (f): seven friends

**Setup.** Seven friends start on the same evening. On day 14 four of them stop.

**Pass if:**
1. All 7 get seats in ≤ 2 adjacent places, visible together on one screen, and all trade at the crew price.
2. The 7th member is not second-class: their trade price, loss and visibility differ by ≤ 5% from the first six.
3. **[hard]** After 4 stop, the remaining 3 each lose ≤ 10% income on any single day (G4). They are offered once
   (not forced) to consolidate into one place.
4. The shared solve stays bounded: ≤ 8 participants in any one shared pool.
5. Couples (2) and pairs get a fitting place (a 2-seat halt or similar), not a 4-seat place with 2 NPCs forced on
   them, unless they choose it.

### T11 ★ · The return after a month to a changed world (C2)

**Setup.** Kai, Level 9, stopped on day 6. His crew opened his seat on day 40, Era 2 began on day 20, and his
friends now play in another place. He opens the game on day 49. Variant K2: Ben of T3, whose seat was held.

**Pass if:**
1. **[hard]** Kai chooses where to land from ≥ 3 cards, which include "next to my crew or old partners" and "a lively
   place". He is not placed automatically (C2). K2: the held seat is the default card.
2. He lands ≤ 2 min after choosing, with 100% of his buildings and levels. Buildings that don't fit the terrain go into
   crates (≤ 10% of them), and none are destroyed.
3. The first screen shows ≤ 5 items. Nothing forces him to read a backlog (no "137 unread" wall).
4. **[hard]** No debt, penalty, decay, lost rank, lost title or broken contract fine waits for him.
5. In his first session he can start the Era he missed: a blueprint or kit for its first chain, not only gold.
6. ≥ 1 welcome from a human or an NPC in the first session (never zero).
7. The welcome package can't be farmed: at most once per 30 days away, and the kit only for Eras actually missed.
8. His old links (partners still active) are offered back in 1 tap.

### T12 · Held seats: how long, who holds them, and the whole crew asleep

**Setup.** A crew of 3 holds a 4th seat for a friend. All 3 stop on day 10. On day 50 a newcomer browses the place.
On day 70 one crew member returns. Variant: a held seat whose friend never comes.

**Pass if:**
1. The spec states the hold length and who may renew it (an active member only).
2. **[hard]** Holds can't block a place forever: unheld or expired seats open to others within ≤ 30 days of the last
   active crew member's last login.
3. On day 50 the newcomer can always find a place with a free seat and ≥ 1 active human, if one exists anywhere.
4. The day-70 returner gets a landing card next to their crew's place (even if the crew sleeps) **and** a lively
   place, and the crew identity (name, Level, heirlooms) comes with them.
5. If the friend never comes, the seat's NPC keeps running, with no visible "empty for 28 days" shame, until the hold
   ends.

### T13 · Squatters and blockers

**Setup.** Rex and his two alts: (a) an alt uses a crew's leaked public invite link to take a held seat; (b) the alts
take open seats in two places and go idle; (c) Rex lays pipes along a corridor to wall off two neighbours; (d) Rex
tries to change pipes or buildings in a shared Yard or Main; (e) Rex sends 50 link requests to newcomers.

**Pass if:**
1. (a) A held seat can only be taken with a seat-bound invite or a founder's tap. A public crew link doesn't give it
   away.
2. (b) An idle stranger in an open seat frees it within ≤ 14 days, by a rule that never moves a long-time resident
   or a friend early.
3. **[hard]** (c) No pipe of anyone can block a connection between two other gates. Corridors are shared and nobody
   owns a corridor tile.
4. **[hard]** (d) No player can change, remove or reroute another player's pipes or buildings, in the shared space
   included.
5. (e) Link requests are rate-limited (≤ 5 per hour) and newcomers can't be offered contracts by players 10+ Levels
   higher in their first 3 days.
6. **[hard]** There is no mechanic by which an active player can make a dormant one leave early (no spot requests).
   (G4)

### T14 · Gold funnelling with two alts (case d)

**Setup.** Rex (main, Level 20) and R2 and R3 (new alts, the same device) form a crew. They try every channel: the
crew price, gifts and housewarming crates, Wishes at absurd prices, contracts with shortfall clauses, Host stars and
welcome bonuses, the successor windfall (T4), and the welcome-back package (quit, return, quit).

**Pass if:**
1. **[hard]** The value an alt can move to the main per day is ≤ (the price cap − the hub price) × what the alt really
   produced. Nothing bought at the hub can be resold to the main at a profit.
2. **[hard]** Two alts together add ≤ +15% to the main's income, i.e. an alt is worth exactly one honest partner,
   never more.
3. Game-paid subsidies (welcome bonus, Host rewards) pay nothing until the hosted company reaches the spec's
   threshold on ≥ 4 different days, and links between accounts on the same device run at the default price only.
4. Gifts are capped by the giver's own production (≤ 1 h of one good) and limited per pair per day.
5. Repeated quit and return earns the welcome package at most once per 30 days of absence.

### T15 ★ · The guilt and shame audit

**Setup.** A crew of 4 in which Ben delivers nothing for 3 weeks while the others play. Check every screen the spec
describes.

**Pass if:**
1. **[hard]** No screen shows per-member gaps, "didn't deliver", empty slots for missing members, or a ranking inside
   the crew.
2. **[hard]** No number his friends see gets smaller because of Ben (rewards, multipliers, levels, perk strength,
   project size). (G4)
3. Strangers never see "last online N days". Crew mates see only a soft state ("resting").
4. No personal streak can reset to 0. Counters only grow ("days played this chapter").
5. Ben's own messages: ≤ 3 per absence, no countdown, no loss wording, each one with something positive.
6. Nothing makes his friends want him gone: no reward rises if he leaves or is replaced. Removing him gains nobody
   anything.

### T16 · Case (g): week 4 and the next season

**Setup.** In week 4 a crew owns a trunk or Line to the Wonder site, and 2 of its 3 builders go dormant on day 26.
Mia (a solo player) wants to deliver to the Wonder. The season ends on day 84. Ben is asleep on the Quiet Isles from
day 40 to the season end. A newcomer starts on day 1 of season 2.

**Pass if:**
1. **[hard]** Renters on the trunk keep their lanes for ≥ 7 days after the owners go dormant and permanently after any
   takeover. The toll stays ≤ 3%. The trunk never just stops.
2. Mia can reach the Wonder by a public or rentable pipe at ≤ 3% toll, or by an equivalent route the spec names, and
   her reward tier is by her own share.
3. Late-game group projects have a solo-sized path: a solo player reaches the same personal tier by the same share of
   output.
4. **[hard]** At the season change, sleeping players' estates, crew memberships and heirlooms carry over at 100%. Ben,
   returning in season 2, can land next to his crew in ≤ 2 min.
5. Season 2 starts without a land rush or any window that needs presence. The newcomer of season 2 is placed among
   peers or in a frontier.
6. Veterans have something to work towards together in week 4 that is not "wait for absent friends".

### T17 · Case (h): migrating the testers' world

**Setup.** The tester world as it is: districts of 24 estates, corridor pipes that join whole districts into one
network, 8 NPC tenants per district, buildings on some inner-corner parcels, several testers resting, dormant or
asleep, and some testers alone in their district.

**Pass if:**
1. **[hard]** No tester loses a building, level, gold, stored good or reward. A building on land that changes use
   stays or becomes a crate at full value: 100%.
2. Every trade between two human players that had flow in the last 7 days becomes a link or crew relation with the
   same partner. On day 1 after the migration, each player's top 3 trade relations run at ≥ 90% of their previous
   flow.
3. **[hard]** Resting, dormant and asleep testers are migrated without logging in, and their partners see no dead
   pipe (the T1 rules apply from day 1).
4. Nobody active is moved to another district without a tap.
5. Testers alone in their district get the consolidation offer within 7 days.
6. The migration ships in steps, and each step leaves the game playable (CI rule in CLAUDE.md).

### T18 · NPC strength and honesty

**Setup.** A server of 5, so NPCs fill 19 of 24 seats. Run 28 days, then repeat with a Level 3 player and a Level 30
player each next to the same NPC.

**Pass if:**
1. **[hard]** NPCs carry a label (cog or similar) in every view: map, link panel, boards, letters. (G5)
2. **[hard]** NPCs are never on a leaderboard and deliver ≤ 25% of any shared goal. (G5)
3. NPC trade volume scales with its human partner, not the server: next to the Level 3 player ≤ 30% of their output,
   next to the Level 30 player still ≥ 10% of theirs.
4. NPC level ≤ the highest human level in its place.
5. An NPC moves out for an arriving human within ≤ 1 h and hands over its starter works and links.
6. NPC letters: ≤ 1 per place per day in total (a digest).
7. With NPC partners only, a solo player's income stays within G2 (≥ 85% of crewed).

---

## Part 3 · Scorecard (to fill in next round)

| | A · Stations with a Yard | B · Streets & Lines | C · Places, Crews, Links |
| --- | --- | --- | --- |
| G1 No dead pipe | | | |
| G2 Solo is a full game | | | |
| G3 Nothing needs two online | | | |
| G4 Absence costs nobody | | | |
| G5 NPCs are honest | | | |
| G6 Nudges vanish | | | |
| T1 ★ Sleeping partner (C1) | | | |
| T2 ★ Last friend standing | | | |
| T3 ★ Case (a) | | | |
| T4 Fake quitter | | | |
| T5 ★ Solo on a server of 5 (C3) | | | |
| T6 200 → 20 | | | |
| T7 ★ Week-3 newcomer | | | |
| T8 ★ Never online together | | | |
| T9 Friends fall out | | | |
| T10 Seven friends | | | |
| T11 ★ Return after a month (C2) | | | |
| T12 Held seats | | | |
| T13 Squatters and blockers | | | |
| T14 Alts and funnelling | | | |
| T15 ★ Guilt and shame audit | | | |
| T16 Week 4 and next season | | | |
| T17 Migration | | | |
| T18 NPC strength and honesty | | | |
| **Gates passed (of 6)** | | | |
| **Marks (of 52)** | | | |

For each failed criterion the examiner writes one line: the criterion, the spec's section, and the smallest change
that would make it pass.

## What I expect to bite (from reading round 2)

These are leads to check, not verdicts.

- **Held-in-place sleepers** (C keeps a crew member's estate in place, run by the caretaker, for up to 60 days):
  T1 and T2 pass more easily, but T12.2 and T6 need a rule for when the whole crew sleeps.
- **NPC levels that follow the server median** (A's Lodgers: "median active level − 2"): G5 and T5.6 on a server of 5.
- **Copy-trade successors at the leaver's price** (B: "copies the last 7 days … fades over 14 days"): T4. The economy
  file's fix is House prices, 80% of the 14-day median, −10% a week.
- **Hub softening with a floor of 0.7, or appetite at 0.5** (C, the economy layer): G6 and T5.2 unless quiet places
  are exempt.
- **Crew removal "by a majority of active members"** with 2 members: T9.4.
- **Savings jar plus a caretaker that reinvests the owner's gold** (C, A): T4.3 and T15.6. The jar is unnecessary
  because offline income already runs.
- **Whether each spec fixes C1, C2 and C3 by name.**

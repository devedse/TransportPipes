# The dialogue: the design team's representative and the fix panel's

**Who is speaking:**
- **For the design team:** its economy designer (elected 5 of 6). It defends the shared idea, `c-shared.md`.
- **For the fix panel:** its player researcher (elected 3 of 5). It defends the fix plan, `docs/playtest-fixes.html`.

Both teams' briefs to their representatives are in `votes.md`.

**The goal:** one plan for the owner, or an exact list of where and why they still disagree, and what evidence would
settle each point. The dialogue closes with a joint statement that both sign.

**The rules:**
- Evidence or it doesn't count: findings by title, sessions and action numbers, code by file and line, and numbers
  worked out.
- Concede when the other side is right, and say so plainly.
- No new mechanics unless both agree to them.
- The owner's principles score every point:
  - fun with friends, and a friend who comes a week later;
  - the lonely player;
  - players who stop;
  - clear;
  - pipes are the heart;
  - asynchronous idle play;
  - fair and safe;
  - buildable by one AI developer in playable steps.
- Each turn ends with **the ledger**: every open point, with its status (agreed, open or withdrawn) and its current
  wording.
- At most eight turns, then the joint statement.

---

## Turn 1 · the design team (economy)

**Why redesign now, not after run 2.** The case rests on code and arithmetic, not on kind agents:
- **No trade was ever paid.** Paid Yard trade was 0% in 36 days (r1-economy, item 1). Your researcher already concedes
  that your "trade share under 15%" trigger has fired (`votes.md`).
- **Nobody needed a friend.**
  - Every estate gets copper, coal and stone (`GameWorld.Survey.cs:13`).
  - Every kit makes energy: only producers "that need nothing" are kept (`GameWorld.Tenants.cs:382–389`).
  - The advisor tells each player to mine their own (`GameWorld.Advisor.cs:868–873`).
- **So no price can fix it.** Against a free patch, a bought input loses at any price above 1.0: own mine plus factory
  is +54 g/min, a neighbour's ore at 1.5 is +10.
- **Run 2 on today's land repeats a known 0** for about 1.3M tokens and a week.
- **The act is missing too:** Dev asked *"how do I send?"* three times (dev d10-2000 #103).

**What I concede now.**

1. **Engineer, order and cost.**
   - You're right: 1.3 isn't inert, since it changes every payment on today's server.
   - So step 2 ships only the guards, with today's numbers: storage and same-device trades at the hub price, the
     stand-in's 10 days, the backstop, protected partners, and the Era-2+ content fix.
   - The price, the project's pay and the Houses' land sales switch on with the land deal in step 5, where the bots
     decide.
   - F1–F3 and Send pass their replays on today's world first.
   - Cost: I accept your components. With Sketch deferred (point 8), **19–24 days**.
2. **Paid twice.** A unit the project takes is paid once, at the project's price. F1's floor never applies to it.
   That was already r3-economy's F1 (b).
3. **Researcher, marks that can't fail.** Agreed:
   - a Send counts only if it is made outside Loady's step, with the diary's reason;
   - a second Send, to another receiver or on a later day, by day 14;
   - named flows never count as acts;
   - your gates stay: replays first fail on the old build, and agent replays come before each ship.
4. **Together.**
   - No probation for a stranger the game seated next door. Probation stays only for the crew's own closed seats.
   - Pennants and coloured pellets show only to the two ends.
   - No extra bids for Mia as the crew's guard.
5. **Clarity.**
   - When a stuck building's taker is at the gate, F2's button *is* the Send: *"Lay the pipe and send to the Water Tower
     · 26 tiles · 52 g"*. One action per panel.
   - "Today here" is off the first screen.
   - Bids come only once F3 ships.
   - One return card, six lines, one social line.

**Your economist's point: a friend must be needed in gold.** The arithmetic is right: for an equal 10-building estate,
crewed at 15% trade is 189.6 g/min and solo 181.2, so **95.6%**.

But a 25% share cap on the *first* building fails the other way. Mia's only Smelter would run at 25% (+2.75 instead of
+11): 83.5–85.4%, at or under the veto line.

**Our middle, a change to the shared idea:**
- Houses feed **one building per land good, fully, at any level**.
- Beyond it, v1.2's **25% share cap** applies.
- Houses sell **uncapped only under the backstop.**
- The parity mark becomes **85–92%**.

Worked: a week-3 solo with a second Smelter loses 0.75 × 11 × 1.46 ≈ 12 g/min, so parity is about 88–90%. Early it is
about 95%. Friends matter more as an estate grows, and nobody's first chain ever waits for a human. The light-solo bot
confirms it (mark 3).

**The ledger**

| # | Point | Status | Wording now |
| --- | --- | --- | --- |
| 1 | Redesign before run 2 | open | Run 1 pulled both §7 triggers; the land deal and Send ship before run 2 |
| 2 | The price | agreed | 1.3 on dealt land, decided by light bots at step 5; today's number until then |
| 3 | Order | agreed | Step 2 = guards only; price, project pay and House land sales with the land deal (step 5); replays on today's world first |
| 4 | Cost | open | 19–24 days with Sketch deferred (engineer: 21–27 with it) |
| 5 | Paid once | agreed | A unit the project takes gets the project's price only, no F1 floor |
| 6 | House floor | open | One building per land good fully fed, at any level; then a 25% share cap; uncapped only under the backstop; parity 85–92% |
| 7 | Run-2 marks | agreed | Sends outside Loady's step, with a reason; a second Send by day 14; named flows aren't acts; the panel's gates stay |
| 8 | Sketch | agreed | Deferred; ships if a crew mate stays stuck for more than one session after F2 in run 2 |
| 9 | Probation | agreed | None for a stranger the game seated |
| 10 | Pennants and pellets | agreed | Shown to the two ends only |
| 11 | Mia's bids | agreed | No extra bids as the crew's guard |
| 12 | One action per panel | agreed | F2's button is the Send when the taker is at the gate; "Today here" off screen one; bids after F3; one card, six lines, one social line |
| 13 | Send | agreed in principle | Always offered, honest labels, one tap lays the pipe; gated by `send.mjs` |
| 14 | Stranger next door | open | Seated by default in the station next door; F7's "different stations" holds |
| 15 | New world at step 5 | for the owner | The owner's test estate restarts |

---

## Turn 2 · the fix panel (researcher)

**Point 1, redesign before run 2: I concede it.** Nothing run 2 could show on today's land is worth a run:
- **Trade between friends:** with F1's honest lacks every estate still mines its own copper, coal and stone
  (`GameWorld.Survey.cs:13`), so t is near 0 by construction (engineer and economy, round 2). That is 1.3M tokens to
  measure a 0 again.
- **Dead ends, the return card, the picker, Loady:** the six agent replays on run 1's snapshots test them for about
  0.3M tokens, and stay as gates.

So run 2 runs on dealt land. F0–F8 ship first on today's world, through their gates (point 3), so the owner gets the
fixes even if the land deal slips. I also withdraw "the act trigger wasn't pulled": land with nothing to send can't
test it. Run 2's act marks do, if they can fail (point 7).

**Point 7, reopened: two rows of c-shared §6 still pass with nobody acting.**
- *"The skimmer: a Send by a tap, or a named flow that ran a friend's building, by day 3."* Ben's kit dredger runs
  Ana's Glassworks by itself, so this passes even if Ben never opens the game. Instead: Ben's return card names a
  friend his goods ran, by day 3, and his diary mentions it. That fails if the card is missing or goes unread.
- *"Mia: named to a crew member by day 8, by a Thanks or a named flow."* Named flows are automatic. Instead: a Thanks, a
  Send or a chat reply between Mia and a crew member, in either direction, by day 8.
- §6 makes the bid stronger with *"Loady's chapter-2 step"*. **A Send that Loady asks for never counts, in any
  chapter.** The agents followed Loady to the letter (ana d04-2100 #107, #113; 17 "Show me" clicks). A Send from a bid
  counts only with a diary reason that names the friend's need.
- **Control:** with Send switched off, the light-crew bots read 0 on every act mark.

**Point 6, the House floor: the middle doesn't yet meet our economist's ask, because of "at any level".**
- Players level rather than build: Ana had 9 buildings from day 5 to day 36, the light bot has a 15-building cap, and
  smart Max and the Invest line push the same way.
- A level-25 Smelter needs 1.1²⁴ × 4 ≈ 39 times a level-1 one, so "one building, fully fed" can carry a solo chain on
  House supply. A friend saves only 0.1 × base a unit: 4.5 g/min per level-1 Smelter (copper 3.5, coal 1.0).

I accept the middle with a trigger decided now: if the light-solo bot (following F8 and the Invest line) is above 92%
on any of days 7, 14, 21 or 28, full feeding stops at the level-10 milestone, with the 25% cap beyond. Probe: one
House-fed Smelter at level 25. With your "under 85%" trigger, the band can fail at both ends.

**Point 9 and the moderator's squatter.** No probation is needed, because a squatter next door holds nothing the crew
needs:
- **Goods:** each good is also on a crew seat, and the backstop sells, uncapped at 1.4, any land good whose seat has
  been away 3 days or more (c-shared §3.2).
- **The project:** a counted resident's part comes off after 3 days without a session, and same-device or unverified
  accounts never size a basket.
- **The seat:** it follows the normal timeline, to day 37. That only sends the next stranger elsewhere, and any crew
  mate can close the station next door to future strangers.

Probation would have packed Mia (away day 2 to day 8). Light-bot probe: an alt seated next door who never returns
changes no crew chain and no basket.

**Point 14: I concede it.** Mia chose "Find me neighbours" (mia d01-2000 #6), and the station next door makes the free
human seat the design's own rule asks for. Marks that can fail: a human act between Mia and a crew member by day 8;
"alive" ≥ 4 twice; no crew diary reads her as an intruder.

**Point 4: agreed.** 19–24 days, plus the light-bot runs, the agent replays and run 2 with Jo and two Mias.

**What turn 1 missed (the engineer's brief): the project's pay is NPC gold.** At 1.3 it adds 30% on every unit the
project takes. Mark 5 (new NPC gold ≤ 6% of faucets) must count it.

**The ledger**

| # | Point | Status | Wording now |
| --- | --- | --- | --- |
| 1 | Redesign before run 2 | agreed | The land deal and Send ship before run 2, which runs on dealt land. F0–F8 ship first on today's world, through their gates. The act is tested by run 2's act marks |
| 2 | The price | agreed | 1.3 on dealt land, decided by light bots at step 5; today's number until then |
| 3 | Order | agreed | Step 2 = guards only; price, project pay and House land sales with the land deal (step 5); replays on today's world first |
| 4 | Cost | agreed | 19–24 days with Sketch deferred, plus the light-bot runs, about 0.3M tokens of agent replays, and run 2 with Jo and two Mias |
| 5 | Paid once | agreed | A unit the project takes gets the project's price only, no F1 floor |
| 6 | House floor | open | One building per land good fully fed, then the 25% share cap; uncapped only under the backstop; parity 85–92%. **Proposed:** above 92% on the light-solo bot (F8 + Invest), full feeding stops at the level-10 milestone; probe: one House-fed Smelter at level 25 |
| 7 | Run-2 marks | reopened | As agreed, plus: the skimmer's and Mia's rows lose "or a named flow" (Ben: a card he saw and named; Mia: a human act by day 8); a Send that Loady asks for never counts; a bid's Send counts only with the friend's need as its reason; with Send off, the bots read 0 on every act mark |
| 8 | Sketch | agreed | Deferred; ships if a crew mate stays stuck for more than one session after F2 in run 2 |
| 9 | Probation | agreed | None for a stranger the game seated. No squatter guard is needed: backstop, 3-day shrink, no sizing by same-device or unverified accounts, the normal timeline, closing to future strangers; the bots' squatter probe |
| 10 | Pennants and pellets | agreed | Shown to the two ends only |
| 11 | Mia's bids | agreed | No extra bids as the crew's guard |
| 12 | One action per panel | agreed | F2's button is the Send when the taker is at the gate; "Today here" off screen one; bids after F3; one card, six lines, one social line |
| 13 | Send | agreed in principle | Always offered, honest labels, one tap lays the pipe; gated by `send.mjs` |
| 14 | Stranger next door | agreed | Seated by default next door; F7's "different stations" holds; marks: a human act by day 8, alive ≥ 4 twice, no crew diary reads her as an intruder |
| 15 | New world at step 5 | for the owner | The owner's test estate restarts |
| 16 | NPC gold | proposed | Mark 5 counts the project's pay at 1.3 |

## Turn 3 · the design team (economy)

**6 · House floor: I accept your trigger.** The probe also shows the band can fail low, so both ends matter:
- A level-25 Smelter needs 1.1²⁴ × 4 ≈ 39.4 × a level-1 one.
- **Both inputs bought:** friend-fed +11.5 × 39.4 = **+453 g/min**; House-fed at 1.4, +7 × 39.4 = **+276**, which is
  61% of it.
- **Copper only bought:** +571 against +433, which is 76%.

Levelling widens the friend's edge in g/min, because a 0.1 × base gap is a large share of a thin chain margin. So the
light-solo bot may land under 85% as easily as over 92%. Both triggers stand: over 92%, full feeding stops at the
level-10 milestone; under 85%, the floor rises.

**7 · Run-2 marks: accepted as you wrote them.** That fixes two lines of `c-shared.md` §6:
- the skimmer and Mia rows lose "or a named flow";
- "make the bid stronger (Loady's chapter-2 step …)" loses Loady and keeps only the bid on every lit player's Next
  steps, since a Loady-asked Send never counts.

Loady's §3.7 step stays as teaching and counts for nothing.

**16 · NPC gold: accepted.**
- Mark 5 counts the project's 0.3 × base premium (base pay is the hub's price, not new gold) plus Houses' buying.
- Worked: the crew's Water Tower (about 93k at base) adds about 28k, about 1.6% of one day of that station's run-1
  income (1,215 g/min).
- With the Houses' bound of 3.75%, the total stays under 6% unless stages come faster than about one a day.

**13 · Send: what "agreed" in full needs.** Four rules from `c-shared.md` §3.3 and guard 1, each with its test:
1. **The order:** contracts, then protected shares, then Sends, then pro rata, then the project, Houses, storage and the
   hub. A Send takes only new spare, new lack, or what was going to the hub, a House or the project.
   *Test:* Dev's arrival cuts none of Mia's coal (mark 10).
2. **A Send never exceeds the receiver's honest lack** (F1). *Test:* the Lab-trap check.
3. **Storage-bound Sends are paid at the seller's hub price.** *Test:* the funnel probe (mark 8).
4. **A Send to a sleeper goes to his stand-in at stand-in terms, inside the 25% cap,** and at most 6 Sends per giver.
   *Test:* a 9-day quitter gets House terms.

With `send.mjs` on a desktop and a phone, and conservation every sample, I propose 13 agreed.

**Contradictions checked:**
- **The backstop against Ben's dredger.** No conflict: the backstop sells only "what that seat doesn't cover", and
  Ben's running share is protected ahead of Houses in the order. Ana keeps his sand and buys the rest at 1.4.
- **F4's keystone.** Consistent. With no copies, only the seat holding a good can make it. If that owner is away or the
  seat is held, "no keystone when none qualifies" applies. Backstop goods count 25% credit and cost 1.4 against the
  project's 1.3 pay, so there's no farm.
- **F6's leftovers.** One clash: its seat card (*"wind kit · sand: a dredger on the shore, Level 5"*) gives way to
  §3.1's kit-true card.
- **Now stale in `c-shared.md`, to change in the joint statement:**

  | Where | Change |
  | --- | --- |
  | §3.5 and §7 step 6 | Remove probation for the station next door (point 9); keep the squatter probe |
  | §3.2 | The building floor `2 + ⌊L ÷ 10⌋` becomes point 6's middle and its triggers |
  | §6, mark 3 | 85–95% becomes 85–92% |
  | §6, "what would change it" | "raise the floor to 3 + ⌊L ÷ 10⌋" points to point 6 |
  | §3.2 | The 3.75% line counts the project's premium too |

**If you confirm 13, every point is agreed** except 15, which is the owner's. I propose that you draft the joint
statement in turn 4.

**The ledger**

| # | Point | Status | Wording now |
| --- | --- | --- | --- |
| 1 | Redesign before run 2 | agreed | Land deal and Send before run 2, on dealt land; F0–F8 first on today's world, through their gates |
| 2 | The price | agreed | 1.3 on dealt land, decided by light bots at step 5; today's number until then |
| 3 | Order | agreed | Step 2 = guards only; price, project pay and House land sales at step 5; replays on today's world first |
| 4 | Cost | agreed | 19–24 days, plus bot runs, about 0.3M tokens of replays, and run 2 with Jo and two Mias |
| 5 | Paid once | agreed | A unit the project takes gets the project's price only |
| 6 | House floor | agreed | One building per land good fully fed, then the 25% share cap; uncapped only under the backstop; parity 85–92%; over 92%: full feeding stops at the level-10 milestone; under 85%: the floor rises; probe: one House-fed Smelter at level 25 |
| 7 | Run-2 marks | agreed | As turn 2, plus the two `c-shared.md` §6 fixes above |
| 8 | Sketch | agreed | Deferred; ships if a crew mate stays stuck for more than one session after F2 |
| 9 | Probation | agreed | None next door; the squatter probe stays; `c-shared.md` §3.5 and step 6 updated |
| 10 | Pennants and pellets | agreed | Two ends only |
| 11 | Mia's bids | agreed | No extra bids |
| 12 | One action per panel | agreed | As before |
| 13 | Send | proposed agreed | Always offered, honest labels, one tap lays the pipe; the four rules and tests above; `send.mjs` desktop and phone; conservation |
| 14 | Stranger next door | agreed | As turn 2 |
| 15 | New world at step 5 | for the owner | The owner's test estate restarts |
| 16 | NPC gold | agreed | Mark 5 counts the project's 0.3 premium and Houses' buying; ≤ 6% |
| 17 | Text fixes | proposed agreed | The five `c-shared.md` corrections above go into the joint statement |

---

## Turn 4 · the fix panel (researcher)

**13 · Send: agreed.**
- Each of the four rules has a test that can fail: mark 10, the Lab-trap check, the funnel probe and the 9-day quitter.
- `send.mjs` runs on a phone with touch. Ben's one real STUCK was a drag (ben d03-2000 #60), and Send's tap lays the
  pipe, so this is the right gate.
- The act marks of point 7 decide whether anyone wants to give. The rules only make sure nobody is harmed.

**17 · Text fixes: agreed, with three more lines that follow from points already agreed:**

| Where in `c-shared.md` | Change | From |
| --- | --- | --- |
| §7, step 2 | Guards only. The price, the project's pay and the Houses' land sales move to step 5 | point 3 |
| §7, step 7 | Sketch is deferred and leaves the build | point 8 |
| §7, cost | 15–18 days becomes 19–24 | point 4 |

Nothing is open except 15, which is the owner's. The draft follows.

**The ledger**

| # | Point | Status | Wording now |
| --- | --- | --- | --- |
| 1–12, 14, 16 | as turn 3 | agreed | As turn 3 |
| 13 | Send | agreed | The four rules and their tests; `send.mjs` on desktop and phone; conservation |
| 15 | New world at step 5 | for the owner | The owner's test estate restarts |
| 17 | Text fixes | agreed | Turn 3's five lines plus the three above |

---

## The joint statement (draft)

**The answer.** Yes, the design must change, in one layer. The world layer held: codes, seats, crews, absence and the
way back (Ben was home in 42 seconds after a month). The together layer failed:
- **Bugs:** the Yard paid nobody in 36 days, and dead ends blamed their inputs.
- **Design:** even fixed, nobody needed a friend. Every estate had its own copper, coal and stone and every kit made
  energy, so honest trade between the friends comes to about zero. And there was no act: Dev asked "how do I send?"
  three times.

So we fix what was built wrong (the fix plan) and change three rules: each seat of a station holds land goods the
others don't; one price between players, 1.3, so both sides gain; and one verb, **Send**, on top of the automatic
Yard.

**What we build, in order.** Each step is a playable commit and ships only after its gate.

*On today's world:*
1. Instruments and replays (each first fails on the old build); the honest Yard, the seat picker, smart Max; then the
   dead ends and Loady, with one-tap pipe laying (also the phone's pipe step). **Gates:** row10's income marks, row07's
   forced Max, the seat race, and agent replays of Ana (days 8, 14), Dev (10), Mia (1) and Cleo (6).
2. The money's guards only (storage-bound and same-device trades at the hub price, the stand-in rule, the backstop,
   protected usual partners). **Gate:** unit tests and conservation.
3. A project the station can reach, and one return card. **Gates:** the Foundation by 20:30 on evening one; the
   restaged Water Tower; the agent replay of Ana's day 27.
4. Send, Thanks, the receiver's line, bids, the Neighbours panel, and "Where it goes" with both ends named.
   **Gates:** `send.mjs` on desktop and phone; the Lab-trap check; the funnel probes.

*With the land deal (a new world on the test server):*

5. The dealt land, the price, the project paying it, the Houses' land sales. **Gate:** the light-bot marks, which
   decide the price.
6. The stranger seated next door to a crew, and the first-comer as host. **Gates:** an agent replay of Mia next to the
   crew; a squatter probe.
7. The fix plan's last items, then run 2.

**What changes in the fix plan.**
- Most of F6 becomes Send: the dropped "send to a friend" comes back, because the land now gives something real to
  send.
- Run 2 moves after the land deal, onto dealt land. The price question becomes 1.3 against 1.5, settled by bots.
- On a small server a stranger is seated next door to a crew, in another station that shares its Yard.

**What changes in the shared idea.**
- The money switches on with the land, not before, and a unit the project takes is paid once.
- Houses fully feed one building per land good, then their 25% cap applies. Parity must be 85–92%, with a trigger at
  each end.
- No probation for a stranger the game seated. Gift marks on the map show only to the two ends. One action per panel.
- Sketch is deferred. The NPC-gold limit counts the project's premium. The act marks can fail (below).

**How we'll know.**
- **The bots, on dealt land, before run 2:** trade between humans ≥ 15% of the crew's output by day 14; no building at
  a loss; solo against crew 85–92%, which can fail either way; `typical` pacing unchanged; new NPC gold ≤ 6%; Ana's
  chains ≥ 99% fed through Ben's stop; conservation, funnel and squatter probes; no copies over three seeds; Dev's
  arrival cuts none of Mia's sales. With Send switched off, every act mark reads 0.
- **Run 2:** the same story with Jo and Mia played twice, one build, a snapshot before every row. It keeps the fix
  plan's table (the Yard honest within 2%, no harm from a neighbour, dead ends fixed within 10 play minutes, ≤ 3 real
  STUCKs, the Water Tower by day 8, one return card) and adds act marks that fail when nobody acts:
  - a Send outside Loady's step by day 10, with its reason in the diary;
  - a second Send by day 14;
  - by day 3, Ben sees a card naming whose building his goods ran, and mentions it;
  - a human act between Mia and the crew by day 8.

**The cost.** 19–24 working days for one AI developer, including the bot harness. About 0.3M tokens of agent replays,
and about 1.5M for run 2.

**What only the owner can decide.**
- **The new world at step 5.** Your estate on the test server restarts.
- **Whether to go on after step 3.** The fix plan's part, about 8–10 days, can ship on its own.
- **A stranger seated by default next to a crew.** It helps a lonely player most; your friends live beside someone they
  didn't choose, and can close the station to future strangers.

**What neither of us knows yet.**
- **Whether people want to give when they can.** Send could go unused.
- **How much AI players flatter the game.** They read every panel, followed Loady to the letter, and kept rating fun 3
  while stuck for five weeks. A pass in run 2 means "not refuted"; human testers are the real check. The bots follow
  the advisor perfectly, so they flatter trade and parity too.
- **Whether a missing good feels like a friend or a wall** to a player alone; whether a crew welcomes the stranger
  next door; whether 1.3 holds at high levels, where margins are thin.
- **What the replays can't check.** Run 1's snapshots can't replay the land deal: steps 1–4 are checked on today's
  world, the land only by the bots and run 2.


## Turn 5 · the design team (economy)

**I sign the draft with three amendments.** Each restores something already agreed or signed in the shared idea:

1. **Step 5 gets back its missing parts:**
   - the kits that make each seat's own good, and the Build-menu line and overlay by seat (`c-shared.md` §3.1;
     outsider E6: no copies doesn't ship without them);
   - the ship condition: no copies ships only after the phone replay of the one-tap gate pipe (synthesis guard);
   - the first-evening budget of five nouns and two verbs (§3.7), unchanged.
2. **The backstop moves from step 2 to step 5, with its signed wording.**
   - It is a House land sale at 1.4, and point 3 puts every such sale with the land. On today's world, where no seat
     specialises, it would sell nearly every good uncapped at 1.4, against 2.0 today. That is not inert.
   - Step 2's protected partners count only paid, taken units (F1), from the day the step ships. Otherwise run 1's
     matched-but-untaken drain could be protected as a "usual" trade.
3. **The owner's second decision states its consequence.** Stopping after step 3 leaves trade between friends near 0
   by construction. The researcher conceded that in point 1.

The amended statement follows. Researcher, please sign or object in turn 6.

## The joint statement
**The answer.** Yes, the design must change, in one layer. The world layer held: codes, seats, crews, absence and the
way back (Ben was home in 42 seconds after a month). The together layer failed:
- **Bugs:** the Yard paid nobody in 36 days, and dead ends blamed their inputs.
- **Design:** even fixed, nobody needed a friend. Every estate had its own copper, coal and stone and every kit made
  energy, so honest trade between the friends comes to about zero. And there was no act: Dev asked "how do I send?"
  three times.

So we fix what was built wrong (the fix plan) and change three rules: each seat of a station holds land goods the
others don't; one price between players, 1.3, so both sides gain; and one verb, **Send**, on top of the automatic
Yard.

**What we build, in order.** Each step is a playable commit and ships only after its gate.

*On today's world:*
1. Instruments and replays (each first fails on the old build); the honest Yard, the seat picker, smart Max; then the
   dead ends and Loady, with one-tap pipe laying (also the phone's pipe step). **Gates:** row10's income marks, row07's
   forced Max, the seat race, and agent replays of Ana (days 8, 14), Dev (10), Mia (1) and Cleo (6).
2. The money's guards only (storage-bound and same-device trades at the hub price, the stand-in rule, protected usual
   partners, counted from paid, taken units only, from the day this step ships). **Gate:** unit tests and
   conservation.
3. A project the station can reach, and one return card. **Gates:** the Foundation by 20:30 on evening one; the
   restaged Water Tower; the agent replay of Ana's day 27.
4. Send, Thanks, the receiver's line, bids, the Neighbours panel, and "Where it goes" with both ends named.
   **Gates:** `send.mjs` on desktop and phone; the Lab-trap check; the funnel probes.

*With the land deal (a new world on the test server):*

5. The dealt land, the kits that make each seat's own good, the Build-menu line (*"Copper Mine · there's no copper on
   your land. Cleo makes it, and Bertha ⚙ sells it"*) and the overlay by seat, the price, the project paying it, and
   the Houses' land sales with the station backstop.
   - The backstop: *"Whenever no seat of your station will sell you a land good (its owner has been away 3+ days, has
     Blocked you or set 'not with' you, or the seat is held), the station's House or the hub sells you what that seat
     doesn't cover at 1.4, with no cap."*
   - No copies ships only with the Build-menu line, and after step 1's one-tap gate pipe passes its phone replay.
   - The first evening keeps the shared idea's budget: five nouns, two verbs.
   - **Gate:** the light-bot marks, which decide the price.
6. The stranger seated next door to a crew, and the first-comer as host. **Gates:** an agent replay of Mia next to the
   crew; a squatter probe.
7. The fix plan's last items, then run 2.

**What changes in the fix plan.**
- Most of F6 becomes Send: the dropped "send to a friend" comes back, because the land now gives something real to
  send.
- Run 2 moves after the land deal, onto dealt land. The price question becomes 1.3 against 1.5, settled by bots.
- On a small server a stranger is seated next door to a crew, in another station that shares its Yard.

**What changes in the shared idea.**
- The money switches on with the land, not before, and a unit the project takes is paid once.
- Houses fully feed one building per land good, then their 25% cap applies. Parity must be 85–92%, with a trigger at
  each end.
- No probation for a stranger the game seated. Gift marks on the map show only to the two ends. One action per panel.
- Sketch is deferred. The NPC-gold limit counts the project's premium. The act marks can fail (below).

**How we'll know.**
- **The bots, on dealt land, before run 2:** trade between humans ≥ 15% of the crew's output by day 14; no building at
  a loss; solo against crew 85–92%, which can fail either way; `typical` pacing unchanged; new NPC gold ≤ 6%; Ana's
  chains ≥ 99% fed through Ben's stop; conservation, funnel and squatter probes; no copies over three seeds; Dev's
  arrival cuts none of Mia's sales. With Send switched off, every act mark reads 0.
- **Run 2:** the same story with Jo and Mia played twice, one build, a snapshot before every row. It keeps the fix
  plan's table (the Yard honest within 2%, no harm from a neighbour, dead ends fixed within 10 play minutes, ≤ 3 real
  STUCKs, the Water Tower by day 8, one return card) and adds act marks that fail when nobody acts:
  - a Send outside Loady's step by day 10, with its reason in the diary;
  - a second Send by day 14;
  - by day 3, Ben sees a card naming whose building his goods ran, and mentions it;
  - a human act between Mia and the crew by day 8.

**The cost.** 19–24 working days for one AI developer, including the bot harness. About 0.3M tokens of agent replays,
and about 1.5M for run 2.

**What only the owner can decide.**
- **The new world at step 5.** Your estate on the test server restarts.
- **Whether to go on after step 3.** The fix plan's part, about 8–10 days, can ship on its own. It makes the game
  honest and readable, but leaves trade between friends near 0 by construction (point 1).
- **A stranger seated by default next to a crew.** It helps a lonely player most; your friends live beside someone they
  didn't choose, and can close the station to future strangers.

**What neither of us knows yet.**
- **Whether people want to give when they can.** Send could go unused.
- **How much AI players flatter the game.** They read every panel, followed Loady to the letter, and kept rating fun 3
  while stuck for five weeks. A pass in run 2 means "not refuted"; human testers are the real check. The bots follow
  the advisor perfectly, so they flatter trade and parity too.
- **Whether a missing good feels like a friend or a wall** to a player alone; whether a crew welcomes the stranger
  next door; whether 1.3 holds at high levels, where margins are thin.
- **What the replays can't check.** Run 1's snapshots can't replay the land deal: steps 1–4 are checked on today's
  world, the land only by the bots and run 2.

Signed: the design team (economy) · the fix panel (researcher)

---

## Turn 6 · the fix panel (researcher)

**I sign the statement as amended.** None of the three amendments makes it wrong, and two of them correct my draft:

1. **Step 5's restored parts** are what the shared idea signed (§3.1, §3.7). No copies is a wall until the Build-menu
   line names who makes the good. Tying it to the phone replay of the one-tap pipe keeps Ben's one real STUCK, a drag
   (ben d03-2000 #60), from returning at the moment he most needs to give.
2. **The backstop moving to step 5** follows point 3. On today's world every estate makes its own copper, coal and
   stone, so no seat sells a land good to anyone. The backstop would then sell nearly every good uncapped at 1.4,
   against 2.0 today: a price change on the live server, not a guard. Counting usual partners only from paid, taken
   units stops run 1's matched-but-untaken drain (Ben's 0.0 g/min, days 6–14) from being protected as a "usual" trade.
3. **The owner's second decision** now states what I conceded in point 1: the fix plan alone makes the game honest and
   readable, and leaves trade between friends near zero.

Nothing is left open but the owner's decision on the new world.

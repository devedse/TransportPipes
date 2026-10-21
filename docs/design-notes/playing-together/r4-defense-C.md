# Round 4 · Concept C defended: concessions, rebuttals, and C revised

Champion: the friends-first designer. I read r3-A and r3-B in full, and the three exams. Where an exam rule decides a
point, I use it: for example, the economy exam's definition of NPC-created gold, "while G1's estate still runs and
pays for its purchases from its own production, that isn't NPC gold". The contract exploit is fixed in 9621766
(a seller promises at most 1.25× what its finished buildings make, and a penalty never takes gold below zero).

**In short:**
- **Conceded:** the waiting consent for strangers (A), the caretaker's reinvest (A, B), the per-pair capacity
  puzzle (B), and the mates' rate (B). Also my own successor rule, which the economy exam would fail.
- **Rebutted:** the held-seat "farm" (A, B), "no gravity" (B), the tier-3 cap (B, aimed at r2, not r3) and "the
  halt is outside the Works" (A, a misreading).
- **Changed attacks:** two of my six attacks on the rivals no longer stand. A1 (strangers take a couple's coal) and
  B1 (Lodgers at the friend price) are fixed by their r3 specs. I replace them with what still breaks.

---

## 1 · The attacks on C

### A-C1 · "Strangers share nothing, so the lonely player's first human contact waits for someone else" → **conceded**

**What A got right.** Mia joins Jo's station at 02:40. In r3-C her link card to Jo waits until Jo next logs in
(r3-C §2.3: "offers wait 72 h"), so her first trade partners are Houses. A spec that is meant to be friends-first
shouldn't make the most fragile player's first human trade depend on another person's timing. It passes the
"never online together" test (nobody needs to overlap), but it loses the moment.

**The change (R1 below):** every player has a plaza setting, **Neighbours: open** by default. Pair links between
residents of a place start by themselves when both are "open", at Fair 1.75.
- **Crew pairs come first** in the seller's priority.
- **A buyer gets at most 50%** of a seller's supply of a good while other partners want it.
- **Either side can pause a pair privately**, and the other side then reads a neutral sentence.

So Mia sells sand to Jo's Glassworks at minute 12 while Jo sleeps. Consent is given in advance by choosing the
place on a picker that shows the residents, as in A where laying the pipe is the consent.

**What I keep:**
- *Pairs, not a pool.* Every flow has its own row, priority and pause, and the engine solves per estate.
- *Strangers never become crew.* That still takes a seat code or a founder's tap.

**Rebutted, "6 pair links, each with a preset, a tank and a state: bookkeeping".**
- Crew pairs have no preset at all (fixed 1.75), and neighbour pairs start at Fair.
- Tanks and state lines are defaults that nobody sets.
- The plaza panel is one ledger with a row per pair that moved goods this week ("Ben → Ana · 12,400 coal · 21,700
  g"). That is the same ledger A's Depot shows ("Ben → Ana 12,400 coal") and B's Main panel shows ("ledger by
  pair"). The difference is that in C you can tap a row to set "Ana first" or to pause.

### A-C2 · "A crew that spans places splits its own project" → **rebutted, with one rule clarified**

A's scenario says the friends in the halt "aren't NE residents, so their steel doesn't count there, and they get a
two-person halt Works instead".

r3-C says the opposite:
- §2.1: halt, "Works: no; a halt delivers to the Works of the station it is attached to".
- Case f: "the halts' members deliver to the station's Works through the crew pipe".

There is no halt Works.

**The real gap A missed** is in the 7–8 case. The second halt is attached (in the pinwheel) to a *different*
station, so a crew member living there needs a rule. I also never said who gets the perk.

**R5 below fixes both:** a crew member counts for the Works and perk of the crew's station, wherever the crew lives;
a non-crew halt resident counts for the station the halt is attached to.

### A-C3 · "Held seats keep a full estate running at full terms for up to 120 days" → **partly conceded**

A makes three claims.

1. **"The farm: an alt quits, the crew holds the seat, and a ghost buys at 1.75 for four months with no decay."**
   **Rebutted.**
   - A held estate in C is the real estate, running. It buys only what its own buildings consume, and pays from its
     own hub sales.
   - By the economy exam's own definition that creates **N = 0** NPC gold: "while G1's estate still runs and pays
     for its purchases from its own production, that isn't NPC gold". Since 9621766 nobody's gold goes below zero.
   - A held alt is worth exactly what it was worth while it played (no growth, see 3), and only while a real member
     keeps holding it.
   - A's own fix, a **profile** run by an NPC at the old price capped at Fair for 14 days, *is* NPC gold. With the
     exam's numbers (1,000 g/min of hub value), that is 0.75 × 1,000 × 20,160 min ≈ **15.1M per alt**, 2.8 days of
     Gary's income (§2, A3). C's "ghost" is cheaper than A's cure.
2. **"The ghost station: it looks full, and held estates are still fully simulated."** **Rebutted on cost, conceded
   on the card.**
   - A held estate never changes: nobody builds, nothing is upgraded. Today's architecture re-solves a network only
     when it changes or a storage fills or empties, so a held estate costs a cached solution and a few storage
     events a day, near zero.
   - The look is a fair point. The station card now says "2 seats held for friends (asleep)", never just "full", and
     the lone-human card still fires for Ana.
3. **"The caretaker spends Ben's gold on upgrades he never chose."** **Conceded.** The autopilot reinvest is gone
   (R3). It acted on someone's estate without the owner present, the lonely exam flags it (T15.6), and it made a
   held alt grow.

### B-C1 · "Friends get a price knob, and friends compare" → **rebutted on the knob, conceded on capacity**

- **The mates' rate doesn't exist in r3-C.** §2.4 says "Crew price · 1.75, fixed, both ways". The mates' rate was
  only a "what would change it" line in decision 5. B is right that it would breed comparisons, so I delete even
  that line: **the crew price is fixed for good.**
- **"Cleo's grey junction pipe (100/min) is shared by four pair links, so Ben's coal to her is throttled by Ana's
  energy."**
  - B has the same constraint ("Throughput per gate: grey 100/min" on the Main), and so does A ("Capacity: your
    plaza pipe's tier").
  - But B found a real snag: in C one pipe carried both directions, so *buying* coal could be crowded out by
    *selling* energy.
  - **R2:** capacity is per direction (grey 100/min in and 100/min out). The plaza panel shows one bar per
    direction, never per pair, and the advisor already reports full pipes.
- **"A crew of six reads 15 pair links."** **Rebutted:** the ledger lists only pairs that moved goods this week,
  exactly as B's "ledger by pair" does.
- **"Three layers to learn."** **Partly rebutted, with counts:**
  - C: station, plaza, crew, link, House, Works, trunk (7).
  - A: station, halt, Yard, crew, link, Lodger, trunk, Works (8).
  - B: lot, street, crew, Main, Lodger, link, Line, Junction (8).
  - I drop one more: "halt" is shown as "small station" in the UI.

### B-C2 · "Held seats keep ghosts in place for up to 120 days" → **partly conceded**

- **"C has no lone-human gravity."** **Rebutted.** r3-C decision 8: "A lone human gets one card after 7 days
  alone", and case b (the shrinking server) places newcomers only in the 1–3 districts with the most lit seats.
- **"Ben returns richer than the casual friend who kept playing."** **Rebutted with the exam's curve.**
  - Ben's day-3 estate earns about 1,930 g/min. Held for 120 days at the caretaker's 10% fee, that is
    1,930 × 0.9 × 1,440 × 120 ≈ **300M**.
  - A casual friend (1,140 g/min on day 3, 2,350 on day 7, 4,710 on day 28, then flat) earns about
    4,400 × 1,440 × 120 ≈ **760M** over the same days, and gains 2–3 times the XP.
  - Quitting never out-earns playing.
- **Conceded: it is still a lot for doing nothing**, and since XP comes from earnings, Ben gains levels while away.
  So R3 raises the caretaker's fee on a held estate from day 30 to 20%. That is a sink, and it costs only the absent
  player (about 267M instead of 300M). Friends see no difference, because Ben's real trades keep running.

### B-C3 · "The stacked solo tax is back: C keeps the tier-3 import allowance" → **rebutted (B quoted r2)**

r3-C has no tier-3 cap:
- §2.4 lists none.
- §2.5 has Houses selling tier 3 at 2.0 from Era 2.
- Quiet grace switches the Hub Order off with 5 or fewer lit seats.

I make this explicit in R8, because both B and the economy exam ("A tier-3 daily import allowance with a 4.0
surcharge (C)") were reading r2.

### Found in the exams: four weak spots of r3-C that no rival named

| Weak spot | Exam | Change |
| --- | --- | --- |
| My successor rule (80% of the 14-day median, −10% a week) gives about **7M per fake-quitting alt in 4 weeks** (1.3 days of Gary's income) and about 18% of his income over a 5-alt cycle. It fails T3 | economy T3 | R4: asymmetric successor terms, N ≈ 1.9M |
| The Works is sized by "active in the last 3 days", so a griefer who logs in without delivering inflates it | economy §7 | R5: size by residents who delivered |
| Links are "filled from the last solve" with no damping, so rings like Ana → Ben → Cleo → Ana can oscillate | economy T17 | R10: smoothing |
| Today's Exchange (0.25–3.75 × base) and uncapped corporation donations are funnels C didn't close | economy §1.4, T1 | R9 |
| The Hub Order "then 0.5" is harsher than both rivals (0.7) | economy T5 | R7: glide to 0.7 |
| Crew removal by majority, with 2 active members | lonely T9.4 | R6: no removal |

---

## 2 · My attacks on A and B, after reading their full specs

| Attack (r3-C §9) | Still stands? | Why |
| --- | --- | --- |
| A1 · a stranger takes a couple's coal from the pool | **No, withdrawn** | r3-A serves crew first in a shortage and caps any member at 50% |
| A2 · the game hands a friend's seat to a stranger | **Yes, weaker** | The hold is now 60 days after sleep (r3-A decision 7), but it still ends by itself, nobody may extend it, and A's own "change it if" shortens it |
| A3 · copy-trade lodgers create gold | **Yes, with new numbers** | r3-A's profile is NPC gold at the old price (capped at Fair) for 14 days |
| B1 · Lodgers pay the friend price | **No, withdrawn** | r3-B uses 1.25 / 2.0 and a 25% cap |
| B2 · a stranger can't use the Main; streets of one | **Mostly withdrawn** | r3-B lets strangers join open lots, prefers humans and has the lone-human card. What remains is worse (B2′ below) |
| B3 · the pro-rata Main hides who gets what | **No, withdrawn** | r3-B serves standing orders first and shows a pair ledger: that is C's pair priority under another name |

### What still breaks A

**A2 (updated) · A rule ends a friend's hold, and nobody may keep it.**
- Mara, the crew's fourth, goes on parental leave on day 5. She is asleep on day 35, and on day 95 her held seat
  "ends by itself" (r3-A, day 90 after her last login).
- Default placement ("Join neighbours", which scores +3 per active human) puts a stranger there.
- Mara comes back on day 110 to "next to your crew", one corridor away from the Yard she built.
- Rule 3 ("nobody presses a kick button") meant the friends couldn't extend the hold either. A's "change it if" goes
  the other way ("shorten the hold to 30 days").
- In C any active member extends it once, to day 155, with one tap.

**A3 (updated) · The profile is a 15M farm per alt.** r3-A: "the 14-day median volume, at the old price capped at
Fair, for 14 days; then Lodger terms". With the economy exam's T3 setup (G1 buys 1,000 g/min of hub value from Gary,
then quits):
- 14 days at Fair is (1.75 − 1.0) × 1,000 × 20,160 ≈ **15.1M**, 2.8 days of Gary's day-14 income (5.4M a day).
  Then the Lodger-terms tail.
- That scores 1 of 3 on the exam at best. The 5-alt cycle is about 75M, **40% of Gary's income** over 5 weeks.
- "Same-device accounts go to Lodger terms at once" only catches alts on the same device; a phone and a laptop
  evade it.

**A1′ (new) · "Not with" is a private block that breaks A's own rule 6 ("every zero has a sentence").**
- Mia (solo) lives in Jo and Kai's station and sells sand into the Yard.
- After a chat spat Jo sets "not with Mia". Her sand sales into the Yard fall from 30/min to 12/min.
- Her line says "Yard: sold 12 sand/min", with no reason, because "not with" is private. Crew-first already puts
  her last in every shortage.
- To the lonely player A champions, the Yard now looks rigged.
- **In C a pause is private about *who*, but never silent:** "a neighbour isn't buying sand from you right now; it
  goes to Houses and the hub".

### What still breaks B

**B2′ (new) · Default-open lots make strangers members of a couple's crew.** r3-B: "Open lots are open by default
while the crew has 2 or fewer active humans."
- Lea and Max are a crew of two, so their two free lots are open. Vex takes one, joins the crew on probation and
  builds 12 Brickworks.
- The Main serves humans by need with a 50% cap, so Vex gets half of Max's coal. Lea's Smelter drops to 50%.
- The couple's tools are both bad:
  - **Pair-mute.** Max has to notice first.
  - **A removal vote.** It needs 2 of 3 active members, so it is the kick drama A calls out.
- If Vex goes away for 5 days, probation **packs him** to the Quiet Isles: a forced move of a human.
- In C, Vex is a neighbour, never crew: crew pairs come first and his share is capped.

**B3′ (new) · The Main is lossless, game-laid, and its throughput comes from Crew Levels.** That takes distance and
pipe decisions away from friends and makes their throughput a group grind.
- r3-B's seven friends get "one lossless Main of about 136 tiles". A grey link over 136 tiles loses
  1 − 0.997¹³⁶ ≈ **34%**. A big crew strung along a street gets free district-wide transport that no link can match.
- Each gate's throughput is grey 100/min until **Crew Level 4** and steel only at **Crew Level 8**. Those levels come
  from Crew Orders, so two casual friends stay at 100 iron ingots/min (1,400 g/min of hub value, about 40% of a
  day-10 income) until they grind the orders.
- In C each player upgrades their own plaza pipe when it is full, which is the first real pipe decision of the game
  (about day 2–3).

**B4 (shared with A, and with my own r3) · The successor rule fails the fake-quitter test.** r3-B uses 80% of the
14-day median at 1.25, falling 10% a week:
- About 0.25 × 10,080 × (800 + 720 + 648 + 583) ≈ **7M** in the first 4 weeks, and more until it reaches the
  baseline.
- About 18% of Gary's income over a 5-alt cycle.

r3-C had the same rule, so I fix mine (R4) before pointing at theirs.

---

## 3 · C, revised: only the rules that change

| # | Rule | Was (r3-C) | Now |
| --- | --- | --- | --- |
| R1 | **Neighbours link by default** | Stranger pairs needed a card accepted by both (72 h) | Plaza setting **Neighbours: open** (default) / ask me / closed. Pairs between "open" residents of a place start by themselves at Fair 1.75. The seller serves crew pairs first; one buyer gets at most 50% of a seller's supply of a good while other partners want it. A pause is private about who, never silent: the other side reads "a neighbour isn't buying X from you right now; it goes to Houses and the hub" |
| R2 | **Plaza pipe capacity per direction** | One capacity for all pair links | Grey 100/min in + 100/min out, copper 500 + 500, steel 5,000 + 5,000. One bar per direction on the plaza panel |
| R3 | **Held estates** | Caretaker-run, autopilot reinvest of up to 30%, 10% fee | Still the real estate, running in place (N = 0 NPC gold). **No reinvest, ever.** Fee 10% during dormancy, **20% from day 30** while held. The card shows "held for a friend (asleep)". Holds: any active member, 60 days after sleep, renewable once (+60); they lapse 14 days after the crew's last active login (unchanged) |
| R4 | **Successor House** (unheld seats only) | 80% of the 14-day median at House prices, −10% a week | **Sells** to the leaver's partners at 2.0: 100% of the 14-day median for 2 weeks, then −25 points a week (75, 50, 25, then the baseline), so a buyer's cost rises 1.75 → 2.0 (+14%) on the handover day. **Buys** from them at 1.25: 50% of the median in week 1, 25% in week 2, then the House baseline inside the 25% cap. Fake-quitter N = 0.25 × (500 + 250) × 10,080 ≈ **1.9M per alt** (0.35 day of a day-14 income); 5-alt cycle ≈ 5% |
| R5 | **Works: who counts** | Sized by residents active in the last 3 days; halt membership implicit | Sized at 6 h of T(L) × residents who **delivered to the Works or traded at the plaza in the last 7 days** (at least 1). Houses count 0.5. A crew member counts for the Works and perk of the crew's station wherever the crew lives. A non-crew halt resident counts for the station the halt is attached to |
| R6 | **No removal from a crew** | Removal by a majority of active members, weekly | Nobody can be removed. Leave is one tap, and any pair can be paused privately (R1). A crew that splits keeps its name with the members who stay; credit, plaques and segments stay with each person |
| R7 | **Hub Order** | Full price up to 0.6 × T(L)/min, then 0.5 | Full price up to the order, then a straight glide to **0.7 at twice the order**, and 0.7 beyond. Quiet grace unchanged (×2 with 6–11 lit seats, off with ≤ 5) |
| R8 | **No tier-3 import cap** | Implicit | Explicit: the hub sells every tier at 2.5 with no allowance; Houses sell tier 3 at 2.0 from Era 2 |
| R9 | **Every funnel closed** | Band, device lock, crates | Plus: **Exchange** prices clamp to the band, 1.0–2.5 × base (today 0.25–3.75). **Corporation donations** at most 1 h of the donor's T(L) a day. **Contracts** as in 9621766 (promise ≤ 1.25× what your buildings make; no gold below zero), plus: NPCs never owe or collect penalties, and a penalty is at most 10% of the week's deliverable value |
| R10 | **Damping** | Links filled from the last solve | A link's offered amount is the average of its last 3 solves, and it changes by at most 25% of the link's capacity per minute. Tanks absorb the difference, so rings settle instead of oscillating |
| R11 | **Crew price fixed for good** | "Add a mates' rate if crews ask" | Deleted. No price setting between crew mates |
| R12 | **Start area** | ★2 surveyor patches moved into the first buyable parcels | Keep today's starter kit in the start area (as A and B do: no change to running code or tests). New estates also get their **★5 home good, 3×3, next to the HQ**, so the role shows at minute 1. The make-or-buy stays ("my ★2 2×2 coal or Ben's ★5 at 1.75") |
| R13 | **Words** | station, halt, plaza, junction pipe | The UI says "small station" for a halt and "plaza pipe" for the junction pipe: 7 player-facing words |

**Unchanged:**
- the places and the pinwheel;
- crews of 1–8 with seat codes and holds;
- crew price 1.75;
- link presets for links between places;
- Houses at 1.25 / 2.0 with all of them together capped at 25%;
- the absence timeline;
- the return screens, Tailwind and jump kits;
- trunks with fixed 2% lanes and no upkeep;
- the Line Race, leagues and the Station Cup;
- the migration;
- the 18 build days. R1, R2, R5, R7 and R10 fit into steps 1, 2, 6 and 7; R9 adds about half a day.

---

## 4 · If C lost: I would back A, on these conditions

**Why A and not B.** After round 3, A and C share most of their skeleton:
- the same places, carved only from corner parcels that nobody owns;
- a plaza at every member's corner, on screen whenever you look at your land;
- a station Works;
- the same economy layer.

B gives up the plaza for a corridor pipe that is lossless and gated by Crew Levels (B3′), and it makes strangers
crew by default (B2′). B's real strength, a migration with nothing carved, matters less now that the corner parcels
turn out to be unowned (Levels 35–50, while Level 30 comes on day 25).

**What A must take from C:**
1. **Holds that friends control.** Any active member renews once (60 + 60 days), and a held estate is **the real
   estate running**, not an NPC profile. That keeps friends' trade exact and creates no NPC gold (A3).
2. **Asymmetric successor terms** (R4): sell at 2.0 at 100% for 2 weeks, buy at 1.25 at 50% then 25%. That passes
   both the lonely exam's T1 (+14% cost on the handover day) and the economy exam's T3 (1.9M per alt).
3. **Pair priority on top of the pool.** A standing "Ana first" for any seller, and "not with" replaced by a pause
   whose other side always reads a sentence (A1′).
4. **No caretaker reinvest**, and Works sized by residents who delivered (R5), so logged-in sleepers can't inflate
   it.
5. **Plaza capacity per direction** (R2) and **damping** of lagged links (R10). A's Yard pool of up to 8 sockets
   plus links filled from the last solve has the same ring problem.
6. **The closed funnels** (R9: Exchange band, donation cap, NPCs never owe penalties).
7. **The crew price fixed for good**, with no mates' rate anywhere.

With those seven, A and C are the same game with a different word for the plaza. That is my real conclusion: the
team has converged, and the owner can pick either name.

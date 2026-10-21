# Round 3 · The outsider's exam

This exam is written in the voice of a real new player and of the owner's core wish: **"it must be fun to play with
3 friends that join"**. It has 20 tests, and each one has a setup, the exact events, and pass/fail criteria. I will
run all of them against r3-A.md (Stations with a Yard), r3-B.md (Streets & Lines) and r3-C.md (Places, Crews, Links).

---

## How I examine

1. **Only the spec's text counts.** If a spec doesn't say what happens, that criterion fails. The owner asked for no
   open questions.
2. **"Tune later" counts as undecided** unless the spec gives a starting number *and* says what would make the team
   change it (the rule in round3.md).
3. **I count clicks, seconds and days from the flows the spec describes.** If a flow isn't written out step by step,
   I count the smallest number that is plausible and mark the criterion partial.
4. **For clarity tests (T1, T2) I read only the player-facing parts:** UI text, tutorial lines and screen names. Rules
   hidden in the solver don't help a new player.
5. **Scoring:** pass = the full weight, partial = half, fail = 0, for **100 points in total**.
6. **Six tests are must-pass: T3, T4, T6, T13, T15 and T20.** These carry the owner's core wish and the safety of
   real players. A concept that fails any one of them is "not shippable as written" whatever its total. I will name the
   smallest change that would make it pass.
7. **Ties** are broken by T4, then T6.

**Red flags.** Each one turns the test it appears in into at most a partial:
- more than one price for the same good between the same two companies;
- any reward, goal or multiplier that counts humans;
- any co-op timer shorter than 12 hours;
- an NPC that sends more than one message a day;
- anything in shipping steps 1–3 that needs more than 8 humans to make sense;
- a structure where a stranger's settings change what *my* buildings receive.

### The cast (the same in every test)

| Player | Who |
| --- | --- |
| Ana | Founds the crew, plays on desktop in the evenings |
| Ben | Plays on his phone. Quits on day 3, returns on day 36 |
| Cleo | Plays late at night. Her sessions rarely overlap the others' |
| Dev | The friend who joins on day 8 |
| Mo | A stranger who already lives in the place where the friends settle |
| Lina | A solo player who installs at 03:00 |
| Ravi | A newcomer in week 3 |
| Gary | A griefer with two alt accounts, Alt1 and Alt2 |

A season is 28 days in a persistent world. "Place" means whatever the spec calls it: station, street or halt.

---

## A. Clarity

### T1 · Count the new words (weight 5)

- **Setup:** list every noun a player must understand to *act* socially in week 1, meaning a word they see on screen
  and need in order to decide something.
- **What doesn't count:**
  - words the game already has: estate, parcel, building, pipe, gate, corridor, hub, Trading Post, gold, level,
    contract, corporation, Wonder, outpost, research;
  - proper names (Loady, a Lodger's name). The category word ("Lodger") does count.
- **What counts, for example:** station, street, halt, crew, Yard, plaza, Main, valve, Line, link, Junction, Lodger,
  House, caretaker, deed, sitter, Works, Wish, tune-up, favour, tending, pump house, slot, toll, Cup, trust.
- **Pass:** at most 5 new words in week 1 and at most 10 over the season.
- **Partial:** 6–7 in week 1, or 11–13 over the season.
- **Fail:** 8 or more in week 1.

### T2 · The one-sentence and routing test (weight 5)

**Setup, on day 2:**
- Ana makes 1,500 coal/h, and her own Smelter uses 500/h.
- Cleo (crew, same place) wants 400/h.
- Mo (not crew, same place, no agreement with Ana) wants 300/h.
- Zed (another place) has a link with Ana at the default price for 200/h.
- Ana's storage is full.

**Events:** Ana asks "where does my coal go, and at what price?"

**Pass (all four):**
1. The spec gives exactly one answer for each destination (own Smelter, Cleo, Mo, Zed, the hub) with its price.
2. The answer has at most 4 priority steps.
3. Ana sees the split in at most 2 taps from her estate view, on a screen the spec names.
4. I can write, from the player-facing text, **one sentence of at most 25 words** that a day-2 player would say and
   that is true here. It must stay true in three edge cases:
   - a crew mate is asleep;
   - a stranger lives in the same place;
   - nobody needs your good.

   An example of a pass: *"Friends next to me buy my spare goods at a fixed fair price; anyone I've linked with gets
   theirs; the rest goes to the hub."*

**Partial:** the answer exists but needs more than 4 steps, or no screen is named.

**Fail:**
- the split is ambiguous (does Mo get any? does Cleo come before Zed?);
- the sentence needs "it depends";
- a price depends on a setting the player has never seen.

---

## B. The first 20 minutes with friends

### T3 · The invite link in 60 seconds (weight 6, MUST-PASS)

- **Setup:** Ana founded "Kettle Works" on desktop an hour ago and holds 3 seats. Ben has never played. He opens the
  invite link on his phone, inside WhatsApp's in-app browser, with no account.
- **Events:** Ben taps the link, then does whatever the spec's flow asks, until he is in the game.

**Pass (1–3 are required):**
1. At most **3 taps and 60 seconds** from tapping the link to seeing his own estate in a held seat **next to Ana**.
   "Next to" means at most 20 tiles gate to gate, in the same place.
2. Nothing stands in front of playing: no account, e-mail or password. It may be asked for later, for example before
   he can save or after day 1.
3. No level gate, and the founder doesn't have to approve the seat invite.
4. Ana sees "Ben moved in" on the first screen of her next session.
5. If the same link is posted on a public forum, a stranger can't take Ben's seat. The link is tied to one seat, or
   needs the founder's tap after 24 hours.

**Partial:** 1–3 pass but 4 or 5 fails. **Fail:** any of 1–3 fails.

### T4 · The first 20 minutes, without being online together (weight 7, MUST-PASS)

- **Setup:** Ana and Ben start together at 21:00. Cleo starts at 23:10, when Ana and Ben are offline.
- **Events:** each one plays the first 20 minutes of their own first session, following the tutorial.

**Pass (all six):**
1. By minute 20 each player has their own working income that depends on nobody else.
2. By minute 20 of *each* player's session, a pipe from their estate carries goods to or from a friend. It is drawn
   in their colours and visible from their own estate view.
3. A first shared goal has started, and one player alone can make visible progress on it.
4. Cleo's late arrival still gets the shared moment (the handshake or its equivalent) with Ana and Ben offline. No
   moment is lost because nobody else was there.
5. No co-op step costs more gold than the tutorial gives, or needs Level 4 or higher, a corporation, or another player
   to be online.
6. At most 3 new social words are introduced in these 20 minutes.

**Partial:** 4–5 of the six, with #1 and #2 among them. **Fail:** 3 or fewer, or #1 or #2 fails.

### T5 · The friend who joins on day 8, after the others forgot (weight 5)

- **Setup:** the crew is Ana (Level 14), Cleo (Level 13) and Ben (Level 9, resting since day 3). One seat is held for
  a fourth friend.
- **Events:**
  - Day 7: nobody renews anything. Friends forget.
  - Day 8, 19:30: Dev opens the original link.

**Pass (all five):**
1. Dev lands in the held seat. If the hold had expired, he lands in the same place or an adjacent one, at most 20
   tiles from a friend, without anyone's help.
2. In his first hour Dev sells at least one good to a friend, and his first-hour income is at least that of a solo
   player on day 1.
3. The catch-up rule is a stated formula that ends by itself, with no countdown.
4. Dev's contributions to shared goals count as a share of *his own* output, so his part is visible.
5. Ben's absence doesn't block or complicate Dev's arrival.

**Partial:** #1 needs a manual workaround, for example a friend must make a free seat. **Fail:** Dev lands in another
place with no path to his friends, or #2 fails.

---

## C. Fun versus chores

### T6 · The guilt audit (weight 6, MUST-PASS)

- **Setup:** a crew of 3 in one place. Ben is away from day 3 to day 13.
- **Events:** I compare every number and message on Ana's screens on day 10 in two worlds:
  - **World X:** Ben is absent.
  - **World Y:** Ben's seat was always an NPC.

**Pass (all six):**
1. No reward, goal, multiplier or bonus counts humans. The two worlds give equal rewards for equal effort.
2. Goals resize to active members within 3 days of an absence.
3. Ana's chains never stall. A fallback feeds them within one solve, and she reads one neutral sentence about it.
4. Strangers see Ben's absence only vaguely ("resting"), never as a number of days.
5. No message to Ana names Ben as the cause of a loss. A line like "Ben has 2,000 coal in storage" while Ana's
   Smelter is empty fails.
6. Nobody's streak resets.

**Partial:** one minor breach of #4–#6. **Fail:** #1 or #3 is breached.

### T7 · Skipping days, and counting the chores (weight 5)

- **Setup:** two identical crew players. P1 plays 3 minutes every day. P2 plays 3 minutes every other day.
- **Events:** I play 14 days for each, by the spec's rules.

**Pass (all five):**
1. The spec lists every recurring co-op action. Renewing holds, tending, filling orders, setting valves, accepting
   link requests, renewing contracts and adopting pumps all count. At most **3** of them are needed for at least 90%
   of the co-op value.
2. The shortest co-op timer is at least 12 hours. Offers, Wishes, orders and favours bank or wait for at least 72
   hours.
3. After 14 days, P2 has at least 85% of P1's co-op gains (estimated from the spec's numbers).
4. Nothing anywhere needs two players online at the same time.
5. There is no daily login reward that resets.

**Partial:** one of #1–#3 is breached. **Fail:** #4 is breached, or two of #1–#3.

### T8 · The coal seat (weight 4)

- **Setup:** Ben took the coal-rich seat, and Ana and Cleo built their chains on his coal.
- **Events:** on day 5 Ben wants to use all his coal for his own steel.

**Pass (all four):**
1. Ben can do it with one switch, in at most 2 taps.
2. His friends' consumers move to the fallback within one solve, with a neutral sentence ("the hub supplies your coal
   meanwhile") that doesn't mention Ben.
3. Every seat can build every Era 1 chain, because every seat has at least small deposits of every basic.
4. No rule turns a seat into a profession. Bonuses come from land, not from a job title.

**Fail:** a seat restricts what can be built, or a message blames Ben.

---

## D. The assumptions the insiders share

### T9 · Five players, and a server shrinking from 200 to 20 (weight 6; hard case b)

**Setup A:** a server with 5 humans: the crew of 3, Mo, and Lina installing at 03:00.

**Setup B:** a server that shrinks from 200 humans to 20 over 3 weeks, spread evenly over the map.

**Pass (all eight):**
- A1. By default, Lina lands in a place with a human, or one next to it (at most 20 tiles).
- A2. Every feature in shipping steps 1–3 works with 5 humans. Nothing needs a crowd: no leagues of 25, rental
  markets or matching by level band.
- A3. Lina's own place has at most 3 NPC companies, all labelled, and together they send at most 1 message a day.
- A4. Lina has a weekly competition she can win, or a personal-best goal.
- B1. Nobody stays the only human in a place for more than 7 days without a one-tap, opt-in offer to move somewhere
  lively.
- B2. The map shows no grey dead estates. They are packed away or tidied.
- B3. Shared projects resize to the active population.
- B4. Nothing breaks or looks broken when Lines, rentals or markets have no users.

**Partial:** 6–7 of the eight. **Fail:** A1 fails, or 5 or fewer pass.

### T10 · More than a price (weight 4)

The assumption under test: "playing together means trading goods for gold".

- **Setup:** a crew of 3 that each play 3 minutes a day for one week.

**Pass (all four):**
1. Friends build a shared *thing* together, and it is visible from each member's own estate view.
2. The crew gets at least 3 named shared moments in week 1: a building rises, a handshake, a segment carries their
   colour.
3. The friendship shows as named flows ("Ben → Ana 12,400 coal this week"), not only as a better number.
4. Friends get more out of the game than strangers do in at least one way other than a price.

**Fail:** the only thing friends get over strangers is a price.

---

## E. Buildability and performance

### T11 · One AI developer, in days (weight 5)

- **Setup:** the codebase the brief describes. Tenants, caretaker, Quiet Isles, relocation, blueprints, teleporters,
  corporations, contracts, Wonders and the flow solver all exist.

**Pass (all five):**
1. Shipping step 1 takes at most 3 developer-days and, on its own, makes T3 pass.
2. The steps that cover T3–T9 take at most 15 developer-days in total.
3. Every step leaves the game playable and comes with its own migration.
4. At most 1 new kind of solver.
5. New art in steps 1–2 is limited to icons, colours and existing sprites.

**Partial:** 16–25 days in total, or one early step needs new art.

**Fail:**
- more than 25 days;
- step 1 doesn't give friends a shared place;
- anything needs a solver that spans a whole district.

### T12 · The performance bound (weight 4)

**Pass (all four):**
1. The largest cross-company structure solved as one unit is at most 8 estates. The alternative is a
   teleporter-style split of O(members × resources) with at most 8 members.
2. The spec states a per-tick cost or bound for a 2,000-player server.
3. Asleep estates are aggregated or simulated cheaply, not solved in full every tick.
4. Long pipes (trunks, Lines) carry at most about 20 links and are solved like teleporters, never as merged
   networks.

**Fail:** a structure the size of a district is solved as one network, or the bounds aren't stated.

---

## F. The hard cases

### T13 · (a) Ben quits on day 3 and returns on day 36 (weight 5, MUST-PASS)

- **Setup:** the full story of hard case (a). Ben's last session is on day 3.
- **Events:**
  - Days 3–36: Ben is away.
  - Day 36: Ben opens the game.

**Pass (all six):**
1. Within 60 seconds and at most 3 screens, Ben is back on his complete estate, in his old seat or next to his
   friends.
2. Nothing he built is lost.
3. While he was away he got at most 3 messages, none with guilt or a countdown.
4. His stand-in created no gold windfall. After a notice period the caretaker or successor trades at NPC prices, and
   its volume falls over time. (This closes 4-economy's "fake quitter" farm.)
5. No crew goal failed because of him.
6. His catch-up has no expiring timer.

**Partial:** #3 or #6 is breached. **Fail:** #1, #2 or #4 is breached.

### T14 · (c) The newcomer in week 3 (weight 4)

- **Setup:** Ravi starts on day 17. Most players are Level 22–30, and the Wonder is 60% built.
- **Events:** Ravi takes the default placement and plays for 7 days.

**Pass (all five):**
1. In his first session he has a human trade partner, or a human neighbour within 20 tiles.
2. No veteran can set his prices outside a fair band or impose penalties on him (no shortfall clauses below a
   stated level).
3. A stated catch-up formula gets him to the active median minus 5 within 10 days.
4. His Wonder and shared-goal credit is a share of his own output, so 20% of his output is worth the same tier as a
   veteran's 20%.
5. The first ranking he sees compares him with players at a similar stage.

**Partial:** 3–4 of the five. **Fail:** #2 is breached, or 2 or fewer pass.

### T15 · (d) A griefer with two alt accounts (weight 6, MUST-PASS)

**Setup:** Gary plus Alt1 and Alt2, on two devices. He has a week. Each attack must be closed by a stated rule with a
number.

| # | Attack | It is closed if |
| --- | --- | --- |
| 1 | The alts sell to Gary's main (or buy from it) at the most favourable price the spec allows, for 7 days | The gold that can be moved per day is bounded by what the alts actually produced, and the bound is stated |
| 2 | **The fake quitter:** Alt1 sets the top buy price on its link with Gary, inflates its demand for a week, then stops playing | Its stand-in drops to NPC prices after a notice period, and its volume decays |
| 3 | The alts join Gary's crew to raise any reward | No reward counts humans, or alts from the same device don't count |
| 4 | **Hoarding seats:** Gary founds crews and holds seats in 5 places | Holds are limited per crew, need an active member, and expire |
| 5 | Walling a corridor, gate or shared plaza with pipes | Impossible: no corridor or plaza tile can be owned |
| 6 | Dumping coal to crash a rival's price | The hub's price reaction is personal |
| 7 | Gary lives among strangers and tunes his settings to drain the shared thing or hog its capacity | Each member's capacity is their own, and every draw is a paid purchase |
| 8 | Posting a friends' invite link in public to take a held seat | The invite is tied to its seat, or needs the founder's tap |
| 9 | An offensive name, banner or chat | Emblems are presets, names are filtered and reportable, and chat can be muted |

**Pass:** 9 of 9 closed. **Partial:** 7–8. **Fail:** 6 or fewer, or attack 2 is open.

### T16 · (e) Two friends fall out on day 10 (weight 5)

- **Setup:** Ana and Ben are in the same crew and the same place, with a crew trade flow between them and a shared
  project at 60%.
- **Events:** Ben says "I'm done with Ana". Both keep playing.

**Pass (all six):**
1. Ben can end all trade with Ana in at most 2 taps. A 1-hour notice and the fallback mean neither factory stalls
   without warning.
2. Ben can leave the crew in 1 tap and keep his estate, seat and all progress.
3. Neither can evict the other from land, and nothing is held hostage: no shared wallet, and no shared storage or
   pipe paid for by one and kept by the other.
4. What each contributed to the shared project stays credited to them.
5. Ben can move away for free if he wants, and Ana isn't shown anything humiliating.
6. If they still share a place, nothing forces them to trade. Being neighbours in the shared thing doesn't mean
   trading with each other.

**Partial:** one of #3–#6 is breached. **Fail:** #1 or #2 is breached.

### T17 · (f) Seven friends (weight 4)

- **Setup:** seven friends start the same evening through one invite link.

**Pass (all six):**
1. All 7 end up in one place or in places next to each other, at most 20 tiles apart.
2. All 7 share one crew identity (name and colours).
3. Every one of the 7 takes part in the shared thing. Nobody is the odd one out in a separate place without it.
4. The default flow does this without a manual workaround.
5. Crew trade works between all 7 at the same price.
6. The performance bound from T12 still holds.

**Partial:** #3 holds only for 6 of the 7, or it needs one manual step. **Fail:** the group is split with no shared
thing between the parts.

### T18 · (g) Week 4 and the next season (weight 5)

- **Setup:** Kettle Works has 3 active members in week 4. The season ends on day 28, and season 2 begins.

**Pass (all five):**
1. In week 4 there is a group goal that needs at least 2 days of the crew's combined output, uses pipes and
   logistics, and can be finished without being online together.
2. Friendly competition between groups is scored per active member.
3. The season change keeps crews, seats, held seats and estates (the world is persistent) and gives one shared
   closing moment.
4. A new player on day 1 of season 2 is not behind veterans in anything that is ranked.
5. Veterans get something new in season 2, not a repeat of season 1.

**Partial:** 3–4 of the five. **Fail:** 2 or fewer.

### T19 · (h) Migrating the testers' world (weight 4)

- **Setup:** today's tester world. It has players with buildings on any parcel, pipes between estates through the
  corridors, corporations, contracts, toll meters, teleporters and franchises.

**Pass (all six):**
1. A step-by-step migration with no wipe.
2. Every player keeps every building, or gets a free move of the whole estate to an equivalent spot with all
   buildings.
3. Existing pipes between estates have a decided fate: they become links, or they are refunded.
4. Corporations, contracts, toll meters, teleporters and franchises each have a decided fate.
5. Testers who trade most with each other end up in the same group or next to each other.
6. The migration can be run on a copy and rolled back, and it takes at most 2 developer-days.

**Partial:** 4–5 of the six. **Fail:** a wipe, or an undecided fate for something players built.

### T20 · No open questions (weight 5, MUST-PASS)

- **Setup:** the 12 questions in round3.md.
- **Events:** for each question I look for a decision, a reason, and a number where a number is needed.
- **Pass:** all 12 are decided. Question 10 (the size of the start area: keep, enlarge, or move patches into the first
  buyable parcels) must be answered explicitly.
- **Partial:** 10–11 are decided.
- **Fail:** 9 or fewer. Any question handed back to the owner counts as undecided.

---

## Weights at a glance

| Test | Weight | Must-pass | | Test | Weight | Must-pass |
| --- | --- | --- | --- | --- | --- | --- |
| T1 Words | 5 | | | T11 Buildability | 5 | |
| T2 One sentence & routing | 5 | | | T12 Performance | 4 | |
| T3 Invite in 60 s | 6 | ✔ | | T13 (a) Return on day 36 | 5 | ✔ |
| T4 First 20 minutes | 7 | ✔ | | T14 (c) Week-3 newcomer | 4 | |
| T5 Day-8 friend | 5 | | | T15 (d) Griefer + alts | 6 | ✔ |
| T6 Guilt audit | 6 | ✔ | | T16 (e) Falling out | 5 | |
| T7 Skip a day, chores | 5 | | | T17 (f) Seven friends | 4 | |
| T8 The coal seat | 4 | | | T18 (g) Week 4, season 2 | 5 | |
| T9 (b) Five players, shrink | 6 | | | T19 (h) Migration | 4 | |
| T10 More than a price | 4 | | | T20 No open questions | 5 | ✔ |
| | | | | **Total** | **100** | |

## What my report will look like next round

For each spec:
- a table of the 20 tests, each marked pass, partial or fail, with the points and the criterion that decided it;
- the must-pass status;
- for every failed test, the smallest change that would make it pass.

The report ends with a ranking, and which parts of each concept I would carry into the final design.

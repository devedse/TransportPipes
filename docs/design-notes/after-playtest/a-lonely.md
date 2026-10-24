# Round A · lonely

I was the guardian of lonely players and leavers. I wrote the six veto rules, the exam the three concepts were
graded on, and the absence timeline the build follows. I read `findings.json` in full, Mia's four sessions and Ben's
five, Ana's day-27 and day-36 sessions, the group chat and the events. I also read the fix plan with the engineer's,
researcher's and economy designer's notes, and checked the absence, return, House and seat code at `2a3ea24`.

My verdict in one line:
- **The leavers' half of my design worked.**
- **The lonely half was built for a world the game doesn't produce.** Its visible-life layer was never built.
- **One rule I never wrote let a present player rob an absent one.**

## What held, what failed

### What held: leaving and coming back

Four of the run's twenty promises were kept. Two of them are mine: the late friend and the returner.

- **Dev, a week late, with nobody online.** He opened Ana's day-1 code on day 8 and was on his estate in about a
  minute. He got Capital Power's three Wind Moilys, already piped, and Tailwind ×1.8 (dev d08-2000 #3–#8, #45).
  Nobody had to answer anything.
- **Ben, after 33 days.** He went from opening the game to his estate in 42 seconds (ben d36-2000 #72–#79). The first
  card read: "33 days of your company on its own · +2.6M gold · … Granny Ida stood in for you · From day 30 your estate
  rested: it made nothing, and nothing was lost". His seat was held in place. His note: *"a month away and nothing
  lost"*.
- **The machinery ran on time.** The stand-in notice went out on day 27 at 02:01, 23 days after Ben's last session.
  Granny Ida ⚙ took over on day 34 and handed the trades back on day 36 ("Riverside Co. is back") (events).
- **What made it in:** `Sleep()` no longer ends trades. The held seat, the keep card, the 5-day threshold for the
  stand-in's old price (`GameWorld.StandIns.cs:36`) and the landing cards with crate counts all made it in.

Nothing in the run argues for changing this machinery. It is half of what worked.

### What failed, and why

| Promise (my domain) | Status | Why | Class |
| --- | --- | --- | --- |
| Mia's spare sells by minute 12, seated beside an active human | missed | The picker loop (mia d01-2000 #10–#22, STUCK #18) was a bug. "A station of my own" put her in a **two-seat** station (`SuggestSeat` takes the lowest slot, `GameWorld.Crews.cs:689–702`), beside one Glassworks House that has no trading post and can never run (`GameWorld.Tenants.cs:55–57, 75`). And the design's own story assumed things that weren't there: "Mia's **sand** sells to **Jo's** Glassworks" (multiplayer-design §17b). Mia's kit made energy, and there was no Jo | (a) and (b), and §17b's core (c) |
| A week in, Mia is not alone | missed | The lone-human card needs a livelier station with a free seat (`GameWorld.Return.cs:208`, `:253`). The only humans were a crew holding all four seats of their station, so it rightly never came. On a fresh small server that is the normal case, and the design had no other answer | (b) |
| The world doesn't feel empty: lit windows, the notice board's daily line, "usually here: evenings", postcards (multiplayer-design §14) | not built | None of these is in the code (searched client, server and game). The status file drops only the Back! ribbon, the Houses' postcard, the stills and the Era kit (ImplementationStatus, refinement of 2026-10-21). Mia's only sign of life was the district chat, and it had no dates: *"their answers are old … I'm not sure anyone is around now"* (mia d08-2000 #105) | (a) |
| Mia earns 85–90% of a crewed player | inconclusive | 66% of Dev raw, 95% per building (r1-economy §7). The crew earned nothing from trade either. But the dead House took about 28 g/min of her energy for 36 days, unpaid (r1-engineer item 1): roughly 1.4M gold, about what she owned on day 15 | (a), not tested fairly |
| Ben's resting estate earns in full | missed | Cleo's level-35 Lab made the Yard take 72 energy a minute of Ben's spare. He earned **0.1–0.3 g/min for eight days** (findings, the Yard bug), about 0.9M gold lost (r1-economy). The cause is a bug in `Measure`. The gap is mine: my veto rule said "absence costs nobody else" and protected the present from the absent. Nothing protected the absent from the present, though a resting player is the one person who can't react | (a) bug, (b) missing rule |
| Ana can tend Ben's estate; Ben reads "tended on N days" | missed | Tending lives only behind the crew chip, which got 0 clicks in 36 days (findings, "What matters waits…"). It also does nothing: `Tend` adds a counter, "never who" (`GameWorld.Return.cs:412–424`). The design's return screen showed names ("Ana tended your estate 17 times"); the build dropped them. I accepted "no stat bonus, so it can't be farmed" in round 5. The researcher is right: a tend that does nothing gets one tap, never a second | (a) and (b) |
| The stand-in is announced and it's clear what it means | missed | The notice sat in a bell with 93 unread and named "Riverside Co.", whom Ana never tied to Ben (ana d27-2000 #247–#253, d21-1900 #240). My design put the notice on a link's state line. With a pooled Yard there are no per-partner lines, so the notice had no home. And it told Ben "your neighbours' trades with you went on" when there were none | (b) |

Two more things belong here.

- **Guilt came from a false promise, not from the game.** Ben's diaries carry guilt about sand three times (*"Still no
  sand for Ana, guilt"*, ben d03-2000). The seat card had promised him *"Riverside: water and sand"*, and his land had
  none.
- **Absence never cost anyone, but only because nobody depended on anyone.** 0 Asks, 0 links, 0 tends (metrics). The
  stand-in's price glide and the old-price week were never tested. The one real dependency (the Yard) was false.

### Where I was wrong

- In rounds 4 and 5 I defended the open Yard as *"the one mechanic that gives a lonely player at 02:40 a human buyer
  without waiting for anyone"*. The run says otherwise:
  - All starter kits make energy, so in week 1 nobody lacks what a neighbour makes.
  - With honest lacks the Yard will trade very little (r2-researcher, "What everyone missed").
  - At 1.75 a neighbour's ore pays the buyer about +1 g/min on a Small Factory (r3-economy).

  Trade can't be the lonely player's lifeline in week 1. Being seen, and a shared project, can.
- I solved loneliness by **seating**: gravity, the lone-human card, the consolidation card. Seating needs another
  human with a free seat. On a fresh server the first crew closes its station (`CrewClosesStationAt = 3`,
  `GameWorld.Crews.cs:82`), and the first solo player always lands alone.
- **The sharpest fact in the run.** Mia's station, Capital N, is the small station directly east of the crew's Capital
  NW (`WorldLayout.cs`, station blocks (2,0)–(2,1) against (0,0)–(1,1)). She lived next door to Ana, Ben and Cleo for
  five weeks, in a separate Yard with a dead House, and nothing joined them.

## Is the fix plan enough?

For leavers and returners, nearly. For the lonely player, no: it fixes the bugs and tells her the truth, but leaves
her in a separate room.

### Where it repairs a cause

- **F1, the Yard's floor.** "What the Yard matched and the buyer didn't take is paid to the seller at its own hub
  price." This is the rule I never wrote: no neighbour can lower your income. It protects the resting player most,
  since he can't react. The gate "row10 + 6 h: Ben ≥ 70 g/min (run 1: 0)" tests exactly Ben's case.
- **F1.5, no dead Houses.** A Neighbour-in-need House gets a trading post, and its missing good is one the station's
  humans make, chosen again daily. Mia's House then buys her energy at House prices: honest, labelled, inside the 25%
  cap.
- **F5, one return card with "your people".** Resting, a tend that repairs, the stand-in, back, moved in. "For a player
  without a crew, who answered them in the district chat and when." That is Ana's day-27 card and Mia's day-8 card.
- **F9, names.** "Riverside Co. (Ben)". Stand-ins copy what partners took, not what was matched.
- **F6, the seat card tells the truth** ("wind kit · sand: a dredger on the shore, Level 5"). That removes the
  promise Ben felt guilty about.
- **F7, the picker.** The stale seat goes, and "A station of my own" prefers four seats.
- **F10, chat with day stamps and "new since you left".** That is Mia's *"not sure anyone is around now"*.

### Where it only patches a symptom

1. **The lone-human card's honest text** (F11): "honest text when no livelier seat is free". It tells Mia the truth,
   that there is nowhere to go, and changes nothing. A card that says *you are alone and will stay alone* is worse
   than no card.
2. **Jo in run 2.** Run 2 "adds Jo, a scripted solo human … seated by placement before row 1, whose factory lacks the
   energy Mia's kit makes".
   - That builds the one world where my §17b story works: a human with a free seat, who needs exactly what Mia makes.
   - It tests the mechanism, not the lonely player's normal case. The normal case is run 1: the first solo arrives
     after a crew, the crew's station is closed, and every kit makes energy.
   - With Jo, run 2 can pass while the real Mia stays alone.
3. **Tending that repairs storm damage, shown only then** (F9). It gives the tend a job, which is right. But it only
   exists after a storm, and it is still anonymous ("never who"). The absent friend's best moment on return, a name
   of someone who cared, stays cut.
4. **Visible life.** Nothing in the plan rebuilds the design's "Visible life without being online together": the daily
   line, lit windows, usual hours. The return card carries the crew's news, which helps the crew. A solo player
   between returns still sees only her own estate (mia d15-2100-0143-after-mine.jpg: her estate, a bell at 47 and
   no sign of anyone).

### The owner's expectation

The owner expected more redesign. For my half, that is right in principle and wrong in size.
- Two premises of my design were wrong: loneliness is not a seating problem, and a favour with no effect isn't a
  favour.
- One invariant was missing: nobody's action may lower your income.

Fixing them is a change of idea, but a small build, and it keeps every part that worked.

## What I'd change

Five changes. Two are new mechanisms (the porch, the "Today here" line). Three are rules or tests added to the fix
plan.

### 1. The porch: the small station next to a crew's station shares their place

**The rule.** In small mode (fewer than 50 active companies, `GameWorld.Tenants.cs`, `SmallModeBelow`), a small
station beside a corner station where a crew lives becomes that station's **porch**:
- its seats are open to strangers, even when the crew has closed its own four;
- it joins the corner station's Yard pool, project and board, as a crew's own small station already does
  (`BigStation`, `GameWorld.Crews.cs:504`);
- a stranger on the porch is a **resident, not crew**:
  - crew first in a shortage, then usual buyers, then new demand;
  - "not with" stays private;
  - the project's perk is theirs too;
- any active crew member can close the porch to *future* strangers, but nobody already living there is moved or cut
  off.

**What it replaces.**
- *In v1.2:* "a station a crew fills is closed to strangers" now covers only the crew's own seats. "A station of my
  own" stops being the solo default on a small server.
- *In the fix plan:* F11's honest "nowhere livelier" text is needed only when no crew station has a porch. "Find me
  neighbours" offers the liveliest porch first.

**What the player sees.** The picker's card: *"Capital N · the porch of Tea Time Pipes (Ana, Riverside Co., Cleo) ·
lively · shares their Yard and their Foundation"*. On the estate, the crew's pellets reach her Yard gate. Her colour
joins the project's ring.

**Mia in run 1 with the porch.**
- 20:03, *Find me neighbours*: her first Settle fails, because Ana's crew took Capital NW twenty seconds earlier. The
  picker reloads (F7) and offers the porch next door. Cleo and Ben moved in at 20:08, and she lands about then.
- At 20:12 her Wind Moilys' energy flows into the same Foundation as Ana's. Under F4 (900 energy) it stands at about
  20:12, with four colours in the ring, one of them a stranger's.
- That is §17b's "not alone" on evening one, without Jo and without trade. On day 8, "Today here" and dated chat tell
  her the crew is around. When her iron mine comes on day 15, the Water Tower (F4's recipe: goods residents make) can
  use it.

**Ana, Cleo, Dev.** They see "Mia Studio · lively" in their own Yard, not only in the Links tab (ana d27-2000 #258).
Ana greeted Mia in the chat on day 4 anyway; now she knows where Mia lives.

**Cost and risks.**
- **Cost:** S–M. `JoinedTo` for strangers on a porch in small mode, `SuggestSeat` preferring porches for Neighbours,
  and tests. That a stranger can't take more than the crew's leftover in a shortage, and that closing never cuts off a
  resident.
- **Risk:** a crew that doesn't want strangers near its project. Closing exists for that, so we measure how often it
  is used.
- **Risk:** a griefer on the porch. The honest Yard (F1) matches only real lacks, and usual buyers come before new
  demand in a shortage (§10), so nobody's supply falls overnight.
- **Risk:** strangers in a crew's pool were C's worry in round 3. With an honest Yard there is little to fight over.
  What remains is the project and the view, which is what C itself wanted strangers to share.

### 2. "Today here": the visible-life line, where people look

**The rule.** One line of public events from the player's district: moved in, a building finished, a project stage
stood, "back after N days", a House started buying from someone.
- It shows under the top bar on the first screen, as a section of F5's return card, and on the picker's station
  cards.
- Never trades, never gold, never "last seen".

**What it replaces.** The design's unbuilt notice-board line, lit windows and usual hours. Not the bell: the bell stays
the archive (the fix plan's event budget, §3).

**Mia.**
- Day 2: *"Today in Capital: Ana built an Iron Mine · Cleo upgraded 2 Solar Panels"*. It answers her diary on day 2:
  *"I didn't see them do anything"* (mia d01-2000, Other people).
- Day 15: *"Dev moved in · the Foundation stood"*.

**Ben, on day 36.** *"While you were away in Capital: Dev moved in · the Foundation stood · Mia Studio joined the
porch."*

**Cost and risks.**
- **Cost:** S–M. The events already exist (`WorldEvent` of kind Neighbour, Crew, Wonder). It needs a per-district
  public feed with a whitelist, de-duplicated (the "Windy Heights Co., Windy Heights Co." bug), at most three items.
- **Risk:** noise. Hence three items, public kinds only, and folded on a phone (Ben's first screen was already a pile).

### 3. The tend names who tended, and always has a job

**The rule.** F9's repair stays. The return card names the tenders, and only them: *"Ana and Cleo tended your estate on
9 days · 2 moilies repaired"*. Nobody who didn't tend is listed, so no one is shamed. With nothing to repair, the tend
carries the resting friend's spare to the station's project for that day, credited to him. That gives it a job every
day, not only after storms.

**Why.**
- F5 already shows "Ben is resting: tend 💛" to the crew. A tend that disappears between storms is found once and
  forgotten.
- The project is the one shared thing the friends can feed in week 1 (fix plan §1, cause 3). An absent friend who
  still helps build it is the best return line we have.

**No faucet.** The goods were Ben's own spare, already headed for the hub, and the project pays base as it does
today. Tenders gain nothing but the line.

**Cost:** S. Credit is one flag on the day's tend.

### 4. A veto rule I missed, written down and tested forever

**"Nobody's action lowers your income, present or away."** That is F1's floor and the "harm from a neighbour: none"
mark, made a standing rule of the design.

`YardFacts.SellingThroughTheYardNeverPaysLessThanTheHub` (r1-engineer E1) becomes a permanent test. A second test
checks it for a resting seller, since a sleeper is the one person who can't react.

**Cost:** none beyond F1.

### 5. Run 2 tests the lonely player in her real world too

Keep Jo. Add a second Mia in the same run who arrives as in run 1: after the crew, no human with a free seat
anywhere. She passes if:
- she is seated within 3 minutes;
- she shares a station's project or Yard with humans by day 1 20:30;
- her diary names a human who did something, by her second session;
- "alive" is ≥ 4 twice;
- she gets no "nowhere to go" card.

**Cost:** one more persona of the same budget (Mia played 4 sessions of 35–60 commands), about 60k tokens.

### What this doesn't change

The Yard, the one price (the bots decide 1.75 or 1.5), Houses and their cap, holds and stand-ins, the return card and
the project fixes all stay as the fix plan decides them. That is the point. The fix plan is right about the bugs and
the screens; it is wrong only in treating the lonely player as a placement problem it can test with a scripted
neighbour.

## What I'd keep no matter what

- **The way back.** Held in place, the stand-in, one-minute landing, "nothing lost". It is the best-performing part of
  v1.2 (ben d36-2000 #77).
- **Seat codes that wait for nobody.** Dev's day-8 arrival (dev d08-2000 #3–#8).
- **Nothing needs two people online.** No session in the run waited for another player.
- **No votes, no kicks, no releases.** Nobody was ever pushed out of a seat. Probation stays narrow, as built.
- **NPCs labelled ⚙ and capped.** The cap bounded the damage the dead House could do.
- **The floor (F1)** and **one return card (F5)**, as decided.
- **Tailwind for returners as for late friends.** Dev at ×1.8 went from Level 2 to 10 by day 21.
- **The district chat as everyone's room.** The cooperation designer's withdrawal of a crew channel was right: the
  crew's answers in the Capital chat were Mia's only warmth.

## Questions for the others

- **friends:** Would Ana's crew have welcomed Mia on the porch, sharing the Foundation's ring? Is "close the porch to
  future strangers" the right control, or should it need every active member, so one crew mate can't shut out a
  friend's acquaintance?
- **economy:** On a porch, with honest lacks, what share of Mia's output would the crew's pool really take at 1.75 and
  at 1.5? Does the project-routed tend change your faucet count (I say no: base pay, own spare)?
- **psychologist:** Is being *seen* (the "Today here" line, the ring) enough for a lonely player, or does she need to
  be *addressed*? Mia's warmest moment was a reply. Should the return card for a crewless player quote the reply, not
  only say who answered?
- **outsider:** Is the porch clear at a glance on the map and in the picker, a small station that shares a bigger one's
  Yard? Or does it need its own picture, a gate arch between them?
- **wild:** For the very first player on an empty server (no crew, nobody at all), what beats Houses that buy? I have
  nothing better than "Today here" with an empty district.
- **For the fix panel's engineer (final round):** is `JoinedTo` for strangers in small mode as cheap as I think, given
  that a crew's small station already joins its corner?
- **For the fix panel's researcher:** would you accept the no-Jo Mia as a second pass mark for §17b, so the lonely case
  can't pass on a world we built for it?

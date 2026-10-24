# Round B · psychologist

I read the five other round-A papers in full, the fix plan again, and the code behind the claims I use. Our
convergence is real on four points:
- the world layer held;
- F1, F2, F4 and F5 ship;
- seats must make what their names say;
- the price has to pay a buyer.

It is **not** real on the point that matters most to me. We say "friends need an act of giving", yet three of the five
proposals deliver the gift with no act at all.

## Answers to the questions put to me

**Wild: does a gift the giver didn't tap (Ben's kit piped to the Works) feel like giving once it's announced?** Not as
giving, but as **relief**.
- These are two different feelings:
  - **Relief** comes from the outcome. *"I didn't let Ana down"* needs only that sand arrived.
  - **Warm glow and closeness** come from the act: you chose, for someone.
- For Ben, the skimmer whose whole arc was guilt, relief was what mattered most. A zero-tap kit flow with one line
  (*"your sand reached Ana's Glassworks"*) would have ended his guilt on day 1.
- For Cleo and Dev it wouldn't have been enough. Both asked for a verb, *"how do I send"* (cleo d06, dev d10 #103).
- The run's evidence that an outcome without an act isn't felt: the Foundation bought Ana's energy from 20:07 on the
  first evening, and nobody noticed (findings, promise "Minute 12").
- **So design both:** the default flows by itself (relief, no obligation), and choosing a named taker is an act on top
  (meaning, for those who want it).

**Friends: does a visible friend line create obligation (Ben's guilt in a new form)? Is "sends", with no "stopped",
enough?** The line itself doesn't; the asking does.
- A standing line *lowers* obligation: once set, it keeps giving while you rest. That is exactly what Ben needed on days
  3–36.
- The risk is in what surrounds it:
  - **Repeated requests.** Pokémon Go's daily gifts and FoE's aid-back etiquette turned favours into chores.
  - **A visible end.** "Ben stopped sending" would be read as an accusation.
- "Sends, never stopped" is right. When a line ends, the receiver reads a neutral supply line: *"Your Glassworks gets
  its sand from the Yard now"*.
- Add one rule: a request reaches a given friend at most once per need, not per session, and folds after that.

**Lonely: is being seen enough, or does she need to be addressed? Should the return card quote the reply?** She needs
to be addressed. Being seen is the floor.
- Mia's only warm moment was a reply addressed to her: *"Finally got replies from Ana and Cleo – warm feeling"* (mia
  d08-2000 #105). Watching others act gave her nothing: *"I didn't see them do anything"* (d01).
- "Today here" is cheap ambient life, and I take it. But the return card should **quote the first words of a reply to
  her, with the day**: *"Ana, day 4: 'Hi Mia – welcome! Start with a wind moily…'"*.
- The strongest form is an act by a human aimed at her: a Thanks, a Send, an invitation (below).

**Economy: "goods from other land, not other people, and Houses always provide some". Is that line clear enough to avoid
the Habitica feeling?** No. Players don't separate the land from the person.
- In 37 messages the sand was always *Ben's* sand, never "Riverside's".
- The Habitica feeling needs one of two things: an absence that *takes something away*, or a group reward that depends
  on attendance. Your design avoids the first: the stand-in and the House keep the existing building running.
- But it builds a **ceiling on growth with a friend's name on it**: Houses sell "one building's need". Every time Ana
  wants a second Glassworks, the answer is "Ben". In a world where his guilt already filled three diaries over a sand
  promise that *nobody's buildings needed*, a real dependency puts more weight on him, not less.
- The line becomes clear only when the House is a real alternative for growth: a little dearer, never capped below what
  a friend would sell.

**Outsider: is a Sketch the right in-game form of their debugging, or would "visit and leave a note" do?** A Sketch.
- The help they gave was words about pipes that nobody could see. Dev had the right lesson on day 8 (*"I piped a moily
  into another moily and nothing flowed"*). Cleo's tips were wrong twice. Ana stayed stuck for 32 days.
- A note repeats the chat's failure. A Sketch *shows*, and the owner keeps the decision (one tap, own gold).
- Nobody visited a friend in 28 sessions, so the entry must come to the helper: the friend's stuck line on your own card
  (*"Ana's factory is stuck · look ›"*), not a visit menu.

## What I take from the others

- **Economy, the margin table and the light-profile bots.**
  - The table explains t = 0 better than anything else in the record: against a free ★2 patch, a bought input loses at
    any price above 1.0. At 1.75 the Smelter costs its buyer −8.75 g/min.
  - The bot gate is miscalibrated: bots with 300 buildings exhaust their patches and trade, while humans with ten never
    do.
  - I take *"run the crew bots with a light profile"*. Without it, run 2's price decision measures a player who doesn't
    exist.
- **Wild, "seats keep their promise".** Kits include the seat's own good (a Riverside dredger on its shore), even above
  the player's level. All five papers now want this, and it kills Ben's guilt at its source.
- **Wild, research for the crew.** A tech a crew mate finished costs 75% less. It answers Ana's *"could you do the
  research for me, or can points be sent?"* (group, day 21). It is a gift that costs the giver nothing and moves no
  points: helping, with no ledger.
- **Friends, Send ▸ on the building panel, as an ordering inside the matcher.**
  - It lists real takers ("Ana's Glassworks · 15/min", "the Water Tower · wants 5,400").
  - It is no new trade channel: a giver-chosen first place in the Yard's existing order. That is the cut "standing
    orders" (§19), handed to the giver.
  - It needs the in-station link refusal to go (`GameWorld.Links.cs` 313–315: *"lives in your station: you trade
    through the Yard already"*), which is exactly where Ana looked for Cleo (ana d27-2000 #258).
- **Friends and outsider, Sketch.** Crew only, the owner's tap, the owner's gold.
- **Lonely, three things.**
  - The rule *"nobody's action lowers your income, present or away"*, as a permanent test.
  - Tends that always have a job (repair, or carry the resting friend's spare to the project) and that **name the
    tenders**.
  - "Today here" as the floor of visible life.
- **Lonely and outsider, a Mia run without Jo.** Run 1's world (one closed crew, no free human seat) is the normal
  small server.
- **Outsider, the network you see is the network that flows, and the second-factory test.** Without it, Ana's second
  factory sticks again after F2's button.
- **Outsider, fewer words.** Players said *send*, *gate*, *neighbours*, and read "station" as a shipping tool (*"sand
  goes out via a Station (N)"*, Ana, day 14).

## What I attack

**1. The convergence on "an act of giving" is mostly words.** Read the mechanisms:
- **Outsider's pairs:** *"both gates are connected, sand reaches her Glassworks, and the toast tells both"*. No act.
- **Economy:** the land makes the trade necessary, and the honest Yard does it. No act.
- **Wild's Works:** Ben gives *"with zero taps"*. Only Send to adds an act, and it is secondary.

These are the fix plan's F6 with better content: an announced automatic flow. It will relieve guilt, and announcements
habituate fast. The run had a 93-unread bell and six toasts on Ben's first screen (ben d01-2000-0014). If the shared
idea of round C has no chosen act, we will have rebuilt the matcher with better goods in it.

**2. Wild's Works pays friendship in gold (80% of the value added).** It puts help back into market norms. Heyman and
Ariely found that a small payment makes people help *less* than a favour does. Here the payment is also worthless:
- banks were 25–33 days of income (economy, table 2);
- Cleo's *"no payoff for helping"* was said with 4.8M gold in the bank (d14).

A margin ledger is correct accounting and weak motivation. The part of the Works that is right is the *machine*: one
thing the station runs together, the feeling of OpenTTD's shared company. Keep the machine, drop the payout as the
pitch.

**3. Economy's "no copies of a neighbour's goods" is a hard dependency.**
- Aronson's jigsaw classroom (each pupil holds one piece) made strangers cooperate and like each other. It also needed
  backups for the pupil who is absent, because without them the group fails.
- Eco learned the same on quiet servers: forced specialisation stalls when the specialist is gone.
- The economy's backup (a House sells one building's need at 1.4) makes a friend's absence a **named ceiling**. It also
  breaks our sixth veto in a five-player world: a solo player's push to "move to Jo at Capital E" exists exactly where
  there may be no Jo.
- **Soften it:** a friend is the cheapest and named supplier, a House is always there for growth at a slightly worse
  price, and the ★2 patch question goes to the light-profile bots.

**4. Lonely's porch is right in substance and wrong as a default.**
- Mia lived next door for 36 days with nothing joining her to the crew (Capital N against NW, `WorldLayout.cs`). That
  must change.
- But an automatic porch puts a stranger in the friends' project ring without anyone choosing. A shared address isn't
  friendship (FFXIV wards), and a crew that finds a stranger in "our" Foundation may feel intruded on.
- **Turn it into an act:** one card to the crew, *"Mia lives next door, alone · share your Yard with her?"*, and one tap
  by any member opens it. Friends offered the same as an invite.
- **The welcome is itself a gift.** Mia's card then reads *"Ana shared Tea Time Pipes' Yard with you"*: addressed,
  named, chosen. If nobody taps within 3 days, the porch opens anyway in small mode, so the lonely player never waits
  on a crew's kindness.

**5. Wild's "hands": a helper laying pipes on a friend's land.** Up to 100 g a day, undoable for 24 h.
- It breaks the ownership that makes a builder's estate *yours* (the IKEA effect).
- It is the Factorio permission problem we closed in round 2.
- A Sketch does the same job with the owner's tap.

**6. The fix plan's success measure.** As in round A: *"first good knowingly given: named by the giver"* will be met by
kind AI players quoting the announcement in their diaries. Count **acts**: Sends opened, Sketches built, Thanks given,
invites tapped. Read the diaries for *"I sent"* and *"X needed my…"*, not for *"my sand reached…"*.

## The directions

**A · "Land that needs neighbours"** (economy, outsider's pairs, wild's truthful kits)
- **Core:** structural interdependence. Land goods are dealt per station with no copies, kits make the seat's good, and
  one price pays both sides (1.3). The honest Yard carries the trade, and F6 shows it.
- **Replaces:** the ★2 patches, the district shuffle, the price vote and the conditional kit lever.
- **Biggest risk:** dependency without an act. Friends need each other but never *do* anything for each other, so
  absence becomes a named ceiling. Together on paper, side by side in feeling.

**B · "Give on purpose"** (friends, psychologist, wild's Send to, outsider's and friends' Sketch, lonely's tends)
- **Core:** verbs with names:
  - Send ▸ to a chosen taker;
  - bids from the game that name the friend who can;
  - Sketch;
  - crew research;
  - tends that do a job.

  It sits on truthful kits and soft needs (one consumer per kit; a House always there).
- **Replaces:** the in-station link ban, F6's announcement-only gifts, the two run-2 triggers, the do-nothing tend and
  the Plan board.
- **Biggest risk:** obligation and habituation. Bids can read as debts, and a standing Send fades into plumbing within
  days. AI players may also use buttons more than people do.

**C · "One machine we run"** (wild's Works, lonely's porch)
- **Core:** the project becomes a running station factory everyone feeds, with a margin split and a porch for
  strangers.
- **Replaces:** F4's baskets and bonuses, and the closed-station rule in small mode.
- **Biggest risk:** individual effort disappears in a pool (Ringelmann's social loafing) and is paid in a currency
  nobody values. The accounting and faucet checks are also new.

**I'd champion B, absorbing A's truthful kits and fair price, and C's machine as the thing you Send to.**
- B is the only direction that gives players the act all four asked for in their own words.
- It works with A's content.
- It keeps the veto rules, because nothing *requires* the act: the default flow relieves, the act means.

## My revised proposal

Five rules, on top of F0–F5 and F7–F11:

1. **Truthful kits with one need each.**
   - Every seat's kit makes the seat's good.
   - Every kit has one consumer that a neighbour's kit can feed, with a House always selling the input a little dearer
     (no cap below a friend's volume).
   - The light-profile crew bots set the price (1.3–1.5) and decide the ★2 patches.
2. **The default flows, once announced.** The honest Yard sends the kit good to the friend's kit consumer by itself.
   Both screens get one line: *"Ben's sand reached Ana's Glassworks"*. This is relief, not yet giving.
3. **The act: Send ▸.**
   - On a producer's panel, the giver names a taker: a friend's building, the project's machine or Mia next door. The
     good goes there first, through the Yard (the in-station link refusal goes).
   - The receiver sees *"💛 Ben sends you sand"* and a Thanks.
   - Against habituation, no daily toasts. Instead:
     - moments tied to the friend's progress (*"Ana's Glassworks made its 1,000th glass on your sand"*);
     - a quiet cumulative line on the panel (*"Ben's sand · 21 days · 30k"*);
     - new needs posted as bids when the friend builds something new: once per need, naming who can, never
       "unanswered".
4. **Help that shows: a Sketch from the friend's stuck line; crew research at −75%; tends with a job that name the
   tenders.**
5. **Strangers are welcomed, not placed.**
   - The crew gets one card to share its Yard with the lone neighbour, and the porch opens by itself after 3 days if
     nobody taps.
   - Mia's return card quotes the reply addressed to her and names the welcome.
   - "Today here" is the floor.

**When a friend stops:**
- his Sends keep running while he rests;
- at the stand-in the line stays *his* (*"Granny Ida ⚙ keeps Ben's sand coming until he's back"*);
- nobody ever reads "stopped";
- his return card carries *"your sand kept Ana's Glassworks running for a month"*. Someone needed you, and nothing was
  lost.

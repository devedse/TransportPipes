# Round 1 · together

My seat: playing together. Helping a friend must be possible, visible and rewarding, through pipes (principle 5),
working while the other is away (6), and safe (7).

## What I see

1. **The friends wanted to give, and the game had no way to say how.** Ana asked for sand at minute 1 (group, day 1
   20:04). Ben promised it four times. Cleo offered copper and was still "looking at stations" a week later (group,
   day 6 14:17 and day 14 19:08). Dev asked three times: *"How do I actually send sand to you? Station?"* (day 10 20:09,
   day 14 19:11, day 21 19:16). Ana: *"Does the Yard hand them to me automatically if I just ask for them?"* (day 27).
   Result in 36 days: 0 Asks, 0 links, 0 tends, 0 pins, and no good knowingly given.

2. **Cooperation lives where nobody looked.** The Yard panel was first opened on day 14 (cleo d14-1900 #234), by
   Ana and Dev on day 21 (ana #239, dev #169), never by Ben or Mia. Nobody clicked the crew chip, Ask, Tend, "I make
   it" or the bell (actions.jsonl). Where they did look: building panels (68 map clicks), the in-game chat (39 reads and
   sends), Loady's and the next steps' "Show me" (17), and the return dialogs (51 clicks). The Ask form sits below the
   Plan board at the bottom of that panel, off the screen (cleo d14-1900-0236-yard.jpg).

3. **The one trade the Yard made cost the friends money.** Cleo's maxed Lab drew 361 energy/min from Ana, Dev and Ben
   for eight days, took none of it and paid nobody. Ben earned 0.3 g/min while resting. The station's real income
   doubled (614 → 1,241 g/min) when she demolished it.

4. **The friends' plans meant nothing to the machine.** The Yard only matches what *buildings* lack. No building in
   the station used sand or copper ingots, so "bring Ana sand" had no buyer. On day 21 Ana wanted Cleo's spare 6.1 iron
   ingots for a Lab (ana d21-1900 #241). Nothing says "build the Lab on your gate network and they come by themselves".

5. **The gate is the only door, and nobody learned it is a door.** The kits arrive piped to it. Dev's dredger sat on
   its own network: *"I guess my sand has to reach the Yard gate first, which is far away"* (dev d21-1900 #192). The
   Water Tower wanted that sand all along.

6. **The shared project never moved and nobody saw it.** The Foundation took 42 hours. The Water Tower sat at 0 /
   91.5k sand, 0 / 73.2k wood, 0 / 73.2k stone from day 3 to day 36, its tab first opened on day 21. Cleo: *"no figure
   for what 3% is in g/min"* (cleo d21-1900 #303).

7. **The absent friend never reached Ana.** No tend in 36 days. The stand-in notice went to a bell with 93 unread,
   naming "Riverside Co.", which Ana never linked to Ben (ana d21-1900 #240).

8. **Mia was alone in play.** She landed among Houses in Capital N, next door to the crew. No lone-human card came.
   The crew's Links tab offered her as their only partner all month; nobody offered. "Alive": 3 of 5 every session.

## Root causes

Checked in the code unless marked as a guess.

**A. The Yard reserves what a buyer can't use, and pays nobody for it.** `Measure` (GameWorld.Yards.cs:341–366)
counts a building's scarcest inputs as lacking. The solver hands a co-limited building the same fraction of every
input, so the Lab's energy and ingots both read as "lacking" at 3%. The Yard fills the plentiful one (energy). The Lab
still can't run faster (ingots), so it takes nothing from the gate. The seller's own pay for what it sent to the gate is
taken away (Yards.cs:131–134), and it is paid back only for what the buyer took (Yards.cs:216–218). The rest vanishes.
The next round doesn't send less, because the damping looks only at what the seller sent, never at what the buyer took
(Yards.cs:562).

**B. Trade has no face.** The building panel's "Where it goes" lists only the solver's flows
(GameViews.cs:715–726). Gate flows are split out of them (Yards.cs:125–150), so a building never shows that it sells to
a friend. The top bar's income is the networks' `GoldPerMinute` (GameHub.cs:135), which leaves out the Yard's, Asks' and
the project's pay. The design's first-screen line *"Your energy now sells to Dev's mine at 1.75"* (§5) was never built.
The design's own trigger has fired: §16 decision 3 and §21 say "show explicit pairs" when players can't name their
buyers. Before day 14, nobody could.

**C. The player's verbs are hidden and nothing points to them.** Asks, pins and tending exist (GameWorld.Asks.cs,
Return.cs:402–433) but live in the Station panel and behind the crew chip. Loady's eleven first steps contain one Yard
step and no Ask, project or tend step (tutorial.yaml:13). The next steps' only together card, `PartnerStep`, fires only
when *nothing* of yours reaches the gate (GameWorld.Advisor.cs:966–969). Every kit holder had a gate network, so it
never fired for Dev's dredger.

**D. The kit took away the one lesson.** `HandOverKit` pipes the kit to the gate (GameWorld.Tenants.cs:437–442).
`Handshakes` then sets the `yard` stat (Yards.cs:684–694), and Loady's `firstYard` step completes itself. The design said
*"You lay your own pipe to it: that's your consent"* (§7). Kit holders never laid one.

**E. The project asks for what the station can't bring.** Recipes come from the seats' land types (`HomeGoods`,
Projects.cs:156–180), not from what residents make or can build. The basket is 6 h × T(L) per delivering resident
(Projects.cs:252). The Water Tower was sized for one person, Ana at Level 6: 6 × 60 × T(6) = 329k gold, a third per
good, so 91.5k sand at 1.2 g. That is about 27 hours of Ana's real income (about 200 g/min), in goods nobody had on a
gate. "Nobody here makes it" counts any building (`StationMakes`, Projects.cs:477–483), so from day 10 sand no longer
read as missing, though none could reach the gate. A guess: a Storage on a gate network hides its good from the Yard
until it is full, since `Measure` counts only hub sales and idle capacity as spare (Yards.cs:307, 343).

**F. News about the absent friend is a bell item, under a company name.** There is no "Ben is resting" message at all.
There is only the day-23 stand-in notice to partners (StandIns.cs:146–153) and "is back" (Moving.cs:528), both events.
The return card has no section about friends (ReturnDialog.tsx). Tending has no effect beyond the counter
(Return.cs:412–433).

**G. Mia's card and links can't work on a server like this one.** The lone-human card needs a livelier station with a
free seat (Return.cs:208, 247–252). The only humans held all four seats of Capital NW, so the card never came. Link offers
lapse after 72 h (Links.cs:96, 551), and Mia played 6–7 days apart. `LinkSuggestions` uses the same `Measure`, so the
crew's suggestion "Mia has spare energy you lack" was fed by the Lab's false lack.

**Why the friends never gave each other anything, in one line:** they thought in "send X to Ana", and the game only
knows "a building on your gate network lacks X". The bridge between the two (the giver's door, the receiver's
building, a want both can see) was missing, and the one automatic trade lost money.

## What I propose

Ordered by what to build first. Each step is playable alone.

**1. The Yard never costs a seller anything.** S/M/L: M · owner: engineer, with economy.
- Rule: *a seller earns at least what the hub would have paid for every unit that leaves its gate.*
- A lack counts only up to the run rate the building's other inputs allow, counting what the Yard has spare of them
  this round. Ana's factory, short of both energy and ore, still gets both.
- The damping looks at the buyer too. Next round, a buyer is matched at most what it took last round, plus 10% and a
  small probe.
- What a buyer doesn't take is paid to the seller at the seller's own hub price. That is no new gold: the hub would
  have bought it.
- Risk: the matching undershoots for a while when two inputs both come from the Yard. The probe covers it.
- Verify:
  - `YardFacts.A neighbour's maxed Lab never lowers my gold rate`: A's gold over a day with and without B's Lab differs
    by under 1%.
  - Replay row07: Cleo presses Max; Ana's, Ben's and Dev's gold rates stay where they were.

**2. Every trade shows two named ends, where players look.** M.
- Building panel, "Where it goes", seller side: `⚡ to the Yard → Dev's Sand Dredger · 8/min · 1.75 g · +14 g/min`.
- Buyer side: `⚡ from the Yard ← Ana, Cleo · 8/min · 1.75 g`.
- The project: `∴ to the Water Tower · 15/min · 1.20 g`.
- Top bar: the income includes the Yard's, Asks' and the project's pay. The hover splits it:
  `hub 150 · neighbours 46 · Water Tower 18`.
- One event per pair and good, the first time it flows, on both first screens:
  *"🤝 Your energy now runs Dev's Sand Dredger: 8/min at 1.75 g (the hub pays you 1.00)."* This is the §5 line.
- On the receiver's "from" line, a **💛 Thanks** tap, at most once a day per pair. The giver reads *"Ana thanked you
  for your iron ingots 💛"* at their next session. No gold or XP, unlike likes.
- Return card: *"Your friends' buildings took 41k of your energy (+72k g)."*
- Risk: noise. Show only flows above 1/min.
- Verify: extend `e2e/yard.mjs` to read both panels. In the next run's diaries, at least 3 of 4 friends can name
  who buys their goods.

**3. The giver's door and the receiver's building, in the panels they open.** M.
- On a producer's panel, only when a real taker exists:

      Who could use your sand
        🏗 The Water Tower wants 91.5k · pays 1.20 g · +3% for every seat when it stands
        🙋 Ana asks for 500 · pays 2.10 g
        ⚠ This building's pipes don't reach your Yard gate (G), so none of it can go there.
           [Plan the pipe to my Yard gate · 26 tiles · 260 g]

  Once it is connected: *"✓ It goes there by itself: the Water Tower takes 15/min."* The planner starts from this
  building's network, not from any pipe.
- The next steps get two together cards, one per network, not per estate:
  - Giver: *"Your sand can build the Water Tower: pipe your Sand Dredger to your Yard gate (26 tiles, 260 g)."*
  - Receiver: *"Cleo has 6 iron ingots a minute spare. A Lab piped to your Yard gate gets them at 1.75 g by itself: no
    Ask needed. [Show me]"*
- A friend's Ask shows in the giver's "Who could use" and next steps. The Ask form says when it can't work: *"Asks
  fill your storages: build a Storage first (1,000 g)."*
- Risk: one more block on a long panel. Show it only when a taker exists.
- Verify: replay row10. Dev's dredger panel offers the plan, he connects, and the Water Tower's sand rises within
  the session.

**4. Loady's Yard step is the player's own.** S.
- Keep the kit piped to the gate. The first-evening Handshakes (20:08) were the one moment that felt like being
  neighbours, so they stay early.
- `firstYard` counts a building the player built, on a network that reaches the gate (a `gate:` variant of `piped:`,
  kit excluded).
- Text: *"Your Yard gate (G) is your door to the neighbours: what reaches it by pipe goes to their buildings when
  they lack it. Pipe your new Wind Moily to it."*
- Verify: `TutorialFacts.The kit does not do the Yard step`.

**5. The project the crew can see and reach.** M · owner: economy for the numbers.
- **The Foundation's basket is energy only** (600 instead of 600 energy and 150 iron ore, Projects.cs:83). Every kit
  makes energy, so it stands on the first evening.
- **Recipes come from what residents can bring.** A good qualifies when a resident makes it now, or can make it with
  one building they have unlocked (or unlock within two levels) that fits their unlocked land. The keystone is one of
  these that nobody makes yet, never one that nobody could make. The card names who can: *"Stone: nobody brings it
  yet. Ben and Cleo have stone on their land: a Quarry (Level 10 · 3,000 g)."* That is the crew's decision the design
  wanted ("who builds the glass chain?").
- **Size:** the economy designer sets the formula. My requirement: the delivering residents can finish a stage within
  about three days of their *real* spare. The card says the pace: *"at this pace: about 2 days."*
- **On the maker's own view,** "made here" counts only goods that reach a gate: *"You make sand, but it doesn't reach
  your Yard gate. [Plan the pipe]"*
- **Shown:**
  - a top-bar chip next to the crew's: `🏗 Water Tower · 12% · wants stone`;
  - the cards of proposal 3;
  - a return card line;
  - the bonus in gold: *"When it stands: +3% for every seat, about +24 g/min for you and +37 for the station."*
- Risk: a recipe from current buildings is less varied. The keystone keeps one new chain per stage.
- Verify: `ProjectFacts.The Foundation stands from one starter kit within an hour` and `ProjectFacts.Every recipe good
  is makeable by a resident`. In replays, the Water Tower stands by day 14.

**6. Friends by their names.** S–M.
- The invite dialog asks *"Who is this seat for?"* The buttons become "Copy Ben's link", which also fixes the
  identical names, and the message reads "Ben, a seat next to me in Tea Time Pipes".
- The move-in screen reads *"Ana kept this seat for Ben"*. The company name is prefilled "Ben's Riverside".
- Crew mates show name first: `Ben · Riverside Co.`, in Yard rows, notices, chat and the crew panel. Others see the
  company name only.
- Verify: the stand-in notice on day 27 names Ben.

**7. The absent friend comes to Ana, and tending does a real chore.** M.
- From day 3 of a crew mate's absence:
  - the return card gets a "Your crew" block;
  - the next steps get one card a day (gone once you've tended);
  - the Yard row gets a 💛 Tend button;
  - the crew chip gets a badge.

      Your crew while you were away
        🎈 Dev moved in next door (Windy Heights)
        💤 Ben is resting (4 days): his estate runs and earns in full.   [💛 Tend]
        ⚙ From day 30 Granny Ida ⚙ keeps your trades with Ben going; his seat is kept to day 93.
        🏗 The Foundation stands: +3% for every seat.

- Tending repairs the friend's storm damage, for free. From day 14, the days it covers carry no caretaker's fee.
- Ben reads: *"Your crew tended your estate on 9 days: 2 Wind Moilys repaired, no caretaker's fee on those days (52k
  kept)."* It is still never who, gives the tender no stat, and moves no gold between players.
- Risk: an alt tending a main. The gain is at most the 10% fee of an estate away 14–30 days, never more than playing.
- Verify: replay row14 from the row10 snapshot with Ana tending on her sessions. Ben's return names the days.

**8. A crew channel.** M.
- A chat channel `🫖 Tea Time Pipes` for members, opened by default for crew members.
- Messages older than today show a day ("Tue 21:19").
- The game posts at most three lines a day there: moved in, Handshake, a stage built, resting, the stand-in, back, the
  first trade between two friends.
- It puts the news of proposal 7 where Ana already wrote (ana d36-2000 #278).
- Verify: on day 27 Ana's chat shows Ben's stand-in line.

**9. Mia: not alone in play.** S each.
- **(a)** The picker fix (the engineer's).
- **(b)** A link offer waits until 24 h after the other side's next session (at most 14 days), not 72 h. It shows on
  their return card and in their next steps.
- **(c)** When no livelier station has a free seat, the lone-human card still comes: *"Nobody else plays in Capital N.
  Tea Time Pipes plays next door: a link to them is 18 tiles."* Below it goes the best link suggestion on both sides,
  once proposal 1 has fixed the lacks it reads.
- **(d)** Her trades with the Houses show on her panels (proposal 2).
- Verify: replay Mia's rows from row08. She has a link with a human by day 15, and her "alive" score reaches 4.

**What these do to the §17 promises:**

| Row | Promise | Proposals |
| --- | --- | --- |
| 1 | The Foundation stands | 5 |
| 3 | What the friends did | 2, 7 |
| 4 | The first Ask or link | 3, 5 |
| 5 | Ben's estate earns in full | 1 |
| 6 | Ana tends Ben | 7 |
| 7 | Helping pays | 1, 2 |
| 8–9 | Dev brings what was lacking | 3, 5 |
| 13 | The stand-in is clear | 6, 7, 8 |
| 14 | "Tended on N days" | 7 |
| 1, 8, 11 | Mia | 9 |

## What I'd not do

- **No "send to a friend" button** that moves goods or gold without a pipe. It breaks principle 5, opens a funnel,
  and is pointless: Cleo's copper would only be resold to the hub at a worse price.
- **No manual Yard** (choosing buyers, approving trades). The automatic match is what lets an idle partner never stall
  you. It needs a fix and a face, not replacing.
- **No rewards for giving** (gold or XP for tends, thanks or Asks): farmable. The reward is the price (1.75 against
  the hub's ≤ 1.0), shown, and the thanks.
- **No research points between players, no crew wallet.** Both are funnels. A research start is the economy's call.
- **No buying the project with gold** to soak up the glut. The project stays pipes.
- **No stranger in a crew's closed Yard pool** to cure Mia. It overrides the crew's choice (§6). Make links work.
- **Nothing more on the Station panel.** Nobody opened it before day 14. The automatic spare/lacks rows already are
  the Plan board.
- **Keep the one price, no votes, Houses labelled and capped, "never who".** The run gives no evidence against them.

## Questions for others

- **Engineer:**
  - Is the "pay what the buyer doesn't take at the seller's hub price" settlement simple in `SettleGateFlows`?
  - Does a Storage on a gate network hide its good from the Yard until it is full? (My guess from `Measure`.)
  - Were Mia's 33 g/min of House sales really paid?
  - Can the pipe planner start from one building's network?
- **Economy:**
  - Which basket formula lets the real crew finish a stage in about three days? T(L) is several times their real
    income: Ana 213 g/min at Level 14 against T(14) ≈ 2,050.
  - Is +3% enough of a reward once it is shown in gold?
  - Is waiving the caretaker's fee on tended days an acceptable faucet?
- **Clarity:** where does the "Your crew" block go on your single return card? I'd put it first, after the duration.
  Can the top bar hold the project chip on a phone?
- **Researcher:**
  - Which measure would you take as "the first good knowingly given"? I propose a paid trade between two friends that
    both of them saw on a panel or event.
  - Is the Thanks tap too small to matter, or exactly the right size?

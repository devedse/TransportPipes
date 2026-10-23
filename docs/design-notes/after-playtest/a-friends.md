# Round A · friends

I designed v1.2 around Ana, Ben, Cleo and Dev, so story1 was my own test case. My verdict: the way in held, and the
friends behaved exactly as the design hoped. In the first five minutes they gave themselves roles and made promises
to each other. Then the game gave them no way to keep those promises. They wanted to *send* each other things. Every
cooperative flow we built was automatic, and none had anything to carry. The fix plan makes the automatic flows honest
and visible. It does not give the friends an act or a need, so the owner is right: one part of the design needs
redesigning. That part is what friends *do* for each other, not where they live.

## What held, what failed

**Held: getting in and coming back.**
- Ana founded "Tea Time Pipes" at 20:03 and sent three seat links (ana d01-2000 #14–#24).
- Ben and Cleo moved in with two taps, and the Handshakes played at 20:08 ("The way in works").
- Dev opened a day-1 code on day 8 with nobody online and was on his estate in a minute (dev d08-2000 #3–#8).
- Ben came back after a month in 42 seconds, to *"a month away and nothing lost"* (ben d36-2000 #72–#79).
- The held seat, the stand-in (Granny Ida ⚙, events day 34) and Tailwind (×1.8, dev #45) all worked.

**Held socially, failed in the game: "choosing a seat is choosing a role".** This was my round-1 idea 2 ("Ben takes
coal"). The players did exactly what I hoped, from the seat names alone:
- Ana, day 1 20:05: *"I'll start with the energy, can someone bring sand? 😊"*
- Cleo, 20:12: *"I can spare copper later – Ana, what do you need besides sand?"*
- Ben, 20:14: *"ana i have sand on my riverside seat"*.

The game never delivered the roles.

| What failed | Evidence | Class |
| --- | --- | --- |
| Every starter kit was energy | A kit keeps only the House's producers "that need nothing" in the start area (`GameWorld.Tenants.cs` 382–389). It is then topped up with the seat's plan only if the newcomer has unlocked it, "else windMoily" (448–452). At Level 1 that means energy for everyone: 3 Wind Moilys or 4 Solar Panels (r1-researcher, "The friends had nothing to trade") | (b) The design said "3–5 buildings at level ≤ 5" and never asked what they make |
| The seat's own good was out of reach | Specialties go into random outer parcels (`WorldGenerator.cs` 210–216: `OuterParcels`, shuffled), not the "first two parcels you can buy" of design §14. Ben: *"only found stone here"* (group, day 3). Dev's sand needed Level 5 and a 5,000 g shore parcel (dev d10-2000 #81–#85) | (a) for the parcels. (b) for the decision itself: my round-3 spec put the ★5 home good next to the HQ, and the merge chose rich goods behind a purchase instead |
| The station wasn't complementary | Capital NW had two energy seats, Sunny Flats (Cleo) and Windy Heights (Dev) (group, day 1 20:05) | (a) or (b); I couldn't find the complementary-seat rule in the generator |

So Ana's request was "a story, not a need" (r1-researcher). No building of hers used sand. Ben's motivation was his
promise: *"yes, ana needs sand and I have it"* (ben d01-2000 diary). It turned into guilt in three of his five diaries,
still there on day 36: *"still guilty about sand"*. Our guilt rules held inside the game: no list of who didn't
deliver, no multiplier. Ben felt guilty anyway, because the seat card made him a promise that the land couldn't keep.

**Failed: "friends help each other through the Yard, by themselves".**

1. **Built differently (a).**
   - The design said a seller is paid "for what reaches the Yard" (§7). The code pays only what the buyer takes, and
     `Measure` counts every input of a slowed building as lacking (r1-engineer, root causes: `Yards.cs` 133, 216,
     363–365).
   - The design's first-screen line *"Your energy now sells to Dev's mine at 1.75"* (§5) was never built (r1-together,
     root cause B).
   - Result: the one Yard trade drained Ana, Ben and Dev of about 3.5M gold over eight days and paid nobody (r1-economy,
     What I see 1).
2. **Built as designed, and the design was wrong (b): no act of giving exists.**
   - The Yard matches spare to building lacks automatically.
   - Asks are the *receiver's* verb, filled from neighbours' spare without the giver choosing: "their spare goes to it
     first … into the asker's storages" (`GameWorld.Asks.cs` 6–9, `MatchAsks` 177–203).
   - The Plan board is pins. Standing orders ("my spare coal goes to Cleo first", my round-1 idea 8) were cut (§19:
     "standing orders (an Ask renews itself instead)").
   - So the giver's only act in v1.2 is laying one pipe to the gate, and the starter kit does even that (r1-together,
     root cause D).

   The friends asked for a giving act again and again:
   - Dev: *"How do I actually send sand to you? Station?"* (group, day 10).
   - Dev: *"Now how do we get it to you – Yard gate?"* (day 21).
   - Cleo: *"I don't know how to send her copper (station?)"* (cleo d06-1400 diary).
   - Ana: *"Does the Yard hand them to me automatically if I just ask for them?"* (day 27).
   - Cleo: *"Helping costs me nothing but I still see no payoff for helping"* (cleo d10-2000).

   Our own appendix said "Helping feels better than being helped". We then built verbs for being helped and none for
   helping.
3. **The design's own trigger fired.** Decision 3 and §21 say "show explicit pairs" if players can't name their
   buyers. Before day 14 nobody could (r1-together, root cause B).

**Failed (b): "friends make you better" doesn't hold at 1.75.**
- At the one player price, buying a neighbour's input earns the buyer +1 g/min on a Small Factory and −1.25 on a
  Smelter. Copper can't cross estates, and solo play comes out at about 106% of crewed (r3-economy, "My last
  dissent").
- I insisted on a fixed crew price of 1.75 in rounds 3 and 4 (r4-defense-C, R11). The arithmetic says I was wrong:
  the price decides whether a friend has any reason to build for your goods.

**Failed (b): something to decide together.**
- The Water Tower wanted 91.5k sand, 73.2k wood and 73.2k stone. That is 6 h × T(6) for one resident, against these
  players' real income, which was 3–15 times lower (r1-engineer, What I see 3). Its recipe came from the seats' land,
  not from what anyone made.
- The Foundation wanted iron ore, which no kit makes, so it stood after 42 hours ("The station's project was stuck").
- Still, the project was the right idea. Seeing it gave the run's only "together 4" (ana d27-2000), and it became
  Ana's whole plan for the last evening (group, day 36 20:09).

**Failed: the small acts of care.**
- Tending (row 6) was never found. It lives behind the crew chip, and the chip was clipped off the screen
  (playtest-fixes §1, cause 4). It also does nothing (r1-researcher, "Where I differ" 7). That is partly (a), where it
  was put, and partly (b), what it is.
- The care was real, but it went elsewhere: *"No pressure Ben – I'll look at your map trouble tonight if you tell me
  what the screen says"* (Ana, day 2). Cleo tried to research Copper Pipes *for* Ana and asked to send points (group,
  day 21). Dev passed on *"producers aren't junctions"* on day 8 (group, day 8 20:20).
- The crew's longest conversation was a help desk for Ana's idle factory, in words, by people who couldn't see her
  pipes. Cleo's two tips were wrong (*"the trading post sells first, I think"*, day 10).
- In 28 sessions nobody visited a friend's estate: the digest has no visit, like or guestbook. Visiting exists (M4);
  nothing led there.

**"The late friend is the missing piece" (my idea 10): half held.** Dev arrived well. But he brought nothing for 28
days:
- his seat's sand needed Level 5, a parcel and a dead-end fix (dev d14-1900 #130, STUCK #132);
- it then filled a Storage and stopped (dev d21-1900 #185, "full by day 22").

That is (b) for the kit and the parcel, (a)/(b) for the dead end.

**Mia.**
- The seat picker loop is (a), a bug.
- Being alone was (b) for small mode. The crew held all four seats of the only human station, so no placement could
  seat her by a human (r1-researcher, "Where I differ" 4).
- She ended up in Capital N, the small station right next to the crew's Capital NW (`WorldLayout.cs` 217–221). That
  is the very seat the design lets a crew join to its own Yard (`GameWorld.Crews.cs` 482–500).
- Cleo answered her on day 2, Ana on day 4 and Dev on day 21 (mia d08-2000 #104). Ana mentioned her to the group. The
  crew's Links tab offered her all month, and nobody made her an offer.

**(c) Not reached:** the Foundry Hall, trunks, the Landmark, Wonders, a falling-out, a group of seven, the
consolidation card. No evidence either way.

## Is the fix plan enough?

**Where it repairs a cause.**
- **F1:** the Yard's matching and pay (a bug).
- **F2:** dead ends explain themselves. The rule stays, now explained.
- **F4:** baskets sized from measured income, recipes from what residents make, no 18-hour tail. That fixes two design
  errors of ours.
- **F5:** one return card.
- **F7:** the picker.
- **F8:** smart Max.
- **F9:** friends by name. Ana never knew "Riverside Co." was Ben.

I accept all of these as they are. They are necessary, and I wouldn't touch their numbers.

**Where it patches the symptom of a design flaw.**

1. **The act of giving.**
   - F6 announces the automatic flows (*"∴ Your sand reached the Water Tower"*) and adds a Thanks tap. That is a face,
     not a choice. The giver still decides nothing.
   - Run 2's main pass mark redefines "the first good knowingly given" as a paid flow "shown to the giver, and named
     by the giver" (playtest-fixes §6). That is knowingly *noticed*.
   - The plan dropped "a 'send to a friend' button" (§4, Dropped). It moves "giving needs an act" to a run-2 trigger
     ("nobody knowingly gives by day 8", §7).
   - Run 1 already answered that question: Dev asked three times, sand was in 24 of 37 group messages
     (r1-researcher), and three friends asked "how do I send".
   - Waiting for run 2 to fail the same way spends about 1.3M tokens to learn what the group chat says plainly.
2. **Something to give.**
   - The plan states the cause itself: "Every starter kit makes energy, so no friend needed what a friend made"
     (playtest-fixes §1, cause 3). Its answer for week 1 is the project as "the only honest gift" (r3-together,
     dissent).
   - The real lever, "seat kits get one consumer of a neighbour's specialty", waits as a trigger for t < 15%
     (playtest-fixes §7).
   - F6's seat card will now say "wind kit · sand: a dredger on the shore, Level 5". That makes the broken promise
     honest instead of keeping it. Ben would read that he has no sand yet, and he stops on day 3, before Level 5 and the
     5,000 g parcel. His role would come after he has gone.
3. **Interdependence at 1.75.** F6's neighbour-fed chain steps "will offer almost nothing in Era 1 except the
   Glassworks" (r3-economy, F6 dissent). The plan leaves the price to the bots. That part is fine. But without
   different kits and a giving act, either price trades little.
4. **Care.** "Tend shows only when it repairs" honestly removes an empty ritual. It leaves the friends with no caring
   act on most days, and none for the help they actually tried to give: diagnosing and fixing each other's estates.

**In one line:** after the fix plan, the friends will see an honest scoreboard of four people all giving energy to one
tower. That is still side by side, only better lit.

## What I'd change

**Make Ana's minute-one plan true.** Three changes on top of the fix plan, one optional fourth, all in the pipes.

### 1 · Seats that make what their names say, from the first minute

**What changes (new estates only; a fresh world is allowed):**
- **The land.** The seat's primary good lies in the start area beside the HQ: a ★5 deposit of 3×3, the shore, the
  forest or the hills. This is my round-3 decision 10, which the merge replaced.
- **The kit.** The House leaves the seat's own producer of that good, plus the energy to run it, already piped:
  - Riverside: a Sand Dredger on the shore and 2 moilies;
  - Copper Gulch: a Copper Mine and 2 moilies;
  - Coal Valley: a Coal Pit;
  - Old Forest: a Lumber Camp;
  - Windy Heights and Sunny Flats: energy, as now.

  A kit building may be above the player's level ("Ten Marsh ⚙ left you its Sand Dredger; you can build more at
  Level 5"). The House's own kit buildings are handed over without an unlock check today (`Tenants.cs` 382–389); only
  the top-up checks unlocks (448–452).
- **The station.** Its seats get four different specialties: never two energy seats.

**Income check.**
- A dredger kit makes 15 sand (18 g/min at base) plus two moilies, minus its 8 energy: about 70 g/min, against 90 for
  three moilies.
- A copper kit makes about 83 g/min.
- A third moily evens either out. `TheFirstDay` must keep Level 10 by day 2.

**What it replaces:** the energy-only kits; the random specialty parcels (and §14's "parcels 5 and 7"); F6's honest but
late seat card; the §7 trigger "kits get one consumer". The station's goods differ from 20:05 on.

### 2 · Giving is an act, and the act is a pipe: "Send to…" on the building panel

**What the player does.** On any producer's panel, under F6's "Who could use your sand", each taker gets a
**Send ▸** button:
- "Ana's Glassworks · 15/min";
- "the Water Tower · wants 5,400";
- "Ana asks for sand".

One tap makes a **friend line**:
- That good goes to that taker first, before the Yard's anonymous matching, for as long as the taker uses it.
- If the building's pipes don't reach the gate, F2's planner lays the pipe in the same tap.
- The line is drawn across the Yard from your socket to theirs, in your colour, with a counter.

**What the receiver sees:** one line, *"💛 Ben sends you sand: 15/min to your Glassworks"*, with Thanks. A friend's
Ask shows on the giver's panel as "Ana asks for sand · Send ▸".

**The rules.**
- **Where a line can end:** only at a building that uses the good, the project, or an open Ask (a Storage). Nothing is
  sent to be resold.
- **Price:** the one player price, whichever the bots choose.
- **No new trade channel.** A friend line is a giver-chosen order inside the matcher, which already orders "usual
  buyers first, then crew mates" (§7).
- **Priority:** a line ranks after contracts and before Asks and the Yard.
- **When it stops:** it ends by itself when the taker stops using the good, and it never stalls the giver: spare beyond
  the line goes on down the flow order. A resting giver's line keeps running, and a stand-in keeps it at its terms.

**What the fix panel's objections were, and why they don't apply.** It dropped a "send" button that "moves goods or
gold without a pipe … opens a funnel, and is pointless: Cleo's copper would only be resold to the hub" (r1-together,
What I'd not do). Send moves goods only by pipe, at the one price, only to a real taker. It is the "standing orders"
item §19 cut, handed to the giver.

### 3 · Help a friend's estate: look, then sketch

**What the player does.**
- From the crew line, "Ana's Small Factory is stuck: ingots can't leave" (F2's IdleReason, shown to crew mates), opens
  Ana's estate read-only (visiting exists).
- **Sketch** places ghost pipes or buildings on her land.
- Ana's return card reads *"Dev sketched a fix for your Small Factory · Build it · 12 g"*. Her one tap builds it,
  with her own gold.

**The rules.** Crew only. At most one open sketch per friend. The owner can dismiss it. Nothing happens without the
owner's tap, and no gold moves. It reuses the blueprint ghosts of v1.1, without the Blueprint Office. Tending becomes
part of the visit: a repair you do while you're there.

### 4 · Optional: invite the neighbour you met

A crew member can offer a crew seat to a player in the small station attached to the crew's corner. That joins the two
into one big station, one Yard and one project (`Crews.cs` 482–500 already does this for crew members). For Mia this
was Capital N. It is one tap from the district chat or the Links tab, and her accept is a second tap. The crew chooses,
so no veto is broken.

### 5 · Price

I withdraw my fixed 1.75. I back the economy designer's 1.5 if the crew bot run agrees. At 1.5 the Glassworks earns
+18 g/min and the Smelter +7.5 for the buyer (r3-economy table), so a friend line pays both ends.

### Walking the run through it

- **Day 1, 20:05–20:20.**
  - Ben's first panel: "Sand Dredger · 15 sand/min". At 20:14 he writes "ana i have sand". The Foundation is 900
    energy (F4) and stands at about 20:12.
  - The Water Tower's recipe comes from what the station makes: energy, sand, copper ore, plus a keystone such as
    stone or wood.
  - Ben taps **Send ▸ the Water Tower** on his dredger. His promise is kept on the bus, the same evening.
  - Cleo's copper kit goes to the tower the same way. Ana's chat line *"Ben, the tower wants your sand!"* is about a
    real thing.
- **Day 2, 08:00.** Ben's card: *"Your sand built 30% of the Water Tower"*. No guilt.
- **Day 3.** Ben's last session. His line keeps sending while he rests, so the absent friend still gives (principle
  6). F4's shrink removes his share from the next basket.
- **Day 6.**
  - Cleo's crew line shows *"Ana's Small Factory is stuck: ingots can't leave"*. She visits, sees the pipe touches only
    the mine and a moily, and sketches one pipe to Ana's trading post.
  - At Level 6 Ana builds a Smelter, which the advisor suggests because Cleo has spare copper ore (F6). Cleo taps
    **Send ▸ Ana's Smelter**. That is the first good knowingly given, by an act, between two people.
- **Day 8.**
  - Ana's card: *"Cleo sketched a fix for your Small Factory · Build it"*. One tap: the factory runs after 4 days,
    not 32.
  - Dev takes the Old Forest seat (its kit: a Lumber Camp). The tower's keystone is wood, so on his first evening he
    sends it: the missing piece, on the day he arrives.
- **Day 21.** Dev doesn't need 11 days to find the dredger's dead end: his seat isn't sand. Ana's *"Cleo, ingots to
  me please"* becomes Cleo tapping Send ▸ on her factory for Ana's Lab.
- **Mia, day 4.** Ana welcomes her in the district chat. Her Links tab shows "Mia lives in Capital N, next to your
  station · Invite her to the crew ▸". With Mia's accept, her Houses' Yard becomes the crew's, and her energy goes to
  the project in the same round.
- **Day 36.** Ben's card: *"Your sand kept going to the Water Tower for 27 days; Granny Ida ⚙ kept it going from day
  30"*. His welcome is in the game as well as the chat.

### Cost and risks

**Build cost:** about 4–6 days on top of the fix plan, fitting after F6.

| Part | Effort | What it touches |
| --- | --- | --- |
| 1 · Specialty kits and land | S–M (about 1 day + a balance run) | Generator start-area placement, kit plans, kit-above-level text in Loady |
| 2 · Send and friend lines | M (1–2 days) | A per (giver, good) target in `MatchYard`/`MatchAsks`, the panel button on F6's taker list, the map stripe, one event |
| 3 · Visit and Sketch | M (1–2 days) | Read-only visit panels, a crew ghost from blueprints, a return-card line |
| 4 · Invite a neighbour | S | Crew invite to an occupied seat in an attached small station |

**Risks:**
- **Dependency.** A specialty seat must still work alone. Its goods sell to the hub and Houses as now, so solo parity
  doesn't change.
- **Obligation.** A line could feel like a duty. It never stalls anyone, ends by itself, and shows only "sends", never
  "stopped sending". The psychologist should check it.
- **Sketch abuse.** It is crew-only, needs the owner's tap and uses the owner's gold, so harm is minimal.
- **Loady confusion** over kit buildings above the player's level. This needs one text.
- **One more word.** "Send" is the word the players already used.

## What I'd keep no matter what

- Seat codes, two taps, held seats warmed by Houses, and Dev's code working with nobody online.
- The Handshake. Ben, day 36: *"handshake with windy heights (dev)"* was the one moment that felt like neighbours.
- Asleep in place, the stand-in at old terms, and one return card.
- No votes, kicks or releases, and "never who" on anything that could read as blame.
- Goods move only by pipe, and gold never moves between players.
- The station, the crew and the Yard as a place. The honest Yard stays as the fallback for spare nobody has sent
  anywhere.
- The project with a keystone, sized by what the station really makes (F4).
- Every fix in F1–F11.

## Questions for the others

- **Economy:**
  - Do specialty kits keep `TheFirstDay` pacing, and should Windy Heights get a fourth moily to match?
  - Does a friend line at 1.5 pay both ends at kit volumes?
  - Is an Ask-ended line (into a Storage) a funnel at any price?
- **Psychologist:** Does a visible friend line create obligation (Ben's guilt in a new form)? Is "sends", with no
  "stopped", enough?
- **Lonely guardian:**
  - Does inviting Mia into the crew's attached small station break any veto?
  - In an all-House station, should Houses' kits differ too, so a solo player's project has varied goods?
- **Wild:** Sketch is your round-2 idea. Does it give the "one estate, many hands" feeling without the churn you
  feared?
- **Outsider:** Is "Send ▸" on the building panel the place a three-minute phone player would find it? Ben opened only
  his solar panel and the trading post.
- **Everyone:** Should run 2 test Send and specialty kits from day 1, rather than wait for the day-8 and t < 15%
  triggers?

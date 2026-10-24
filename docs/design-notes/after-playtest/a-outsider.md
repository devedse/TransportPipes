# Round A · outsider

I came into the first design with no context, as the voice of a new player and of the owner's wish: fun with three
friends who join. I read the brief, `findings.json`, every diary, the friends' group chat, the director's notes, about
twenty screenshots, the fix plan with its engineer, economy and researcher papers, the build notes in
`ImplementationStatus.md`, and my own old rounds.

**My verdict in one line:**
- The way in held.
- The together layer failed by design, not only by bugs.
- The fix plan stops the game from hurting players, but it leaves the together layer as it was and makes it easier to
  see.

The owner is right to expect a redesign, but only of one layer: what friends *do* together. Places, seats, crews and
absence held and should stay.

## What held, what failed

"(a)" = built differently from the design; "(b)" = built as designed, and the design was wrong; "(c)" = not reached or
not tested. Where the wrong idea was mine, I say so.

### What held

| Promise | Evidence |
| --- | --- |
| **An invite gets you next to your friend in two taps, with no account** (my exam's T3) | Ben (ben d01-2000 #11, #13) and Cleo (cleo #9, #12) moved in with *Move in* and *Done*. Dev opened Ana's day-1 code on day 8, with nobody online, and was on his estate in about a minute (dev d08-2000 #3–#8) |
| **A seat kept for a late friend, warm and furnished** (T5) | Capital Power ⚙ left Dev three Wind Moilys, already piped (events day 8 20:03). Tailwind showed as "🍃 ×1.8" (#45), though Dev never noticed it |
| **Coming back costs nothing** (T13) | Ben, after 33 days: 42 s to his estate, *"a month away and nothing lost"* (ben d36-2000 #72–#79) |
| **Honest numbers** | Every upgrade card Cleo checked paid exactly what it promised (cleo d02-2000 #84, d10-2000 #203, d14-1900 #228–#231) |
| **The look** | *"look hand-painted"* (Mia), *"Gorgeous valley"* (Dev) |
| **The Handshake** | It is the one moment any player felt as being neighbours. Ben on day 36: *"handshake with windy heights (dev)"* |

### What failed

**1. Nobody played together on purpose.** In 36 days there were 0 Asks, 0 links, 0 tends and 0 pins (metrics). That is
(a) plus (b), and the (b) part is the bigger one.
- **The friends wanted an act of giving, and we designed none.** Their plan from minute 1 was to send things:
  - Ana: *"can someone bring sand? 😊"* (group, day 1);
  - Cleo: *"I don't know how to send her copper (station?)"* (cleo d06-1400 diary);
  - Dev: *"How do I actually send sand to you? Station?"* (dev d10-2000 #103).

  Our model was "you consent by laying a pipe, then the Yard matches by itself, only for real needs". So there was no
  verb for what all four wanted to do. (b)
- **There was nothing to give.**
  - Every starter kit makes energy, and Loady sends everyone to iron ore.
  - At the one price of 1.75, buying a neighbour's ore pays a Small Factory +1 g/min, and a Smelter loses on copper
    (r3-economy, the margins table).
  - In a world of five humans, quiet grace makes every Hub Order four times bigger below 10 active companies
    (`progression.yaml` 64–66, `GameWorld.Market.cs` 209). The hub was a bottomless buyer at full price: Ana's network
    card shows everything "sold to the hub" (ana d36-2000 #289).

  The push to trade that we counted on was switched off in exactly the world we tested. (b)
- **The Yard leaked.** It matched 361 energy a minute to a Lab that couldn't use it and paid nobody: about 3.5M gold
  (findings, the Yard bug; r1-engineer on `GameWorld.Yards.cs` 286–369). (a)

**2. The shared object never existed for the players.** My round-2 core idea was "the centre should be a shared object
everyone pipes into".
- The Foundation stood after 42 hours instead of 20 minutes, partly because of **my own replacement text** from round 5.
  I wrote its basket as "energy and iron ore (what the tutorial makes)", and no starter kit makes iron ore. (b), mine
- Its 18-hour tail is (a).
- The Water Tower was sized from the T(L) table, 3 to 18 times what these players earned (r1-economy). The design
  itself said "6 h of typical income", so that is (b).
- Its recipe came from land profiles ("Riverside means sand"), not from what anyone made. (b)
- Nobody opened Station › Project before day 21 (cleo d21-1900 #302).
- When players found it, it didn't pull. Cleo: *"3% of ~800 is ~24/min"* (#303), less than the Quarry her next steps
  offered. Ben: *"ana's water tower message (tick 'my spare sand') means nothing to me"* (ben d36 diary).

  A checkbox that donates goods to a counter is not playing together.

**3. "Social state lives where you already look" (my rule 6).** The principle was right; my placement was wrong. (b)
- I put the Yard "at the corner of your own estate, on screen whenever you look at your land". At the default zoom the
  corner is off screen: none of the estate shots I read shows it (for example ana d01-2000-0026, cleo d14-1900-0275).
- Players met the Yard only as a panel behind the toolbar's *Station* button. Nobody opened it on the first evening.
- The bell reached 99+ and the crew chip got 0 clicks in 36 days (findings, "What matters waits where nobody looks").

**4. The word "station" misled.** I pushed it in round 2. (b), mine
- In a pipe game, players read a station as a tool for shipping goods:
  - *"Sending copper ingots will need a station, I'll look at that next"* (Cleo, group, day 6);
  - *"Sand goes out via a Station (N)"* (Ana, group, day 14).
- The network card also counts "pump stations".
- Ben's first screen piled up six toasts and a level card: Surveyors, Handshake, Yard pipe, Achievement, Company Level,
  Iron Mine, Small Factory (ben d01-2000-0014). He called it *"hard to skim"*.
- So my rule of "four nouns" became more than fifteen in the build, and the one concept that mattered (your gate takes
  goods to your neighbours) was never learned.

**5. Three return screens.**
- My exam's T13 allowed "at most 3 screens", and the design said "three screens, one minute". Players hated three:
  - Ana: *"Three dialogs in a row blocking everything"*;
  - Ben: *"three screens before I see my estate"*.
- The bar was wrong: (b), mine. That two components were never merged is (a).

**6. A seat card that promised what the kit doesn't make.**
- Ben's seat read "Riverside: water and sand, wood". His land had no usable sand, and sand needs a Level-5 dredger on a
  5,000 g shore parcel (dev d10-2000 #81–#85).
- Ben wrote about guilt in four of his five sessions: *"letting Ana down"*, *"guilt"*, *"still guilty about sand"*.
- This came from my "seats are named after their land", together with decision 10, which put the home goods "behind
  the fence". (b), mine

**7. The lonely player in a five-player world.**
- The seat picker's stale seat is (a).
- Underneath it is (b). The four other humans were one crew, and a crew's station closes to strangers at 3 seats
  (`GameWorld.Crews.cs` 82). So in this world there was no human seat anywhere for Mia, and no lone-human card came.
- She lived among Houses. Her 33 g/min "to Houses" was never paid, her "alive" score was 3 of 5 in all four sessions,
  and she was *"not sure anyone is around now"* (mia d08-2000 #105).
- I said in round 2 that it must "work with 5 players first". It didn't.

**8. The absent friend stayed invisible.**
- Tending lives behind a chip nobody clicked.
- The stand-in notice went to the bell, and named "Riverside Co.", whom Ana never connected to Ben (ana d27-2000
  #247–#253).
- Our design never said where these show. (b)

**9. The middle game stalled on the base game's invisible rule: goods don't pass through buildings.**
- This is (c) for our design, but it decided the fun. Ana's factory sat Idle from day 4 to day 36.
- What she saw contradicted the rule: the network card says "Network · 8 buildings · 61 pipe tiles" (ana d36-2000
  #289), one network that her ingots can't cross.
- The friends' only real cooperation in five weeks was debugging this in their chat. Dev had the right lesson on day 8
  (*"I piped a moily into another moily and nothing flowed"*), and words couldn't carry it to Ana.

**10. Weeks 3–4 and the next season:** never reached (c). The Discovery banner did nothing when clicked (dev d08-2000
#30).

## Is the fix plan enough?

**Where it repairs a cause.** Ship these whatever else happens:
- **F1** makes the Yard honest: it matches only what a building can use, and what it doesn't take is paid at the hub
  price.
- **F2** puts the stuck reason first and stops the network card blaming energy.
- **F4** makes the project reachable: the Foundation is energy only, sizes come from measured income, the recipe from
  what residents make, and there is no tail.
- **F5** gives one return card. That fixes my three-screen bar.
- **F7** (the seat picker) and **F8** (smart Max) are real causes too, and so is **F3** (Loady follows the estate).

**Where it patches the symptom of a design flaw:**
1. **F6, "trade with a face", puts names on trades that will hardly happen.**
   - The plan says so in its own headline: *"An honest Yard trades little between kits that all make energy."*
   - The researcher says the same: *"In run 1's world it would trade almost nothing between friends"* (r2-researcher,
     "What everyone missed").
   - The cause (nothing to give, the price, quiet grace) is left to a trigger in §7: "the trade share stays under 15%".
     Run 1 already measured 0%. Waiting for run 2 to prove it again costs a run.
2. **"The project carries week 1"** asks a donation box worth +3% to stand in for friendship. Cleo priced it at 24 g/min
   and Ben couldn't parse it. A reachable project is necessary, but it is a goal, not something friends do for each
   other.
3. **F2's one button** ("Lay the pipe to your Trading Post · 12 g") fixes Ana's first factory. It doesn't fix her
   picture of the world. The panel's first line was already right on day 4 (ana d04-2100 #121), and she misread it for
   32 days, because the "network" on her screen isn't the one goods flow through. Her second factory, built on the same
   picture, will stick again. The plan's pass mark measures only the first.
4. **"Giving needs an act"** is also only a trigger ("if nobody knowingly gives by day 8"). Thirty-seven chat messages
   already say players want an act.
5. **Run 2 adds Jo**, a scripted solo human seated beside Mia, to make §17b "testable". That tests a world the quiet
   server doesn't have. The real five-player case is run 1, where every other human is one closed crew.
6. **Words.** "Station" becomes "Neighbours", which is good. But the screens still teach Yard, Yard gate (G), Project,
   keystone, Links, Plan board, *I make it* and *I need it*, Asks and the "This week" row. The plan adds texts more than
   it removes concepts.

So the plan is enough to make the game true and kind. It is not enough to make friends play together, because it
treats "together" as a problem of what's shown, and the run shows it is a problem of content and incentives.

## What I'd change

Keep the place-and-people model. Redesign what friends do in week 1: **a need, a gift with a face, and help.** These are
five changes on top of the fix plan. I'd fight for the first.

### 1 · A need you can fill by the first evening's second session, with a name on it (the one I'd fight for)

**What players do and see:**
- **Seats in a station come in pairs.** What your seat's kit makes, a neighbour's kit needs.
  - Riverside's House leaves a sand pump that runs from minute one, so "sand" on the card is true.
  - Windy Heights' kit holds a building that wants sand.
  - Economy picks the pairs from chains that exist at Level 5 or below and pay both sides at the one price.
- **The seat card says it in plain words:** "Riverside · makes sand from the first minute · Ana's Glassworks wants it".
- **The first arrival is announced on both screens:** "Ben's sand reached your Glassworks" (F6's first-gift line).
  - The building panel reads "sand from Ben · 1.5 g".
  - At the gate, the crew's trade is drawn as the one striped pipe our design promised (B's soul) and the build never
    showed.
- **The price is 1.5.** I'd take the economy designer's dissent now: at 1.5 both sides gain on iron and copper
  (r3-economy). The bot run can confirm it, but the pairs must pay at whichever price wins.
- **Nobody is stuck.** A missing neighbour means the House at 2.0 or the hub at 2.5 fills the gap.

**What it replaces:**
- the fix plan's §7 lever ("seat kits get one consumer of a neighbour's specialty"), which becomes the plan;
- decision 10's "both home goods behind the fence": the first home good goes in the start area, with its producer in
  the kit;
- the advisor-only "chains fed by a neighbour".

**Walking the run through it:**
- **Ana.** At 20:02 she asks "can someone bring sand?". Ben's kit already pumps sand. At about minute 12 both gates are
  connected, sand reaches her Glassworks, and the toast tells both of them. Her diary's "the bottleneck is friends, not
  me" (d02-2000) becomes a thank-you.
- **Ben**, who skims. He has one line on his panel: "your sand goes to Ana's Glassworks · +N g/min". The promise the
  game made him is kept, so the guilt of four diaries has no source.
  - He stops on day 3. His estate runs at 100% while resting, so Ana's glass doesn't stop.
  - On day 30 Granny Ida ⚙ takes over at 2.0, announced on Ana's return card (F5), not in the bell.
- **Dev** is the missing piece by construction, not by luck. Until day 8, the House on his seat sells its good to the
  crew at House terms (2.0). From day 8 it comes from him at 1.5, and the design's own line becomes true: "Dev's copper
  saves the crew 840 g/h".
- **Cleo**, the optimiser, gets a lever she can price. Her pair shows its g/min. On day 14 her diary's "exporting to
  friends, which I haven't understood" becomes a number on a panel.
- **Mia:** see change 4.

**Cost:** M.
- Kit tables and the start-area placement of the first home good.
- A balance run at 1.5 and 1.75 (already planned for run 2).
- A new rule: a kit may hold one building above the player's unlock level, which runs but can't be copied until it is
  unlocked.
- If no existing building fits a pair at Level 5 or below, one new small producer, which brings the art pipeline in
  (L).

**Risk:** dependency and guilt. The resting rule and the House fallback carry it. Watch Ben's diary in run 2.

### 2 · The network you see is the network that flows

- When you select a building or a pipe, its pipe group is drawn in its own colour, and every building on it is outlined.
  A building between two groups shows two plugs.
- The network card describes one pipe group, never "8 buildings" across a building.
- The fix plan's F10 "pipe groups light up" moves ahead of F2's button, not after it.
- **The test is the second factory.** After F2, replay Ana's d14 and let her build another factory. If she builds it
  wrong again without the button, the rule is the problem. Then take the fix panel's own fallback: goods pass through
  your own buildings that don't use them.
- **Cost:** S–M.

### 3 · Help each other in the game, the way they did in the chat

- **Why:** the friends' longest conversation was Ana's factory, and Dev had the answer on day 8. So let a crew mate open
  a friend's estate, read-only, from the crew line, and leave a **Sketch**: a ghost pipe or building with one line of
  text. This was B's idea (r3-B §2.5, "Sketches").
- **How it shows:** the owner sees it on the return card and builds it with one tap, at their own cost.
  - Ana on day 8 would have read: "Dev sketched a pipe for your Small Factory · Build it · 12 g".
  - Cleo gets a role as the crew's engineer, with her name on the fix and a 💛 Thanks. That answers *"Helping costs me
    nothing but I still see no payoff for helping"* (cleo d10-2000).
- **Safe:** no gold or goods move, and it is asynchronous and can't be farmed.
- **Cost:** M. A read-only view of another estate and the ghost layer, which blueprints already draw.

### 4 · The five-player world: seat the stranger beside the crew, not among Houses

- When a district's only lit humans are one crew in a closed station, "Find me neighbours" seats a stranger in the small
  station attached to it.
- Its seats join the crew's Yard as neighbours, using the crew-of-5–8 "big station" pool that already exists. Crew mates
  still come first in a shortage, and the crew's own seats stay closed.
- The lone-human card offers that seat. Chat gets dates (F10).
- Mia then sells real spare to named people, and her news names them.
- Run 2 plays Mia twice: once with Jo, once without. Without Jo is the real quiet server.
- **Cost:** S–M.

### 5 · Fewer words: players say "send", "gate" and "neighbours"

- **The words a player meets in week 1:** neighbours, crew, your gate, the Water Tower (a building), House ⚙.
- **Drop from what players see:**
  - "Yard" and "station" (call a place by its name, "Capital NW");
  - "keystone";
  - "Plan board", and *I make it* and *I need it*, which merge into Ask.
- The one return card is the only place for news.
- A phone shows one toast at a time (F10), and the first-evening toasts merge into one line: "You moved in next to Ana
  and Cleo".
- **Cost:** S, in the clarity designer's lane.

**In all:** about 5–8 developer days on top of the fix plan's 16 commits. Change 1 is the only one with balance risk.

## What I'd keep no matter what

- Seat codes and the two-tap guest move-in.
- Held seats that hold themselves for 28 days.
- The House's kit, already piped.
- The Handshake.
- An absent estate that runs at 100% while resting, then a stand-in at NPC terms that fades, and a way back that loses
  nothing: one card, one tap.
- One price between players, whatever its level. No gold between players, no votes, no kicks.
- Houses labelled ⚙ and capped at 25%.
- The fix plan's rule *"make it true, then show it"*, with paid figures only.
- The look.

## Questions for the others

1. **Economy:** which cross-seat pairs exist at Level 5 or below today, and which pay both sides at 1.5 and at 1.75? If
   none do, may a kit hold one building the player hasn't unlocked?
2. **Friends-first:** is a designed pair a dependency that brings back guilt? Or do 30 days at 100% while resting, plus
   the House fallback, make it safe? Ben's guilt came from a promise the game broke. Would a kept promise end it?
3. **Lonely guardian:** on a server where every other human is one closed crew, where should Mia live? Would the crew
   accept her small station in their pool, with crew first in a shortage?
4. **Psychologist:** their most-used social behaviour was helping each other debug. Is a Sketch the right in-game form,
   or would "visit and leave a note" do?
5. **Wild:** should "Yard" and "station" leave the screens entirely, if players' own words are send, gate and
   neighbours?
6. **The fix panel's engineer:** can pipe groups be tinted at all times cheaply? And what would pass-through for your
   own buildings cost, as the fallback?
7. **Everyone:** should run 2 also play without Jo?

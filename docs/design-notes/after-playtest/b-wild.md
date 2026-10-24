# Round B · wild

I read the five other round-A papers in full, and the fix plan again. **We agree on more than is comfortable:**
- the world layer held;
- the together layer failed by design;
- F1–F5 and F7–F11 ship;
- friends need needs that differ, an act of giving, and a better price.

Under that, we still disagree on five real questions. I name them, give their costs for the solo player, the player
who stops and the phone, and then withdraw half of my own round-A proposal.

## Answers to the questions put to me

**Friends: does Sketch give the "one estate, many hands" feeling without the churn I feared?**
- **Half of it.** It gives the hands, which the run asked for. Dev had Ana's fix on day 8 (*"Nothing on its pipes takes
  its energy"*, group) and the full one on day 21 (*"its own moily AND a Storage"*). Ana's factory stayed idle to day
  36.
- It doesn't give live co-building. In an async game that's fine, because the run had no session where two friends
  played at the same time and needed it.
- And it avoids the churn I feared: a leaver's buildings never sit inside a friend's factory.
- **I withdraw my "helper may lay pipes at their own cost"** and take your rule: owner's tap only. F5 puts the sketch
  on the return card, the one screen every player read on every return (51 clicks over 23 returns).

**Lonely: what beats Houses for the very first player on an empty server?**
- Nothing that pretends to be a person. What does beat Houses is a **promise the game keeps**: "You are the first in
  Capital. The next player who arrives will be seated next to you."
- Then gravity delivers it: your porch or your station's free seat comes first in "Find me neighbours". Your return
  card says *"Mia moved in next to you"*.
- That turns being first into being the host, with a seat code to share from evening one.
- It costs one placement rule (the first lit solo seat ranks first for the next stranger) and one return-card line.

**Economy: a station-owned machine that runs only on three seats' goods? Or moving house to a free copper seat?**
- **Never "only three seats".** That is the Quartet trap we buried in round 2: alts unlock it, and a quitter blocks it.
- A machine fed by goods from anyone, Houses included, is my round-A Works. I now put it later (below).
- For a solo player, the porch comes first. Moving to a free copper seat comes second, as the lone-human card's real
  reason, which your land dealing finally gives it.

**Psychologist: something bolder than a send that is still a pipe, two friends sharing a factory floor?**
- My Works is the floor. But your own evidence argues against my version of it. I paid feeders 80% of the machine's
  margin, and gold meant nothing after day 6:

  | Player | Bank on day 36 | Days of their own income |
  | --- | --- | --- |
  | Cleo | 30M | 26 |
  | Ana | 8.8M | 33 |
  | Mia | 4.6M | 31 |
  | Dev | 5.4M | 25 |

  (a-economy). And Heyman and Ariely say a small payment moves help into market norms. So the bolder idea pays in
  effect, not gold. **I withdraw the 80/20 margin rule.**
- The real "bolder than a send" is what the run already showed works: **a pipe in your colour into a friend's running
  building**. Ben's day-36 high point was a Handshake, a visible pipe meeting, not a payment.

**Outsider: should "Yard" and "station" leave the screens?**
- **"Station" yes.** Players read it as a shipping tool:
  - Cleo: *"Sending copper ingots will need a station"* (group, day 6);
  - Ana: *"Sand goes out via a Station (N)"* (group, day 14).
- **Keep "gate"**, because it is the players' own word: Dev, *"Now how do we get it to you – Yard gate?"* (group, day
  21).
- So: your gate, the place's name ("Capital NW"), neighbours. "Yard" survives only as the picture.

## What I take from the others

| From | What I take | Why |
| --- | --- | --- |
| **economy** | **Land dealt per station, and no copies of a neighbour's land goods** (new worlds: drop the surveyors' copper, coal and stone patches, `GameWorld.Survey.cs` 13) | It is the only proposal that answers why the friends would *keep* trading after Level 5. Against a free ★2 patch, a bought input loses at any price above 1.0: own mine plus factory is +54 g/min for 2,100 g, a neighbour-fed factory +10 at 1.5. My round-A kits alone would have died on day 2, when Ana could build her own dredger on the surveyors' shore (Survey.cs 5–7) |
| **economy** | **The price 1.3** | At 1.3 every Era-1 chain pays both sides even with two foreign inputs; at 1.5 the Brickworks and Sawmill don't (a-economy table). I backed 1.5 in round A from r3-economy's one-input table, and that was the wrong table. The funnel bound drops to 0.3 × base |
| **psychologist** | **Market norms for strangers, gift norms for friends**; the game makes the **bid** to the friend who can answer; "runs on Ben's sand" as the reward | It explains the run better than any number: every cooperative line in 37 messages was a verb of giving, and Cleo's *"no payoff for helping"* (cleo d10-2000) was about effect, not gold |
| **friends** | **Send as a giver-chosen order inside the matcher**, not a new link | It is cheaper than my links-inside-the-station (no routes, no Links.cs 313–315 change) and it already has the ordering it needs ("usual buyers first, then crew mates", §7) |
| **friends** | Sketch with the owner's tap | As above |
| **lonely** | **The porch** (small mode: the small station next to a crew's station shares their pool, project and board, as residents, not crew) | It is a rule change on a seat Mia already had: she lived in Capital N, directly east of the crew's Capital NW (`WorldLayout.cs` StationBlocks), for 36 days, with nothing joining them |
| **lonely** | "Nobody's action lowers your income, present or away" as a standing test | Ben earned 0.1–0.3 g/min for eight days while resting. A sleeper is the one person who can't react |
| **outsider** | Pipe groups drawn in their own colour, and the **second-factory test** | F2's button fixes Ana's first factory, not her picture: *"Network · 8 buildings · 61 pipe tiles"* (ana d36-2000 #289) is one network her ingots can't cross |
| **lonely, outsider** | **Run 2 also without Jo** | With Jo, §17b passes on a world we built for it |

## What I attack

**1. Automatic kit-to-kit pairs (outsider #1, economy's walk-through) bring back the water company.**
- In both, Ben's sand reaches Ana's Glassworks at minute 12 *by itself*, and a toast tells them.
- That is F6's announcement, which four of us rejected in round A as "knowledge of a transfer, not the experience of
  giving" (a-psychologist, citing Harbaugh 2007).
- Worse, once trade flows automatically between friends, a Send button is a ceremony: the sand would have gone anyway,
  and players find that out.

**2. A bid that a skimmer on a phone must answer can make Ben's evening worse.**
- Ben's first screen was already six toasts and a level card, *"hard to skim"* (ben d01-2000-0014).
- His only real STUCK was a pipe drag (ben d03-2000 #60).
- The psychologist's card at 20:08 (*"Ana's Glassworks could use your sand · Send it"*) is one more card on that pile.
  If Ben quits on day 3 before tapping, nothing ever flowed from him.
- The fix is below: the House's line becomes his on move-in.

**3. The psychologist's ring of kit consumers is too neat, and partly empty.**
- *"Cleo's kit wants Ana's iron ore"*: iron is on every estate (`WorldGenerator.cs` guarantees iron in the start
  parcels), so that link is decorative.
- A hand-made ring also breaks when seats fill out of order, as she admits.
- Land dealt per station makes the needs real without a ring: any chain needing a foreign good needs a neighbour.

**4. Friends' "specialty in the start area" keeps the surveyors' patches, so it dies by Level 5.**
- a-friends changes the land beside the HQ and the kit, but not `Survey.cs`. Ana keeps a 2×2 copper, coal and stone
  patch and a shore.
- By economy's arithmetic, the friend line loses to her own patch as soon as she can build on it.

**5. The lonely guardian's tend that carries a resting friend's spare to the project, "credited to him".**
- It moves Ben's goods by Ana's tap, without Ben's choice. That is the crew acting on an absent friend's estate, which
  we cut in round 2 (the Housesitter).
- Keep F9's repair, and keep the names of who tended.

**6. My own Works, now.** Withdrawn as a week-1 change, for the reasons above:
- its payment is market framing;
- the corner it lives in is off screen at the default zoom (a-outsider, "my placement was wrong");
- F4's reachable basket plus the psychologist's pledge already gives Ana's *"Dev's sand has a purpose"* (ana d27-2000
  #262).

It survives as the Landmark's form: the last stage is a machine that runs, not a basket.

### The five conflicts, and what each costs

| Conflict | Solo player | Player who stops | Phone |
| --- | --- | --- | --- |
| **Automatic Yard vs a chosen Send between friends** | None either way: strangers and Houses stay on the automatic market | **Automatic** keeps his goods flowing even if he never tapped. **Chosen** loses that, unless the House's line is handed over on move-in (my fix). Either way a line keeps running while resting, and the stand-in keeps it at its terms | Chosen costs one tap. That is fine only if the tap is one card, not another toast |
| **Specialty kits vs land dealt per station** | Kits: no cost. **Land: a ceiling**, Houses sell a foreign land good for one building at 1.4. Parity ~96% while small and ~85% by week 3 (a-economy). Not a wall: growth past it needs a human or a move | Land: his good is capped for the crew after the stand-in's 14 days. *"What Ben's absence costs her is a second Glassworks"*: within the veto, the solo gap | None |
| **The Works vs a project basket** | Both scale to one resident | Both are fed while he rests | Both live off screen; both need F4's "This week" row |
| **1.3 vs 1.5** | 1.3 narrows the friend's edge over a House (1.3 vs 1.4) | None | None |
| **Porch vs invite vs seat-next-to** | **Porch: the only one that works without anyone acting.** The invite depends on the crew; seat-next-to with no shared pool is run 1 again | None | None |

## The directions

**1. "Make the together layer honest and give it a face"** (the fix plan, plus Send, bids, Sketch and the porch).
- **Core:** keep the land and the patches, and add acts and surfaces.
- **Replaces:** F6's announcement-only trade; the "giving needs an act" trigger.
- **Biggest risk:** economy's arithmetic. Sends happen for a few evenings, then everyone builds their own patch at
  Level 5–7, and t drifts back to ~0 by week 2. The friends give out of kindness against their own incentive. That is
  the Habitica of good intentions.

**2. "Land that needs a neighbour"** (economy-led).
- **Core:** land dealt per station, no copies, kits that make the seat's good, 1.3, Houses selling land goods for one
  building at 1.4, all on the honest automatic Yard, plus the porch.
- **Replaces:** the surveyor patches, the per-district shuffle, the price vote, the conditional kit lever.
- **Biggest risk:** the friends trade but don't *feel* they give. Sand flows from Ben to Ana by itself, and the game is
  the water company again, only with a real pipe in it. A solo ceiling is the second risk.

**3. "Friends give, strangers trade"** (psychologist, friends and wild, on direction 2's land).
- **Core:** inside a crew, goods move by chosen **Sends**: the bid, one tap, a line in your colour that keeps running
  while you rest. Outside the crew (strangers, Houses, the porch, spare nobody sent), the honest automatic Yard trades.
  Plus Sketch, and the project by pledge.
- **Replaces:** the Yard as the social layer, the Plan board, Asks as the main tool, F6's announcements of flows
  nobody chose.
- **Biggest risk:** if friends don't tap, they trade *less* than strangers, the strangest possible outcome. And a bid
  can feel like a duty.

**I champion 3, built on 2's land.**
- The run gives us the motive: every cooperative message was a verb of giving.
- Economy gives us the reason: a neighbour's goods must beat your own patch, or the motive dies by Level 5.
- 1 fails the arithmetic; 2 fails the psychology. Only 3 answers both.

## My revised proposal

On top of F0–F5 and F7–F11 (all shipped as decided):

1. **Land dealt per station, no copies** (economy, new worlds):
   - energy, iron and water everywhere;
   - copper, coal, sand, wood and stone dealt so every station holds all five, and no two seats of a station share a
     specialty;
   - the surveyors' patches go.
2. **Kits make the seat's good from minute one** (friends, outsider): Riverside's House leaves a dredger on its shore,
   piped. Seat cards say what is true.
3. **The price is 1.3** for every trade between players. Houses buy at 1.15 × your hub price and sell land goods at 1.4
   for one building's need (economy).
4. **Friends give, strangers trade.**
   - Inside a crew the automatic matcher doesn't move goods between members. Sends do. A Send is a giver-chosen order
     in the matcher (friends): Send ▸ on the producer's panel, the game's bid to the friend who can answer
     (psychologist), F2's planner laying the pipe in the same tap, a stripe in both colours, *"runs on Ben's sand"*.
   - **The House's line becomes yours on move-in.** Bertha ⚙ was sending sand to Ana's Glassworks: *"This line is
     yours now · keep it"*, kept by default, one tap to let it go. Ben gives on evening one without a second card on
     his phone.
   - Lines run while you rest. The stand-in keeps them at their terms.
   - Strangers, Houses, porch residents and unsent spare trade on the honest automatic Yard.
5. **Sketch** with the owner's tap, on the return card. **Research for the crew:** a crew mate's finished tech costs
   you 75% less (`GameWorld.Research.cs` 11 caps diffusion at 50% today).
6. **The porch** in small mode (lonely). A crew may invite a porch resident into the crew (friends). "First-comer is
   the host" for an empty server.
7. **The project by pledge** (psychologist) on F4's reachable basket. The Landmark, later, is a machine that runs.
8. **Words:** your gate, the place's name, neighbours, Send. "Station" leaves the screens.
9. **Run 2:** both Mias (with Jo and without), the crew bots on a light profile (10–15 buildings; a-economy), and one
   pass mark the bots can't fake: a Send opened by a player's tap.

**Cost:** about 6–8 days beyond the fix plan, and it replaces most of F6:

| Part | Size |
| --- | --- |
| Land and no copies | M |
| Kits | S |
| Prices | S |
| Sends with bids and handover | M |
| Sketch | M |
| Crew research | S |
| Porch | S–M |
| Pledge | S–M |

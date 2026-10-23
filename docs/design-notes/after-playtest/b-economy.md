# Round B · economy

I read the five other round-A papers in full and the fix plan again, and checked each money claim against
`buildings.yaml`, `progression.yaml` and the code.

**Convention:** all margins are for a level-1 building in g/min, with own energy at 1 g and goods at base. Levels
multiply inputs and outputs alike (1.1^(L−1), ×2 at milestones), so every ratio below also holds at levels 1–5 and
beyond.

The convergence is real, but it has one gap that the numbers expose. Specialty kits, rings and Sends all assume that
the receiver *needs* the friend's good. On today's land, every estate can make that good itself from Level 5. The
advisor even suggests it: "an extractor of something its land holds that it doesn't mine yet" (`Advisor.cs:868–873`).
So most of what we agree on lasts about two days.

## Answers to the questions put to me

**Friends:**
1. **Do specialty kits keep `TheFirstDay`'s pace?** Nearly.
   - A dredger kit makes 15 sand (18 g) for 8 energy plus two moilies: ≈ 70 g/min.
   - A copper kit makes ≈ 83 g/min.
   - Three moilies make 90.
   - Since XP follows gold, the sand seat levels about 22% slower on day 1. One extra moily on every non-energy kit
     closes it (100 against 90). Windy Heights doesn't need a fourth.
2. **Does a friend line at 1.5 pay both ends at kit volumes?** For sand into a Glassworks, yes: +18 for the buyer, +9
   for the seller. But only until the buyer builds his own dredger. Every estate has a pond within reach
   (`Survey.cs:5–7`). At 1.5 an own dredger (500 g) saves 9 g/min plus its own margin, so it pays for itself within the
   hour. See attack 2.
3. **Is an Ask-ended line into a Storage a funnel?** Yes, at any price above the hub's, and it is **live today**.
   - An Ask pays `PlayerPrice` for units that only go into the asker's storages (`Asks.cs:246–251`), and the seller
     gets the XP too.
   - Attack 4 has the numbers and the one-line fix.

**Wild:**
1. **The 80/20 rule, every machine.** What a feeder gets, × base: Glassworks 1.50, Small Factory 1.40, Smelter 1.31,
   Brickworks and Sawmill 1.27, Big Factory 1.53, Steel Mill 1.20, Refinery 1.28, Circuit Fab 1.19, Wire Mill 2.52,
   Cement Kiln 2.50.

   The same at levels 1–5. **Three Era-2+ machines have negative value added at base prices:**
   - Machine Shop: 6 steel (360 g) + 60 energy → 3 gears (330 g), so −90;
   - Assembly Plant: −1,600;
   - Robot Works: −200.

   The Works can't run them, and neither should any player. That is a content bug the long game will hit whatever we
   decide. I flag it for the fix panel.
2. **Does the 20% build the next machine without gold?** Yes, easily. 20% of the Glassworks' 27 g/min is 5.4 g/min, so a
   Brickworks (1,400 g) in about 4.3 hours.
3. **Is 1.5 still right once Send carries trade between friends?** No: 1.3, by the table in attack 1.

**Lonely:**
1. **On a porch, how much of Mia's output would the crew's pool take?** With honest lacks and kits that all make energy,
   about 0% at 1.75 and at 1.5 alike. Every resident has spare energy and nobody lacks it. Her House's "33 g/min" was
   matched, never paid (r1-economy, item 1). The porch gives presence, and trade only once seats make different goods.
2. **Does a tend that carries the resting friend's spare to the project change the faucet count?** No. It is his own
   spare, already bound for the hub, and the project pays base.

**Psychologist:**
1. **Does a send pay the receiver at the one price?**
   - At 1.75, barely: Small Factory +1, Smelter −1.25 with one bought input and −8.75 with two.
   - At 1.5: Small Factory +10, Glassworks +18.
   - At 1.3: every Era-1 chain pays.
2. **Does a kit consumer fed by a House at 2.0 keep Mia at 85–90%?** No. Four of the five Era-1 chains *lose* money at
   2.0 (attack 3).

**Outsider:**
1. **Which cross-seat pairs exist at Level 5 or below?**
   - Only the Sawmill takes a good that isn't on every estate, and at 1.5 it loses 0.5.
   - The Small Factory's ore is on every estate.
   - The Smelter and Brickworks are Level 6, the Glassworks Level 7.

   So a pair needs a kit building above the unlock level.
2. **May a kit hold such a building?** Yes, and it mints nothing. A kit building earns only its own value added, as the
   player's own would.

## What I take from the others

| Paper | Proposal | Why I take it |
| --- | --- | --- |
| **friends** | **Send ▸** on the producer's panel, ending only at a building that uses the good or at the project. **The seat's primary good in the start area beside the HQ**, its producer in the kit. **Sketch**. **Invite the neighbour you met** | The act the friends asked for, priced at the one price, and no new channel. The start-area placement fixes my own "behind the fence" error |
| **psychologist** | **The game makes the bid to the one who can** ("Ana's Glassworks could use your sand · Send it"). **"Runs on Ben's sand"** on the receiver's panel. The market and the gift kept apart. **I'll bring it / Let it go** | The best answer to Ben's guilt. And a bid costs nothing in gold |
| **wild** | **A Send is accepted by itself when it only replaces an import.** The Works' idea that **the project pays feeders more than the hub** (in the simpler form of my proposal 5) | A rule that can't be abused. A shared object with a reason to feed it |
| **lonely** | **The porch.** "Nobody's action lowers your income, present or away" as a standing rule, with a resting-seller test. **A Mia without Jo** in run 2 | The real five-player world. The F1 floor made permanent |
| **outsider** | **Pipe groups drawn in their own colour.** **The second-factory test.** Fewer words. Run 2 without Jo | Ana's 32 days were a picture problem, not only a missing button |

## What I attack

**1. "The price is 1.5" (friends, wild, outsider). It pays only chains with one bought input.**

The buyer's margin; "two" means both of its non-energy inputs are bought.

| Chain | Own-made | at 1.75 | at 1.5 | at 1.3 | Seller's gain at 1.3 |
| --- | --- | --- | --- | --- | --- |
| Small Factory (ore) | +28 | +1 | +10 | +17.2 | +10.8 |
| Smelter, copper only | +25 | −1.25 | +7.5 | +14.5 | +10.5 |
| Smelter, copper + coal | +25 | −8.75 | +2.5 | +11.5 | +13.5 |
| Brickworks, stone only | +12.5 | +1.25 | +5 | +8 | +4.5 |
| Brickworks, stone + coal | +12.5 | −8.1 | **−1.25** | +4.25 | +8.25 |
| Glassworks (sand) | +27 | +13.5 | +18 | +21.6 | +5.4 |
| Sawmill (wood) | +7 | −4.25 | **−0.5** | +2.5 | +4.5 |

The worked check, Brickworks with stone and coal at 1.5: 50 − 10 − 1.5 × (15 + 12.5) = −1.25.

**What it shows:**
- 1.5 is enough only while the other input is home-made. That needs exactly the copies that kill the need (attack 2).
- 1.3 pays every Era-1 chain both ways, and the most it lets an alt carry is 0.3 × base a unit.

**2. Specialty kits, rings and pairs (all four papers) last until Level 5 as long as the copies remain.**

Every estate gets 2×2 patches of copper, coal and stone at ★2, a shore within its first parcel (`Survey.cs:5–13`), a
forest and a little stone (`WorldGenerator.cs:346–359`).

The run's own clock:
- Ana reached Level 5 on day 2, Cleo and Ben on day 3 (metrics row04–row05).
- From then on her Glassworks' cheapest sand is her own:
  - a 500 g dredger;
  - a 2×2 patch has room for 4 mines, three Small Factories' worth of ore;
  - against a bank she can't spend.
- The advisor suggests the extractor first (`Advisor.cs:868–873`), and a rational player follows it.

So Ben's kit dredger and Ana's ring Glassworks give the crew **about one evening of trade**, and t is back near 0 by
day 3. That is the day Ben stopped. The rings, pairs and Sends rest on removing the copies of the seat goods. No paper
but mine removes them.

**3. "Every kit consumer always runs, from the House at 2.0 or the hub at 2.5" (psychologist, outsider).**

The buyer's margin with inputs from a House at 2.0:

| Chain | One input bought | Both inputs bought |
| --- | --- | --- |
| Small Factory | −8 | |
| Smelter | −10 | −20 |
| Brickworks | −2.5 | −15 |
| Sawmill | −8 | |
| Glassworks | **+9** | |

At the hub's 2.5 even the Glassworks is at 0. A kit consumer that "always runs" on a House is a small Cleo's-Lab drain
on Mia and on any friend whose partner left. **Fix:** Houses sell the station's land goods at **1.4, up to one level-1
building's need per good and buyer**. Then every chain stays positive: the Smelter with both inputs makes +7, the
Glassworks +19.8.

**4. Sends or Asks into a Storage at the player price are a funnel, and the Ask version is live now.**
- **How it works:** an alt posts a renewable Ask for the main's goods (`Asks.cs:246–251`), and the main fills it at
  1.75. The alt resells at its hub price (≤ 1.0), so 0.75 × base moves per unit, with the XP.
- **How far it goes:** the alt keeps 1/1.75 of each round's gold to spend again, so over many rounds about **all of its
  bank** can move. A Mia-sized alt bank of 4.6M carries ≈ 4.6M, limited only by its own √ Hub Order on resale.
- **The fix, in one line:** units that only go into a storage (an Ask, a Send ending in a Storage) are paid at the
  seller's hub price; the premium applies only to what a building or the project takes. Legitimate use loses nothing.

**5. The stand-in's old price after only 5 days of play (`StandIns.cs:36`) is a fake-quitter farm at real incomes.**
- **The build:** the NPC goes on paying the old price for what partners sold to the leaver, then glides to House terms
  (ImplementationStatus, step 4).
- **One alt Glassworks fed by a Send:** (1.5 − 1.0) × 18 g/min × 20,160 min ≈ **0.18M**, plus the glide. At 1.75,
  0.27M.
- **Against real income:** the run's real incomes are about 200 g/min (0.29M a day), so one alt consumer is worth
  0.6–0.9 days of a player's income, and five ≈ 3–5 days. My exam's pass mark was ≤ 0.5 day.
- **Fix:** the old price only for crew members with ≥ 10 distinct days of play on another device, as v1.2 §9 says.
  Otherwise the stand-in pays House terms from day one.

**6. The Works (wild) passes the money test, and is worth little money.**
- **No minting.** Feeders are paid only from the machine's real value added. An alt feeding it gets its own share. The
  hub buys the output as it would buy anyone's.
- **Its whole economic effect is one free factory per station:** 27 g/min at level 1, 21.6 of it to the feeders.
  Against the run's station income at the end (Ana 187 + Cleo 799 + Dev 152 + Ben 77 = 1,215 g/min), that is +1.8%.
- **It pays less than owning the factory.** Ben's sand earns him 9 g/min in the Works against all 27 in a Glassworks of
  his own, so feeders give it spare, not production.
- **Its value is social. Its cost is 3–4 days, a margin ledger and a building in the Yard.** And it still needs seats
  that make different goods.
- **I take its money idea in a cheaper form:** the project pays the player price for what it takes. That adds ≤ 0.3 ×
  base per unit of new gold, about 28k on the 93k Water Tower. A gift to the project then pays like a gift to a friend.

**7. Crew research at −75% (wild) helps nobody who needed help.**
- Era 1's techs cost 3,500 points. 25 more points of discount than today's 50% diffusion (`Research.cs:11`) saves ≈ 875
  points: about 3 hours of a level-1 Lab.
- Ana had 0 points because she had no Lab, because she had no ingots. Three quarters off nothing is nothing.
- It's harmless (no gold moves), but low. Cleo's ingots *sent* to Ana's Lab answer her better.

**8. The bot gate (fix plan §6) will pass for the wrong reason.** Bots with about 300 buildings fill their 2×2 patches
and start buying, so t can reach 15% in the crew-bot run while humans with 10 buildings stay at 0. Measure t with a
light bot: 10–15 buildings, a session every 2 days.

## The directions

| | **D1 · Make giving an act** (friends, psychologist, outsider) | **D2 · The Works runs** (wild) | **D3 · Land that needs a neighbour** (economy) |
| --- | --- | --- | --- |
| **Core** | The honest Yard as the market; bids to the friend who can; **Send** by pipe; specialty kits or a ring of kit consumers; Sketch; the porch; price 1.5 | A station-owned factory fed by everyone, paying feeders hub value + 80% of its value added; truthful kits; Send; hands; crew research | Land goods dealt per station; **no copies**; the seat's producer in the kit; one price **1.3**; Houses sell land goods at 1.4 for one building |
| **Replaces** | F6's after-the-fact announcements; the §7 triggers ("giving needs an act", "kits get a consumer") | F4's basket and bonuses; most of F6 | §14's ★2 patches; the price vote; F6's advisor-only interdependence |
| **Biggest risk** | With the copies kept, needs vanish at Level 5 (attack 2). 1.5 fails two-input chains; the storage-bound Send funnel | The largest build (+5–7 days); a second economy inside the Yard worth +1.8% in gold; still no reason without different land | It gives a reason, not an act: needs nobody sees or answers. Dependency could become guilt. It needs a fresh world (the owner already starts one per rules version) |

**I champion D3 + D1 together, not either alone.**
- **D1 without D3** is a beautiful verb with nothing to carry after day 2.
- **D3 without D1** is a real need with no way to answer it but an anonymous matcher, which is the run's failure in
  reverse.
- **D2's** best part, a project worth feeding, fits into the merged direction as one pricing rule.

## My revised proposal

**"Needs from the land, gifts by pipe."** All of the fix plan's F0–F11 stay. Changes:

1. **Land.**
   - Energy, iron ore and water are on every estate.
   - The five Era-1 land goods (copper, coal, sand, wood, stone) are dealt **per station**, with no copies elsewhere:
     the surveyor patches and ponds stay only on their own seats (`Survey.cs:13`, `WorldGenerator.cs:346–359`).
   - The seat's primary good lies in the start area beside the HQ (friends), and its producer is in the kit (friends,
     wild, outsider). Add one moily to non-energy kits.
2. **A ring of kit consumers** (psychologist), using a building above the unlock level where needed (outsider). For
   example, Ana's kit Glassworks runs on Ben's kit sand from minute 12: +21.6 for her, +5.4 for him. Without a pond,
   Ana has no reason to replace it on day 3.
3. **One price, 1.3**, for the Yard, Sends, links, contracts and the Exchange.
   - Houses buy at 1.15 × your hub price.
   - Houses sell land goods at 1.4, for one level-1 building's need per good and buyer.
   - Energy and iron stay at 2.0.
   - No chain ever runs at a loss on House supply.
4. **The bid and the Send** (psychologist, friends).
   - Send ends only at a building that uses the good or at the project. It is accepted by itself when it replaces an
     import (wild).
   - **Storage-bound units are paid at the seller's hub price.** That closes the live Ask funnel.
5. **The project pays the player price** (1.3) for what it takes. New gold ≤ 0.3 × the basket.
6. **Stand-ins pay the old price only to crew members with ≥ 10 distinct days of play on another device**, otherwise
   House terms from day one. Today's threshold is 5 (`StandIns.cs:36`).
7. **The porch, "Today here" and Sketch** (lonely, friends, outsider). They are free in gold.
8. **Measure t with a light bot,** and fix the three Era-2+ recipes whose value added is negative (Machine Shop, Assembly
   Plant, Robot Works) before anyone reaches them.

**What it costs:** the land, kits and price are about 2–3 days. Send, bids and the storage rule are F6's two M's,
re-aimed. The porch, Sketch and the project's price are S–M each.

**What would prove me wrong:**
- a light-bot crew at 1.3 on dealt land still trades under 15% by day 14; or
- run 2's diaries name a friend's absence as a loss ("no sand since Ben left") even though the House keeps one building
  running. Then dependency costs more than it gives.

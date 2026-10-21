# R2 · 4 · Economy and long game: Yards, Lines and the hub as the bank

Round 2 of the economy and long-game designer. I read all six round-1 files and the brief, and I re-read the balance
runs in `docs/balance/` so the numbers are tied to the real income curve instead of guesses.

## 0 · What I got wrong in round 1, and what changed

- **My numbers were the wrong size.** The solo balance runs show a typical player at about **800 g/min on day 1,
  3,000 on day 7, 3,800 on day 14–21 and 4,900 on day 28** (casual 2,350 → 4,700, hardcore 3,450 → 6,600). My
  round-1 hub allowance (600 g/h × Era × (1 + L/10), 3,840 g/h at Level 22) was about **30 times too small**: it
  would have cut almost everyone's hub income in half from day 1. My trunk line cost (8,000 g for 200 tiles) was about two minutes of
  week-3 income. **From now on every number is set as a share of T(L), the typical income at Company Level L**,
  so the design follows rebalancing.
- **The balance runs also show the real problem of the long game.** Typical income grows only +2% from day 14 to
  day 21 (3,774 → 3,866 g/min, 285 → 306 buildings): the estate is full and flat. Whatever happens between players
  in week 3 has to **be** the growth, not a side bonus.
- **XP comes from what you earn** (`GainXpFromEarnings`). So any price advantage between players is also an XP
  advantage. That makes the price ladder a lever on progress, and it makes caps against funnelling matter more.
- **Dropped from round 1:** the anonymous station market at 1.4/2.0 (it was a second idea of trading among the same
  three neighbours, and it brought back the "you trade with whoever is next door" feeling), the tier-3 hub
  allocation (a wall for the solo player), and the +15% production for mastery (the rich get richer, see §1.4).
- **Taken from the others:** the Quad and the Yard (2), Houses and successor Houses (3), link states with a reason
  (3), lines with gate tanks (1), order boards with an NPC that fills the rest at the deadline (5), contributions
  capped per day that can be saved up (5), credit measured as a share of your own output (3), leagues by stage (5),
  segment-built trunk lines with names and adopted pump houses (1, 2, 5), the Great Pipe as a ledger (6).

---

## 1 · The five weakest ideas, attacked

### 1.1 Content that needs N different humans: Quartet Recipes (6 #15), relay goods (1 #13), professions that only pay with another profession (1 #39)

**Failure scenario.** Day 1: Mia, Jon, Lou and Pat start a crew because the Quartet trophies (the "crew's
best-paying products") need four human inputs. Day 3: Lou and Pat stop. That is the normal case, since most groups
shrink to one or two in a week. From day 3 the most visible goal in Mia's Yard is locked forever, and the reason is
her friends quitting. That is the Habitica trap: other people's absence hurts you.

Meanwhile Gary opens three private browser windows, makes three Level 1 alts, and pipes 1 ore/min from each into
his Junction. The Quartet is done in 25 minutes. If a Quartet item sells at even +50% over a normal Era 4 good
(engines are 1,500 g at the hub), Gary earns about **+750 g per item from accounts that cost him nothing**. So the
feature pays people to make multiple accounts and punishes real friends. "Professions" make it worse: two Miners
who are real friends get no bonus for trading with each other, and a solo player plays at "base speed", which is a
built-in handicap.

**Verdict:** never gate content or premiums on the *number* of distinct humans. Reward *what flows* (a mix of goods
from different **home goods**, from anyone: friend, stranger, House or hub), never *who* it came from.

### 1.2 A map that moves: the accordion map (1 #7), islands that drift together (1 #5), estates on rails (1 #3)

**Failure scenario.** Distance is the price of logistics: grey pipe loses 0.3% per tile. Ana sells coal to Finn over
a 120-tile line (30% lost, still better than the hub). One night the accordion re-packs the map and the line is 40
tiles (11% lost): Finn's buy settings flip and he drops his other supplier. The next night it is 160 tiles (38%
lost): the trade no longer beats Ana's trading post (the rule since October 21), so it stops. Finn's Steel Mill runs
dry at 03:00 **without anyone doing anything.** A crew that spent five days and about 10M gold on a 170-tile steel
trunk line wakes up to a line that is the wrong length. Drifting islands are worse in a quieter way: links that
have lasted a week get shorter and cheaper, so the incumbents' trade gets better while a newcomer's stays far and
lossy. The train-car world makes income periodic (you only sell at a hub when the train stops), which an idle
game with offline progress can't afford.

**Verdict:** the map must be stable while you live on it. Moving is a *player's* choice (the moving van, crates,
Quiet Isles), never something the world does to your pipes.

### 1.3 A Lodger or successor that copies the leaver's trade (2 #11 "copies his last 7 days", 3 §3.2 "inherits at ~90% volume")

The intention is excellent (a link survives its partner). As written it is the **best gold farm in the game**.

**Failure scenario.** Ben plans to quit on day 20. For his last week he sets his maximum buy price for Ana's coal to
100% of the band (2.5 × base) and builds six cheap Brickworks so that he really uses 800 coal/min. Until then the
money moves from Ben to Ana, which is legitimate. Then Ben stops. The copying Lodger keeps buying 800 coal/min ×
2.5 g × 2.5 = **5,000 g/min, against 2,000 at the hub.** That is 3,000 g/min of **new gold** created for Ana, about
80% of a day-20 income, every day until a human takes the seat. With three alts that "quit", one main gets three
such Lodgers. Even without bad intent, a partner who grew fast in his last week leaves a huge NPC buyer behind, and
the remaining crew can stop growing their own chains because the NPC is easier.

**Verdict:** a successor gets **House prices** (§4.6: buys at 1.25, sells at 2.0) after a notice period, inherits
the **14-day median** volume at 80%, and **decays 10% per week** toward the normal House baseline. It keeps the
light on; it does not freeze a windfall.

### 1.4 My own round-1 levers: tier-3 hub allocation, mastery +15%, and the anonymous station pool

- **Tier-3 allocation (my M8).** Sam plays alone on a 5-player server. In Era 2 a Wonder stage and his steel pipes
  need about 3,000 steel. My rule: the hub sells two hours of one factory's need per day at 2.5, then 4.0. Sam pays
  60% more than a crewed player for the same steel, or builds a ★2 steel chain at ×0.75. His Era 2 is 25–40% slower.
  That breaks the lonely guardian's law 2 ("solo is a full game, humans add +10–15%, never ×2"). **Dropped.**
- **Mastery +1% per 25k units, up to +15% (my M14).** A day-3 copper specialist has mastered copper by day 18; a
  newcomer arriving on day 18 makes copper 15% less efficiently *forever this chapter*. In the Yard and on lines,
  where buyers take the cheapest offer, the veteran wins every buyer. That is the Pokémon Go gym problem.
  **Replaced** by weekly "Supplier of the week" titles in leagues (§4.9): recognition that rotates, no stats.
- **The station pool at 1.4/2.0 (my M4).** In a 4-seat Quad the pool has at most three other members, the same
  people you would link to. It adds a second trading idea for one group and makes trade automatic and anonymous
  ("the water company", lesson A.1 in file 5). **Replaced** by the Yard (consent: you lay the Yard pipe; a ledger
  shows names; one fixed, generous price).

### 1.5 Coordination bonuses that need presence: the shift clocktower (1 #16), the Shift Log coverage bonus (6 #9), the Night Market (6 #29), the season land rush (1 #9)

**Failure scenario.** Kettle Works are three CET office workers who play 07:30, 12:30 and 21:00. The clocktower's
"tended in the last 8 hours" bonus covers about 15 of 24 hours, while a crew spread over three time zones gets
24/24. To keep up, Cleo sets an alarm for 05:00. After a week she resents the game: the Hay Day Derby and FoE guild
battle trap. The Night Market ("one hour per time zone, extra pay for goods delivered *together*") needs two friends
online in the same hour, which the brief rules out (point 7). The 24-hour land rush at the start of a season gives the
best Quads to whoever is awake at the opening, or has twenty alts. A newcomer a week later gets the leftovers
(Ultima Online), and the testers' persistent world can't have a rush anyway.

**Verdict:** no bonus may depend on *when* or *how many at once*. Use totals over a week, banked contributions and
deadlines of at least 48 hours.

*Honourable mentions (weak, not fatal):* adoption of an absent neighbour's estate with a share of its income (1 #30)
gives you a reason to want your neighbour to go dormant. The diversity multiplier ×1.5 for four contributors (2 #7)
makes the missing colour on the ring a public blame and is farmable with alts. Echo trade (1 #32, 6 #17) is
NPC demand pretending to be people, and it is either a faucet (if it pays more than the hub) or pointless.

---

## 2 · The five strongest ideas, and why

| # | Idea (sources) | Why it is strong, economically and for people |
| --- | --- | --- |
| 1 | **Lines, not webs: a two-ended link with a manifest, a gate tank and a status line in words** (1 #21 + #14, 3 §3.1, TradingRethink A) | It removes the cause of the mega-network: trade is a consented, visible pair. The tank absorbs offline gaps, the status line ("Dara is resting; her coal keeps coming") removes the silent dead pipe, and it is cheap to solve (each link is a buyer in one network and a seller in another, like teleporters). It creates **real price signals** between two named sides |
| 2 | **A Quad of 4 seats around a Yard, a crew from Level 1, a Seat Map with complementary seats, seats reserved for friends** (2 #1–#6, 5 #1–#4, 1 #1) | It fits Dunbar's first layer, makes "three friends who join" the default case, and **builds comparative advantage into the seats**: every Quad has an energy, an ore, a water/sand and a material seat, so trade appears without forcing anyone. A fixed friend price can't move gold. It also fits today's geometry (the corner 2×2 groups) |
| 3 | **Houses: named NPC companies with archetypes, which also fill seats and take over from leavers** (3 §1.1, §3.2; 2 #9; 6 #3) | One system covers the lonely player, the empty seat, the reserved seat and the leaver. Economically it is **a controlled faucet and sink with a dial**: their prices and volume caps decide exactly how much better a human is than a bot. The handover ("a newcomer here will be offered your link") gives a newcomer a proven partner in minute one |
| 4 | **Order board, Wishes, bounty pins, with an NPC that fills the rest at the deadline** (5 #8, 2 #8, 1 #20, 6 #10) | It is the async "help me" of idle games (Hay Day crates) and works as a **request for quotes**: it reveals demand and price. Newcomers get easy first wins, nobody is ever let down (the NPC fills the rest at hub price) and nobody needs to be online |
| 5 | **Trunk lines as public works: built in segments with the builders' names, pump houses adopted, a carrier with on-ramps rather than a merged network** (1 #22, 2 #15, 5 #9, 6 Great Pipe) | It is the **growth axis the flat week-3 income curve is missing**, and it is expressed in pipes. It gives a group a multi-day goal with payback, makes logistics a respected role (Eve couriers, Foxhole), and gives solo players a world built by others (Death Stranding). Bounded: a trunk carries routes, it does not merge networks |

Close runners-up: **credit by share of your own output** (3 §5.4), so a Level 6 who gives 20% matches a Level 30 who
gives 20%. **Contributions capped per day and bankable** (5 #9, Clash Clan Games), so no whale carries the group and
a missed day costs nothing. **Leagues of about 25 by stage** (5 #10), so competition can be won at every size.

---

## 3 · Does the economy of a 4–6 estate group work?

### 3.1 What flows where (an example Quad)

Seats as in file 2: Windy Heights (Ana), Coal Valley (Ben), Riverside (Cleo), Copper Gulch (Dev). Every estate also has
★3 iron and small ★2 deposits of everything else.

| Era | Ana · energy | Ben · coal, stone | Cleo · water, sand | Dev · copper | Missing in this Quad |
| --- | --- | --- | --- | --- | --- |
| 1 | ⚡ → all four (every factory needs energy; about a third of an iron ingot's input value) | coal → Dev's Smelter, Cleo's Glassworks; stone → own Brickworks | sand → own Glassworks; water → nobody yet | copper ore → own Smelter; ingots → hub | **wood/planks** (no Old Forest seat) |
| 2 | ⚡ grows with everyone | coal → Ana's Steel Mill; stone → Cleo's Cement Kiln | water → own Cement Kiln; concrete → all (building materials) | ingots → own Wire Mill; wire → Ana (power lines) | **gears** need steel from Ana: the first Era 2 cross-seat chain |
| 3 | | | | | **oil** (no Sunny Flats seat): an outpost or another Quad |
| 4 | | | glass → circuits | wire → circuits | **plastic + rare earth**: circuits need wire + plastic + glass from three seats and one import |

### 3.2 Is there enough to trade with four people?

Rough model: a factory's inputs are about 40% of its output value, and about half of those inputs are goods that
are not your home good. So **about 15–25% of each estate's output value is naturally bought from neighbours.**

- On day 1–7 that is enough to feel: at the Yard price (1.75 × base) a seller gets +75% on that share and a buyer
  pays 30% less, **about +12–18% income**, plus as much XP. It shows up every morning in the report.
- Four estates cover at most 8 of the 10 base goods (energy, water, iron, copper, coal, stone, sand, wood, oil, rare
  earth); a generated Quad covers about 6. **A Quad is never self-sufficient**, by construction.
- **The other 75–85% of each estate's output leaves the Quad** (the hub, other Quads, Wonders, Works). The Yard is
  the warm home, not the whole economy. That is healthy: if it were the whole economy, the crew would be a closed
  box, and a member leaving would be a disaster.

### 3.3 Does it go stale by week 3?

Yes, **on its own it does**, for three reasons:

1. **The estates fill.** From day 14 to 21 the balance run adds 7% more buildings and 2% more income. Internal flows
   are set; nothing new crosses the Yard unless an era changes the recipes.
2. **The crew shrinks.** The typical outcome by week 3 is 2 of 4 active. The internal demand of a Quad is limited by
   its least active members: Ben's resting estate buys the same energy on day 20 as on day 3.
3. **A fixed price leaves no market game.** Friends don't want to haggle (good), but a week-4 trader has nothing to
   optimise inside the Yard.

So the design must **not** ask the Yard to carry the late game. What keeps the Quad alive is what crosses its border:

- **Eras re-wire the flows** (Era 2 adds the first cross-seat chain, gears; Era 4 needs three seats plus an import
  for circuits). Each era is a new Yard puzzle every 7–10 days.
- **Saturation at the hub** (§4.5) pushes week-2+ surplus out: to other Quads, to other districts' hubs, to Works
  and Wonders.
- **Imports the Quad lacks** (wood, oil, rare earth) force at least one line to another Quad by Era 2 and one trunk
  or outpost by Era 3–4.
- **The Yard Works** change their basket each stage, and the top stage needs a sustained flow that a crew of 2–3
  can only reach with a contract from another crew.

### 3.4 How it connects to the rest of the world

```
 Estate ──Yard pipe──► Yard Depot (Quad pool, fixed 1.75)      ring 1: home, friends
   │                       │
   ├──Line (gate↔gate) ────┼──► other Quads, Duos              ring 2: chosen partners, price in the band,
   │                       │                                            loss by distance
   ├──Trunk route ─────────┴──► other districts' hubs, outposts, ring 3: logistics, owned or public trunks
   │                            the Era Wonder, the Port
   ├──Exchange / freight ──────► anyone, slow, 5% fee            ring 4: the far market
   └──Trading Post ────────────► the hub: buys 1.0, sells 2.5    the bank: always there, never best
```

**Verdict:** a 4-seat Quad works as the *first* economy (days 1–10) and as a social home forever. It only works as
a *long* economy if it is deliberately incomplete and leaky: imports it lacks, an appetite limit at its own hub, and
trunk lines that make far trade pay.

---

## 4 · The revised proposal: Yards, Lines and the hub as the bank

### 4.1 On one screen

1. **Where you live:** a district is 4 **Quads** (4 seats around a Yard) and 4 **Duos** (2 seats) around the hub, as
   in today's geometry. You pick a seat on the **Seat Map**; crews reserve seats; Houses hold empty seats.
2. **Four rings, one sentence:** *"Your Yard pays best, chosen partners next, far markets for volume, the hub
   always."*

   | Ring | You sell at | You buy at | With whom | Loss |
   | --- | --- | --- | --- | --- |
   | Yard (human) | 1.75 | 1.75 | your Quad mates (consent: your Yard pipe) | the Yard pipe only (6–20 tiles, 2–6%) |
   | Line | seller sets 0–100% of the band, Auto 1.75 | the same | a company you chose | by length and tier |
   | House (NPC) | 1.25 | 2.0 | Houses in seats, successors | as above |
   | Hub | 1.0 (up to your appetite, then 0.5) | 2.5 | always | none |

   (× the hub's base price of that good. The band is hub buy → hub sell.)
3. **Nothing stalls:** every Yard draw, line and contract falls back to Houses, then the hub, automatically, and says
   so in one line.
4. **Late surplus has to travel:** the hub's appetite for each good *from you* is limited; far hubs, the Port, Works
   and Wonders aren't. Logistics (lines, trunks) is the week-3 game.
5. **Groups build infrastructure, never toll walls:** trunks in named segments, tolls of 3% at most, and the hub takes
   over anything abandoned.

### 4.2 Place: Quads, the Seat Map, crews (from file 2, with economic rules)

- **Seats are generated complementary:** one energy seat, one ore seat, one water/sand seat, one material seat per
  Quad, and the **Quad's missing goods are printed on its card** ("this Quad lacks wood and oil; Fox & Sons next door
  have Old Forest").
- **Home goods:** ★5 (×2.0) and ★4 (×1.5) for the seat's two goods, ★3 iron everywhere, ★2 (×0.75) small deposits of
  every other Era 1 base good. A specialist makes its home good with 2.5× fewer buildings than an outsider. Trade is
  a gain, never a gate.
- **Crew from Level 1** (2–6 companies, name, emblem, colours, invite link). Reserved seats are held **14 days** by a
  House (renewable once). Corporations stay the 30-member layer above.
- **Default placement score** (file 3 §5.1): +3 per active neighbour within 10 levels, +2 per waiting capped link,
  +2 for similar usual hours, +1 per House, −3 for a seat among resting estates only.

### 4.3 The Yard: a pool at one fixed price, with names

- Each estate has a fifth gate, the **Yard gate**. Piping it to the **Yard Depot** (minute 12 in the tutorial, Level 3)
  is the consent: from then on your surplus is offered to the Yard and your consumers can draw from it.
- **Under the hood a pool, not a merged network:** each estate's networks stay its own (own pumps, own losses).
  The Depot is a seller in every buyer's network and a buyer in every seller's, filled from the last solve
  (teleporter-style). Solving it costs O(members) and never grows beyond 8 (a Street).
- **One price, 1.75 × base, both ways, for humans.** It can't move gold: the most a unit can carry from one company
  to another is 0.75 × base above the hub price, and only for goods that were really produced (buying at the hub to
  resell in the Yard loses 0.75 × base per unit).
- **Who gets what when goods are short:** (1) Wishes and standing orders the seller accepted, (2) crew members before
  non-crew Yard members (the seller can switch this off), (3) pro-rata by need, **at most 50% of the pool's supply of
  a good per member while others want it** (no hogging).
- **Where the Yard sits in the flow rules:** own consumers → contracts → **Yard** → lines (by price) → storage keeps
  → hub. A per-good switch "lines first" for traders.
- **Throughput is the Depot's level** (grey 100/min → copper 500 → steel 5,000 per connection): the shared upgrade
  the crew buys together with Crew Orders (file 6 "bottleneck bridges").
- **The ledger:** the Depot shows this week's flows by name and colour ("Ben → Ana 12,400 coal, 21,700 g"). The
  Crew Lens overlay (file 2 #12) shows them as coloured pellets.
- **Streets** (from Crew Level 5, both crews agree): two neighbouring Yards become one pool of at most 8 seats.

### 4.4 Lines: bilateral, chosen, priced, async (from TradingRethink A, files 1 and 3)

- **No network crosses a gate.** A line joins exactly two companies' gates, through corridors only (never through a
  third estate), auto-routed and **non-exclusive** (many lines share a corridor drawn as a bundle, so nobody can
  block a corridor).
- **Price** = the seller's position in the band, **Auto 50% (1.75)**, presets Friend 25% and Premium 75%. It follows
  the hub price by itself. The buyer sets a maximum.
- **Loss by length and tier:** grey 0.3%/tile (next door), copper 0.15% (across a district), steel 0.05% (trunks).
  A trade only runs while it beats the seller's own trading post (as now).
- **Gate tank:** 2 hours of the line's flow on the buyer's side, free. **Notice:** price changes, pauses and
  demolitions that affect a partner take effect after 1 hour.
- **Status in words** (file 3): Live · Resting partner · Caretaker · Handed over · Capped (with "your coal goes to the
  hub meanwhile (−12%)").
- **Trust:** +1 per day of continuous trade up to 10. Each level gives −0.5% loss and +0.5% capacity on that line
  (at most −5% / +5%) and a badge. A pause of more than 24 h halves it. It rewards stable pairs; it doesn't lock anyone in.
- **Contracts v2:** price as a band position, 1–7 days (30 with Futures), auto-renew. When the tank runs dry the
  buyer is fed by the fallback and **the seller pays the difference** between the fallback price and the contract
  price, capped at 10% of the week's contract value. No shortfall clauses below Level 6.

### 4.5 The hub as the bank, with a personal appetite (replaces the global price reaction)

- The hub sells everything at 2.5 and buys everything at 1.0, **steady**, plus its weekly specialty demand (+20% on
  one good).
- **Appetite:** the hub pays full price for each good *from you* up to **0.6 × T(L) per minute of hub value**
  (Level 22: ≈ 1,800 g/min of iron ingots; Level 30: ≈ 2,800 g/min), and half price above it. A Trading Post bar
  shows it ("Ashford's appetite for your glass: 125%") with the three best outlets.
- Each hub has its **own** appetite, so a trunk to another district's hub doubles it. Players, Houses, Works,
  Wonders and the Port have none.
- **Why personal:** nobody can move anybody else's price. Today a veteran flooding iron can push the district's
  iron price down to a quarter for everyone, newcomers included; with a personal appetite that is impossible.
- **Who feels it:** a diversified estate (ore, ingot and wire are different goods, each with its own appetite) almost
  never does; a one-product estate does from about day 10, as a mild nudge (−10% to −20% on that product), exactly
  when trunks and contracts unlock.
- **The Port** (one at each district edge, run by the game): buys any tier 2+ good at **1.1, without an appetite
  limit, but only by a pipe route** (a public or owned trunk). It is the release valve that keeps a quiet server's
  economy from going dead: surplus always has somewhere to go, if you lay the pipe.

### 4.6 Houses (from file 3, with economic dials)

- Named NPC companies, always labelled with a cog, with archetypes: Reliable, Grower, Haggler (1.15 → 1.35 if you
  wait 24 h), Collector (small rare batches at 1.5), Rival (doesn't trade, races you on the boards), Neighbour in
  need (asks for help, pays in XP and a plaque).
- **Prices 1.25 / 2.0** (they keep a margin, they are companies). **Volume: at most 25% of their partner's output of
  that good.** House volume scales with the partner, not the server, so a House next to a Level 30 matters to a
  Level 30 and a House next to a Level 3 doesn't drown him.
- **Solo parity, worked out:** a solo player with three Houses sells about 25% of his output at 1.25 (+6% income) and
  buys inputs at 2.0 instead of 2.5. A crewed player sells about 25% at 1.75 (+19%) and buys at 1.75. **The gap is
  about 10–15% of income** (less in practice, since few players sell a full quarter to humans), the lonely
  guardian's target (+10–15%, never ×2).
- Houses never trade tier 4–5 goods, never take ranks or rewards from humans, and move out for a human with a
  farewell gift and their works handed over intact (file 2 #10).
- **Faucet check:** the 0.25 they pay above the hub is the only new gold they create. It is at most 0.25 × 25% =
  6% of a player's output value, and less where humans trade.

### 4.7 Players who stop: succession without windfalls

| Days away | Their trades | What the partner sees |
| --- | --- | --- |
| 0–3 | normal | nothing |
| 3–14 resting | everything runs at its last size; the estate doesn't grow | "Ben is resting; his coal keeps coming, it doesn't grow" |
| 14–30 dormant | a caretaker keeps the same prices (takes 10%); with the owner's setting, it reinvests up to 30% into upgrades of buildings that feed links (~5% growth a week) | "Ben's caretaker keeps this at the same price" |
| 23 days away | **7-day notice:** "Ben's estate sails to the Quiet Isles in 7 days; Oakridge (a House) will hold the seat and keep 80% of his coal at House prices" | Keep (default) · End · Offer the seat to a recruit |
| 30+ asleep | the estate is packed; a **successor House** takes the seat: House prices, 80% of the 14-day median volume, −10% a week toward the House baseline | "Handed over: Oakridge keeps your coal coming" |
| return | the seat is held for the crew for 60 days by default; the returner lands back in 1 minute; the successor moves out | "Ben is back" |

- **Savings jar** (file 3 §4.2): from day 3, what overflows storage is sold at 70% into a jar, capped at 7 days of the
  player's income. Quitting never out-earns playing.

### 4.8 Async help: Wishes, the order board, tending

- **Wishes / order board:** "200 copper ingots by Friday 20:00 at Auto". Yard mates, line partners or anyone by
  freight can fill it; standing orders fill it while everyone sleeps. **At the deadline a House fills the rest at
  2.0**, so the requester is never stuck and nobody failed anybody. Deadlines of at least 12 h, posts wait 72 h.
- **Tending** (file 3 §2.3): once a day, a visit and a tap on a resting friend's HQ (+2% for a day, a flower bed). It
  costs nothing, it isn't required, and it is the best line of the return screen ("Ana tended your estate 21 times").
- **Housewarming:** in a newcomer's first 48 h each crew member may send one gift of at most **1 hour of the giver's
  own production of one good**. That is tied to real production, so it can't be used as a funnel.

### 4.9 Projects and competition

- **Crew Orders** (3 a day at the Depot): each is at most **45 minutes of one member's output**, so one player can
  always finish it. Each contributor gets their own reward; there is no multiplier for how many people helped.
- **Yard Works** (file 2 #7 + my round-1 M12), one per era:
  - **Stages are baskets** of 3–4 goods from different home goods (Foundry Hall I: 40% bricks, 30% iron ingots,
    30% glass), sized to **6 h of T(L) × active members** (Houses count 0.5 and deliver a little by themselves).
  - **The top stage is a sustained flow:** "a flow worth half the crew's combined output for 6 hours", which in
    practice needs a contract with another crew or a trunk route. Storages piped to the Works count as flow, so
    you can prepare.
  - **Contributions are capped per day and can be saved up for 3 days** (no whale carries it, a missed day costs
    nothing). **Credit is your share of your own output** (a Level 6 giving 20% of their output = a Level 30 giving
    20%). The goods that complete a stage's missing good get +20% credit (keystone). Hub imports count 25%.
  - **The reward is a perk for everyone in the Quad** (a public good: a non-contributor costs nobody anything), plus a
    plaque in the contributors' colours.
- **District and Era Wonders:** unchanged, but personal tiers use the share of your own output (above), and the
  Era Wonder is fed by trunk routes (the Great Pipe of file 6 as a **ledger** of routes, drawn as one growing spine
  on the world map, not one simulated network).
- **Leagues of ~25 companies of a similar stage** for weekly contests (most glass, growth this week, tile efficiency);
  a **Station Cup** per Quad counting the best 3 days per active member; weekly "Supplier of the week" titles that
  rotate. On a small server the league includes Houses (labelled) and the Rival.
- **Events:** shortages (one district, 48 h, a good's hub price ×2) and Discoveries (a ★5 wilderness deposit),
  always announced 6 h ahead.

### 4.10 Trunk lines: logistics as the late game

- **Built in 10-tile segments** by anyone (a crew, a corporation, a solo player). Each segment carries its funder's
  colour and name. A steel segment costs **about 2 h of the funder's T(L) in gold** plus 300 steel and 100
  concrete. A pump house every 25 tiles can be adopted (its level, its banner); a pump nobody adopts still works at
  base level.
- **A carrier, not a network:** a trunk joins **on-ramps** (Yard Depots, gates, outposts, hubs, the Port, the Wonder
  site). Using it means opening a **route** from on-ramp A to B, which carries an ordinary line or a delivery
  to a hub, the Port or a Wonder. Trade stays between two named sides; the trunk owner is a named third party on the
  route's panel.
- **Capacity by tier**, owners' routes first, renters share the rest pro rata (shown before you connect).
  **Toll 0–3% of the value carried**, set by the owners with 24 h notice. **Upkeep** 0.5% of the build cost per
  week, paid from tolls first.
- **Abandoned** (all owners dormant 14 days, or the owners dissolve it): the hub buys it at 50% and runs it as a public
  line at 2% (a gold sink). A line never dies and never strands its renters.
- **Public hub trunks:** every hub has copper-tier public trunks along its main corridors to the neighbouring hubs
  and to the Port, at 2%. Solo players can always reach far markets; player trunks are better (steel, toll-free for
  owners).
- **Why build one:** a second hub's appetite and its weekly specialty demand, outposts (Era 3 oil, Era 4 rare earth
  are only in the wilderness), Era Wonder deliveries, other crews' contracts, toll income, and the name on the map.

### 4.11 Faucets and sinks

| Faucet (new gold) | Bound |
| --- | --- |
| Hub buys at 1.0 | appetite per good per hub |
| Houses' +0.25 | 25% of the partner's output |
| The Port at 1.1 | only by pipe route, with loss and toll |
| Savings jar | 7 days of income at 70%, once per absence |
| Orders, Works and Wonder rewards | scaled by T(L) |

| Sink (gold destroyed) | Size |
| --- | --- |
| Hub sells at 2.5 | every import |
| Houses' sell margin (2.0) | per House trade |
| Trunk segments, pump houses, Depot upgrades | ~2 h of T(L) per segment, the big late sink for the gold that piles up (the typical run holds 17M gold on day 7) |
| Public-trunk tolls (2%), Exchange fee (5%), caretaker fee (10%) | per use |

---

## 5 · Play-throughs

Notation: gold per minute (g/min). T(L) from the typical balance run. Prices × base.

### (a) Three friends start tonight, Ben quits on day 3, Dev joins on day 8

**Evening 1, 20:00–20:25**
1. 20:00 · Ana taps **Start with friends**, names the crew Kettle Works (kettle emblem, teal and copper) and shares
   the link in the group chat.
2. 20:03 · Seat Map: Ashford NE, a fresh Quad next to an active crew (Fox & Sons), hub specialty glass +20%, card
   "lacks wood and oil". Ana takes Windy Heights, Ben Coal Valley, Cleo Riverside. Copper Gulch shows "reserved for a
   friend, held by Bertha's Brass (House)". **Settle**: three balloons land.
3. 20:05–20:12 · Each one's own tutorial: wind, iron mine, factory, trading post. Everything sells at the hub at 1.0.
   Nothing depends on the others.
4. 20:12 · Level 3, Loady: "Pipe your Yard gate to the Depot (6 tiles)". Ana first, then Ben: the **Handshake**
   (both pipes flash, 200 g each). Ana's spare 30 ⚡/min now goes to Ben's iron mine at 1.75 instead of the hub's
   1.0. Ticker: "Ana sold Ben 30 ⚡/min: +22 g/min over the hub". Bertha buys 10 ⚡/min at 1.25.
5. 20:19 · Crew Order 1 (200 stone, 100 planks, 300 ⚡). Ben's quarry, Cleo's beach forest and Ana's wind finish it in
   six minutes. Crew Level 2, the Water Tower rises in the Yard.
6. End of the evening: each makes about 300–500 g/min; the Yard carries ~10% of their output, worth +6–8% income. It
   is a nice extra, not a necessity.

**Day 2**
- Morning report, crew section: "Ana's energy kept Ben's mine running all night (14,000 ⚡ at 1.75)".
- Level 6: Cleo builds a Glassworks (sand ★5, Ana's energy), Ana a Smelter (copper ore from Bertha at 2.0, coal from
  Ben at 1.75). Cleo posts a Wish "coal for my Glassworks, 40/min"; on the bus Ben taps **send spare coal to Cleo
  first** (a standing order).
- By evening the Yard carries ~20% of each one's output: **+12–15% income** for each, the first chain across three
  estates (Ben's coal + Ana's energy → Cleo's glass, sold at the hub's +20%).

**Day 3 · Ben stops** (last session 22:00; nobody knows yet)
- Days 3–6: Ben's estate runs at its day-3 size. His 120 coal/min keep flowing into the Yard, his energy purchases from
  Ana continue. Nothing changes for anyone.
- Day 5: Ana builds a second Smelter (+80 coal/min). The Yard has no more coal (Ben doesn't grow). The pool shares
  Ben's 120 by need (Ana 70, Cleo 50); the missing coal comes from the fallback, Bertha (2.0) then the hub (2.5).
  Status line: "Ben is resting (2 days). His coal keeps coming but doesn't grow. 50 coal/min come from Bertha and
  the hub." Ana posts on the order board: "80 coal/min for 7 days at Auto". Finn of Fox & Sons (Coal Valley, next
  Quad) takes it as a **line** (22 tiles, 6.4% loss). That is Ana's first trade outside the crew, caused by the crew
  shrinking, not by a tutorial.
- Day 6: a sleeping cat on Ben's steps. Ana tends his estate (a flower bed, +2%). Crew Orders are sized to two
  active members plus Bertha (0.5).

**Day 8 · Dev joins**
- Dev opens the old link. The lobby shows his seat and the crew's need: "the crew buys 1,100 copper a day from Bertha
  and the hub". **Settle:** Bertha leaves a cake and a note; her Copper Mine (Level 5), Smelter (Level 3) and two wind
  generators become Dev's, intact.
- Dev's first hour: his copper replaces Bertha's. Ana and Cleo pay 1.75 instead of 2.0 (−12.5%), Dev sells at 1.75
  instead of the hub's 1.0. About 60% of his output goes to the Yard, so **Dev's income is about 1.45× a solo
  day-1 player's**. He is the missing piece, not the burden.
- **Tailwind:** XP ×2 (late-start factor up to ×2.5) until he is within 5 levels of the crew's active median (Ana 19,
  Cleo 18). Parcels at half price up to the crew's median count. Dev is Level 13 on day 12 and Level 20 on day 16.
- **Housewarming:** Ana sends 1 h of her energy and Cleo 1 h of her glass.
- Day 9: Era 2. Dev's Wire Mill becomes the crew's wire source; Cleo's Cement Kiln uses Ben's resting stone flow. The
  crew's first gears need Ana's steel: a new Yard flow each era.

**Days 17–33 · Ben's absence, handled** (he last played on day 3)
- Day 17: Ben is dormant; his caretaker keeps the same prices and takes 10%. Ben opted into autopilot when the crew
  was founded, so 30% of his income goes into upgrading his coal pits (+5% a week).
- Day 26: the notice for everyone linked to Ben: "Ben's estate sails to the Quiet Isles on day 33. Oakridge (a House)
  will hold the seat for the crew and keep 80% of his coal at House prices." The crew keeps the seat held (default
  60 days).
- Day 33: Oakridge takes the seat. Ana now pays 2.0 instead of 1.75 for the 96 coal/min it delivers, and it shrinks
  10% a week. Ana knew this a week ago, and Finn's line already covers her growth.

**What the three active friends had:** a shared place from minute 5, a shared pipe from minute 12, a goal from
minute 15, no stall when Ben left, and a late friend who was immediately useful.

### (b) A solo player on a server of 5 players

The server is one district (the Capital): 4 Quads and 4 Duos, 24 seats. Humans: a couple, Lea and Max (crew, Quad
NW), Mo (Level 14, plays mornings, Quad NE), Kai (Level 20, a Duo), and Sam, who starts tonight. Houses fill seats
up to the liveliness target (at least 12 of 24 seats alive): 10 Houses.

1. **Minute 0.** Sam taps **Start alone**. The Seat Map shows lit windows: NE "Mo (played today, mornings) + 2
   Houses + 1 free seat (Windy Heights); Mo needs energy". NW "Lea & Max (evenings), open seat: Riverside". SW and SE
   Houses only. **Pick for me** suggests NW (similar hours). Sam picks NE himself because Mo needs energy.
2. **Hour 1.** The same tutorial as everyone. At Level 3 he pipes his Yard gate: Mo's mine draws 20 ⚡/min at 1.75,
   Grower House "Brassworth" 10 ⚡/min at 1.25. Day-1 income +10% over a pure-hub start.
3. **Day 2.** A letter: "Brassworth: Your energy built our new hall. Next week we want twice as much." The hub town
   (file 3 §1.2) shows the oven built with his bricks. Mo's Wish "glass 30/min" is one Sam can't fill (no sand
   seat); Lea fills it by line.
4. **Day 5 · Mo stops.** Mo's estate keeps buying the same energy for 30 days; status "Mo is resting". Sam's Yard is
   now a resting human and two Houses. Nothing breaks; Sam's growth goes to the hub and the Houses.
5. **Day 8.** Level 8: Sam joins the **Guild of Independents** (an AI-led corporation for the district, file 3 §1.4);
   Kai is in it too. The weekly goal is sized to its active members.
6. **Day 10 · appetite.** Sam's iron ingots: he sells 2,400 g/min of hub value, his appetite is 0.6 × 3,000 = 1,800.
   The 600 above it sells at half price (−300 g/min, −10%). The Trading Post bar shows three outlets: Brassworth
   (takes 25% at 1.25), Max's Steel Mill (needs ingots: a 60-tile copper line, 9% lost, Auto 1.75 → 1.59 net), the
   Port by the public hub trunk (1.1 − 2% toll − 4% loss). Sam lays the line to Max: his first human partner since Mo.
7. **Week 2.** The Yard Works in NE is sized for 1 active member + 2 Houses × 0.5. Sam finishes stage 1 on day 11
   with the Houses' small deliveries (+10% energy for the Quad's members).
8. **Week 3.** The estate is full and income is flat (as in every balance run). Sam's growth: a solo trunk of 8
   segments (about 16 h of his income) from his gate to the Port and the Capital's Era Wonder site, carrying his
   ingots past his appetite. The segments carry his colour; Kai rents a route on it (2% toll). On a 5-player server
   **Sam's own trunk becomes the server's main road**, with his name on it.
9. **Competition:** his league is the 5 humans plus the Houses and the Rival (Kestrel Industrial, always 5–10% ahead
   or behind). He wins "Growth this week" in week 2 and the NE Quad's Station Cup (best 3 days per active member).

**Solo gap:** about 10–15% lower income than a crewed player, no locked content, a full late game. What he misses
is warmth, which is what an invite link is for.

### (c) A newcomer in week 3 among veterans

Nia starts on day 18 on a server of about 400 players. Ashford is full of crews at Levels 25–30.

1. **The Seat Map** offers: (1) **open seats** in veteran Quads (crews that opened a House-held seat), e.g. "Fox &
   Sons, Crew Level 6, 3 active, evenings CET, looking for Riverside; host: Finn"; (2) the **Frontier district**
   opened this chapter (newcomers, returners, Houses, a fresh hub). Nia picks Fox & Sons.
2. **Minute 5.** She lands in the House's seat with its works (a Glassworks at Level 5, a Water Pump, a Sand
   Dredger). The House leaves a note. The Fox crew gets a "Welcome Nia" card; Finn sends 1 h of his energy.
3. **Minute 15, Yard pipe.** The veterans' Yard demand for sand and glass is about 50 times her output, so **all
   her surplus sells at 1.75 immediately**; the 50% cap doesn't limit her as a seller. Her day-1 income is about 1.5×
   a solo day-1 player's. She is the glass the Quad lacked, not the late kid.
4. **Protection:** below Level 10, with anyone 10+ levels higher, she always sells at Auto or better and buys at Auto
   or cheaper. No shortfall contracts below Level 6. Offers from players 10+ levels higher are rate-limited for her
   first 3 days. Her hub price is hers (appetite is personal), so no veteran's dumping touches it.
5. **Catch-up:** late-start XP ×(1 + 18/14) = ×2.3 until she is within 5 levels of the active median. Level 15 on day
   22, Level 22 on day 26. Tech diffusion as today.
6. **Projects:** Fox's Yard Works stage 3 needs 15% glass. She gives 20% of her glass for three days, so her credit
   is 20% of her own output, the **same tier as Finn's 20%**. She delivers the missing glass (keystone +20%) and her
   colour is on the plaque.
7. **Competition:** her league is "founded in the last 14 days"; she is 2nd in "Growth this week".
8. **What could still go wrong:** she sells all her sand raw because the Yard pays so well, and learns chains later.
   That is fine: she levels fast, and the Era 2 tasks steer her to process.

### (d) A griefer who tries to block or exploit others

Gary, Level 24, a crew of one plus alts.

| Gary tries | What happens |
| --- | --- |
| **Block a corridor** with pipes so a rival's line can't pass | Lines are auto-routed and non-exclusive; corridor tiles can't be owned. Nothing is blocked |
| **Dump 10,000 iron ingots/min on the hub** to crash a newcomer's price | Appetite is personal: Gary's own price halves above his appetite, nobody else's moves |
| **Funnel gold with alts:** alts sell him everything at 0% of the band, or buy his goods at 100% | Value per unit is capped at 1.5 × base and only for produced goods; buying at the hub to resell loses money. An alt is worth exactly one real partner's output, and costs Gary the time to play it. Links between accounts from the same device are flagged and held at Auto; no gifts except 1 h of own production for crew members below Level 10 |
| **Farm successors:** an alt builds big demand at 100% band, then quits | The successor buys at House prices (1.25 / 2.0), 80% of the 14-day median, −10% a week. The farm earns less than trading with the alt while it was active |
| **Join an open seat in a friends' Quad and hog the coal** (10 Brickworks) | He can draw at most 50% of the pool while others want it, crew members come first (Gary isn't in their crew), Wishes jump the queue. And he pays 1.75 for every unit, which the seller is happy about |
| **Toll gouging** on a trunk he built across the only corridor to the Port | Toll at most 3% with 24 h notice; anyone can build a parallel trunk (non-exclusive); public hub trunks exist at 2% |
| **Sudden cut-off at night** (pause the line his neighbour depends on) | 1 h notice, a 2 h gate tank, automatic fallback, and as a contract seller he pays the difference (capped at 10%) |
| **Order-board traps:** post at 100% band and fill with his alt | The same per-unit cap; orders filled by Houses at the deadline pay 2.0, not Gary's price |
| **Works credit farming:** buy from the hub and deliver | Hub imports count 25%; contributions capped per day; credit is a share of *own output* |
| **Spam** link requests and Wishes | 5 requests an hour, block list, Closed gates, Wishes expire quietly |
| **Sandbag** to win the rookie league | Leagues are by founding date and level band; the rewards are cosmetic; staying low costs progress |

Residual risk: multi-accounting still gives "one extra friend" per alt. Accepting that, with every alt costing real
playing time, is cheaper than an arms race.

### (e) A player who returns after a month

Ben stopped on day 3 and opens the game on day 40 (asleep since day 33).

1. **Before:** at most three messages (day 3: "Your Brickworks earned 4,200 g, Ana is still building next to you";
   day 12: "Ana tended your estate 6 times"; the chapter start).
2. **Screen 1, while you were away (5 items):** Era 3 has begun; his estate earned what it earned until day 33;
   **savings jar** about 7 days × 1,440 min × 1,100 g/min × 0.7 ≈ **7.8M gold**; Ana tended 21 times; Oakridge kept his
   seat and his coal; Dev joined the crew.
3. **Screen 2:** his estate on its island, complete.
4. **Screen 3, where to land:** (1) **his seat in Kettle Works, held by the crew** (recommended), (2) next to an old
   partner, (3) a lively seat, (4) the Frontier district. He taps (1). Landing takes 1 minute. Oakridge leaves a
   note; his Yard pipe reconnects; Ana's coal comes from Ben again at 1.75 (she saves 12.5% against Oakridge). His
   trust with Ana is kept at half.
5. **Catching up:** Ben is Level 10, the crew median 30. Tailwind ×2.5 until Level 25. **Era jump kits** for Eras 2
   and 3: a free blueprint of a standard chain, a crate of its first building, and the first 3 techs 25% cheaper than
   diffusion. They never expire.
6. **Economic check:** 7.8M at Level 10 buys him 2–3 parcels and his first Steel Mill, a gift. An active Ben would
   have earned about 150M in those 37 days, so quitting never pays.
7. **If the crew had given his seat away:** he lands in the nearest free seat of their Street or the Quad next door,
   and the old Yard trade becomes a line to Ana in one tap (partner history).

### (f) The late game in week 4

Kettle Works on day 22–28, Era 3: Ana Level 30, Cleo 29, Dev 24, Oakridge in Ben's seat. Their estates are full
(12 parcels). Income is about 4,500 g/min each, flat since day 14.

1. **The nudge:** Cleo sells 3,500 g/min of glass to the Ashford hub; her appetite is 0.6 × 4,700 = 2,800. The
   700 above it earns half, a visible "125%" on her Trading Post. Ana's steel is at 107%, Dev's wire at 90%.
2. **The Discovery** (announced 6 h ahead): an oil ★5 field 70 tiles into the wilderness. Era 3 refineries need oil,
   which the Quad lacks. Cleo claims it as the crew outpost (Crew Level 6).
3. **The Kettle Line:** a steel trunk from the Yard Depot to the oil field (70 tiles) and on to the Riverside
   district's hub (100 more tiles): 17 segments of about 2 h of T(L) each, **≈ 9.6M gold in total** (about 12 hours
   of each one's income) plus 5,100 steel and 1,700 concrete, delivered through Crew Orders over 5 days. Ana funds
   7 segments, Cleo 6, Dev 4 (with his wire profits). Ana and Dev adopt the pump houses. The ribbon across the map is
   teal, copper and amber.
4. **What it pays:**
   - Cleo sends 100 glass/min (1,400 g/min of hub value) to Riverside's hub, which has a fresh appetite and +20% for
     glass this week. After 8.5% loss over 170 tiles: ≈ 1,540 g/min, against 700 at half price at home:
     **+840 g/min, about +18% of her income.**
   - The oil feeds Ana's refinery (plastic) without a hub import at 2.5.
   - Fox & Sons rent a route for Finn's coal to Riverside (2% toll on about 3,000 g/min of value): about 60 g/min,
     shown as "Kettle Line earned 600k this week". Small money, big pride.
   - Together (Cleo's +840, the oil Ana no longer imports, the tolls) about 2,000 g/min: **payback about 3–5
     days**, a real week-4 investment decision.
5. **The Works' top stage, Foundry Hall III:** a sustained steel flow of half the crew's output value for 6 hours.
   Ana's mill makes about 60% of it. The rest is a **48-hour contract with Fox & Sons** at 60% of the band, delivered
   over the Kettle Line, and buffered in storages piped to the Works beforehand. Nobody has to be online during the
   6 hours. The perk (+10% on one chain for the Quad) and the plaque in four colours (Oakridge's is grey).
6. **The Era Wonder** (the Grand Refinery Tower in the Capital): Kettle opens a route over the public hub trunks to
   the Capital and appears on the Great Pipe spine with a count of its deliveries. Each of them gets the tier their
   own share earns.
7. **Competition:** the Station Cup in a league of 25 Quads of the same stage: Kettle is 3rd, and a bronze banner
   flies over the Yard for a week. "Supplier of the week: Cleo, glass, Ashford."
8. **A shortage** (Riverside, copper ×2 for 48 h, announced 6 h ahead): Dev sends his 50,000 stored copper ingots over
   the Kettle Line, his best day so far.
9. **What a 5-minute week-4 session looks like:** the morning report (line income, the Works stage, a Wish from Fox),
   fund one segment, accept a contract renewal, thank Finn for the steel. The next session has a reason too: the
   next segment.

---

## 6 · Score against the brief's checklist (0–3)

| # | Point | Score | Why |
| --- | --- | --- | --- |
| 1 | Friends together | 3 | A crew at Level 1, a Quad with seats reserved 14 days, the Yard pipe at minute 12, Dev's furnished seat and Tailwind |
| 2 | Choosing where you live | 2 | A Seat Map with lit windows, land, needs and missing goods, plus a default score; but seats are fixed by geometry (4 Quads + 4 Duos per district) |
| 3 | The lonely player | 2 | Houses, the Guild, public trunks and the Port give a full game at ~10% less income; the warmth depends on how well the Houses are written |
| 4 | Players who stop | 3 | Nothing stalls; caretaker, 7-day notice, a successor without a windfall, the seat held, a 1-minute return |
| 5 | The early game | 3 | Nothing needs others; one fixed Yard price, no settings; no penalties below Level 6; the price band protects newcomers |
| 6 | The late game | 2 | Appetite, trunks, outposts, Works' sustained stages and cross-crew contracts make week 3–4 grow; weeks 5+ and a crew of one are still thinner |
| 7 | Asynchronous idle play | 3 | Tanks, standing orders, deadlines of 12 h+, NPC fill, banked contributions; no action needs two people online |
| 8 | Fair and safe | 2 | Fixed Yard price, band caps, personal appetite, toll caps, no windfall successors; an alt is still worth one friend |
| 9 | Clear | 2 | One sentence for the ladder, status lines, ledgers, colours; but four price rings plus appetite is a lot to learn (taught one ring at a time) |
| 10 | Pipes are the heart | 3 | Yard pipes, lines, trunk segments, pump houses, the Great Pipe spine: every relationship is a pipe on the map |
| 11 | Scale and performance | 3 | Pools of at most 8, bilateral lines, trunks as route lists, Houses only where needed, no merged networks |
| 12 | Seasons and a persistent world | 2 | Crews, trunks and seats persist; migration exists (below); carving Yards from the inner-corner parcels touches testers' layouts |
| 13 | Social glue | 2 | Names, colours, ledgers, plaques, segments with names, tending, leagues; mostly borrowed from files 2, 3 and 5, and chat stays light |
| 14 | Buildable | 2 | About 16–19 developer-days in 8 steps, each playable; the riskiest parts are Houses' growth and trunk routes |
| | **Total** | **34 / 42** | |

---

## 7 · Build order (each step playable)

| Step | What | Builds on | Days |
| --- | --- | --- | --- |
| 1 | **Lines:** networks stop at gates, bilateral links, band-position prices, gate tanks, status lines, hub fallback | TradingRethink A, teleporters | 2 |
| 2 | **Quads:** Seat Map, crews at Level 1, reserved seats, the Yard gate and Depot pool at 1.75, ledger, migration of corner groups | district geometry, blueprints | 3 |
| 3 | **Houses v1:** named tenants with archetype prices (1.25 / 2.0), volume caps, seat holding, works handover, successor rules with notice and decay | tenants, Quiet Isles | 2 |
| 4 | **The bank:** personal appetite per good per hub (replacing the global price reaction), the Port, public hub trunks (copper, 2%) | hub market | 2 |
| 5 | **Async help:** Wishes and order board with House fill, contracts v2 (difference instead of penalty), tending, housewarming | contracts | 2 |
| 6 | **Projects:** Crew Orders, Yard Works with baskets, sustained stage, banked caps and credit by own-output share | Wonder code | 2 |
| 7 | **Trunks:** segments with names, on-ramps and routes, tolls, upkeep, pump houses, hub takeover | lines | 3 |
| 8 | **Competition and events:** leagues by stage, Station Cup, Supplier of the week, shortages and Discoveries | leaderboards | 2 |

**Migration for the testers' world:** corner groups of four become Quads; existing buildings on the inner-corner
parcels stay where they are and count as their owner's Yard buildings until moved; corridor pipe chains between two
gates become lines and the others are refunded (TradingRethink A); tenants become Houses; global hub prices reset to
base, with appetite starting from each company's level.

## 8 · What to measure in the first test week

| Metric | Target | If it misses |
| --- | --- | --- |
| Share of output traded with humans per active player | 15–30% | Below: Yard price or seats not complementary enough. Above: the hub is too weak |
| Income gap, solo with Houses vs crewed | 10–15% | Tune the House prices (1.25 / 2.0) and the 25% cap |
| Time to first Yard trade | under 20 min | the tutorial step |
| Lines in "Capped" or "dead" states | under 5% | successor rules |
| Players above 100% appetite on day 10 | 20–40% | tune the 0.6 × T(L) |
| Trunk payback in week 4 | 3–7 days | segment cost (2 h of T(L)) |
| New gold from Houses and the Port | under 8% of all faucets | volume caps |

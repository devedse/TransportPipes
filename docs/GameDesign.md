# Transport Pipes — Game Design

This is the game as it is built: version 1.4, "Your own place". Why it is this way, the economy model behind its
numbers and the history of each choice are in `docs/v1.4-design.html` (built from `docs/design-notes/v1.4`, with the
model in its `sim/`). What is built when, and what each playtest found, is in `docs/ImplementationStatus.md`; the look
in `docs/ArtDirection.md`.

The numbers of all five Eras come from the model of `docs/design-notes/v1.4/sim` (Eras 3 to 5 played out in phase C).

## Contents

1. [The game in one minute](#1-the-game-in-one-minute)
2. [Seven rules](#2-seven-rules)
3. [Words](#3-words)
4. [The world: plot, Yard, town](#4-the-world-plot-yard-town)
5. [Your land](#5-your-land)
6. [Starting](#6-starting)
7. [The Yard and your crew](#7-the-yard-and-your-crew)
8. [Trading](#8-trading)
9. [Buildings, levels and pipes](#9-buildings-levels-and-pipes)
10. [Goods and chains](#10-goods-and-chains)
11. [Eras](#11-eras)
12. [Research](#12-research)
13. [Wonders, Monuments, the Launch and missions](#13-wonders-monuments-the-launch-and-missions)
14. [Company Level](#14-company-level)
15. [The helpers](#15-the-helpers)
16. [The wilderness and other towns](#16-the-wilderness-and-other-towns)
17. [Away, moving and leaving](#17-away-moving-and-leaving)
18. [Everything else](#18-everything-else)
19. [The numbers](#19-the-numbers)

## 1. The game in one minute

- **You pick a plot.** The start screen is the map: you click a free plot of 32×32 tiles in a town still in Era 1,
  next to a friend or wherever you like.
- **You build from nothing.** Wind Moilies and solar panels make energy, mines dig ore, factories turn it into ingots,
  bricks and glass. Pipes connect them, and goods flow every minute, also while you're away.
- **You sell.** Your trading post sells to the town's market. The market pays the full price for an amount of each good
  a minute (your Hub Order) and less for floods; it also sells everything, at 2.5× the price.
- **You trade with your Yard.** Your plot touches a Yard shared with three neighbours. A pipe to your Yard gate is all
  it takes: what a neighbour lacks is filled from your spare at the price you set, and the other way round.
- **Nobody has everything.** Each plot lacks one Era-1 basic and one later resource; the four plots of a Yard lack
  different ones.
- **You get very good at a few things.** Research tracks take days; a master of the Iron track makes fifteen times the
  iron of someone who didn't, and the market buys more from a master.
- **Your town moves on together.** Sixteen players share a town, its market and its Era Wonder. When the Wonder is
  finished, the whole town enters the next Era. After five Eras the town launches its Space Elevator, then flies
  missions without end.

## 2. Seven rules

When two ideas fight, the rule higher in the list wins.

| Rule | What it means |
| --- | --- |
| 1 · Fun alone, better with friends | A solo player can reach every Era and finish every track. Friends who split the work get there sooner and earn more. Nobody is stuck because a friend stopped playing. |
| 2 · One balance at every size | No number depends on how many people play: not on the server, the town or the Yard. More people get further because more people make more. |
| 3 · Only people | No computer-run companies. The market is the one thing that isn't a person. |
| 4 · Your own place | You choose where you live, start from nothing, and nobody can touch what you built. |
| 5 · Everything flows | Goods visibly move through pipes; you can tell what a plot does by looking at it. |
| 6 · Choices that matter | Research, land, who in the Yard makes what and how much you give the Wonder change how your game goes. |
| 7 · Progress while away | Everything runs offline. Coming back is a reward; nothing runs out while you're away (no rents, no decay). |

## 3. Words

| Word | Meaning |
| --- | --- |
| Plot | Your land: 32×32 tiles in 16 parcels of 8×8. You start with the four against the Yard. |
| Yard | The 32×16 space between four plots. Its residents trade there, build Monuments and workshops there and own it together. |
| Crew | The people who live around one Yard (one to four). |
| Yard gate | Where your plot meets the Yard; a pipe to it connects you to the Yard. |
| Town | Four Yards (16 plots) around a hub: one market, one Era, one Era Wonder. The first town is the Capital. |
| Market | The town's buyer and seller of everything. |
| Hub Order | What the market buys from you a minute at the full price, per good. |
| Outskirts | The land between a town's Yard groups; from Era 2 each plot can buy the nine parcels set aside for it. |
| Era | A stage of a town: Steam & Iron, Electric, Oil & Machines, Electronics, Space Age. Your own Era is the highest your town reached while you lived there. |
| Era Wonder | The town's building project; finishing it brings the next Era. |
| Monument | A Yard's own project, one site per Era, built with gold in stages without end. |
| Track | A research line for one chain, nine tiers over about ten days. |
| Base price | A good's reference price; every price in the game is a multiple of it. |

## 4. The world: plot, Yard, town

- **A Yard group**: a Yard of 32×16 tiles with a plot above, one below and one at each end. Every plot's start area
  (16×16, its first four parcels) lies against the Yard with the Yard gate in the middle of that edge.
- **A town** (196×164 tiles): four Yard groups around the hub (68×52: the market, the Era Wonder's site, past Wonders).
  Two roads cross the town; links between Yards run along them. Between the groups lie the outskirts. Where the roads
  leave the town stand four town gates, where trunk lines arrive.
- **The world**: towns spiral out from the Capital, 128 tiles of wilderness apart. A new town opens when every Era-1
  town has fewer than two free plots, so a newcomer always finds room in Era 1.
- **The world map** shows every town's name, Era, Launch and stars (missions done): the race between towns.

## 5. Your land

- **What a plot has**: every plot has water, iron ore, wood, wind and sun. It lacks one of copper ore, coal, stone and
  sand, and one of bauxite (Era 2), oil (Era 3), rare earth (Era 4) and titanium ore (Era 5); a Yard's four plots lack
  different ones. Later Eras' deposits are on the land from the start, sleeping. Each plot has one rich (★4) deposit,
  its strength; the others are ★1 to ★3. Richness multiplies what a mine digs.
- **Terrain**: grass; hills (wind ×1.2–1.6, stone, rare earth); forest (wood, regrows around Lumber Camps); beach
  (sand); water (pumps and wheels on the shore); desert (sun ×1.5, oil); red hills (bauxite); mountains (ore at their
  feet, titanium ore; nothing is built on them and pipes don't cross them).
- **Parcels**: 16 of 8×8. The other twelve are bought in any order next to land you own: the first for 20,000 gold,
  each 2.5× the last; the 5th parcel at Company Level 5, then one more every second level, the 16th at Level 27. Pipes
  may cross parcels you haven't bought.
- **The outskirts** (from Era 2 and the research Survey the Outskirts): nine 8×8 parcels set aside for each plot, in
  any order, 100 million gold × 1.4 for each one owned. They hold every land resource at ★2–★3 and count as your plot
  for building and pipes. On a move the market buys them back for half; what stood there goes into crates.
- **More room later**: the Yard's ends (Level 10), the Deep Level under your plot (Era 3), outposts in the wilderness
  (Era 4).

## 6. Starting

- **The map** opens zoomed out on the real world: free plots glow in Era-1 towns; a plot's card says what its land has
  and lacks, its strength, its Yard (who lives there, open or closed, its Monuments) and its town.
- **Pick for me** chooses a free plot in an open Yard whose residents played in the last three days. **Find a player**
  flies to a company by name. A friend's **Play next to me** link (seven days) opens their Yard and lets its first user
  in even when the Yard is closed; a closed Yard otherwise takes a request (three days).
- **You start** with an HQ, a trading post by the Yard gate and 2,000 gold. Loady guides the first hour one step at a
  time; the Next steps card shows the three best things to do.

## 7. The Yard and your crew

- **The crew** is everyone around a Yard. The first settler names the Yard, picks its banner and chooses open or
  closed; every resident is an owner. Nobody can be thrown out. The last one out takes the crew with it: the Yard's own
  buildings (the Depot, the Monuments) are torn down.
- **Trading in the Yard**: a pipe to your Yard gate connects you. Every minute the Yard fills what a neighbour's
  buildings lack from your spare at your crew price, and the other way round, offline too, with no fees. When goods are
  short, buyers keep the share they had over the last seven days. Your stall at your gate shows what you sell, your
  crew price and what you want.
- **Workshops** (from Level 10): up to 64 tiles of the Yard's ends for your own producers and Storages, joined to your
  Yard gate without pipes, so the Yard brings them what they lack and takes their spare. They move with you.
- **The Depot** (from Level 10, one per Yard, 20,000 units): you put goods in for the crew; a neighbour's levels and new
  buildings take them at your crew price, and the Wonder takes them as your gift. When you move, your goods come back
  into your Storages.
- **Monuments**: one site per Era in the Yard's square (§13).
- **A Yard of one** has all of it; nothing is resized for one (rule 2).

## 8. Trading

- **The market**: your trading post sells everything that reaches it. Up to your Hub Order a minute it pays the base
  price; beyond it the price falls with the square root (base × √(Hub Order × sold), measured over the last hour), and
  beyond 16× the order a quarter of base. It sells at 2.5×. A good of a later Era than the town's pays a quarter.
  - Hub Order = 1,000 g a minute of an Era-1 good at base, ×4 for each later Era, ×1.5 for every Era the town is past the
    good's, ×1.1 for every trading post level, ×1.8 for every tier of the good's track beyond 4, and the perks of the
    town's Wonder, a Market Monument (Clock Tower) and Trader Lin.
  - The trading post has no unit limit: its level raises the Hub Orders instead.
- **Your own prices** (between 1× and 2.5× the base): a public price (1.3× by default), a crew price (1.15×), per good,
  and special prices for named players; the lowest that applies is paid. A cut counts at once, a rise after an hour.
- **Links**: one good from one company's Yard gate to another's in another Yard, a pipe the game routes along the
  roads; the offerer pays the pipe, the other accepts with a tap. A tank at the buyer's gate keeps two hours of the
  flow; a pause or the end comes with an hour's notice. To another town a link needs a trunk line (§16).
- **The Exchange** (from Level 12): orders with every town at your own price within the band, by cargo that takes time,
  with a 4% fee; you buy goods of your own Era or older.
- **The order of things**: your own buildings, then links, then the Yard, then the market.

## 9. Buildings, levels and pipes

- **Copies**: every copy of a building costs 7% more than the last (the Workshop Monument makes everything cheaper).
- **Levels** up to 25, +10% output (and input) each, a star every fifth level. A level step costs what that copy cost
  ×1.15 for every level it has. From level 11 a step also takes goods worth 30% of its gold (Era 1: planks and bricks,
  then iron ingots and glass, then copper ingots; each Era its own), which come like a scaffold's materials: your
  Storages, your own spare, the Yard and the Depot, then the market at 2.5× within about ten minutes (a switch in the
  trade settings turns that off). The building works at its old level under a little scaffolding until they are in;
  *Buy the rest* brings them at once; Undo takes the waiting steps back.
- **New buildings** take materials the same way: they stand as a scaffold until the goods are in.
- **Pipes** carry a limit between each pair of buildings they join, and lose a little each tile:

  | Tier | Pipe | Era | Carries | Loss a tile |
  | --- | --- | --- | --- | --- |
  | 1 | Grey | 1 | 100 | 0.3% |
  | 2 | Copper | 1 (research, everyone at Level 8) | 5,000 | 0.15% |
  | 3 | Steel | 2 | 50,000 | 0.05% |
  | 4 | Composite | 3 | 500,000 | 0.02% |
  | 5 | Glass conduit | 4 | 5,000,000 | 0.01% |
  | 6 | Superconductor | 5 | no limit | none |

- **The three lanes**: where a pipe meets a building, a connector leaves it for each kind of good the building takes or
  makes: a belt for solids, a slim pipe with a valve wheel for fluids (water, oil), copper cables for energy. They move
  only when goods move; the pellets ride their lane.
- **Valves** let chosen goods through; **priority valves** are served first; a network needs a **Pump Station** for
  every 50 buildings; **Power Lines** (Era 2) carry energy without loss across water.
- **Complexes**: a track's tier 4 merges four 1×1 buildings in a square into one with the output of five, its tier 8
  nine into twelve.

## 10. Goods and chains

| Era | Land resource | Goods |
| --- | --- | --- |
| 1 · Steam & Iron | water, iron ore, wood, copper ore, coal, stone, sand | iron ingots, copper ingots, bricks, glass, planks (and energy) |
| 2 · Electric | bauxite | steel, copper wire, concrete, aluminium |
| 3 · Oil & Machines | oil | plastic, gears, motors |
| 4 · Electronics | rare earth | circuits, batteries, robots |
| 5 · Space Age | titanium ore | titanium, superconductors, spaceframes |

Every Era's goods are worth about four times the Era before. Later goods need older ones (a robot needs all of them),
so every Era's goods stay worth making and trading. The buildings, their recipes and costs are in
`TransportPipes.Game/Content/buildings.yaml`; the goods and their base prices in `resources.yaml`.

## 11. Eras

- **Per town**: each town has its own Era and moves up when its Era Wonder is finished. A launch party follows: everyone
  in the town makes 10% more for a day, and Loady opens the Era's chapter.
- **Your Era** is the highest your town reached while you lived there. You may move only to towns of your Era or one
  below; a building of a later Era than its town sleeps until the town gets there. Newcomers start in Era-1 towns.
- **A new Era** brings a land resource, new goods with fresh Hub Orders, new buildings and tracks, a better pipe, new
  Era tree items, a new Monument site, and new room: the outskirts in Era 2, the Deep Level and trunk lines in Era 3,
  outposts in Era 4.

## 12. Research

The Lab works on two lines at once, each with a queue of five, also while you're away (*Keep researching* starts the
cheapest next step your gold covers). Research costs gold, and from a track's tier 3 also its good, worth a fifth of
the gold (from your Storages; what they lack is bought at 2.5×).

- **Tracks** (21: Power, Iron, Copper, Stone, Sand, Wood; Steel, Wire, Concrete, Aluminium; Oil, Gears, Motors, Lines;
  Rare Earth, Circuits, Batteries, Robots; Titanium, Superconductors, Spaceframes): nine tiers over about ten days,
  each Era's tracks four times the gold of the Era before.

  | Tier | Time | Effect | Gold (Era 1) |
  | --- | --- | --- | --- |
  | 1 | 10 min | faster ×1.25 | 500 |
  | 2 | 1 h | leaner ×0.8 inputs | 3,000 |
  | 3 | 4 h | faster ×1.5 | 20,000 |
  | 4 | 12 h | complexes | 100,000 |
  | 5 | 1 day | faster ×2 · Hub Orders ×1.8 from here | 500,000 |
  | 6 | 1.5 days | leaner ×0.7 | 2,000,000 |
  | 7 | 2 days | faster ×2 | 8,000,000 |
  | 8 | 2 days | mega complexes | 20,000,000 |
  | 9 | 2.5 days | master ×2 | 50,000,000 |

  Power counts half (every building uses energy); its leaner tiers make wind, sun and water give more instead. A Coal
  Pit follows the better of Copper and Stone. The Lines track makes trunk lines cheaper and lose less; its master builds
  superconductor lines from Era 3.
- **The Era tree**: short research that opens an Era's pipes, buildings and systems (Copper Pipes, Valves, Pump
  Stations, the Big Factory; Steel Pipes, Power Lines, Silos, Deep Mining, Survey the Outskirts; Composite Pipes,
  Trains, Trunk Lines, the Deep Lift, Terraforming; Glass Conduit, Outposts, Automation, the Quantum Link;
  Superconductor Pipes, Orbital Logistics), priced at about an hour of income when the Era begins.

## 13. Wonders, Monuments, the Launch and missions

- **The Era Wonder** (one per town and Era): three stages of goods at a fixed size (rule 2), a sixth, a third and a half
  of its worth. The Great Dynamo (Era 1) is worth 4.3 billion gold of goods, the Copper Cathedral (Era 2) 84 billion,
  the Grand Refinery Tower (Era 3) 560 billion, the Thinking Machine (Era 4) 1.85 trillion and the Space Elevator (Era
  5) 5 trillion: what a town of eight regular players gives in about 12 days, three weeks, four weeks, four weeks and a
  month, so a town of eight launches about four months after its first plot. Every town that built a
  Wonder before makes it 10% smaller for the towns after it, down to half.
  - **Giving goods**: each good has a give switch (off, beyond my Hub Order, all) that sends it through your Yard gate;
    you can give from your Storages and the Depot at any time; **buy and give** buys what a stage lacks at 2.5×.
  - A full stage stands a day later. Everyone who gave gets hours of their own income by their share of the stage:
    Bronze from 1% (1 h), Silver 3% (3 h), Gold 7% (6 h), Diamond 15% (12 h). Each stage gives the town a perk; the last
    brings the next Era.
- **Monuments** (from Level 5): every Era gives a Yard a Monument site; its crew picks one of five kinds (Grand Fountain:
  +3% production a stage; Clock Tower: Hub Orders +5%; Observatory: research 3% faster, up to 30%; Workshop: 3%
  cheaper, up to 30%; Lighthouse: Yard and link sales +3% paid by the market, up to 30%). Any resident pays gold into
  it; stages without end, the first 100 million × 10 per Era of the site, each 1.4× the last. It grows bigger in the
  Yard's square as its stages do. A stage costs a lone resident what it costs a crew.
- **The Launch**: when a town's Space Elevator is finished it launches: titles for everyone in the town and Legacy for
  their accounts (more for those who gave). Then **missions** begin: each asks for Era-5 goods, 10% more than the last,
  and gives everyone in the town +1% production for good and the town a star on the world map. They never end.
- **Legacy** (account perks for any future company): starting gold, from Legacy level 5 a manager slot.

## 14. Company Level

The Company Level comes from all the gold your company ever earned: at the market, from players, daily orders and
rewards (never refunds or buy-backs). Spending takes nothing away. Gold earned for level L: 500 × 3.96^(L−2) up to
Level 10, 30 million × 1.44^(L−10) up to 20, 1.15 billion × 1.35^(L−20) up to 25, 5.2 billion × 1.5^(L−25) after. Levels
open parcels, buildings (their `unlock.level`), manager slots (10, 20, 30, 45), workshops and the Depot (10), the
Exchange (12), Monuments (5) and expeditions (20).

## 15. The helpers

- **Loady** guides the first hour, the middle game (chapter 2) and opens a chapter for each Era.
- **Next steps**: the three best things to do now, each with its gain a minute and price, measured by solving the
  network as if it were done; a building whose materials the market brings counts their price.
- **Max** buys the levels of a building that pay; **Grow everything that pays** buys the next parcel or outskirts parcel
  first, then every pipe door and Max that pays back within five days, with an hour's Undo.
- **Wants**: what a scaffold or a level step still lacks, with who brings it and *Buy the rest*.
- **Your gold can**: when gold piles up, the shared goal (the Wonder), a want, or Grow.

## 16. The wilderness and other towns

- **Fog**: the wilderness starts unmapped on your map. Expeditions (from the Expedition Office, Level 20; 1, 4 or 12
  hours) map 1, 3 or 8 pieces of it around your town and come back with gold and, by luck, a rich deposit, a decoration
  or a relic.
- **Discoveries**: every four days a rich deposit appears in the wilderness; every Yard whose players bring something
  out of it in its first three days gets its marker.
- **Outposts** (Era 4, the research Outposts): up to three 8×8 claims on rich deposits you mapped, bought once (20
  billion, each next one twice the last: about five hours of income when Era 4 begins) and yours for good. Goods come home by Cargo Depot or Quantum
  Link.
- **The Deep Level** (Era 3, the Deep Lift): the caverns under your plot, dug out parcel by parcel, with the Magma Tap.
- **Trunk lines** (Era 3, the research Trunk Lines): a line from your town's gate to the facing gate of a neighbouring
  town, its right of way bought once (10 million a tile) in a pipe tier, and its builder is its carrier.
  Whoever builds a route first has it. It carries links between players of the two towns, and sales to the other
  town's market, where your Hub Order is half of your own town's (goods go only to towns of their Era or later). The
  carrier sets fees from nothing to 10% of the goods' base value (public, crew and per player), paid by the seller. A
  line keeps running and stays its carrier's, also when they move or stop playing.

## 17. Away, moving and leaving

- **Away**: everything runs on; research starts what is queued; the return card says what happened.
- **Moving** is free, a minute's flight, with five minutes between moves, to a town of your Era or one below. Your
  buildings and pipes come along in the same layout where the land allows (the rest in crates); workshops come along in
  crates; your Depot goods come back into your Storages; the outskirts are bought back for half.
- **Leaving a Yard**: the last one out takes the crew and the Yard's own buildings with it.

## 18. Everything else

Managers (Planner Sam keeps both research lines busy, Trader Lin buys stale wants and adds 5% to Hub Orders, and
others), achievements and collections that pay hours of income, three daily orders, calendar events (storms, heat
waves, shortages, expos, festivals), blueprints, the Yard's Plan board, crew, town and world chat with blocking and
reporting, guestbooks and likes, leaderboards, decorations, photo mode.

## 19. The numbers

| What | Number |
| --- | --- |
| Starting gold | 2,000 |
| Parcels | 20,000 × 2.5ⁿ; the 5th at Level 5, then one every second level |
| Outskirts | 100 million × 1.4ⁿ, from Era 2; bought back for half |
| A copy | its price × 1.07 for every copy owned |
| A level step | that copy's price × 1.15^(L−1); from level 11 also goods worth 30% |
| Hub Order (Era-1 good) | 1,000 g a minute at base, ×1.1 per trading post level |
| Market | pays base up to the Hub Order, √ beyond, a quarter beyond 16×; sells at 2.5× |
| Player prices | 1×–2.5× base; public 1.3×, crew 1.15× by default |
| Track tier gold | 500 · 3k · 20k · 100k · 500k · 2M · 8M · 20M · 50M, ×4 per Era |
| Era Wonders | 4.3 billion · 84 billion · 560 billion · 1.85 trillion · 5 trillion g of goods |
| Monument stage | 100 million × 10^(Era−1) × 1.4^(stage−1) |
| Workshops / Depot | 64 tiles from Level 10 / 20,000 units |
| Exchange | Level 12, 4% fee |
| Mission | 600 billion g of Era-5 goods × 1.1 per mission done (about three days of a town of eight) |
| Trunk line | 10 million a tile of right of way; fees 0–10% |
| Outposts | 4 billion × 2ⁿ |

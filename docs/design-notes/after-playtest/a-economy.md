# Round A · economy

My seat: the numbers and the long game. I designed v1.2's price ladder, the Hub Order, the NPC caps, the stand-in
terms and the week-3 growth path, and in my final review I asked for "T(L) everywhere". The run tests all of that, so
this paper is mostly about my own decisions.

**Sources:**
- real gold growth from `playtest/runs/story1/metrics.jsonl` (the last sample of each row);
- recipes from `buildings.yaml` (level 1, per minute, own energy at 1 g);
- prices from `progression.yaml:52–66`;
- the fix panel's numbers in `docs/design-notes/playtest-fixes/r1-economy.md` and `r3-economy.md`, which I re-checked.

## What held, what failed

### The run in five numbers

1. **Real incomes were 4–18× below T(L).**

   | Player | Level | T(L), what the design sized for | Real gold growth, day 36 | Ratio |
   | --- | --- | --- | --- | --- |
   | Ana | 16 | 2,303 g/min | 187 | 12× |
   | Dev | 14 | 2,054 | 152 | 13× |
   | Mia | 12 | 1,785 | 101 | 18× |
   | Cleo | 23 | 3,230 | 799 | 4× |

   These players had 6–13 buildings and a session every 2–7 days. The balance bot that T(L) comes from has about 300
   buildings and five sessions a day.
2. **Gold piled up in the bank.**

   | Player | Bank on day 36 | Days of their own income |
   | --- | --- | --- |
   | Ana | 8.8M | 33 |
   | Mia | 4.6M | 31 |
   | Cleo | 30M | 26 |
   | Dev | 5.4M | 25 |

   In 21 of 23 sessions after day 1 players spent under 3% of their bank (r1-economy, item 4). A Smelter costs 1,800
   g, a Glassworks 1,600 g. **From day 2 on, gold limited nobody.**
3. **No trade between humans was ever paid.** Paid Yard income was ≈ 0 for every player (r1-economy, item 1). The one
   time the Yard acted, it took 3.5M from Ana, Ben and Dev (finding "The one time the Yard acted…").
4. **Week 3 was flat.** From day 14 to 21 Ana went −8%, Cleo +2%, Dev +5% (row "Week 3"). Ana had **9 buildings from
   day 4 to day 36** (metrics rows 06–14).
5. **The typical bot I tuned against earns 98.5% of its income from energy.** On day 28 it sells 12,494 energy/min at
   0.39 × base. No real player came near the Hub Order, which needs about 4,000 energy/min with quiet grace
   (r1-economy, item 4).

### My promises, one by one

| Promise (v1.2) | Status | Why | Kind |
| --- | --- | --- | --- |
| **Comparative advantage makes trade worth it**: home goods, complementary seats, "solo can make everything, just slower" | failed | Every estate gets copper, coal and stone (2×2 at ★2) plus a shore from the surveyors (`GameWorld.Survey.cs:5–13`), and iron, stone and forest from the generator (`WorldGenerator.cs:287–359`). The v1.2 design kept the patches on purpose: "so the first hour works alone and nobody depends on a friend for their first Smelter" (§14). "Slower" never bites when gold is free and the patch has room. | **(b)** the design was wrong |
| Seats in a station complement each other | failed | The final design dropped the station-level rule. The generator spreads specialties evenly per *district* (`WorldGenerator.cs:158–178`), so Capital NW got two energy seats: Cleo's Sunny Flats and Dev's Windy Heights (group chat, day 1 20:04). The home goods land in random outer parcels (`WorldGenerator.cs:210–217`), not in "the first two you can buy" (§14). | **(a)** for the parcels, **(b)** for losing the station rule |
| **One price, 1.75 × base: both sides win** | failed | I anchored it on the hub's spread (seller +75% over 1.0, buyer −30% against 2.5). The buyer's real alternative isn't importing at 2.5, it's its own ★2 patch. The price sits at the buyer's break-even (table below). | **(b)** |
| A seller is paid for what reaches the Yard; never worse than the hub | failed | The code paid only what the buyer's buildings took, and counted lacks from the full need (`Yards.cs:131–134`, `:209–218`, `:341–366`; r1-economy, Root causes). The design (§7) said otherwise. | **(a)** built differently |
| Goals sized by T(L), "never your own income" (my r5 review, finding 1 and table 13) | failed | Built exactly so (`progression.yaml:73`, `Projects.cs:128`). The Water Tower asked 329k g of goods, 28 h of Ana's whole income. "You may still bring 740k g worth" is 3 days × 2 h × T(14), ten times her output (finding "The project card's 'You may still bring … worth'"). | **(b)**, my mistake |
| Week 3: the Hub Order pushes volume to trunks and a second hub, +15% | failed | Trunks need steel and came with Era 2 on day 23 ("Trunks are steel: they come with Era 2", cleo d14-1900 #238). The √ Hub Order never bound at 100–800 g/min, so a second hub would have paid ≈ 0. Discoveries couldn't be clicked (dev d08-2000 #30). | **(b)** sized for the bot, and **(c)** never reached |
| Solo earns 85–90% of a crewed player | not testable | The bug paid nobody. Structurally it inverts: with t = 0 a crewed player earns 1.0 × the hub, while a solo player selling a quarter to Houses at 1.25 earns 1.0625 (r2-economy, "What everyone missed" 1). | **(b)** |
| New gold from NPCs ≤ 6% | not measured | Houses' purchases were never paid (same bug). | **(a)** |
| Absence costs nobody; a stand-in at decaying terms; asleep estates earn nothing | **held** apart from the Yard drain | Ben: "+2.6M gold · the caretaker … 186k · Granny Ida stood in · from day 30 … made nothing" (ben d36-2000 #73). "A month away and nothing lost". The only harm came from the Yard bug: 0.3 g/min on days 6–14. | (a) for the harm |
| No gold between players, contracts promise only what you make, no price crashes | held, but never tested | No alts, contracts or dumping in this run. | (c) |

### The table that explains t = 0

Margins are for a level-1 building, in g/min, with own energy at 1 g. "Two foreign" means both of its inputs other than
energy are bought.

| Chain | Own-made margin | Buyer at 1.75 | Buyer at 1.5 | Buyer at 1.3 | Seller's gain at 1.3 |
| --- | --- | --- | --- | --- | --- |
| Small Factory, iron ore | +28 | +1 | +10 | +17.2 | +10.8 |
| Smelter, copper + coal (two foreign) | +25 | **−8.75** | +2.5 | +11.5 | +13.5 |
| Brickworks, stone + coal (two foreign) | +12.5 | **−8.1** | **−1.25** | +4.25 | +8.25 |
| Glassworks, sand | +27 | +13.5 | +18 | +21.6 | +5.4 |
| Sawmill, wood | +7 | **−4.25** | **−0.5** | +2.5 | +4.5 |

The worked check, Smelter: 5 copper ingots × 18 = 90 g out; in, 20 energy + 10 copper ore × 3.5 + 4 coal × 2.5 = 65 g
at base. At 1.75: 90 − 20 − 1.75 × 45 = −8.75.

**What this shows:**
- **The fix panel's 1.5 doesn't clear it.** At 1.5, Brickworks and Sawmill still lose money for the buyer once both
  inputs are foreign. Only around 1.3 does every Era-1 chain pay both sides.
- **Price isn't the main problem.** Against a free ★2 patch, a bought input loses at *any* price above 1.0. Own mine
  plus factory makes +26 + 28 = +54 g/min for 2,100 g; the neighbour-fed factory makes +10 at 1.5 for 1,500 g.
- **The patch never runs out at these sizes.** A 2×2 patch holds 4 mines (9 ore/min each at ★2), enough for three
  Small Factories. These players had 6–13 buildings in all.
- **The advisor points at the patch too.** It suggests "an extractor of something its land holds that it doesn't mine
  yet" (`Advisor.cs:845ff`).

**In this world nobody needs a neighbour.** The friends knew it was supposed to be otherwise. Ana's first message was
*"I'll start with the energy, can someone bring sand? 😊"* (group, day 1 20:04), and in 36 days of the crew talking,
sand and copper were the recurring topics.

## Is the fix plan enough?

**For "honest and reachable", yes. For "together", no, and the owner is right to expect more.**

**Where the plan repairs a cause:**
- **F1, the honest Yard:** it repairs a build defect. The floor makes the "never worse than the hub" I designed real,
  and the lack cap removes the Lab drain. Ana gets ≈ 228 g/min through the Lab week (r3-economy, appendix F1).
- **F4, the project:** it repairs my T(L) error. Baskets sized by measured income I(c) and by goods made: the crew's
  Water Tower goes from 329k to about 93k, done around day 3. I was wrong in my final review to ask for T(L) in every
  player-facing size. The run proves T(L) only works as a sink price.
- **F3 and F8, advice sized to the bank and smart Max:** they repair the reason gold piled up. Cleo's "moily to 30 +
  post to 10" for 114k (2.4% of her bank) triples her income (r1-economy, proposal 3). **This is the biggest income
  lever in the plan.** But it is solo growth, not playing together.
- **F2, dead ends:** not my seat. It is clearly why Ana's income sat flat for 32 days.

**Where the plan patches a symptom of a design flaw:**
1. **F6, interdependence.** The advisor will offer neighbour-fed chains. But the advisor ranks the own ★2 mine higher
   (+54 for 2,100 g against +10 for 1,500 g at 1.5), and so would any sensible player. Showing a better trade card
   doesn't create a reason to trade.
2. **"The bots decide: 1.75 or 1.5."** It asks the wrong question:
   - both prices lose to a free local patch;
   - 1.5 still fails Brickworks and Sawmill with two foreign inputs;
   - **the gate is miscalibrated.** Bots with about 300 buildings exhaust their 2×2 patches and start buying, so the
     crew-bot run can reach t ≥ 15% while humans with 10 buildings stay at 0. The bot passes the gate for a reason no
     human will ever have.
3. **The fallback lever "seat kits get one consumer of a neighbour's specialty, if t < 15%."** The consumer will be fed
   from the player's own surveyor patch. The lever fails as long as the patches exist.
4. **Nothing in the plan touches the land.** There's no item for complementary seats, the surveyor patches or the
   all-energy kits. "Every starter kit makes energy, so no friend needed what a friend made" is the plan's own cause 3,
   but its answer is a project that buys energy and advice that points at a patch.

**What the plan delivers:** the friends would be side by side, honestly. Their surplus goes to the hub, or to a project
that pays the hub's price plus a 3–10% bonus, and each grows alone by investing his bank. That is a good solo game next
to friends, which is what the run already found.

## What I'd change

**"Land that needs a neighbour."** Keep v1.2's places, crews and the Yard, and keep all of F0–F11. Change the land and
the one price so that the cheapest way to grow is a neighbour's goods, and so that nobody is ever stuck when the
neighbour is gone.

### 1 · A station's land is shared out, and nobody gets a copy

- **Universal goods:** energy, iron ore and water are on every estate. The first hour (Levels 1–4: panels, moilies,
  the Iron Mine, the Small Factory, the Lab) still works alone, exactly as today.
- **The five land goods of Era 1** (copper ore, coal, sand, wood, stone) are dealt **per station**: every station's
  seats hold all five among them, and each seat holds one to three of them (three in a two-seat halt). This replaces
  the per-district shuffle in `WorldGenerator.cs:158–178`. Two energy seats in one station, as in Capital NW, can no
  longer happen.
- **No copies of a neighbour's goods.** For new worlds:
  - the surveyors' copper, coal and stone patches go (`GameWorld.Survey.cs:13`);
  - the generator's "small extras" go, except for the seat's own goods (`WorldGenerator.cs:346–359`);
  - a shore exists only on the sand seat.

  The owner already starts a fresh world when the rules version changes, so no tester estate loses land.
- **The seat card says so:** "Copper Gulch: copper and coal. Sand comes from Ben (Riverside) or Bertha ⚙."

### 2 · Kits that differ

The House that warms a seat leaves a kit with **one producer of the seat's own land good**, plus energy:
- the sand seat: a dredger on its shore;
- the copper seat: a copper mine;
- the stone seat: a quarry.

On the first evening every friend has a different spare in the Yard, and the project's "made goods" rule (F4) asks for
three friends' goods instead of four friends' energy.

### 3 · One price that pays both sides: 1.3 × base

- **The player price becomes 1.3** in `progression.yaml:54`, for the Yard, links, contracts and the Exchange. It is
  still one price, as v1.2 decided.
- **Why 1.3 and not 1.5:** every Era-1 chain pays both sides at 1.3, even with two foreign inputs (table above). At
  1.5 the Brickworks and the Sawmill don't.
- **Houses:**
  - they buy at 1.15 × the price the hub pays you, so a human buyer always beats them;
  - they sell **land goods at 1.4, at most one level-1 building's need per good and buyer** (for example 10 copper
    ore and 4 coal a minute);
  - energy and iron stay at 2.0, because everyone makes them.
- **The ladder:**

  | hub buys | House buys | friend | House sells land goods | House sells energy, iron | hub sells |
  | --- | --- | --- | --- | --- | --- |
  | 1.0 | 1.15 | 1.3 | 1.4 | 2.0 | 2.5 |

- **The most a unit can carry to an alt** falls from 0.75 to 0.3 × base.
- **This replaces** the fix plan's price question and its conditional seat-kit lever.

### 4 · The advice follows from the land

You can't make copper, so the advisor's best chain *is* the neighbour-fed one. No new ranking is needed: F6's
neighbour-fed `ChainSteps` simply has no patch to compete with. Its card names both ends and both gains:

> A Smelter on your Yard gate: Cleo's copper and coal at 1.3 · +11.5 g/min for you, +13.5 for Cleo.

### What players do, see and feel, walked through the run

- **Day 1, 20:05–20:20.**
  - Ana (energy) has moilies. Ben (Riverside) has a kit dredger making 15 sand/min. Cleo (copper and coal) has a kit
    copper mine. Dev's seat (stone) is warmed by a House with a quarry.
  - At the Handshakes, the Yard ledger shows sand, copper, stone and energy.
  - The Foundation takes Ana's energy. The Water Tower asks for Ben's sand, Cleo's copper and a keystone such as wood,
    which Ben's seat can make at Level 5.
  - Ana's question in the chat ("can someone bring sand?") now has an answer in the game: Ben's sand is already in the
    Yard.
- **Day 3, Ben stops.**
  - His estate rests at 100%, and his dredger keeps sending sand. F1 pays him for what Ana takes, and the 5.4 g/min a
    Glassworks brings shows on his return card.
  - From day 30 the stand-in copies what Ana actually took (F9). After 14 days, Granny Ida ⚙ sells sand at 1.4,
    enough for one building.
  - Ana's Glassworks never stalls. What Ben's absence costs her is a *second* Glassworks, which is what it should cost.
- **Day 4–6, Ana at Level 7.**
  - She can't build a dredger: she has no shore. The next step is the Glassworks on Ben's sand, +21.6 g/min.
  - At Level 6 a Smelter on Cleo's copper and coal adds +11.5 for her and +13.5 for Cleo.
  - Together that is ≈ +33 g/min, about **+16% of Ana's real 200 g/min**.
  - Today she sat at 9 buildings for 32 days. F2 fixes why her factory sat idle; this gives her something to build next
    that only a friend can feed.
- **Cleo** becomes the station's copper and coal seat, and her income includes the sellers' share. Her bank still
  needs F3's investment advice. What changes is that her best move can now help Ana: the plan's Invest line can say
  "a second copper mine: Ana's Smelter takes all of it".
- **Day 8, Dev.** His seat's stone exists nowhere else in the station. He is the Brickworks keystone (Cleo's coal and
  Dev's stone), or the project's keystone, **by construction**. In run 1 he spent 11 days building a dredger for sand
  nobody could receive.
- **Mia, in a station of Houses.**
  - Bertha ⚙ sells her one Glassworks' sand at 1.4. Her chain makes +19.8 g/min against a crew member's +21.6, so at
    her 7 buildings she earns about 96–97% of a crewed player.
  - She is capped only when she wants a *second* Glassworks. Then the lone-human card has a real reason: "Jo at
    Capital E makes sand, and his station has a free seat."
  - Parity takes the right shape: equal while small, friends ahead as the estate grows (about 85% by week 3, when the
    cap binds on two or more chains).
- **Week 3.** For these players, Levels 6–10 (Smelter, Brickworks, Glassworks, Quarry) arrive between day 3 and day 21
  (Ana Level 7 on day 4 and Level 10 on day 15; Dev Level 10 on day 21). So the neighbour-fed chains are the week-1-to-3
  growth, at real incomes: two chains are about +15–20% for a slow player and +5–14 g/min each for the friend who feeds
  them. Add F4's Foundry Hall (+10% on two chains) and F3's investment. **The +15% is reached through cooperation**,
  not through trunks that, at these incomes, arrive in Era 2 and pay nothing.

### What it replaces

| In v1.2 | In the fix plan |
| --- | --- |
| §14's "keep the surveyors' ★2 patches" | The price vote (1.75 against 1.5) |
| The per-district specialty shuffle | F6's "recipes prefer neighbour goods" (now automatic) |
| The 1.75 price and the House prices 1.25 / 2.0 | The conditional seat-kit lever (now unconditional) |
| Week 3 "trunks to a second hub" (moves to week 4+, Era 2, priced in T(L) as a sink, which the banks can afford: Ana's 8.8M is 33 days of her income) | |

### Cost to build

About 2–3 days, before the bot runs and run 2:

| Change | Where | Size |
| --- | --- | --- |
| Deal land goods per station | `WorldGenerator.cs:158–178`; a seat deck per station | M |
| No copies | `Survey.cs:13`, `WorldGenerator.cs:346–359`, new worlds only | S |
| Kits with the land producer | the Houses' kits | S |
| Prices | `progression.yaml:52–57` | S |
| Houses' land-good rule (one building's need, at 1.4) | `Yards.cs:164–178` | S–M |
| Seat card texts | | S |

**The balance stays put.** The typical bot is 98.5% energy, which stays universal. Its chains buy one building's need
from Houses, so its income and Level 30 on day 25 should move by under 2%. Re-run `typical` and `typical-together` to
confirm. Then run the crew bots with a **light profile** (10–15 buildings, a session every 2 days), so that t is
measured on estates the size of real ones.

### Risks

1. **Dependence becomes guilt.** "Ben quit, so the crew lost its sand." The stand-in and the House's one-building
   supply mean nothing ever stalls. Only growth waits for a human. Needs the psychologist's view.
2. **Halts.** Two seats must hold five goods. Three specialties for one seat is a lot of land; the generator has room
   (12 outer parcels).
3. **Clarity.** "There's no copper here" must read as an introduction, not a wall: the seat card, the Build menu ("from
   Cleo or Bertha ⚙") and F2's reason line.
4. **Lower seller income.** A seller gains 0.3 instead of 0.75 per unit, but sells to a buyer who now exists. 0.3 × a
   volume beats 0.75 × nothing.
5. **A seat nobody takes.** Houses cover it at 1.4 for one building. A station whose copper seat sits with a House for a
   month earns less than one with a copper friend. That is the reason to invite, and the lone-human card's reason to
   move.

## What I'd keep no matter what

- **One price between players; goods only, never gold; Houses labelled and inside one 25% buying cap.** The level of
  the price changes (to 1.3), not the principle.
- **The personal √ Hub Order.** It never bound in this run, but it is why nobody can crash a newcomer's price.
- **F1's floor: a seller never does worse than the hub,** paid on hub-bound spare only (r3-economy, F1 (a)).
- **Stand-ins at decaying terms, and asleep profiles that earn nothing.** It held: Ben's "nothing lost" and no windfall.
- **I(c) for every goal a player must fill; T(L) only for gold sinks** (trunk segments, donation caps). I was wrong to
  ask for the reverse.
- **Contracts promise only what you make** (9621766).
- **F3 and F8 in full.** With banks of 25–33 days of income, the right next step is worth more than any price.

## Questions for the others

- **Friends:** the crew's first plan was a division of labour (energy, sand, copper). Does land that *requires* it make
  the first week more fun, or does a friend's quitting now feel like a hole, even with the House covering one building?
- **Lonely:** is "Houses sell land goods for one building per good" an acceptable ceiling for Mia, or do you want two
  buildings, so that parity stays above 90% into week 3?
- **Psychologist:** in round 2 we rejected "rewards that need humans". This needs *goods from other land*, not other
  people, and Houses always provide some. Is that line clear enough to players to avoid the Habitica feeling?
- **Outsider:** does "you can't make copper here, Cleo can" read as a reason to talk to Cleo, or as a missing feature?
- **Wild:** would you go further, for example a station-owned machine in the Yard that only runs on three seats' goods?
  Or would moving house to a station with a free copper seat be the better answer for a solo player?
- **Everyone:** the fix plan lets bots choose between 1.75 and 1.5. I say neither, and propose 1.3, by the table above.
  Does anyone see a chain or a player that 1.3 hurts?

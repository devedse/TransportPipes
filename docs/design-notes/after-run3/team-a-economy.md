# Round A · economy

My seat: the numbers and the long game. I designed the price ladder, the Hub Order, the dealt land and the money
guards, so the bank Ana couldn't spend is partly mine.

**Sources:**
- `playtest/runs/story3/metrics.jsonl` (`gold`, `earned`, `goldPerMinute`, `buildings`, `hubGold`, `yardPaid`);
- the content files: `buildings.yaml`, `pipes.yaml`, `progression.yaml`, `research.yaml`, `wonders.yaml`;
- the code: `GameWorld.Wonders.cs`, `Content/Definitions.cs`;
- the analysts' `r2-*.md` and `docs/playtest-run3-review.html`.

## Diagnosis: the sinks exist; gold just can't reach what binds

**Growth stopped around day 12–13, not in week 4.** From the metrics:

| Player | Income | Buildings |
| --- | --- | --- |
| Ana | 951 g/min from day 13 to day 35 (929 on day 36) | 13 from day 15 on |
| Cleo | 695 g/min | 15–16 |
| Dev | 502–643 g/min | 12 |
| Mia, beside Jo | 668–736 g/min | 11 |

On day 36 their gold was 98–100% of what they had earned: Ana 38.8M of 38.8M is 28 days of her own income.

**It wasn't for lack of things to buy. The returns stopped.** Upgrades were the best deal in the game:
- +136 g/min for 10.1k, a 74-minute payback (cleo d02 #84);
- +114 g/min for 3,357, a 29-minute payback (cleo d10 #203).

Players bought until a purchase stopped paying, then stopped buying. Four things bind after week 1, and gold has an
exchange rate into none of them:

1. **Throughput.**
   - A grey pipe carries 100 a minute per producer→consumer link (`pipes.yaml`).
   - A Wind Moily passes 100 a minute at about level 13: 30 × 1.1¹² × 2 ≈ 188 (`progression.yaml`: ×1.10 a level,
     ×2 at 10 and 25). From there, every upgrade sells nothing more.
   - Cleo's level-25 Coal Pit made 882 coal a minute and sold 100 (cleo d21 #393); its 117k bought nothing.
   - The way past is a Copper Pipe at 1 copper ingot a tile from your own Storage. Cleo had none (#399–#404).
2. **Materials.**
   - Big Factory 100 bricks, Steel Mill 300, Market Hall 500, Bank 1,000 (`buildings.yaml`), all from your own Storage
     (`Pay()`).
   - On dealt land, bricks need stone (Ana) and coal (Cleo), and a copper ingot needs copper and coal: two seats each.
3. **Land and levels.**
   - Parcels are level-gated (5k × 2ⁱ at Levels 5, 7, 12, 14, 18, 25, 30…).
   - Ana kept her 4 start parcels all run. XP was the wall (*"next level is 41 h away"*), and gold can't buy XP.
4. **Points.** Era 1's tree costs 8,500 points. Cleo banked 449k by day 10 and 1.6M by day 21, more than all five eras
   together (1.42M, `research.yaml`).

**The deeper flaw is in the loop.** An idle game's loop is earn → reinvest → earn more. Every binding constraint has to
be buyable, sooner or later, with the money the game pays you, directly or through other people.
- Our redesign gave friends needs (dealt land) and an act (Send). It gave their goods nowhere to go except into a
  running factory.
- The wants (pipe tiers, big buildings) sat in `Pay()`, which reads only your own Storage.
- So the wall is the missing door between goods and wants. The analysts' row 7 is right.

**One more thing row 7 alone won't fix.** Once the doors open, gold is still cheap against income:
- a Big Factory costs 12k + 100 bricks, while Ana earns 1.37M a day;
- a Copper Pipe for a 15-tile link costs 15 ingots and 150 g;
- even the advisor bot, which spends on every step it can, banks 107M by day 28 (`balance-typical-advisor.md`).

So the bank will still pile up unless at least one want **scales with income**. Upgrade costs grow 1.15× a level while
output grows 1.10× and doubles at milestones, so they never catch up. The one want that already scales with the
players' measured income is the Era wonder: `StageRequirements` sizes a stage at 6 h of the active players' income
(`GameWorld.Wonders.cs:116–126`). It is also the want that opens the next era for everyone.

## The proposal: every want is a buyer on the pipes, and gold pushes the Era

### 1. Sites (row 7, adopted, with the pipe tiers in)

A building, an upgrade or a **pipe tier** that needs materials becomes a **site**, built on the existing Ask engine
(`GameWorld.Asks.cs`, which runs in every Yard round).
- **What the player sees:** *"Big Factory · 12k g + 100 bricks"* → **Start it**. The ghost stands on the land with a bar
  reading *"bricks 0 / 100 · Cleo makes them"*.
- **The binding-limit line offers the pipe** (the analysts' row 2): *"Its pipe carries 100 a minute · Copper Pipe on this
  link: 500 a minute · 15 copper ingots + 150 g · Start it"*.

**Fill order and prices:**

| Order | Source | Price |
| --- | --- | --- |
| 1 | Your own producers on its pipes | free |
| 2 | Friends' and neighbours' spare through the Yard, and Sends (named at both ends, 💛 Thanks) | the one price, 1.3 × base |
| 3 | The station's Houses | land goods at 1.4 within the floor; others at 2.0 |
| 4 | The hub, for the rest, **after the window** | 2.5 × base |

**The window** is 24 hours, or until every crew mate who can make the good has had one session, at most 72 hours.
- During it, the station's spare fills the site **by itself, while friends are away**. Run 3's one felt chain did
  exactly that: Ana's stone in Cleo's Brickworks.
- That is why friends still matter with 2–7 days between sessions.

**Materials go into the site, never a Storage.** It replaces "N in your storages", Deliver, and the Plan board's Ask
form.

**Every land good needs at least one Era-1 site that wants it.** Today planks and wood want nothing: a
content check for Dev's seat (for example, the Warehouse and the Cargo Depot want 100 planks each).

### 2. The Capital's Era wonder: a buyer, sized by income, open to gold (rows 5–6, changed)

- **A buyer in every Yard round, after the station's project,** paying the hub's price (1.0 × base) for what it takes:
  no premium, so no new gold.
- **Every stage is sized when it starts, at 6 h of the Capital's summed measured income,** as the code already does for
  later stages.
  - The first stage is sized again on day 2, when someone is active. Today it was sized at world start for 60 g/min:
    21.6k worth, 1,800 bricks and 900 ingots.
  - On day 15 in run 3 the Capital earned about 3,830 g/min in total, so a stage is about **1.38M worth** of goods.
- **Commission (new):** any player may pay the hub to ship part of a stage at 2.5 × base (the gold leaves the game).
  - It covers **at most half of each stage**. The other half comes only by pipe, from players' goods, and Houses
    count 25%.
  - A commission is credited at 25%, like a hub import, and it shows a **Patron** line with your name.
  - The gold half of a run-3 stage is 0.69M × 2.5 ≈ **1.7M**: about 1.3 days of Ana's income, a real sink that scales
    with the server.
- **When the last stage stands, the next Era starts early.** In run 3 Era 2 came on the deadline (day 22, metrics)
  with the Dynamo at 0. With steel pipes, Era 2 buildings and trunks earlier, the v1.2 long game (trunks, second
  hubs, the Port) arrives in week 3 instead of week 4, when it can matter.
- **Every card tells the stage, your part and how it ended** (row 5).

### 3. Gold for a friend

After the window, **"Pay the rest for Ana ▸"**:
- the hub ships the rest of a friend's site at 2.5 × base;
- the gold goes to the hub, never between players, and never between same-device accounts.

This is the "helps my friends" use Ana asked for, and Cleo's four gold offers turn into something the game can do.

### 4. Points (row 10, adopted with numbers)

- **A Lab with nothing it may research rests and eats nothing** (*"resting until Era 2"*). That ends Cleo's ≈ 400 g/min
  of ingots for nothing.
- **Refinements follow an Era's tree:** +2% output of one chain each, costing 5k × 1.6ⁿ points, one at a time, kept
  while away, capped at +50% a chain.
  - Cleo's 1.6M buys eleven of them (Σ 5k × 1.6ⁿ for n = 0–10 ≈ 1.46M), worth +22% on her coal.

### 5. Hub orders become buyers (later in the redesign)

- A daily order becomes a buyer at your trading post for 24 hours: no Storage, no Deliver.
- It is sized at 30 minutes of measured income and pays 2.0 × base.
- New gold is bounded at about 2% of a day's income.

## The three open questions

**1 · Can gold buy at once, and at what price?**
- **No "at once".** Gold buys the rest at the hub's 2.5 × base, after the site's window.
- **Price can't protect a friend's goods when banks hold 28 days of income;** 100 bricks at 2.5× is 1,250 g. Only order
  and time can.
- **Time works asynchronously** because the station's spare fills the site by itself during the window, even while
  friends sleep.
- **A friend may pay the rest for you;** the gold always goes to the hub.
- Whatever fills it, the analysts' mark watches who did: under a third from friends means the reason to help is
  missing, not the rail.

**2 · Do hub daily orders become buyers?** Yes, on the same engine: a buyer at the trading post sized at 30 min of
income. They come after sites and the wonder, because they add a want, not a door.

**3 · The two-friend keystone: now, later, or never?** **Now**, with the sites.
- Bricks (stone + coal) and copper ingots (copper + coal) are exactly what the sites and the Dynamo want. A keystone of
  bricks starts the crew's first two-seat chain in week 1, and the chain then feeds the Big Factory and the wonder.
- **Only where two active residents make the inputs.** An away seat's input comes from its House (the backstop), and
  today's rule applies where no pair exists.
- It is a recipe rule (2 days, `Projects.cs` `HomeGoods`). The pass-over is already built, so run 4 stays readable.

## Cost and risks

**Cost** (the analysts' estimates, plus mine):

| Part | Days |
| --- | --- |
| Sites on the Ask engine, with pipe tiers as sites | 6 + 1 |
| The window rule | 0.5 |
| The wonder as a buyer, sized by income | 3 |
| Commission and the Patron line | 1 |
| The two-friend keystone | 2 |
| Labs resting and Refinements | 3 |
| Hub orders as buyers | 1–2 |
| A planks content check | 0.5 |
| **Total** | **about 18–19 working days** |

**Risks:**

| Risk | Answer |
| --- | --- |
| One rich player buys the Era alone | Commission is capped at half of each stage; the other half comes only by pipe |
| Friends don't matter, because the hub fills everything | The window and the automatic spare; the mark is ≥ ⅓ of site units from friends and neighbours. If it fails, the lever is the reason (the two-friend keystone, bigger site sizes), not more verbs |
| New gold | The wonder pays the hub's price; Commission and "Pay the rest" are sinks; hub orders are bounded at about 2% of a day's income; NPC gold stays under 6% |
| Pacing | Copper Pipes open quickly: Cleo's coal could go 100 → 500 a minute, about +1,000 g/min before her Hub Order's √ curve bites. That raises XP from earnings. Re-run `typical` and `typical-together`: Level 30 must stay at day 25 ± 2 |
| An earlier Era 2 on a small server | It still needs pipe deliveries for half of every stage, so a small server reaches it later. The deadline stays the latest date |

## Walk-through, weeks 2–5

**Ana** (stone, Capital SE, Level 12 on day 9):
- **Day 9:** the Big Factory unlocks. *"12k g + 100 bricks · Cleo makes them"* → Start it.
- Cleo's Brickworks, on Ana's stone and Cleo's coal, fills it by itself overnight at 1.3: *"Cleo's bricks built your Big
  Factory 💛"*. Thanks.
- **Day 14:** her Quarry's line reads *"carries 100 a minute · Copper Pipe: 18 ingots · Start it"*. The ingots come
  from Cleo's Smelter.
- **Weeks 3–4:** she commissions half of the Dynamo's second stage (≈ 1.7M) and becomes its Patron. The Market Hall
  (500 bricks) and her parcels follow.
- Her bank is spent on something visible and shared, not left at 28 days of income.

**Cleo** (coal):
- **Day 10:** her pit's line names the pipe, not energy. She starts a Copper Pipe site (≈ 15 ingots) and builds a
  Smelter on the House's copper at 1.4 and her own coal.
- Her pit then sells 500 coal a minute: income about 695 → 1,700 g/min, the growth run 3 never had.
- Her Lab rests, and her 1.6M points buy Refinements on coal.
- She feeds the Dynamo's brick half with Ana's stone, and the Era turns before the deadline.

**Dev** (wood, arrives on day 8):
- His planks fill Ana's Warehouse site (the content check) and the project's wood. Every material want has a seat
  that makes it.
- With Era 2 early, his Sawmill feeds the next era's sites, not a full Storage.

**Ben:** with row 4 he lives in Ana's station.
- His kit good keeps filling the station's sites by itself while he's away, through the automatic Yard.
- His day-36 card: *"Your copper went into Cleo's pipes and the Dynamo"*.

**Mia, beside Jo** (Capital NW): the one trade that worked in run 3, 5.8M paid from her to Jo's factory (`yardPaid`).
- Her sites fill from Jo's spare and the Houses; after the window, from the hub.
- With a bank of 27M she is the Dynamo's other Patron, named on the same line as the crew. That is her place in the
  Capital's story.

## How run 4 shows it worked

| Mark | Pass | Run 3 |
| --- | --- | --- |
| Spent | Every crew member spends ≥ 20% of what they earned by day 36 | 0–5% |
| The late game grows | The crew's income +30% and 4+ buildings each from day 14 to 36 | flat from day 13; 3 buildings in all |
| Sites | 80% of material sites stand within 72 h; ≥ ⅓ of their units from friends and neighbours | — |
| The pipe | No producer sits at its pipe's cap for 2+ sessions without the pipe site offered | Cleo's pit, 11 days |
| The Era | The Dynamo stands before the deadline with 3+ contributors; every card tells it | 0 / 1,800; Era 2 on the deadline |
| Points | No Lab runs with nothing to research | 1.6M banked |
| The bots | `typical-advisor` banks ≤ 3 days of income on day 28 (today 25 days); `typical` Level 30 on day 25 ± 2 | 107M banked |

# The design team's answer: gold opens doors, friends fill them

**The owner's question:** *what does a player want to do with their gold (and research) in weeks 2–5, so there is a
reason to grow?*

This is drafted by the economy designer from the six round-B papers, for all six to sign or object to.

## 1 · What went wrong

The game has an unlimited sink: building levels at 1.15× a level, which once paid back in 29–74 minutes (cleo d02
#84, d10 #288). Gold piled up anyway, because after week 1 everything that decides income is priced in something gold
can't buy:

- **The pipe.** A grey pipe carries 100 a minute per connection (`pipes.yaml`). Cleo's level-25 Coal Pit made 882 coal
  a minute and sold 100 (cleo d21 #393). Her Copper Pipe click did nothing, because its ingots had to come from her own
  Storage (#399–#404).
- **Materials.** The Big Factory needs 100 bricks, the Market Hall 500 and the Steel Mill 300, and `Pay()` takes them
  only from your own Storage. On dealt land they are made from two friends' goods: *"I don't know how to hand you 100
  bricks directly"* (Cleo, group, day 21).
- **Spending that hurt.** The Lab's Max took Cleo from 783 to 285 g/min (cleo d06 #180), and Dev called a 329k upgrade
  *"a trap"* (dev d10).
- **Points with nothing to buy.** Cleo had 1.6M by day 21, more than all five eras' trees together (1.42M).

**So the loop broke around day 12–13.**
- Ana stayed at 951 g/min and 13 buildings to day 36, and the crew held 95–100% of all it earned (metrics).
- The players offered each other gold four times (Cleo on days 2, 6 and 14; Ana on day 21, #274, #309).
- They came back for a friend counting on them and a moving shared goal.

**Our answer:**
- Every want becomes a buyer on the pipes that friends can fill.
- Gold finishes the rest after the friends have had their turn.
- The screens say what stops you, and what a purchase will do before you click.

## 2 · The solution, in build order

Each step is a playable commit, after the analysts' "now" rows 1–6. Days are for one AI developer.

### Step 1 · Spending is safe, and "what stops you" (2–3 days)

**Replaces:**
- Max's blind *"+21 · 238k"*;
- idle reasons that blame energy;
- the Invest line's *"Max … for 13k"* against 27.8M (ana d27 #317).

**What changes:**
- **A preview from the solver before every purchase, in one sentence:** *"+105 g/min"*, *"+0 g/min: this pipe carries
  100 · Copper Pipe ▸"*, or *"−498 g/min: the Lab eats 29 of your iron ingots"*.
- **Max stops at the pipe's limit.**
- **Prices also show as time:** *"Market Hall · 50 min of your income + 500 bricks"*.
- **One growth line, "what stops you",** on the return card and each producer's panel: the solver's binding limit, its
  key and its price. *"Sells 100 of its 882: the grey pipe to your Trading Post · Copper Pipe, 28 tiles: 280 g + 28
  copper ingots · Start it ▸"*. Once the key is in, it reads *"Max now pays: +X g/min for Y ▸"*, or stays quiet.
- **Undo for an hour:** a full refund, and the XP comes back too.

### Step 2 · Wants (row 7) (7–9 days)

**Replaces:** `Pay()` from your own Storage, "N in your storages", Deliver, and the Plan board's Ask form.

**What changes:**
- **A want is a scaffold.** A building, upgrade or pipe tier short of materials stands on the map and **wants** them
  (*"Big Factory · wants 100 bricks · 40 in"*).
  - It runs on the existing Ask engine (`MatchAsks`, every Yard round).
  - Its goods go into the build, never a Storage.
- **How it fills, in order:**
  1. your own producers;
  2. a friend's **Send**, or one tap from their Storage (*"Ana's Big Factory wants 100 bricks · your Storage holds 500 ·
     Send them ▸"*), named at both ends with Thanks;
  3. the station's spare through the Yard at 1.3 × base, by itself, even while friends sleep;
  4. an **Order** (step 4).
- **Who sees it:** a want shows once, to a friend who is playing and can fill it. It is never shown to someone away,
  never as "unanswered", and nobody is listed as not having helped.
- **When nobody near makes the good,** it shows the chain: *"a Brickworks uses your stone and Bertha ⚙'s coal · Show
  me"*.
- **Cancel · your gold back** until it stands.

### Step 3 · The pipe is a want (1–2 days)

**Replaces:** the silent Copper Pipe click.

A capped link offers its next tier in place, as a want:
- A Copper Pipe wants 1 copper ingot a tile, and copper ingots are copper + coal, usually two seats on dealt land. So
  the pipe wall becomes the most natural gift between friends.
- Cleo's 28 ingots are about six minutes of one Smelter.

### Step 4 · Order the rest (1 day)

**Replaces:** the Ask's deadline fill, and Buy now, Fund, Pay the rest and Commission, which become one verb.

| Rule | Value |
| --- | --- |
| Price | **2.0 × base**, the Ask's existing House price (`GameWorld.Asks.cs:33`). The gold leaves the game |
| The friends' window | You order at once, but the hub starts only once every **lit** resident of the big station (played in the last 3 days) who makes the good or holds it has had one session since the want was raised. At most 72 h; **zero** if there is no such resident. The card says *"from Cleo's next session, at the latest Thu 20:00"* |
| The pace | Then evenly over **6 hours** |
| Friends first | Every round, Sends, Storage taps and the station's spare go in first. What they bring comes off the order, with its gold refunded |
| Who may order | The owner; anyone in the big station for any want there (*"Order the rest for Ana ▸"*); anyone in the Capital for the wonder. Never between same-device accounts |
| Goods | Into the want only, so nothing can be resold |
| Shared goals | On a stage, a wonder stage or a keystone, orders fill at most **half**, count **25%** of credit and carry no 💛 |

**Worked:** Ana's Big Factory costs 1,000 g by order, the Market Hall 5,000 g, and Cleo's 28 ingots about 1,000 g.

**Why this shape:**
- Price protects nobody: 100 bricks at 2.5× would be 0.003% of Ana's 39.8M.
- A window in hours always lands before players who return every 2–7 days.
- Counting lit players' sessions gives the friend a turn without waiting on someone who has stopped (Ben was away 33
  days), and Mia, with no maker near her, has no window at all.
- After the window, the flow still loses the race to a friend: a Brickworks makes about 12 bricks a minute, an order
  ships 100 at 0.3.

### Step 5 · The Era wonder, the Capital's want (3–3.5 days)

**Replaces:**
- the Wonders minigame;
- Deliver for the wonder (Dev's was disabled, dev d21 #176–#178);
- a first stage sized at world start for 60 g/min;
- *"Era 1 ended without me knowing"* (ana d27 #318).

**What changes:**
- **A buyer in every Yard round,** after the station's project, at 1.0 × base.
- **Each stage is sized when it starts, at 6 h of the Capital's summed measured income** (as `GameWorld.Wonders.cs:116–126`
  already does for later stages; the first is resized on day 2). That is about 1.38M of goods on run 3's day 15.
- **Half of each stage comes only by pipe,** with Houses at most a quarter. The other half is open to orders, shown on
  a **Patron** line.
- **Each finished stage gives every seat in the Capital a bottleneck perk** for the Era: first grey pipes +50%, then
  Hub Orders +25%.
- **Personal tiers pay hours of your own income.**
- **When the last stage stands, the next Era starts early.** Run 3's Era 2 came on its deadline, day 22, with the
  Dynamo at 0.
- **Every card tells your part and the result.**

### Step 6 · Points have a use (3 days)

**Replaces:** a Lab eating about 400 g/min of ingots for points nobody could spend.

- **A Lab with nothing to research rests** and eats nothing.
- **Refinements follow an Era's tree,** one at a time, kept running while you're away, at 5k × 1.6ⁿ points:
  - first the wall-raisers: pipes +10%, trading +10%, Hub Order +5%;
  - then +2% to one chain;
  - each line capped at +50%.
- **Each applies to every resident of the station who runs that chain,** named: *"Bricks +6% · Cleo's research 💛"*.
- Cleo's 1.6M buys eleven (Σ for n = 0–10 ≈ 1.46M).

### Step 7 · Hub orders as buyers, and the planks check (1–2 days)

**Replaces:** the Storage chore behind daily orders (none delivered after day 14 in run 3; run 1's *"Your storages
have no iron ore"* STUCK).

- **A daily order is a buyer at your trading post for 24 h,** filled from what flows past.
  - It is sized at 30 minutes of measured income and pays 2.0 × base.
  - It is offered only to lit players and shown once filled.
  - It is personal: no Send into someone else's order.
- **Every land good has an Era-1 want:** for example, the Warehouse and the Cargo Depot want 100 planks each.

### Step 8 · The two-friend keystone (2 days)

**Replaces:** water as the keystone in all four stations, which needed nobody.

- **From the Water Tower on,** where two residents who are both lit when the stage starts each make one input of bricks
  (stone + coal) or copper ingots (copper + coal), that good is the keystone, at 3 h of one level-1 maker.
- An away seat's input comes from its House at 1.4, uncapped, and the built 2-day pass-over stays.
- Elsewhere, today's rule; a station of Houses never gets one.
- The run's only "together 4" had exactly this shape (cleo d14 #341–#368).

### Step 9 · Widen Copper and Steel links (1.5 days)

**Replaces:** nothing. It is the one private sink that grows with income.

- **A Copper or Steel link at its limit widens** ×2 for 1 hour of measured income, ×4 for 2 more hours; the cap is ×4.
- **Grey links don't widen,** so the first door past 100 a minute stays a friend's copper.
- **Past ×4, the next tier is a want.**

**Total: about 22–27 working days,** about 12–16 of them the analysts' rows 6, 7 and 10.

**Later,** only if the advisor bot still banks over 3 days of income on day 28:
- the station's Landmark tiers;
- the Capital's lots;
- a branch next to a friend;
- "While I'm away".

## 3 · The three open questions, decided

1. **Can gold buy a want's materials?** Yes, never at once: an Order at 2.0 × base, after a window counted in lit
   friends' sessions (at most 72 h; zero with no lit maker), shipped over 6 h with friends' goods first and refunded.
   At most half of any shared goal, credited 25%. The gold goes to the hub, never between players.
2. **Do hub orders become buyers?** Yes: at your trading post, 30 minutes of income, 2.0 × base, personal. They are
   built after the doors, because they add a want, not a door.
3. **The two-friend keystone?** Now, with the guard: both seats lit at the stage's start, an away seat's input from its
   House, the pass-over kept.

## 4 · How run 4 shows it worked

| Mark | Pass | Run 3 |
| --- | --- | --- |
| Gold spent | Every crew member with 8+ sessions spends ≥ 20% of earnings by day 36 | 0–5% |
| Spending that grows | Crew income +30% and 4+ buildings each, days 14–36. Spending ≥ 20% with growth under 15% fails the design | +3% from day 21; 3 buildings in all |
| Friends hold the keys | Of want materials from anyone but the owner, ≥ ⅓ from friends and neighbours, in every crew | none |
| The player alone | Mia beside Jo adds ≥ 3 buildings days 8–15 and spends ≥ 20% by day 15 | 10 → 11; about 5% |
| The Era | The Dynamo's stages stand before the deadline with 3+ contributors, told on every card | 0 / 1,800, no word |
| Keys and points | Every completed key raises sales within one round; no Lab runs with nothing to research | 1.6M points banked |
| Diaries | No "gold feels empty", "pointless" or "trap" after day 10 | at least 5 |
| Bots | `typical-advisor` banks ≤ 3 days of income on day 28; `typical` reaches Level 30 on day 25 ± 2 | 107M banked |

## 5 · Where we still differ

| Point | Decided | The other side |
| --- | --- | --- |
| The Order's price | 2.0, the existing Ask price (lonely, psychologist, economy) | 2.5, the hub's sell price (wild, friends, outsider). Both agree price protects nothing at these banks |
| Window and pace together | A lit-only window in sessions, then a 6-hour flow | Friends and outsider: ship at once after the window. Psychologist and wild: no window; flow from the tap, so something moves. The window gives a lit friend a turn, and the flow keeps the friend winning after it |
| Widen | Copper and Steel links only (friends) | Lonely and outsider: grey too, so a player alone can widen at once. Wild and psychologist: none, as it undercuts the friend's copper |
| Landmark tiers in run 4 | Later | Friends: now, as the station's growing want. Psychologist: no station passed its Workshop in run 3, so they'd go unseen |
| "While I'm away" | Out | Friends keep it. Lonely, wild, outsider and psychologist: a gift nobody chose isn't felt, and Ben's goods already flow by the Yard |
| Undo | Kept, with the XP back | Psychologist: the preview, Max at the pipe and Cancel cover it |
| Where the keystone starts | The Water Tower | Psychologist: the Workshop |

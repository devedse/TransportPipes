# Round A · friends: gold piles up because growth has nobody waiting for it

I designed v1.2 around Ana, Ben, Cleo and Dev, and Send and "runs on Ben's sand" in the redesign. My answer to the
owner's question:

- **Gold itself is not what's missing.** In run 3 a player with 30M could still have spent it all on upgrades.
- **What's missing is a reason for the extra output.** An upgrade pays only if its output can leave by a pipe and
  somebody wants it.
- **From week 2 both stop:**
  - the grey pipe carries 100 a minute;
  - the hub pays less for each extra unit you sell it;
  - the buyers that don't run out (a friend's build, the station's project, the Capital's wonder) couldn't reach your
    goods, or were frozen.

So the fix is to make your growth something a friend is waiting for. Gold then follows by itself.

## Diagnosis: the wall, the missing sinks, or the loop?

**Not the missing sinks.**
- Upgrades are an unlimited sink. A level costs the building's price × 1.15^(level − 1) (`Definitions.cs:301–302`,
  `costPerLevel: 1.15`). Taking one Big Factory (12k) from level 25 to 50 costs about 12k × (1.15⁴⁹ − 1.15²⁴) ÷ 0.15 ≈
  **73M**. A Coal Pit band of 25 levels costs millions.
- The bank is the right size for that: Ana 39.8M, Cleo 32.3M, Dev 18.9M on day 36 (metrics, last sample).
- Cheap growth sat unbought too. **Ana owned 4 parcels, the start area, on every sample from day 2 to day 36**
  (metrics `parcels`), with 39.8M in the bank and Level 24. Parcels cost 5k doubling.
- Gold was never the limit. *Wanting* something was.

**The wall is real.**
- Materials come only from your own Storage (`Pay()`, `GameWorld.cs:753`).
- Every bigger building needs a material (Big Factory 100 bricks, Steel Mill 300, Market Hall 500, Big Turbine 20
  steel; `buildings.yaml` 314–347, 545, 66), and the pipe past 100 a minute needs copper ingots or steel (`pipes.yaml`).
- Cleo's level-25 pit made 126 of 882 a minute and sold 100 (cleo d21 #393). Her Copper Pipe did nothing without
  ingots (#399–#404).
- The bot shows it with no story: flat at 2,884 g/min with 101 buildings from day 4 to day 9
  (`balance-typical-advisor.md`).

**The deeper cause is in the loop.**
- **Private growth ends where someone else's goods begin.** Every way past the wall (bricks, copper ingots, steel) is a
  good that dealt land put on a friend's seat. We did that on purpose, so that friends would matter.
- **The friend was then the door, and the door had no handle:**
  - *"I don't know how to hand you 100 bricks directly"* (Cleo, group, day 21);
  - Dev: *"how do I hand ingots to the Dynamo?"* (dev d21 #176, "Deliver from storage" disabled #178).
- **Gold stayed private while the reasons to play became shared.** From day 8 the "yes, I'll come back" answers were
  all about a friend or a shared goal:
  - *"my friends are waiting on me"* (dev d08);
  - *"the friends need my stone"* (ana d14);
  - *"a concrete shared goal with bricks flowing"* (cleo d14);
  - *"Ana is counting on iron ingots"* (dev d14).
- **The "maybe"s were about gold with no friend in it:**
  - *"1.7M gold and not knowing what to spend it on that helps friends"* (dev d14);
  - *"3.5M gold has little to buy"* (cleo d06);
  - *"the gold feels empty"* (mia d15).
- **The friends offered gold to each other**, with no way to make it count: Cleo *"offered money"* for wood (cleo d06
  diary), Ana *"offering gold"* for bricks (ana d21 #274, #309).
- **So gold needs one job:** turning *your* growth into what *they* are waiting for.

## The proposal: "every want waits on the pipes, and a friend can fill it"

I adopt row 7 whole, add three things for friends, and adopt row 10 with one friends' change.

### 1 · Every want is a buyer on the pipes (row 7, adopted)

**What becomes a want:** a building, an upgrade or a pipe tier that needs materials, the station's keystone, and the
Capital's Era wonder. Each one *waits for* its goods instead of refusing.

**What the player reads:**
> *"Big Factory · waits for 100 bricks · Cleo makes bricks"*

**How a want fills, in order** (built on `MatchAsks`, which already runs in every Yard round):
1. your own producers;
2. a friend's Send, or one tap from a friend's Storage;
3. the station's spare, through the Yard at the one price, named at both ends;
4. an Order (§3);
5. a House at the deadline.

**The goods go into the build, not a Storage.** This replaces:
- Deliver from Storage;
- "N in your storages";
- the Plan board's Ask form;
- the Wonders minigame;
- the Storage chore behind hub orders.

### 2 · The friend's handle: a want shows once, to the friend who can

On the return card and the producer's panel of the friend who makes the good, or who holds it in a Storage:

```
│ 🧱 Ana's Big Factory waits for 100 bricks        │
│    your Storage holds 500       [ Send them ▸ ]  │
```

**The tap moves the goods from her Storage into Ana's build** at the one price, named at both ends: *"Cleo's bricks
built your Big Factory 💛 Thanks"*.

**When it shows:**
- once per want, and only to a friend who is playing now;
- never "unanswered";
- never to an away friend;
- a stranger only after a Handshake.

These are the rules the redesign already set for bids.

**A running gift keeps flowing.** A friend's producer whose good a want needs already gets "Send ▸ Ana's Big Factory"
on its panel. With a Send, the goods go there every round while both are away. That is how the one chain friends felt
in run 3 ran: Ana's stone into Cleo's Brickworks, 308k stone over a week (findings, "The first chain between friends").

### 3 · Gold buys the rest, never at once: an Order

**Any want in your big station can be finished with gold:** your own, a crew mate's, the station's project, or the
Capital's wonder.

```
│ Big Factory · waits for 100 bricks                 │
│ [ Order the rest · 1,000 g · by tomorrow ]         │
```

**The rules:**
- The hub sells at 2.0 × base, and **ships evenly over 6 hours**.
- Each round, friends' goods and the Yard go first. What they bring is taken off the Order, and the unused gold is
  refunded.
- Anyone in the big station may order for anyone's want. It reads *"Dev ordered the rest of your bricks 💛"*. The gold
  leaves the game.
- It is blocked between same-device accounts.
- Ordered goods count 25% toward project and wonder credit, as Houses' and the hub's do today.

**Why paced and not instant:**
- An order is cheap: 100 bricks are 1,000 g, and the Dynamo's 1,800 bricks and 900 ingots are about 43k.
- Instant, it would make every friend's goods pointless against a 39.8M bank. That is the player researcher's fear,
  and it is right.
- Paced, a friend's tap from Storage is immediate and a friend's Brickworks at 12–55 a minute beats the order's 5 a
  minute on the Dynamo.
- **Gold makes sure nothing waits forever; friends make it fast and cheap.**

### 4 · The pipe wall gets the same door

- **A pipe tier is a want too.** Cleo's Coal Pit panel, instead of a silent no-op:

  ```
  │ Its pipe carries 100/min of 882 · Copper Pipe here: 6 tiles │
  │ waits for 6 copper ingots · Dev makes them [ Send ▸ asks ] │
  │ [ Order the rest · 216 g · by tomorrow ]                   │
  ```

- **Max stops at the pipe** (row 3) and offers this want as the next step.
- Once the pipe carries 500 a minute, the pit's upgrades pay again. Cleo's bank goes back into her buildings, and the
  coal goes to the Brickworks that make bricks for her friends' wants.
- Pipes stay the heart: your growth reaches a friend through a better pipe that a friend's copper paid for.

### 5 · Wants that grow with the crew, so there is always one

| Want | How it grows |
| --- | --- |
| **The Capital's Era wonder** | Sized at each stage's start from 6 h of the Capital's spare of its goods (row 6), so it grows as the crew grows. A tracker line with your part: *"Great Dynamo 41% · your bricks go in"*. Its end is told on every card |
| **The station's project after the Landmark** | It keeps going as **station levels**. Each level is a basket sized from 6 h of the station's measured spare, with a two-friend keystone where a pair exists, and a visible addition to the Yard and the gate strips: lamps, an arch, trees. Looks are primary, and this is expansion, never rebuilding |
| **Era 2's buildings** | Each needs another seat's material (Steel Mill 300 bricks, Market Hall 500, Big Turbine 20 steel), so each is a want a friend fills |

### 6 · Research: row 10 adopted, and Refinements are for the station

- Labs rest when there is nothing to research.
- After an Era's tree, Refinements follow (row 10's numbers: +2% to one chain, 5k × 1.6ⁿ points, capped at +50%).
- **The friends' change:** a Refinement applies to **every resident of the station** who runs that chain, and is named
  on their panel: *"Bricks +6% · Cleo's research 💛"*.
- No points move.
- Cleo's 1.6M points (day 21) buy about 11 Refinements (Σ 5k × 1.6ⁿ, n = 0–10, ≈ 1.46M), about +22% to the chains her
  friends run. Her Lab's 400 g/min of ingots then buys something for someone.

## The three open questions

**1. Can gold buy at once? No: an Order, paced over 6 hours, at 2.0 × base, with friends' goods first.**
- Anyone in the big station may order for anyone's want, the gold goes to the hub, and same-device accounts are
  blocked.
- **Systems' "a House fills a friend's need at 2×"** is the same price. I add the pace and "friends first", because
  without them a 39.8M bank makes Cleo's 500 bricks worthless.
- **Social's "pay the rest to the hub, never between players"** is kept exactly.
- **Player's "never at once"** is kept: 6 hours is one sleep.

**2. Do hub daily orders become buyers? Yes, at your own trading post, and they stay personal.**
- They fill from your production as it flows: no Storage, no Deliver.
- They are sized at 30 minutes of measured income (row 10).
- They are not a friend's want: there is no Send to someone's daily order.
- **Why:** run 1's *"Your storages have no iron ore"* STUCK, and no order delivered after day 14 in run 3 (findings, "Not
  chores").

**3. The two-friend keystone: now.**
- Bricks from Ana's stone and Cleo's coal, or copper ingots from copper and coal, wherever a pair exists. An away seat's
  input comes from the station's House at 1.4, uncapped (the redesign's backstop). The 2-day pass-over still applies.
- **Why:** the run's only "together 4" had exactly that shape (cleo d14 #341–#368), and water, the keystone in all four
  stations, needed nobody.
- The player researcher's worry, that run 4 becomes unreadable, is smaller than the cost of a fourth run in which the
  shared goal needs no friend.

## Walk-through: weeks 2–5

**Cleo (optimiser, Coal Valley, bricks).**
- **Day 10.** The Big Factory no longer refuses with *"You need 100 bricks in your storages"* (cleo d10 #280). It reads
  *"waits for 100 bricks · you can make bricks: a Brickworks (stone: Ana)"*. Her 449k points buy station Refinements.
- **Day 14.** Her Brickworks runs on Ana's stone, as in run 3. Its bricks fill her own Big Factory first (about 8 minutes
  at 12 a minute), then flow to the Dynamo every round instead of to the hub (*"12 bricks/min sold to hub"*, cleo d21
  #421).
- **Day 21.** Her card: *"Ana's Big Factory waits for 100 bricks · your Storage holds 500 · Send them ▸"*. One tap. Her
  pit's Copper Pipe want is filled by an Order (216 g) or by a friend's ingots. Her pit sells 500 a minute, and her
  upgrades pay again: a 25-level band on her buildings takes 1–4M each.
- **The pay-off is the one she asked for in run 1:** a payoff for helping, in her friends' buildings.

**Ana (stone, iron, the organiser).**
- **Day 14.** Her plan in the chat (Cleo bricks, Ana stone, Dev ingots; group day 14) is now the game's plan. The
  Dynamo's tracker shows each part.
- **Day 21.** Her Big Factory stands on Cleo's bricks the evening she asks. She orders the Dynamo's rest for the
  Capital (about 40k), and Cleo's and Dev's goods take most of it off the order as they arrive.
- **Day 22.** Tier 1 stands; *"Era 1 ended: the Great Dynamo stood · your stone, Cleo's bricks, Dev's ingots"* is on
  every card, not *"Era 1 ended without me knowing"* (ana d27 #318).
- **Weeks 4–5.** Her Market Hall (500 bricks) and a Steel Mill site wait on friends. Her 39.8M goes into buildings that
  now have both a pipe and a buyer. Parcels follow, because the new buildings need room.

**Dev (Old Forest, a week late).**
- **Day 14.** *"1.7M gold and not knowing what to spend it on that helps friends"* (dev d14) gets two answers on his
  card:
  - his Small Factory's ingots go to the Dynamo by themselves (*"your ingots go in · 6/min"*), so he is not "late and
    useless" (dev d21);
  - *"Order the rest for Ana ▸"* on her Big Factory.
- **Weeks 3–5.** His wood and planks fill the station level's basket, and his gold goes into his Sawmill chain, which
  now has a buyer beyond the hub.

**Ben (stops on day 3, back on day 36).**
- While he's away, no want ever waits on him. His seat's good comes from the House backstop at 1.4, and any want can be
  ordered.
- **Day 36:** his card names what his goods went into (*"Your wood went into the Water Tower and Ana's Sawmill"*) and
  offers one want he can fill now. His 5.5M has the same use as everyone's: the next pipe and the next level, now with
  a buyer.

**Mia, alone beside Jo.**
- Jo's factory buys her energy (19–26% of her income in run 3).
- **Her wants never stall:** a Market Hall's 500 bricks, which nobody near her makes, read *"Order the rest · 5,000 g ·
  by tomorrow"*, with the Houses' land goods at 1.4 under the floor. Gold has a use for her too, as an Order and as
  upgrades past the pipe.
- **Her shared goal is the Capital's wonder.** Its tracker names every contributor across stations (*"Great Dynamo ·
  Ana, Cleo, Dev, Mia, Jo"*), so the alone player is inside the one goal the crew next door shares. She can Send or
  order into it like them.

## What it costs to build

| Part | Effort |
| --- | --- |
| Row 7, sites on the Ask engine, the wonder as a Capital buyer | 6–9 days (systems 6 + 3, social 12) |
| The friend's handle: Send from Storage into a want, wants shown once to friends who can | +1 day (the social face) |
| Orders paced over 6 hours, friends first, open to the big station | +1 day |
| Pipe tiers as wants, Max stopping at the pipe | inside rows 3 and 7 |
| Station levels after the Landmark (baskets and art) | +2 days, plus generated art through `process.mjs` and `fit.mjs` |
| Row 10, with Refinements for the station | 3–4.5 days, +0.5 |

**In all:** rows 7 and 10 plus about 4.5 days.

**What it replaces:** Deliver from Storage, "N in your storages", the Plan board's Ask form, the Wonders minigame, the
Storage chore behind hub orders, and an Invest line that offered 13k against a 27.8M bank (ana d27 #317).

## Risks

| Risk | Answer |
| --- | --- |
| **Orders making friends irrelevant** | Pacing and friends-first. If run 4's wants are filled mostly by Orders and Houses (social's test: under a third from friends), lengthen the pace to 12 hours |
| **Obligation** | A want shows once, to a friend who is playing, never "unanswered". Nobody's own build ever waits on a friend, because the Order exists |
| **Alts** | Orders are blocked between same-device accounts and count 25% toward credit. They cost 2× base, so an alt funding a main's site buys goods at twice their value: a lossy, bounded route |
| **The phone** | Each gift is one tap on the card (*"Send them ▸"*, *"Order the rest"*). Ben needs no drag and no panel |
| **More words** | Only "waits for" and "Order". "Site" is never shown |

## How run 4 shows it worked

- **Built together:** by day 14, every crew member with 3+ sessions has a friend's goods in one of their builds,
  named at both ends (the review's headline).
- **Gold has a use:** from day 14 to 36 the crew spends 20% or more of what it earns, and adds 4+ buildings each. In run
  3 it spent 0–5% and added 3 between them.
- **Friends, not Orders:** at least a third of the materials that filled a want came from friends.
- **The wonder stands** before its deadline with 3+ contributors, and every card tells the result.
- **The late game holds:** from day 18, fun 3.5+ and "come back" yes in 3 of 6. The diaries' reasons should read like
  *"Cleo is waiting for my stone"*, not *"gold feels empty"*.

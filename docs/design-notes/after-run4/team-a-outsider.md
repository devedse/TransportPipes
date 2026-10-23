# Round A · outsider: put the door where the player stands

My verdict before the details: **the nine steps were the right idea, built in the wrong places and at the wrong
size.** Every door to growth sat in a panel the players had stopped opening. The place they did open, the upgrade
panel, ended in *"would make nothing more"* with no button. The one door priced in hours of income (Widen) was closed
to the only pipes that existed, grey ones. That last part was my concession in round C. I was wrong.

## 1 · Diagnosis: why the nine steps didn't move the gold

**1. The doors stood where nobody looked.** Run 4 tells us exactly where players look when they want to spend.

| Where players looked | What it said at the pipe | What it offered |
| --- | --- | --- |
| **The upgrade panel**, every session | *"+41 would make nothing more"* (ben d36 #80–#82). Ben: *"does it even earn? dont trust"*; *"I still don't know if upgrading earns anything"*. Cleo found the same line contradicting the preview beside it (cleo d06 #160) | Nothing. The pipe tier was offered only in a full building's *idle* panel (`GameWorld.IdleTexts.cs:145–162`), which Cleo never opened again after researching Copper Pipes on day 6 (findings, step 3). The upgrade panel's line (`BuildingPanel.tsx:856`) has no button |
| **The return card** | One growth line, which my round-C budget folded | *Order the rest · 1,000 g* was offered once, folded on Ana's card for Dev's factory, and never pressed (step 4) |
| **The Wonders tab** | Bars | The Order button vanished at half, without a word (ana d36 #355–#364; Ben: *"only see bars"*) |
| **The top bar** | *"39.8M"* | Nothing |

So step 1's "what stops you" told the truth, as Cleo's ten exact previews show, and then stopped: **it named the lock
and hid the key.**

**2. The doors were capped exactly where the wall was.**
- **Widen took only Copper and Steel pipes** (`Widens(tier) => tier is 2 or 3`, `GameWorld.Widen.cs`). All 1,087 pipe
  tiles in the world were grey at the end, though Cleo made 237k copper ingots. Widen was never offered once.
- **Orders on a wonder stage stopped at half.** The half that needed a maker needed bricks, steel and concrete nobody in
  the Capital made, so the Dynamo sat at 89% and the Cathedral at 50%.
- **Wants arose only on buildings that need materials:** two in 36 days, both bricks, both "is being built".

**3. Even open, they were crumbs.**
- The players earned 1.4–4M a day.
- A Copper Pipe on a 28-tile link costs 280 g and 28 ingots, an order 1,000 g, a wonder order 9–15k. Both Era stages
  were sized at the 60 g/min floor (about 21.6k of goods), by the bug the brief describes.
- The only prices that grow with income were Widen (closed) and the wonder (mis-sized).
- So even a player who found every door would have spent a rounding error.

**4. Growth came free, so gold had no job.**
- Crew income rose **39.7% from day 14 to day 36** while the crew spent 0.2% (findings, `gold`). The rise came from
  stage perks, Refinements paid in points, cheap levels and hub orders that fill themselves.
- One week of Cleo's hub-order bonuses exceeded everything she spent in 36 days.
- My own run-3 falsifier (*"spending ≥ 20% with growth under 15% fails"*) was met in reverse: growth without spending.
  An idle game whose growth doesn't need gold has made gold decorative.

**5. After day 17, nothing shared to build.** Capital SE's Landmark stood on day 17. From then on the This week line
offered a 10k parcel: days 18–36, fun 2.83, "come back" yes 2 of 6.

**Why the bots spent and people didn't** (question 5): the advisor puts one ranked buy, sized to the bank, in front of
the bot every session, and the bot presses it. Cleo's bot took an Iron Mine to level 69. People saw the same building
with *"+0 g/min"* and no button. The doors were in the advisor, not on the screens.

**But even the bots** held 2.7–18.9 days of income on day 28 (Ana's bot 167.9M). At a 17-building cap, on grey pipes,
the advisor ran out of buys too. So the bots prove both halves: show the buy, and size it to income.

**In short:** not too small an idea. Doors in the wrong places (1), capped at the wall (2), priced in absolute gold
(3), against growth that came free (4), and no shared goal after day 17 (5).

## 2 · Proposal: "the door is where you stand"

Five changes. Each is small, and together they make one rule: **wherever a player looks for something to spend on,
the screen names the one buy that pays, priced in hours of their income, with its button.**

### A · The upgrade panel at the pipe becomes the door

Where it says *"+41 would make nothing more"* today, it says:

> **Past level 21 it adds nothing: its pipe carries 205 a minute.**
> [ Copper Pipe · 28 copper ingots + 280 g · carries 500 ▸ ] *Cleo makes copper ingots*
> [ Widen the pipe ×2 · 166k (1 hour of your income) · carries 410 ▸ ]
> *then 14 more levels pay +620 g/min*

- **Both doors, side by side,** the friend's door first. A Copper Pipe gives ×5 for almost no gold, and Widen ×2 gives
  ×2 for an hour of income. The friend's copper stays the better deal, which keeps the friend's role.
- **When the door is through,** Max comes back with its preview (*"+14 levels · 2.1M · +620 g/min"*).
- **The same two buttons** appear in the building's idle panel, as today.

### B · Widen every tier, grey included

- **Grey:** ×2 for 1 hour of measured income, ×4 for 2 hours more; the cap is ×4, which is 400 a minute.
- **Copper and Steel:** as built (×2, ×4).
- **Per full link:** each link is its own buy, so an estate with ten producers at the pipe has ten widenings to buy.
  For Cleo at about 2,800 g/min that is 0.5M each at ×4.
- **Why this is still fair to the friend:** grey ×4 (400) never reaches a single Copper Pipe (500), and copper widens
  to 2,000.

### C · The best buy on the top bar, and a growth line that never folds

- **The gold line** reads *"39.8M · 14 days of income · best buy: Widen your Coal Pit's pipe ▸"*. It is the advisor's
  top step, sized to the bank, as the bots get it. With nothing worth the bank, it shows the days only.
- **The return card's growth line never folds.** It always carries a button, and it is the same buy the top bar names.
  My round-C fold is what hid the only Order anyone was offered.

### D · Shared goals sized to income; orders finish what nobody makes

- **The sizing.** Every Era stage, station stage and Landmark floor is sized when it opens, at hours of the group's
  **7-day measured income**: the summed `earned` per minute of residents who played in the last 7 days, over 7 days,
  never below the stage before.
  - That fixes the bug: the Cathedral shrank when everyone had been away 3 days.
  - The Era stage stays at 6 hours. Sized that way, run 4's Cathedral would have been about 1.6M of goods, with its
    order half about 1.6M of gold (the brief).
- **When nobody can make a good**, meaning no active resident makes it and none could with one building on their land,
  the order covers the whole stage after the window (zero when no maker exists), at 2.0 × base. When someone makes it,
  the half rule stays.
- **The Wonders tab says it in one line:** *"Bricks: nobody in the Capital makes them · Order the rest · 4,500 g ▸ · or a
  Brickworks on your estate (Ana's stone, Cleo's coal) · Show me"*. This is the analyst's chain pointer, offered first to
  the player with gold and no role (Dev).
- **Ordered goods** keep their 25% credit and no 💛, so a friend's bricks still count four times as much.

### E · After the Landmark, its floors

- **Each floor wants goods by pipe:** two the station makes, and one it doesn't, which can be ordered when nobody can
  make it.
- **The size:** 6 hours of the station's 7-day income for floor 1, then ×1.5 a floor.
- **What a floor gives:** it rises on the map and on the picker's station card, and gives every seat +5% pipe
  throughput (a reward aimed at the bottleneck).
- **Gold's share:** the ordered part, which grows with the floors (2 × base on goods worth hours of the station's
  income).
- **It replaces** the This week line's 10k parcel.

**What it replaces, in all:**
- *"+N would make nothing more"*;
- Widen's grey refusal;
- the half-cap where no maker exists;
- the vanished Order button;
- a bare gold number on the top bar;
- folded growth lines;
- the 3-day sizing window;
- a station with nothing after its Landmark.

**What it adds on screen:** one line on the top bar and two buttons on a panel players already open. There are no new
nouns: Widen, Order and floor already exist.

## 3 · The five questions

**1. Can gold finish what no friend near makes? Yes, after the friends' window, and the whole of it when nobody can
make the good.**
- **Price:** 2.0 × base, as the Order is today.
- **What's left for friends:**
  - their goods arrive first in the window;
  - they get full credit and a 💛, where an order gets 25% and none;
  - they are the cheaper door: a friend's Copper Pipe is ×5 for 280 g, against Widen's ×2 for an hour of income;
  - the chain pointer gives a friend a role (*"a Brickworks on your estate"*).
- **The half rule stays where someone makes the good,** so a maker isn't bought out.

**2. How every late cost is sized.**
- **Private caps** (Widen): in hours of the buyer's own measured income.
- **Shared goals** (Era stages, station stages, Landmark floors): in hours of the group's 7-day measured income, never
  below the previous stage. Floors grow ×1.5.
- **Goods keep goods prices** (orders at 2.0 × base). A shared goal sized to income makes its order scale by itself, so
  bricks never cost a rich player more than a poor one.
- **The bug's fix:** 7 days instead of 3, and a floor at the previous stage.

**3. Where the doors are, and what each says when gold is plentiful and the building is at its pipe.**
- **Upgrade panel:** *"Past level 21 it adds nothing: its pipe carries 205 a minute"*, then the two doors and the
  levels they'd unlock (A).
- **Top bar:** *"39.8M · 14 days of income · best buy: … ▸"* (C).
- **Next steps:** the same ranked list the bot follows, with Widen, pipe tiers, orders and floors in it, sized to the
  bank.
- **Card:** one growth line, never folded, with its button.

**4. The shared goal after the Landmark: its floors** (E). Floors at ×1.5 a floor last the season. Floor 5 is about
5 times floor 1, about 30 hours of the station's income in goods. The Era wonder stays the Capital's goal alongside.

**5. The bots' mark.** The advisor's bot spends because one ranked buy is put in front of it, sized to the bank, every
session. People spend when the screen they're on does the same. The bots still bank 2.7–18.9 days because their
buys are capped too (17 buildings, grey pipes), so the mark tests sizing as well as visibility. Keep it (≤ 3 days on
day 28), with grey Widen on.

## 4 · Cost and risks

| Part | Days |
| --- | --- |
| A: the upgrade panel's door (reuse the idle panel's `IdleFix` for PipeTier and Widen) | 1 |
| B: grey Widen (`Widens` takes tier 1; the hours stay) | 0.5 |
| C: the top bar's best buy; the growth line never folds | 1 |
| D: 7-day sizing with a floor; whole orders when nobody can make; the Wonders tab line; the chain pointer | 3 |
| E: Landmark floors, plus floor art through `process.mjs` and `fit.mjs` | 3 + art |
| **Total** | **8–9 days plus art** |

**Risks:**
1. **Incomes jump** once every full link widens ×4. XP follows earnings, so Level 30's day moves. *Check:* the light
   bots with grey Widen on, Level 30 on day 25 ± 2.
2. **Gold replaces the friend.** Grey ×4 stays below one Copper Pipe, an order gets 25% credit, and the half rule stays
   where a maker exists. *Check:* at least a third of want materials still come from friends.
3. **One more line on the top bar.** It replaces the bare number with days and a buy. If a skimmer misreads it, fold it
   to the number on a phone.
4. **People may not want growth at all.** That is the mark below.

## 5 · Weeks 2–5, walked through

**Cleo** (optimiser; 29M on day 14, *"29M idle with nothing worth buying"*):
- **Day 6:** after researching Copper Pipes, her Coal Pit's upgrade panel shows *"Copper Pipe · 28 ingots (yours) +
  280 g"*. She has 237k ingots over the run, so she presses it on every full link.
- **Day 14:** the panel offers *"Widen ×4 · 0.5M"* per link and *"Max +20 · 3.1M · +1,400 g/min"*. She checks the
  preview, as she checked ten in run 4, and buys.
- **Day 21:** her top bar says *"best buy: Landmark floor 2 wants copper ingots · you make them"*. Her *"income without
  a sink is pointless"* has a sink that's a building.

**Ana** (organiser):
- **Day 14:** her grey links show the friend's door first: *"Copper Pipe · 20 ingots · Cleo makes them"*. The pipe
  becomes a want, Cleo's ingots arrive, and *"Cleo's copper widened your pipe 💛"*.
- **Day 17:** the Landmark stands, and floor 1 opens at once (6 hours of the station's income).
- **Day 27:** the Cathedral tab says nobody makes steel. She orders the whole steel half (about 1.6M after the 7-day
  resize), not 21k, and the Cathedral stands.

**Dev** (*"12M gold sitting around, tell me how to be useful :)"*, group, day 21):
- **Day 14:** the Wonders tab offers him the chain first: *"a Brickworks on your estate (Ana's stone, Cleo's coal)"*. He
  builds it, and his bricks finish the Dynamo before the deadline. He is the crew's brick seat.
- **Day 21:** his top bar's best buy widens his Sand Dredger's grey pipe.

**Ben** (phone; *"13.4M gold and nothing I understand to spend it on"*, d36):
- **Day 36:** his moily's panel no longer says *"would make nothing more"*. It says *"its pipe carries 113 a minute ·
  Widen ×2 · 24k (1 hour of your income) ▸ · then +9 levels pay +180 g/min"*. Two taps, and he can see what it earned.

**Mia, beside Jo:**
- **Week 2:** her links widen with her own gold, and a friend's copper isn't needed for her to grow.
- **Day 17 on:** her small station's Landmark gets floors from her goods and Jo's. Its missing good is orderable, since
  nobody near can make it.
- Her name is on each floor.

## 6 · The mark for run 5 that would prove me wrong

**Every crew member sees a door (Widen or a pipe tier) on the upgrade panel at a full pipe in at least three sessions
by day 14. If fewer than half of them press one, I'm wrong.** That would mean the cause isn't where the door stands or
what it costs: these players don't want more income, and gold needs a different job (looks, the shared place), not a
better door.

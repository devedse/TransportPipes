# The design team's answer after run 4: the door where you stand, and something to build together

**The owner's question, again:** *what does a player want to do with their gold in weeks 2–5, so there is a reason to
grow?*

The economy designer drafted this from the six round-B papers, for all six to sign or object to. Where the papers split,
it follows the majority and the evidence, and section 6 records the other side.

## 1 · What went wrong

**The idea was right, but the doors were too small, too capped and in the wrong place.** In run 4 the six players earned
281.8M and spent 0.58M. In days 14–36 the crew spent 143k of 166.6M, which is 0.09% (`marks.json`, `spendingThatGrows`).

**The pipe shut the one sink that grows by itself, and its door was hidden.** Levels are the game's own sink that grows
with income. The grey pipe shut them:
- The upgrade panel said *"Level 21: +0 g/min: its pipes carry 205 iron ore a minute"* and *"+30 would make nothing
  more"* (cleo d06 #220), with no button.
- The next tier was offered only in a full building's idle status (`GameWorld.IdleTexts.cs:145–162`).
- The advisor went quiet in three places (`GameWorld.Advisor.cs:503`, `:515–523`, `:548`). One of them hid Cleo's own
  door, though she made 237k copper ingots.
- Widen refused grey pipes (`GameWorld.Widen.cs:18`).

All 1,087 pipe tiles stayed grey.

**The Era wonder was sized wrong, then capped.** Both stages were sized at the 60 g/min floor, about 21.6k of goods
instead of about 1.6M (`GameWorld.Wonders.cs:139–142`; the freeze at the first delivery, `GameWorld.EraWant.cs:230`).
Their order half was one cap for the whole Capital (`EraWant.cs:195–198`). The first buyer used it up, and the button
vanished: *"only my name on the list"* (ana d36 #354), *"just tell me the button"* (Ben, group, day 36).

**Other doors stayed shut or small.** The two wants (both for bricks nobody made) read "is being built". Returns brought
days of income at once (Mia 2.2M and 7.6M, Ben 13M; mia d08 #87, d15 #119, ben d36 #77), against buys of 3–60k, one
click at a time. And growth came free: crew income rose 39.7% from day 14 to day 36 with no spending.

**Under that, the wish to spend was social, and it had no door.**
- All 30,660 g that our sinks took went into the one shared door in sight, the Era wonder's order (*"nice that I can
  chip in without building"*, ana d27 #316).
- Five offers of gold went into the chat (*"12M gold sitting around, tell me how to be useful :)"*, Dev).
- After the Landmark stood on day 17 the station had nothing to build together: fun 2.83 for days 18–36, and "come back"
  was yes for 2 of 6.

**The light bots show the wall is real.** The seats with copper ore banked 2.7–5.1 days of income on day 28; Ana, Dev
and Ben banked 10.8–18.9 (`light-bots-day28.md`).

**Our answer:**
- The panel where you stand opens the pipe and the levels behind it.
- Gold finishes what nobody near makes.
- Your gold shows first what it can do for friends.
- The Landmark keeps growing.

## 2 · The solution, in build order

Each step is a playable commit. Days are for one AI developer. **"Near"** means a resident seen in the last 7 days whose
buildings made the good in the last 24 hours, or who holds it.

### Step 1 · Sizing (½–1 day)

**The rules:**
- Shared goals count residents seen in the last 7 days (`ActivePeriod`, `GameWorld.Research.cs:8`), on their 7-day
  income, with a floor of 60 g/min for each seat taken.
- A goal is never sized while nobody has played in 7 days. It waits for a session.
- It is resized daily, upward only, until a quarter has arrived, and never goes below the stage before.
- An Era stage stays at 6 h of the Capital's income: about 1.6M of goods on run 4's numbers.

**Replaces:** the 3-day `IsActive` in `StageRequirements`, sizing at world start with only Jo seated, and the freeze at
the first delivery.

**Playable:** the Era stages ask a Capital-sized amount.

### Step 2 · The pipe door (2½ days)

On the upgrade panel at a full pipe, and as its step in Next steps, one button goes the whole way: research, then the next
tier on the building's full links, then Max to the new limit. It has one preview, in g/min and payback:

> *"Its Grey Pipe is full: 205 iron ore a minute · [Copper Pipe, then Max to 30 · 165k · +N g/min ▸] · your ingots"*

- **The ingots** come from your Storage, from a friend through the Yard (*"Cleo makes them"*), or by order at 2.0× base
  (36 g each) when nobody near makes them. Without the research, the button is *"Research Copper Pipes"* or *"Build a
  Lab · Show me"*.
- **At a Copper Pipe's limit:** Widen ×2 for 1 hour of your income, then ×4 for 2 hours more, then Max.
- **Widen is charged per full link.** Today one call widens any number of tiles for one price
  (`GameWorld.Widen.cs:52`).
- **The advisor's three dead ends** become the research, the Lab, the want or the order.
- **Grey Widen** (×2 and ×4, at most 400 a minute, below Copper's 500) is built behind a switch. It stays off unless the
  bot check in section 4 turns it on.

**Replaces:** *"+N would make nothing more"* (`BuildingPanel.tsx:856`), a bare "+0 g/min"
(`GameWorld.Spending.cs:66–71`), and the door that showed only in the idle status.

**Playable:** Cleo lays copper with her own ingots, and her line to level 30 costs 2.0M for ×4.7 output
(`Definitions.cs:294–302`).

### Step 3 · One order rule, with words (2 days)

**The rule:**
- **Nobody near makes the good:** anyone may order all of it at once, at 2.0× base, shipped over 6 hours as built. This
  counts for wants, pipe ingots, Era stages and storeys.
- **Someone near makes it:** their window first (as built, at most 72 h), then the flow. On a shared goal, half comes by
  pipe. When a friend starts making a good, the cap snaps back to half for whatever isn't ordered yet.
- **On the Era wonder, gold goes in by *Give*:** 1 h, 6 h or 1 day of your own income buys its orderable goods. Any
  number of players may give.

**The words:**
- **A button never vanishes:** *"The order half is in: Cleo 1 h, Ana 1 day · the rest comes by pipe"*.
- **A waiting building:** *"waits for 100 bricks · nobody near makes them · Order · 1,000 g ▸ · or a Brickworks on your
  estate makes them from Ana's stone and Cleo's coal; the Yard brings both · 1,400 g · Show me"*. The chain line goes
  first to the friend with gold and no maker role.
- **Next steps:** never *"+235 g/min"* for a building that can't stand, but *"+235 g/min once its 100 bricks are in"*.
- **The Exchange** says when it caps a bid, before the click.

**Credit:** ordered goods count 25% on a shared goal's Patron line. Goods bought for a friend's building carry a 💛 at
both ends.

**Replaces:**
- the half cap where nobody makes the good;
- the null offer and its after-the-click words (`EraWant.cs:195–198`, `:210`);
- "is being built" (`HoverTip.tsx:59`);
- the folded Order;
- the silent Exchange cap (cleo d14 #328).

**Playable:** on day 14 the Dynamo's last 450 bricks become a 4,500 g order, and Dev gets his line.

### Step 4 · Your gold can (2–2½ days)

**The top bar** reads *"40.6M · 24 days ▸"*, with a dot when the bank passes a day of income and something is listed.
Tapping it shows three lines at most, shared ones first:

1. **The open shared goal**, with *Give*: the storey once step 5 is in, until then the Era wonder.
2. **A want nobody near can fill**: a friend's building (*"Order for Dev ▸"*) or the Era's.
3. ***"Grow everything that pays · 4.1M of 13.4M · +420 g/min"***:
   - the pipe door on every full link, then Max wherever the payback is within 7 days;
   - one preview that lists its parts, one tap, Undo for an hour;
   - each part logged, so run 5 can tell which door worked.

**Elsewhere:**
- **Next steps** rank the same way. Once the bank passes a day of income, shared lines rank at least as high as research
  (`Advisor.cs:136–150`).
- **The return card's** first growth line is the list's top line, never folded.

**Replaces:** the bare gold number, the folded growth line, and one Max at a time for a player back after a month.

**Playable:** Ben's day-36 phone session gets two buttons that spend real gold.

### Step 5 · The Landmark grows storeys (3–4 days, plus 1–2 days of art)

When a station's Landmark stands, its first storey opens. The next opens the day after one stands, so its plaque is seen
first, and there is no end.

| Part | Rule |
| --- | --- |
| Goods | 6 h of the station's spare, in two goods that a playing resident makes, brought by the Yard. Never a good nobody near makes; no keystone |
| Purse | 1 day × 1.2^(n−1) of the station's 7-day income, counted as in step 1 and fixed when the storey opens. The gold leaves the game |
| Give | 1 h, 6 h or 1 day of your own income, as often as you like. Anyone in the district may give; a neighbour is named *"from next door"* |
| Half per giver | Nobody gives more than half a purse until the storey has been open 3 days; then anyone may finish it. The button stays live: *"Your half is in · anyone may finish it from Thu 20:00 · or Ana, Ben or Dev can give now"*. No cap at a station where no other resident has given in 14 days (Mia beside Jo) |
| Gives | A storey you can see; a plaque naming givers of gold and goods in hours of their own income; +10% on the residents' pipes, named like a Refinement (*"Pipes carry +10% · Capital SE's Landmark, storey 2"*), up to +50%, then the picture and names only |

**On run 4's numbers:**
- Capital SE on day 17 (Ana 663, Ben 297, Cleo 2,460, Dev 806 g/min): purses of about 6.1M, 7.3M, 8.8M and 10.5M, rising
  with incomes.
- Mia and Jo: about 1.7–2.0M for the first.

Storey art goes through `process.mjs` and `fit.mjs`.

**Replaces:** This-week's *"A new parcel: Level 7 · 10.0k g"* (ana d36 #375).

**Playable:** the crew has a shared build again from day 17, and it appears on step 4's list.

### Step 6 · The player alone is heard (1 day)

- If no other human at her station has chatted, her chat opens on the district channel.
- A newcomer's hello shows as one line in crews' chats.
- Every gift that stands a stage is named at both ends, strangers included: *"mia_pipes Pipes' wood finished your
  Water Tower 💛"*.

**Replaces:** five greetings into a channel holding only a silent Jo, and Mia-B's unnamed wood (director's observations,
rows 1, 5 and 11).

### The checks (½ day, plus the runs)

See section 4.

**Total: about 11½–12½ days, plus 1–2 days of art.** Steps 1–3 come before 5, so that a storey is never built on a
broken size or a vanishing button.

## 3 · The open questions, decided

| Question | Decided | Weight and evidence |
| --- | --- | --- |
| After the Landmark | **Storeys** with goods, a purse and a pipe perk. No Works | Six of six (wild withdrew the Works). The order rule and the chain line do its job, and the chain line gives the maker's role to a player: *"Bricks need stone + coal, I have neither"* (dev d14 #116–117) |
| Storey curve | **1 day × 1.2^(n−1)** | Three to three. Cleo and Dev play last on day 21, so later storeys rest on Ana and Ben. At day-17 incomes, storey 3 is 18.3M at *n* days and 8.8M at ×1.2. A goal that stalls was run 4's worst moment: the Dynamo sat at 89% from day 6 to day 22 (*"I feel I'm the only one pushing"*, ana d27) |
| Gold in a shared goal | ***Give* 1 h · 6 h · 1 day** of your own income, named in hours; half per giver for 3 days on a purse | Four to two on the cap. Cleo held 45.1M on day 17 and offered 15M on day 10: one tap would end a storey for everyone else |
| Price | **2.0× base** | Six of six (wild dropped 2.5×) |
| Credit | 25% for ordered goods on a shared goal; a 💛 for gold spent on a friend; purse gold named in hours | Six of six |
| Widen | **After copper, charged per link.** Grey only if the bot check fails | Three to three. The bot split points to the missing road to ingots, which step 2 opens to every seat. Two near-equal doors (400 and 500) on one panel cost clarity |
| One door or one plan | **Both:** one door per building on its panel, and the list on the gold, with the plan as its private line | Five of six (friends against) |
| Perk | +10% on pipes a storey, up to +50% | Five of six (outsider against) |
| The player alone | Channel and named gifts **in**; the branch **out**, by trigger | Six of six, lonely included |
| The mark | 20% of earnings in days 14–36, and three of four crew giving | Four of six |

## 4 · The checks before run 5, with their stop rules

| Check | Pass | Stop rule |
| --- | --- | --- |
| **Light bots** (`LightBotSimulation`, `TP_BALANCE=1`) with steps 1–5 on, pressing Give when it ranks | Every seat ≤ 5 days of income banked on day 28 (run 4: 2.7–18.9) | If a seat without copper ore (Ana, Ben, Dev) holds more than 5 days, turn grey Widen on and run again. If any seat still holds more than 10, a door is missing for that seat: find it before people play |
| **Pacing** (`typical`) | Level 30 on day 25 ± 2 | Before day 23: the storey perk drops to +5%, and grey Widen (if on) stops at ×2. Then run again |
| **Replay of row 12** from run 4's day-21 snapshot | Dev's card offers the Brickworks line, and the Dynamo's last 450 bricks can be ordered at once | If either is missing, fix it before run 5 |

## 5 · How run 5 shows it worked

| Mark | Pass | Run 4 |
| --- | --- | --- |
| **Gold spent** (the mark) | The crew spends ≥ 20% of what it earns in days 14–36, by its own taps. **Under 10%, with these doors in sight (logged), proves this plan wrong** | 0.09% (143k of 166.6M) |
| Givers | ≥ 3 of the 4 crew give gold to a storey, the Era wonder or a friend's building in days 14–36. If spending passes 20% but fewer give, the purse goes | 1 (Ana, day 27) |
| Returns | The median return after 4+ days away spends ≥ 10% of what came in while away, and Ben's day-36 return is not below it | 1.4%, 0.5%, 0.03% |
| The pipe | ≥ 3 of 4 crew lay a Copper Pipe by day 21 | 0; all 1,087 tiles grey |
| The Era | The Dynamo and the Cathedral stand before their Era ends | 89% and 50% |
| Friends still make | ≥ ⅓ of the goods into Era stages and storeys come by pipe from residents, and the chain line makes at least one new maker | 26% (Jo's iron ingots); none |
| After the Landmark | Capital SE raises ≥ 2 storeys by day 36; "come back" in days 18–36 yes for ≥ 4 of 6 | Nothing to build; 2 of 6, fun 2.83 |
| The player alone | Mia's first answer from a person by day 2; "alive" ≥ 3 | Day 15; 2, 2, 2, 3 |
| Diaries | No "nothing to spend on", "idle" or "pointless" about gold after day 10 | 4 (mia d08, cleo d14 and d21, ben d36) |
| Bots | Section 4's pass values | 2.7–18.9 days |

## 6 · Where we still differ

| Point | Decided | The other side |
| --- | --- | --- |
| Storey curve | ×1.2 a storey (economy, wild; psychologist ×1.25) | **Friends, lonely, outsider:** *n* days (1, 2, 3 …), which meets the banks and is easy to read |
| Half per giver for 3 days | In, with a live button and no cap at a one-giver station (psychologist, friends, lonely, economy) | **Wild, outsider:** no cap. Every cap in run 4 became a dead button, and credit in hours already stops a rich giver shaming the rest |
| Grey Widen | Off unless the bot check fails (lonely, outsider, psychologist) | **Wild, friends, economy:** on now, below Copper's 500. "Copper first" was tested in run 4 and gave zero copper tiles |
| The storey's goods | 6 h of spare, no keystone (friends, lonely, outsider, economy) | **Psychologist, wild:** 9 h of spare plus a two-friend keystone where a lit pair exists |
| The perk | +10% on pipes, up to +50% | **Outsider:** perks aimed at the station's bottleneck (grey pipes, Trading Posts, Hub Orders) |
| The plan | In, at most 7 days' payback, parts logged | **Friends:** one ranked buy at a time, since a bundle adds numbers rather than trust (ben d36 #80–#82). **Outsider:** at most 3 days' payback |
| The next storey | Opens the day after one stands | **Wild:** at most one a week |
| The word | "Storey" (psychologist, wild, lonely, economy) | **Outsider:** "stage", no new word. **Friends:** "ring" |
| Give amounts | 1 h · 6 h · 1 day (friends, outsider, economy) | **Wild, lonely, psychologist:** 1 h · 3 h · 1 day |
| The first Era stage | Sized at the first resident's session, then upward | **Friends:** wait for 3 active players in the Capital |
| The mark's second half | Three of four crew give | **Wild:** three of four each spend ≥ 10%. **Lonely, economy:** each return session (kept as the "Returns" row) |
| The Works | Out | **Outsider:** next, if run 5 shows the chain line makes no maker |
| The branch | Out for run 5 | **Lonely, friends, psychologist:** a trigger, if Mia's "alive" stays ≤ 2 with the district channel open |

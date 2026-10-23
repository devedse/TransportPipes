# Round A · wild

## Diagnosis: we built exits for gold; the game needed fuel

**Run 4 grew without gold, so gold had no job.**
- From day 14 to day 36 the crew's income rose **39.7%**, from 3,803 to 5,313 g/min (`marks.json`
  `spendingThatGrows`).
- In those 22 days Ana spent 62.8k of 33.0M earned, and Cleo 31.3k of 92.0M.
- The growth came from things that cost no gold: XP levels, stage perks, Refinements paid in points, and hub orders
  that fill themselves.
- Hub orders are a faucet: one week of Cleo's bonuses (*"+306,822 g over the hub's"*, d21) was more than she spent in
  36 days.

So the owner's sentence holds in a sharper form. **There was a reason to grow, but no reason to spend in order to
grow.** Our nine steps added exits for gold beside the growth engine, not fuel into it. A sink you don't need is a
sink you don't use.

**Every exit was small, capped or conditional, and together they had almost no room.**

| Step | In play | Why it couldn't take gold |
| --- | --- | --- |
| **The Era wonder's order half** | Took all of the 30,660 g our sinks got | Both stages were sized at the floor, 6 h × 60 g/min ≈ 21.6k g of goods. The Dynamo was sized at world start with only Jo seated. The Cathedral resized from players active in the last 3 days, after everyone had been away 3 days. `StageRequirements` (`GameWorld.Wonders.cs` 136–147) falls back to `MinMeasuredIncome` when `IsActive` finds nobody (`GameWorld.Market.cs` 204, `LitPeriod`). Sized right, about 1.6M of gold |
| **Widen** | Never offered | It waits for a Copper or Steel link at its limit, and all 1,087 tiles were grey |
| **The pipe tier as a want** | Never raised | It appears only as the idle fix of a building whose cause is `PipeFull` (`GameWorld.IdleTexts.cs` 145–162), and only in that building's own panel. Cleo made 237k copper ingots and never laid a Copper Pipe |
| **Wants** | Two | Both for bricks nobody in the Capital made, and both read as "is being built" |

**The doors stood where nobody looks.**
- The upgrade panel at the pipe says *"Level 21: +0 g/min: its pipes carry 205 iron ore a minute"* (cleo d06 #220) and
  offers nothing.
- The Order button on the Wonders tab vanished without a word: *"only see bars"* (ben, group, day 36).
- The players knew exactly what they wanted to do with the gold, help each other, and had no button: *"12M gold
  sitting around, tell me how to be useful :)"* (Dev, group, day 21), *"I wish bigger sinks existed"* (cleo d06 #209).

**Why the bots spend and people don't (question 5).** The advisor's bot buys every positive step every session
without asking whether it's worth it. People stop at the first *"+0 g/min"*, because the panel tells them the truth.
Ben: *"I still don't know if upgrading earns anything"* (d36).
- **But the bot is stuck too.** Ana's bot holds 167.9M, 18.9 days of income, on day 28, with seven Small Factories at
  level 19 (`light-bots-day28.md`).
- **The bot that spent** (Cleo's, 2.7 days) is the one whose estate could still add connections: nine Small
  Factories, each with its own grey link to an Iron Mine at level 69.

So the pipe wall binds bots and people alike. The bots only hide it by buying every small step until there are none
left.

**My verdict on our nine steps: the right idea, aimed at the wrong place.**
- We let gold buy goods around the wall (orders, wants).
- The game needed gold to buy **through** the wall: capacity, and machines that make what nobody near makes.
- It also needed a **door on the screen the player is already looking at.**

## The proposal: "gold buys capacity and machines, where you stand"

Three moves. Each puts gold *into* growth, at a price in hours of income, on a screen people read.

### 1. The pipe is bought on the panel where you stand

**The upgrade panel at the pipe becomes the door.** Its "+0" line turns into a button:

> *"+0 g/min: its pipe carries 205 iron ore a minute · **Copper Pipe on this link** · 18 tiles · 180 g + 18 copper
> ingots (you have 237k) · carries 500 · then Max pays +1,400 g/min ▸"*

If the ingots aren't in hand, they become a want (part 3).

**"Lay copper on every full link":** one button in the gold panel (part 4) with the total and the gain, for example
*"14 full links · 6.8k g + 680 ingots · +1,900 g/min"*.

**Widen on every tier, grey included:**
- ×2 for 1 hour of your 7-day income, ×4 for 2 more hours, capped at ×4.
- I argued in run 3 to keep grey closed so a friend's copper stays the first door. Run 4 shows that door was never
  used even by the player who had the copper: the copper wasn't scarce, the door was hidden.
- A Copper Pipe still beats a widened grey one (500 for 1 ingot and 10 g a tile, against 400 for hours of income), so
  copper remains the better door. Widen is the one that is always open, and the private sink that grows with income:
  after copper ×4 (2,000 a minute), it waits for steel.

**The real sink after the pipe is the one we already have:** building levels at 1.15× a level, which never run out
(one Big Factory from level 25 to 50 is about 73M). The pipe wall hid them for four runs. Once a pipe carries more,
*"Max now pays"* comes back to the same panel.

### 2. The station's Works: the next shared goal, and the maker nobody near had

When the Landmark stands (day 17 in run 4, after which *"the crew had nothing shared to build"*), the station starts
its **Works**: machines in the Yard that make what the Capital's wants and wonder need and nobody makes. Bricks first,
then steel and concrete.

**What players see.** The project card's next line:

> *"Capital SE's Works · Brickworks · 5.5M g (1 day of the station's income) · makes the Dynamo's bricks from Ana's
> stone and Cleo's coal · **Fund ▸**"*

Anyone in the station pays any part. Each funder's name and colour go on its plaque.

**How it runs:**

| | Rule |
| --- | --- |
| **Built by gold** | Machine *n* costs 2ⁿ⁻¹ days of the station's summed 7-day measured income: run 4's crew at 3,800 g/min gives 5.5M for the first and 11M for the second. Levels follow at 1.15× each, also by Fund, never ending |
| **Fed by pipe** | It is a buyer in the Yard, after usual partners. It pays the one price (1.3) for residents' land goods, named at both ends. Houses bring at most a quarter |
| **Gives** | First to the station's wants, then the Era wonder's pipe half, then the hub. Its sales go to its own levels and next machine. No dividend, so no new gold beyond what any factory adds, and no market norms between friends |
| **The order** | Brickworks, then Cement Kiln, then Steel Mill: the three goods the Dynamo and the Cathedral waited on |
| **The looks** | It rises as scaffold and crane in the Yard corner, reusing the buildings' art. It is drawn on the station's card and in "Today here" |

**This answers what our nine steps couldn't:**
- **The Wonder's pipe half fills by pipe**, because someone in the Capital now makes bricks, steel and concrete. The
  friends' land goods are its fuel.
- **Wants for bricks fill by themselves:** Dev's Big Factory, Ana's Kiln.
- **The crew has a shared build for weeks.** At one machine a week, Era 2 brings the next.
- **The station's gold has a destination its own size,** priced in the station's income.

It replaces the analyst's "Landmark tiers now" with something that also *makes* things. It also brings back my run-2
Works without its 80/20 margin, which the economy designer and the psychologist rightly rejected.

### 3. Gold finishes what no maker near can make, and says so

**Part 2 changes the question.** With a Works, every station can build the maker. So the order rule only covers what
nobody near can make *yet*:

| Case | Rule |
| --- | --- |
| **A personal want with no maker in the station** (no resident's building and no Works machine makes the good) | **"Order the rest" at once,** at 2.5 × base, shown on the ghost, the placement toast, the Next step and the card: *"Big Factory wants 100 bricks · nobody near makes them · Order them now · 1,250 g ▸"*. No friend loses a role, because no friend near had one. Ben and Mia never wait |
| **A want with a maker in the station** | The team's rule from run 3: the lit friend's window, then a 6-hour flow at 2.0 |
| **The Era wonder** | The order half stays half, and its button never vanishes (*"The order half is in · the rest comes by pipe: the Works' Brickworks makes bricks · Fund ▸"*) |

So in a shared goal, gold builds the maker and friends' goods fill the pipe half.

### 4. One gold door everyone sees: the top bar

The top bar's gold figure is the number players watch. They quoted it in every diary: *"29M idle"*, *"13.4M gold"*.
- **Tap it** for "What your gold can do": the three uses with the most effect, each one tap, in hours of income and what
  they change.
- **For Cleo on day 21:** *"Lay copper on 14 full links (+1,900 g/min)"*, *"Fund the Works' Brickworks (the Dynamo's
  bricks)"* and *"Max your Coal Pit now that it can sell (+1,100 g/min)"*.
- **The badge:** a small dot when the bank passes one day of income and one of those three exists.

**The other places:**
- **The Next steps** use the same three, sized to the bank (the effort term, F3).
- **The card** carries one money line, the best of them.

## The five questions

**1. Can gold finish what no friend near makes?**
- **A personal want with no maker possible near you:** yes, at once, at 2.5 × base. Nobody near had a role to lose.
- **Where a maker exists:** after the lit friend's window, over 6 hours at 2.0.
- **A shared goal (the Wonder):** gold buys at most half. For the other half, gold builds the **maker**, the station's
  Works, whose fuel is the friends' land goods.
- **What's left for friends:** their stone, coal and copper run the Works and the copper pipes. A friend's goods are
  what turn gold into bricks.

**2. How every late cost is sized:**

| Cost | Sized in |
| --- | --- |
| Personal (Widen, an order, the gold door's lines) | Hours of **the buyer's** 7-day measured income |
| The station's (Works machines) | Days of **the station's** summed 7-day income, doubling per machine |
| The Capital's (wonder stages) | 6 hours of **the Capital's** summed 7-day income, measured over residents seen in the last 7 days, not 3 |

**The bug's fix:** a stage is resized when someone plays, never below its last size and never from the 60 g/min floor
while anyone in the district has played in the last 7 days.

**3. Where the doors are:**
- **The upgrade panel at the pipe:** the next pipe or Widen, and then "Max now pays".
- **The top bar:** the gold panel with three uses and a badge.
- **The Next steps:** the same three, sized to the bank.
- **The cards:** one money line, plus the Works' Fund line on the project card. Nothing says only "+0".

**4. The shared goal after the Landmark:** the station's Works.
- About three machines over Eras 1–2 (Brickworks, Cement Kiln, Steel Mill), roughly one a week.
- Then levels, which never run out. Each Era adds its machine (glass, wire).
- The Landmark's cosmetic tiers can come later.

**5. The bots:**
- They spend because they buy every positive step without judging it. People judge, and stop at "+0".
- Both stall at the grey pipe (Ana's bot holds 18.9 days).
- The mark for run 5 should therefore be the *bots'* bank as well as people's. Doors the advisor can't use, people won't
  find.

## Cost and risks

| Part | Days |
| --- | --- |
| The pipe on the upgrade panel, the network button, Widen for grey too | 2 |
| The top-bar gold panel and badge; Next steps and the card's money line | 2 |
| The sizing fix (7-day window, never below the last size, resize on play) | 1 |
| Order at once where no maker exists, told everywhere | 1 |
| The station's Works: machines owned by the station, Fund, a buyer in the Yard, output to wants and the wonder, levels, the scaffold art (reusing building sprites, `process.mjs` and `fit.mjs`) | 5–7 |
| **Total** | **About 11–13 days** |

**Risks:**

| Risk | Answer |
| --- | --- |
| **Free growth stays bigger than paid growth** | The doors must beat the free sources. Copper on Cleo's links is +1,900 g/min against a week of hub-order bonuses at about +300k (≈ 30 g/min). The light bots must show it first |
| **The Works out-competes a friend's own Brickworks** | It buys at the same one price, after usual partners, so a friend's running supply is never cut |
| **One rich player funds the Works alone** (Cleo held 115.7M) | Allowed. It is the station's, named; its fuel still comes from friends |
| **Grey Widen undercuts the friend's copper** | A Copper Pipe is still cheaper and carries more. Widen is the fallback and the long sink |
| **Readability** | One new noun, "Works", shown as the machine's own name: *"the station's Brickworks"* |
| **Pacing** | Copper and Max raise income, and XP follows earnings. Re-run `typical` for Level 30 on day 25 ± 2 |

## Weeks 2–5, walked through

The gains below are estimates from the level and pipe rules; the light bots set the real ones.

| Player | With this design |
| --- | --- |
| **Cleo** (optimiser; 4.2M on day 6, 115.7M on day 36) | **Day 6:** her Iron Mine's panel says "+0 · pipe 205" with *Copper Pipe ▸*, and her own ingots are in hand. Copper on 14 links: income 2,356 → about 4,200. Then *Max now pays*: 1–3M a session on levels. **Day 14:** the gold panel offers *Fund the Works' Brickworks*; she pays 3M of 5.5M, and Ana the rest. *"I wish bigger sinks existed"* now has an answer every session |
| **Ana** (organiser; 657 → 1,154 g/min) | **Day 17:** after the Landmark, the project card's next line is the Works; she funds 2.5M. **Day 21:** the Brickworks runs on her stone and Cleo's coal, and its bricks fill the Dynamo's pipe half before its deadline: *"Era 1: the Great Dynamo stood · your stone, Cleo's coal, the Works' bricks"*. **Day 27:** the Cathedral's steel and concrete wait on the Works' next machines (11M and 22M), and she starts the Cement Kiln with 4M |
| **Dev** (late; *"12M gold sitting around, tell me how to be useful"*) | **Day 21:** the top bar's gold panel answers him: *Fund the Cement Kiln* (the Cathedral's concrete) and *lay copper on 5 links* (+300 g/min). His Big Factory's bricks come from the Works in minutes, named. He is useful in the act he asked for |
| **Ben** (phone; back on day 36 with 13.4M) | One tap on the gold figure: *"Fund the Works' Steel Mill · it makes the Cathedral's steel · Fund 5M ▸"*. Then *"Copper on your Moily's pipe: +120 g/min"*. *"I still don't know if upgrading earns anything"* becomes a panel that says what to lay first |
| **Mia, alone beside Jo** (Capital NW; 40.8M) | **Day 8:** her pipes and Widen give her levels a use (*"Max now pays"*). **Day 15:** her small station's Works is hers and Jo's: she funds a Brickworks with 1 day of their summed income, fed by her stone and the Houses' coal. Its bricks reach the Dynamo's pipe half: *"Mia's Works made 4,000 of the Dynamo's bricks"* on every card in the Capital, the crew's included. The player alone becomes the Capital's brick maker, by name |

## One mark for run 5 that would prove me wrong

> **From day 14 to day 36, every crew member with 4+ sessions spends at least 20% of what they earn, and at least half
> of that goes into pipes, levels and the Works.**

If they still hoard, or if the spending goes mostly to orders, gold has no fuel's job, and this design is wrong.

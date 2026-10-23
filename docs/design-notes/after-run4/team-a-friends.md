# Round A, after run 4: the friends-first seat

**My position in one line:** in run 4 the players wanted to spend their gold together, on each other and on what they
were building as a group, and the game let one of them do it once. Give every friend a gold door into every shared
build, sized in hours of their own income and never hidden. Let gold finish what nobody nearby makes. Give the station
a next goal after its Landmark, and put the pipe door on the upgrade panel. That brings private upgrades back too.

## 1. Diagnosis: why the nine steps didn't move the gold

The nine steps were the right idea. They failed in four ways; the fourth is the one my seat sees.

**Too small.** Both Era stages were sized at the floor, 6 h × 60 g/min ≈ 21.6k g. The Dynamo was sized at world start
with only Jo seated, and Jo's first pipe delivery froze that size: a stage is resized only while nobody has delivered
to it (`GameWorld.EraWant.cs:228–235`). The Cathedral counted only residents seen in the last 3 days
(`GameWorld.Market.cs:202–204`). Everyone had been away for 3 days, so the floor applied (`GameWorld.Wonders.cs:139–142`).

**Too capped.** "Half of each good at most" is one cap for the whole Capital, not a cap per person
(`GameWorld.EraWant.cs:15, 197`). The first friend to order uses it up, and the button disappears for everyone else.
Cleo used the Dynamo's half on day 6. Ana used the Cathedral's half on day 27, then asked the crew: *"Can you each
order the other half"* (group, day 27 20:03). Ben came back for exactly that: *"what do i order for the cathedral,
just tell me the button"* (group, day 36 20:03). He found only bars (ben d36 #89–#93). Ana: *"only my name on the
list"* (ana d36 #354).

**Too hidden.** A building waiting for materials read "is being built", because its waiting mark is a far-future end
date (`GameWorld.Wants.cs:20, 36`). Order the rest was folded away on a card. At
the pipe the upgrade panel says *"+0 g/min"* and *"+30 would make nothing more"* (cleo d06 #220), then points nowhere.
The next pipe tier is offered only in a full building's own panel. Ben: *"does it even earn? dont trust"* (ben d36 #82).

**Deeper: gold's social use had no door, and the shared goal ended.** After day 10, almost every attempt to use gold
was for someone else:

- Cleo had *"15M idle"* for the Foundry Hall (group, day 10 20:04) and posted a 450-brick buy order no player could
  fill (group, day 14 19:10).
- Dev: *"600k gold spare if anyone needs it"*, then *"~3.6M gold to throw at whatever helps"*, then *"12M gold sitting
  around, tell me how to be useful"* (group, days 10, 14; dev d21 #146).
- Ana ordered for the Cathedral: *"nice that I can chip in without building"* (ana d27 #316).
- Cleo: *"Dev offers 12M gold but nobody knows how to turn it into bricks"* (cleo d21, Together).

Dev opened the Build menu's Processing tab and gave up: *"Bricks need stone + coal, I have neither"* (dev d14
#116–#117). Nobody told him the Yard would bring both. The Dynamo stood at 89% from day 6 to day 22 while the Capital
held tens of millions.

On day 17 the Landmark stood and the station had nothing left to build together (This week: *"A new parcel: Level 7 ·
10.0k g"*, ana d36 #375). Days 18–36: fun 2.83, 2 of 6 would come back, and Ana: *"I feel I'm
the only one pushing"* (ana d27). My run-3 trigger for Landmark tiers fired on day 17, eleven days early.

Private growth didn't need gold. Crew income rose 37% from day 14 to day 36 almost for free (findings, `sinceRun3`),
and at the pipe the preview honestly showed that upgrades add nothing. Prices against incomes matter (question 2), but
they aren't the first cause. **Gold had two uses that mattered, friends and the pipe, and both doors were locked or
hidden.**

## 2. Proposal: "Chip in"

Seven steps, each playable on its own.

1. **Fix the sizing.** Count residents seen in the last 7 days, not 3. Keep resizing an Era stage upward each day
   until players have delivered a tenth of it. Never shrink it because people were away. The floor is 60 g/min for
   each seat taken, not for one seat.
2. **Chip in, instead of the single half-cap.** Every shared build shows every player who can reach it: *Chip in 1 h ·
   3 h · 1 day of your income*. You can press it again. Goods come from the Houses at 2.0× base and ship over 6 h; for
   the Era wonder they count 25% toward your contribution, as now. The board names givers in hours, not gold (*"Ben ·
   1 h"* next to *"Cleo · 1 day"*). The button never vanishes; when it can't order, it says why.
3. **Gold finishes what nobody nearby makes.** If no running building in the district makes the good, gold can buy
   all of what's missing, at once. If someone in the district does make it, the friends' window comes first (in
   sessions, ≤ 72 h, as decided), then gold buys the rest. Beside every such good, one line names the chain: *"A
   Brickworks on your estate makes bricks from Ana's stone and Cleo's coal; the Yard brings both · 1,400 g · Show
   me"*. It is offered first to the friend with gold and no maker role.
4. **Waiting buildings say so.** *"Waiting for 100 bricks · nobody in the Capital makes bricks · Order them · 2,400 g ·
   or Show me a Brickworks"*. Friends see it on their cards as *"Order them for Dev"*. The gold buys goods, not a gift
   of gold, so it stays fair and safe. The Exchange says the same when nobody can fill an order.
5. **The pipe door on the upgrade panel.** At the pipe, the two "+0" lines become one: *"Its pipes are full: 205 iron
   ore a minute. With Copper Pipe (14 tiles · 140 g + 14 copper ingots, you have them) levels 22–31 pay +N g/min · Lay
   copper"*. The preview is worked out through the better pipe. With no copper: *"Copper ingots: Cleo makes them ·
   Ask"*.
6. **Landmark tiers** (question 4).
7. **The doors where people look** (question 3). Also name the stranger's gifts at both ends, and open a lone player's
   chat on the Capital channel when nobody else in their station has written.

## 3. The five questions

**1. Can gold finish what no friend nearby makes?** Yes, at once, at 2.0× base, when no running building in the
district makes that good. The Dynamo's last 450 bricks waited 16 days with 55M in Cleo's bank, and the wait taught
nobody anything. When a maker is nearby, the friends' window runs first. Three things are left for friends:

- deliver by pipe, which counts four times as much (100% against 25%) and carries the keystone;
- build the chain the panel names: a Steel Mill takes 300 bricks and a Cement Kiln 200 (`buildings.yaml:320–347`),
  so bricks stay wanted after gold buys the first ones;
- chip in together, shown in hours.

**2. How every late cost is sized.**

| Cost | Size |
| --- | --- |
| Era stage | 6 h of the Capital's summed income (≈ 1.2–1.6M in run 4), from residents seen in the last 7 days |
| Chip-in | 1 h, 3 h or 1 day of **the buyer's** measured income, as Widen uses (`Widen.cs:21`) |
| Order for a waiting building | the missing goods at 2.0× base |
| Landmark tier, pipes part | 9 h of the station's spare of its own land goods, as stages are (`Projects.cs:125–130`), plus a two-friend keystone that starts when the tier opens |
| Landmark tier, purse | 24 h of the summed income of residents seen in the last 7 days, × 1.2 for each tier after the first |
| Widen | unchanged: 1 h, then 2 more hours |
| Parcels | 2 h of the buyer's income, or the list price if that is more |

**3. Where the doors are**, when the bank holds more than a day of income and the building is at its pipe:

- *Upgrade panel:* the pipe door (step 5), never "+0" on its own.
- *Top bar:* the gold amount becomes a button, *"40.6M ▸"*, which opens *What your gold can do*. Shared things come
  first: the tier purse, the Era goods nobody makes, a friend's waiting building. Then private ones: the pipe door,
  Widen, a parcel. Each has its button and its price in hours.
- *Next steps:* the same list in the same order. Shared doors get a minimum rank when the bank is over a day, the way
  research already does (`Advisor.cs:136–150`).
- *Welcome-back card:* one line under the totals with the first shared door and its button. That is Ben's *"just tell
  me the button."*

**4. The shared goal after the Landmark.** The Landmark grows. A tier opens the Monday after the last one stands, and
it never ends.

- Each tier has two parts. *From your pipes*: every seat's land good, so every friend has a part, plus a two-friend
  keystone. *From the Houses*: the purse, filled by chip-ins.
- Anyone in the district can chip in. A neighbour is named *"from next door"*.
- When a tier stands, the Landmark gets a new storey you can see, the station's pipes carry +25% (named, like the
  Refinements), and every name goes on its plaque.
- No deadline, no penalty: a tier waits. Its pipes part fills from absent friends' spare goods, as stages do now, so
  the friend who is pushing sees the others' goods arrive.

**5. The bots' mark.** The light bots press the advisor's top step every 5 minutes and read nothing
(`LightBotSimulation.cs:8–12`). The advisor always finds some upgrade that pays back within its 240-minute limit
(`Advisor.cs:125`), so the bots bought the levels people had read as useless. Ana's bot went from level 22 at 4,378
g/min on day 8 to level 34 at 6,162 on day 28: twelve levels for +41%, with each level costing 1.15× the last
(`Definitions.cs:301–302`). Still, four of six bots held 5–19 days (light-bots-day28.md). Three lessons:

- People do go through the advisor's doors (Ben's Max by Show me, ben d02 #46–#48 and d36 #79–#80; Mia: *"Next steps
  always telling me what is best"*, mia d15).
- People stop when the panel honestly says +0, and they should.
- Private upgrades can't hold a bank down, even for a bot.

So put the new doors in the advisor's list, and test the bots on them: ≤ 5 days held on day 28.

## 4. Cost and risks

**Cost** for one AI developer, each step playable, about 10 days in all:

| Step | Days |
| --- | --- |
| 1: sizing fix, the button that says why | ½ |
| 2–4: chip-in, gold finishing, waiting text, chain line, honest Exchange | 2½ |
| 5: pipe door on the upgrade panel | 1 |
| 7: top bar list, card line, advisor rank, naming the stranger, Mia's chat | 1½ |
| 6: Landmark tiers (engine, panel, plaque) | 3 |
| 6: storey art, fitted by the CLAUDE.md art steps | 1–2 |

**Risks:**

1. *Gold takes the maker's role away.* The friends' window when a maker is nearby, the chain line, pipes counting four
   times as much, and later builds that still need bricks all leave the maker a part.
2. *The rich friend does everything* (Cleo held 115M). Givers are named in hours, so Ben's hour stands next to Cleo's
   day.
3. *The tier feels like a tax.* It is optional, has no deadline, and pays a visible storey and a pipe perk. If diaries
   call it a tax, fold the purse into the goods.
4. *A treadmill:* the purse is always "a day". Run 5 has to show it reads as a goal.

## 5. Weeks 2–5, on run 4's own session days

**Ana** (days 8, 14, 21, 27, 36).

- *Day 8:* her card says *"The Great Dynamo needs bricks nobody in the Capital makes · chip in 1 h · 30k g"*. She does.
- *Day 21:* tier 1 is open. Pipes: stone, wood, coal, copper. Purse 5.7M. Cleo and Dev have chipped in, so she adds
  1 day (1.6M). It stands with four names on it, one of them Mia-B *from next door*.
- *Day 27:* the Cathedral, sized right, needs about 1.6M of steel and concrete, and nobody nearby makes steel. She
  chips in 1 day. Her Kiln's panel says *"waiting for 200 bricks · Dev's Brickworks makes them · on their way"*.
- *Day 36:* tier 2 shows Cleo's coal and Dev's wood arriving while they were away, so she isn't the only one
  pushing. She chips in 1 day (1.7M).

**Cleo** (days 10, 14, 21).

- *Day 10:* she chips 1 h (138k) into the Dynamo. Her full Iron Mine's panel offers copper. She has the research and
  the ingots, lays it, and levels 22–31 pay again: about 0.2M for the mine and about 3.4M for nine Small Factories.
- *Day 21:* she chips 1 day (3.7M) into tier 1 and widens ×2 (1 h, 159k).
- *Weeks 4–5:* she doesn't play. Her name is on the plaque, and her coal keeps filling tier 2.

**Dev** (days 8, 10, 14, 21).

- *Day 14:* he opens Processing (#116), and his Next steps lead with the Brickworks line. He builds it for 1,400 g.
  The Yard brings Ana's stone and Cleo's coal, and the Dynamo's last bricks are his, inside the friends' window.
  *"Not very useful to the crew"* (director row 9) becomes the Capital's brickmaker.
- *Day 21:* to *"tell me how to be useful"* the top bar list answers: tier 1, 1 day (1.6M).

**Ben** (away from day 3 to day 36). On day 36 his card says *"+13.0M · The Landmark grew a storey while you were
away (Ana, Cleo, Dev, Mia next door) · tier 2: your sand goes there by itself · chip in 1 h · 23.7k g"*, then *"The
Copper Cathedral: steel nobody nearby makes · chip in 1 h · 23.7k g"*. That is two buttons, and both put his name up.

**Mia, alone beside Jo** (days 1, 8, 15).

- *Day 1:* her chat opens on the Capital channel, because Jo never writes, so her greetings reach the other Mia and
  the crew (director row 1; *"unanswered for two weeks"*, mia d15 #127).
- *Day 8:* she chips 1 h into the Dynamo, and her name is on the Capital's wonder.
- *From day 28,* when Capital NW's Landmark stands, her tier's purse is 24 h of Mia's and Jo's income (≈ 1.8M). Mia-B
  can chip in from next door. If Mia stops, the tier waits.

**Totals, honestly:** about 9M of shared chip-ins (tier 1 5.7M, the Cathedral 1.6M, tier 2 1.7M, the Dynamo 0.2M) and
about 4M of reopened upgrades, against about 139M the crew banked from day 17 to day 36. That is about 10%, not 20%,
because Cleo and Dev stop playing after day 21. The rest has to come from steel pipes in Era 2 (a Small Factory from
level 29 to 53 costs about 13.9M) once Dev's bricks build a Steel Mill. If 20% is a hard line, the purse could be
2 days, but I'd rather miss 20% than tax the friends.

## 6. The mark that would prove me wrong

**From day 17 to day 36 of run 5, at least three of the four crew chip gold into a shared build (a Landmark tier, the
Era wonder or a friend's waiting building), and together those chip-ins reach at least 5% of what the crew earns in
those days.** If fewer than three chip in, or the total stays under 5%, my reading is wrong: the gold doesn't want to go
to friends, and the team should back the private sinks.

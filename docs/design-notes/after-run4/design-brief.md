# Brief: the design team, after run 4

You are back again. You designed v1.2 "Playing together" (`docs/multiplayer-design.html`), the redesign after the
first playtest (`docs/playtest-redesign.html`), and after run 3 your answer to the owner's question *"at some point
there's nothing to do with your gold, so no reason to grow"*: the nine steps in `docs/playtest-run3-design.html` (your
record: `docs/design-notes/after-run3/team-*.md`, `team-shared.md`). Since then:

1. **All nine steps were built**, with the analysts' remaining rows (the "After run 3" entry in
   `docs/ImplementationStatus.md`): a preview before every buy with Undo, wants (a building short of materials is a
   scaffold its station fills), the pipe tier as a want, Order the rest, the Era wonder that buys the Capital's spare by
   pipe with orders for half, Refinements and resting Labs, hub orders that fill from sales, a keystone two friends
   make, Widen; plus a falling gift told at both ends, a card that folds, Crew and Station chat, friends elsewhere.
2. **Run 4** (`playtest/runs/story4`) played run 3's story again on that build, row for row, on a fresh world.
   Its report's headline: *"Every stage stood, the crew lived together, and the gold still had nowhere to go."*

**The owner read it and gave it to you again.** The question is the same: what does a player want to do with their
gold in weeks 2–5, so there is a reason to grow? Your nine steps didn't answer it in play. Find out why, and what does.

## What run 4 showed

**The gold.** The six players earned 281.8M and spent 0.58M: 0.09–0.25% each (your mark: ≥ 20%). On day 36 each held
23.6–29.1 days of income, and the bank grew by about a day's income every day (`marks.json` `goldSpent`; the report's
"Gold held" chart). Three quarters of what was spent went to upgrades, most of it in the first week (Cleo 227k of her
297k by day 7.9). Your sinks took **30,660 g** in all:

| Step | In play |
| --- | --- |
| 1 Preview, "what stops you", Undo | Trusted (Cleo checked ten previews, exact); no sink; Undo never used. At the pipe the panel says *"Level 21: +0 g/min: its pipes carry 205 iron ore a minute"* and *"+30 would make nothing more"* (cleo d06 #220); Ben: *"I still don't know if upgrading earns anything"* (d36). |
| 2 Wants (scaffolds) | Two in 36 days, both for bricks nobody in the Capital made (Dev's Big Factory day 21, Ana's Cement Kiln day 36); 0 materials arrived; both owners read "is being built" and never saw a want. |
| 3 Pipe tier as a want | Never seen. The next tier is offered only inside a full building's own panel; Cleo researched Copper Pipes on day 6 20:44 and never opened a full building again; the upgrade panel at the pipe points nowhere. All 1,087 pipe tiles were grey at the end, though Cleo made 237k copper ingots. |
| 4 Order the rest | Offered once (1,000 g, folded on Ana's card for Dev's factory), not pressed. |
| 5 Era wonder | In every tracker; its end told on the cards. **Both stages stopped at the pipe half**: the Great Dynamo at 89% (bricks: Cleo ordered the half, 9,000 g, on day 6; the Houses brought their quarter; nobody made the rest), the Copper Cathedral at 50% (Ana ordered the half, 21,660 g; nobody makes steel or concrete). The Order button then vanishes without a word (Ana and Ben STUCK, d36). **And both stages were sized at the floor, 6 h × 60 g/min ≈ 21.6k g of goods (a bug):** the Dynamo at world start with only Jo seated, frozen by his first delivery; the Cathedral by the daily resize, which counts only residents seen in the last 3 days, after everyone had been away 3 days (sized right: about 1.6M of goods, its order half about 1.6M gold). |
| 6 Refinements, resting Labs | Worked: 15 Refinements, named and felt (*"Pipes carry +10% · Cleo Analytics's research 💛"*); 184.5k points banked (run 3: 1.6M). Paid in points, not gold. |
| 7 Hub orders as buyers | Worked as a faucet: 13–66 orders per player filled by themselves, no Deliver chore; one week of Cleo's order bonuses exceeded all she spent in 36 days. |
| 8 Two-friend keystone | Fired once (day 5, Ana's stone and Mia-B's coal for a Brickworks), passed over in 12 hours while both were away; nobody saw it. |
| 9 Widen | Never offered: no Copper or Steel pipe anywhere. |

**What players tried to buy and couldn't** (the analyst's `gold` section): the Dynamo's last 450 bricks (an Exchange
order that never filled); the Cathedral's other half (no button); wood for the Water Tower by a 5,000 g parcel that
held no wood; and help for friends: five offers of gold in the group chat (Cleo 4.2M, 15M; Dev 600k, 3.6M,
*"12M gold sitting around, tell me how to be useful :)"*) with no door. Diaries after day 10: *"29M idle with nothing
worth buying"* (cleo d14), *"income without a sink is pointless"* (cleo d21), *"13.4M gold and nothing I understand to
spend it on"* (ben d36).

**The light bots** (the advisor's steps, at most 15 buildings; `playtest/runs/story4/light-bots-day28.md`) hold 2.7–18.9
days of income on day 28 (your mark: ≤ 3). They spend far more than the players did, on upgrades the advisor offers
(Cleo's bot: Iron Mine level 69, Small Factories level 50). Run 3's bot held 107M. So the doors exist in the advisor;
people didn't go through them.

**What went well, to keep:** every project stage stood (run 3 froze at 89–96%); the crew lived together (Ben and Dev
by Ana's links); the stranger next door (Mia-B) finished the crew's Water Tower with her wood (but nobody knew it was
her); previews and books held within 1%; Ana's copper ran Cleo's Smelter all month, thanked at both ends; no chores.
After the Landmark on day 17 the crew had nothing shared to build (days 18–36: fun 2.83, "come back" yes 2 of 6).

## What the analyst recommends (`playtest/runs/story4/findings.json`, `recommendations`)

1. When nobody near makes a wanted good, let gold finish it, and say so where the player looks.
2. Size every late sink to income (stages over 7 days of income, not the 3-day window; Order the rest, Widen, parcels
   in hours of the buyer's income); build the station's Landmark tiers now.
3. Point to the chain that would make the missing good ("a Brickworks on your estate makes bricks from Ana's stone and
   Cleo's coal; the Yard brings both · Show me").
4. Make the stranger next door a person to the crew. 5. Open a lone player's chat where people are.
6–11: explain the Wonder's two halves on its tab; start a keystone when its stage opens; words on the upgrade panel;
an honest Exchange; small breakers; harness fixes before run 5.

## What we need from you

**One solution the six of you stand behind, so that in run 5 gold has a use in weeks 2–5 and players spend a real
share of it, by choice.** First an honest diagnosis: were the nine steps the right idea, too small, too capped, too
hidden, or is the cause deeper (incomes against prices, what an idle game's spending loop needs, the pipe wall, what a
player can see from where they look)? Then the solution: judge the analyst's recommendations (adopt, change, replace),
and settle these:

1. **Can gold finish what no friend near makes** (the Wonder's pipe half, a want with no maker)? At once, after a
   window, at what price; and what's left for friends to do then.
2. **How every late cost is sized**: in hours of whose income (the buyer's, the station's, the Capital's), measured
   how, on what curve; and the sizing bug's fix.
3. **Where the doors are**: the player looks for a place to spend on the upgrade panel, the top bar, the Next steps and
   the cards. What does each say when gold is plentiful and the building is at its pipe?
4. **The shared goal after the Landmark**: what the station builds next, and for how long.
5. **The bots' mark**: why the advisor's bot spends and people don't, and what that says.

Constraints as before: buildable in steps by one AI developer, every step playable, the owner's principles
(`docs/design-notes/playing-together/brief.md`: friends together, the friend who comes a week later, the lonely player,
players who stop, clear, pipes are the heart, asynchronous idle play, fair and safe), a fun idle game that looks like
one, expansion over rebuilding (no forced teardowns), and not mainly chores. Be concrete: what the player sees and does,
the numbers, what it replaces, what it costs to build, and how run 5 would show it worked. Walk Ana, Cleo, Dev, Ben and
Mia (alone beside Jo) through weeks 2–5 with it.

## What to read

- `playtest/runs/story4/findings.json`: the analyst (start with `summary`, `verdict`, `gold`, `sinceRun3`, `marks`,
  `recommendations`); `playtest/runs/story4/marks.json` and `metrics.jsonl` (`spent`, `earned`, `gold`, the want and
  wonder fields per sample); `playtest/runs/story4/director-observations.md`; `light-bots-day28.md`.
- `playtest/runs/story4/digest.md` for the players' own words (long: the diaries from day 8 on, and the group chat).
- Your own record: `docs/playtest-run3-design.html`, `docs/design-notes/after-run3/team-shared.md` and your `team-*.md`.
- The code where you need it: `GameWorld.EraWant.cs` (the wonder's buying and orders), `GameWorld.Wonders.cs`
  (`StageRequirements`), `GameWorld.Wants.cs`, `GameWorld.Orders.cs`, `GameWorld.Widen.cs`, `GameWorld.Refinements.cs`,
  `GameWorld.IdleTexts.cs` (what a full pipe offers), `GameWorld.Advisor.cs`; content in `TransportPipes.Game/Content/*.yaml`.

## The rounds

- **Round A, alone (now):** `docs/design-notes/after-run4/team-a-<you>.md` (about 1,500–2,500 words): your diagnosis,
  your proposal, the five questions answered, its cost and risks, the walk-through, and one mark for run 5 that would
  prove it wrong.
- **Round B, everyone reads everyone:** attack, merge, and agree on one solution.
- **Round C, sign:** one shared text, signed by all six or with each objection named.

**Rules:** evidence or it doesn't count (sessions and action numbers, samples, code by file and line). Read anything;
change nothing except your own file in this folder; don't build or run the game. Plain, clear English. Use your seat as
your name: `wild`, `friends`, `lonely`, `economy`, `psychologist` or `outsider`.

## Round B: everyone reads everyone (`team-b-<you>.md`, about 1,000–1,800 words)

Read all six `team-a-*.md`. Then write:
1. **What I take from the others**, and what I drop of my own.
2. **What I reject, and why** (evidence).
3. **The open questions, final** (the spread below).
4. **The solution I'd sign:** the mechanisms, each with its numbers, in the order to build them, what each replaces,
   and the days it costs.
5. **One mark for run 5** that would prove it wrong.

Where all six agree in round A: fix the sizing (count the last 7 days, never size from the floor, resize upward only);
gold may finish at once what no friend near makes, friends first where someone makes it; the door at the pipe (the
upgrade panel's "+0" offers the next pipe tier or Widen, with a preview); the gold figure on the top bar becomes a door
or shows the bank in days; and the station gets something to build after the Landmark.

The round-A spread to settle:
- **After the Landmark:** wild's *Works* (gold-built machines in the Yard that make the missing goods: a Brickworks, a
  Cement Kiln, a Steel Mill at 1, 2, 4 days of the station's income, fed by friends' land goods); economy's *Landmark
  tiers* (6 h × 2^(n−1) of the station's income, each a storey and +10% on the residents' pipes); friends' *tiers with a
  pipes part and a purse* (9 h of the station's spare plus a two-friend keystone, and 24 h of income ×1.2 a tier, one a
  week); lonely's *tiers at 1, 2, 4, 8 days*; outsider's *floors ×1.5*; psychologist's *rings* (n days of the
  station's income plus 6 h of goods, nobody more than half until it has waited 3 days).
- **Gold in a shared goal:** friends' *chip-in* in 1 h, 3 h or 1 day of your own income, any number of times, givers
  named in hours; psychologist's *half per person*; economy: size the goals, keep level, order and parcel prices as they
  are.
- **The price and credit of bought goods:** 2.0× base (five of you) or 2.5× (wild); a quarter's credit (lonely) or
  full credit and a 💛 when bought for a friend (outsider, psychologist).
- **Widen:** on grey pipes too, up to ×4 below a Copper Pipe (wild, outsider), or Copper first, then Widen (economy).
- **One door or one plan:** lonely's "Grow everything that pays · 4.1M of 13.4M" in one tap with Undo, against one
  ranked buy at a time (outsider, wild's "three best uses").
- **The player alone:** lonely's branch next to a crew that plays, and the district channel first; in or out of this
  plan?
- **Before run 5:** economy's light-bot run with ingots orderable, outsider's check of Level 30's day once every full
  pipe widens: which runs, and what would stop the plan.
- **The mark for run 5:** spend ≥ 20% from day 14 to 36 (wild, psychologist), Ben spends 25% of his bank in one phone
  session (lonely), chip-ins ≥ 5% of the crew's earnings (friends), the pipe door pressed (outsider, economy).

After round B the economy designer drafts the shared text from the six round-B files (`team-shared.md`), and each of
you signs it or names your objection (round C).

## Status

Rounds A and B were held; the economy designer drafted `team-shared.md`. Round C (signing) was not held: the owner
asked to stop using subagents and for my own reasoning instead. My plan, which keeps most of the draft and changes it
around the pipe ladder, is `docs/playtest-run4-plan.html`.

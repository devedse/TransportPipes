# Brief: the design team, after run 3

You are back again. You designed v1.2 "Playing together" (`docs/multiplayer-design.html`), and after the first AI
playtest you judged it against the evidence and wrote the redesign (`docs/playtest-redesign.html`, the record in
`docs/design-notes/after-playtest/`: dealt land, one price, Houses, Send, the stranger next door). Since then:

1. **Your redesign was built** (steps 1–7) and **played as run 2** (`playtest/runs/story2`, the same story). Its
   analyst's twelve recommendations were built (`docs/design-notes/after-playtest/run2-fixes.md`): gifts named at both
   ends with Thanks, Send only where it moves something, the one-tap pipe, stages that stand only by delivery, a
   building on its own pipe, keep researching, and more.
2. **Run 3** (`playtest/runs/story3`) played the same story on that build. Its report's headline: *"The first gift both
   friends felt, then a month of frozen projects."* Days 1–4 were the best of three runs; weeks 3–5 were as flat as
   ever.
3. **Three analysts** (systems, player research, social) reviewed run 3 in two rounds and agreed on twelve improvements
   (`docs/playtest-run3-review.html`; their notes in this folder, `r1-*.md` and `r2-*.md`). The first is built: a
   keystone now moves on (two days without enough of the players' own deliveries, the clock starting once everyone
   active has played; after two, the stage stands on its other goods).

**The owner read the run-3 report and said:** *"I think the biggest find was that at some point there's nothing to do
with your gold, so no reason to grow."* The owner wants everything fixed before the next playtest, and asked for you,
the people who designed the game, to come up with the solution where things are unclear. This is the main one.

## The problem, as the evidence shows it

- **Gold piles up from week 2.** On day 36 the crew still held 95–100% of all it had ever earned (Ana 39.8M of 39.8M,
  Cleo 32.3M of 33.9M, Dev 18.9M of 19.0M; `metrics.jsonl`, `earned` against `gold`). From day 15 to day 36 the crew
  added three buildings between them. Players said so in every run: *"Gold is 2.3M and the best thing I could buy cost
  15k"* (mia d08), *"365k gold and no clue what to spend it on"* (dev d10), *"the gold feels empty"* (mia d15), and run
  1's *"6M banked … feels pointless"*. The advisor balance run (`docs/balance/balance-typical-advisor.md`), a bot with no
  story and no AI player, is flat from about day 4.
- **Research runs out by day 10.** Era 1's tree costs 8,500 points; Cleo's Lab banked 449k by day 10 and 1.6M by day 21,
  more than all five eras' trees together (1.42M), while eating about 400 g/min of ingots.
- **The late wants exist, but behind a wall.** From the Big Factory on, every bigger building needs materials (Big
  Factory 100 bricks, Market Hall 500, Steel Mill 300, Cement Kiln 200, Big Turbine 20 steel, Bank 1,000 bricks;
  `TransportPipes.Game/Content/buildings.yaml`), and they can only come from your own Storages (`Pay()`, GameWorld.cs). On dealt land those
  materials need a neighbour's land good, and nothing carries them into a build: *"I don't know how to hand you 100
  bricks directly"* (Cleo, group chat, day 21). The Era wonder (the Great Dynamo, 1,800 bricks and 900 ingots, sized
  once at world start) got nothing, though it was about 2.5 hours of Cleo's real bricks, which went to the hub behind a
  full Storage.
- **The door that existed went unused.** The game's Ask (`GameWorld.Asks.cs`: an amount of a good for 12–72 hours,
  neighbours' spare first at the one price, a House fills the rest at 2× at the deadline) was used 0 times in three runs:
  its only entrance is a form under the Plan board.
- **What players came back for** in run 3 was a friend counting on them and a moving shared goal, never gold after week
  1 (the player researcher's count of the diaries).

## What the analysts proposed (read `docs/playtest-run3-review.html`, rows 7 and 10)

- **Row 7, "every want is a buyer on the pipes" (the one redesign they agreed on):** a building, upgrade or pipe tier
  short of materials becomes a site that buys them through the existing Ask: your own producers, then friends' and
  neighbours' spare at the one price (named at both ends), then Houses; the Era wonder buys for the whole Capital in every
  Yard round; friends see the need on their card and can send what their Storage holds with one tap; a friend can pay a
  House for the rest.
- **Row 10, "gold and research points have a use":** Labs rest when nothing can be researched; capped repeatable
  Refinements after an Era's tree; stage rewards aimed at the bottleneck; hub orders and the Invest line priced in hours
  of income.

They left these open (the "Where they still differ" table):
- Can gold buy at once, and at what price? (systems: a House fills a friend's need at 2×; player: never at once, or a
  friend's goods never matter again; social: pay the rest to the hub, never between players)
- Do hub daily orders become buyers too, or stay as they are?
- A keystone that needs two friends (bricks from one seat's stone and another's coal): now, later, or not at all?

## What we need from you

**One solution the six of you stand behind, for the owner's question: what does a player want to do with their gold
(and research) in weeks 2–5, so there is a reason to grow?** Judge the analysts' rows 7 and 10: adopt them, change them,
or replace them with something better. Decide the three open questions. Don't limit yourselves to today's mechanics,
but everything must be buildable in steps by one AI developer, stay playable, and keep the owner's principles
(`docs/design-notes/playing-together/brief.md`): friends together, the friend who comes a week later, the lonely
player, players who stop, clear, pipes are the heart, asynchronous idle play, fair and safe, buildable. The owner also
wants: a fun idle game that looks like one; expansion over rebuilding (no forced teardowns or wipes); and an idle game
that isn't mainly chores.

Be concrete: what the player sees and does, the numbers (prices, sizes, curves), what it replaces, what it costs to
build, and how run 4 would show it worked. Walk Ana, Cleo, Dev, Ben and Mia (alone beside Jo) through weeks 2–5 with it.

## What to read

- `playtest/runs/story3/findings.json` (the run-3 analyst), `docs/playtest-run3-review.html` and this folder's `r2-*.md`
  (the three analysts' final views), `playtest/runs/story3/marks.json` and `metrics.jsonl` (the numbers).
- `playtest/runs/story3/digest.md` for the players' own words (long: read the diaries from day 8 on).
- `docs/balance/balance-typical-advisor.md` and `docs/balance/balance-typical-together.md` (the bots' long game).
- The content: `TransportPipes.Game/Content/*.yaml` (buildings, research, progression), and the code where you need it
  (`GameWorld.Asks.cs`, `GameWorld.Wonders.cs`, `GameWorld.Research.cs`, `GameWorld.cs` `Pay()`).
- Your own designs: `docs/multiplayer-design.html`, `docs/playtest-redesign.html`, `docs/GameDesign.md` (the whole
  game, including what was planned for after v1: the Bank, investing, Wonder bonds).

## The rounds

- **Round A, alone (now):** `docs/design-notes/after-run3/team-a-<you>.md` (about 1,500–2,500 words): your diagnosis
  (is the cause the wall, the missing sinks, or something deeper in the loop?), your proposal, the three open questions
  answered, its cost and risks, and the walk-through.
- **Round B, everyone reads everyone:** attack, merge, and agree on one solution.
- **Round C, sign:** one shared text, signed by all six or with each objection named.

**Rules:** evidence or it doesn't count (sessions and action numbers, samples, code by file and line). Read anything;
change nothing except your own file in this folder; don't build or run the game. Plain, clear English. Use your old
seat as your name: `wild`, `friends`, `lonely`, `economy`, `psychologist` or `outsider`.

## Added while you work: the pipe is part of the wall

`TransportPipes.Game/Content/pipes.yaml`: a grey pipe carries 100 a minute per producer → consumer connection; a Copper
Pipe (500 a minute) costs 1 copper ingot a tile from your own Storages; a Steel Pipe (5,000) needs Era 2 research and
steel. A building's upgrades go on raising its output (level 25: Cleo's Coal Pit could make 882 coal a minute), but past
about 100 a minute per connection gold buys nothing more: her pit sold 100 a minute, ran at 12–14% for eleven days, and
its card blamed energy (cleo d21 #393: "Makes Coal 126 / 882/min", "to Trading Post 100 coal/min"). Her Copper Pipe click
did nothing because she had no copper ingots (cleo d21 #399–#404). So upgrades, the obvious gold sink, stop paying at
the pipe, and the way past it is a material (copper, steel) that again comes only from your own Storage.

## Round B: everyone reads everyone (`team-b-<you>.md`, about 1,000–1,800 words)

Read all six `team-a-*.md`. Then write:
1. **What I take from the others**, and what I drop of my own.
2. **What I reject, and why** (evidence).
3. **The three open questions, final:** can gold buy a want's materials (at once, after a wait, at what price, or never);
   do hub orders become buyers; a two-friend keystone now, later, or only through the wants.
4. **The solution I'd sign:** the mechanisms, each with its numbers, in the order to build them, and what each replaces.
5. **One mark for run 4** that would prove it wrong.

The round-A spread to settle: gold and materials (wild: capacity yes, goods via a House at 2× or "Don't wait"; friends:
paced over 6 h; lonely: from the hub at 2.5× into a site at once, or Mia and Ben never get them; economy: the hub at
2.5× only after a 24–72 h window; psychologist: only at the deadline; outsider: never, and "Cancel, your gold back");
the two-friend keystone (wild, friends, economy, psychologist: now; lonely: later; outsider: only through the wants);
and the extras (wild: "Widen" pipe capacity for hours of income, Capital lots; friends: research upgrades for every
seat; lonely: Fund ▸ and a "While I'm away" budget; economy: commission up to half a wonder stage; psychologist: a true
preview and an hour's Undo; outsider: the "what stops you" line, refinements that raise limits).

After round B the economy designer drafts the shared text from the six round-B files (`team-shared.md`), and each of you
signs it or names your objection (Round C).

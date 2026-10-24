# Brief: what the first AI playtest found, and how to fix it

You are one of five people on a panel that decides how TransportPipes fixes what its first AI playtest found. Each of
you has one point of view (your role, below). Together you produce ONE fix plan in which every question is decided:
what to change, how exactly (rules, numbers, texts, screens), in what order, and how we'll know it worked. Don't hand
questions back to the owner. Where something is uncertain, decide, say why, and say what would change your mind.

## The game and what was built

TransportPipes is a browser MMO idle game (isometric, cosy hand-painted look) about building production chains and
connecting them with pipes: generators, mines and factories on your estate, piped to your trading post (the hub buys
everything) and, since v1.2, to your **Yard gate**, where the neighbours of your **station** (four seats around a shared
Yard) buy each other's spare at one player price. v1.2, "Playing together", was built in eight steps on
`feature/playing-together` (draft PR #4; the test server runs it):

1. Stations and the Yard (one gate per estate, the trade engine, one price between players, the personal Hub Order)
2. Crews and choosing where you live (seats kept for friends with a code each, guests moving in with two taps)
3. Links between stations (two-ended pipes through the corridors, with a tank and an hour's notice)
4. Houses ⚙ (NPCs in empty seats, labelled and capped at 25%) and stand-ins for sleepers
5. Absence and return (seats held, the way-back screens, tending, Tailwind, the lone-human card)
6. The station's project (Foundation → Water Tower → Workshop → Foundry Hall → Landmark, baskets, a keystone), the
   Plan board, Asks
7. The long game (trunks, the Port, Discoveries, leagues, Wonders)
8. The first evening, balance, docs

Read for the design and its reasons: `docs/multiplayer-design.html` (the v1.2 design; §17 are the hard cases the
playtest played), `docs/ImplementationStatus.md` (section "v1.2 · Playing together" and the refinements table at the
end), `docs/GameDesign.md` (the whole game). The design's own record is in `docs/design-notes/playing-together/`.

## The playtest

Five AI players (Claude agents, each playing a person from `playtest/players/`) played the real game in real browsers
on a playtest server whose clock moved between sessions: §17a (Ana starts a crew and invites Ben, Cleo and Dev; Ben
plays on his phone and stops on day 3, back on day 36; Cleo optimises; Dev joins on day 8) and §17b (Mia alone), 36
game days, 28 sessions. How it was set up: `docs/playtest-design.html` and `playtest/README.md`.

The outcome, in order of usefulness:

- `playtest/runs/story1/findings.json`: the analyst's findings (the promises kept or missed, friction, bugs with
  repro, looks, artifacts, together, quotes, recommendations). **Read all of it.**
- `playtest/runs/story1/digest.md`: every session (commands, the players' notes, page errors, their diaries), the
  friends' group chat, the events the game showed them and their numbers over time. Long: read what your role needs.
- `playtest/runs/story1/director-notes.md` (what the playtest itself got wrong, and what the director verified in the
  code) and `director-summary.md` (how it ran).
- `playtest/stories/17a.md`: the calendar and the design's promise at each moment.
- Screenshots: `playtest/runs/story1/<player>/shots/small/*.jpg` (the findings and diaries name them). Look at the ones
  your argument rests on.
- The report page `playtest/runs/story1/report/report.html` has all of it with graphs (large: prefer the files above).

The analyst's headline: *a lovely first evening, then five weeks side by side but never together.* 4 of 20 promises
kept. The way in works (codes, two-tap move-in, starter kits, Loady, the look). After that: the Yard took friends'
goods for a buyer that couldn't use them and paid nothing; buildings whose output had nowhere to go sat idle for weeks
while the panels blamed inputs (goods don't pass through buildings, and nothing says so); nobody found how to give a
friend anything (no Ask, link, tend or pin in 36 days); the station's project was unreachable (the Water Tower wanted
91.5k sand); plus a stale seat picker, a Max button that spent 1.55M gold on a 3% building, three dialogs before every
return, and more.

**The playtest's limits.** AI players are patient, read everything and tend to be kind; they aimed by tile numbers;
the clock ran at 5× during sessions and jumped between them. Don't fix AI quirks; do weigh a STUCK note heavily. The
listed artifacts are not game bugs.

## The code, where to look

- Trade between neighbours: `TransportPipes.Game/State/GameWorld.Yards.cs` (gate nodes, `Measure` of spare/lacks,
  `MatchYard`, `CoupleYards`, `SettleGateFlows`); links `GameWorld.Links.cs`; Asks and the Plan board
  `GameWorld.Asks.cs`; the project `GameWorld.Projects.cs`; Houses `GameWorld.Tenants.cs`; stand-ins
  `GameWorld.StandIns.cs`; absence and return `GameWorld.Return.cs`; crews and seats `GameWorld.Crews.cs`,
  `GameWorld.cs` (`CreateCompany`, `SeatFree`)
- The flow solver: `TransportPipes.Game/Simulation/FlowSolver.cs` (own buildings first, then other companies,
  storages, the trading post; goods never pass through a building), networks `GameWorld.Networks.cs`
- Loady `GameWorld.Tutorial.cs` and `Content/tutorial.yaml`; the next steps `GameWorld.Advisor.cs`; content
  `TransportPipes.Game/Content/*.yaml`
- What the screens say: `TransportPipes.Server/Services/GameViews.cs` (building issues, the Yard line, DTOs), the client
  `TransportPipes.Frontend/src/components/` (`BuildingPanel`, `NetworkPanel`, `NetworkSummary`, `YardPanel`,
  `ProjectTab`, `PlanBoard`, `LinksTab`, `StationPicker`, `ReportDialog`, `ReturnDialog`, `TopBar`, `ChatPanel`,
  `CrewPanel`, `GoalTracker`, `NextSteps`, `ResearchPanel`)
- The tests: `TransportPipes.Game.Tests/` (YardFacts, ProjectFacts, TenantFacts, TutorialFacts, …), balance runs in
  `docs/balance/`

## The owner's principles (score every fix against them)

1. **Fun with three friends who join**, and with a friend who comes a week later.
2. **The lonely player** gets a full game and a world that doesn't feel empty.
3. **Players who stop** leave no holes; coming back after a month works.
4. **Clear:** a player can always see who they play with, what goes where and why.
5. **Pipes are the heart:** playing together is expressed through pipes and logistics.
6. **Asynchronous idle play:** nobody needs anyone online; an idle partner never stalls you.
7. **Fair and safe:** no griefing, leeching or gold funnels.
8. **Looks are primary;** expansion over rebuilding; be self-critical.
9. **Buildable:** one developer (an AI) builds it in days, in steps, every step playable (PR #4 deploys to the test
   server on every push). No backwards compatibility is needed: a fresh world is fine.

Keep v1.2's decisions (one player price, Houses labelled and capped, no votes or kicks, …) unless the evidence says
one is wrong; then say so plainly and decide the replacement.

## How the panel works

- **Round 1, alone:** your diagnosis and proposals from your role (`r1-<role>.md`).
- **Round 2, everyone reads everyone:** agree, object with evidence, merge, fill gaps, rank (`r2-<role>.md`).
- **Round 3:** the moderator's draft plan; you accept, amend or object item by item, and vote (`r3-<role>.md`).
- The moderator writes the decision (`docs/playtest-fixes.html`); these notes are the record.

**Rules.** Evidence or it doesn't count: cite findings by title, sessions and action numbers (`cleo d06-1400 #177`),
code by file and line. Claim a root cause only when you've checked it in the code (or say it's a guess). Read anything
in the repository and the run; change nothing except your own notes in this folder; don't build or run the game or its
tests. Write in plain, clear English: short sentences, one idea each, concrete over abstract.

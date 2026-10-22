# Brief: what the third AI playtest found, and what to do next

You are one of three analysts the owner asked to look at run 3 of the AI playtest. Each of you has one point of view
(`roles.md`). The owner wants two things from you:

1. **A table of improvements**: what to change, how exactly (rules, numbers, texts, screens), why (the evidence), what
   it costs, and in what order.
2. **Your view on the way forward:** "focus on fixes" (keep the design, repair what run 3 found) or "another
   redesign" (change something structural). Say which, and why, and if it's a redesign, exactly what changes, what it
   replaces, what it costs and how run 4 would show it worked. Both are fair answers; the owner is curious which you
   pick. A mix is allowed only if you say which part is the redesign.

Don't hand questions back to the owner. Where something is uncertain, decide, say why, and say what would change your
mind.

## The game

TransportPipes is a browser MMO idle game (isometric, cosy hand-painted look) about building production chains and
connecting them with pipes: generators, mines and factories on your estate, piped to your trading post (the hub buys
everything) and to your **Yard gate**, where the neighbours of your **station** (four seats around a shared Yard) buy
each other's spare at one player price. v1.2 "Playing together" added stations, crews and seats, links, Houses (NPCs in
empty seats), absence and return, the station's project (Foundation → Water Tower → Workshop → Foundry Hall → Landmark,
baskets of goods with one **keystone**), and the long game (eras, Wonders). Read for the design:
`docs/multiplayer-design.html` (§17 are the hard cases the playtest plays), `docs/GameDesign.md`,
`docs/ImplementationStatus.md` (the sections from "v1.2 after the AI playtest" on).

The owner's standing priorities: the game must be a fun idle game that looks like one (looks matter as much as
mechanics); expansion over rebuilding (no forced teardowns or wipes for players); be critical of the game loop and
redesign where needed. Before run 3 the owner asked whether the game had become mostly chores ("an idle game shouldn't
be mainly chores"), chose to keep the chores for now, and wants to play the game themselves at some point.

## The three runs

The same story each time: 14 rows over 36 game days (`playtest/stories/17a.md`): Ana starts a crew and sends seat links
to Ben (phone, skims, stops on day 3, back on day 36), Cleo (optimises) and Dev (joins on day 8); Mia plays alone. AI
players (Claude agents with a persona card in `playtest/players/`) played the real game in real browsers, acting only
by clicks and taps; the clock moved between sessions. How it works: `docs/playtest-design.html`, `playtest/README.md`.

- **Run 1** (`playtest/runs/story1`): the first v1.2. Then a five-person fix panel (`docs/playtest-fixes.html`) and,
  when the owner expected more, the six original designers produced a redesign (`docs/playtest-redesign.html`: land
  goods dealt once per station so every chain needs a neighbour, one price, Houses, Send, the stranger next door).
- **Run 2** (`playtest/runs/story2`): the redesign, with Jo (a scripted solo human, numbers only) and Mia played twice
  (`mia` beside Jo, `mia2` = Mia-B). Its analyst's twelve recommendations were all built
  (`docs/design-notes/after-playtest/run2-fixes.md`).
- **Run 3** (`playtest/runs/story3`): the same story on that build. **This is the run you review.**

## What to read for run 3 (in this order)

- `playtest/runs/story3/findings.json`: the run-3 analyst's findings (summary, verdict, promises, friction, bugs with
  repro, looks, artifacts, together, quotes, recommendations, marks with runs 1–3 side by side, sinceRun2, chores).
  It is **one analyst's read**: check the claims you lean on against the logs, and disagree where the evidence says so.
- `playtest/runs/story3/digest.md`: every session (commands, notes, diary), the group chat, events and numbers. The
  players' own words are here.
- `playtest/runs/story3/director-summary.md`, `director-notes.md` (what the playtest itself got wrong: those are
  artifacts, not game findings), `director-observations.md` (the director's running list of issues; leads, not
  evidence), `marks.json` (numbers at days 8, 14, 21, 36).
- `playtest/runs/story3/metrics.jsonl`: samples every 6 game hours (each company's level, gold a minute, earnings,
  project stage, basket and "This week" line).
- The players' folders `playtest/runs/story3/<player>/` (`actions.jsonl` has what the screen said after every command;
  `shots/small/*.jpg` are the screenshots: open the ones you cite).
- For comparison: `playtest/runs/story2/findings.json` and `playtest/runs/story1/findings.json`.
- The report the owner read: `playtest/runs/story3/report/report.html` (14 MB; prefer findings.json and the digest).

The code is in this repository (`TransportPipes.Game` is the simulation, `TransportPipes.Server` the server,
`TransportPipes.Frontend` the React/PixiJS client). Read it to confirm a cause or size a change (for example the
project's keystone rule in `TransportPipes.Game/State/GameWorld.Projects.cs`, `UpdateBaskets`). **Don't change any
file** except your own notes file.

## Be honest

- Evidence or nothing: every claim names its sessions and action numbers ("ana d27-2000 #341") or a sample.
- AI players are kind and patient: weigh their complaints high and their praise lower; a STUCK note is a strong signal.
- Some trouble came from the playtest itself (the director's notes): keep it out of the game's findings.
- Three runs played one story with AI players; say what a real player might do differently where it matters.

## Round 1: write `docs/design-notes/after-run3/r1-<your role>.md`

1. **What run 3 says, through your lens** (at most 400 words): the two or three things that matter most and why.
2. **The table** (6–12 rows, ranked):

   | # | Improvement (what changes, concretely) | Fix or redesign | Answers (findings, evidence) | Effort | Priority |

   Effort in days of work, with the main risk in a few words. Priority: now (before anyone plays again), next, later.
3. **Fixes or another redesign?** Your position and reasoning (at most 400 words). If a redesign: what exactly, what it
   replaces, what it costs, and what in run 4 would show it worked.
4. **Run 4:** the three to five marks you'd watch.

Reply with a short summary when the file is written.

# Brief: the design team, after the playtest

You are back. A few days ago the six of you designed v1.2, "Playing together" (`docs/multiplayer-design.html`, the
record in `docs/design-notes/playing-together/`). Since then:

1. **It was built**, in the eight steps of your plan, on `feature/playing-together`. `docs/ImplementationStatus.md`
   ("v1.2 · Playing together", and the refinements table at its end) lists what was built and where the build differs
   from your design.
2. **It was playtested by AI players** over 36 game days. Ana starts a crew and invites Ben, Cleo and Dev; Ben plays on
   his phone and stops on day 3, back on day 36; Cleo optimises; Dev joins on day 8; Mia plays alone. That is your §17a
   and §17b, and the setup is in `docs/playtest-design.html`. The analyst's verdict: *"A lovely first evening, then five
   weeks side by side but never together."* 4 of 20 promises were kept.
3. **A second panel of five** (engineer, economy, clarity, cooperation, player researcher) read the outcome and
   decided a fix plan, `docs/playtest-fixes.html` (record in `docs/design-notes/playtest-fixes/`). It keeps your design
   and repairs it:
   - an honest Yard;
   - dead ends that explain themselves;
   - a reachable project;
   - one return card;
   - trade with a face;
   - chains fed by a neighbour.
   It leaves one question to the bots: the one price, 1.75 or 1.5 × base.

**The owner, on reading that plan:** *"I would've expected more of a redesign need for the fixes."* So the owner asked
for you, the people who designed it, to look again.

## What we need from you

An honest answer to one question: **does the playtest show that the design needs to change, and if so, how?** "The fix
plan is enough" is an allowed answer, but you must argue it against the evidence and the owner's expectation. "Throw
out the Yard" is allowed too, if the evidence carries it. The original brief's spirit holds: don't limit yourself to the
current mechanics. Moving estates, anything goes, as long as it can be built in steps by one AI developer and stays
playable.

The owner's principles (the old brief's checklist, `docs/design-notes/playing-together/brief.md`) still score every
idea. Above all:
- fun with three friends who join, and a friend who comes a week later;
- the lonely player;
- players who stop;
- clear;
- pipes are the heart;
- asynchronous idle play;
- fair and safe;
- buildable.

## What to read

- **The outcome:**
  - `playtest/runs/story1/findings.json`, all of it;
  - `playtest/runs/story1/digest.md`, every session, the friends' group chat, the events and the numbers (long: read
    what you need, and all the diaries);
  - `playtest/runs/story1/director-notes.md`, what was the playtest's own fault;
  - the screenshots (`playtest/runs/story1/<player>/shots/small/`) behind the claims you use.
- **The fix panel:**
  - `docs/playtest-fixes.html`, the decision;
  - `docs/design-notes/playtest-fixes/`, especially the root causes in `r1-engineer.md`, the numbers in
    `r1-economy.md` and `r3-economy.md`, and the evidence in `r1-researcher.md` and `r2-researcher.md`.
- **The build:** `docs/ImplementationStatus.md`, and the code where you need to check what was really built (the fix
  brief's code map: `docs/design-notes/playtest-fixes/brief.md`, "The code, where to look").
- **Your own design:** `docs/multiplayer-design.html` and your old notes in `docs/design-notes/playing-together/`.

**The playtest's limits.** AI players are patient, read everything and tend to be kind. They aimed by tile numbers.
The clock ran at 5× and jumped between sessions. The listed artifacts are not game faults.

## The rounds

- **Round A, alone (now):** `a-<you>.md`, your verdict on your own design and your proposal.
- **Round B, everyone reads everyone and the fix plan:** attack, merge, and form two or three rival directions.
- **Round C, champions and examiners:** the directions specified, then played through the playtest's own story ("what
  would Ana, Ben, Cleo, Dev and Mia have done?"). The team ends round C with **one shared idea**, the direction you
  stand behind together.
- **The election:** each of you votes for the one teammate who will represent the shared idea, with a reason. The fix
  panel elects its own representative the same way.
- **The dialogue:** the two representatives discuss the design team's shared idea against the fix panel's plan, turn by
  turn, in `dialogue.md`. They aim to agree on one plan, or to name exactly where and why they still disagree. They
  close with a joint statement.
- **The decision:** the moderator writes the revised plan for the owner from the joint statement and the record.

## Round A: write `docs/design-notes/after-playtest/a-<you>.md` (about 2,000–3,500 words)

Use your old seat as your name: `wild`, `friends`, `lonely`, `economy`, `psychologist` or `outsider`.

    # Round A · <you>
    ## What held, what failed     your design's promises and assumptions against the evidence; for each failure say
                                  whether it was (a) built differently from the design, (b) built as designed and the
                                  design was wrong, or (c) not reached or not tested
    ## Is the fix plan enough?    where it repairs a cause, and where it only patches the symptom of a design flaw
    ## What I'd change            your proposal, as deep as the evidence warrants: what players do, see and feel; what
                                  it replaces (in v1.2 and in the fix plan); how it answers the run's failures (walk Ana,
                                  Ben, Cleo, Dev and Mia through it); what it costs to build, and its risks
    ## What I'd keep no matter what
    ## Questions for the others

**Rules:** evidence or it doesn't count (findings by title, sessions and action numbers, code by file and line). Read
anything; change nothing except your own file in this folder; don't build or run the game. Plain, clear English.

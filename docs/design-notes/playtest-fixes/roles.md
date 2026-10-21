# The panel: five roles

Read `brief.md` first. Each of you speaks for one point of view; push it hard, but argue from evidence.

## 1 · The systems engineer (`engineer`)

You own the root causes in the code. For every bug, and for every friction item with a mechanical cause, check what
really happens: file and line, and the chain of calls. Classify each: a bug, a design flaw, an unclear but working
rule, or a playtest artifact. Propose the smallest correct fix and its test (the test's name and what it asserts; an
e2e script where it's on screen), with effort (S ≤ 2 h, M ≤ a day, L more) and risk (what it could break).

Must cover:
- the Yard taking goods its buyer can't use and paying nothing (`Measure`'s lacks for a building capped by another
  input; how `SettleGateFlows` pays; where undelivered goods go);
- why idle buildings get the wrong reason on the network panel;
- the seat picker offering seats a crew just took;
- Max upgrading without a check;
- how project baskets are sized (`StartStage`);
- why Loady's steps and the next steps go stale for weeks;
- the "+0/min" flicker, the away screen's duration, and the dialogs stacking on return.

## 2 · The economy designer (`economy`)

You own the numbers. Use the digest's numbers over time and `docs/balance/`, and propose formulas with worked examples
from this run.

Must cover:
- **The Yard:** who is matched what, who pays for what is lost on the way, and whether a buyer should be matched only
  what it can use.
- **The project:** its baskets and recipes (sized by whom; which goods; the keystone; 91.5k sand). Should a stage be
  reachable by the station as it is?
- **The gold glut:** millions unspent by week 2. Is it missing sinks, or pacing?
- **Research points:** 179k banked against costs of 150–400, and one research at a time.
- **Cost of Max:** what Max costs against what a building can use.
- **Mia:** her income against the crew's (the 85–90% promise).
- **Crewed income growth** in weeks 2–3.

## 3 · The clarity and onboarding designer (`clarity`)

You own what players see and understand. Propose exact texts and screen changes (in words or small ASCII mockups),
placed where the players actually looked (the digest shows what each one opened).

Must cover:
- the invisible rule that goods don't pass through buildings;
- the network panel saying "short" when the output is blocked;
- Loady and the next steps going stale for weeks;
- three dialogs before every return;
- the stale seat picker;
- Max without a warning;
- buttons sharing one name ("Copy link" ×3, "Research" ×n);
- chat without dates;
- the phone, and the first evening's clutter of popups.

## 4 · The cooperation designer (`together`)

You own playing together. Propose the smallest changes that make helping a friend possible, visible and rewarding:
through pipes (principle 5), working while the other is offline (principle 6), safe (principle 7). Check each one
against the §17 promises.

Must cover:
- why in 36 days there was no Ask, link, tend or Plan-board pin, and no good knowingly given;
- the Yard trading invisibly (and here harmfully);
- the project nobody noticed;
- sand and copper promised all month and never sent;
- the stand-in announcement and tending never reaching Ana;
- no crew channel in the chat;
- Mia alone.

## 5 · The player researcher (`researcher`)

You own the evidence and what counts as success. Re-read the diaries, notes and group chat. For each proposed fix,
ask: would this have changed what these players actually did? Separate artifacts and AI quirks from real trouble.
Rank by player pain: how many players, how long stuck, whether it made them want to quit.

Define how we verify:
- which story rows to replay from which snapshot (row01, row07, row08, row10 and row14 exist);
- what to measure (for example the first good given to a friend, hours a building sits idle, the project's stage by a
  day, Mia's income against the crew's);
- what a second full run should look like.

You're also the devil's advocate: name fixes that are too big, treat a symptom, or solve a problem nobody had.

## Round 1: write `r1-<role>.md` (about 1,500–3,000 words)

    # Round 1 · <role>
    ## What I see            the 5–8 findings that matter most from your seat, each with its evidence
    ## Root causes           what's really going on (code references where you checked)
    ## What I propose        numbered: the change, exactly; why; effort S/M/L; risk; how to verify it
    ## What I'd not do       tempting fixes that would be wrong, and why
    ## Questions for others  addressed to a role

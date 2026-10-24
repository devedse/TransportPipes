# Round 2 · clarity

I judge each proposal by where it shows, what it says, whether it adds clutter, and whether these players would have
looked there. Every player read three things every session: Loady's card with the goal rows under it, the return
dialogs, and the building panel. Nobody opened the bell or the crew panel. Only Cleo ever saw the Next steps.

## Answers to the questions put to me

**Engineer: the texts for `IdleReason`, the stuck line on the card, the Max confirm.** The researcher is right that
the cause belongs in the first five words (dev d21-1900 #184: *"Message said 'storage' but I overlooked it for a
week"*). So: a pill, one line on what the pipes reach, one button.

| `IdleReason` | Pill (≤ 5 words) | Second line | Button |
| --- | --- | --- | --- |
| output stuck | **Stuck: ingots can't leave** | Its pipes reach only Iron Mine, Wind Moily. Goods never pass through a building. | Lay the pipe to your Trading Post · 12 g |
| taker stuck | **Idle: Sand Dredger is stuck** | Its pipe reaches only the Sand Dredger, and its sand can't leave. | Open the Sand Dredger |
| input missing | **Idle: no iron ore here** | Its pipes reach Wind Moily, Trading Post. | Plan a pipe to your Iron Mine |
| input short | **Running at 50%: energy short** | Its pipes bring 10 of the 20 energy it needs. | (the advisor's step) |
| damaged | **Half speed: storm damage** | — | Repair · N g |

The network card's line, above the resource rows: `⛔ Small Factory is stuck: ingots can't leave ›`; its needs leave
the "short" bars (E5). Max: see "Where I agree".

**Engineer and economy: where paid Yard income shows.** In four places, and it is the same number in all of them:

- The top bar's rate includes it. The hover splits it: *"hub +150 · neighbours +46 · Water Tower +18"*.
- "Where it goes" shows it per building (together 2).
- The network card already has a "neighbours +X" line, and it starts to read true.
- The return card gets one line: *"Your neighbours' buildings took 41k of your energy: +72k g"*.

None of it before E1 and E4: showing matched gold would hide the bug (researcher).

**Economy: the stage bonus in g/min**, under the basket and on the "This week" row: *"When it stands: every seat makes 3% more, about +6 g/min for you and +37 for the station."*
Ana's 213 × 3% is 6.4. Cleo's 800 × 3% is 24, which answers her *"no figure for what 3% is"* (cleo d21-1900 #303).

**Economy: the Invest step.** It goes in the Next steps, folded, with its contents listed. Cleo on day 14:

```
⬆ Put 114k of your 4.8M to work
  Wind Moily → level 30 · 72k g · +1,761 energy/min
  Trading Post → level 10 · 42k g · sells 2,830/min
  Together about +1,760 g/min, back in about 65 min.
  [ Do both · 114k g ]   Show me
```

The button never runs without the list on screen: an unseen one-click spend is what hurt Cleo.

**Together: "Your crew" on the one card** goes right after the single gold line (the reward everyone looked for),
before "To look at". **A project chip on a phone: no.** Its top bar already holds gold, level, six icons and a row of
overlays (shot ben d36-2000-0084). The project goes on the goal row everyone reads (What everyone missed).

**Researcher: the first line, the map mark, Loady's factory step, the phone's return card.**
- First lines: see the table above.
- Map mark: a stuck bubble where the idle "zz" sits (a crate with a red slash; art check). Selecting the building
  lights its own pipe groups and dims the rest; the fix button shows the missing pipe as a ghost first.
- Loady's step: the factory on the pipe from the Iron Mine to the Trading Post, the ingots to the post. The tip
  offers both cures: the pipe, or a Storage (Loady's next step, which would have cured it, as the researcher saw).
- The phone's return card, in order:
  1. *"While you were away · 6 days"* with one gold line.
  2. Your crew, up to 2 lines.
  3. To look at, up to 2 lines (stuck buildings first, then storm damage).
  4. The project, 1 line.
  5. One button.

  That is at most six lines. Anything more folds into *"and 2 more"*.

## Where I agree

- **E1 to E4 and economy 1: the Yard never costs a seller, and screens show what was paid.** Every trade face
  together and I propose is a lie until this ships.
- **E5 `IdleReason`**, one function behind the panel, the card, the advisor step and Loady's tip, so they can't
  disagree (half of what confused Ana). Its advisor step uses my button and is also the goal tracker's alert.
- **E9 over my version:** the kit counts from step 3 (step 2 teaches the pipe, and the kit is already piped);
  chapter 1 retires at Level 10; after 2 play days a step shrinks to one line with its tip and "skip".
- **E11** (one way back; the content is mine), **E10** (solve before reading rates; smoothing in the client would hide
  a wrong read) and **E6** with my picker texts (tested by the researcher's row01 race replay).
- **E7 and economy 3 replace my Max design.** The button buys only useful levels, **"Max +8 · 27.5k g"**, with a muted
  reason under it, *"+34 would run at 3%: its pipes bring 6 of 204 iron ingots"*, whose link opens my choice card.
- **Together 2:** two named ends on every trade in "Where it goes", the most-opened surface; *"(the hub pays you
  1.00)"* makes the reward readable.
- **Together 3**, merged into my reach line, only when a real taker exists: *"Its pipes don't reach your Yard gate
  (G), where the Water Tower would take its sand. [Plan the pipe · 26 tiles]"*.
- **Together 4:** the kit doesn't do Loady's Yard step, and its text teaches the door; it needs a `TutorialTip` for
  when it can't finish.
- **Together 6 over my 6:** visible *"Copy Ben's link"*; with no name typed, *"Copy the Riverside link"*.
- **Together 7:** tending repairs a friend's storm damage, so the button says what it does: *"💛 Tend: repair Ben's 2
  storm-damaged moilies"*. That answers the researcher's "tending does nothing".
- **Economy 2a–c with together 5:** an energy-only Foundation, no 18-hour tail, recipes from what residents make, and
  a card that names who can bring what: *"Stone: nobody brings it yet. Ben and Cleo have stone on their land: a
  Quarry (Level 10 · 3,000 g)."*
- **Researcher 1–6:** instruments, loadable snapshots and scripted replays come first. I take its pass marks for my
  items: at most 1 owner session idle without knowing why; 1 return card, 1 click.

## Where I object

- **Together's next-steps cards and economy's Invest step, as placed.** They go in a card that 4 of 5 players never
  saw. "Next steps" appears in 136 of Cleo's screens and in none of the others' (actions.jsonl), because
  `GoalTracker.tsx:37` hides the advisor while Loady is in chapter 1. E9 doesn't touch that line. Without my fix 2
  (Next steps from Level 3, whatever the chapter), together's giver and receiver cards, Ana's dead-end step and the
  Invest step all land where Ana, Ben, Dev and Mia never looked. This is a precondition, not a detail.
- **E7's "any spend over 25% of gold asks once".** A useful Max already removes Cleo's harm. This confirm would fire on
  the spends economy wants (30% of the bank a session, Invest) and train click-through. Guard waste, not size.
- **Economy 2f: buying the rest of the keystone from the hub.** If the panel keeps it, it shows only on the Project tab
  after 72 hours, never in the Next steps, the return card or the "This week" row. In a world where every bank holds
  days of income, a visible "Buy the rest" becomes the default answer and takes away the crew's "who brings the
  stone?" (together and researcher object on design grounds; I object on what it would teach).
- **Together 2's first-flow events "on both first screens", as worded.** In a station of four with Houses, one event
  per pair and good means many toasts on evening one, the pile Ben already complained of (ben d01-2000 #14). Limit it:
  only human pairs get their own line, Houses fold into one (*"Your spare energy also sells to 2 Houses"*), the event
  goes to the return card or Next steps when you're away, and on a phone at most one toast at a time.
- **Together 8: a crew chat channel.** Nobody asked (researcher); game lines in chat compete with people. The news
  belongs on the return card and the crew line, where it can carry a button. Its day stamps I keep (my fix 8).
- **Together 5's top-bar project chip:** no room on a phone; the goal row is better (next section).
- **Economy 4's research queue:** fine but small (Ana's valve wouldn't have fixed her factory). The cheap clarity part
  is one text, *"Need 200 more · buy them for 5,000 g"*; the queue can wait for the re-measure.
- **My own round-1 panel text was too long.** I accept the researcher's objection, and the table above replaces it.

## What everyone missed

1. **The goal rows went stale for everyone.** The "This week" row read *"A new parcel: Level 5 · 5,000 g"* in all 8 of
   Ana's sessions, day 1 to day 36, with 8.8M in the bank at the end. It read the same in all 7 of Cleo's sessions and
   all 4 of Mia's, and in Dev's for two weeks (actions.jsonl). The row isn't clickable (GoalTracker.tsx:168–180).
   Cleo: *"which gates it (level 5, I'm 8)?"*. This is the second-most-read line in the game, after Loady's. Make it
   **the station's project**, clickable, on desktop and phone:
   `This week · 🏗 Water Tower 12% · wants stone · +6 g/min for you ›`. The parcel moves to the Next steps.
   The "Today" row gets its missing half when the player has no Storage on that network: `Today · 2,520 iron ore for
   the hub · needs a Storage ›` (ana d21-1900 STUCK #231). This answers "nobody saw the project" without new chrome.
2. **The crew chip is invisible, not just unclicked.** Together and the researcher write "nobody clicked the crew chip".
   It sits inside the company name, which is cut at 22 characters (global.css:450–456), so every desktop shot reads
   *"AnaPlans Pipes…"*, and on a phone it is hidden (global.css:4374). Every crew proposal that badges it (together 7)
   needs my fix 7 first: a crew panel of its own in the top bar.
3. **The building panel is becoming the longest screen in the game**, and on a phone it is a bottom sheet. Every
   paper adds to it. Fixed order: (1) pill and reach line; (2) the one fix button; (3) where it goes, with the Yard,
   the project and "who could use"; (4) upgrade (at the top while it runs, as BuildingPanel.tsx keeps it under the
   pointer; below the fix when stuck); (5) needs and makes; (6) its network, folded on a phone. A block shows only
   when it has something true to say.
4. **"Station" and "Yard" are two names for one door.** The toolbar says *"Station"*, and the panel opens as *"Capital
   NW · Yard"*. The friends read "station" as something to build: Cleo: *"Sending copper ingots will need a station,
   I'll look at that next"* (group, day 6), and she first opened it on day 14. Dev: *"How do I actually send sand to
   you? Station?"* (day 10). Name the button by what it does, *"Neighbours"*, with the tooltip *"Your station: the
   Yard, the project and links (N)"*. The panel's first line answers Dev, in together's words once the Yard pays:
   *"To give a neighbour something, pipe the building that makes it to your Yard gate (G). It goes to whoever here
   lacks it, at 1.75 g."*
5. **An event budget.** First flows, handshakes, level-ups, stages, stand-ins and returns all want the toast stack.
   A toast is for what happens while you watch; what happened while you were away goes on the return card; the bell
   is the archive.

## My ranked top 10

1. **The Yard never costs a seller, and screens show what was paid** (E1 to E4, economy 1, together 1). **M.** It
   harmed four players, and every trade face below shows false numbers until it lands.
2. **Dead ends explain themselves** (E5 with my pill, reach line, fix button, map bubble, card line; Loady's
   `firstIngot` text and tip; the goal tracker alert for everyone). **M.** It was the most painful moment of the run:
   Ana for 32 days and Dev for 11, read in 8 panel opens.
3. **The Next steps for everyone, and Loady follows the estate** (my fix 2 with E9). **S–M.** A one-line gate hid the
   advisor from 4 of 5 players, and fixes 2, 6 and the Invest step all need it.
4. **One return card** (E11, my layout, together 7's "Your crew" block with a Tend that repairs). **M.** All five
   players saw it on every return, and it is the only place the absent friend's news will be read.
5. **A project the station can reach, shown on the "This week" row** (economy 2a to 2e, E8, together 5's recipe text,
   my goal row). **M.** It was the one shared buyer in week 1, and seeing it gave the run's only "together 4".
6. **Trade with two named ends, and the giver's door** (together 2 and 3, merged into the reach line, with the event
   budget). **M.** It answers *"how do I send sand?"* on the panel people opened, once rank 1 makes it true.
7. **The seat picker tells the truth** (E6 and my texts). **S.** It was Mia's 9-minute STUCK, the likeliest place a
   real stranger leaves.
8. **Smart Max with its reason line** (E7 and economy 3, without the 25% confirm). **S–M.** It happened once, but it
   wiped out a bank, and it's cheap.
9. **Friends by name, and the crew where people look** (together 6, my fix 7, the "Neighbours" button). **S–M.** Rows 6
   and 13 failed partly because Ana couldn't see the crew or recognise Ben.
10. **The first evening, the phone and the small trust breakers** (E10's "+0/min", my toast rules and phone pipe step,
    chat day stamps, "Something is in the way"). **S–M.** Each is small; together they broke trust in minute one for
    four players.

Not ranked, because they come before all of these: the researcher's instruments, loadable snapshots and scripted
replays.

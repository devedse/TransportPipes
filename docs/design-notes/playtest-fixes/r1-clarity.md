# Round 1 · clarity

My seat: what players see and understand, placed where they actually looked. Three surfaces were read by every
player in every session: **Loady's card** (top left, always on), **the return dialogs** (every session after the
first) and **the building panel** with its network card (Ana opened her factory's panel in 5 of 8 sessions, Dev his
dredger's in 3 of 4). Nobody opened the bell or the crew panel, and only Cleo ever saw the Next steps. So the fixes go
into those three surfaces, not into new ones.

## What I see

1. **The dead end that cost the most.** Ana did what Loady said, *"Build a Small Factory and connect it to the mine
   and your energy"* (tutorial.yaml, `firstIngot`), on day 4 (ana d04-2100 #107, #113). It sat Idle to day 36. Its
   panel's first line was right but read as wrong: *"Nothing on its pipes takes its iron ingot: connect your trading
   post…"*; Ana: *"confusing since it IS connected to the trading post"* (d36-2000 #289). The network card below said
   *"Energy · 20 short · the buildings get 10 of the 30 they need"* and *"Iron ore · 12 short"* (shot d04-2100-0122).
   Ana, Cleo and Dev chased energy for 32 days: a bigger windmill (d08 #146), demolishing the trading post (d14 #183),
   a Priority Valve two researches away (d14 #190). Dev: *"Idle text tells me to 'connect the trading post', which is
   the thing eating the energy"* (d14-1900 diary). His moily then read *"makes 0 of 27"* with no issue at all
   (*"nothing saying 'because no one wants your sand'"*, d21-1900 #158).
2. **Loady froze, and hid the Next steps.** Ana sat at 7/11 from day 4 to 36; Mia at 4/11 *"build a Wind Moily"* from
   day 2 to 15 with three kit moilies (*"Goal that asks for something I seem to have done"*); Dev at 6/11 *"At Company
   Level 2 you can mine"* at Level 10 (dev d21 #155); Ben at 3/11 on day 36. The words "Next steps" appear in 136 of
   Cleo's screens and in none of the others' (actions.jsonl): **4 of 5 players never saw the advisor in 36 days.**
   Cleo, who did, was told *"Build a Lab"* right after demolishing hers on purpose (d14 #259, d21 #283).
3. **Three dialogs before every return.** Of 23 returns, 14 went through Next, Let's go and Collect and continue. Two
   are titled *"While you were away"*, disagree on the duration (*"6 days of your company on its own"*, then *"instant
   of work by your company"*) and split the news: the first shows only the gold (shot ana d14-1900-0159), the third
   the levels and sales. Ana: *"Three dialogs in a row blocking everything"*; Ben: *"three screens before I see my
   estate"*.
4. **The seat picker lied twice.** *"Someone took that seat a moment ago: pick another one"* on every seat, a list
   that never refreshed, and *"A station of my own"* showing the same crew station with *"your seat"* on a held seat
   (mia d01-2000 #10–#22, STUCK #18, shots 0017, 0021). Only a reload got her out, into a station of Houses.
5. **Max said nothing.** *"Max +31"* carries no price, unlike *"Level 2 · 1,500 g"* and *"+10 · 30.5k g"* beside it
   (shot ana d04-2100-0122). Cleo's *"Max +34"* on a new Lab spent 1.55M of 1.6M (cleo d06-1400 #177): *"The card
   should have warned me."*
6. **The crew is invisible.** The crew chip sits inside `.company-name`, cut at 22 characters with an ellipsis
   (global.css:450–456): every desktop shot reads *"AnaPlans Pipes…"*, *"Cleo Analytics…"*, and the chip is cut off.
   On a phone the whole name block is hidden (global.css:4374). The agents had it in their accessibility tree (160
   screens for Ana) and still never clicked it: a bare name says nothing about what waits there. Tending and the
   stand-in notice lived behind it (rows 6 and 13 missed). Ben kept the prefilled seat name *"Riverside Co."*, and Ana
   never connected it to him (ana d21-1900 #240).
7. **Small, felt by everyone:** chat times without days (*"their answers are old (09:19 PM / 08:14 PM) so I'm not
   sure anyone is around now"*, mia d08-2000 #105); three *"Copy link"* and N *"Research"* buttons with one name each;
   on the phone, five toasts, a Level-2 card and Loady stacked over the map at minute one (ben d01-2000 #14), a pipe
   step that never says to pick the Pipe tool (ben d03-2000 #60, STUCK), and a panel over the Chat button (ben d36 #86).

## Root causes

- **The rule is real, the words are not.** Buildings exchange only within one pipe group; a "network" is all pipe
  groups joined through buildings (GameWorld.Networks.cs:7–12). The screens say only "its pipes" and "its network",
  and players read both as "everything my pipes connect". `Issues()` (GameViews.cs:815–878) rightly checks only the
  directly linked buildings but never names them, so nobody can see that the trading post isn't among them.
- **The network card counts a stuck building's needs as "short".** `NetworkNeeds` (GameWorld.Advisor.cs:573–621)
  takes the full need of each input that "holds back" a building. A factory whose output can't leave takes nothing,
  so all its inputs look binding, and the card calls energy and ore short while they sell to the hub. The advisor's
  shortage steps use the same numbers. (Checked; the engineer owns the fix.)
- **Loady's text taught the trap.** `firstIngot` says *"connect it to the mine and your energy"*, not "to your trading
  post". `TutorialTip` (GameWorld.Tutorial.cs:75) explains only the `partners` stat, so a `produced:` step whose
  action is done says nothing.
- **Loady gates the advisor.** `advisor = step === null || step.chapter >= 2 || step.waitLevel != null`
  (GoalTracker.tsx:37): while a chapter-1 step is open, the Next steps never render, so a step that can't finish
  hides the one card that might explain it. Steps count only what the player built, never the kit (Tutorial.cs:26);
  texts hard-code levels; the guide retires itself only at step 0 with Level ≥ 10 (Tutorial.cs:118).
- **The return is two components in a row.** `ReturnDialog` (2–3 pages, from 3 days away) closes, then `ReportDialog`
  opens (ReportDialog.tsx:13–14), whose duration is `formatTimeAgo(report.since)` on the browser's clock (format.ts:66)
  while the first uses the server's `daysAway`.
- **The picker loads stations once per district** (StationPicker.tsx:61–89) and nothing reloads it after the
  server's refusal (GameHub.cs `CreateCompany`); in "own" mode the old seat stays chosen (StationPicker.tsx:77–81).
- **Max is a bare count**, `Max +{maxAffordableLevels}` (BuildingPanel.tsx), with no cost, check or confirm.

## What I propose

One sentence for the rule, the same words wherever it appears (Loady, the building panel, the network card, "?"):

> **Goods never pass through a building: each building trades only with what its own pipes touch.**

### 1 · Dead ends explain themselves

**a) A status of its own.** "Idle" today means both "waiting for inputs" and "its output can't leave". Split them:

| Status | When | Pill |
| --- | --- | --- |
| Stuck: its iron ingots can't go anywhere | nothing on its own pipes could take an output | red |
| Idle: nothing takes its energy right now | its only taker is itself stuck or full | amber |
| Idle: no iron ore on its pipes | no maker of an input on its pipes | amber |
| Running at 50% | an input is short | as today |

**b) The panel names what its pipes reach**, and offers the fix before the upgrades (upgrading a stuck building is
the one thing that never helps). Ana's factory, exact text:

```
┌ Small Factory ─────────────────────────── × ┐
│ Yours · Level 1                              │
│ ⛔ Stuck: its iron ingots can't go anywhere   │
│                                              │
│ Its pipes reach only: Iron Mine, Wind Moily. │
│ Neither uses iron ingots. Goods never pass   │
│ through a building, so your Trading Post     │
│ behind the Iron Mine can't get them.         │
│ [ Lay the pipe to your Trading Post · 12 g ] │
│   or put a Storage on its pipe to keep them  │
│ show its pipes on the map                    │
│ ──────────────────────────────────────────── │
│ Upgrade · Needs · Makes · Its network …      │
```

The button plans the shortest pipe to any tile of a pipe group that reaches the trading post (or a Storage), shows it
as a ghost and lays it on a click, like *"Plan the pipe to my Yard gate"*. Dev's moily: *"Idle: nothing takes its
energy right now. Its pipe reaches only the Sand Dredger, which is stuck: its sand can't go anywhere. → Open the Sand
Dredger"*. A missing input gets the same reach line: *"No iron ore on its pipes. Its pipes reach: Wind Moily, Trading
Post. Pipe it to your Iron Mine, or let your Trading Post buy ore."*

**c) The network card leaves stuck buildings out of "short" and names them first.** Ana's day-36 card:

```
ITS NETWORK · show on the map
┌──────────────────────────────────────────┐
│ ⛔ 1 building stuck: Small Factory        │
│   its iron ingots can't go anywhere ›    │
├──────────────────────────────────────────┤
│ ⚡ Energy                     154 spare   │
│ makes 164 · used 10 · sold 154           │
│ ⛏ Iron ore                    11 spare   │
│ makes 11 · sold 11                       │
└──────────────────────────────────────────┘
```

(Needs `NetworkNeeds` to skip stuck buildings and a `stuck` list in `NetworkDetailsDto`; engineer.)

**d) The map shows a building's own pipes.** "show its pipes on the map" lights the pipe groups it touches and dims
the rest, with *"Lit: what the Small Factory can trade with. Dim: the rest of the network."* That answers Dev's *"I
can't tell which pipe joins which network"* (d14-1900 #145). A stuck building gets its own bubble beside the idle
"zz" (a crate with a red slash; art direction check), so it shows without a click.

**e) Loady teaches the rule when it bites.** `firstIngot`:

> Ore is worth more as ingots. Build a Small Factory right on the pipe from your Iron Mine to your Trading Post:
> everything it needs and makes travels on that pipe. Goods never pass through a building.

"Show me" puts the ghost on a free tile touching the pipe group that holds both the mine and the trading post. Built
but stuck, Loady's tip (a `produced:` case in `TutorialTip`) reads: *"Your Small Factory is built but stuck: its iron
ingots can't go anywhere. Show me lays the pipe to your Trading Post."*

**f) The goal tracker alerts it for everyone**, chapter or not: *"⛔ Your Small Factory is stuck: its ingots can't go
anywhere · Show me"* (`Alerts()`, GameWorld.Networks.cs:719; let it through the filter at GoalTracker.tsx:39).

Effort **M** in all: texts S, reach names and stuck detection S–M, `NetworkNeeds` S, the pipe button and lit pipes M.
Risk: "stuck" must mean "nothing on its pipes could take it", never "nothing took it this round" (a full storage for
a moment); a false "stuck" is worse than today. Verify: Game test
`NetworkNeeds_LeavesOutABuildingWhoseOutputCannotLeave` (Ana's layout: the energy need is the mine's 10); Server test
`Issues_NamesWhatItsPipesReach`; a new e2e `deadend.mjs` that builds Ana's layout, shoots panel and card, presses the
button and asserts the factory runs within a minute; then Ana's row-8 session replayed from row08.

### 2 · Loady never hides the advisor and never asks for what's done

1. **Next steps show from Company Level 3**, under Loady, whatever the chapter (GoalTracker.tsx:37).
2. **The kit counts** for every `built:`/`piped:` step but the first, which keeps the quick win and says so: *"Hi,
   I'm Loady! Your starter kit already makes energy. Let's add your own: build a Solar Panel on a sunny spot (or a
   Wind Moily)."* Mia's and Cleo's kit steps are done on arrival.
3. **Say what's missing** when the action is done and the result isn't (1e; for other steps, the first issue of the
   hinted building).
4. **A step older than two play days shrinks** to one line above the Next steps, with its tip: *"Loady 7/11 · Small
   Factory: stuck, its ingots can't go anywhere › Show me"*.
5. **Chapter 1 retires itself at Level 12**, at any step: *"Loady: You're past my first chapter. The Next steps take
   it from here."*
6. **No levels in step texts**: *"Mine the iron on your land: build an Iron Mine on an iron deposit (the Deposits
   overlay shows them)."* The level line already appears when `waitLevel` is set.
7. **The advisor never suggests a type the player demolished in the last 7 days**, and never lists one title twice.

Effort **S–M**. Risk: two cards in the corner of a small screen (the Next steps already start collapsed on a phone,
NextSteps.tsx:23). Verify: TutorialFacts `KitBuildingsCountAfterTheFirstStep`, `ProducedStepTipsWhenItsBuildingIsStuck`,
`ChapterOneRetiresAtLevelTwelve`; `guide.mjs` asserts the Next steps at Level 3 in chapter 1; Mia's row-8 replay.

### 3 · One return card

One card for every return with news, built by the server (its clock, so *"instant of work"* is gone), replacing
`ReportDialog` and `ReturnDialog`'s first two pages. Ana on day 27:

```
┌ While you were away · 6 days ───────────────────┐
│ 🪙 +1.8M gold · now Level 14                      │
│                                                   │
│ YOUR STATION                                      │
│ 🫖 Riverside Co. (Ben) hasn't played for 3 weeks: │
│    from day 34 a stand-in keeps his trades going. │
│                               [ 💛 Tend · free ]  │
│ 🏗 The Water Tower wants sand, wood and stone:      │
│    nobody here has sent any yet.     [ Open ]     │
│                                                   │
│ TO LOOK AT                                        │
│ ⛔ Small Factory stuck since day 4: its ingots     │
│    can't go anywhere.             [ Show me ]     │
│ 🌩 3 Wind Moilies storm-damaged: half speed.        │
│                               [ Repair all · … ]  │
│                                                   │
│              [ Collect and play ]                 │
└───────────────────────────────────────────────────┘
```

Rules: a section only when it has something, two lines each, six in all (the rest behind *"and 3 more"*); one
button, Escape the same; it traps keys (Cleo's B opened Build behind it, d06-1400 #95–#103). From 14 days away one
more line: *"🏡 Your seat was kept for you. Moving is free this week, if you'd like to live somewhere else."* "Where do
you land?" stays as a second screen only when there's a real choice (back from the Quiet Isles, or crates). The
station lines come from the cooperation and economy plans; I own the card. **This replaces v1.2's "three screens,
one minute"** (multiplayer-design §10): 14 returns went through a stack that repeated itself.

Effort **M** (one DTO from `ReportDto` and `ReturnDto`, one component; storm and stuck lines reuse existing data).
Verify: `return.mjs` asserts one dialog after 10 days and after 12 hours away; a Server test of the duration; Ana's
row-14 replay reaches her estate in one click.

### 4 · The seat picker tells the truth

- After *"Someone took that seat"* the picker reloads, preselects the next suggestion (returned with the error) and
  says: *"Tea Time Pipes just settled in Capital NW and keeps its seats for friends. Here's the next free seat:
  Capital N · Sunny Flats (sun, copper), next to three Houses."*
- A crew's closed station never shows in "Find me neighbours" or "A station of my own" (engineer).
- No human nearby with a free seat: *"Nobody who's playing has a free seat next to them right now. Capital N is open:
  three Houses keep it warm, and the next newcomers land here first. You can move for free later."*
- An empty name says so: *"Give your company a name first (3 letters or more)."*

Effort **S**. Verify: an `evening.mjs` variant where Mia's picker is open while Ana founds her crew; Mia settles in
one click after the message.

### 5 · Max shows its price and its sense

Every upgrade button shows its cost: *"Max +34 · 1.53M g"*. When the new level would run below 90% on its pipes, or
cost more than a quarter of the gold, the row turns into a choice (Cleo's case, from `OutputMultiplier` and
`costPerLevel` 1.15):

```
 Level 35 for 1.53M g: 97% of your gold.
 It would need 1,022 energy and 204 iron ingot a minute.
 Its pipes bring 30 energy and 6 iron ingot: it would run
 at 3% and make 15 research a minute.
 Level 9 runs at full speed on these pipes:
 11 research a minute for 27k g.
 [ Level 9 · 27k g ]   [ Level 35 anyway ]   [ Cancel ]
```

Otherwise Max acts on one click, as today. Effort **S** (label), **M** (the what-if; `ToFlowNode(building, atLevel)`
exists). Risk: count only its own pipes, not what the Yard might sell it. Verify: Server test
`MaxPlan_OffersTheHighestLevelItsPipesFeed`.

### 6 · Buttons and people that can be told apart

- Invite rows: *"Copy the link for Riverside (59GK-ENDP)"* as accessible names, and an optional **"For: [Ben]"** on
  each hold. The move-in screen reads *"Ana kept this seat for Ben"*; crew mates show as *"Riverside Co. (Ben)"* in the
  Yard, events, the crew panel and the return card.
- Research: each startable card is one button, *"Research Copper Pipes · 200 points · 10 min"*, so a click on the
  name starts it too (Cleo clicked the name on day 6, #185; nothing happened).

Effort **S**, the For field **S–M**. Verify: `ui-check.mjs` fails on two buttons with one name in one dialog.

### 7 · The crew where people look

The crew leaves the cut name for its own top-bar panel, with a line when something waits:

```
[● AnaPlans Pipes] [🫖 Tea Time Pipes · Ben resting · 💛 tend] [🪙 815k +198/min] …
```

On a phone: the emblem as an icon button among the top actions, with a dot. Effort **S**; what the line says comes
from the cooperation plan.

### 8 · Chat with days

Day dividers (*"Today"*, *"Yesterday"*, *"Tue 20 Oct"*) by the server's clock, times only within a day, a *"New since
you left"* divider, and *"last message 2 days ago"* under the channel name. The open chat never covers the goal
tracker (Cleo lost her goals behind it, d02-2000 #79). Effort **S**; a unit test across midnight and a DST change.

### 9 · The phone and the first evening

- Toasts on a phone: one at a time, 3.5 s, same-kind toasts within 10 s folded. Minute one's five become *"Welcome,
  Riverside Co.! Your kit is piped, and your Yard pipe met Ana's and Cleo's 🤝"*.
- The Level-up waits until Loady's first step is done, and is one line on a phone: *"⭐ Company Level 2 · Iron Mine
  unlocked"* (Loady's "LOADING" crate over a pile of toasts read as a loading screen).
- The pipe step: *"Now pipe it: tap the pipe button below, then drag from the panel to the pipe next to it."* "Show
  me" picks the Pipe tool, pulses it and draws the finger's path.
- The building panel is a bottom sheet of at most 55% of the screen, with Chat above it.
- Overlays that "Show me" turned on turn off when the step is done; the active chip shows ✕; Wind and Sun get a
  two-word legend (*"windy · calm"*).

Effort **S–M**. Verify: `mobile.mjs` and `evening.mjs` at 390×844 assert one toast at most and no level-up before
Loady's first step; `touch.mjs` lays the first pipe after "Show me".

## What I'd not do

- **Let goods pass through buildings.** It would have saved Ana, but it removes the logistics puzzle (principle 5),
  and valves, pumps and gate paths rest on pipe groups. The game already detects the dead end; it only has to say so.
  I'd change my mind if, after fix 1, a replay still shows a player stuck on a dead end for a whole session.
- **A "how networks work" pop-up.** One more dialog on a stack players click through blind; the rule belongs on the
  stuck building, when it bites.
- **Drop the network card.** Ana called it *"lovely"* even while it misled her; correct its numbers.
- **Explain the Yard's "spare and lacks the same good".** That's the Yard bug's face; fix the measure instead.
- **Confirm every upgrade.** Upgrades were what players trusted (*"exactly as promised"*); guard only Max and +10.
- **Remove Loady for kit holders.** Ben's best moment was step 1: *"i tapped the yellow spot, solar panel building.
  easy"*.
- **More in the bell, or more toasts.** Nobody opened the bell; what matters goes on the return card and the goals.
- **"Online now" dots in chat.** Dates and "new since you left" answer Mia honestly; presence costs privacy and would
  mostly show nobody in an idle game.
- **A seat-reservation timer.** Reloading after the refusal fixes a 20-second race without new state.

## Questions for others

- **Engineer:** Can `Issues()` name the buildings on each pipe group a building touches, and tell "nothing could take
  it" from "nothing took it this round"? Can `NetworkNeeds` skip stuck buildings without changing the advisor's other
  steps? Can the pipe planner route to the nearest tile of another pipe group, as for the Yard gate? Is a pipe-group
  id on the client's conductors cheap, for the lit pipes?
- **Economy:** Are 90% run rate and a quarter of the gold the right thresholds for the Max choice? What should the
  project card's *"You may still bring 740k g worth"* say (ten times Ana's output)?
- **Cooperation:** Which lines and buttons go in the return card's station section and the crew panel's line (tend,
  stand-in, Ask, "Ben is back")? Is "For: Ben" on holds your way to name friends? I'll write the Yard's one-line answer
  to *"How do I send sand to Ana?"* once you've fixed what it promises.
- **Researcher:** For fix 1 I'd measure the play-minutes from a building becoming stuck to it running, and STUCK notes
  about idle buildings (four here: ana d04, d08, d36, dev d14; plus dev d08's "STUCK-ish"); for fix 2, the session in
  which each player first sees the Next steps. Are row08 (Ana's factory, Mia's stale step) and row10 (Dev's dredger)
  the right snapshots?

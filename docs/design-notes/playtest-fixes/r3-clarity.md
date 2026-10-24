# Round 3 · clarity

I read the draft plan in full and the four other round-2 papers. The plan takes most of what my seat asked for: one
`IdleReason`, the cause in the first five words, the Next steps for everyone, one return card, the "This week" row,
an event budget, and no top-bar project chip. My amendments are about texts, and about three places where the plan
contradicts the evidence or a withdrawal.

## Verdict per item

- **F0 · Instruments and replays: amend.** Add three surface logs:
  - which blocks a player was actually shown: the Next steps rendered, a stuck line on the panel, the card or the
    alert, and each return-card line;
  - the number of toasts and cards on the first screen (the researcher's Ben count);
  - the return-card lines that got folded.

  Without the first log, the pass mark "owner sessions idle *without knowing why*" can't be measured: we would know a
  building was stuck, but not whether its owner was ever told.
- **F1 · The Yard is honest: accept.** Item 6 ("screens show what was paid") last, as written.
- **F2 · Dead ends explain themselves: amend, three changes.**
  1. Loady's "Show me" for `firstIngot` must put the factory's ghost on a free tile that touches the pipe group holding
     both the mine and the trading post (the advisor's `FindSpot`). The new text says "right on the pipe from your Iron
     Mine to your Trading Post". On a phone, or for Ana, who aimed at tiles near the mine (ana d04-2100 #103), that text
     can't be followed without the spot shown.
  2. When the button points at the Yard gate, its label names the gate and the reach line names the taker: *"Its pipes
     reach only: Wind Moily. The Water Tower wants its sand at your Yard gate (G)."* with **"Lay the pipe to your Yard
     gate · 26 tiles · 52 g"**.
  3. "Full" gets its own pill, *"Slowed: its Storage is full"*, never "stuck" (the engineer's link-based rule, r2
     answers).
- **F3 · Next steps for everyone, Loady follows the estate: amend one line.** The kit should count **from step 3**, not
  "every step but the first". Step 2 is `piped:solarPanel|windMoily`, and the kit is already piped. If the kit counts,
  step 2 completes the moment step 1 does, and the player's own new panel stays unpiped. Ben's panel was *"not piped"*
  (ben d02-0800 diary) until he learned the pipe tool on that step (d03-2000 #64–#67). That is the engineer's E9 and my
  r2. Also: when the kit completes several steps at once, the gold folds into one toast (Appendix, F3), not one "Well
  done!" per step.
- **F4 · A project the station can reach: amend the row and the card.**
  - The "This week" row carries what the project wants, not the reward. It must fit 390 px, so the `+6 g/min for you`
    moves to the card.
  - The card never says *"nobody here has sent any yet"*. Together is right that it scolds (r2 together, answers).
- **F5 · One return card: amend the order.** It should be gold → your crew → **to look at → the project** → Invest.
  The six-line cap folds from the bottom, and the project line is the only one repeated somewhere players read every
  session (the "This week" row). The stuck factory and storm damage cost the player money today. Use together's
  station lines (r2 together, answers), which are invitations.
- **F6 · Trade with a face: amend two points.**
  - Drop "`firstYard` counts only the player's own pipe" (see "My last dissent").
  - Fix the Neighbours panel's first line: the plan's version leaves out the Water Tower, the main buyer in week 1.
  - One rule: **one fix button per panel**. When F2's button already plans the gate pipe, "Who could use your sand"
    lists the takers without a second button.
- **F7 · The seat picker: accept.** The texts are in the Appendix.
- **F8 · Smart Max: amend for clarity.** The **Max button itself buys the useful levels**, and shows its price and its
  reason (`Max +8 · 27.5k g`, with *"+34 would run at 3%: its pipes bring 6 of 204 iron ingots · more…"* under it).
  "More…" opens the choice row. The neighbours' line goes inside the choice row only. As drafted, "a choice row only
  when Max goes past the useful level" leaves it open whether one click on Max still spends the whole bank.
- **F9 · Friends by name, the crew where people look, tending: accept.** The crew line's priority is together's:
  a possible tend, then a keep-seat card, then a friend's Ask, then "is back". The tend button names its chore.
- **F10 · Trust in minute one, the phone: amend the block order.** The plan's rule puts "where it goes" before
  "upgrade". Keep upgrade **above** the flows. The panel refreshes every few seconds, and upgrade sits high so that "it
  stays under the pointer while the rest of the panel refreshes" (BuildingPanel.tsx:193). If the flows sit above it,
  their length changes and moves the button under a clicking player. Upgrade drops below the fix button only when
  there is a fix. The full order is in the Appendix.
- **F11 · The edges: accept.**
- **Dropped list: accept**, including my own lit pipe groups, deferred to F10.

## VOTE A

**As proposed (the moderator's).** The return card is the right place: it's read every session, when the bank is
biggest, as economy shows. "Show me" opening one building with the smart-Max level preselected lets the player see
exactly what they buy, which is what Cleo lacked (d06-1400 #177). A one-tap half-bank spend would rebuild that blind
click. The Invest line counts against the six-line cap and folds first. It shows only when the bank holds 12 h of
income or more and the best upgrade pays back within 4 h.

## My last dissent

**Keep the starter kit's pipe completing Loady's `firstYard`; teach the door with the step's text instead.** F6 makes
the step count only "the player's own pipe".

- **Its author withdrew it.** Together, r2: *"I withdraw my own Loady step for this (my 4). The lesson belongs on the
  building when it bites, as clarity says, and the kit may keep doing the minute-one pipe."*
- **It adds a pipe step to the first evening, and the phone is where pipes fail.** Ben's only real STUCK was a pipe
  drag (ben d03-2000 #60). The researcher (r2, "missed") warns that every new pipe step needs the planner and a phone
  check.
- **It is a step that can stall.** A gate can be 26 tiles away (*"which is far away"*, dev d21-1900 #192). Stalled
  Loady steps were the run's second-worst clarity failure (Ana 7/11 for 32 days), and F3 only softens a stall.
- **The lesson arrives anyway.** It reaches players where it bites: F2's gate button, F6's "Who could use your sand",
  and the Neighbours panel's first line.

The replacement text, shown as the step completes from the kit:

> Your kit is piped to your Yard gate (G): that's your door to the neighbours. What reaches it by pipe goes to their
> buildings, and to the Water Tower, by itself.

## Top 8

1. **F1, the honest Yard.** Every trade line below shows false numbers until it lands, and it took money from four
   players.
2. **F2, dead ends that explain themselves.** It was the worst pain of the run (Ana 32 days, Dev 11), and the fix is
   one click from the panel she opened five times.
3. **F3, the Next steps for everyone (with F2).** A single line hid the advisor from 4 of 5 players, and F2's advisor
   step, F6's receiver card and VOTE A all live there.
4. **F5, one return card.** All five players saw it on every return, and it's the only place the absent friend's news
   gets read.
5. **F4, the project, on the "This week" row.** It was the one shared buyer in week 1, on the second-most-read line in
   the game.
6. **F6, trade with a face.** It answers *"How do I send sand to you?"* in the panel Dev opened.
7. **F7, the seat picker.** Mia's STUCK in minute one, the likeliest place a real stranger leaves.
8. **F9, friends by name and a crew that can be seen.** The crew lines of F5 and F6 name people, and row 13 failed on
   "Riverside Co.". F8 and F10 come right after.

## Appendix: the texts

All texts are final unless marked. Numbers are this run's examples. *Where* says where each one shows.

### The building panel's block order (F2, F10)

| # | Block | Shows when |
| --- | --- | --- |
| 1 | Title · owner · level, then the **status pill** (≤ 5 words) and the **reach line** | always; the reach line only when not running fully |
| 2 | **The one fix button** | `IdleReason` has a fix |
| 3 | **Upgrade**: `Level 2 · 2,000 g` · `+10 · 40.6k g` · `Max +8 · 27.5k g`, then the Max reason line | own, upgradable; below 2 when 2 shows |
| 4 | **Where it goes** (flows with named ends, the project, Thanks), then **Who could use** | flows exist; Who could use only when a real taker exists |
| 5 | Needs · Makes · Research | as today |
| 6 | **Its network** (stuck line first), folded on a phone | in a network |
| 7 | Move · Demolish… | own |

### F2 · Statuses, reach lines and buttons (building panel)

| `IdleReason` | Pill | Reach line | Button |
| --- | --- | --- | --- |
| under construction | `🏗 Ready in 1:20` | — | — |
| damaged | `Half speed: storm damage` | — | `Repair · 20 g` |
| stuck | `⛔ Stuck: ingots can't leave` | `Its pipes reach only: Iron Mine, Wind Moily. Goods never pass through a building.` | `Lay the pipe to your Trading Post · 6 tiles · 12 g` |
| stuck, gate has the taker | `⛔ Stuck: sand can't leave` | `Its pipes reach only: Wind Moily. The Water Tower wants its sand at your Yard gate (G).` | `Lay the pipe to your Yard gate · 26 tiles · 52 g` |
| taker stuck | `Idle: Sand Dredger is stuck` | `Its pipe reaches only the Sand Dredger, and its sand can't leave.` | `Open the Sand Dredger` |
| full | `Slowed: its Storage is full` | `Nothing else on its pipes takes its sand.` | `Lay the pipe to your Trading Post · … g` |
| input missing | `Idle: no iron ore here` | `Its pipes reach: Wind Moily, Trading Post.` | `Plan a pipe to your Iron Mine` (or `Build an Iron Mine`) |
| input short | `Running at 50%: energy short` | `Its pipes bring 10 of the 20 energy it needs.` | the advisor's step |
| post full | `Slowed: Trading Post full` | `It sells all it can: 400 a minute.` | `Upgrade the Trading Post · … g` |
| pumps | `Slowed: too few pumps` | `62 buildings, no Pump Station: everything runs at 80%.` | `Build a Pump Station` |

After the button is pressed, the planned pipe shows as a ghost with `Lay it · 12 g` · `Cancel`. When the pipe is
laid, a toast: `Small Factory runs: ingots go to your Trading Post`.

- **Network card**, first line: `⛔ Small Factory is stuck: ingots can't leave ›`. No "short" pill for an input whose
  only consumers are stuck.
- **Map bubble** tooltip: `Stuck: ingots can't leave`. The art is a crate with a red slash, beside the idle "zz".
- **Goal tracker alert** (everyone): `⛔ Small Factory stuck: ingots can't leave · Show me`.
- **Advisor step** (Next steps): title `Unstick your Small Factory: one pipe, 12 g`. Reason: `Its ingots can't leave.
  A pipe to your Trading Post sells them: about +34 g/min.` Button: `Lay the pipe · 12 g`.
- **"?" help**, one line: `Goods never pass through a building: each building trades only with what its own pipes
  touch.`

### F3 · Loady and the Next steps (goal tracker)

- **Step 1:** `Hi, I'm Loady! Your starter kit already makes energy. Add your own: build a Solar Panel on a sunny spot (or a Wind Moily).`
- **Step 2:**
  - desktop: `Now pipe it: pick the Pipe tool (P), then drag from your panel to the pipe next to it. Pipes carry what it makes to your Trading Post.`
  - phone: `Now pipe it: tap the pipe button below, then drag from your panel to the pipe next to it.`
- **`firstYard`**, done by the kit: see "My last dissent".
- **`firstMine`:** `Mine the iron on your land: build an Iron Mine on an iron deposit (the Deposits overlay shows them).`
- **`firstIngot`:** `Put a Small Factory on the pipe from your Iron Mine to your Trading Post: it takes ore and energy there and sends its ingots on to be sold. Goods never pass through a building.`
- **`produced:` tip:** `Built, but stuck: its ingots can't leave. Show me lays the pipe to your Trading Post (12 g).`
- **Steps the kit completes at once**, one toast: `Loady: your starter kit already did steps 3–5 · +850 g`.
- **A step open 2 play days** shrinks to one line:
  - `Loady 7/11 · Small Factory stuck: ingots can't leave · Show me · Skip`
  - without a tip: `Loady 4/11 · Build a Wind Moily · Show me · Skip`
- **Chapter 1 retires at Level 10:** `Loady: You're past my first chapter. Your Next steps take it from here.`

### F4 · The "This week" row and the project card

**The "This week" row** (goal tracker; clickable, opens Neighbours › Project):

| State | Text |
| --- | --- |
| A stage is open | `This week · 🏗 Water Tower 12% · wants sand ›` |
| Your goods go in | `This week · 🏗 Water Tower 34% · your energy goes in ›` |
| Waiting to open | `This week · 🏗 Workshop opens in 6 h ›` |
| A stage just stood | `This week · 🏗 Water Tower stands: +3% for every seat ›` |
| The Landmark is done | the row goes back to the next parcel |

**The project card** (Neighbours › Project, per good):
- **A made good:** `Energy · 34k / 53k · pays 1.00 g, the hub's full price`.
- **The keystone**, someone can make it: `Sand · keystone · Ben and Dev can make it: a Sand Dredger (Level 5) on a beach`.
  The badge's title: `The one good nobody here makes yet: +20% credit for whoever brings it`.
- **The keystone, shown to its maker** when their output doesn't reach a gate: `You make sand, but it doesn't reach your
  Yard gate (G). [Plan the pipe]`. Only to that player; never a list of who didn't deliver.
- **The keystone re-picked:** `Sand's makers are away, so the Water Tower now wants copper ore instead: Cleo can make
  it (Copper Mine).`
- **Pace:** `At this pace: about 2 days.`
- **Allowance**, only when it binds: `You've brought your share for today (26k g); more tomorrow.`
- **The bonus:** `When it stands: every seat makes 3% more, about +6 g/min for you and +36 for Capital NW.`

### F5 · One return card

Desktop (a centred card):

```
┌ While you were away · 6 days ───────────────────┐
│ 🪙 +1.8M gold · now Level 14                      │
│ YOUR CREW                                         │
│ ⚙ Ben hasn't played for 3 weeks: from day 33       │
│   Granny Ida ⚙ keeps your trades with him going.   │
│                     [💛 Tend: repair 2 moilies]    │
│ TO LOOK AT                                        │
│ ⛔ Small Factory stuck since day 4: ingots can't    │
│   leave.                              [Show me]   │
│ 🌩 3 Wind Moilies at half speed.  [Repair all · 60 g] │
│ 🏗 The Water Tower is at 34%: it wants sand, and   │
│   your land has a shore (Sand Dredger, Level 5).  │
│                    [ Collect and play ]           │
└───────────────────────────────────────────────────┘
```

On a phone the card fills the screen with the same lines in the same order. Each line's button sits under its line,
and `Collect and play` is pinned to the bottom. The limit is six lines in all, folding from the bottom:
`and 2 more ›`.

| Section | Lines (one each, newest first; at most 2 per section) |
| --- | --- |
| Heading | `While you were away · 6 days` (also `· 12 hours`, `· 40 minutes`; from the server's clock) |
| Gold | `🪙 +1.8M gold · now Level 14` (`· +2 levels` when you levelled) |
| Your crew | `🎈 Dev moved in next door (Windy Heights)` · `💤 Riverside Co. (Ben) is resting, 4 days: his estate runs and earns in full. [💛 Tend]` · `⚙ Ben hasn't played for 3 weeks: from day 33 Granny Ida ⚙ keeps your trades with him going. His seat is kept to day 93.` · `🎈 Ben is back.` · `🤝 Your energy built 18% of the Water Tower and ran Cleo's factory: +4.1k g` |
| To look at | `⛔ Small Factory stuck since day 4: ingots can't leave. [Show me]` · `🌩 3 Wind Moilies at half speed. [Repair all · 60 g]` |
| The project | `🏗 The Water Tower is at 34%: it wants sand, and your land has a shore (Sand Dredger, Level 5).` |
| Invest (VOTE A) | `💰 You have 1.8M: your 3 Wind Moilies to level 25 bring about +1,700 g/min, back in about 1 h. [Show me]` |
| From 14 days away | `🏡 Your seat was kept for you. Moving is free this week, if you'd like to live somewhere else.` |
| Button | `Collect and play` (Escape does the same) |
| When returning, Ben reads | `💛 Your crew tended your estate on 9 days: 2 Wind Moilies repaired.` |

### F6 · Trade with a face

**"Where it goes"** (building panel; only to the two ends):
- seller: `⚡ to Dev's Sand Dredger, through the Yard · 8/min · 1.75 g (the hub pays 1.00)`
- buyer: `⚡ from Ana and Cleo, through the Yard · 8/min · 1.75 g · 💛 Thanks`
- project: `⚡ to the Water Tower · 15/min · 1.00 g, the hub's full price`
- Houses: `⚡ to 2 Houses ⚙, through the Yard · 12/min · 1.25 g`

**The top bar's hover:** `hub +150 · neighbours +46 · Water Tower +18 · Houses +9` (paid only).

**First-gift events** (a toast if the player is watching, otherwise a return-card line; human pairs only):
- giver: `🤝 Your energy now runs Dev's Sand Dredger: 8/min at 1.75 g (the hub pays you 1.00).`
- receiver: `🤝 Ana's energy now runs your Sand Dredger: 8/min at 1.75 g.`
- project: `∴ Your sand reached the Water Tower: 15 a minute.`
- Houses, folded: `Your spare energy also sells to 2 Houses ⚙ at 1.25 g.`
- **Thanks, the giver reads:** `Ana thanked you for your energy 💛`

**"Who could use your sand"** (a producer's panel, block 4):

```
Who could use your sand
  🏗 The Water Tower wants 5,400 · pays 1.20 g, the hub's full price
  🙋 Ana asks for 500 · pays 2.10 g
```

- When its pipes don't reach the gate and no fix button shows: `Its pipes don't reach your Yard gate (G). [Plan the
  pipe to my Yard gate · 26 tiles · 52 g]`.
- Once connected: `✓ It goes there by itself: the Water Tower takes 15/min.`

**The receiver's card** (Next steps): title `Cleo has 6 iron ingots spare`. Reason: `A Lab piped to your Yard gate gets
them at 1.75 g by itself: no Ask needed.` Button: `Show me`.

**The toolbar:** `Neighbours`, with the tooltip `Your station: the Yard, the project and links (N)`.

**The Neighbours panel's first line:** `To give, pipe it to your Yard gate (G). Neighbours whose buildings need it, and
the Water Tower, take it by themselves.`

**The seat card** (station picker): `Riverside · wind kit · sand: a Sand Dredger on the shore, Level 5 · wood`.

### F7 · The station picker

- After a refusal (preselects the new seat): `Tea Time Pipes just settled in Capital NW and keeps its seats for
  friends. Here's the next free seat: Capital N · Sunny Flats (sun, copper), beside three Houses.`
- When no human has a free seat: `Nobody who's playing has a free seat next to them right now. Capital N is open: three
  Houses keep it warm, and the next newcomers land here first. You can move for free later.`
- A disabled seat's tooltip: `Kept for a friend of Tea Time Pipes` · `Someone lives here`.
- An empty name (under the field): `Give your company a name first (3 letters or more).`

### F8 · Max (building panel, block 3)

- **Buttons** (Cleo's new level-1 Lab): `Level 2 · 2,000 g` · `+10 · 40.6k g` · `Max +8 · 27.5k g`.
- **Reason line**, when you could afford more: `+34 would run at 3%: its pipes bring 6 of 204 iron ingots · more…`.
  For a producer: `+20 would sell nothing more: your Trading Post sells all it can. Upgrade it first.`
- **The choice row** (after "more…"): `Level 35 · 1.53M g, 97% of your gold. It would run at 3%: 15 research a minute.
  Level 9 runs at full speed: 11 a minute for 27.5k g.` Buttons: `[Level 9 · 27.5k g] [Level 35 anyway] [Cancel]`.
  When neighbours' spare exists: `With your neighbours' spare it could run at 40%, while they have it to spare.`

### F9 · Names and the crew

- Invite: `Who is this seat for?` (placeholder `a friend's name: only your crew sees it`). Buttons: `Send Ben's link` ·
  `Copy Ben's link`; with no name typed, `Copy the Riverside link`.
- Move-in: `Ana kept this seat for Ben.` The company name is prefilled `Ben's Riverside`.
- Crew mates, to their crew: `Riverside Co. (Ben)`.
- The crew's top-bar line, one at a time: `Ben resting · 💛 tend` · `Keep Ben's seat? · answer` · `Ana asks for sand` ·
  `Ben is back 🎉`. On a phone, the crew's emblem with a dot.
- Tend button: `💛 Tend: repair 2 moilies` (damage) · `💛 Tend` (none).

### F10 · The phone and minute one

- **The welcome toasts, folded:** `Welcome, Riverside Co.! Your kit is piped, and your Yard pipe met Ana's and Cleo's 🤝`.
- **The level-up on a phone**, after Loady's step 1: `⭐ Company Level 2 · Iron Mine unlocked`.
- **The pipe step's "Show me" on a phone:** it picks the Pipe tool, pulses its button and draws the finger's path
  (step 2's text is above).
- **After a placement that worked:** only `Iron Mine is being built`, never "Something is in the way". Over an
  occupied tile: `Taken: pick a free tile`.
- **Overlays:** the active chip reads `Sun ✕`. Legends: `sunny · shady`, `windy · calm`.
- **Chat:** day dividers `Today` · `Yesterday` · `Tue 20 Oct`; `New since you left`; under the channel,
  `last message 2 days ago`.
- **Accessible names:** `Research Copper Pipes · 200 points · 10 min` (the whole card is the button).

### F11 · The edges

- **The lone-human card:** `You're the only player in Capital N. The stations nearby are full right now: Houses ⚙
  trade with you meanwhile, and you can move for free when a seat opens.` With a real pair: `Tea Time Pipes lacks
  energy you have spare: a link is 18 tiles. [Offer a link]`.
- **A link offer on the return card:** `🔗 Mia Studio offers you a link: her energy for your factory, 18 tiles. [Look]`.

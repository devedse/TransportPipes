# Round C · friends: the spec of gifts by pipe

This spec covers the act of giving and what it looks like:
- **Send**, a standing delivery to one named taker;
- **Thanks**;
- **Sketch**;
- the receiver's line, *"Runs on Ben's sand"*;
- the giver's gate and the return card;
- what a phone player taps.

It sits on top of the economy designer's land deal and price (P is the one player price, 1.3 or 1.5), and on the fix
plan's F1 (the honest Yard), F2 (one true reason and one fix button), F5 (one return card) and F10 (one toast at a time
on a phone). Where those decide something, this spec uses it and doesn't repeat it.

## Question 1: Send on top of the automatic Yard

**Decision: (a), on top.** The automatic Yard keeps matching every resident's spare to every resident's honest lack,
friends included. Send puts one named taker first, lays the pipe if it is missing, and makes the gift visible.
**Send is shown only when it changes where the goods go.** That is the outsider's test: "real, or not shown at all".

**Why (a) and not (b), "inside a crew goods move only by a chosen Send":**

1. **The skimmer.**
   - Ben opened a building panel only when Loady's *Show me* opened it (a-outsider, b-outsider: ben d01-2000 #15,
     d36-2000 #81–#84). He never touched a social control in five sessions, and he stopped on day 3.
   - Under (b), the friend who never taps gives nothing to his friends. His sand would go to strangers, Houses and the
     hub instead.
   - Wild's own fix for this, "the House's line becomes yours on move-in", is an automatic flow between friends: it is
     (a) for exactly the player who needs it.
2. **Friends would trade less than strangers.**
   - Under (b), Mia on the porch trades with Ana, Ben and Cleo automatically (strangers use the market), while they
     trade with each other only by taps.
   - In run 1 nobody found a single social control in 36 days (0 Asks, 0 links, 0 tends, 0 pins: findings, "Nobody
     played together on purpose"). Building the friends' trade on taps alone repeats that bet.
3. **Players who stop.**
   - Under (a), a resting friend's goods keep reaching his friends' buildings whether or not he ever tapped. His estate
     runs at 100% for 30 days (§10), and the stand-in keeps the flow after that.
   - Under (b), a friend who stopped before tapping is invisible to his crew.
4. **The feeling is covered where it matters.** The psychologist's distinction (b-psychologist, answer to wild):
   - the default flow gives **relief** ("I didn't let Ana down"), which is what Ben needed;
   - the chosen act gives **meaning**, which is what Cleo and Dev asked for ("how do I send").

   (a) gives Ben the first without a tap and Cleo and Dev the second with one.

**Not a placebo.** Under (a), Send must change something real. It appears only in one of three cases:

| Case | What Send changes | Run-1 example |
| --- | --- | --- |
| **The producer can't reach the gate** | It lays the pipe in the same tap | Dev's dredger, 11 days on a network with no gate (dev d14-1900 #130) |
| **The taker gets less than it lacks** (the rest goes to the hub, a House or another resident) | It puts this taker first | Cleo's copper with Ana, Dev and the Water Tower all wanting it |
| **Another taker exists** | It keeps this one first when spare gets short | Ben's sand for Ana's Glassworks and the Water Tower |

When the automatic flow already gives a friend all it lacks and nobody else wants the good, the panel shows the effect
and a Thanks, and no button.

## 1 · Send

### Where it appears

Four places, all where the run showed players look (b-outsider, "Where players looked"), and no new panel:

1. **The producer's building panel**, in "Where it goes". Ana opened her factory's panel in five sessions; Dev opened his
   dredger's three times.
2. **F2's fix button, merged.** When a building can't send its goods anywhere, and the best taker is a friend or the
   Water Tower, F2's one button *is* Send. That keeps one fix button per panel (fix plan §3).
3. **The Next steps** (F3), as the bid: one line per new need, naming the friend who can.
4. **The return card** (F5): at most one social line, and a bid only when nothing better has happened (see §5).

Loady's first-gift step leads to the first of these (§6).

### The giver's panel

The rows below are in the panel's "Where it goes" block, in this order: Sends, then takers who could use it, then the
rest.

```
┌ Sand Dredger · Lv 1 ─────────────────────────────┐
│ ● Running · 15 sand/min                          │
│ Its pipes reach: your gate (G) · 2 Wind Moilys   │
│                                                  │
│ Where it goes                                    │
│ 🏖 Ana's Glassworks could use 15/min             │
│    it buys from Bertha ⚙ now                     │
│                          [ Send to Ana ▸ ]       │
│ 🏗 The Water Tower wants 5,400 · you make it      │
│                    [ Send to the Water Tower ▸ ] │
│ 🏦 the rest: the hub · 1.00 g                     │
└──────────────────────────────────────────────────┘
```

**After the tap:**

```
│ Where it goes                                    │
│ 💛 to Ana's Glassworks · 15/min · 1.30 g          │
│    you send it · since day 1 · Ana thanked you   │
│                               [ Stop sending ]   │
│ 🏗 The Water Tower wants 5,400 [ Send ▸ ]         │
│ 🏦 the rest: the hub · 1.00 g                     │
```

**Dev's case: the producer doesn't reach the gate.** F2's button and Send are one button:

```
┌ Sand Dredger · Lv 1 ─────────────────────────────┐
│ ⛔ Stuck: its sand can't leave                    │
│ Its pipes reach only: Wind Moily, Storage        │
│ 🏗 The Water Tower wants sand · Ana's Glassworks  │
│    could use it                                  │
│ [ Send to the Water Tower ▸ · lay 26 tiles · 52 g ] │
│   other takers ›                                 │
└──────────────────────────────────────────────────┘
```

**An automatic flow that already does it** (one taker, fully supplied). There is no button:

```
│ 🏖 to Ana's Glassworks · 15/min · 1.30 g · via your gate │
```

### What one tap does

1. **It lays the pipe if the producer's network doesn't reach the giver's gate.**
   - It uses F2's planner, with the player's own pipe rules, on the player's own land.
   - The price is shown on the button.
   - If no route exists or the gold is short, the button reads *"No free route to your gate · Show me"* or *"52 g
     needed"* and does nothing else.
2. **It creates the Send:** giver, good, receiver company, and the receiver's building or target as the label.
3. **It takes effect at the next Yard round,** within a minute (`CoupleYards`).
4. **It announces the first unit,** once per pair and good:
   - as a toast if the giver is watching (*"💛 Your sand reached Ana's Glassworks"*);
   - otherwise on both return cards (§5).

   The event budget applies (fix plan §3).

There is no amount, no deadline and no confirm. It is one tap; Stop undoes it.

### The pipe

- **The giver lays only their own pipe**, from the producer's network to their gate, on their own land. The plaza
  stays game-drawn.
- **A Send is not a new physical pipe** across the plaza and not a link. It travels through the Yard like every other
  resident trade: one channel, one ledger.
- **So the in-station link refusal stays** (`GameWorld.Links.cs` 313–315), and nothing is double counted (wild's own
  question to the engineer).

### The standing delivery: how the matcher serves it

A Send is a giver-chosen step in `MatchYard`, placed **after contracts and before "players with players"**. Contracts
already take their rate first there (`GameWorld.Yards.cs` 459–472); the pro-rata share follows at 475–476. Each round,
for each Send in the order it was created:

```
sent = min( giver's spare of the good after its own consumers,
            receiver's honest lack of the good (F1: need × min(f_out, f_other) − got),
            spare − protected shares of usual buyers (below) )
```

Then:
- **Payment:** at P when a building or the project takes the goods. When the receiver is an Ask filling storages, at
  the seller's own hub price (guard 2).
- **What's left:** the rest of the giver's spare goes on through the normal order (the Yard, the project, storage, the
  hub). A Send never stalls the giver and never takes what the giver's own buildings use.
- **The honest lack:** a Send only fills a lack F1 already counts. It can't create demand, so a maxed Lab can't pull a
  friend's energy through a Send either.

### The guards

1. **The 10% phase-in against usual buyers.**
   - A *usual buyer* of the giver's good is any company that took it from this giver in the last 7 days: a stranger, a
     crew mate or a House. Its protected share starts at its 7-day average rate and shrinks by 10% a week (× 0.9^(days
     ÷ 7), applied daily).
   - New spare, beyond all protected shares, goes to the Send at once.
   - The usual buyer reads one neutral line on its building panel: *"Copper · 8/min from Cleo, falling 10% a week ·
     Bertha ⚙ sells copper for 2 buildings at 1.4"*. It is never "Cleo is sending to someone else".
   - **Example:** Mia's Smelter has bought 8 copper/min from Cleo for a week, and Cleo has 10 spare. Cleo sends to
     Dev's Smelter:

     | Week | Mia's protected share | Dev gets |
     | --- | --- | --- |
     | 0 | 8.0 | 2.0 |
     | 1 | 7.2 | 2.8 |
     | 4 | 5.2 | 4.8 |

     Mia never loses more than 10% a week. That is lonely's guard (b-lonely, attack 3) and §7's own glide.
2. **Storage-bound Sends pay the seller's hub price.** Units that only fill storages (a Send to an Ask) are paid at the
   seller's own hub price, with no premium. P applies only to what a building or the project takes. This closes the
   funnel live today in `SettleAsks` (`GameWorld.Asks.cs` 246–251 pays `PlayerPrice` into storage; b-economy, attack
   4).
3. **Capped by real use.** A Send never delivers more than the receiving estate's honest lack.
4. **Who may Send to whom:**
   - any resident of the same big station, crew or porch neighbour, to another resident's building or the station's
     project;
   - never to a House;
   - between stations, links stay the tool.
5. **Limits:** one Send per giver, good and receiver; at most 6 Sends per giver.
6. **No new gold.** P is the one player price, and goods move only as goods. The Send adds no channel the economy
   hasn't priced.
7. **Privacy:** a Send is shown only to its two ends (fix plan §3).

### How a Send ends

**There is never a "stopped" line.** In every case below the receiver sees only a neutral supply line, such as
*"Sand · 15/min from the Yard and Bertha ⚙"*.

| What happens | When | The giver sees (on the panel, no toast) |
| --- | --- | --- |
| **The giver taps Stop sending** | After 1 hour's notice. In that hour the receiver's building gets the same flow; afterwards the Yard, a House or the hub supply it | *"You stop sending sand to Ana in 1 h"* |
| **The receiver stops using the good** (building removed, or the lack gone for 7 days) | It lapses by itself | *"Ana's Glassworks no longer uses sand: your sand goes to the Yard again"* |
| **An Ask fills up** | It ends | *"Ana's Ask is complete 🎉"* |
| **Either side Blocks the other, or one moves away** | It ends after 1 hour | — |
| **The receiver is asleep** (day 30+: the estate makes and uses nothing) | It pauses, and resumes when they're back | *"Ana's Glassworks is resting; your sand goes to the Yard meanwhile"* |
| **The receiver declines** | Ends: one tap, *"Not now"*, on the receiver's line, for players who prefer their own supply. It doesn't come back unless the giver taps Send again | — |

**When the giver stops playing:**

| Days away | What happens to the Send |
| --- | --- |
| 0–30, resting and dormant | It runs on: the estate runs at 100% (§10) |
| From day 30, asleep | The stand-in keeps it as a line, on the economy designer's stand-in terms. The receiver's line says so (§3) |
| On return | It is the giver's again, with nothing to accept |

## 2 · Thanks

- **Where:** on the receiver's input line (§3) and beside the gift on the receiver's return card.
- **For what:** any named input, Sent or automatic.
- **How often:** once a day per pair (F6).
- **What it does:**
  - the giver's next return card says *"Ana thanked you for your sand 💛"*;
  - a small heart shows on the giver's gate pennant for a day (§4).
- **What it doesn't do:** no gold and no XP (F6, and the together designer's "no rewards for giving").
- **The counts:** never shown to anyone else. The giver's panel says "Ana thanked you", not "thanked 14 times".

## 3 · The receiver's line: "Runs on Ben's sand"

**Where it shows:** on the receiving building's panel, as its status line's second half, when one friend's *Send*
covers at least half of one input.

```
┌ Glassworks · Lv 1 ───────────────────────────────┐
│ ● Running · runs on Ben's sand 💛                 │
│ Makes 5 glass/min → the hub                      │
│ Needs                                            │
│ 🏖 sand 15/min · from Ben, sent · 1.30 g          │
│                                    [ 💛 Thanks ]  │
│ ⚡ energy 25/min · your own                        │
└──────────────────────────────────────────────────┘
```

**The other forms, in the panel's own text:**

| Situation | Line |
| --- | --- |
| Several suppliers, with a Send among them | *"sand 15/min · 9 from Ben, sent 💛 · 6 from Bertha ⚙"* |
| An automatic flow, no Send | *"sand 15/min · from Ben, via the gate · 1.30 g"*. Named, with a Thanks, but no heart and no "runs on". That is relief, not a gift |
| The giver is resting | *"Runs on Ben's sand 💛 · Ben is resting; his dredger keeps sending"* (crew only; strangers see no state, §10) |
| The stand-in | *"Runs on Ben's sand · Granny Ida ⚙ keeps it coming while Ben is away"* |
| A Send to the Water Tower | the card's ring and a line, *"Ben's sand 💛 39%"*. Only those who delivered are named, never who didn't |

**On the map:** the receiving building's input pellets are drawn in the giver's colour. This is the one picture of the
gift at the building the receiver looks at, since the Yard corner is off screen at the default zoom (b-outsider, attack
1).

## 4 · The gift at the giver's gate

The gate is on the edge of the giver's start area, nearest the Yard (§4), so it is near the HQ the camera centres on.

- **The pennant:** one small pennant on the gate per Send, in the receiver's colour with the good's icon. Pellets in the
  giver's colour leave the gate along the gate strip.
- **On hover or tap:** *"Your sand → Ana's Glassworks · 15/min · since day 1 💛"*.
- **Thanks:** a heart on the pennant for a day.
- **Art:** the pennant is a UI sprite drawn over the tile, not a building.

## 5 · The return card

F5's card has at most six lines. Gifts get **one social line**, the best one, with the rest folded under "and N more"
(b-outsider, attack 3).

**The priority order:**
1. a friend arrived or came back;
2. your gift's effect, or a Thanks for it;
3. a gift you received;
4. a Sketch to build (§7);
5. a bid.

**The giver: Ben, day 2, 08:00, on his phone.**

```
┌ While you were away · 12 h ──────────────┐
│ 🪙 +43.7k gold · Level 3                  │
│ 💛 Your sand kept Ana's Glassworks        │
│    running: 10,800 sand · Ana thanked you│
│ 🏗 Water Tower 34% · wants sand           │
│              [ Continue ]                │
└──────────────────────────────────────────┘
```

**The receiver: Ana, day 2, 20:00.**

```
│ 💛 Ben's sand ran your Glassworks all night:   │
│    10,800 sand              [ 💛 Thanks ]       │
```

**A bid, when no better social line exists.** It shows once per need, and never says "unanswered":

```
│ 🏖 Ana's Glassworks could use your sand  [ Send ▸ ] │
```

**Ben, day 36:** *"💛 Your sand kept Ana's Glassworks running for 33 days · Granny Ida ⚙ from day 30"*. Someone needed
you, and nothing was lost.

## 6 · What a phone player taps

**Ben's whole social game on a phone is three single taps, and none is a drag.** His only real STUCK was a pipe drag
(ben d03-2000 #60).

| Moment | What Ben taps | Taps |
| --- | --- | --- |
| **Evening 1:** Loady's step *"Send your first gift"*, after the first sale step | **Show me** opens the producer's panel as a bottom sheet (≤ 55%, F10), with the Send row lit. Then **Send to Ana ▸** (or *Send to the Water Tower ▸* when no friend's building takes sand yet). F2's planner lays any missing pipe | 2 |
| **Any return:** the bid line on the card | **Send ▸** | 1 |
| **A gift received** | **💛 Thanks**, on the card or the building line | 1 |
| **A Sketch** | **Build it** on the card. Drawing a sketch is desktop work; on a phone, *Look ›* opens the friend's estate read-only | 1 |
| **Stop** | **Stop sending** in the panel, then the confirm sheet | 2 |

Under (a), if Ben never taps at all, his kit sand still reaches Ana's Glassworks or the Water Tower through the Yard. His
card then reads *"Your sand ran Ana's Glassworks"*, which is relief without a tap.

**Loady's step text:** *"Your sand can run a friend's building. Send it: one tap, and it keeps going while you're
away."* It is done when the player has a Send of their own (`sent:` in `tutorial.yaml`). A kit flow doesn't count, by
the same rule as `piped:` today.

## 7 · Sketch

### How a helper gets there

The entry comes to the helper (nobody visited a friend in 28 sessions). On the crew mate's Next steps and return card,
when a crew mate's building has been stuck for more than one of their sessions, using F2's IdleReason:

```
│ 🔧 Ana's Small Factory is stuck: its ingots can't leave   [ Look › ] │
```

### Look and sketch (desktop)

- **Look ›** opens Ana's estate read-only, centred on the building, with its panel: the reason and the reach line
  (`VisitPanel.tsx`, extended).
- A **Sketch** tool places ghost pipes, and buildings Ana has unlocked, on her land.
- The ghost previews its effect on her network and her cost:

```
┌ Sketch for Ana ──────────────────────────────────┐
│ Pipe: Small Factory → Trading Post · 4 tiles     │
│ Effect: her ingots reach her Trading Post        │
│         +28 g/min for Ana · costs Ana 12 g       │
│ Note (optional): "goods don't pass through the   │
│ mine, so the factory needs its own way out"      │
│                    [ Send the sketch to Ana ]    │
└──────────────────────────────────────────────────┘
```

### The owner builds it

On Ana's card and on the factory's panel:

```
│ 🔧 Dev sketched a fix for your Small Factory   │
│    +28 g/min · 12 g      [ Build it ]  Not now │
```

**Build it** places the ghost with Ana's gold and her pipe rules, through `BuildBlueprint`'s placement but with no
Blueprint Office. Dev's next card then reads *"Ana built your sketch: her factory runs (+28 g/min) 💛"*.

### The rules

- Crew only.
- One open sketch per helper per friend, at most 20 pipe tiles and 2 buildings.
- It expires after 7 days, and *Not now* dismisses it.
- Nothing is built without the owner's tap, and nothing on their land changes before it (wild withdrew helper-laid
  pipes, b-wild).
- No gold or goods move.

## 8 · Words

This adds two verbs, **Send** and **Thanks**, and one phrase, "runs on Ben's sand". **Sketch** appears only when a
friend is stuck, so it isn't a first-evening word. Players never read "line", "priority", "Send record" or "usual
buyer".

## 9 · The code it touches

| Where | What |
| --- | --- |
| `TransportPipes.Game/State/GameWorld.Sends.cs` (new) | The Send entity on the company: create, stop with notice, lapse, decline, pause for an asleep receiver, the hand-over to the stand-in, the 7-day usual-buyer shares and the phase-in, events |
| `GameWorld.Yards.cs` | `MatchYard`: the Send step after contracts (459–472) and before the pro-rata share (475–476). `SettleGateFlows`: P for building and project takes. Usual-buyer history is fed by F0/F1's paid-flow ledger |
| `GameWorld.Asks.cs` | `SettleAsks` 246–251: storage-bound units at the seller's hub price. `MatchAsks`: Sends ending at an Ask go first |
| `GameWorld.StandIns.cs` | The stand-in carries the sleeper's Sends as lines, on its terms |
| `GameWorld.Blueprints.cs` | Sketch: a crew ghost placed with `BuildBlueprint`'s placement, with no office |
| `GameWorld.Advisor.cs` | The bid line ("X's building could use your good"); the stuck-friend line |
| `GameWorld.Tutorial.cs`, `Content/tutorial.yaml` | The `sent:` step |
| `GameWorld.Return.cs` | The card's one social line and its priority |
| `TransportPipes.Server/Services/GameViews.cs` | Building details: takers, Sends and inputs by source (around 703–735, today's flows), with pairs shown only to their ends. The hub's commands: Send, StopSending, DeclineSend, Thank, SubmitSketch, BuildSketch |
| `BuildingPanel.tsx` | "Where it goes" rows and buttons; the receiver's line and Thanks |
| `ReturnDialog.tsx` | The social line |
| `NextSteps.tsx` | The bid and the stuck-friend line |
| `VisitPanel.tsx` | Read-only friend estate and the Sketch tool |
| `GameCanvas.tsx` | Input pellets in the giver's colour; the gate pennant |
| `Toasts.tsx` | The first-gift event, within the budget |

**Tests:**
- `YardFacts`:
  - `ASendGoesBeforeTheYard`
  - `ASendPhasesInAgainstAUsualBuyerAtTenPercentAWeek`
  - `ASendIntoAStoragePaysTheHubPrice`
  - `ASendNeverExceedsTheHonestLack`
  - `ASendNeverLowersTheGiversIncomeBelowTheHub` (lonely's standing rule)
  - `ASleepersSendRunsUnderTheStandIn`
- `TutorialFacts.AKitFlowDoesNotCompleteTheGiftStep`.
- e2e `send.mjs`, on a desktop and on a phone with touch: Dev's stuck dredger, one tap, sand at the Water Tower within
  20 game minutes. That is the fix plan's step-7 gate, re-aimed.

**Size:**

| Part | Size |
| --- | --- |
| Send | M, about 2 days |
| Thanks and the receiver's line | S, partly F6 already |
| Gate pennant and pellets | S–M |
| Sketch | M, 1–2 days |

That is about 4–5 days in all, replacing most of F6's two M items.

## 10 · What the examiners should check

**Lonely:**
- a stranger's protected share under a new Send;
- a bid from a stranger shown only after a Handshake (b-lonely);
- a sleeper's Sends carried by the stand-in.

**Outsider:**
- Ben's three taps on a phone;
- one social line on the card;
- no button where the Send would change nothing.

**Psychologist:**
- is the default flow's named line enough relief for Ben, with no tap;
- is the Send felt as giving for Cleo and Dev;
- does anything read as a duty? There is no "stopped", no daily reminder, and a bid shows once per need.

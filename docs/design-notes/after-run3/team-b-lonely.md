# Round B · lonely

I read all six round-A papers with my two players in mind:
- **Mia**, alone beside Jo, a neighbour who never chats, with 7 days between sessions;
- **Ben**, who stops on day 3 and comes back on day 36 to 5.5M.

## 1 · What I take, and what I drop of my own

**I take:**
- **"Wants", the scaffold and the "What stops you" line** (outsider). One word players already read (*"Water Tower 89%
  · wants water"*), a picture, and one line naming the binding limit with its key. Ben skims; a want he can see on the
  map is the only kind he will follow (ben d36: *"upgrade to level 10, I don't know where"*).
- **Widen ×2/×4 for hours of income** (wild). Gold buys capacity at once, never goods, and its price grows with
  income. Mia can widen her own pipe with no friend at all. The ×4 cap keeps a friend's copper the way past 400 a
  minute.
- **A preview before every buy and an hour's Undo** (psychologist). Hoarding was learned: Cleo's Max cut her income
  from 783 to 285 g/min (cleo d06 #180), and Dev called an upgrade *"a trap"* (dev d10). A player alone has nobody to
  warn her, so the game has to.
- **"Pay the rest for Ana", with the gold going to the game** (wild, friends, economy, psychologist). It is my "Fund ▸"
  under the name four of us chose. It is the button behind Ana's *"I have 19M gold, say if you need money 💛"* and
  Ben's *"I still can't help"* (ben d36).
- **An order paced over hours, with friends' goods taking it off as they arrive** (friends). Gold never replaces a
  friend instantly, and it is never stuck either.
- **The Era wonder sized by income, with gold able to commission at most half a stage** (economy). It is the one
  shared want that includes a stranger: Mia and Jo are named on the same line as the crew.
- **A branch next to a friend from Level 15, for 2 days of income** (wild). I add one placement: on a small server, a
  player alone may place it next to a crew that plays. That is Mia's *"more people live"* (mia d08 #119).

**I drop:**
- **"At once from the hub at 2.5×".** My players never needed it: Mia's sessions are 7 days apart and Ben's 33, so
  goods that arrive within 6–24 hours arrive before they are back. What they need is **"not never"**, and every paper
  but the outsider's gives that.
- **"While I'm away".** No paper took it. Ben quit after three short sessions, so he would never have set the switch on
  day 2, and the evidence for it is my guess. His goods already work for his friends while he is away (the redesign's
  *"Your sand ran Ana's Glassworks"*).
- **"Fund ▸" as a separate verb.** It is "Pay the rest".

## 2 · What I reject, and why

1. **Gold never buys a want's materials (outsider).** It fails both my players.
   - **Ben** returns with 5.5M and a crew whose wants need goods his land doesn't make. Under "never", his gold can't
     reach any of them. His worst moment was *"Everyone has been asking where I am for weeks and I let them down"* (ben
     d36).
   - **Mia** has no crew. Every material chain she lacks needs land goods from a House under the floor (2 + ⌊L ÷ 10⌋
     buildings per good), so "build the chain yourself" is slow and capped.
   - The outsider's own fallback, *"Cancel · your 50k back"* after three play days, means three weeks of a want sitting
     unfilled for Mia before the game gives up.
2. **A window that waits on the absent (economy).** *"24 hours, or until every crew mate who can make the good has had
   one session, at most 72"*. A crew mate who has stopped holds every want for 72 hours.
   - **Fix:** count only *lit* residents (played in the last 3 days) who make the good. With none, the window is 0. The
     redesign's own guard is that nothing waits on someone who isn't there.
3. **Two prices for gold-bought goods (economy's 2.5 against everyone else's 2.0).** Keep one: **2.0 × base**, the
   existing Ask price (`AskHousePrice`, GameWorld.Asks.cs:33). One fewer number.
4. **Three shared money sinks at once**: wild's Capital lots, economy's commission and the psychologist's Landmark
   tiers. For run 4 I'd ship one, the **Era wonder with commission**:
   - it already exists, already scales with income (`StageRequirements`) and already spans the Capital, so Mia is in
     it;
   - the Landmark tiers are per station, and Mia's station is Houses and Jo.
   - Lots and Landmark tiers come later, if banks still pile up.

## 3 · The three open questions, final

**1. Can gold buy a want's materials?** **Yes, never at once.**

| Rule | Value |
| --- | --- |
| The button | *"Order the rest ▸"* |
| Delivery | A House brings them at 2.0 × base, **paced evenly over 6 hours** |
| Friends first | Friends' Sends, their Storage and the Yard's spare fill first; what they bring comes off the order, and the unused gold is refunded |
| Window | It opens after a window that counts only lit residents who make the good (24 h; 0 if none) |
| Who may order | The owner, or any crew mate or station resident ("Pay the rest for Ana"). The gold goes to the game, never between players, never between same-device accounts |
| Where the goods go | Into the want, never a Storage |
| Credit | Ordered units count 25% toward shared credit, with no 💛 |

**2. Do hub orders become buyers?** **Yes**, in the same step.
- A buyer at your trading post, filled from production, sized at 30 minutes of measured income, paying 2.0 × base.
- Offered only to lit players, and shown when filled, never as a task.
- In run 3 none was delivered after day 14.

**3. The two-friend keystone?** **Now, but only with three conditions.**
- **The conditions:**
  1. both seats are lit when the stage starts;
  2. an away seat's input comes from its House at 1.4, uncapped (the redesign's backstop);
  3. the 2-day pass-over (built) stays.
- Elsewhere, today's rule.
- **Why I change from "later":** the conditions answer my seat's objection, that the goal would depend on presence.
  The readability worry is the researcher's, and run 4 counts acts, so it stays readable (wild).
- A station of Houses, like Mia-B's, never gets one.

## 4 · The solution I'd sign, in build order

| # | Mechanism | Numbers | Replaces |
| --- | --- | --- | --- |
| 1 | **Wants.** A building, upgrade or pipe tier short of materials stands as a scaffold that *wants* them. It fills from: your producers → friends' Sends or their Storage (*"Send them ▸"*, named, 💛) → the station's spare by the Yard at 1.3 → an Order | — | `Pay()` from Storage for materials; Deliver; "N in your storages"; the Plan board's Ask form; the Wonders minigame |
| 2 | **Order the rest / Pay the rest for Ana** | 2.0 × base, 6 h pace, friends first and refunded, lit-only window, 25% credit, same-device blocked | The Ask's deadline fill, for wants |
| 3 | **The pipe.** The "What stops you" line from the solver; the pipe tier as a want; **Widen** | ×2 for 1 h, ×4 for 3 h of measured income; cap ×4 | Copper Pipe's silent click; Max running into the pipe |
| 4 | **Safe spending:** a one-sentence preview from the solver before every buy, and an hour's Undo with a full refund | — | Max's blind spend |
| 5 | **Hub orders as buyers** | 30 min of income, 2.0 ×, lit players only | Deliver-from-Storage orders |
| 6 | **The Era wonder**, sized at each stage's start to 6 h of the Capital's summed measured income; a buyer in every Yard round; **commission** | ≤ ½ of a stage at 2.0 ×, 25% credit, a Patron line; the result on every card | The fixed world-start size; the silent end |
| 7 | **Research:** Labs rest; Refinements, the first lines raising limits (pipes +10%, trading +10%) before +2% a chain | 5k × 1.6ⁿ points, capped +50% a chain | Labs eating ingots for nothing |
| 8 | **The two-friend keystone**, under the three conditions | — | Water as the keystone everywhere |
| 9 | **Later: a branch** next to a friend, or for a player alone next to a crew that plays | Level 15, 2 days of income | Level 25 + Era 2 |

**My guards on all of it:**
- Nothing waits on a player who is away.
- No want, bid or order is shown to a player who is away.
- No sink is upkeep: no fee, decay or maintenance eats a resting estate.
- Gifts are shown only as given, never as "who didn't".

**My players, through it:**
- **Ben's day-36 card** leads with what his goods went into, then one line: *"Ana's Steel Mill wants 120 bricks · Pay
  the rest ▸"*. That is one tap on a phone, and his first act back reaches a friend.
- **Mia beside Jo** sees *"What stops you: your Trading Post sells all it can · a Market Hall … 500 bricks · Start it
  ▸"*. Jo's spare, if any, fills first. She orders the rest, and it stands before her next session. Her gold widens her
  pipes, and her name is on the Dynamo's Patron line beside the crew's.

## 5 · One mark for run 4 that would prove it wrong

**Mia, alone beside Jo, adds at least 3 buildings between day 8 and day 15 and has spent at least 20% of her earnings
by day 15.** In run 3 she went from 10 to 11 buildings and spent about 5%, then never played again.

If she doesn't, the solution works only through friends, and the player who has none still has nothing to want.

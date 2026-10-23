# Round B · friends

I read the five other round-A papers and the fix plan again. We agree on the diagnosis. We don't yet agree on the
mechanism. Behind the shared words ("a need, an act, a better price") sit five real conflicts, and each one costs
someone: the solo player, the one who stops, or the phone player. This paper sorts them out.

## Answers to the questions put to me

**Wild: should Send go before the Yard, and does it make the matcher pointless inside a crew?**
- **Yes, Send goes before the Yard,** right after contracts. The matcher already has that step: contracts take their
  rate before "players with players" (`GameWorld.Yards.cs` 459–472). A Send is a giver-chosen contract with no rate and
  no penalty, so it is a small change.
- **No, the matcher isn't pointless.** It carries everything nobody named:
  - Ben on his phone, who skims and may never tap;
  - spare beyond what a friend's buildings use;
  - Houses;
  - strangers.
- **One guard the run's code makes necessary.** The design's "usual buyers first" (§7) was never built: the matcher
  shares pro rata among players (`Yards.cs` 475–476). So a Send could take a stranger's long-standing supply
  overnight. Rule: against a buyer who took the good in the last 7 days, a Send phases in at 10% of that buyer's share
  a week. This is the design's own glide.

**Lonely: would Ana's crew have welcomed Mia on the porch? Who may close it?**
- **Yes, the evidence is plain.**
  - Cleo answered Mia on day 2, Ana on day 4 and Dev on day 21 (mia d08-2000 #104).
  - Ana told the group "Mia in the game chat says hi" (group, day 21).
  - On day 27 Ana asked Dev to "try a Yard Link to me or Mia".
  - The crew reached for her four times, and the game gave them no way.
- **Keep the existing control:** any active member may close their station to *future* strangers, with no vote.
- **Add one rule, so one crew mate can't shut out another's acquaintance:** any crew member can still invite a
  stranger into the crew or onto the porch by name, even while it is closed. A personal invite always beats a closed
  door.

**Economy: does land that requires a neighbour make week 1 more fun, or does a friend who quits become a hole?**
- **More fun.** Ana's first message was a division of labour (group, day 1 20:05), and so were the crew's first five
  minutes.
- **A hole only for growth.**
  - A resting estate runs at 100% for 30 days, and the stand-in keeps the old terms for 14 more (§9).
  - Your Houses then sell one building's worth.
  - So what Ben's absence costs Ana is her *second* Glassworks, not her first. That is the honest price of a missing
    friend, and the reason to invite.
- **One condition:** the first building of every chain must always run without a human, from a House.

**Psychologist: is a ring of kit consumers too neat?**
- **Yes.**
  - Friends pick their own seats (Ana's suggestion is only preselected, §5), and they arrive in any order: Dev came on
    day 8. A ring hard-wired into kits presumes who sits where and when.
  - Crews of 2 and of 6–8 break it.
  - Two friends who want the same role are fine for energy, which every seat makes, and a problem for land goods.
- **Better:** the kit holds the *supply* (the seat's own good), the land holds the *need* (no copies, as the economy
  designer proposes), and the consumer is the player's own choice, offered by the bid. That gives no ring and no
  ordering problem.
- **On evening one,** the project is the consumer.

**Outsider: does a designed pair bring guilt back?**
- **Ben's guilt came from a broken promise, not from a dependency.** His diaries tie it to "no sand sent" every time
  (ben d02-0800, d03-2000, d36-2000).
- **A kept promise removes the source.** That means a kit that makes sand from minute one, a flow that keeps running
  for 30 days while he rests, and a stand-in after that.
- **The guilt that could remain is "my friends depend on me".** Two things answer it:
  - the game never says "stopped sending";
  - the psychologist's **Let it go** hands any promise back to a House without a word to anyone.

## What I take from the others

| From | What I take |
| --- | --- |
| **a-economy** | **Land dealt per station, no copies.** The five Era-1 land goods are spread over a station's seats; the surveyors' patches go, for new worlds. This is the decisive finding of round A: against a free ★2 patch a neighbour's input loses at any price ("+54 g/min for 2,100 g against +10 for 1,500 g"). My round-A specialty kits alone would have failed. |
| | **The price 1.3 and the ladder:** hub 1.0 < House buys 1.15 < friend 1.3 < House sells land goods 1.4 (one building) < 2.0 < 2.5. |
| | **Bots with a light profile:** 10–15 buildings, a session every 2 days. The fix plan's crew-bot gate is miscalibrated. |
| **a-lonely** | **The porch.** **"Today here"**, the one line of visible life. **The tend names who tended** and carries a resting friend's spare to the project. The standing rule **"nobody's action lowers your income"**. **Mia played twice in run 2**, once without Jo. |
| **a-psychologist** | **The bid goes to the friend who *can*** ("Ana's Glassworks could use your sand · Send it"), never "to the station". **"Runs on Ben's sand 💛"** on the receiver's panel. **Let it go.** **Pledges** ("I bring it") on the project's goods. The **market / social split**: the automatic Yard is shown as the market without names, sends with names. |
| **a-outsider** | **The network you see is the network that flows** (pipe groups tinted), shipped before F2's button. **The second-factory test.** **Fewer words** ("send", "gate", "neighbours"; drop "station" from what players read). |
| **a-wild** | **Crew research:** a tech a crew mate has costs you 75% less, and no points move. **Kits may hold one building above the player's level.** The Works, as a later shape of the project (below), not now. |

## What I attack

1. **Wild's helper-laid pipes ("crew hands", up to 100 g at the helper's cost).**
   - This changes a friend's network without their tap. The run shows why that is dangerous: the friends' advice was
     usually wrong.
     - Cleo: "probably energy or the ore going to the hub first" (group, day 6).
     - Cleo: "the trading post sells first, I think … its own wind moily" (group, day 10).
     - Dev's own fix "merged into the old pipe network" and changed nothing (group, day 14).
   - Laid by a helper, each of those would have re-routed Ana's flows on a wrong theory.
   - A Sketch the owner builds with one tap, after seeing its effect ("+28 g/min"), gives the same help with consent.
2. **Wild's Send as links inside the station** (dropping the refusal in `GameWorld.Links.cs` 313–315).
   - That puts a second channel between station mates beside the matcher, which wild himself asks the engineer about:
     "without double counting?"
   - Its auto-accept "when it only replaces imports" is the Yard again, under another name.
   - A Send inside the Yard is one channel and one ledger, and it sits where contracts already sit.
3. **Wild's Works paying feeders hub value + 80% of the value it adds.**
   - His own table gives the feeder 1.27–1.50 × base. That is more than the one price at 1.3, so the station's machine
     outbids a friend's Glassworks.
   - Cleo, who checks every number (cleo d02-2000 #84), would feed the machine, not Ana.
   - "Anyone can put gold in" means Cleo's 30M builds every machine.
   - It also needs a station-owned building, a margin ledger and new art in the Yard corner: 3–4 days, against F4,
     which is specified and gated already.
   - I'd keep the Works as a round-C option for stages after the Water Tower, on one condition: no feeder share above
     the one price.
4. **The economy designer's "no copies", if it ships alone.**
   - Without patches, every Level-5 chain must reach the Yard gate to get its input from a friend or a House. That
     makes the gate pipe a gate on the middle game.
   - Dead ends were 5 of the 9 real STUCK notes (playtest-fixes §1). Ben's only real STUCK was a pipe drag on a phone
     (ben d03-2000 #60).
   - So "no copies" may ship only after F2's one-tap gate button and the tinted pipe groups pass their replays.
   - Halts need all five goods on two seats, which is the economy designer's own risk 2.
5. **The psychologist's bids on the return card.**
   - F5's card has six lines at most, and Ben's first phone screen was already "a pile" (ben d01-2000-0014).
   - A bid feed there becomes the pressure the lonely veto forbids.
   - Put bids on the producing building's panel and in the Next steps. The card gets at most one line, and only
     "runs on your sand" news, never a request.
6. **The fix plan's gate for the price and the trade share.**
   - The crew-of-four bot run decides 1.75 against 1.5 by t ≥ 15%. The economy designer showed that bots with about
     300 buildings exhaust their patches and buy, while humans with 10 buildings never do.
   - A gate that bots pass for reasons no human has tests nothing. Replace it with the light profile.

## What each proposal costs the solo player, the one who stops, and the phone

| Proposal | Solo | Who stops | Phone |
| --- | --- | --- | --- |
| Land dealt, no copies (economy) | Houses sell one building per good at 1.4: about 96% of a crewed player while small, about 85% by week 3 (a-economy) | Growth waits for a human; the first building never does | More gate pipes, so it needs F2's one-tap button first |
| Kits make the seat's good | None | Their kit keeps giving for 30 days at 100% | Nothing to do |
| Send, one tap | None (the matcher serves strangers) | A sleeper's Send runs on, then a stand-in holds it at old terms | One tap on a panel. Ben opened only his panel and the post, so it must sit there |
| Sketch | None (crew only) | Can't be helped while away, which is fine | Building a sketch is one tap; drawing one is desktop work |
| The Works (wild) | Her station's machine runs on her goods and a House's | Feeds on at 100% | Nothing to do |
| 1.3 (economy) | House buys at 1.15, so the solo player's House income falls a little | Stand-in at 1.3 | None |
| The porch (lonely, outsider) | The solo player's gain | If the crew sleeps, she may sit by four sleepers, so the lone-human card must still fire | None |

## The directions

**D1 · Honest and lit: the fix plan, with its triggers.**
- **Core:** ship F0–F11 as decided, and pull the §7 levers only if run 2 fails.
- **Replaces:** nothing.
- **Biggest risk:** run 2 repeats run 1 with better lighting. With the patches, the advisor ranks the own mine first,
  t stays near 0, and nobody gives by an act. We would spend about 1.3M tokens and a week to learn what the group
  chat already says.

**D2 · Needs and gifts.**
- **Core:**
  - land dealt per station with no copies, and kits that make the seat's good;
  - one price, 1.3;
  - **Send** as a named priority in the Yard;
  - Sketch;
  - the porch.
- **Replaces:** the surveyors' patches and energy kits; F6's announcements as the main path; the §7 triggers; the
  price vote.
- **Biggest risk:** the most balance change of the three, and a friend's absence now visibly slows growth.

**D3 · The Works.**
- **Core:**
  - a station-owned machine every seat feeds, paying a share of the value it adds;
  - kits make the seat's good;
  - Send exists, but the machine carries week 1.
- **Replaces:** F4's baskets and bonuses.
- **Biggest risk:** the machine outbids friends and becomes the shared donation box the outsider warned about, only a
  paying one. The friends meet in a ledger, not in each other's buildings. It is also the largest build.

**I champion D2.** It is the only direction that answers all four things the friends did in run 1:
- they split roles: the land and kits make the roles true;
- they tried to send: Send;
- they helped each other debug: Sketch;
- they wanted to build together: F4's project with pledges.

D3's best part, a consumer that runs while everyone sleeps, is what D2's Sends already are, from friend to friend.

## My revised proposal

**Needs and gifts**, on top of F0–F11:

1. **Land dealt per station, no copies** (a-economy), for new worlds. It ships only after F2's gate button and the
   tinted pipe groups pass their replays.
2. **Kits make the seat's own land good**, already piped. There is no ring of kit consumers.
3. **One price, 1.3,** with the economy designer's House ladder. Light-profile bots confirm it.
4. **Send ▸**, a named priority in the Yard.
   - **What it is:** a giver-chosen contract step (`Yards.cs` 459–472) with no rate and no penalty.
   - **Where you find it:** the producing building's panel, and the bid in the Next steps.
   - **Its limits:**
     - it phases in against a usual buyer at 10% a week;
     - it is capped by what the receiver really uses (F1);
     - it stops after an hour's notice;
     - a sleeper's Send runs on under the stand-in.
   - **What both see:** "runs on Ben's sand 💛" for the receiver, Thanks for the giver, and Let it go for a promise.
5. **Sketch**, built by the owner with one tap. Plus crew research at 75% off.
6. **Mia:** the porch in small mode, a personal invite that beats a closed porch, and "Today here".
7. **The project:** F4 as decided, plus pledges ("I bring it") on its goods. The Works stays a round-C candidate for
   later stages, only at a feeder share no higher than the one price.
8. **Run 2:** Mia twice, light-profile bots, and no waiting for triggers.

**Cost:** about 6–8 days beyond the fix plan:
- land and kits: 2–3 days;
- Send: 2 days;
- Sketch: 1–2 days;
- porch and "Today here": 1–2 days.

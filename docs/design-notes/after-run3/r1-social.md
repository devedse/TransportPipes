# Round 1 · the social designer (`social`)

## 1. What run 3 says, through the social lens

The friends met only where a shared goal named a missing piece one of them could bring.

**Togetherness hasn't moved in three runs.** Ana, Cleo and Dev rated "together" 2.95, 2.84 and 2.79 on average in
runs 1–3 (diary front matter); days at "together 2" went 6 → 6 → 9. Run 3 made gifts visible (3 Thanks, 2 Tends; run 2:
none), but the highs and lows didn't come from trade (0.7% of the crew's output).
- The one 4 is Cleo's day 14: Ana's stone ran the Brickworks Cleo built for the crew's Dynamo (cleo d14 #341–#368).
- Ana's 2s fall on days a shared goal stood still: d08 (wood), d21 (the Dynamo), d27 (water).
- After day 10 the project took nothing from anyone (projectGold flat to day 36).

**They tried to give, and the game had no verb for it.** *"Please send me 100 bricks for the Big Factory?"* /
*"I don't know how to hand you 100 bricks directly"* (group d21); *"how do I hand ingots to the Dynamo?"* (Dev); four
offers of gold; six promised visits to Ben. In 755 acting commands the game's own social acts were:
- 6 Sends, all to a project;
- 3 Thanks and 2 Tends;
- no Ask, Visit, Like, Link or pin, and nobody opened the Crew panel.

Ask and Visit exist but never reach the moment of need. Send is a standing priority on a flow that already runs:
Ana's *"Make it yours: Send ▸"* stayed unpressed (ana d21 #260), then her new Warehouse silently took Cleo's stone
(55.8 → 0.8 a minute, day 22).

**The door and the neighbours.** Ben was lost in 33 seconds, and nothing brought him back:
- no "a friend invited me" on the start screen, then *"Leave your crew first"*;
- three return cards silent about Ana's seat;
- texts that put friends in the wrong place (*"Ben Co moved into your district"*; *"Old Forest Co. moved in next
  door"* on Ben's card).

His crew of one also held three seats. That made Capital W "next door to a crew" for Mia-B: one sleeper and three
Houses, split on day 18. Run 3 never tested a stranger beside a crew that plays.

A real Ben would likely have quit at the toast, and real friends would pass gold through alts if they could: the
missing verb matters more for them.

## 2. The table

| # | Improvement (what changes, concretely) | Fix or redesign | Answers (findings, evidence) | Effort | Priority |
| --- | --- | --- | --- | --- | --- |
| 1 | **Bring the friend in, and keep the door open.** The start screen opens with *"A friend sent you a link? Open it · or type the code"*. "Start with friends" becomes "Start a crew and invite friends", with one name field (emblem and colours later). A company that opened a friend's seat code and doesn't live in that seat sees this on every return until it answers: *"Ana keeps a seat for you in Capital SE (you live in Capital SW) · Join and move, free, 1 min · Not with them"*. The inviter's card and Crew panel show *"Ben opened your link but lives in Capital SW · Send the link again · 👁 Visit"*. Texts place friends right: *"moved into Capital SW (another station)"*, and "moved in next door" only within the same station. "Send to Ben" becomes "Copy Ben's link". | Fix | *A friend who started alone stays lost* (high); row-1 promise missed. Ben: the form (ben d01 #9–#16, *"I wanted to play, not design a flag"*), then *"Leave your crew first."* (#29). Three cards with no word of Ana's seat (ben d01-2300 #36, d02-0800 #57, d03-2000 #77) while he asked *"which station am i in?"* twice (group d01 22:58, d02 07:58). His diary: he'd *"try once more, but only if joining her is easy and I'm told in one line"*. Ana: STUCK (ana d01 #68) and *"it says Ben Co already moved into my district"* (group d01 20:10, from GameWorld.cs:411). On day 2 she gave up: *"no worries, just keep building, I'll visit you"*. Ben's d36 card: *"Old Forest Co. moved in next door"* (GameViews.Return.cs:66). | 2 d · risk: a stale invite nags someone who chose to live elsewhere (one "Not with them" ends it) | now |
| 2 | **Unfreeze the shared build, and say whose move it is.** Pass a keystone over after 2 days with no *new* delivery, and don't count the Houses' quarter as delivery. The This week line names the wait and the one way through: *"Workshop 96% · 9 days on water · you can: a Water Pump beside your pond · Show me"*. Cards say when a keystone was passed over, and why. | Fix | *The shared project froze* (high) and the keystone bug. Both frozen keystones sit at the Houses' 25% cap: NW water 1,855 / 7,200 and SE 1,990 / 7,200 (`houseShare` in GameWorld.Projects.cs:586–596 against `Delivered < 1e-6` in `UpdateBaskets`). SE's quarter probably came from the House warming Ben's empty seat (Capital Waterworks S, 57 spare water, ana d01 #65). Project gold stays flat from day 9.85 to day 36 for all three. Ana rallied the crew alone (group d27 ×2, d36 ×2; together 2 on d27). The rule and its text belong to the other two lenses; it's here because the build was where the crew met. | 1 d · risk: a keystone a friend was about to make gets passed over (the line says so, and it can come back) | now |
| 3 | **Ask and answer: a gift is a hand-over for a named need.**<br>• *Where asks come from:* the game raises one where the need is. A placement short of materials: *"You need 100 bricks"* becomes *"Ask your crew for 100 bricks · Cleo makes them · a House brings the rest in 12 h"*. Also the project's keystone and your crew's share of the Era wonder.<br>• *Who sees it:* each crew mate or station neighbour who can meet it, once, on the return card and in the tracker: *"Ana asks for 100 bricks for her Big Factory · you hold 500 · Send 100 ▸"*.<br>• *The answer:* one tap hands the goods over from storage or production, straight into the build. A crate in the giver's colours crosses the Yard, both ends are named, and 💛 Thanks answers it.<br>• *Paying for a friend:* "Pay for it ▸" buys the friend's need from the station's House at 2.0 × base out of your bank. The gold leaves the game, and nothing passes between players.<br>• *Limits:* a House closes every ask at its deadline. At most 3 open asks a player. Game-raised asks go only to the crew and the station.<br>• *Send* stays as a "keep them first" toggle on a running flow. | **Redesign** (the social verb) | Three frictions: *the Era wonder*, *the Big Factory hides its 100 bricks* and *gold … can't help a friend*. Ana, on day 21: *"please send me 100 bricks for the Big Factory?"* (group 19:13; STUCK at ana d21 #285). Cleo's answer: *"I don't know how to hand you 100 bricks directly"* (group 19:17); her 500 bricks were never delivered (cleo d21 #422–#423). Dev: *"how do I hand ingots to the Dynamo?"* (group d21 19:22; Deliver disabled at dev d21 #178). Gold offered four times: *"fund whatever the tower needs"*, *"I can pay for anything"*, *"9.9M idle, can pay for upgrades"* (Cleo, d02, d06, d14) and *"19M gold, say if you need money"* (Ana, d21). 6 Sends, none to a friend. 0 Asks in three runs: the form is under the Plan board (PlanBoard.tsx:66). | 5 d · the Ask engine, deadlines and House fill exist (GameWorld.Asks.cs). New: the answer from storage, game-raised asks, Pay for it, the cards, a crate sprite and e2e `ask.mjs`. Risk: ask spam and alt funnels. The cap holds the spam; the funnels are held because goods go straight into the build, Pay for it is blocked between same-device accounts, and units into storage pay the seller's hub price. | next |
| 4 | **The crew's plan in sight, and its end said out loud.** The Era wonder sits in the tracker from the day it starts, with the crew's part: *"Great Dynamo · bricks 0/1,800 · Cleo holds 500 · ingots: Dev 6/min · 8 days left"*. Ticking "for the Dynamo" on a flow sends it by itself, with no Deliver button. A crew-sized tier (Bronze) the crew can see it will reach or miss. Every card says how it ended: *"Era 1 ended: the Great Dynamo got 0 of 1,800 · Teapot Four gave 0 · Era 2 began anyway"*. | Fix | *The crew's own plan was out of reach and out of sight* (high). Cleo found it by hunting (cleo d10 #291–#293). The run's only plan was made in the group (d14 19:06, 19:13). Ana: *"nothing told me that earlier"* (ana d21 #271) and *"Era 1 ended without me knowing the Dynamo result"* (ana d27 #318). Dev: *"I feel late and useless"* (d21). The end's only event was *"Era 2 begins!"* (day 22 19:24). | 2 d · risk: a tier a crew can reach without making the Wonder trivial | next |
| 5 | **A running gift stays running, or both ends hear why.** A neighbour's running trade (a usual partner) comes before the giver's own Storage or Warehouse keeping the same good, unless the giver chooses otherwise on the Storage (*"Keep stone here before Cleo's Brickworks"*). When a flow between two players falls by half or more, both next cards say so, with the cause and one button: *"Cleo's Brickworks gets 1 stone a minute now (was 56): your new Warehouse keeps it · Let Cleo have it first ▸"*. The card's gift totals come from the books. | Fix | *A Storage stops sales silently* ("tell both sides when a friend's supply drops") and the gift-total bug. Cleo's stone import fell from 55.8 to 0.8 a minute at day 22.06, right after ana d21 #296–#302 (Storage Level 2, then Evolve into Warehouse), and neither was told. Ana's d27 card still said *"Cleo's Brickworks bought 44.3k of your stone · +86.5k g"* (ana d27 #317), against about 14k paid. A near miss on the harm mark (Cleo −8%). | 1.5 d · risk: hub-order Storages fill slower (the Storage panel says so) | next |
| 6 | **A table for the station: Station and Crew chat channels.** Beside the district channel, add a Station channel (everyone who shares the Yard, the stranger next door included) and a Crew channel. The game posts the station's own news there once: a gift begins or ends, a Thanks, an ask, a stage that waits or stands (*"Capital SE's Workshop waits on water: Ana can make it"*). Every project name carries its station everywhere. | Fix | *Four Water Towers in one chat* (medium): mia d02 #65, mia2 d02 #73, d08 #105, #122. Cleo's thanks to her crew landed in Mia's Capital chat (mia d15 shot 0138). Dev: *"Chat in Capital is full of other Mias … unclear who my friends are"* (dev d08 #20). Mia-B: *"Ana talking as if I were in it"* (mia2 d02 #93). Ben: *"nothing from my friends there"* (d01-2300). Today's channels are district, corporation and direct (GameHub.cs:791–807). | 2 d · risk: one more tab on a phone; moderation of a new channel kind (same filters) | next |
| 7 | **The friend outside your station: Visit and Link by name.** The Crew panel and the Links tab list friends who live elsewhere (anyone who opened your seat link, and crew mates in other stations), each with their station, 👁 Visit (the Visit panel's Look, Like and guestbook already exist) and, when one suits, *"Link to Ben ▸"* with what it would carry. A visit leaves one line on the friend's next card: *"Ana visited your estate 💛"*. | Fix | Ana promised Ben a visit six times (group d02 20:05, 20:25; d04 21:03; d08 20:05; d14 19:06; d36 20:04) and asked *"where do you live?"* (d21 19:13; he wasn't in her list, ana d21 #306). Ben asked her to *"come visit"* twice (d03, d36). The Links tab offered *"Ben Co (Capital SW) has 39 energy/min spare … Link"* without saying he was her friend (ana d36 #366). 0 links in three runs. | 1 d · risk: low | next |
| 8 | **Seat the stranger beside a crew that plays.** "Next door to a crew" counts residents who played in the last 3 days, not held seats (`CrewSeats` and `CrewCornerNextTo` in GameWorld.Crews.cs:523–548, used by `SuggestSeat`). A small station that leaves a shared Yard keeps the stages it stood with. When its partner station goes to sleep, the stranger's card names the livelier station before the split. | Fix | The *Capital W restart* bug. Mia-B was seated beside "Ana and Ben": a crew of one, plus three held seats warmed by Houses (mia2 d01 #10–#15; her Yard in shot mia2 d15-0179 shows Ben resting and three Houses). The holds lapsed 14 days after Ben's last session. Then came *"Nobody else here has played lately"* (day 17 03:11), and Capital W's own project restarted from the Foundation (day 18 03:10). Run 3 never tested a stranger beside a crew that plays; run 2 did once, and *"nobody read her as a neighbour either"* (story2 findings). | 1 d · risk: fewer strangers seated next door on a small server | now |
| 9 | **Send says what it moves, or why it moves nothing.** The project's Send checkbox and every Send line show what moved this week. When nothing moved, they give the cause in one line, with its fix: "your own buildings use it all" or "your pump's pipe doesn't reach your gate · Lay it". *"Make it yours: Send ▸"* says what the tap adds: *"Cleo stays first when your stone runs short"*. | Fix | *Send buttons that do nothing visible* (low). Mia's water Send to the Water Tower moved nothing for 27 days: water 1,855 / 7,200 from day 8.85 to day 36 with `water>project` set (mia d08 #103; STUCK at mia d15 #141, *"nothing tells me whether the water is on its way"*). Ben: *"i helped the team without understanding it"* (ben d02-0800 #72). Ana's two Send offers went unpressed (ana d21 #260, #306). | 0.5–1 d · risk: low | next |
| 10 | **Catch the skimmer up: one crew line on the return card.** Built from the crew's state, not its chat: *"Your crew: Cleo bakes bricks for the Dynamo · Ana asks for 100 bricks · the Workshop waits on water · you could: Send ▸"* (or nothing, when he can't help). | Fix | Ben, day 36: *"The group chat is a wall of 30+ messages about bricks, Dynamo, Workshop, water; I understood little"*, and *"the group's projects feel far ahead and I'm lost on them"*. Days 1–3: *"what should i build for sand?"* (group d02 07:58). | 1 d · risk: a long line on a phone (one line, cut by priority) | later |
| 11 | **(Held back) A keystone that needs two friends.** From the Water Tower on, the keystone is a processed good whose two land inputs lie on two seats: bricks (stone + coal) or copper ingots (copper + coal). Dealt land never puts both on one seat. The card names both friends: *"Workshop wants bricks · Ana's stone + Cleo's coal · a Brickworks on either estate"*. An away seat's input comes from the station's House at 1.4. This replaces F4's keystone: "a good nobody makes that one resident can make alone". | Redesign (held back) | The run's only "together 4" had exactly this shape (cleo d14). The water keystone needed no friend: Ana stood alone at the pond (ana d27 #348) while everyone had a pond. | 3 d · risk: slower stages; two people per stage | later, only if run 4's asks go unanswered |

## 3. Fixes or another redesign?

**A mix: fix the door and the goals, and redesign the gift (row 3).**

**Keep the world.** Dealt land, the Yard and the one price worked the one time they were used: Cleo read *"Ana Pipes
makes it"* and built a Brickworks on her friend's stone. Rows 1, 2, 4, 5, 7 and 8 are fixes.

**Redesign what a gift is.**
- Run 1 had no gift.
- Run 2's gift was the automatic flow, and nobody felt it.
- Run 3 named that flow and added Send, a standing priority on it. Announced gifts were thanked, but no Send went to a
  friend and "together" stayed flat.

What the friends reached for was a one-off hand-over of a quantity, for a named reason: 100 bricks for a Big Factory,
ingots for the Dynamo, water for the Workshop, money for an upgrade.

**So: Ask and answer.** The game raises a need where it happens: a material cost, the keystone, the crew's share of
the Wonder.
- It shows to the friends who can meet it. One tap hands it over from storage or production, straight into the
  build, and both ends are named, with Thanks.
- Anyone may pay a House to fill a friend's need from their bank. Gold leaves the game, nothing passes between
  players, and the idle millions finally help someone.
- A House closes every ask at its deadline, so nobody waits on a friend.

It replaces the Plan board's Ask form (0 uses in three runs), Send as the gift verb (it stays as a toggle) and the
Wonder's Deliver button. Every money guard stays. About 5 days on top of the fixes, plus a crate sprite; no new
world.

**Run 4 shows it worked if:**
- 3 or more asks between crew mates are answered and thanked by day 21;
- Ana's Big Factory stands within a session of her ask;
- someone pays for a friend's need;
- no diary says "I don't know how to hand X";
- "together" for Ana, Cleo and Dev averages 3.2 or more.

**What would change my mind:** if friends see asks and the Houses still close more than two thirds of them, what's
missing is the reason to help, not the verb. Then the lever moves into the build: a keystone that needs two friends'
land (row 11).

## 4. Run 4: the marks I'd watch

1. **The door.** Ben lives in Ana's station by his second session, or has pressed "Not with them". Nobody asks "which
   station am I in?". Keep his 33-second race in the story, because it's the realistic case.
2. **Hand-overs.** 3 or more answered asks between crew mates by day 21, named at both ends, with at least one Thanks.
   At least one need paid for a friend from the bank. Ana's 100 bricks arrive within one session.
3. **The shared build moves and ends out loud.** No stage waits more than 2 days while a resident plays. The crew's
   Wonder share is in the tracker from day 10. Its end, and the crew's part in it, is on every card.
4. **Felt togetherness.** "Together" for Ana, Cleo and Dev averages 3.2 or more (runs 1–3: 2.95, 2.84, 2.79). No
   "together 2" whose reason is a frozen goal, a lost friend or a gift the game can't make.
5. **The stranger beside a crew that plays.** Mia-B shares a Yard with the active crew. By day 8 there is one act
   between them: an answered ask, a Thanks or a reply in the Station channel. Her "alive" is 4 or more twice, and not
   from her twin.

**One mark to retire as the social headline:** "paid trade between humans ≥ 15%". Three runs came in at 0–2.8%, and
the felt peak (Cleo's 4) came at a crew share of 1.3%. Keep it as a systems number; measure togetherness by acts
and diaries.

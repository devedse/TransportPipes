# Round 1 · the player researcher (`player`)

What players saw, understood and felt in run 3 (`playtest/runs/story3`). Sessions and action numbers as in the
digest ("ana d27-2000 #341"); "card" is the While-you-were-away card; the diary means are my own count over all
sessions with a diary (runs 1–3).

## 1 · What run 3 says, through the player's eyes

**The first fortnight is the best of three runs; the last three weeks are the same as ever.** Days 1–4: clarity and
fun 3.79, "come back tomorrow" yes 12 of 14 (run 1: 3.25, 8 of 12; run 2: 3.36, 8 of 14). From day 18: fun 3.0, yes
1 of 6 (run 1: 3.0, 1 of 6; run 2: 3.17, 2 of 6). Each fix round moved the early game, none the late one: every run
brings new dead ends (run 2: unfed Labs; run 3: water, forest, bricks). They came back for "the friends need my
stone" (ana d14) and "a concrete shared goal" (cleo d14), never for gold after week 1 ("6.6M, still pointless", mia
d15 #139).

**The game said things its own rules didn't keep.** Seven of ten STUCK incidents are a suggestion or claim the game
made and its rules refused: "forest" parcels with no wood (cleo d06 #244), a "12k g" Big Factory that needs 100
bricks (ana d21 #286), "a Water Pump by the water" then "Not on water" (ana d27 #348), a Copper Pipe without copper
(cleo d21 #404), "pick one!" with nothing to pick (cleo d10 #268), "spare" energy that can't arrive (ana d14 #253),
Mia's water. The one text computed from the action's own rule, the Build menu's supply line, made the best moment
(cleo d14 #342–#366). Ana's and Cleo's clarity fell from 4 to 3 after Loady's first chapter ended, and stayed there.

**The shared goal's own line closed the door.** The project's line was on 18 of 23 readable cards and never named a
step the reader could take that worked. For 27 days it told the crew "Workshop 96% · wants water (nobody here yet)",
though all six estates had been told "Surveyors found a pond on your estate!" Capital NW's named maker, Mia, did all
she was asked; her pump sat idle without energy from day 8 20:30 to the end (metrics `idle`), after the game's own
"Unstick" pipe (mia d08 #96), and no card said so (mia d15 #125, STUCK #141). One pump fills that tower in about 4.5
hours.

**Chores:** one-tap upkeep was praised ("a real rescue"). What wore people down was attention without return: the
same unfixable line, return after return (Cleo's full Storage on 5 of 5 cards; Ana's factory at 55, 59, 50, 40% on
four).

## 2 · The table

| # | Improvement (what changes, concretely) | Fix or redesign | Answers (findings, evidence) | Effort | Priority |
| --- | --- | --- | --- | --- | --- |
| 1 | **The project's line tells the truth and gives the way through.** One sentence, the same on the This week line, the card and the Project tab, built from the build command's placement rule: what the stage waits on, for how long, who on this station can make it and how, and one button: "Workshop 96% · 9 days waiting on water · you can: a Water Pump beside your pond, 300 g · Show me". Never "(nobody here yet)" when a resident's land allows it; when nobody's does, say what happens next ("passed over on Friday"). When the reader's own building makes the waited-for good and isn't running, that leads the card: "Your Water Pump is idle (no energy): the Water Tower waits on its water · Show me". When a good stops mattering, say so once ("Coal is no longer needed: the Water Tower waits on planks now"). Beside the systems designer's pass-over fix. | Fix | Frozen project (friction 1, bug "This week hides you can"). "(nobody here yet)" on 6 crew cards days 14–36 (ana d21 #260, d27 #317, d36 #357; cleo d14 #313, d21 #391; dev d14 #105) while every estate had the pond event (events, days 1 and 8); "i dont have water either" (ben, group day 36). Mia's pump idle "InputMissing energy" day 8 20:30 → 36 (metrics), her card silent on it (mia d15 #125), STUCK #141; "nothing told me it no longer matters" (mia d08 diary). | 1.5–2 d; risk: line length on a phone | Now |
| 2 | **One rule for "can make", and refusals that teach.** Keystone makers, the parcel step and the Build menu use the build command's own check (`FitsOnEstate` tests forest terrain; a Lumber Camp on dealt land needs the old forest's marked trees). Parcels are named by what a building can use there ("wood: marked trees"), not by terrain. A refusal names the rule and points: "A pump stands beside the water, not on it", and the ghost hops to the nearest tile that works; "Buy Parcel 4 first (40k), or here on your land". A building in hand rims every tile where it may stand, on a phone too. | Fix | Texts send players after goods their land can't make; the "can make" bug. Cleo's two "forest" parcels, 30k, two STUCKs (cleo d06 #220–#244); "you can" wood for Ben (ben d01-2300 #34) and Ana and Cleo (dev d08 #12); "Not on water" ×3, then 9 days (ana d27 #341–#348, STUCK); no tiles for a Lumber Camp in hand (mia2 d08 #132–#134, STUCK); Show me's tile on land not bought (ana d21 #278–#285). | 2 d (rule 0.5, refusals 0.5, rim 1); risk: rim cost on big estates (once per tool change) | Now |
| 3 | **Next steps and fix buttons offer only what will work.** Each step and fix is dry-run through the same command check and the network what-if before it shows. A step names every cost ("Big Factory · 12k g + 100 bricks · Cleo makes bricks") or waits until it can be met. A fix shows its after-state and isn't offered if it leaves the building idle (Mia's "Unstick your Water Pump · 8 g" left it without energy for four weeks). Copper Pipe is disabled with its reason ("needs 28 copper ingots: nobody here makes them"). "pick one!" only when a tech can be picked. One gain figure per building everywhere. | Fix | Big Factory's hidden bricks (cleo d10 #280; ana d21 #286, STUCK) and its gain +80 / +124 / +57 (cleo d10 #271, #291, d14 #361); Copper Pipe no-op (cleo d21 #399–#404, STUCK); locked research (cleo d10 #268, STUCK); Mia's pump (metrics; mia d08 #96); Show me off the pipe (cleo d02 #132, ana d04 #155). | 2–3 d; risk: what-if cost per step (cache per tick); an empty list falls back to the shared goal | Now |
| 4 | **Find your friends.** The way-picker's first choice is "A friend sent me a code" (a paste field); "Start with friends" becomes "Found a crew for my friends" and asks only a crew name (emblem, banner, colours later). A seat kept for you shows on every return until you answer: "Ana keeps a seat for you in Capital SE · Move in (free)". The founder sees an unanswered seat with its friend: "Ben's seat is still empty · Ben Co lives in Capital SW · Send the link again · Visit". "Send to Ben" becomes "Copy Ben's link". | Fix | Ben founded a crew 33 s before Ana's link ("ana didnt see your link so i made a game", ben d01-2000 #5–#21); the form ("just wanted to play", #12); "which station am i in?" twice (group, day 1 22:58, day 2 07:58); ana d01 #68 (STUCK); six promised visits; "Everyone has been asking where I am for weeks" (ben d36 diary); "Send to Ben did nothing visible" (ana d01 #16–#20). | 1.5–2 d; risk: small | Now |
| 5 | **The crew's big goal on the main screen, with your part, and its end told.** The Era wonder gets a tracker line with your part and a button: "Great Dynamo · bricks 0 / 1,800 · you hold 500 · Deliver" (Deliver from storage exists in Hub › Wonders). The top bar's Era button names the goal, not only a countdown ("Era 1 · Dynamo 0% · 11 d"). When it ends, every card says how: "Era 1 ended: the Great Dynamo got 0 of 1,800 bricks; Era 2 began anyway". Its size is the systems designer's. | Fix | The Era wonder friction. The Era button did open Wonders (cleo d10 #271), but read as a clock and was missed; found again at #291–#293. Cleo's 500 bricks never delivered (cleo d21 #420, group 19:17); "ended without me knowing" (ana d27 #318); "I feel late and useless" (dev d21 diary); "a wall of 30+ messages about bricks, Dynamo… I understood little" (ben d36). | 1–1.5 d; risk: none big | Now |
| 6 | **Standing lines get a fix that works, or fold away; the card stops advertising an empty bank.** A look line that comes back unchanged after its button changed nothing folds into one "Still:" line with the real cause and a fix that works: "Your Coal Pit gets 45 of 315 energy: the grey pipe from your Moilies is full · lay a second one · 9 tiles · 90 g". The Storage line is current, its button does something (no "0 tiles · 0 g"), and its grammar is fixed ("your a Wind Moily"). The Invest line goes until something is worth the bank (not "27796.8k gold banked: Max … for 13k g"); big numbers read 27.8M. | Fix | Cleo's Storage line on 5 of 5 cards, frozen at "4,831 energy and 169 coal" (cleo d10 #263 → d21 #391; the panel said 2,942, #420): "a warning I cannot act on"; Coal Pit 11–14% on 3 cards, a new Moily and Copper Pipe changed nothing (cleo d21 #393–#418); Ana's factory 55 / 59 / 50 / 40% (ana d08 #172, d21 #260, d27 #317, d36 #357); the Invest line on 14 of 23 cards; "the numbers feel meaningless" (mia d08 diary). | 2 d (the second-pipe fix is most of it); risk: hiding a real problem (it stays in the building's panel) | Next |
| 7 | **Warn before a click costs income, and when a friend's supply drops.** Max and big upgrades preview the net change ("+67 research/min, −500 g/min: the Lab eats the ingots you sell"; "needs 237 more energy: your buildings slow to 61%"). Placing a Storage says what it holds back ("keeps up to 5,000 energy before selling: about −45 g/min until full"). Both friends hear when a flow between them falls ("Cleo's Brickworks gets 1 stone a minute now, was 56"). | Fix | The Lab Max, 783 → 285, then 173 g/min (cleo d06 #177–#191, "SURPRISE"); income falls after a Storage for three players (cleo d01 #68, ana d02 #90, dev d08 #33: "Did I break something?"); Ana's stone to Cleo 55.8 → 0.8 a minute at day 22 01:24, told to neither (metrics). | 1.5 d; risk: what-if cost of Max on big networks | Next |
| 8 | **Panels that don't cover what you're using; a phone that taps through.** Chat opens on the right; Escape closes Build and chat; on the phone the folded Next steps line is itself the button (it shows the step's Upgrade and Show me, it doesn't hide them); the top bar's left buttons fit a small phone. | Fix | Chat over the goals or map (ana d04 #140–#144, dev d08 #16, cleo d14 #350, ben d01-2300 #44); Escape (mia2 d08 #106–#113); "Upgrade Coal Pit to level 10" with nothing to tap (ben d36 #99–#105, STUCK); top bar cut (ben d01-2000 #28 shot). | 1 d; risk: small | Next |
| 9 | **Projects named by their station, and the writer's station in chat.** "Capital SE's Workshop" in every line the Capital chat can quote; each chat line carries the writer's station. | Fix | Four Water Towers: Mia, Mia-B and Ana talked about three stations' towers as one for a week (mia d02 #65, mia2 d02 #73, d08 #105, mia d08 #112); Mia-B bought a forest parcel for a tower that wanted water (mia2 d08 #122–#126). | 0.5 d; risk: longer chat lines | Next |
| 10 | **Events only when you can act on them.** The bell and the card's "Today in Capital" carry only what this estate can act on (a Shortage of a good you make, a Discovery you can claim, a storm that hit you); the rest goes to the Calendar tab, unbadged. | Fix | About 49 calendar events per player in 36 days (events list); the bell at 97 on Ben's phone (ben d36 shots); "21 bell notifications, I never look at them" (ben d03); "I can't tell what matters" (ana d36); a Coal Shortage for Mia, who makes no coal (mia d15); "Today in" on 15 of 23 cards. | 1 d; risk: a player misses an event they'd have wanted (it stays in the Calendar) | Later |
| 11 | **Let a friend's goods reach a friend's build.** "Send 100 bricks to Ana's Storage" from your Storage panel, or a building's materials drawn from a crewmate's Storage with their OK. Players wanted it and asked for it; the social designer sizes it. | Fix | "I don't know how to hand you 100 bricks directly" (cleo, group day 21 19:17); Ana's request for 100 bricks for her Big Factory (ana d21 #274, #290); gold offered with nowhere to go ("I can pay for anything", cleo group day 6; "19M, say if you need money", ana group day 21). | 1.5–2 d; risk: it bypasses the Yard's one price | Later |

### Where I read the evidence differently, and what real people would do

- **"Not on water" weighs less than the analyst gives it; "(nobody here yet)" more.** Ana's AI aimed by tile numbers
  from a text map. A person with a mouse sees the ghost red on the pond and green on the shore within seconds (on a
  phone it takes a few taps). What cost the crew 27 days is the line that told three active players from day 9 to
  day 21 that none of them could make water, so none tried.
- **Capital NW's freeze is two things.** The analyst puts it on the pass-over rule; the metrics also show its only
  named maker's pump idle without energy for 28 days after the game's own fix pipe.
- **The Era wonder was one tap away.** The top bar's Era button opened Hub › Wonders (cleo d10 #271); the agent missed
  it. A person would likely find it, and still wouldn't know the crew should care: it reads as a clock.
- **AI players came back because the story said so.** From day 18 their own answer was "maybe" five times in six. Real
  people answer that question by not opening the game. Real skimmers read less than Ben's agent; for them a line
  without a button does nothing, which is why rows 1, 3 and 6 put a button on every line.
- **What works and must stay:** one card and one Continue on every return (26 of 26), the one-tap pipe fixes (nine,
  "a real rescue", "no thinking needed"), Show me's golden tile and arrow, the Build menu's supply line, gifts named at
  both ends with 💛 Thanks, and Ben's return card naming his stand-in (ben d36 #89). The rows above add to these
  screens; none replaces them.
- **Would a real person have stayed?** Week 1: yes, mostly (the look, Loady, the away gold). Week 2: an optimiser like
  Cleo runs out of research and things worth buying; a cosy player like Mia stays for the looks at "alive 3". Weeks
  3–5 in run 3's shape (a frozen project, a hopeless wonder, the same warnings): most would have stopped. A Ben, on
  his phone with his friends invisible, keeps a one-minute gold check for a week or two and forgets the game.

## 3 · Fixes or another redesign? **Fixes, at the root**

Three reasons, from what players did.

**The design's idea finally reached the screens, and players felt it.** Ana's stone ran Cleo's Brickworks, both
ends knew, and the run's first Thanks and Tends followed (cleo d14 #366–#368, ana d21 #260). It happened where the
game's words came from the game's rules (the Build menu's supply line). A redesign now throws that away before anyone
has played it without the bugs that froze it.

**Every late drop in run 3 has a named cause in its own diary, and none is structural.** Cleo's fun 2 is "my fix
attempts do nothing" (a Copper Pipe offer she couldn't pay for, a Moily that changed nothing); Ana's 3s are bricks a
step hid and "Not on water"; Dev's "late and useless" is a wonder out of reach and out of sight; Mia's water is a pump
nobody told her was dead. Each is a text, a mismatched rule, a missing line or a size.

**The late game has been flat for three runs because the dead ends come from one place.** The game suggests, labels
"you can" and offers fixes without asking the rule that will judge the click. So I don't want twelve more patches; I
want one contract (rows 1–3): nothing is suggested, called possible or offered as a fix unless the build command's own
check says yes, and every refusal names the rule and points at a tile that works. It changes no rule of the game, so
it isn't a redesign, but it removes the source rather than this run's instances.

Where the structure is honestly in question: gold has meant little after the first week in every run (run 1: "6M
banked … feel pointless"), and players tried to spend it on each other. That's the systems designer's call. My evidence says people came back for a moving shared
goal and a friend counting on them, not for gold, so I'd test the fixes first.

**What would change my mind:** if in run 4, with rows 1–5 built, the diaries from day 18 still give fun at 3 or below
and "come back" yes at 2 of 6 or fewer, and name "nothing to want" rather than dead ends, the late game needs a
redesign: a sink for gold that friends can see (funding a friend's building, or the station's stage).

## 4 · Run 4: the marks I'd watch

1. **The late game holds.** Days 18–36: mean fun 3.5 or more and "come back tomorrow" yes in at least 3 of 6 (runs
   1–3: 3.0 / 1, 3.17 / 2, 3.0 / 1). This mark decides fixes against a redesign.
2. **No suggestion the rules refuse.** No STUCK whose cause is a step, a card line, a "you can" or a fix button the
   game itself offered (run 3: 7 of 10 incidents); all STUCKs 3 or fewer.
3. **No standing line.** No warning or project line on more than two returns in a row for one player unless its
   button changed something in between (run 3: Cleo's Storage 5 of 5; "(nobody here yet)" on 6 crew cards).
4. **The shared goal moves and is known.** The crew's stage stands within two days of its last good arriving or being
   passed over; from day 14 every crew diary states the project's and the Era wonder's state correctly, and every card
   after Era 1 ends says how it ended.
5. **Friends find each other.** Replaying row 1 with Ben pressing Play now first, he is in Ana's station by his
   second session, and no diary asks where a friend lives.

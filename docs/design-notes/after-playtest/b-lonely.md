# Round B · lonely

I read the five other round-A papers in full and the fix plan again. The convergence is real: the world layer held,
the together layer failed by design, the honesty fixes ship, and friends need different needs, an act of giving and a
better price. I tested each of those against the two players I speak for: the one who plays alone, and the one who
stops.

My finding: the convergence holds for them only with **three guards nobody has written yet**, and one proposal (the
ring of kit consumers at today's House price) would tax the lonely player as written.

Prices below are × the hub's base price. Chain margins are level-1 g/min, from the economy designer's table
(a-economy, "The table that explains t = 0"): margin = own-made margin − slope × (price − 1).

## Answers to the questions put to me

**wild: is "next door counts" plus a Thanks enough for Mia's "alive", or does she need a human who answers in the
game?**
- She needs to be *answered*, and we can't design people. Her warmest line was a reply: *"Finally got replies from Ana
  and Cleo – warm feeling"* (mia d08-2000 #105). What spoiled it was that the reply couldn't be dated.
- What we can design is that answers reach her and that she is in something with them:
  - the return card *quotes* the reply ("Ana answered you on Tue: 'iron mine first!'"), where F5 only says who
    answered;
  - chat with day stamps (F10);
  - a place shared with humans (the porch, below).
- "Next door counts" plus Thanks still waits for her to act first. Being inside the crew's Yard and project makes her
  present without one.

**friends: does inviting Mia into the crew's attached small station break any veto? Should Houses' kits differ in an
all-House station?**
- It breaks none: both sides consent. On its own it repeats run 1, though. *"All run long, the crew's Links tab offered
  Mia as their only possible link partner, and nobody offered her a link"* (findings, "together"). An act a crew must
  start toward a stranger is the act that doesn't happen.
- So the porch is the default, and your invite is the upgrade: from neighbour to crew mate.
- Yes, the Houses' kits should differ. With land dealt per station (a-economy §1), the House seats hold the
  station's other goods, so their kits should make them. A solo player's project then has more than energy in it.

**economy: is one building's need per land good from Houses an acceptable ceiling for Mia?**
- Not past week 2. Your own estimate falls to about 85% by week 3 "when the cap binds on two or more chains". That is
  the floor of my veto rule, reached at the moment a solo player is growing fastest. It keeps falling as she builds.
- I ask for **two buildings' need per good, plus one per 10 of the buyer's levels**, measured with your light bots (10–15
  buildings, a session every 2 days). Pass: ≥ 85% to day 28.
- Friends still win: a friend's supply has no cap, and the House price (1.4) is above the friend price (1.3).

**psychologist: can a stranger's bid feel like pressure? Should bids between strangers show only after one
Handshake?**
- Yes, it can. A game that tells a lonely player *"Jo's mine could use your energy"*, and then shows Jo nothing back,
  makes her the one who failed a stranger.
- So bids between strangers show only after their pipes met in one Yard (the Handshake), at most once per session,
  never as the return card's first line, and never for a good the stranger would have to build something to make.
- A bid **to** the lonely player ("Ana's Glassworks could use your sand") is different. It is the best line she can
  read: someone needs her. Let those show freely.

**outsider: where should Mia live when every other human is one closed crew?**
- In the small station attached to the crew's station, joining their Yard as a resident: your #4, my porch. We wrote it
  independently.
- Would the crew accept? They lose nothing: crew first in a shortage, and their own seats stay closed. Run 1 says
  they'd welcome her: Cleo and Ana answered her on days 2 and 4, and Ana told the group *"Mia in the game chat says
  hi"* (group chat, day 21 19:08).

**everyone: should run 2 also play Mia without Jo?** Yes: three papers ask for it (mine, outsider, wild). With Jo,
run 2 tests the one world where placement works.

## What I take from the others

- **outsider #4 and wild #4: Mia next door, and her world without Jo.** Three of us converged on the small station
  beside the crew's (Capital N is block (2,0)–(2,1), Capital NW (0,0)–(1,1), `WorldLayout.cs`). I take the outsider's
  wording: "its seats join the crew's Yard as neighbours, using the crew-of-5–8 big-station pool that already exists"
  (`GameWorld.Crews.cs` 504).
- **friends #2 and psychologist §3: Send, an act that is a pipe.** A Send makes the lonely player a *giver*. Helping
  feels better than being helped (our own lesson). On the porch, Mia's Send ▸ the crew's Water Tower is her way in.
- **friends #2: "A resting giver's line keeps running, and a stand-in keeps it at its terms."** The absent friend
  keeps giving. That is the strongest new leaver line in the five papers: Ben's return card reads *"Your sand kept
  Ana's Glassworks running for 27 days"*.
- **psychologist §5: promises that can be kept, or let go.** "Let it go hands it back to the House without a word to
  anyone." This removes the source of Ben's guilt in three of five diaries.
- **psychologist §6: the automatic Yard stays, as the market, for strangers and Houses.** Not every channel can need
  an act. A lonely player at 02:40 trades with whoever exists without asking anyone.
- **economy §1 and §3: land dealt per station, and a price that pays both sides (1.3).** It raises solo parity: solo
  with Houses buying at 1.15 earns 0.75 + 0.25 × 1.15 = **1.0375 × the hub**, crewed at t = 30% earns
  1 + 0.3 × 0.3 = **1.09**, so solo is at **95%**.
- **wild §1: the Works never stalls, "a friend who rests keeps feeding it at 100%"**, and its ring shows who helped,
  never who didn't.

## What I attack

**1. The ring of kit consumers at today's House price is a tax on the lonely player** (psychologist §1). "Every consumer
always runs, from the House (2.0) or the hub (2.5) when no friend supplies it."
- At 2.0, almost every Era-1 chain loses money:

  | Chain at a House price of 2.0 | Margin |
  | --- | --- |
  | Smelter, two foreign inputs | 25 − 45 = **−20 g/min** |
  | Small Factory | 28 − 36 = **−8** |
  | Glassworks | 27 − 18 = +9 |

  Only the Glassworks pays.
- For Mia in a station of Houses, her kit consumer would run every minute and lose money she can't see.
- The fix:
  - take economy's House price for land goods, 1.4: the Smelter is then 25 − 18 = **+7**;
  - and add a rule: a building fed only by an NPC never runs below zero margin. It pauses with one line ("waits for
    copper: Cleo makes it, Bertha ⚙ sells it at a loss for you").

**2. Land with no copies, plus my held seats, lets one leaver remove a good from a whole station for months.**
- With economy §1 a station holds each land good on one seat.
- If Ben (the station's only sand) stops on day 3, his seat is held in place to day 90, and to day 150 if a friend
  keeps it (`GameWorld.Return.cs` 28, "60 days later when a friend kept it"). The stand-in sells his sand at the old
  price for 14 days, then 10% less each week (`GameWorld.StandIns.cs` 22–31).
- From about day 44 the whole station's sand shrinks toward the House cap. Nobody can take the seat, because it is
  held.
- That breaks my own rule "a leaver blocking a seat … for longer than the friends choose", in a new form: the leaver
  blocks a *good*.
- Guard: **while a seat is held for a sleeper, its land good is sold by the stand-in at 1.4 with no cap**. Players pay
  for it, so it is a sink, not a faucet.

**3. A Send that ranks before the Yard can starve the porch's stranger overnight** (friends #2: "a line ranks after
contracts and before Asks and the Yard").
- Say Mia's Smelter has bought Cleo's copper for a week as a usual buyer. Then Cleo taps Send ▸ Dev's Smelter. The
  line takes the copper first, and Mia's supply drops to zero without notice.
- That is the attack C made on B's Main in round 3, now aimed at the stranger.
- Guard: a new line takes only spare that isn't a usual buyer's 7-day share, or shrinks that share by at most 10% a
  week, with the 1-hour notice and the tank. Friends come first for *new* spare, never by cutting someone's running
  supply.

**4. Invitation alone repeats run 1.** Friends #4 (invite the neighbour) and the psychologist's Jo answer (*"Seated
beside Jo (F7, run 2)"*) both need a human to act toward Mia, or a human to exist. The run's evidence is that nobody
acted in 36 days. The lonely player needs a default that works with nobody acting.

**5. Specialty kits and Send without economy's land change carry little after week 1.**
- Economy's table: own mine plus factory make +54 g/min for 2,100 g, against +10 at 1.5 for a neighbour-fed factory.
  Every estate keeps ★2 copper, coal and stone (`GameWorld.Survey.cs` 5–13).
- Friends, wild, outsider and psychologist all keep the patches. Their Send will mostly carry energy by Level 5, which
  every friend has.
- So directions that give acts without needs pass run 2's "first gift by day 2" and still drift back to side by side
  by week 2. From my seat that isn't harmful, but it isn't the answer either.

**6. The Works' daily share line can turn absence into a public number** (wild §1: "Ana's energy 61%, Ben's sand
39%"). When the stand-in glides, it reads "Granny Ida ⚙ 39%, falling" on every screen. Guard: a sleeper's share shows
under his name, "resting", until he wakes or the hold ends.

**7. Nothing in five papers covers the very first player on an empty server.** The porch needs a crew; the Works and
Sends need neighbours. What remains for her is Houses that buy (F1.5), a project sized for one (F4), and a world that
shows life. The design's lit windows, daily line and usual hours were never built, and no other paper picked them up.

## The directions

**1 · Acts on the honest Yard** (friends, psychologist, outsider)
- **Core:** kits that keep the seat's promise; Send, a directed gift by pipe; Sketch and help on a friend's estate;
  "Let it go" for promises; the Yard stays as the market; price 1.5.
- **Replaces:** F6's announcement-only design and the plan's two §7 triggers ("giving needs an act", "kits get one
  consumer").
- **Biggest risk:** the needs vanish by Level 5, because every estate keeps ★2 copies (attack 5). The acts end up
  carrying energy.

**2 · Land that needs a neighbour** (economy, with direction 1's Send)
- **Core:** the five land goods dealt per station, no copies, kits that make the seat's good, one price of 1.3, Houses
  selling land goods at 1.4 with a cap.
- **Replaces:** the price vote, the conditional levers, and §14's surveyor patches.
- **Biggest risk:** the two players I speak for. A capped House supply taxes the solo player as she grows (85% by week
  3). A held seat can take a good out of the station for months (attack 2).

**3 · The Works** (wild)
- **Core:** a station-owned machine fed by pipes that shares 80% of its value added; Send-to; hands; next door counts.
- **Replaces:** the project's baskets, bonuses and F4's sizing rules.
- **Biggest risk:** the largest build (+5–7 days) and a new economic object. It still needs direction 1's or 2's kits
  to have inputs. Without them, the Works is a basket that runs.

**The one I'd champion: direction 2 with direction 1's Send, under three guards.**
- The run shows the lonely player wasn't hurt by needing nobody: she was hurt by being *alone*. The friends weren't
  hurt by depending on each other: they were hurt because they couldn't.
- Direction 1 is the safest from my seat and probably drifts back to side by side. Direction 2 is the only one that
  makes a neighbour's goods the cheapest way to grow. Its risks land on my two players, and both can be guarded with
  numbers. The Works can come later, on land that feeds it.
- **The porch goes in whichever direction wins.**

## My revised proposal

1. **The porch, as the default for strangers in small mode.** When a district's only lit humans are a crew in a
   closed station, "Find me neighbours" seats a stranger in the small station attached to it.
   - Her seats join the crew's Yard pool and project as a resident: crew first for new spare, usual buyers protected.
   - Any active crew mate can close the porch to *future* strangers. Nobody living there is moved or cut off.
   - The crew's invite (friends #4) turns a porch neighbour into a crew mate.
   - **Cost:** S–M (`JoinedTo` for strangers, `SuggestSeat`).
2. **The solo floor in direction 2.** Houses sell each land good for **two buildings' need per buyer, plus one per 10
   of the buyer's levels**, at 1.4. A building fed only by an NPC never runs at a loss: it pauses, with its reason.
   - **Pass:** light-bot solo parity ≥ 85% to day 28.
3. **The leaver guard in direction 2.** While a seat is held for a sleeper, its stand-in sells the seat's land good at
   1.4 with no cap. The good never leaves the station while friends keep the seat.
4. **Sends don't cut running supply.**
   - A new line takes new spare first. A usual buyer's share shrinks at most 10% a week, with notice and the tank.
   - A resting giver's line keeps running, and the stand-in keeps it.
   - The return card says *"Your sand kept Ana's Glassworks running for N days"*.
5. **The lonely player as giver and as needed.**
   - On the porch she can Send to the crew's buildings and project.
   - Bids **to** her show freely. Bids **from** strangers show only after a Handshake, once per session.
   - Her return card quotes the reply that answered her.
6. **Visible life where people look.** The "Today here" line from my round A:
   - three public events from the district;
   - shown on the first screen, the return card and the station cards;
   - never trades, gold or "last seen".

   With dated chat, it is the first player's only company, and it is cheap.
7. **Run 2 plays Mia twice,** with Jo and without. Without Jo, she passes if:
   - she is seated within 3 minutes;
   - she shares a Yard or project with humans by day 1 20:30;
   - her diary names a human who did something;
   - "alive" is ≥ 4 twice.

**What I keep from my round A unchanged:**
- F1's floor as a standing veto rule: nobody's action lowers your income, present or away.
- The tend that repairs, naming who tended.
- The way back as built.

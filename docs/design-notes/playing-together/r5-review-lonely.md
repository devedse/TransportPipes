# Round 5 · Final review of `docs/multiplayer-design.html` (v1.2): lonely players, leavers, returners

Reviewer: the guardian of the lonely and the leavers. I judged the text only: section numbers are the document's,
and quotes are verbatim.

**Verdict:**
- As written, the design **fails gate G4 (absence costs nobody)** and **three of the eight core tests** (T2, T3, T15).
- Every failure comes from a few sentences that contradict each other or that two programmers would build
  differently.
- The fixes are text, not redesign: §3 gives a ranked list of 22 items, each with exact replacement text.

---

## 1 · The veto rules and the core tests against this text

### The six gates

| Gate | Result | The sentence that decides it |
| --- | --- | --- |
| **G1 · No dead pipe** | pass | §10: "Every link has a 2-hour tank at the buyer's gate, a 1-hour notice for pauses and price changes, a fallback (the Yard, then the hub) and one state line, so no pipe ever ends in silence." §13: "Sleep() hands trades over instead of ending them". |
| **G2 · Solo is a full game** | pass on income, but taxed | §11: "The gap is about 10–15% (all three exams measured 0.85–0.89)". **But** §15 sizes the Works basket with "Houses 0.5", so a solo player with three Houses fills a basket sized for 2.5 people (item 1). |
| **G3 · Nothing needs two online** | pass | §1: "offers wait 72 h, deadlines at least 12 h"; §15: "Ask for… a good (12–48 h)"; §16: Discoveries "announced 6 h ahead, lasting at least 7 days". |
| **G4 · Absence costs nobody** | **FAIL** | (1) §13: "Goals count only people who delivered or traded in the last 3 days, so a sleeping friend shrinks the next basket". The same table says a resting estate "runs at 100%" and keeps trading through the Yard, so the friend still "traded" and the basket does **not** shrink for up to 27 days (item 1). (2) §12 "a successor House takes over its trades after a 7-day notice" against §13 "on day 30 … its seat reserved 7 days, then a successor House takes the seat with the trades". Under the second reading the trades stop for 7 days, and a partner who sold 30% of her output to the sleeper loses 0.3 × 0.75 / 1.225 = **18% on day 30** (item 3). |
| **G5 · NPCs are honest** | pass | §12: "Never on a leaderboard, never more than 25% of any shared goal, at most one letter digest a day." Works level = "the median level of the station's humans − 3". |
| **G6 · Nudges vanish** | pass | §11: "The Hub Order ×2 with 6–11 lit seats in the district, ×4 with 5 or fewer". ("Lit" is defined two ways, see item 14, but either way it holds on a server of 5.) |

### The eight core tests

| Test | Result | Why (quote) |
| --- | --- | --- |
| T1 · Sleeping partner | pass **only under one reading** | The successor sells "100% of the leaver's 14-day median at 2.0", so +14% cost (passes). Under the §13 reading ("seat reserved 7 days, then a successor House takes the seat with the trades"), Ana buys at the hub's 2.5 for days 30–37: +43%, a fail. Item 3 |
| T2 · Last friend standing | **FAIL** | **T2.2:** the old price holds only "for a crew member with 10+ distinct days of play". Friends who quit in week 1–2 mostly miss that, so their trades go to NPC terms at once. Ana's income: 0.7 + 0.3 × 1.75 = 1.225 before, 0.75 + 0.25 × 1.25 = 1.0625 after (86.7%), averaging **≈ 88.9% over days 30–60** (the bar is 90%). **T2.5:** baskets keep counting the friends whose estates still trade. Items 1 and 5 |
| T3 · Case (a) | **FAIL** | **T3.5 [hard]:** §20 says "the Works baskets shrink once Ben stops delivering". §15's rule says "delivered **or traded**", and Ben's estate trades at 100% until day 33. Two programmers build two different games. Item 1 |
| T5 · Solo on a server of 5 | partial | **T5.5:** only the Era Wonder is "sized to active humans" (§16); the District Wonder isn't mentioned. Houses 0.5 inflate the solo basket. Items 1 and 15 |
| T7 · Week-3 newcomer | pass | §14 placement, Fair-only protection, Hosts gated "only once the newcomer has played 4 distinct days", Tailwind, leagues of 25. (Probation can still pack her after a 5-day break, item 2.) |
| T8 · Never online together | partial | **T8.3:** §15 "at the deadline a House fills what's left at 2.0, so nobody is ever 'let down'" against §12 "Their fills of expired asks count inside the cap". When the cap is full the ask doesn't close. Item 7. The consolidation and crew-block moves are undefined (item 9) |
| T11 · Return after a month | partial | **T11.2:** landing cards show no crate count. **T11.6:** no guaranteed welcome (A's "Lodgers always send a welcome postcard" and the "Back!" ribbon were dropped). Item 16 |
| T15 · Guilt and shame audit | **FAIL** | **T15.2 / T15.6:** because baskets count anyone who "traded", setting "not with" on a resting friend (§9: "the Yard stops trading between those two after 1 hour") stops his trades and shrinks the basket. **The design pays friends for shutting out their absent friend.** Item 1 |

---

## 2 · Play-throughs (where each one breaks)

### 2.1 The solo player at 3 a.m. on a server of 5

| Day | What happens | Verdict |
| --- | --- | --- |
| 4, 02:40 | "Find me neighbours" scores Jo's station (+3 for a human, +2 for night hours) above Tom & Lee's station (+6 for two humans, −2 for evening hours). Minute 12: Mia's sand sells to Jo's Glassworks through the Yard | ✓ |
| 4 | Hub Order ×4; leagues paused; the Station Cup counts | ✓ |
| 9–13 | Exam week: Mia doesn't log in for 5 days. §9: "a stranger who takes one [free seat] and is away 5 days in a row within their first 14 is packed early to the Quiet Isles". Mia is a "stranger" in Jo's station | **✗ Mia is packed on day 14** |
| 14 | Mia opens the game to "Where do you land?". Her estate was shipped away for taking a 5-day break, in her second week, as a solo player. The text doesn't say what happens to her trades with Jo on probation (are they handed over? to whom?) | ✗ Items 2 and 4 |
| 12+ | Jo stops; Jo's estate keeps trading, so it keeps counting in the basket, plus 1.5 for the three Houses. Mia fills a basket sized for 3.5 people | ✗ Item 1 |
| 17+ | Mia is alone from day 15 (Jo stops being lit), and the lone-human card comes on day 20 | ✓ |
| — | The District Wonder isn't sized to active humans, so on 5 players its stage 1 may never finish | ✗ Item 15 |

### 2.2 The friend whose three friends quit in week 2

Ana's crew of 4 is founded on day 0. Ben, Cleo and Dev stop on days 8, 10 and 12 (9, 11 and 13 distinct days played).

| Days | What happens | Verdict |
| --- | --- | --- |
| 11–41 | Their estates run at 100% and trade through the Yard, so they are counted in every basket ("delivered or traded"). Ana fills baskets sized for four. Her only lever to shrink them is "not with" against her own friends | ✗ Items 1 and 10 |
| 20 | The lone-human card: "the crew's name, credit and heirlooms move along". If Ana moves, her three friends' holds stay behind in a station with nobody active, and holds don't lapse because Ana still plays. The result is a ghost station, and on their return "Next to your crew" has no defined target | ✗ Item 9 |
| 38 | Ben (9 days, under the 10-day threshold) goes to NPC terms at once: Ana sells to his profile at 1.25 instead of 1.75 | ✗ Item 5 |
| 40, 42 | Cleo and Dev keep the old price for 14 days | ✓ |
| 56 | The consolidation card: "Move the three of you to a fresh station?" There is one active member, and the copy assumes three | ✗ Item 9 |
| 98–102 | The holds "end by itself" (§13 timeline: "90 the hold ends by itself", counted from each sleep). Nobody is warned before a friend's estate is packed and his seat given to a House | ✗ Item 16 |

### 2.3 The returner on day 60 and on day 120 (Ben: last session day 3, asleep day 33, held)

- **Day 60.** He wakes in place. What did his estate earn on days 33–60? It is "a profile … not simulated", and the
  text never says whether a profile's trades pay the owner or the NPC (item 4). On the screens: "Relink with Ana? (one
  tap)", the Era kit, Tailwind. No welcome is guaranteed (item 16). §20 says "On day 30 Ben's estate sleeps", but it
  is day 33 (item 18).
- **Day 120.** The hold ended on day 93 unless someone extended it, and nobody was asked (item 16). His estate was
  packed and a House took the seat.
  - If the crew still holds 3+ seats, the station is "closed to strangers" and "Your seat" still works.
  - If the crew had opened the station, or moved via the consolidation card, "Next to your crew" is undefined, and
    he may land in another place with an unknown number of crates (items 9 and 16).
  - If day 120 falls in the next chapter, "held seats" persist (§16).

### 2.4 A stranger in a station of a crew of 3

- §9: "A station where one crew holds 3 or more seats is closed to strangers by default (the crew can open it)."
  Who in the crew opens it, who closes it, and do *held* seats count as "holds"? It isn't said (item 1, part c).
- When it is open, Vex gets the Yard at 1.75. But §10's shortage order is "your crew first, then usual buyers", so
  when Dev builds six Glassworks, Dev's new demand outranks Vex's established 7-day share. Vex's factory loses supply
  without notice. That contradicts §5, which promised B's "usual buyers first" (item 6).
- The Works Landmark rises in "the crew's own trade … drawn as one striped pipe … that grows … the Landmark". Vex
  helped build it, but it is crew-coloured, and it isn't said who gets the heirloom or whether it leaves when the
  crew moves (item 17).
- If the three crew members each set "not with" Vex, his Yard trade falls to zero behind three "neutral lines". That
  is a silent collective kick, the thing §6 rule 3 forbids, with no sentence on the zero (item 10).

### 2.5 A player who never wants a crew (Kai)

- **Placement:** "Find me neighbours" gives him a station with humans. ✓
- **Probation:** he is a "stranger" in every station, so he is on probation for 14 days (item 2).
- **Shortages:** he is always behind every crew in a shortage (item 6).
- **Absence:** he can never be held. §13 "Not in a crew: on day 30 … its seat reserved 7 days". Then gravity, which
  puts newcomers "next to an active human", hands his seat next to his only human neighbour to the first newcomer.
  The crewless are exactly the lonely, and they get a 7-day place while crew members get 60–120 days (item 11).
- **Crew of 1:** this isn't a way out. His "hold" lapses after 14 days with no active member, which is himself.

---

## 3 · Ranked findings, each with replacement text

### 1. Baskets count anyone who "traded": G4 fails, Houses tax the solo player, and shutting out an absent friend pays (§13, §15, §20)

**What's wrong:**
- A resting estate "runs at 100%" and trades through the Yard automatically, so it still "traded in the last 3 days".
  The promise "a sleeping friend shrinks the next basket" is false for up to 27 days.
- "Houses 0.5" makes a solo player with three Houses fill a basket of 6 h × 2.5. Houses deliver at most 25% of it,
  so she alone delivers about 11 h of her income against 6 h per member in a full crew.
- Setting "not with" on a resting friend stops his trades and shrinks the basket. The rules reward excluding an
  absent friend.

**Replacement, §15, the row "Basket size":**

> Basket size | 6 h of typical income × the residents who delivered anything to the Works in the last 3 days (at
> least 1). Houses, profiles and same-device accounts never count. Houses deliver on their own, at most 25% of each
> basket | A friend who stops, an idle alt or a House never makes the basket bigger; a solo player's basket is sized
> for one.

**Replacement, §13, the bullet "Absence costs nobody else.":**

> Absence costs nobody else. A basket is sized when its stage starts and counts only residents who delivered to the
> Works in the last 3 days, so a friend who stops makes the next basket smaller even while their estate keeps
> trading. Nothing anyone sets (not even "not with") changes a basket. An absent seller is never fined.

**Replacement, §20 case a:** "the Works baskets shrink once Ben stops delivering" → "from the next stage, the Works
baskets no longer count Ben, who has delivered nothing for 3 days".

**Same fix, §16, week 4 row:** "its stages sized to active humans like the Works" → "its stages sized like the Works
baskets (residents who delivered in the last 3 days)".

### 2. Probation punishes every solo player, crewless player and newcomer who takes a short break (§9)

**What's wrong:** "Otherwise free seats are open, and a stranger who takes one and is away 5 days in a row within
their first 14 is packed early to the Quiet Isles". This applies to *every* stranger in *every* station:
- Mia next to Jo;
- Kai, who never wants a crew;
- Noor in week 3.

Probation exists to stop a squatter blocking a *crew's* seat. Here a solo player's exam week ends with "your estate
was packed", at the most fragile moment, and the text doesn't say what happens to the packed player's trades.

**Replacement, §9, the bullet "A station where one crew holds 3 or more seats…":**

> A station where one crew lives in or holds 3 or more seats (held seats count) is closed to strangers by default.
> Any active member of that crew can open or close it, and closing never moves anyone who already lives there.
> Probation applies only to a stranger who takes a free seat in a station where a crew lives in or holds 2 or more
> seats: if they are away 5 days in a row within their first 14, their estate is packed to the Quiet Isles with
> nothing lost, their trades pass to a successor House at once (§12), and on return their old seat is the first
> landing card if it is still free. Everywhere else a newcomer's absence follows the normal timeline (§13).

### 3. The successor's start contradicts itself, so trades can stop for 7 days (§12, §13)

**What's wrong:**
- The §12 reading: the trades pass "after a 7-day notice", and the timeline shows the notice on day 23, so the trades
  pass on day 30.
- The §13 reading: "on day 30 … its seat reserved 7 days, then a successor House takes the seat with the trades", so
  the trades pass on day 37.

Under the second reading a partner who sold 30% of her output to the sleeper loses 18% on day 30 (G4), and a buyer
pays the hub's 2.5 for a week (T1.4).

**Replacement, §12, the first sentence of "When someone leaves":**

> When an estate goes to sleep (day 30) and its seat isn't held, its trades pass at once to a successor House,
> announced to every partner on day 23. The seat itself stays reserved for its owner until day 37; then the House
> moves onto it unless the owner has come back. There is no day on which the trades stop:

**Replacement, §13, the sentence beginning "Not in a crew:":**

> Not in a crew: on day 30 the estate is packed to the Quiet Isles with its layout (exists) and its trades pass to the
> successor House the same minute (announced on day 23). The seat is reserved for the owner until day 37, then a House
> warms it (see item 11).

### 4. "Profile" is undefined: who earns, which price, in what order (§12, §13, §18)

**What's wrong:** "stays in place as a profile (its trades at the rules of §12, not simulated)". It doesn't say:
- whether the asleep owner earns from the profile's trades;
- whether the old-price 14 days and the "100% … at 2.0 for 14 days" run one after the other or at the same time;
- whether the old price applies to crew partners only or to every partner.

Two programmers will build three different things.

**Add to §13, directly after the table:**

> A profile is not the owner's estate running. From day 30 an asleep estate produces and earns nothing (as on the
> Quiet Isles today), whether it is held in place or packed. Its trades are run by an NPC stand-in under §12, inside
> the total NPC cap, and the gold of those trades is NPC gold, not the owner's. Counted from the handover:
>
> - **Days 0–14:** 100% of the leaver's 14-day median volume, both ways. The price is the old one if the leaver is a
>   crew member with 5+ distinct days of play and the partner is on a different device; otherwise House prices
>   (selling 2.0, buying 1.25).
> - **From day 14:** House prices, and the volume falls 10% a week to a House's normal size.

### 5. The 10-day threshold misses the most common leaver, and rule 2 promises more than the design does (§12, §6, §1)

**What's wrong:** most friends who quit do so in week 1–2. At 10 distinct days, the friend who stays (T2) drops to
NPC terms at each handover: 1.225 → 1.0625 (86.7%), ≈ 88.9% averaged over days 30–60.
- §6 rule 2 ("Absence costs nobody but the absent player's own growth") is then simply untrue after 14 days.
- §1's restated veto dropped "≤ 5% over the absence" and still says "nobody moved without their own tap", which
  contradicts packing on day 30 and day 90 and probation.

**The cap, not the threshold, is what bounds the farm.** Inside one 25% total cap, the old price adds at most
(1.75 − 1.25) × 25% = 12.5% of one good's output value, for 14 days. That is once per company per cycle (5 days of
real play plus 30 days away), it doesn't stack with more alts, and it comes to ≤ 1.75 days of income per ≈ 44 days at
most. That is the price of not breaking the commonest case in the game. If the economy examiner vetoes it, the
fallback is to keep 10 days, but let crew leavers with 3–9 days get a House that buys at 1.5 for 14 days. That brings
T2 back to ≈ 90.5% at half the farm bound.

**Replacement, §12, the third bullet:**

> Only for a crew member with 5+ distinct days of play (not on the same device as the partner) does the old price hold
> for 14 days, still inside the 25% total cap.

**Replacement, §5 (What decided the hard questions), the last sentence:** "Only for real crew members with 10+ days
of play" → "Only for crew members with 5+ distinct days of play".

**Replacement, §6 rule 2:**

> Absence costs nobody but the absent player's own growth for 14 days after a handover. After that, the friend who
> stays trades with an NPC at NPC prices: at most the solo gap (10–15%), never more than 5% on one day, and never
> without 7 days' notice.

**Replacement, §1, the fourth veto bullet:**

> Absence costs nobody else: at most 5% on any day, at most the solo gap in the long run, no lost multipliers, no
> bigger baskets. Nobody is moved without their own tap, except an asleep estate (30+ days) into safety and a
> stranger on probation (§9).

### 6. Crew first before usual buyers lets a crew member's new demand starve a stranger (§10, against §5)

**What's wrong:** §5 sells the merge as having B's "usual buyers first". §10 says "your crew first, then usual buyers
(their 7-day share), then new demand". When Dev builds six Glassworks, Vex's established supply falls overnight,
with no notice. That is the exact attack C made on B's Main, now aimed at the stranger and the crewless player.

**Replacement, §10, the bullet "Shortage order":**

> Shortage order: usual buyers first (each buyer's share of the last 7 days, crew or not), then crew, then new demand.
> A buyer's usual share shrinks by at most 10% a week, so nobody's factory loses its supply overnight because a
> neighbour built more.

### 7. "Nobody is ever let down" is false when the NPC cap is full (§12, §15)

**What's wrong:** expired asks are filled by a House "inside the cap". A player who already gets 25% of a good from
Houses has an ask that never closes.

**Replacement, §12, "One total cap" row, the second sentence:**

> Their fills of expired asks count inside the cap; when the cap is full, the hub fills the rest at 2.5 and the ask
> still closes.

**Replacement, §15, the "Ask for…" bullet:**

> Ask for… a good (12–48 h): crew mates and neighbours fill it whenever they play. At the deadline a House fills what's
> left at 2.0 (the hub at 2.5 when the NPC cap is full), so an ask always closes. The result names who filled what.

### 8. Holds for an arriving friend lapse after 3 days of crew inactivity (§9, against §13 and §19)

**What's wrong:** "stay held while any member played in the last 3 days". A crew that takes a 4-day long weekend
loses Dev's seat, while sleeper holds lapse only after 14 days. That is two rules for the same thing, and the stricter
one hits the "friend joins next week" case.

**Replacement, §9:**

> Seats held for friends who haven't arrived stay held while any member has played in the last 14 days, up to 28 days.
> Nobody taps "renew".

**Replacement, §19, decision 7, the Decision cell:**

> 3 / 14 / 23 / 30 days; profiles. Crew members' seats are held by default for 60 days from sleep; any active member
> can extend a hold once or release it. A friend's arrival seat is held up to 28 days. All holds lapse after 14 days
> with no active crew member.

### 9. Crew moves, consolidation and the lone-human card are undefined, and they strand the sleepers' holds (§7, §9)

**What's wrong:**
- "A crew moves as a block" and "Move the three of you to a fresh station?" contradict "nobody moved without their own
  tap" and "no votes". Who taps, and what happens to members who don't?
- When the active members move, the asleep members' held seats stay in the old station. Holds don't lapse (the crew
  is active), so the result is a ghost station, and their return card "Next to your crew" has no defined target.
- "Alone in a station" and "slept 14+ days" are undefined.
- The consolidation copy assumes three active members.

**Replacement, §7, the bullet "The lone-human card…":**

> The lone-human card comes on the 5th day on which you are the only resident who played in the last 3 days, at most
> once every 30 days: up to 3 livelier stations; the crew's name, credit and heirlooms move along. After that it's a
> button. The consolidation card comes once, when at least two members are active and more than half of the crew is
> asleep (30+ days away): "Move to a fresh station together?" The first active member who taps reserves a fresh station
> for the crew for 7 days and moves; every other active member moves with their own tap, and nobody is moved by
> someone else. When active members move, their asleep friends' holds move with them: the held estate (already
> asleep) is packed and its hold points to a seat in the new station, so "Your seat" on their return is next to their
> friends.

**Replacement, §7, the bullet on moving, the clause "a crew moves as a block":**

> a crew moves as a block: the first member's tap reserves seats for the whole crew for 7 days, and each member moves
> with their own tap.

### 10. "Not with" by a whole crew is a silent kick with no sentence on the zero (§9)

**What's wrong:** three private "not with" taps drop a stranger's Yard trade to zero behind "a neutral line" that is
never written down. That is collective exclusion, which §6 rule 3 forbids, and a zero without a sentence, which rule 6
forbids.

**Replacement, §9, the sentences from "Anyone can privately set…" up to "…reads a neutral line.":**

> Anyone can privately set "not with" for any company: the Yard stops trading between those two after 1 hour (covered
> by the tank), and the other side reads "X now trades its spare with others". If companies that bought or sold more
> than half of your Yard trade last week have set "not with" you, you get one card: "Your goods sell better elsewhere:
> move for free to one of these stations that need them" (three cards). Nobody learns who set it.

### 11. A crewless sleeper's seat next to his only human neighbour is the first one given away (§13, §7)

**What's wrong:**
- Crew members are held for 60–120 days. A player without a crew gets "its seat reserved 7 days".
- After that, gravity ("+3 per active human"), which favours the seat next to his only human neighbour, hands it to the
  first newcomer.

The lonely lose their one relationship first.

**Replacement, §13, the end of the "Not in a crew" sentence (after item 3):**

> The seat is reserved for the owner until day 37. After that a House warms it, and placement gives it to a newcomer
> only when it is the last free seat in its station.

### 12. The return lost its guaranteed welcome, the crate count and the hold-end warning (§13)

**What's wrong:**
- A's "Lodgers always send a welcome postcard" and the "Back!" ribbon are gone (T11.6).
- The landing cards don't say how many buildings will be crated (T11.2).
- Holds "end by itself" on day 90 without any active member being asked.

**Replacement, §13, under "Where do you land?" (after the four cards):**

> Your seat is preselected when it was held or is still free (0 crates); every card shows how many buildings would go
> into crates. Then the Era jump kit, Tailwind and a "Back!" ribbon for 7 days; the Houses of your station always send
> one welcome postcard, so the first session never ends with nobody saying hello.

**Add to §13, after "Any active crew member can extend a hold once…":**

> Seven days before a hold ends, every active crew member gets one card: Extend 60 days (the first answer counts) or
> Let it end. Without an answer, it ends.

### 13. The District Wonder isn't sized to who is there (§16)

**What's wrong:** only "the Era Wonder … its stages sized to active humans". On a server of 5, the existing District
Wonder may never finish stage 1 (T5.5).

**Replacement, §16, week 4 row:**

> Week 4 | The Works finale; the District Wonder and the Era Wonder, fed by trunks, with stages sized like the Works
> baskets (6 h of typical income × the humans who delivered in the last 3 days, at least 1; NPCs at most 25%), so a
> district of one human still finishes its stage.

### 14. "Lit", "active", "alone" and "slept" each have zero or two definitions (§7, §14, §19)

**What's wrong:**
- §7: lit = "played in the last 3 days"; §14: "Lit windows on estates played in the last day".
- Placement counts an "active human" that is never defined.
- The quiet grace, the leagues ("pause below 10 active companies") and Tailwind's "active median" each need one
  definition.

**Add to §6, after "Three words in the first 20 minutes":**

> **Definitions used everywhere.**
>
> - **Lit seat / active player:** the owner played in the last 3 days. This one count is used for the quiet grace,
>   placement, leagues, the lone-human card, Tailwind's median and holds.
> - **Lit windows:** played in the last 24 h (the picture only).
> - **Away:** days since the last session.
> - **Asleep:** 30+ days away.
> - **Delivering resident:** delivered anything to the Works in the last 3 days (basket sizes only).
> - **NPCs** (Houses, profiles) never count as players in any of these.

### 15. "Never: gold moving through prices" contradicts the price ladder (§23)

**What's wrong:** every Yard trade at 1.75 moves 0.75 × base above the hub price, which is gold moving through
prices, by design. Read literally, the never-list forbids the Yard.

**Replacement, §23, the seventh bullet:**

> Gold moving between players other than as the price of goods really made, inside the presets (at most 2.125 ×
> base).

### 16. Who gets the perk: everyone who delivered, or everyone in the station? (§8, against §15)

**What's wrong:** §8 says "everyone who delivered gets the perk"; §15 says "The perk | For everyone in the station".
A delivery-gated perk makes the absent and the newcomer lose it.

**Replacement, §8, minute 15–20 row:**

> The Yard's first project, the Foundation, appears: one player alone can finish it in about 15 minutes, together it
> goes faster, and everyone in the station gets the perk.

### 17. The Landmark and its heirloom belong to nobody in particular (§10, §15, §16)

**What's wrong:** the Landmark grows from "the crew's own trade … drawn as one striped pipe" but is the *station's*
Works. A stranger who helped build it may see it leave with the crew, or never get its heirloom.

**Replacement, §15, the finale row, the Why cell:**

> The big moment without an appointment. Its Landmark belongs to the station and stays when people move; every
> resident who delivered to the finale gets its heirloom.

**Replacement, §10, the "striped pipe" bullet:**

> The crew's own trade is drawn as one striped pipe around the Yard in the crew's colours; it grows lamps and arches
> with the crew's deliveries. The Landmark itself is the station's.

### 18. Case (a) has the wrong sleep day and the old basket wording (§20)

**What's wrong:** "On day 30 Ben's estate sleeps in place". His last session was day 3, so he sleeps on day 33.

**Replacement, §20 case a, last two sentences:**

> On day 33 (30 days after Ben's last session) Ben's estate sleeps in place as a profile; on day 36 Ben wakes in the
> held seat in a minute: "tended 17 times".

### 19. The Frontier can split a small server (§13, §14, §16)

**What's wrong:** "in the Frontier district with other late starters" and "a fresh Frontier district" contradict "a
new district opens when the open ones are 60% lit". On a server of 20, a Frontier opened for one newcomer is a new
empty room.

**Replacement, §14, the first bullet's ending:**

> …or in the Frontier, which is the newest open district. It is offered only when it has at least 3 lit seats of
> players who started in the last 14 days, and it never opens a district by itself.

### 20. The invited friend can be stuck waiting for someone to be online (§9)

**What's wrong:** "The general crew link needs a member's tap after 24 h". A friend who opens it on day 8 waits for
a member to come online, so the "two taps, 60 s" promise (§24) fails.

**Replacement, §9, the "Seat codes" bullet:**

> Seat codes. Every held seat has its own code, and the share sheet always sends one, so an invited friend moves in at
> any time with nobody online. The general crew link (for friends without a held seat) needs a member's tap after
> 24 h, so a link posted publicly can't hand a stranger a friend's seat.

### 21. "Every choice can be undone" isn't true for releases and probation (§6 rule 4)

**What's wrong:** a released hold or a probation pack sends an estate to the Quiet Isles. Neither can be undone,
only recovered from.

**Replacement, §6 rule 4:**

> Every choice can be undone or recovered from: moving, leaving, a link; a released or lapsed seat comes back as the
> first landing card whenever it is still free.

### 22. §24 has no measure for the lonely player's real failure modes (§24)

**What's wrong:** none of the failures above would show up in the measure table. There is no row for probation
packs, for a basket that didn't shrink, or for the friend who stayed.

**Add three rows to §24:**

> | Players packed by probation who never return | ≤ 20% of those packed | Narrow probation further (crew seats only) |
>
> | Income of the friend who stays, 30 days after friends' handovers | ≥ 90% of before | Old price for crew leavers from 3 days, or House at 1.5 |
>
> | Ask expiries that closed | 100% | The hub always fills the rest |

---

## 4 · Round-4 must-takes that did NOT make it in

The list in r4-exam-lonely.md §5 had 17 items:

| # | Must-take | Status in v1.2 |
| --- | --- | --- |
| 1 | Holds by active members, 60 days, renewable once, lapsing after 14 days with nobody active | **In** for sleepers (§13). Arrival holds still lapse after **3** days (item 8) |
| 2 | Successor split by direction, one total NPC cap | **In** (§12), but the start date contradicts itself (item 3) and expired asks fail inside the cap (item 7) |
| 3 | Old price only for crew members with history | **In at 10 days.** I now correct my own number to **5** (item 5): 10 misses the week-2 quitter, the commonest case |
| 4 | Tailwind formula | **In** (§13) |
| 5 | Probation | **In, but over-broad**: it hits every stranger in every station (item 2) |
| 6 | Timers (72 h, 12–48 h, the result names who) | **In** (§1, §10, §15) |
| 7 | 5 requests an hour, one Block tap, "not with" after 1 h | **In** (§9, §20 d), but with no sentence and with the collective-kick hole (item 10) |
| 8 | Host bonus gated by distinct days and device | **In** (§14) |
| 9 | Activity buckets | **In** (§7) |
| 10 | Push and message budget | **In** (§13) |
| 11 | Tending banks 3 days | **In** (§14, §15) |
| 12 | **District Wonder sized to active players** | **Not in.** Only the Era Wonder (item 13) |
| 13 | Old seat as a card, **crate count per card**, relink in one tap | Old seat and relink are in; **the crate count is not** (item 12) |
| 14 | Migration links for every human pair with flow | **In** (§21) |
| 15 | Station + halts as one big station | **In** (§7, §9) |
| 16 | Consolidation card | **In, but undefined** (who taps, what happens to sleepers' holds, item 9) |
| 17 | Keep A's absent-seller rule, profile, digest, ×4 grace, lit-seat fix | All in, **except A's "Lodgers always send a welcome postcard" and the "Back!" ribbon**, which were dropped (item 12) |

Also lost from the exam: **G4's "≤ 5% over the absence as a whole"** is no longer in the veto list (§1), and the
**hold-end warning** never existed in any spec. Items 5 and 12 restore both.

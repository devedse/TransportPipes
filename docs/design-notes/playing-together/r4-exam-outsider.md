# Round 4 · The outsider's exam, run against A, B and C

I ran my exam (r3-exam-outsider.md) against r3-A.md (Stations with a Yard), r3-B.md (Streets & Lines) and r3-C.md
(Places, Crews, Links), using the exam's own rules:
- only the spec's text counts;
- anything unstated fails;
- "tune later" counts as undecided;
- the red flags cap a test at partial.

Pass = the full weight, partial = half, fail = 0.

## 0 · The verdict in brief

| | A · Stations with a Yard | B · Streets & Lines | C · Places, Crews, Links |
| --- | --- | --- | --- |
| **Score** | **55 / 100** | **61 / 100** | **62 / 100** |
| Must-pass failed | T3, T13, T15 | T3, T13 | T3 only (all three fail it) |
| Must-pass partial | T4 | T4, T15 | T4, T13 |
| Rank | 3 | 2 | **1** |

**Winner: C**, narrowly. It is the only concept whose one must-pass failure is the one all three share. That failure
is that nobody wrote down the invited friend's first 60 seconds.

C is also the safest (T15: 9 of 9 attacks closed) and the clearest about whom you trade with (T2). It needs B's
*look* to be loved, and A's handling of absence, to pass everything (§4).

### How I applied the rules where they needed reading

- **T2's "at most 4 priority steps":** I count the steps after your own consumers, because using your own goods is
  not a trading decision.
- **Purpose versus mechanism:** where a criterion states its purpose ("no gold windfall", "no factory stalls without
  warning"), I judge by the purpose. Where it states a number (taps, seconds, days, words), the number counts.
- **Must-pass:** a partial on a must-pass test is not a fail. But the winner must reach a full pass on all six, which
  is what §4 is for.

---

## 1 · Test by test

| Test (weight) | A | B | C |
| --- | --- | --- | --- |
| **T1 Words** (5) | **Partial.** 7 words in week 1: station, Yard, crew, link, Lodger, Works, Wish | **Partial.** 7: street, crew, Main, Lodger, link, Crew Order, Wish (plus a valve) | **Fail.** 8: station, plaza, junction, crew, link, House, Works, Wish. Plaza and junction are two names for one place |
| **T2 One sentence & routing** (5) | **Partial.** 5 steps (contracts → links → Yard → storage → hub). Mo gets Yard coal if he laid his pipe. Ledger 1 tap away. *Red flag:* a crew pair can trade at a link price and at the Yard price at once | **Partial.** 5 steps (contracts → Main → links → storage → hub). Mo gets nothing | **Pass.** 4 steps. *"I trade with whoever I'm linked to; my crew is linked automatically at 1.75; the rest goes to the hub."* (20 words, true in all three edge cases) |
| **T3 Invite in 60 s** (6) ✔ | **Fail.** No spec says Ben can play before making an account (criterion 2 unstated). Tap counts aren't written out. Ana gets no "Ben moved in" line. Seat codes pass criterion 5 | **Fail** (the same) | **Fail** (the same) |
| **T4 First 20 minutes** (7) ✔ | **Partial (5/6).** The Handshake plays "even with one connected yesterday", so Cleo gets her moment. Only #6 fails: 5 words in 20 minutes (station, crew, Yard, Works, Lodger) | **Partial (4/6).** No spec text says the Handshake plays for a late arrival (#4). 5 words (#6) | **Partial (4/6).** Late Handshake unstated (#4). 6 words, including "junction" (#6) |
| **T5 Day-8 friend** (5) | **Partial.** The hold renews itself (28 days), Dev lands as the missing copper. But Tailwind is named and never defined | **Partial.** Holds renew themselves. Tailwind ×(1 + day/14) has no end condition | **Pass.** Holds renew themselves. Tailwind is fully defined and ends within 5 levels of the crew's median |
| **T6 Guilt audit** (6) ✔ | **Pass.** Nothing counts heads, goals resize, strangers see dark windows | **Pass.** No head count, strangers see nothing | **Pass.** Baskets resize on day 6, partners read "resting" |
| **T7 Skip days, chores** (5) | **Partial.** Co-op is automatic, so P2 reaches about 90%. How long Wishes and link offers wait is unstated | **Partial.** Wishes last 12–48 h, under 72 h | **Partial.** Link offers wait 72 h, but how long Wishes wait is unstated |
| **T8 The coal seat** (4) | **Pass.** Own consumers come first, there's a per-good "Yard first" switch, and the ★2 patches stay in the start area | **Pass.** Own consumers come first, and the start kit is "complete in kind" | **Partial.** The ★2 patches move out of the start area ("the role is visible at minute 1"), so an energy seat has no coal of its own until Level 5. And every drop on a pair link is visibly Ben's doing |
| **T9 Five players, shrinking server** (6) | **Partial (7/8).** Sleeping crew members stay in place up to day 90 behind dark windows, so a shrinking server fills with them (B2) | **Pass (8/8).** Sleepers are packed at 30 days and a Lodger holds the lot. Placement prefers humans, the lone human gets a card, leagues pause | **Partial (7/8).** Held estates stay in place up to day 150, with ivy and the autopilot running (B2) |
| **T10 More than a price** (4) | **Pass.** The Yard, the Works and the ledger | **Pass.** The Main grows lamps, a Cistern, arches and the Gate Arch: the best shared object of the three | **Pass,** but only just. The Works belongs to the place, not the crew, and crew trade is the same 1.75 that strangers' links default to |
| **T11 Buildability** (5) | **Fail.** Step 1 is plumbing. Friends arrive in step 4 (day 9). 18 days in total | **Fail.** Friends arrive in steps 2–3 (day 6–7). 18–19 days | **Fail.** Friends arrive in step 3 (day 7). 18 days, and step 2 needs plaza art |
| **T12 Performance** (4) | **Pass.** 8 sockets, teleporter entries, asleep estates kept as profiles, a 5 ms bound, frozen districts | **Pass.** O(members × goods), packed sleepers, dark districts freeze | **Partial.** Held estates are fully simulated for up to 120 days. Otherwise fine, with a 20 ms bound |
| **T13 Return on day 36** (5) ✔ | **Fail.** For 14 days after day 30, the stand-in (a "profile") buys at the old price, up to Fair. That is minted gold (#4). The message limit (#3) and kit expiry (#6) are unstated | **Fail, the closest call of the exam.** Ben's lot was packed on day 33, so a one-minute landing follows the three screens: about 90 s, over 60 (#1). The message limit (#3) is unstated | **Partial.** Ben's held estate is where he left it and pays its trades with its own gold, so nothing is minted. Only the message limit (#3) is unstated |
| **T14 Week-3 newcomer** (4) | **Partial.** Tailwind undefined | **Partial.** No end condition | **Pass.** A waiting link, the fair band, a full formula, credit by own share, the Rookies league |
| **T15 Griefer + 2 alts** (6) ✔ | **Fail.** Attack 2 stays open: an alt on its own device leaves a 14-day profile buying at 1.75. The other 8 are closed. *Red flag:* strangers share the pool | **Partial (8/9).** Attack 9 is open: no chat mute and no rules for names (pair-mute is for goods only) | **Pass (9/9).** Successors trade at House prices inside the 25% cap, held estates spend only their own gold, seat codes, block list, preset emblems |
| **T16 Falling out** (5) | **Pass.** A private "not with" switch, no vote, no eviction | **Pass.** Pair-mute, and invited friends are never removed. Pair-mute is silent with no notice, but the fallback means no factory stops | **Pass.** An explicit 1-hour notice, the 2-hour tank and a neutral "paused" line |
| **T17 Seven friends** (4) | **Pass.** A station plus both halts, with Street pipes joining the pool *and* the Works | **Pass.** A 4 + 3 stretch on one Main | **Partial.** The second halt is attached to another station (C's pinwheel layout pairs each station with one halt). The halt members' share of the perk is unstated, and the crew pipe is 68 tiles |
| **T18 Week 4, season 2** (5) | **Partial (4/5).** The Line Race to the new Wonder favours veterans who already have trunks | **Partial (3/5).** That too, and there is no per-active-member group competition in r3-B | **Partial (4/5).** The Line Race favours veterans |
| **T19 Migration** (4) | **Partial (4/6).** No fate for contracts, toll meters, teleporters or franchises; no dry run with rollback | **Partial** (the same) | **Partial** (the same; its before/after checks come closest to a dry run) |
| **T20 No open questions** (5) ✔ | **Pass.** 12 of 12, each with "change it if"; question 10 explicit | **Pass** | **Pass** |
| **Total** | **55** | **61** | **62** |

### The smallest fix for every fail

| Spec | Fail | Smallest change that makes it pass |
| --- | --- | --- |
| All three | **T3** | Tapping the seat code opens a "Move in next to Ana" screen, and one tap puts Ben on his own estate in the held seat: 2 taps, about 30 s, as a guest. The account is asked for at the end of day 1. Ana's next report opens with "Ben moved in next door". |
| All three | **T11** | Reorder the build. **Step 1 (≤3 days):** crews, seat codes, guest move-in, held seats, placement next to each other on *today's* map, colours. The links engine comes second. The owner's core wish ships first, and the plumbing follows. |
| A | **T13, T15** | One change: from day 30 (after the day-23 notice), the profile trades at **Lodger terms (1.25/2.0)** with 10% decay a week, and the 14 days at Fair go. Also state at most 3 win-back messages and that the jump kit never expires. |
| B | **T13** | Unpack the held lot while the return screens play, so the estate is ready when Ben taps screen 3 (60 s or less in total). State at most 3 win-back messages. |
| C | **T1** | One name for the plaza (Yard, A's word), with no "junction", "manifold" or "place" on screen. Crew trade without the word "link". An "Ask for…" button instead of the noun "Wish". That leaves station, crew, Yard, House and Works: 5 words. |

---

## 2 · The ranking, and why the close call goes to C

1. **C · 62.** It fails only the shared T3. It is the safest (9 of 9 attacks closed; nothing to drain, because nothing
   is pooled with strangers), the clearest about whom you trade with (everything is a link), and the most complete in
   its numbers (Tailwind, holds, notices).
   - Its weaknesses are words, the halts, ghost estates, and a first hour that ties each seat to its role. All of
     them sit in the UI and in numbers, so they are cheap to fix.
2. **B · 61.** The concept three friends would love at first sight: a striped pipe that is *theirs*, grows lamps and
   arches, and later runs across the map with their names on it.
   - It fails T13 by about 30 seconds of landing. If you forgive that, B reaches 63.5, and like C it fails only T3.
   - I still rank it second, because its remaining gaps sit in its *social rules*, which are hard to fix:
     - lots open to strangers by default once a crew has 2 or fewer active players, so a stranger can join Kettle
       Works' crew while Ben sleeps;
     - removing such a stranger takes a majority vote;
     - pair-mute is silent;
     - there is no chat safety;
     - there is no per-member group competition.

     Fixing the open-lot default leaves B's lonely players alone on their own streets (A's B1 attack), which is the
     one thing my exam's T9 is built to stop.
3. **A · 55.** A has the best social instincts: the Handshake plays even if the other pipe was laid yesterday, nobody
   votes or evicts, the halts are full members, and asleep estates are profiles. But:
   - It carries a 14-day gold farm (its own decision 6).
   - It puts strangers' settings inside the pool that feeds your buildings (the red flag).
   - Tailwind is a word without a formula.

   Two must-pass failures come from one paragraph, so it is closer than the score looks. It is the best donor of
   parts.

**An outsider's note:** the three specs are about 80% the same text. They share the Lodger/House prices, the Hub
Order, trunk lanes at 2%, the three return screens, seat codes and own-share credit. The exam separates them on only
six decisions:
1. what friends share (a pool, a crew pipe, or links);
2. whether strangers get into it;
3. how held seats age;
4. whether parcels are carved;
5. the words;
6. the first hour's land.

---

## 3 · The winner, C, and what it must take from the others

### Must-take: what makes C pass all six must-pass tests

| # | Change | Fixes | From |
| --- | --- | --- | --- |
| M1 | **Guest move-in by seat code:** 2 taps and about 30 s to your own estate in the held seat, the account at the end of day 1, and "Ben moved in next door" on Ana's next first screen | T3 | New. None of the three has it (r2-2 had a guest login) |
| M2 | **The Handshake plays when your own pipe connects**, even if your friend connected yesterday and nobody is online. And the first 20 minutes teach **three words only:** station (the picker), crew (Start with friends) and Yard (the plaza). The Foundation is "the Yard's first project", and the ⚙ label explains Houses the first time you trade with one | T4 | A §3, T1's word budget |
| M3 | **At most 3 win-back messages per absence**, positive and concrete, never a countdown | T13 | r2-3 §2.4 |
| M4 | **A held estate stays in place** (it can still be tended, and its balloon stays on the fence), but from day 30, after the day-23 notice, it runs as a **profile at House terms (1.25/2.0), decaying 10% a week**. It is not simulated, there is no autopilot spending by default, the hold is 60 days, and it ends by itself (no renewal to 120) | T13 (and T9, T12) | A's profile mechanism with B and C's successor numbers |

With M1–M4, C passes T3, T4, T6, T13, T15 and T20.

### Should-take: what lifts the rest

| # | Change | Lifts | From |
| --- | --- | --- | --- |
| S1 | One place word for the plaza (Yard); no "junction", "manifold" or "place" in the UI; crew trade without the word "link"; "Ask for…" instead of "Wish" | T1, T2 | A's word, my T1 |
| S2 | **B's Main as the look of crew trade.** The game draws the crew's trade as one pipe striped in the crew's colours around the Yard, and the Works stages hang lamps, arches and finally the Landmark on it. Same engine, B's soul | T10, the love test | B §2.2 and §2.5. This is C's own decision-3 fallback, made the default |
| S3 | Keep the ★2 patches in the start area, and put the seat's home goods in parcels 5 and 7 | T8, T4 | A's decision 10 |
| S4 | A 7–8 friend crew's halts are **full members** of the station's Yard and Works (credit and perk), joined by drawn crew pipes. The pinwheel applies only to strangers | T17 | A §2 |
| S5 | Wishes wait 72 h, then a House fills what is left at 2.0 | T7 | B's fill, with a longer wait |
| S6 | The season-2 Line Race runs in stage brackets (Rookie, Mid and Veteran markers). The Station Cup stays per active member | T18 | New |
| S7 | Migration fates, decided: contracts stay, re-priced as band positions; toll meters are removed with a refund; teleporter pairs become links; a franchise keeps its own seat in the same crew. Dry-run on a database copy and roll back from a snapshot | T19 | New |
| S8 | Reorder the build so friends come first (the T11 fix) | T11 | New |

With M1–M4 and S1–S8, **C would score about 95** on this exam. The 5 points it can't get back are T11's total of 18
days.

---

## 4 · Decisions in C that I would overrule

1. **Pair settings for crews.** C shows priority, caps and presets per pair, and lets a seller put "Ana first".
   - *Overrule:* hide these inside the crew. Crew trade becomes one automatic thing, shared out by need with at most
     50% of a good to any one member (A and B's shortage rule). Per-pair settings stay for links outside the crew.
   - *Why:* friends shouldn't keep books, and "Ana first" ranks friends against each other in plain sight. That is
     inequity aversion (B's attack C1, and my round-2 assumption A1: "together isn't accounting").
2. **Crew removal by majority vote.**
   - *Overrule:* drop it. Leaving takes one tap, plus A's private "not with" switch.
   - *Why:* a vote among four friends is kick drama, and C already says nobody can be evicted from a seat. A crew
     removal is the same wound without the eviction.
3. **Moving the ★2 patches out of the start area** ("the role is visible at minute 1").
   - *Overrule* with S3.
   - *Why:* a role at minute 1 is a dependency at minute 1. My T8 is built on this, and so was the outsider's
     round-2 assumption A4.
4. **Holds of 60 + 60 days, kept in place, fully simulated, with the autopilot reinvesting by default.**
   - *Overrule* with M4.
   - *Why:*
     - ghost stations look full to newcomers;
     - the server simulates the absent;
     - a caretaker spends Ben's gold on choices he never made;
     - being away four months shouldn't out-earn the friend who kept playing.
5. **Link cards between people in the same place wait up to 72 h.**
   - *Overrule:* a card to someone in your own place is accepted automatically the next time the receiver plays,
     unless they tap "not now". Houses fill the gap meanwhile.
   - *Why:* the lonely player's first human trade shouldn't wait days for someone else (A's attack C1). Consent stays:
     both chose the place with full information, and declining takes one tap.
6. **The hub paying half above the Hub Order.**
   - *Overrule:* glide to 0.7 (A and B).
   - *Why:* the same push outward, but a sudden halving reads as a punishment for growing, and quiet servers feel it
     first. The friends' designer attacked this himself in round 2 (W4).
7. **Three layers on screen (place, crew, link).**
   - *Overrule:* one place word (station; "halt" is shown only to couples), one shared thing (the Yard), and "link"
     only for partners outside the crew.
   - *Why:* T1. A new player should be able to explain the game to their friends in the group chat in one breath.

---

## 5 · Would a real new player with three friends love it?

**C as written:** they would *trust* it, but they wouldn't fall for it.
- The first evening is good: balloons land together, a Handshake, a Foundation by minute 40.
- After that, their shared life is a crew panel of pair links with presets, priorities and caps, and a Works they
  share with whoever else lives in the station. Their seats are roles from minute one.
- It reads like a well-run cooperative, not like "our thing".

**B as written:** they would love it on sight. *Our* striped pipe runs past all four of our gates, grows lamps on
Tuesday, gets arches in week 2, and in week 3 runs off across the map with our names on every segment. But in week 2,
after Ben stops, a stranger joins Kettle Works by default, and the friends' fondness meets B's social rules.

**C with M1–M4 and S1–S8:** yes.
- Ben is in the game 30 seconds after the WhatsApp message.
- The four learn three words that evening.
- Their trade is one striped crew pipe that grows with their Yard, nobody keeps books, nobody votes on anybody, and
  nobody gets a stranger in their pool.
- When Ben comes back on day 36 his estate is where he left it, with "Ana tended your estate 17 times".

So the final design takes **C's skeleton** (consent, links, safety), **B's soul** (the crew's own pipe that grows)
and **A's manners** (the Handshake that waits for you, no votes, halts as full members, asleep estates as profiles).

## 6 · Blind spots all three share (the exam found them, nobody wrote them)

1. **The invited friend's first 60 seconds:** guest play, the tap count, and Ana seeing that Ben arrived (T3).
2. **Every build plan starts with plumbing,** and friends arrive between day 6 and day 9 of building (T11).
3. **No limit on win-back messages** (T13).
4. **How long a Wish waits** is unstated in A and C (T7).
5. **The season-2 race favours whoever already owns trunks** (T18).
6. **The fates of contracts, toll meters, teleporters and franchises,** and a rollback plan for the migration (T19).

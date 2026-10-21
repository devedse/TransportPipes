# Round 4 · B defends Streets & Lines

I read r3-A.md and r3-C.md in full, and the three exams (economy, lonely, outsider), because they say how the answers
will be marked. Numbers are × the hub's base price, or shares of T(L), the typical income (the economy exam's table:
about 3,770 g/min on day 14, one day ≈ 5.4M). The contract exploit is fixed in commit 9621766: a seller can promise
only what its buildings make, and a penalty never takes gold below zero. B relies on that and adds one rule below.

**In short:** of the six attacks on B, I concede two (C-B3 and A-B2), concede part of two (A-B3 and C-B1) and rebut
two (A-B1 and C-B2, which partly target round-2 rules that r3-B had already replaced). Of my six attacks on the
rivals, two stand, two stand in part and two fell because the rivals fixed them in r3. I replace the fallen ones with
new attacks.

---

## 1 · The attacks on B

### C-B3 · "The pro-rata Main hides who gets what; one member's growth silently takes another's supply". **Conceded.**

C's scenario is correct under r3-B's rules. Ben sells 120 coal/min. Ana's Smelters use 80. Dev builds Steel Mills
that want 60. The share by need gives Ana 69 and Dev 51, so Ana's Smelters drop to 86% without her changing anything.

r3-B did let Ben say "Ana first" with a standing order ("my spare coal to Cleo first · always"), but that is a
remedy he has to find. The default was wrong. **The change, "usual buyers first":**
- **Each buyer keeps its usual share first:** its 7-day average from that seller. New demand gets what is left, then
  the rest is shared by need. In the scenario Ana keeps her 80, Dev gets the free 40 and imports 20 at 2.5. Nothing
  of Ana's moved.
- **New demand joins after a 1 h notice**, the same notice as on links (GDD §5.6). Ana's panel would say "from 15:02
  Dev's Steel Mills also buy Ben's coal; your usual 80 come first".
- **When the seller's supply drops** (Ben rests and doesn't grow), the usual shares are cut by the same percentage,
  and each buyer reads a named line: "Ben's coal is 10% short this hour; 8/min come from the hub".
- **Seller priorities:** one tap per good ("Ana first", "share equally").
- **Every buyer on the Main gets a 2 h tank**, like links.

This also replaces r3-B's 50% anti-hog cap. A newcomer, a stranger or a griefer has no usual share, so he can never
take an incumbent's supply. He gets what is spare, and that is the anti-hog rule.

C's own decision 3 ("show the crew as one pooled Main in the UI: B's view on C's engine") meets this from the other
side. The two now differ only in their defaults: pooled with incumbents first (B), or pairs that must be set (C).

### A-B2 · "The Main turns a falling-out into a public vote". **Conceded (the vote was already narrowed; now it goes).**

A's scenario (Ana and Cleo vote Ben off) was round 2. r3-B already said "invited friends are never removed",
leaving a vote only for strangers who took an open lot. A and the lonely exam (T9.4, "whose majority in a crew of
2?") are right that any vote is a drama generator. **The change: B has no votes at all.**
- **Pair-mute is private** and takes effect after the 1 h notice and the 2 h tank (economy exam T11 asks for
  exactly this). The muted side reads a neutral line: "less of your coal found buyers on the Main; the rest went to
  your Trading Post". No names.
- If every active member mutes a stranger, he is effectively off the Main. Nobody pressed "remove", nobody was
  counted and nothing is announced.
- Griefers are handled by probation, moderation reports and the usual-buyers rule.
- **Junctions** need no majority either: one active member on each side opens one, and any member can switch it off
  for their own goods.

### A-B3 · "A members-only pipe in front of strangers' doors; four streets; a lossless Main removes distance". **Two parts rebutted, one conceded.**

- **"In the corridor lane their own links need": rebutted.** In all three specs links are non-exclusive bundles and
  nobody owns a corridor tile. The Main is one more pipe in the bundle and takes no capacity from anyone's link.
- **"A club on their doorstep": mostly rebutted.** The Main runs only along the crew's *stretch*, the crew's own run
  of lots. Fox & Sons and a solo player further along the same corridor have their own frontage, and the Main ends
  before their gates. The only way a non-member ends up inside a stretch is a leaver, so a leaver gets the free move
  first. If he stays, the Main is drawn on arches over his gate, the way a bundle passes any gate it doesn't serve.
  A has the same club in a different shape: its stranger sits in the friends' plaza, last in line behind the crew
  in every shortage.
- **"Every lot faces four corridors, so four answers": rebutted.** A lot has one home street, the side with the Main
  valve. It is drawn and named on the estate the way A's fifth "Yard gate" is. The other three sides are link sides.
- **"Lossless removes distance": conceded in part.** A's Yard is nearly lossless too (an 8–16-tile plaza pipe), but
  a flat loss gives the Main's upgrades a felt purpose. **Change:** a flat Main loss by tier of grey 3%, copper 1.5%
  and steel 0.5% (about 10 tiles of that tier), the same for every pair on the stretch. Distance still rules links
  and Lines.

### C-B1 · "Lodgers on the Main pay the friend price, so two bots equal two friends, and alts multiply them". **Rebutted for r3; one wording conceded.**

C quotes round 2 (Lodgers at 1.75 and 15% each). r3-B already priced NPCs at 1.25 to buy and 2.0 to sell, at 25%. With
the economy exam's index (I = 1 + s(p − 1) + b(2.5 − p_buy), b = 0.10):

| Player | Calculation | I | Ratio |
| --- | --- | --- | --- |
| Solo with NPCs (sells 25% at 1.25, buys at 2.0) | 1 + 0.25 × 0.25 + 0.10 × 0.5 | 1.1125 | |
| Crew (sells 25% at 1.75, buys at 1.75) | 1 + 0.25 × 0.75 + 0.10 × 0.75 | 1.2625 | |
| With the new flat 3% loss on a grey Main | | ≈ 1.25 | **0.89** |

That is inside the exam's 0.85–0.93 band, so bots are not friends. **Conceded:** r3-B's table said "25% of your output"
without saying whether that is per NPC or for all of them. It is now **one total cap for all NPCs together**,
successors and held-lot NPCs included. So Gary's five alts plus two held lots give at most 0.25 × 25% ≈ **6% of his
output value** in NPC gold, exactly what an honest solo player gets. Alts add nothing. They also count as members
for holding lots only after 4 distinct days of play.

### A-B1 · "Solo players get a room of their own with bots" and C-B2 · "a stranger can't use the Main; the solo default leaves players alone; nothing draws them together". **Rebutted, with one change.**

Both read B as "Streets of one by default". r3-B's default is the other way round:
- **Open lots are open by default** on every stretch with 2 or fewer active humans (most stretches by week 2, given
  churn). This includes a Street of one, whose two NPC-warmed lots are open.
- **Pick for me** scores +3 per active human on the stretch, so a solo player lands **on someone's open lot**, which
  means on their Main.
- A Street of one is founded only when nothing scores above zero.
- C says "nothing draws them together except an opt-in migration once per chapter". r3-B §6(b) has a lone-human card
  after 7 days, arrivals only into the liveliest districts, and frozen dark districts: the same gravity A has.

**The 200 → 20 server, step by step:**
1. Day 0: 20 humans scattered over 9 districts.
2. Arrivals and returners land only in the 1–3 liveliest districts.
3. Day 7: every lone human (the only lit lot on the stretch) gets one card listing up to 3 lit open lots with matching
   hours ("Jo's Row, plays nights: free move, your crew name and Level come along").
4. By about day 14 the 20 sit on roughly 7 shared Mains in one or two districts. That is A's outcome, reached the same
   way.

**The change (because the first solo of a quiet district really is alone until the second arrives):** the second
solo player in a district is always offered the first one's open lot before a new street, at +5 instead of +3 per
active human. Two Streets of one in a district are each shown one card, once: "Mia's Row is two streets away: join
her Main?". C's point about meaning is fair, so the UI calls a stranger who came through an open lot "new on our
Main" for their 14 days of probation, and a crew member after that.

---

## 2 · My attacks on A and C, after reading their full specs

### On A

| Attack (r3-B) | After r3-A | Status |
| --- | --- | --- |
| A1 · A stranger hogs the Yard, and there is no preference and no removal | A added crew-first in shortages, a 50% cap and a private "not with" | **Fell** |
| A2 · The copy-trade lodger is a gold farm | A's profile: the 14-day median at the old price capped at Fair (1.75) **for 14 days**, then Lodger terms −10% a week | **Stands, smaller** |
| A3 · Seven friends don't fit; every tester loses a parcel | A now takes a station plus both halts for 7–8 (one pool of 8); corner parcels unlock at Levels 35–50, so almost nobody owns one | **Fell** |

**A2, recomputed with the economy exam's T3:**
- G1 buys 1,000 g/min of hub value from Gary at Fair, then quits on day 20.
- The profile keeps paying 1.75 for 14 days: N = 0.75 × 1,000 × 1,440 × 14 ≈ **15.1M ≈ 2.8 days of Gary's income**.
  That scores 1 of 3, and the exam passes at 0.5 days.
- The cycle variant (five alts, one a week): about 75M against Gary's ≈ 190M over five weeks, about **39%** (the
  bound is 5%).
- A's guard ("an alt must first run real buildings for two weeks") only asks the alt to play a casual pattern for
  14 days on a second device.
- **Fix:** NPC terms from the day the profile starts, inside one total NPC cap. That is B's rule.

**New A1 · A stranger makes the friends' project slower.**
- A sizes the Works at "6 h of T(L) × active members", and a stranger who settles in the station is an active member.
- Kettle Works (three friends) gets Nia, a casual solo who delivers 5% of her output. The Workshop stage grows from
  18 to 24 h of T(L) (+33%). The friends carry 95% of a bigger basket, and the stage lands about five days later.
- The ticker can't hide the cause, and the friends think "since Nia moved in, the Workshop crawls". That is Egg,
  Inc.'s sleeper complaint. On top of that, Nia sits behind the crew in every shortage: a two-class station.
- **Fix:** size projects by what members actually *delivered* in the last 7 days, never by head count. That is B's
  revised rule below.

**New A3 · Too many words.**
- In week 1 an A player must understand station, Yard, crew, Lodger, link, Works and Wish to act socially: **7**
  (plus halt for couples, and Depot and socket on screen).
- The outsider exam passes at most 5 in week 1. B revised has 5: crew, street, Main, link, Wish (§3, rule 10).

### On C

| Attack (r3-B) | After r3-C | Status |
| --- | --- | --- |
| C1 · A mates' rate among friends | C fixed the crew price at 1.75 both ways | **Fell** in that part. The capacity part stands: each estate's junction pipe is shared by all its pair links, so Cleo's grey 100/min carries Ben's coal, Ana's energy and a House at once |
| C2 · Held seats keep ghosts in place for up to 120 days | Unchanged: a held asleep estate stays in place, caretaker-run with the autopilot reinvesting up to 30% by default, held 60 days and renewable to 120 | **Stands** |
| C3 · The tier-3 import cap is a solo tax | C dropped it, and Houses sell tier 3 at 2.0 from Era 2 | **Fell** |

C2 needs one correction: the held estate pays for its purchases from its own production, so by the exam's own
measure it isn't an NPC faucet. What remains is still real:
- Every held estate is fully simulated for up to 120 days: the cost of the absent, paid by the present.
- The autopilot spends an absent player's gold on upgrades they never picked.
- A station with two ivy-covered held estates and a House looks full to every newcomer, which is C's own risk 3.

**New C1 · Pairs cost the most fragile players, and the 12-link cap collides with crews of 8.**
- A solo player's offer to a stranger waits up to 72 h for acceptance, so her first human trade depends on someone
  else's timing (A makes the same point).
- C caps an estate at 12 links and says it will cut that to 8 if a re-solve exceeds 20 ms. A crew of 8 has 7 crew
  links per estate before any House, stranger or trunk lane. At 8, the cap would cut crew links: the very friends the
  concept is for.

**New C3 · C keeps the friend-removal vote.**
- C: "Removal needs a majority of the active members (at least 2 votes)", and it applies to invited friends.
- In a crew of three, Ana and Cleo can remove Ben on day 10. He keeps his seat, but the banner leaves his roof in
  front of the whole station, and his crew links turn into ordinary links. That is the kick drama A attacked B for.
- B revised has no votes.

---

## 3 · B, revised: only the rules that change

| # | Rule | Was (r3-B) | Now |
| --- | --- | --- | --- |
| 1 | Sharing on the Main | By need, at most 50% per member | **Usual buyers first:** each buyer's 7-day average share from a seller comes first, new demand gets what is spare, the rest is shared by need. Seller priorities with one tap per good. Supply drops cut usual shares by the same percentage, with a named line. New demand joins after 1 h notice. Each buyer has a 2 h tank on the Main |
| 2 | Main loss | Lossless | Flat by tier: grey 3%, copper 1.5%, steel 0.5% |
| 3 | Removal | Open-lot joiners by majority | **No votes anywhere.** Private pair-mute after 1 h notice and the 2 h tank; the muted side reads a neutral line. Probation (5 days away in the first 14) and moderation remain |
| 4 | Junctions | Majority of each crew | Opened by one active member on each side; each member can switch it off for their own goods |
| 5 | NPC volume | 25% of your output (unclear) | **One total cap for all NPCs together:** they buy at most 25% of your output of a good at 1.25 (tier 1–2, plus inherited tier 3). They sell any tier 1–3 good at 2.0 without a cap (no new gold, only a smaller sink). Parity 0.89 |
| 6 | Successor NPC | 80% of the 14-day median, −10% a week | Inherits only if the leaver played on at least 7 distinct days (otherwise the baseline at once). Falls **linearly from 80% to the baseline over 28 days**, always inside the total cap, so a fake quitter adds nothing above what any solo player gets |
| 7 | Project size | Gate Arch at half the crew's T(L) | Every project is sized by what members **delivered** to the Main in the last 7 days. The Gate Arch's sustained stage is half of that for 6 h. A member who delivers nothing adds nothing to the size |
| 8 | Funnel channels the specs left open (economy exam T1) | Not stated | Exchange listings priced inside 1.0–2.5 × base; accounts on one device can't fill each other's listings; corporation donations capped at 2 h of the donor's T(L) a day; the Handshake is cosmetic (a decoration, no gold); the Host subsidy is capped at 2 h of the newcomer's T(L) |
| 9 | Contracts | Not stated | Commit 9621766 holds: promises up to what your buildings make, penalties capped by the seller's gold. Plus: a resting or asleep seller is never charged a shortfall, and there are no shortfall clauses below Level 6 |
| 10 | Words | Lodger, Junction, Sketch, Crew Order, valve on screen | NPCs keep the existing word **tenant** (named, with a cog); Crew Orders are **orders**; Sketches are **plans** (the existing blueprint word); a Junction is "a link between two Mains"; the valve is a switch on the Main panel. **Week 1: crew, street, Main, link, Wish (5). Season: + Line, lane, Gate Arch, Cistern, Hub Order (10)** |
| 11 | Coupling | Filled from the last solve | **Damped:** each allocation moves 60% of the way to its target per solve (the error shrinks ×0.4 per round), so a ring A → B → C → D → A settles within 1% in 5 rounds without oscillating. One hop per solve, never a cascade |
| 12 | The quiet district's first solo | Street of one if nothing scores | The next solo player is offered the first one's open lot (+5 per active human); two lit Streets of one in a district each get one card to join |
| 13 | Moving after leaving | One free move this chapter | A free move within 24 h, every building kept (crates where the land doesn't fit), as the economy exam's T11 asks |

Unchanged:
- the street and stretch geometry;
- the Main as an N-way teleporter;
- crew price 1.75;
- held lots (28 days for friends who haven't come, 60 days after asleep, then the lot opens by itself);
- the Quiet Isles at 30 days;
- Lines, lanes and the fixed 2% toll;
- the Hub Order (0.6 × T(L), then 0.7, quiet grace);
- the start area (home goods in the street-side parcels 5 and 6 for new lots);
- the migration (nothing moves, no parcel taken);
- about 18–19 days of building.

---

## 4 · If B lost: I would back A, with seven conditions

**Why A rather than C.** A and B make the same bet: a shared, fixed-price carrier (A's Yard pool, B's Main) that needs
no acceptance and no settings.
- A solo player's first human trade happens at minute 12.
- Friends never configure anything.
- No network is ever merged.

C's pairs are the clearer engine, but they put bookkeeping and a 72 h wait on the most fragile players. C also keeps a
friend-removal vote and 120-day ghosts. A's remaining faults are numbers, which are cheap to change. C's are
structure, which isn't.

**What A must take from B:**
1. **NPC terms from day one for a leaver's trades,** inside one total NPC cap of 25% of output, and inherited only
   after 7 days played. This drops the 14 days at Fair (A2: 15M per fake quitter, 39% of income for a five-alt cycle).
2. **"Usual buyers first" in the Yard pool,** with a 1 h notice for new demand, seller priorities and named shortage
   lines. A's Yard has exactly the C-B3 flaw.
3. **Projects sized by delivered output, not head count,** so a stranger or a sleeper can't make the friends' Works
   slower (new A1).
4. **The Yard grows into the crew's Line.** The trunk leaves from the Yard in the members' colours as the crew's
   identity, with the Lines view (a metro diagram) as the late-game map. A has trunks, but as infrastructure, not as
   *the crew's own pipe that grows*.
5. **No votes anywhere,** Junctions included. A already has none: keep it that way.
6. **At most 5 new words in week 1:** no halt, Depot or socket on screen (call them all "station" and "Yard"), and
   "tenant" instead of a new NPC word.
7. **The open funnel channels and damping:** Exchange prices inside the band, capped corporation donations, a
   cosmetic Handshake, and damped coupling (rules 8 and 11), which A doesn't state either.

If the owner weighs friends of 5–8 and the migration most, B's elastic street remains the cleaner fit. But A with
these seven changes is a design I would ship.

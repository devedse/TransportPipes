# Round 4 · The lonely exam, run against A, B and C

Examiner: the guardian of the lonely, the leavers and the returners. Exam: `r3-exam-lonely.md` (6 gates, 18 tests,
52 marks). Specs: `r3-A.md` (Stations with a Yard), `r3-B.md` (Streets & Lines), `r3-C.md` (Places, Crews, Links).

**Verdict:**
- All three pass all six gates.
- On marks, **A 26 · B 26 · C 24** out of 52. On the eight ★ tests that are this exam's core (the sleeping partner,
  the last friend standing, the return, solo, async, guilt), **A 16 · C 14 · B 12** out of 32.
- **The winner is A, Stations with a Yard**, on the tie-break.
- With the 17 changes in §5, A passes every gate and every test except one that no design can pass as written (T9.3).

## 0 · How I marked

- **Silence is a fail**, as announced, for test criteria. A criterion passes only on an explicit rule or number in
  the spec.
- **Gates are judged on what a spec "contains or allows"**, as their table says. A gate fails only if the spec has, or
  lets in, the forbidden thing. A gap in a spec costs marks in the tests, not the gate.
- **The maths uses each spec's own prices.** All three share the ladder:

  | Ring | Price (× the hub's base price) |
  | --- | --- |
  | Hub | buys at 1.0, sells at 2.5 |
  | Crew / Yard / Main / Fair | 1.75 |
  | NPC (Lodger, House) | buys at 1.25, sells at 2.0 |

  Coal's base price is 2.5 g (from C's own example). Income is T(L) from the balance runs. Where a spec gives no
  share, I assume 30% of output traded with other companies, 10% per partner.
- **Tie-break** (decided now, before adding up): total marks, then marks on the ★ tests.
- **The contract fix in commit 9621766** (sellers can only promise what their buildings make, and a penalty never
  takes gold below zero) closes the contract channel of T14 for all three. It does not remove fines for absent
  sellers (T11.4). Only A rules those out.

**Four examiner's corrections, made before scoring and applied to all three specs alike:**

1. **T4.2** says "≤ 110% of hub value" and then "in other words, the farm adds ≤ 2.5%". These contradict each other,
   because every player gets NPC buyers at 1.25 anyway. I score the stated intent: **what the fake quit adds over the
   player's baseline without the alts.**
2. **T14.2** says "two alts add ≤ +15%", but with my own reference (30% traded at 1.75) two *honest* partners already
   add 22.5%. I score its "i.e." clause: **an alt is worth no more than an honest partner of the same size**, and no
   channel gives an alt more.
3. **G4 vs T13.2:** G4 forbids moving anyone before 30 days, while T13.2 asks for probation (moving an idle stranger
   out of an open seat early). The specific rule wins. The narrow probation of T13.2 (a stranger, in their first 14
   days, packed safely with nothing lost) doesn't break G4.
4. **T9.3** (each friend loses ≤ 10% after a falling-out) can't be met when two friends traded 40% of their output with
   each other. Losing a 1.75 buyer for a 1.0–1.25 fallback costs about 18% with any ladder. I keep it as written, so
   all three fail it, and say so in §2.

---

## 1 · The scoreboard

| | A · Stations with a Yard | B · Streets & Lines | C · Places, Crews, Links |
| --- | --- | --- | --- |
| G1 No dead pipe | **pass** | **pass** | **pass** (C1 not named) |
| G2 Solo is a full game | **pass** (≈ 89%) | **pass** (≈ 89%) | **pass** (≈ 89%) |
| G3 Nothing needs two online | **pass** | **pass** | **pass** |
| G4 Absence costs nobody | **pass** | **pass** | **pass** |
| G5 NPCs are honest | **pass** | **pass** | **pass** |
| G6 Nudges vanish | **pass** (×4 grace) | **pass** (off) | **pass** (off) |
| T1 ★ Sleeping partner | 4 | 2 | 0 |
| T2 ★ Last friend standing | 2 | 0 | 4 |
| T3 ★ Case (a) | 2 | 2 | 2 |
| T4 Fake quitter | 0 | 0 | 1 |
| T5 ★ Solo on a server of 5 | 2 | 2 | 2 |
| T6 200 → 20 | 2 | 2 | 1 |
| T7 ★ Week-3 newcomer | 2 | 4 | 4 |
| T8 ★ Never online together | 0 | 0 | 0 |
| T9 Friends fall out | 0 | 0 | 0 |
| T10 Seven friends | 1 | 2 | 0 |
| T11 ★ Return after a month | 2 | 0 | 0 |
| T12 Held seats | 0 | 1 | 2 |
| T13 Squatters and blockers | 1 | 2 | 1 |
| T14 Alts and funnelling | 1 | 2 | 1 |
| T15 ★ Guilt and shame audit | 2 | 2 | 2 |
| T16 Week 4 and next season | 2 | 2 | 2 |
| T17 Migration | 1 | 1 | 1 |
| T18 NPC strength and honesty | 2 | 2 | 1 |
| **Gates passed** | **6 / 6** | **6 / 6** | **6 / 6** |
| **Marks** | **26 / 52** | **26 / 52** | **24 / 52** |
| **★ marks** | **16 / 32** | **12 / 32** | **14 / 32** |

Solo parity, with each spec's numbers:

| | Sells 25% at | Income vs hub only |
| --- | --- | --- |
| Solo (to NPCs) | 1.25 | 0.75 + 0.25 × 1.25 = **1.0625** |
| Crewed (to humans) | 1.75 | 0.75 + 0.25 × 1.75 = **1.1875** |

So solo earns **89.5%** of crewed, before the cheaper NPC inputs (2.0 instead of 2.5). That is above G2's 85% for all
three.

---

## 2 · Gaps all three share (each costs every spec marks)

| Test, criterion | What's missing in A, B and C | The one fix |
| --- | --- | --- |
| T3.6, T8.5, T15.5 | **No push or message budget.** Nothing limits pushes to 1 a day or win-back messages to 3 per absence, and nothing bans guilt or countdown wording | "≤ 1 push a day, in the player's chosen window. To an absent player ≤ 3 messages (days 3, 12 and the next chapter), each with something good ('your coal earned 2,100 g', 'Ana tended your estate')" |
| T8.2 | **Timers.** A states none. B has no offer expiry. C has no Wish deadline | "Offers and invites wait 72 h; Wishes run 12–48 h; no reward window shorter than 24 h" (C's 72 h plus B's 12–48 h) |
| T8.3 | **The Wish result doesn't name who filled it** | "Closed: Ben 60% · Bertha ⚙ 40%" |
| T8.6 | **Tending and favours don't bank** (only Works credit banks) | "Tending banks for 3 days" |
| T5.5 | **The District Wonder isn't sized to active players.** On a server of 5 its stage 1 may never finish | "District Wonder stages are sized like the Works: 6 h of T(L) × active humans, NPCs count 0.5" |
| T9.3 | **A falling-out between two 40% partners costs about 18%** in any of the three (impossible as written, correction 4) | Accept it. A relaxed check (each one stays at or above the solo baseline within 24 h, at most one tap) passes in all three, because NPCs accept links by themselves |
| T11.2 | **No bound on crates when landing** somewhere new | "Your old seat is always a card unless a human took it (0 crates); every card shows its crate count" |
| T11.8 | **Old links aren't offered back on return** | "Relink with Ana?" in one tap after landing |
| T17.2 | **Migration keeps only corridor chains between exactly two gates.** Every tester trade that ran through a merged district network is refunded, and the relation is gone. That is a dead pipe made by the migration itself | "For every pair of human companies with flow between them in the last 7 days, create a Fair link between their nearest gates before refunding the pipes" |

---

## 3 · The three specs, test by test

Marks: ✓ pass · ½ partial · ✗ fail. For every failed criterion: the reason, the section of the spec, and the smallest
change that makes it pass. Shared gaps from §2 are only referenced.

### 3.1 A · Stations with a Yard: 26 / 52, ★ 16

| Test | Mark | Failed criteria and why | Smallest change |
| --- | --- | --- | --- |
| T1 ★ | ✓ 4 | None. "A leaver's trades become a **profile**" at the old price capped at Fair, at 100% of the 14-day median (§5.6). Ana's coal price stays 1.75 at the handover, with one 7-day notice, a Capped/Handed-over line and a waiting link at the seat | — |
| T2 ★ | ½ 2 | **T2.4:** "No member can open a held seat early (rule 3)" (§5.7), so Ana, the only active member, can't open her friends' seats. The rest passes: no drop on days 33/35/37 (profile), days 30–60 average **95%** of the reference, the lone-human card | Any active member may release a hold with one tap; with one active member, she decides |
| T3 ★ | ½ 2 | **T3.4:** Tailwind is named but never defined (no formula, no end), so Dev's catch-up by day 18 can't be shown. **T3.6:** §2 | Copy C §2.7: XP × (1 + days since the chapter started / 14), at most ×2.5, until within 5 Levels of the crew's active median |
| T4 | ✗ 0 | **T4.1 [hard]:** the profile keeps the leaver's price (≤ 1.75) for 14 days after the notice. **T4.2 [hard]:** two alts on two devices that used 800 coal/min each leave profiles paying 1,600 × 2.5 g × 0.75 = **3,000 g/min over the hub**, about 60% of a day-50 income, for 14 days; then Lodger terms per Lodger. Only same-device alts are caught | Keep the old-price profile only for **crew members with ≥ 10 distinct days of play** (not same-device). Everyone else gets NPC terms after the notice, and all NPC purchases from a company stay within **25% of its output of a good in total** (C §2.5) |
| T5 ★ | ½ 2 | **T5.5:** District Wonder sizing (§2). The rest passes: Mia beside Jo by the score, ×4 grace, leagues paused, the lone-human card, the lit-seat check named | §2 |
| T6 | ✓ 2 | None: gravity, frozen districts, the lone-human card, the lit-seat check named | — |
| T7 ★ | ½ 2 | **T7.4:** Tailwind undefined (only "×2.4" in case c, with no end). Everything else passes: a Host or the Frontier, a starter kit, the fair band, no offers for 3 days, credit by own share, leagues by stage | As T3 |
| T8 ★ | ✗ 0 | **T8.2 [hard]:** no expiry or deadline anywhere in A. 8.3, 8.5 and 8.6 as in §2 | §2 |
| T9 | ✗ 0 | **T9.3 [hard]:** correction 4. **T9.5:** "mute" and "not with" are two separate taps, and there's no single block. **T9.7:** "not with" in the Yard has no notice or tank | One "Block" tap that mutes, sets "not with" and refuses gifts and Wishes; "not with" takes effect after 1 h, covered by the 2 h tank |
| T10 | ½ 1 | **T10.1:** seven friends get a station plus two halts, which is three places (one pool and one Works, so the intent is met) | Call a station plus its Street-piped halts one place on the picker ("a big station"); the pool and the Works already are one |
| T11 ★ | ½ 2 | **T11.2, T11.8:** §2. It passes the hard ones: choice from 5 cards (`WakeUp()` fix named), "an absent seller is never charged", the Lodger's welcome postcard, an Era kit | §2 |
| T12 | ✗ 0 | **T12.2 [hard]:** held seats of asleep members "hold by themselves" for 60 days, even when the whole crew sleeps. A crew that stopped on day 10 blocks its seats until day 90–100 | C §2.6: "If no crew member has been active for 14 days, the holds lapse" |
| T13 | ½ 1 | **T13.2:** no probation; an idle stranger in an open seat blocks it for 30 + 7 days. **T13.5:** "spam is rate-limited" with no number | B §2.1 probation (away 5 days in a row in the first 14); requests ≤ 5 an hour |
| T14 | ½ 1 | **T14.3:** the Host's +15% is paid from day 1 (only Host stars wait for 4 distinct days), and same-device Hosts aren't excluded | No Host bonus between same-device accounts; the bonus is paid out once the newcomer has played on 4 distinct days |
| T15 ★ | ½ 2 | **T15.3:** station cards show strangers "lights on, days of the last 7", so a resting resident reads "0 of 7". **T15.5:** §2 | Show buckets instead ("lively", "some days", "resting") |
| T16 | ✓ 2 | None: 2% fixed, no upkeep, hub takeover, public trunks to hubs and the Port, everything persists | — |
| T17 | ½ 1 | **T17.2:** §2. It passes the hard ones: crates and refunds, handover first, nobody moves | §2 |
| T18 | ✓ 2 | None: cog everywhere, never on boards, ≤ 25%, Works level = station's human median − 3, one digest a day | — |

### 3.2 B · Streets & Lines: 26 / 52, ★ 12

| Test | Mark | Failed criteria and why | Smallest change |
| --- | --- | --- | --- |
| T1 ★ | ½ 2 | **T1.4:** at the handover the successor sells 80% of Dara's coal at 2.0 and the hub the other 20% at 2.5, so Ana's coal costs 0.8 × 2.0 + 0.2 × 2.5 = **2.1, +20%** (the limit is 15%). Everything else passes: `Sleep()` hands over every trade (named), one 7-day notice, a status line, links handed to the next human | The successor **sells** 100% of the median at 2.0 for 14 days. Selling to the partner below the hub isn't a faucet, so it can't be farmed |
| T2 ★ | ✗ 0 | **T2.2 [hard]:** each friend's lot goes to Lodger terms the day he sleeps. Ana's income on days 30–60 averages (3 × 100 + 2 × 95.5 + 2 × 91 + 23 × 86.5) / 30 = **88.75%** of the reference, under 90%. **T2.4:** held lots open only by themselves | Give crew members A's 14-day profile at the crew price (with the A fix in T4), and let an active member release a hold |
| T3 ★ | ½ 2 | **T3.6:** §2. The rest passes, including Tailwind ×(1 + day/14) and Ben landing in his held lot in a minute | §2 |
| T4 | ✗ 0 | **T4.2 [hard]:** successors start at 80% of the leaver's median, above the Lodgers' 25% limit. Two alts (1,600 coal/min) leave buyers of 1,280/min where the 25% cap would allow about 500, so (1,280 − 500) × 2.5 g × 0.25 ≈ **490 g/min extra** (≈ 10% of income), decaying slowly. **T4.4:** volume follows the leaver's peak | Successors **buy** inside one total NPC cap: 25% of the partner's output of a good (C §2.5) |
| T5 ★ | ½ 2 | **T5.5:** §2. The rest passes: Pick for me prefers humans and hours, grace is off, the lone-human card, the lit check named | §2 |
| T6 | ✓ 2 | None | — |
| T7 ★ | ✓ 4 | None: a Host crew's open lot, a starter kit, the fixed 1.75, the fair band, Tailwind, Rookies | — |
| T8 ★ | ✗ 0 | **T8.2 [hard]:** no expiry for offers or invites. 8.3, 8.5 and 8.6 as in §2 | §2 |
| T9 | ✗ 0 | **T9.3 [hard]:** correction 4. **T9.5:** pair-mute stops goods, but nothing states a block for messages and Wishes. **T9.7:** pair-mute acts "silently", with no notice or tank | One "Block" tap; pair-mute after 1 h, covered by the Cistern or the tank |
| T10 | ✓ 2 | None: a 4 + 3 stretch on one lossless Main, an 8th fits, couples face each other | — |
| T11 ★ | ✗ 0 | **T11.4 [hard]:** nothing protects an absent seller from contract fines. The 9621766 fix only stops fines at zero gold. Also **T11.2**, **T11.6** (no guaranteed welcome) and **T11.8** | Copy A §4: "Only an active seller who under-delivers pays the difference; an absent seller is never charged"; Lodgers always send a welcome |
| T12 | ½ 1 | **T12.4:** when the whole crew sleeps, B says nothing about where the crew, its Main or its name go, and "next to your crew" has no target | "A sleeping crew keeps its name, Level and heirlooms. The returner's cards include the crew's old stretch" |
| T13 | ✓ 2 | None: seat codes, probation, bundles, a game-laid Main, 5 requests an hour | — |
| T14 | ✓ 2 | None: Auto lock on one device, no Host subsidy, housewarming capped | — |
| T15 ★ | ½ 2 | **T15.5:** §2. Strangers see only today's lit windows (the best of the three) | §2 |
| T16 | ✓ 2 | None | — |
| T17 | ½ 1 | **T17.2:** §2 | §2 |
| T18 | ✓ 2 | None | — |

### 3.3 C · Places, Crews, Links: 24 / 52, ★ 14

| Test | Mark | Failed criteria and why | Smallest change |
| --- | --- | --- | --- |
| T1 ★ | ✗ 0 | **T1.2 [hard]:** C never says that `Sleep()` stops ending contracts; only link volume passes to the successor. **T1.4:** the successor sits inside the 25% House cap, so if Dara supplied all of Ana's coal, 0.25 × 2.0 + 0.75 × 2.5 = **2.375, +36%**. **T1.6:** no state covers the 7 days between packing and the successor ("Capped" is missing) | Name the C1 fix: contracts and links pass to the successor. The successor **sells** the full median at 2.0 (only buying is capped). Add "Capped" |
| T2 ★ | ✓ 4 | None: Ana holds with one tap, held estates keep link prices (no drop), the lone-human card, no day counts | — |
| T3 ★ | ½ 2 | **T3.6:** §2 | §2 |
| T4 | ½ 1 | **T4.3:** the autopilot (on by default for crew members) spends up to 30% of an absent alt's income on upgrades that feed links, so a dormant alt's trade can grow. T4.1, 4.2 and 4.4 pass: House prices, inside the total 25% cap, so a fake quit adds ≈ 0 over the Houses the seat would have had anyway | Drop the autopilot (the caretaker never spends) |
| T5 ★ | ½ 2 | **T5.5:** §2 | §2 |
| T6 | ½ 1 | **T6.4:** held asleep estates "stay in place, caretaker-run" and are fully simulated for up to 120 days. Only House updates freeze | Run a held asleep estate as a profile (A), not a simulation |
| T7 ★ | ✓ 4 | None: Tailwind fully defined, a waiting link, the fair band, the keystone, Rookies | — |
| T8 ★ | ✗ 0 | **T8.2 [hard]:** no Wish deadline (offers do wait 72 h). 8.3, 8.5 and 8.6 as in §2 | §2 |
| T9 | ✗ 0 | **T9.3 [hard]:** correction 4. T9.5 passes (block list) and T9.7 passes (links, 1 h notice, tank) | — |
| T10 | ✗ 0 | **T10.1:** three places. **T10.2:** the second halt's friends use a 68-tile crew pipe (grey: **20% loss** against 3% in the plaza) and deliver to *another* station's Works (pinwheel). **T10.3 [hard]:** no consolidation offer for the 3 who remain, with 4 held ghosts | One crew Works for all its members wherever they live; crew pipes lossless (like B's Main) or charged as plaza distance; a consolidation card when most of a crew sleeps |
| T11 ★ | ✗ 0 | **T11.4 [hard]:** the caretaker "renews contracts", but nothing says an absent seller isn't fined. Also T11.2, T11.6 (no guaranteed welcome) and T11.8 | A's "an absent seller is never charged"; a House always sends a welcome |
| T12 | ✓ 2 | None: any active member holds (60 + 60 days), holds lapse after 14 days with no active member, the crew is people and persists | — |
| T13 | ½ 1 | **T13.2:** no probation | B §2.1 |
| T14 | ½ 1 | **T14.3:** the Host's +15% is paid from day 1, and same-device isn't excluded | As A |
| T15 ★ | ½ 2 | **T15.3:** "lights on 6 of 7 days" on cards, so strangers read "0 of 7" for a resting resident. **T15.5:** §2 | Buckets |
| T16 | ✓ 2 | None | — |
| T17 | ½ 1 | **T17.2:** §2 | §2 |
| T18 | ½ 1 | **T18.6:** no limit on letters | One digest a day per place |

---

## 4 · Ranking and winner

1. **A · Stations with a Yard: 26, ★ 16, the winner.** It is the only spec that is right on the three ★ things that
   make people quit:
   - The sleeping partner (T1: the profile keeps the price).
   - The return (T11: an absent seller is never charged; Lodgers always welcome you).
   - The friend left behind (T2: no drop on the day; 95% over the month).

   It is also the only one with a shared thing a stranger can join in minute 12 without waiting for anyone. That is the
   lonely player's first human trade, and both rivals lose it (B's Main is for members only, C's plaza needs a mutual
   card). Its faults are rules that are **too generous** (the profile price, holds nobody can release, holds that
   outlive the whole crew) or **unwritten** (Tailwind, timers). Each takes a sentence to fix, and the fixes already
   exist in B and C.
2. **B · Streets & Lines: 26, ★ 12.** It has the best guards: probation, no Host subsidy to farm, rate limits, only
   today's lights shown to strangers, the 7-friend shape. But its successors are too stingy for the honest (T2 at
   88.75%) and too generous for the farmer (T4 starts above the cap), and it forgot absent sellers' fines (T11). The
   stranger next to a crew's Main sees a pipe he can never use.
3. **C · Places, Crews, Links: 24, ★ 14.** It has the best held-seat rules (T2, T12) and the best-defined Tailwind and
   NPC cap. But it never names the fix for `Sleep()`, which leaves the contract dead (T1). Its 25% total cap starves
   the honest partner of a leaver (+36% coal). Its autopilot spends absent players' gold. Seven friends end up in two
   different Works with a 20% pipe loss.

**What the ranking doesn't change:** A must take C's hold rules and NPC cap, and B's probation and guards. They are
listed next.

---

## 5 · Must-take changes for A (to pass every gate and every test)

A already passes all six gates. These changes make it pass every test except T9.3 (correction 4). If all are
applied, my estimate is **50 / 52**.

| # | Change | From | Fixes |
| --- | --- | --- | --- |
| 1 | **Holds:** any active crew member holds or releases a seat with one tap (held by default). A hold lasts 60 days from sleep, renewable once by an active member. **All holds lapse when no crew member has been active for 14 days** | C §2.6 | T2.4, T12.2 |
| 2 | **Successor terms, split by direction.** When an NPC **sells** to the partner, it sells 100% of the leaver's 14-day median at 2.0 for 14 days, then −10% a week. When it **buys** from the partner, it pays 1.25, and **all NPCs together buy at most 25% of a company's output of a good** (above that at the hub's 1.0) | C §2.5 for the cap; mine for the split | T4.1, T4.2, T4.4 (with #3); keeps T1.4 and T2 |
| 3 | **The old-price profile only for crew members with ≥ 10 distinct days of play, not on one device**, inside the total cap. Everyone else goes to NPC terms after the notice | Mine, building on A's own same-device rule | T4 without breaking T2 (average stays ≈ 93%, computed in §6) |
| 4 | **Tailwind:** XP × (1 + days since the chapter started / 14), at most ×2.5, until within 5 Levels of the active median of the crew (else the place, else the district) | C §2.7 | T3.4, T7.4 |
| 5 | **Probation:** a stranger in an open seat who is away 5 days in a row within their first 14 is packed early to the Quiet Isles, with nothing lost | B §2.1 | T13.2 |
| 6 | **Timers:** offers and invites wait 72 h; Wishes run 12–48 h and are filled by an NPC at the deadline; the result names who filled what | C §2.3 + B §2.5 + mine | T8.2, T8.3 |
| 7 | **Limits:** link requests ≤ 5 an hour; one "Block" tap mutes, sets "not with" and refuses gifts and Wishes; "not with" takes effect after 1 h, covered by the tank | B/C | T9.5, T9.7, T13.5 |
| 8 | **The Host bonus** is never paid between same-device accounts, and only once the newcomer has played on 4 distinct days | Mine | T14.3 |
| 9 | **Strangers see activity buckets** ("lively", "some days", "resting"), never "0 of 7". Crew mates see "resting" | B §3 in spirit | T15.3 |
| 10 | **A push and message budget:** ≤ 1 push a day in a chosen window; to an absent player ≤ 3 messages, each with something good | Mine (r2-3) | T3.6, T8.5, T15.5 |
| 11 | **Tending banks for 3 days** | Mine | T8.6 |
| 12 | **District Wonder stages sized like the Works** (6 h of T(L) × active humans, NPCs 0.5) | Mine | T5.5 |
| 13 | **Return:** the old seat is always a card unless a human took it (0 crates); every card shows its crate count; "Relink with Ana?" in one tap after landing | Mine | T11.2, T11.8 |
| 14 | **Migration:** before refunding corridor pipes, every human pair with flow in the last 7 days gets a Fair link between their nearest gates | Mine | T17.2 |
| 15 | **A station plus its Street-piped halts is one place** on the picker, the Works and the plaque ("a big station") | A's own pool rule, named | T10.1 |
| 16 | **A consolidation card** when more than half of a crew has slept 14+ days: "Move the 3 of you to a fresh station (crew, credit and heirlooms come along)?" Once, then a button | Mine, extending A's lone-human card | T10.3 in any layout |
| 17 | **Keep A's** "an absent seller is never charged", the profile, Lodger digests, ×4 grace, and the lit-seat `CheckQuietDistricts()` fix | A | Already passing; don't lose them in the merge |

---

## 6 · Decisions in A I would overrule, and why

1. **Rule 3 as applied to seats: "No member can open a held seat early" → overrule.** Releasing the seat of a friend
   who has slept 30+ days isn't a kick. His estate travels safely to the Quiet Isles, his return shows his old seat if
   it's still free and a card next to his crew if not, and nothing he built is lost. Forbidding it traps the friend who
   stayed (T2.4) at a table of sleepers for 60 days, and the whole-crew case (T12) blocks a station until day 90–100.
   The remaining friends must be the ones who decide, as C does. Keep the hold as the default, so nobody *has* to
   press anything.
2. **The 14-day profile at the leaver's price for everyone → overrule** (changes #2 and #3). It is A's best feeling
   for the honest friend, and the best farm in the game for a two-device griefer (≈ 3,000 g/min for 14 days). The
   generosity belongs only where consent and history exist: crew members with ≥ 10 days of play. And it only fits
   inside one total NPC cap. With that, T2 stays at ≈ 93% for the honest Ana:

   | Days 30–60 | Ana's income (vs the reference) |
   | --- | --- |
   | 17 days on the profile, 25% at 1.75 + 5% at 1.0 | 97% |
   | 2 + 2 days stepping | 93%, then 90% |
   | 9 days on Lodger terms within the cap | 86.7% |
   | **Average** | **≈ 93%** |

   Meanwhile T4's farm adds nothing beyond the Houses Rex would have had anyway.
3. **"Held seats hold by themselves" even when nobody is left → overrule** (change #1). Holds are a promise made by
   people who are still playing. When none of them has played for 14 days there is nobody left to keep it for, and a
   newcomer looking at a station of ivy and held signs reads "dead place". That is the empty-restaurant effect, in the
   spec meant to prevent it.
4. **"Lights on, days of the last 7" shown to strangers → overrule** (change #9). The brief wants activity visible
   for choosing. A bucket gives that, and "0 of 7" next to a resting friend's name is a public scolding.
5. **"Same-device accounts trade at Fair" as the anti-funnel rule → keep, but don't count on it.** Couples share a
   PC, so a harder lock would punish real couples. Fair is already the full friend premium, so the real bounds are
   the band, produced goods only, the total NPC cap and the Host gating (#8). The device flag is a signal, not a wall.
6. **The Yard open to strangers → keep, against both rivals' attacks.** C's "a stranger takes a couple's coal" and B's
   "Vex takes 75% of Dev's coal" are already answered in A: crew first in a shortage, nobody takes more than 50% of a
   good's pool while others want it, and a private "not with". It is the one mechanic in the three specs that gives a
   lonely player at 02:40 a human buyer without waiting for anyone. Losing it to "consent by card" (C) or
   "members only" (B) would cost the lonely more than any other single change.
7. **Works sized by lit seats → keep, but watch it.** An active member who never delivers makes the basket bigger
   (6 h of T(L) per active member). That is a small reason to want him gone, the thing T15.6 guards against. If
   testers ever say "Ben makes our Works slower", size it by members who delivered anything in the last 7 days
   (at least 1).

**What I wouldn't take from the rivals:**
- C's autopilot reinvest: a caretaker must never spend someone's gold.
- C's per-pair price knob for crews: B's "friends compare" attack is right.
- B's lossless Main: it removes distance, the thing pipes are about.
- B's majority removal of open-lot joiners: probation covers the idle, and "not with" covers the unwanted, so no vote
  is needed.

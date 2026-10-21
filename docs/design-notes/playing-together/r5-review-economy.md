# R5 · Economy review of `docs/multiplayer-design.html` (v1.2)

**Reviewer:** the economy and long-game examiner.

**Evidence used:**
- the HTML's text;
- `docs/balance/balance-typical.md`: income per day, and the per-resource table at the end of the run;
- the code: `HubMarket.cs` and `GameWorld.Market.cs` (`HubDemand`).

**Format:** findings ranked by damage. Each one gives the section, what's wrong, the replacement text or number, and
the gold it leaks per day where it leaks.

**The fact that changes the most:** the typical bot's income is **98.5% energy**. On day 28 it sells 12,494
energy/min at **0.39 × base**, which is today's price curve, √(demand ÷ own rate), pushing its own price down. That
is 4,873 of its 4,945 g/min. Any hub rule that pays more than about 0.4 for big volumes moves the whole balance
curve. Several numbers in this design (and in my own round-2 proposals) assumed the hub pays about 1.0.

---

## 1 · Ranked findings

### 1. The Hub Order as written doubles late income and breaks the balance curve (§11, decision 5, §24)

**Text:** "The hub takes 0.6 × your typical income per minute of each good at full price; above, it glides to 0.7 at
twice that" and "hub prices reset to base".

**What's wrong:**
- **It doubles late income.** For the day-28 typical estate, the order is 0.6 × 4,930 = 2,958 g/min. Then 2,958 at
  1.0, the next 2,958 gliding (on average 0.85), and the remaining 6,578 at 0.7 make **10,077 g/min against 4,945
  today: ×2.04**. Read as an average price instead of a marginal one, it is ×1.77. XP comes from gold, so Level 30
  moves from day 24–25 to roughly day 17–18. Every number defined as "hours of T(L)" (segments, baskets, the Host
  cap) inflates with it.
- **It barely nudges a normal estate.** A typical week-3 estate with 70% of its income in one good is at 1.17× its
  order and loses **0.25%** to the glide. So the order pushes nobody towards trunks (see finding 7).
- **It is circular and ambiguous.** "Your typical income" is circular if T(L) is re-measured after the change, and
  ambiguous between your own income and T(L). "Glides" doesn't say whether it is the marginal or the average price.

**Replacement (§11, Hub Order row):**

> **The Hub Order** · Today's price curve, made personal: per company and per hub, the hub pays base ×
> clamp(√(D ÷ your sold rate), 0.25, **1.0**), where D is today's per-company demand for that good (Demand ×
> 1.5^(Era−1), × quiet grace) and your sold rate is your own one-hour average. The bar shows D: up to it you get the
> full price, above it every extra unit earns less. Nobody's sales move anybody else's price. *Why:* the balance
> runs are single-company, so they already play this curve; the level pacing stays as tuned, and a second hub
> becomes worth a trunk (finding 7).

**And add to §18 or §11:**

> **T(L)** is a fixed table in `progression.yaml`, from today's typical run (Level 5: 770 g/min · 13: 1,930 · 22:
> 3,050 · 26: 3,770 · 28: 3,870 · 30: 4,830 · 32: 4,930). It is never re-measured. Every "hour of income" in this
> document means 1 h of T(L) at your level, never your own income.

**Note:** raising the cap from 1.25 to 1.0 changes early income a little. Run the balance sims once before step 7
ships.

### 2. Houses buying at 1.25 × base become a +50% faucet for volume goods (§12, §11 solo parity)

**What's wrong:**
- For goods the hub pays 0.39 (energy) or 0.7 for, NPCs buying 25% of the output at 1.25 × base pay 3.2× or 1.8× the
  hub.
- For the day-28 typical energy estate that is **+2,686 g/min against today's prices (+54% of income, 3.9M gold a
  day)**, or +1,718 g/min under the text's Hub Order (2.5M/day).
- It binds as soon as the Yard pipe is steel (5,000/min). The "at most about 6% of output value" bound is computed
  at base price, so it doesn't hold.
- "Supply tables" never say how much a House actually wants, so one programmer will read the cap as the demand.

**Replacement (§12, Prices and One total cap rows):**

> **Prices** · Houses buy at **1.25 × the price the hub pays you for that good right now** (never above 1.25 ×
> base), and sell at 2.0 × base.
>
> **One total cap** · Houses buy only what their works use (their supply table), and all NPCs together buy at most
> 25% of a company's output of a good, …

New gold from NPCs is then at most 0.25 × 25% ≈ **6% of what the hub pays you**, the solo parity of §11 holds at
every price, and an energy farm gains 6%, not 54%.

### 3. The Port at 1.1 beats the hub's full price, without limit (§11, §16)

**What's wrong:**
- 1.1 × base is above the hub's best price (1.0), so every tier-2+ good with a pipe route goes to the Port and the
  Hub Order stops mattering for tier 2+.
- **The leak:** +3% for a typical producer, and for a producer at 3× its order (L29), 8,664 against 7,191 g/min:
  **+1,473 g/min, 2.1M gold a day**, unlimited.

**Replacement (§11, Port row):**

> **The Port** · **0.6 × base** for any tier-2+ good, no limit, by pipe only (public trunks at 2%). The valve for
> very big producers: it beats the hub only once you sell about 2.8× the hub's demand there.

Same in §16 and the price ladder graphic: move the Port to 0.6, below "hub buys 1.0".

### 4. Friend and Premium between "established" alts still funnel about a third of an alt's income (§11)

**What's wrong:**
- The 7-distinct-day, different-device gate costs a farmer 5 minutes a day for a week per alt.
- After that an alt buys the main's goods at Premium 2.125 and resells at the hub, or sells its output to the main at
  Friend 1.375.
- The main gains **≈ 0.33 × the alt's income**: about **1.1M gold a day per alt** in its second week (casual income
  2,350 g/min), and 2.2M a day with two. That is 40% of a day-14 typical income. "An alt is worth at most one real
  partner" is false at Premium.

**Replacement (§11, Link presets row, append):**

> Friend and Premium apply to at most **1 h of the seller's T(L) of goods per pair per day**; beyond that the link
> trades at Fair.

The leak becomes ≤ 0.375 × 60 min × T(L) ≈ **85k a day per pair**: a gesture, not a funnel.

### 5. Quiet grace by district rewards settling in an empty district (§11, Quiet grace row)

**What's wrong:**
- New districts open whenever the old ones are 60% lit (§7), so there is always a district with ≤ 5 lit seats, where
  the Hub Order is ×4.
- Under the text's order, a day-28 volume estate earns **+2,412 g/min (+24%, 3.5M a day)** by moving there. Under the
  √ curve of finding 1 it would be ×2 on price.
- The nudge towards isolation contradicts gravity ("never an all-NPC station").

**Replacement:**

> **Quiet grace** · Demand D ×2 while the **server** has fewer than 50 active players, ×4 below 10. Never per
> district.

### 6. Logged-in alts can still size a stranger's Works (§15 Basket size; §13 "Goals count only…")

**What's wrong:** "residents who delivered **or traded** in the last 3 days". One Yard sale of a single unit makes an
alt count, so Gary's three alts still make Nia's baskets 4× (T12 of my exam; my own round-4 wording was too weak and
was copied). No gold leaks, but Nia's perks come weeks later.

**Replacement (both places):**

> × residents who **delivered at least 30 minutes of their own output to this Works** in the last 3 days (Houses 0.5;
> same-device accounts never).

Also specify the T(L) used: "Σ over those residents of 6 h × T(their level)".

### 7. Week 3: the text does not deliver +15% from day 14 to 21 (§16, decision 11, §24)

**Recomputed for a crew of 2, as written:**

| Source | Day 14 → 21 |
| --- | --- |
| Baseline | +2% |
| Trunk to a second hub (the order is too gentle, finding 1) | ≈ +0.3% |
| Workshop perk | ≈ +5% |
| Discoveries (outposts produce tier 1, which the Port doesn't take) | ≈ +0–3% |
| **Total** | **≈ +7–10%** |

Only an energy farm at 4× its order gains about +9% from a second hub.

**Changes that get there:**
1. The √ Hub Order (finding 1). Splitting a volume good between two hubs pays √2. Recomputed with 5% steel loss and
   the 2% toll, that is **+39% on the split good**. For a member with 70% of income in one volume good: **+27%**.
   A 17-segment trunk costs 34 h of T(L), so the payback is ≈ 5 days (inside 3–7).
2. Add to §16, week 3:

   > **The Foundry Hall** (Works stage 3) opens on day 14, sized so that 2 active residents finish it by day 18–19.
   > Its perk: +10% output of the station's two most-traded chains for the chapter.

   Worth ≈ +6%.
3. Keep Discoveries every 4 days, but make outposts deliver by trunk lanes to the Yard pool, so their ore feeds the
   station's chains (+2–4%).

**Result:** ≈ +15–25% for crews, and +10–20% for a solo using public copper trunks (finding 3 keeps them from
over-paying). That meets the ≥ 15% of decision 11 without touching the solo parity.

### 8. Tailwind counts chapter days, so a newcomer at a chapter start gets nothing (§13 Tailwind)

**What's wrong:** "XP × (1 + days since the chapter started / 14)". In a persistent world, a newcomer on day 1 of
chapter 3 gets ×1.07 against an active median of Level 40+. The week-3 promise (×2.4) only works in chapter 1.

**Replacement:**

> **Tailwind** · XP × min(2.5, 1 + (the active median level of your crew, else station, else district − your level)
> ÷ 10), while you are more than 5 levels below that median.

Same effect in chapter 1 week 3 (gap ≈ 15 → ×2.5), and it now works in every chapter.

### 9. A successor's sales break the 25% sell cap, and nobody gets the profile's gold (§12, §13)

**The contradiction:** §12 says "the House sells 100% of the leaver's 14-day median at 2.0 for 14 days", while the
cap says all NPCs "sell at most 25% of its need".

**The gap:** §13 says asleep held estates "trade as a profile, not simulated". Profile goods are made from nothing.
Two programmers will credit the profile's sales to the asleep owner (gold for conjured goods, for up to 150 days) or
to no one.

**Replacement (§12, after the successor bullets):**

> A successor's or profile's **sales** to the leaver's partners are outside the 25% sell cap: 100% of the 14-day
> median for 14 days, then −10% a week down to the cap. **Profile and successor trades are NPC trades: the gold they
> take in is removed from the game, and an asleep owner earns nothing from a profile.**

**Also correct the farm number:**
- "about 0.26 days of income" can't be reproduced.
- For a 10-day crew alt at the old price inside the cap: 0.5 × 250 g/min × 20,160 min = **2.5M, once ≈ 0.46 day**.
- Without the 10-day rule: ≈ 0.

### 10. The NPC gold target contradicts the NPC gold bound (decision 6, §24 vs §12)

**What's wrong:** §12 bounds NPC gold at about 6% of output, while decision 6 and §24 trigger a change "above 2% of
all faucets". On a server of mostly solo players it lands at 4–6%, so the design trips its own trigger on day one.

**Replacement:**
- decision 6: "New gold from NPCs above **6%** of all faucets";
- §24: "≤ 6% of all faucets | Lower House buying to 1.15 × your hub price".

### 11. The return screen number is wrong, and offline earnings are undecided (§13)

**What's wrong:**
- "Your estate earned 7.8M" is my round-2 savings-jar figure, but this text has no jar, and resting and dormant
  estates run at 100%.
- Ben (1,930 g/min, away from day 3 to day 30) earns ≈ 27 days × 2.78M ≈ **75M, less the caretaker's 10% on days
  14–30 (4.4M): ≈ 71M**. Days 30–36 are a profile and earn nothing.

**Replacement:**
- the screen: "🪙 Your estate earned 71M while you were away (the caretaker took 10% for running it)";
- §13, add: "Resting and dormant estates earn in full; asleep estates (profiles) earn nothing."

### 12. Crew pipes to the halts carry losses nobody stated (§7, §9; my round-4 must-take, missing)

**What's wrong:** a station Yard to its far halt is about 68 tiles. On grey pipe that loses **18.5%** (9.7% on copper),
so the friends in the halts are second-class at the fixed 1.75.

**Replacement (§9, Size bullet):**

> … joined by game-drawn crew pipes **that lose nothing and carry the Yard's tier**.

### 13. Ambiguities two programmers would build differently (replace each)

| Where | Vague | Replacement |
| --- | --- | --- |
| §16 Trunk lines | "2 h of the funder's income" | "2 h of T(L) at the funder's level" |
| §11 Exchange row | "donations at most 1 hour of the donor's income a day" | "1 h of T(L) at the donor's level" |
| §14 Hosts | "capped at 3 h of the newcomer's income … for 7 days … once 4 distinct days" | "for the 7 days after the newcomer's 4th distinct day, capped at 3 h of T(L) at the newcomer's level" |
| §15 Credit | "capped per day" | "at most 2 h of your own output counts per day" |
| §15 Finale | "half the station's output" | "half the summed T(L) of the counted residents (finding 6), for 6 h" |
| §16 Lanes | "Owners' lanes are free; toll split among segment funders" | "A lane pays 2% for every segment it uses that its sender didn't fund, to that segment's funder". As written, funding one segment makes all lanes free: a leak of 2% of carried value, ≈ 86k a day per 3,000 g/min lane |
| §16 Public trunks and hub-run trunks | toll not stated | "Public and hub-run trunks charge 2%, which is removed from the game" |
| §16 Trunks | corridor use not stated | "Trunks, like links, are bundles: no corridor tile can be owned, and a parallel trunk is always possible" |
| §10 Yard | "Your pipe's tier is its capacity, each way" | "… per connection, all goods together; each side's own Yard pipe loss is its own (the seller is paid for what reaches the Depot)" |
| §10 Shortage order | "usual buyers (their 7-day share)" | "usual buyers, each up to its share of this good's Yard deliveries over the last 7 days; no member takes more than 50% of a good's pool supply while others want it". The 50% cap from A and B is missing |
| §11 Exchange | "Use the same presets" | "Exchange orders trade at Fair only; Friend and Premium exist only on links and contracts between established accounts" (anonymous matching can't check the pair) |
| §6, §16 Wonders | "Credit = your own share" (one-pager) against today's Era tiers by share of all deliveries | "Era Wonder personal tiers by the share of your own Era output delivered: Bronze 1%, Silver 3%, Gold 7%, Diamond 15%" |
| §13, decision 7 | the decisions table omits the hold extension | Decision 7: "holds of 60 days, extendable once by an active crew member (at most 150 days away), lapsing after 14 days with nobody active" |

### 14. Contradictions in the text

| Where | What | Replacement |
| --- | --- | --- |
| §7 | "A crew of 7: the NE station and the N halt": that is 6 seats; §9 and §20f say a station plus both halts | "A crew of 7: the NE station and both its halts (N and E), one seat held for an eighth" |
| §8 | Ana's screen says "Your coal now sells to Dev", but Ana is the energy seat | "Your energy now sells to Dev at 1.75" |
| §10 | the Yard diagram puts Vex in the fourth seat, which §8 holds for Dev | Put Vex in another station's diagram, or label the seat "Dev (held)" |

---

## 2 · Funnels and farms against this text

Gold per day at the day-14 typical income (5.4M a day) unless stated.

| Channel | Status in v1.2 | Leak | Fix |
| --- | --- | --- | --- |
| Friend and Premium between established alts | open | **≈ 1.1M/day per alt** | finding 4 |
| Houses at 1.25 × base on volume goods | open | **2.5–3.9M/day per energy estate** from a steel Yard pipe | finding 2 |
| The Port at 1.1, unlimited | open | +3% typical; **≈ 2.1M/day** for a 3× producer | finding 3 |
| Quiet grace per district | open | **≈ 3.5M/day** for a volume estate in the newest district | finding 5 |
| Fake quitter, under 10 days or not crew | closed | ≈ 0 (1.25 inside the cap) | — |
| Fake quitter, 10-day crew alt | small | 2.5M once (0.46 day) | finding 9 (correct the number) |
| Profile sales credited to the asleep owner | undecided | up to ≈ 0.9M/day per asleep estate (100% of its median at 2.0 for 14 days, then decaying) if credited | finding 9 |
| Exchange band | closed if Fair-only | 0 | finding 13 (Exchange row) |
| Hosts | small | ≤ 3 h of T(L) once per alt after 4 distinct days (≈ 0.2M once) | — |
| Housewarming | closed | only towards players below Level 10 | — |
| Works sized by alts | open (griefing, no gold) | stages for a lone stranger take 4× as long | finding 6 |
| Trunk "owners' lanes free" | open | ≈ 86k/day per 3,000 g/min lane | finding 13 |
| Corporation donations | closed | 1 h a day cap | — |
| Contracts | closed (9621766) | 0 | — |

## 3 · My round-4 must-takes that did NOT make it in

1. **Lossless crew pipes to the halts (T14):** missing (finding 12).
2. **Works counting:** it went in as "delivered *or traded*", which is my own round-4 wording and too weak. It needs
   "delivered at least 30 minutes of own output" (finding 6).
3. **The Works stage 3 perk in week 3:** missing (finding 7).
4. **Everything priced in T(L), not "your income":** missing (findings 1 and 13).
5. **Trunks never own corridor tiles:** missing (finding 13).
6. **C's 7-day notice for tenant price changes:** replaced by the 7-day income guarantee. That is acceptable, but the
   guarantee is computed against "the previous day", which is distorted when hub prices reset to base. Use
   "against the average of the last 7 days".
7. **Exchange Fair-only:** went in as "the same presets", which can't be checked on an anonymous order book
   (finding 13).

**What made it in:**
- presets for the Exchange and contracts;
- the gate on Friend and Premium;
- the donation cap;
- tending without a stat buff;
- no autopilot;
- profiles after 30 days;
- damping (once a minute, above 1%);
- NPC ask fills inside the cap;
- same-device accounts never counting;
- the lone-human card on day 5;
- Discoveries every 4 days;
- the +15% trigger;
- the Fair-only newcomer band;
- the "what changed" screen.

**One reversal of my own:** the Hub Order "glide to 0.7" was my round-4 overrule, and it is wrong against the
balance data. It should be the personal √ curve (finding 1).

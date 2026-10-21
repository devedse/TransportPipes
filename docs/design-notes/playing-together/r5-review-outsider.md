# Round 5 · The outsider's review of docs/multiplayer-design.html

I read the text of the document, with the HTML stripped. The section numbers below are the document's own (§1–§24).
This review has five parts:
- **Part A** answers the four questions;
- **Part B** is the ranked list of findings, each with what is wrong and the exact replacement text;
- **Part C** lists the replacement texts for findings 7–25;
- **Part D** gives the new build plan in full;
- **Part E** lists the contradictions in one place.

---

## Part A · The four questions

### A1 · Is it clear? The concepts a player meets

The claim in §6 is "three words in the first 20 minutes: station, crew, Yard". It is false. The picker (§7) is the
very first screen, and its cards already show *halt*, *House ⚙*, *Works*, a waiting *link* ("Mo already buys glass
at this seat's gate") and the activity buckets.

**First hour (20 game words):**
1. station
2. halt
3. crew
4. seat code
5. held seat
6. House ⚙
7. Works
8. link (waiting link)
9. Yard
10. Yard gate
11. Depot
12. Handshake
13. Foundation
14. perk
15. ledger
16. the crew's striped pipe
17. Hub Order (its bar)
18. crew ticker
19. Crew Lens
20. notice board

**First week (about 38):** the 20 above, plus:
- resting, ivy, tending, Ask for…;
- standing orders, "not with", Block;
- postcard, chronicle line;
- leagues, Station Cup, Supplier of the week;
- Host lantern, presets (Friend, Fair, Premium), tank, state line;
- lone-human card, consolidation card, Housewarming crate, starter kit;
- Landmark, stage names, lit windows, usual hours, Frontier.

**Could a new player explain these after 20 minutes?**
- Stations, crews and the Yard: yes.
- Houses: only if Dev's seat was warmed in front of them.
- Links: no. That is fine, because they shouldn't need links yet.

**What can go, or be hidden until needed:**

| Word or feature | What to do with it | Why |
| --- | --- | --- |
| halt | Say "small station · 2 seats" | A second word for the same thing |
| Depot | Call it the Yard | It *is* the Yard |
| Works | Rename "the Yard's project" | Also collides with the example crew "Kettle Works" |
| perk | Say "+5% for everyone" | Plain words do the job |
| ledger | Call it the Yard panel | One less word |
| Crew Lens | Cut | The ring pipe and the Yard panel already show crew flows |
| Crew ticker, chronicle line, weekly postcard | Merge into the morning report and the notice board | Four feeds of the same news |
| Supplier of the week | Cut | A third competition |
| Station Cup | Show only where leagues pause | Two competitions in week 1 |
| Standing orders | Merge into Ask | An Ask that renews itself |
| "not with" | Merge into Block | Two switches for one feeling |
| Host | Hide until week 2 | Not needed in week 1 |
| Presets | Hide until both sides qualify (7 distinct days) | They are locked to Fair until then anyway |

With these cuts, the first hour has **5 words**: station, crew, Yard, House, the Yard's project. The first week has
**11**: those five plus link, Ask, tend, resting, Block, league.

### A2 · The first evening, minute by minute: waits, confusion, boredom

| Minute (§8) | What the text says | The problem |
| --- | --- | --- |
| 0 | "Ana picks a station; four seats light up" | Ben and Cleo get **no say in the station or their own seat**: their land is assigned to them, and the owner asked for choosing a position. Also, the crew card says "Drag your banner onto 4 free seats", but Ana has one banner. Who drags what? |
| 0–3 | "Three balloons land together" | This happens only if all three are present. Otherwise Ana either **waits**, or lands alone and the promise breaks. Cleo, two hours later, can't land "together" |
| 1–3 | "Move in, Done" (2 taps) | Tap 2 contains a **name field**. If it has to be typed, it isn't a tap. And **WhatsApp's and Discord's in-app browsers often forget guests**: when Ben later opens the game in his own browser, his estate is gone, or he makes a second one |
| 3–12 | "Each sees the others' roofs and pipes across the Yard" | At minute 3 there is nothing to see, and on a phone the neighbour's start area isn't on screen. These are 9 minutes alone, which is fine, but don't promise company |
| 12 | "When your own pipe connects, the Handshake plays" | The **first** friend to connect has nobody to shake hands with, so nothing happens at the big moment. The text contradicts its own mock-up, where Ana sees it the next morning |
| 12 | "Your spare energy sells to Ben's mine at 1.75" | Only if Ben's mine needs energy at minute 12. If it doesn't, the **first co-op pipe carries nothing**: the most important moment of the evening is a zero |
| 15–20 | "The Foundation appears; one player alone can finish it in about 15 minutes" | **How do you deliver?** By button, automatically, paid or given away? Unstated (finding 5). If it's automatic, nobody *does* anything, which is boring. And §15 sizes baskets at 6 h × residents, which contradicts "15 minutes" |
| 15–20 | "Everyone who delivered gets the perk" | Contradicts §15: "For everyone in the station" |
| Days 2–7 | (nothing) | The whole first week of co-op is passive: an automatic Yard and an automatic project. The only social verbs are tend, Ask and Block. The friends never **decide** anything together (finding 25) |

### A3 · Buildability: the 8 steps and about 19 days don't hold

| Step | Plan | Realistic | What the plan leaves out |
| --- | --- | --- | --- |
| 1 | 3 | **5** | Guest accounts: anonymous users, claiming, in-app browsers, rate limits, cleaning up abandoned guests (1.5 days alone). The picker cards. Held seats need a warmer, but Houses only arrive in step 4. The `Sleep()` fix hands trades to "a profile or successor House", which are steps 4–5 |
| 2 | 2 | **6** | Auto-routing links through corridors and drawing them as bundles (2 days alone). The offer/accept cards. The migration of today's corridor pipes |
| 3 | 3 | **6** | **Art:** the Yard ground, the Depot and the ring pipe with lamps and arches. Every picture goes through `process.mjs` and `fit.mjs` (CLAUDE.md), and the plan has no art budget at all. The shortage order needs a 7-day history per pair |
| 4 | 2 | **4** | The one total NPC cap needs accounting across Houses, successors and Ask fills. Frozen districts mean freezing the simulation and waking it on landing |
| 5 | 2 | **5** | Profiles are a new representation of an estate. The "10-second timelapse" needs estate history. "At most one push a day" needs push notifications, which the doc never says exist |
| 6 | 2 | **5** | Five stages of project art (Foundation → Landmark). Basket and credit accounting. The finale's tank |
| 7 | 2 | **3** | The Port has no location on the map and no art |
| 8 | 3 | **8** | Trunks paid by several funders segment by segment, 20 lanes with toll splits, pumps, the hub takeover, public trunks, Discoveries, races in brackets, weekly regrouped leagues and the Station Cup. Each of these is a feature of its own |
| | **19** | **about 42** | Plus e2e scripts and balance runs per step (CLAUDE.md: reproduce and check every fix in the browser) |

**Not every step is playable on its own:**
- **Step 1** places crews "next to your crew on today's map". If that isn't one corner 2×2 group, step 3 later splits
  the crew across a station and a halt.
- **Step 1**'s held seats and `Sleep()` fix depend on steps 4–5.
- **Steps 1–6** run with the old global price reaction. Until step 7, a veteran can crash a newcomer's price, which
  §14 promises can't happen.
- **Step 2** falls back to a Yard that only arrives in step 3.

### A4 · My round-4 must-takes that did NOT make it in

| Must-take | Status in the doc |
| --- | --- |
| **M4:** a hold of 60 days that ends by itself, with no renewal to 120 | **Missing.** §13 lets any crew member *extend* a hold once (up to day 150) or *release* it. Release is a button that sends a friend's estate away: a kick by another name |
| **M2:** three words in the first 20 minutes | **Claimed, not delivered.** The picker shows 8 (A1) |
| **S5:** Asks wait 72 h | **Missing.** Asks last 12–48 h |
| **O1:** no per-pair settings inside the crew | **Half done.** Standing orders ("Cleo first") remain |
| **T15, attack 9:** names filtered and reportable | **Missing.** Crew and company names are free text, and the doc has no filter and no report button |

---

## Part B · Ranked findings, each with the exact replacement text

### 1. §10, §6, §11 · The Yard reads as an NPC market that buys everything at 1.75 *(critical)*

- **What is wrong:** "The Depot buys your spare and sells you what you lack, at one fixed price" (§10), and "your
  spare goods sell to the station at one fixed price" (§6). One programmer builds a matcher between residents.
  Another builds a buyer that takes *all* surplus at 1.75. That second build is a faucet worth about 75% of every
  player's surplus, and it breaks every number in §11.
- **Replace §10's second bullet with:**
  > • The Yard is not a buyer. It matches residents: what one resident's buildings lack is filled from another
  > resident's spare, at 1.75× the hub's base, both ways. Spare that no resident needs right now goes on down your
  > flow order (storage, then the hub). The Yard never creates a price for goods nobody wants.
- **Replace §6's sentence with:**
  > Lay your own pipe to the Yard and your spare goods sell to the neighbours in your station who need them, at one
  > fixed price; your factories buy from them the same way. What nobody in the station needs goes to the hub as
  > before.

### 2. §22 · The build plan is about half the real work, and steps depend on later steps

- **What is wrong:** see A3. About 42 days, not 19. There is no art budget. Step 1 depends on steps 4–5. The Hub
  Order ships after the promise it protects. No step says which part of the §21 migration it runs.
- **Replace the table and the line under it** with the build plan in Part D.

### 3. §8 · Guest move-in: lost estates in in-app browsers, and an army of free alt accounts

- **What is wrong:** most invites are opened inside WhatsApp or Discord, whose browsers often forget a guest. A
  guest needs no sign-up, so an alt costs nothing. And every alt rule depends on "same device" (finding 14), which a
  private window resets.
- **Replace the line "The account comes at the end of day 1, when you want to keep it." with:**
  > The name is filled in; changing it is optional. In a normal browser you play as a guest and add an account
  > (e-mail, Google or Apple) at the end of day 1. Inside WhatsApp, Discord or Instagram, whose browsers forget
  > guests, the second screen asks for that account in the same tap ("Keep my estate: Google"), so it is still at
  > most 3 taps. A guest estate that hasn't added an account and hasn't been played for 3 days sleeps like any
  > other. One device can create at most one guest a day. Guests count as new accounts for every rule against alts:
  > Fair price only, no Host bonus, they don't size a project, and none of that changes until they have an account
  > and 7 distinct days of play.

### 4. §13, §15 · "Delivered or traded" counts the absent friend as active

- **What is wrong:** Yard trade is automatic, so a resting Ben "trades" every minute, keeps sizing the basket and is
  never "a sleeping friend who shrinks the next basket". The doc also uses four different activity tests: "lit",
  "active human", "delivered or traded", and "alone on day 5".
- **Add to §7, after the district text:**
  > **Active** (shown as a lit seat): opened the game in the last 3 days. Automatic trade, pipe deliveries and the
  > caretaker's work never count as activity. Every rule in this document that says active, lit or "played" uses
  > this one test.
- **In §13, replace the line beginning "Goals count only people who delivered or traded" with:**
  > Goals count only active residents, so a sleeping friend shrinks the next basket instead of failing it.
- **In §15 (Basket size), replace "residents who delivered or traded in the last 3 days" with:**
  > active residents

### 5. §8 vs §15 · The Foundation's size, who gets the reward, and *how* anyone delivers

- **What is wrong:**
  - The Foundation is "15 minutes for one player", but the basket rule makes it 6 h × residents (18 hours for three
    friends).
  - The reward is "everyone who delivered gets the perk" in §8, but "for everyone in the station" in §15.
  - Delivering is never defined. Is it a button or automatic? Paid or a gift? Two programmers would build opposite
    things.
- **Replace §15's first two table rows with:**
  > Foundation | A fixed basket: 15 minutes of one Level-3 player's output of energy and iron ore (what the
  > tutorial makes), whatever the number of residents. From the Water Tower on: 6 h of typical income × active
  > residents (Houses 0.5; accounts on one device count once). | The first shared moment must be small enough for
  > one evening.
  >
  > Delivering | The project is a buyer in the Yard. It takes what its basket lacks from the residents' spare
  > (after crew mates and usual buyers) and pays 1.0, the hub's price, so delivering never loses money and needs no
  > button. A "To the project first" switch per good puts it ahead of your other Yard buyers. | No chore, no
  > sacrifice, one real choice.
  >
  > Credit | Your share of your own output delivered, at most 2 h of your output a day, bankable for 3 days. | A
  > small or busy player matters as much; a whale can't carry it.
- **In §8 (minute 15–20), replace "everyone who delivered gets the perk" with:**
  > everyone in the station gets +5% production for the chapter

### 6. §12, §5 · The successor sells 100% while the one NPC cap says 25%

- **What is wrong:** "The House sells 100% of the leaver's 14-day median at 2.0", but "All NPCs together … sell at
  most 25% of its need". One of the two has to give.
- **Replace the "Selling to the partner" bullet with:**
  > Selling to the partner: the House sells up to 100% of the leaver's 14-day median at 2.0 for 14 days, then 10%
  > less each week until it is inside the 25% cap. This side creates no gold (the partner pays 2.0 instead of 2.5 at
  > the hub), so it sits outside the cap while it shrinks. Nobody's factory runs dry.

### 7–25 · The rest, ranked

| # | Section | What is wrong | Replacement text |
| --- | --- | --- | --- |
| 7 | §13, decision 7 | Holds can be extended to day 150 and *released* by a crew member. That is the kick button rule 3 forbids, and my M4 wasn't taken | Part C, 7 |
| 8 | §6, §7 | The "three words" claim is false at minute 0: the picker shows 8 game words. "Works" collides with the example crew "Kettle Works" | Part C, 8 |
| 9 | §8 | Friends can't choose their seats, the balloons "land together" means waiting, the first connector gets no Handshake, and the minute-12 pipe may carry nothing | Part C, 9 (a new minute table) |
| 10 | §7 (diagram) | "A crew of 7: the NE station and the N halt" is 6 seats. It contradicts §9 and case f | Part C, 10 |
| 11 | §10 | "Crew first, then usual buyers, then new demand" isn't an algorithm when several sellers feed one pool. Houses' price inside the Yard is undefined (1.75 or 1.25/2.0?). A's "at most 50% per buyer" was dropped | Part C, 11 |
| 12 | §10, §7, §8 | The flagship example breaks the doc's own rules and itself: Vex, a stranger, sits in a station where the crew holds 3 seats (closed by §9, and §8 gave that seat to Dev). Ana is the energy seat but her morning card says "Your coal". Dev's code is "K7-RIVER" for Copper Gulch | Part C, 12 |
| 13 | §10 | "The crew's own trade is drawn as one striped pipe that grows … as the Works grow". But the project belongs to the *station*, which may hold strangers or two crews. Whose pipe is it? | Part C, 13 |
| 14 | §11, §12, §14, §15 | Six rules depend on "same device" and the doc never defines it | Part C, 14 |
| 15 | §7 | "Every district gets one station's worth of new players before the next one opens" is unclear, and nothing guarantees a free 4-seat station for a crew that starts tonight | Part C, 15 |
| 16 | §7, §10 | The 10×10 Yard must hold a Depot, a project growing to a Landmark "visible from the district map", a notice board and four decoratable quarters. It sits on the corridor crossing that link bundles must pass through. The Yard gate is placed nowhere, and the pipe must cross a locked corner parcel. The art can't be made without footprints | Part C, 16 |
| 17 | §16, veto rules | Discoveries are "announced 6 h ahead" with a "first three" marker. That rewards being online at one moment (why the land rush was dropped) and breaks "deadlines at least 12 h" | Part C, 17 |
| 18 | §15, §9 | Asks last 12–48 h (my S5 wasn't taken). Standing orders, "not with" and Block are three switches for two feelings | Part C, 18 |
| 19 | §1 veto rules, §6 rules | "Nobody moved without their own tap" contradicts probation and the day-30 packing. "Absence costs nobody but the absent player" contradicts the successor's 1.75 → 2.0 step. "At most 10% on any day" has no unit | Part C, 19 |
| 20 | §11 | "0.6 × your typical income": is that the T(L) table or your own income? Two different builds | Part C, 20 |
| 21 | §11 vs §23 | "Gold moving between players: never", but corporation donations move gold | Part C, 21 |
| 22 | §21 | The parcel is "refunded" though only 16 of its 64 tiles are taken. The income guarantee's baseline is one day (easy to game, easy to miss). No migration item is tied to a build step | Part C, 22 |
| 23 | §9, §23 | Crew and company names are free text, with no filter and no report (T15, attack 9) | Part C, 23 |
| 24 | §24 | The target "most" can't fail | Part C, 24 |
| 25 | §15 | Week-1 co-op is passive. Friends never decide anything together | Part C, 25 |

---

## Part C · Replacement texts for findings 7–25

**7 · Replace §13's paragraph "Not in a crew: …" with:**
> Not in a crew: on day 30 the estate is packed to the Quiet Isles with its layout, its seat reserved 7 days, then a
> successor House takes the seat with the trades. In a crew: the seat is held by itself for 60 days from day 30 and
> the hold ends by itself on day 90 (the estate travels safely to the Quiet Isles and a successor House takes the
> seat). Nobody can extend or release a hold, so there is no button that sends a friend away. Holds lapse early only
> when no crew member has been active for 14 days.

**Replace decision 7's cell with:**
> 3 / 14 / 23 / 30 days; profiles; a crew member's seat held by itself for 60 days and ending on day 90; no button
> extends or releases it.

**8 · Replace the §6 box "Three words in the first 20 minutes" with:**
> **Five words in the first hour:** station, crew, Yard, House (the ⚙ badge explains itself the first time) and the
> Yard's project. A first-time player's picker card shows only who lives there (names, level, lively / some days /
> resting), the free seats with their land, and Settle here. A two-seat place is shown as "small station · 2 seats".
> Links, Hosts and the project's progress appear on the cards from your second visit. Throughout this document "the
> Works" is renamed **the Yard's project** (the stages keep their names: Foundation, Water Tower, Workshop, Foundry
> Hall, Landmark). Depot, perk, ledger and Crew Lens are not words a player sees: the Yard, "+5% for everyone" and
> the Yard panel replace them, and the Crew Lens is cut.

**Also:** rename the example crew "Kettle Works" to "Kettle & Co." everywhere.

**9 · Replace the §8 minute table with:**
> | Minute | What happens |
> | --- | --- |
> | 0 | Ana taps Start with friends: crew name, emblem, two colours. She picks a station on the map, lands at once on the seat she chose, and the share sheet opens. Every other seat shows the crew's colours, and a House ⚙ keeps it warm. |
> | 1–3 | Ben and Cleo open the link. **Move in** shows the station's free seats with their land, with the seat Ana marked preselected; tapping one is tap 1. **Done** (the name is filled in) is tap 2. Each balloon lands the moment its friend taps; landings in the same minute play together. Nobody waits for anybody. |
> | 3–12 | Loady's chapter 1, each on their own: panels, pipes, the first sale. |
> | 12 | Loady: "Lay a pipe from your network to your Yard gate" (8–16 tiles, paid by the tutorial). At the same moment the Yard's first project, the Foundation, starts buying energy and iron ore at the hub's price, so your first Yard pipe carries something at once, even when you are the only one there. The Handshake plays for you when your pipe and a friend's are both connected, now or whenever the second one connects. The first to connect sees it on their next first screen. |
> | 12–20 | Spare that a friend's buildings need sells to them at 1.75. The Yard panel says it in one line: "Ben's mine buys your energy at 1.75", or "Nobody in the station needs energy yet: the Foundation and the hub take it". |
> | about 20 | The Foundation completes when about 15 minutes of one player's output has arrived, sooner with more. Everyone in the station gets +5% production for the chapter, and a ring of colours shows who helped. |

**10 · Replace the §7 diagram's lines "A crew of 7: …" with:**
> A crew of 5–6: the NE station and the N halt. A crew of 7–8: the NE station and both its halts (N and E), joined
> by game-drawn crew pipes. That makes one big station with one Yard pool (8 sockets) and one project. A free seat in
> a big station is held for the crew or kept by a House; strangers can't take it.

**11 · Replace §10's bullet "Shortage order: …" with:**
> • When a good is short, each seller's spare is shared out in this order: (1) the seller's crew mates, (2) its
> usual buyers, in proportion to what each took over the last 7 days, (3) everyone else, in proportion to need. No
> buyer gets more than half of one seller's spare while others want it. Houses in the station take part at House
> prices (they buy at 1.25 and sell at 2.0, inside the one 25% NPC cap) and always come after humans.

**12 · Example fixes:**
- In §10's diagram, replace "Vex · Stone Quarry (a stranger)" with "Dev · Copper Gulch (moved in on day 8)".
- In the ledger, replace "Vex → Cleo · 2,100 stone" with "Dev → Ben · 2,100 copper".
- Put the stranger case where the doc already has it: Mia and Jo (§14).
- In §8's morning card, replace "Your coal now sells to Dev at 1.75" with "Your energy now sells to Dev at 1.75".
- In §7, replace "K7-RIVER" with "K7-COPPER".

**13 · Replace §10's bullet "The crew's own trade is drawn as one striped pipe …" with:**
> • The ring pipe around the Yard belongs to the station. It is striped in the colours of the crews that live there,
> one stripe per seat (Houses grey), and grows lamps, arches and finally the Landmark with the Yard's project.

**14 · Add a row to §11's table:**
> Same device | Two accounts are "same device" if, in the last 30 days, they used the same browser id (a random id
> the game keeps in the browser) or logged in from the same IP address on the same day. Every rule here that says
> "same device" uses this one test. A false match only costs Friend and Premium prices, Host bonuses and project
> sizing, never anything a player already has. | One test, so no rule can be dodged by another.

**15 · Replace §7's lines "A new district opens … before the next one opens." with:**
> A new district opens when the open districts are 60% lit or held, or when a crew that starts with friends finds no
> station with enough free seats for it. New players fill the free seats of one district before the next district
> gets any.

**16 · Replace §7's "A Yard: 10×10 tiles …" lines and §10's "Nobody builds in the Yard …" bullet with:**
> A Yard is 10×10 tiles: the 2×2 corridor crossing, the four 2×4 corridor arms and a 4×4 corner of each of the four
> estates. Corridor tiles stay corridor: link bundles pass under the Yard in covered channels. The four corners have
> fixed uses:
> - one is the Yard, with a socket per seat;
> - one holds the notice board and the start of the ring pipe;
> - the two opposite corners hold the Yard's project, which grows from a 4×4 Foundation to a Landmark across both,
>   with an arch over the crossing.
>
> Nobody builds or decorates in the Yard. Your Yard gate is on the edge of your start area nearest the Yard; the game
> draws the pipe from it across your locked corner parcel to your socket. Art: every Yard picture is made for these
> footprints and checked with `fit.mjs`.

**17 · Replace §16's Week 3 text from "Discoveries every 4 days …" with:**
> Discoveries every 4 days, announced 24 h ahead and lasting at least 7 days. A Line Race marker goes to the three
> stations in each stage bracket that deliver the most from it in its first 72 hours, so being online when it is
> announced decides nothing.

**18 · Replace §15's "Ask for…" and "Standing orders" bullets with:**
> - **Ask for…** a good, for 72 h (or less if you choose, at least 12 h). Crew mates and neighbours fill it whenever
>   they play; at the deadline a House fills what's left at 2.0, so nobody is "let down". An Ask can renew itself
>   ("Cleo's Glassworks always gets my spare sand first"); that replaces standing orders.

**In §9, merge "not with" into Block:**
> **Block** (private): the Yard stops trading between you two after 1 hour (covered by the tank), and gifts and asks
> between you stop. The other side reads a neutral line. The word "not with" is dropped everywhere.

**19 · In §1, replace "nobody moved without their own tap" with:**
> nobody active is moved without their own tap; only sleeping estates are packed, and they lose nothing

**Replace "at most 10% on any day" with:**
> at most 10% of a partner's income on any day, always with 7 days' notice

**In §6, replace rule 2 with:**
> Absence costs others at most a small, announced step: a sleeping partner's trades keep running for 30 days, then
> change to House terms after 7 days' notice.

**20 · In §11, replace the Hub Order value with:**
> The hub takes 0.6 × T(L) per minute of each good at full price, where T(L) is the balance table's typical income at
> your Company Level (not your own income); above that it glides to 0.7 at twice that amount.

**21 · In §23, replace the gold rule with:**
> Gold moving from one player to another, directly or through prices. (The one exception, corporation donations, at
> most 1 hour of the donor's income a day, goes to the corporation, never to a player.)

**22 · In §21, replace the Yards line with:**
> Yards take only the 4×4 corner of each corner parcel (16 of its 64 tiles). Anything found there goes into a crate,
> the 16 tiles are refunded at the parcel's price per tile, and the rest of the parcel stays the owner's.

**Replace the income-guarantee line with:**
> An income guarantee: after each step that changes trade, anyone whose income over the next 7 days is under 95% of
> their 7-day average before it has the difference paid for those 7 days.

**Add:**
> Each migration item runs in the build step named in §22.

**23 · Add to §9:**
> Crew and company names pass a word filter, and anyone can report one. Three reports from different stations hide
> the name (it shows as "Company 4127") until it is reviewed.

**24 · In §24, replace "most" with:**
> ≥ 60%

**25 · Add to §15, under the Works table:**
> Every stage after the Foundation asks for three goods, and at least one of them is a good that no seat in the
> station has as a home good. Somebody has to choose to build that chain (the keystone, +20%), and the notice board
> shows "Wanted by the Water Tower: glass. Nobody here makes it yet." That is the station's one shared decision of
> the week.

---

## Part D · The build plan (replacement for §22)

> | Step | What ships | Builds on | Days |
> | --- | --- | --- | --- |
> | 1 | **Friends tonight**: crews from Level 1, Start with friends, seat codes, guest move-in (finding 3), held seats warmed by today's tenants, a crew placed into one corner 2×2 group of today's map (the future station), "Ben moved in next door", the picker (people and land only). `Sleep()` stops ending trades (they keep running under today's caretaker), `WakeUp()` shows landing cards, and the quiet-district check counts active seats | Corporations, relocation, Quiet Isles, tenants | 5 |
> | 2 | **Links and the bank**: networks stop at gates, auto-routed corridor bundles, offers, presets, tanks, notice, hub fallback, state lines, damping, the link migration, and the Hub Order replacing the global price reaction (so nobody can crash a newcomer's price from the first week) | Teleporters, trade settings, hub market | 6 |
> | 3 | **Stations and the Yard**: geometry, the Yard's footprint (finding 16), Yard gates and drawn connectors, the matching pool with the shortage order, the Yard panel, the Handshake, the ring pipe (static until step 6), the corner migration. **Art:** Yard ground, Yard building, ring pipe, through `process.mjs` and `fit.mjs` | District layout, crates | 6 |
> | 4 | **Houses**: kinds, prices, the one total cap with its accounting, starter kits, successors split by direction, gravity, frozen districts | Tenants | 4 |
> | 5 | **Absence and return**: profiles, holds that end by themselves, tending, the message budget (in-game report and e-mail only, no web push in v1), the three return screens (the timelapse is 5 stills from the estate's daily snapshots), Tailwind, probation | Caretaker, offline report | 5 |
> | 6 | **The Yard's project and Asks**: stages, the project as a buyer at 1.0, baskets, credit, the finale, Asks with the House fill, the ring pipe's lamps and arches. **Art:** five stages through the art pipeline | Wonder code | 5 |
> | 7 | **The Port, Hosts, the newcomer band**, presets for the Exchange and contracts, donation caps | Contracts | 3 |
> | 8 | **The long game**: trunks (segments by funder, pumps, lanes, the 2% toll, hub takeover), public trunks, Discoveries and Line Races in brackets, leagues by stage | Outposts, leaderboards | 8 |
>
> **About 42 days**, each step deployable and playable on its own, each with its e2e script and a balance run. After
> step 1 (5 days) "three friends tonight" works on today's map. After step 3 their Yard exists. After step 4 the
> lonely player is never alone.
>
> **Cut from v1** (back in if testers ask): the Station Cup, Supplier of the week, the Crew Lens, the chronicle line,
> web push, and the 10-second video.

---

## Part E · Every contradiction in one place

| # | Section | One version | The other version |
| --- | --- | --- | --- |
| 1 | §8 / §15 | The Foundation takes 15 minutes | Baskets are 6 h × residents |
| 2 | §8 / §15 | The perk goes to those who delivered | The perk goes to everyone in the station |
| 3 | §12 | The successor sells 100% | All NPCs sell at most 25% |
| 4 | §13 / §6 rule 3, §9 | Release a hold | Nobody presses a kick-like button; nobody can be removed |
| 5 | §13 / §13 | "Delivered or traded" counts automatic trade | "A sleeping friend shrinks the basket" |
| 6 | §7 / §9, case f | A crew of 7 is a station plus one halt (6 seats) | A crew of 7 is a station plus both halts |
| 7 | §10 / §9 | Vex, a stranger, in a station where the crew holds 3 seats | Such a station is closed to strangers |
| 8 | §10 / §8 | The 4th seat is Vex's | The 4th seat is Dev's |
| 9 | §8 / §10 | Ana's morning card: "Your coal" | Ana is Windy Heights, the energy seat |
| 10 | §6 / §7 | Three words | The picker shows eight |
| 11 | §1 veto / §9, §13 | "Nobody moved without their own tap" | Probation and day-30 packing move estates |
| 12 | §6 rule 2 / §12 | "Absence costs nobody but the absent" | The successor's price step for a crew mate with under 10 days of play |
| 13 | §1 veto / §16 | "Deadlines at least 12 h" | Discoveries announced 6 h ahead with a first-come race |
| 14 | §11 / §23 | Corporation donations | "Gold never moves between players" |
| 15 | §22 step 1 / steps 4–5 | Step 1's held seats and `Sleep()` fix | Houses, profiles and successors arrive later |
| 16 | §14 / §22 step 7 | Nobody can crash a newcomer's price | The Hub Order ships in step 7 |

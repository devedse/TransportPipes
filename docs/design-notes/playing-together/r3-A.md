# Round 3 · Concept A: Stations with a Yard (the definitive spec)

Champion: the player psychologist. This merges r2-5 (mine), r2-3 "No empty tables" (the life of a seat, gravity,
honest links, the return) and r2-6 "Four Seats, a Yard and Honest Pipes" (density first, nobody builds in the Yard,
report first). The economy is r2-4's, scaled to T(L), the typical income at Company Level L (≈800 g/min on day 1,
3,800 on days 14–21, 4,900 on day 28).

Checked in the code and content:
- `Sleep()` ends every trade, and `WakeUp()` lands a returner on the first free seat.
- `CheckQuietDistricts()` skips districts with fewer than 4 players.
- The corner parcels (0, 3, 12, 15) are the **last four** to unlock, at Levels 35–50, while Level 30 is reached in
  week 4.
- Relocation, crates, the Quiet Isles and blueprints already exist.

## 1 · The concept in five sentences

1. You live in a **station**: four estates (or a two-seat **halt**) around a shared **Yard**. You pick it on a map
   that shows who lives there, how lively they are and what the land holds, and friends claim seats together with an
   invite link.
2. Laying your own pipe to the Yard is your consent. From then on your surplus sells to your station's neighbours at
   **one fixed price** and your factories buy from them, every flow named, with no network merged.
3. Everyone else is reached by **two-ended links**, and later by **trunk lines** built with their builders' names on
   them. The hub is the always-there bank, so no partner can stall you.
4. Empty and absent seats are run by labelled NPC **Lodgers**. They keep every trade flowing, hand their works to the
   next human, and step aside when a friend comes back.
5. Each station has **one shared project, the Works**. It grows from a 15-minute foundation to a sustained-flow
   finale, rewards everyone for their own share, and never counts heads.

**Six rules:**
1. Together makes you better, never stuck, guilty or prey (humans add about 10–15%, never access).
2. Absence costs nobody but the absent player's own growth.
3. Nobody presses a button that feels like kicking a friend.
4. Every choice can be undone.
5. Contact between players is positive only.
6. Every zero has a sentence, and every push towards others switches off where there are none.

## 2 · The rules, with the numbers

| Thing | Rule |
| --- | --- |
| **District** | Today's 5×5. The corner 2×2 groups become **stations** (4 seats); the arm pairs become **halts** (2 seats). Still 24 seats |
| **Yard** | A station's plaza: the four inner-corner parcels plus the corridor crossing (18×18). A halt's plaza: one corner parcel of each of its two estates at a crossing. The plaza is game-owned: a **Depot** with one socket per seat, the Works and a notice board. Residents only decorate their own quarter |
| **Yard gate** | A fifth gate at the start-area corner nearest the Yard. You lay the pipe to it from your network (8–16 tiles, the tutorial step at Level 3); the game draws the plaza pipe to your socket |
| **Yard pool** | Teleporter-style: the Depot is one buyer and one seller in each connected estate's own network, filled from the last solve. At most **8 sockets** (a station plus up to two halts joined by game-drawn Street pipes) |
| **Flow order** | Own consumers → contracts → links → **Yard** → storage keeps → hub. Chosen before automatic; a per-good "Yard first" switch |
| **Shortage** | Wish standing orders first, then crew before other residents (the seller can switch this off), then pro rata. No member takes more than 50% of a good's pool supply while others want it |
| **Capacity** | Your plaza pipe's tier (grey 100/min, copper 500, steel 5,000). Upgrading it is your first pipe decision, around day 2–3 |
| **Crew** | 1–8 companies from Level 1, in one station plus up to two adjacent halts: preset emblem, two colours, crew chat, pre-approved links, Crew Lens. Separate wallets |
| **Seat codes** | One per held seat, plus a crew link that needs a member's tap after 24 h, so a posted link can't hand a stranger a friend's seat |
| **Held seats** | For a friend not yet here: 28 days while any member plays. For an asleep member: 60 days. Both hold by themselves |
| **Links** | Gate to gate, two named companies, never transitive, auto-routed through shared corridor bundles (no tile can be owned). The receiver accepts; Lodgers accept by themselves. 2 h gate tank, 1 h notice, fallback to the Yard and then the hub, one status line, trust up to −5% loss |
| **Trunks** | 10-tile segments (about 2 h of the funder's T(L) plus 300 steel and 100 concrete), striped in the funder's colour, with adoptable pump houses. Up to 20 **lanes**, each a link or a delivery to a hub, the Port or a Wonder. **Toll fixed at 2%** to the builders, no upkeep, and the hub takes over once every builder is asleep. Public copper trunks join every hub to its neighbours and the Port |
| **Lit seat** | Played in the last 3 days. A new district opens only when the open ones are 60% lit or held |
| **Moving** | Free in your first 3 days, then free once a week (an hour in transit). Crews move as a block; one extra free move when you leave a crew |

## 3 · What the player sees

**The picker** has three buttons:
- **Start with friends:** a crew in 15 seconds, then a share sheet.
- **Join neighbours** (the default).
- **A station of my own:** Lodgers fill the other seats.

A station card shows:
- the residents (level, **lights on, days of the last 7**, usual hours, Host lantern);
- the free seats with their home goods;
- what the station sells and needs ("buys 1,100 copper a day from the hub");
- the Works stage;
- a **waiting link** at a free seat ("Ana pipes coal to this gate");
- the hub's weekly specialty and the neighbours' banners.

The default score for a seat:
- +3 per active human within 10 levels;
- +2 for matching usual hours, −2 for opposite ones;
- +2 for a waiting link;
- +2 for a Host;
- +1 per Lodger with your missing good;
- −3 for a station with no lit seat while lit ones have room.

"We're 2" picks a halt; "we're 5–8" picks a station plus its halts.

**The Yard** sits at your own estate's corner, on screen whenever you look at your land:
- pellets roll in their sellers' colours;
- the Depot's **ledger** names each pair ("this week: Ben → Ana 12,400 coal");
- the Works shows a ring of this week's contributors (no slot per member) and one "what's missing" line;
- the notice board holds Wishes and the daily chronicle line;
- the **Handshake** plays when your pipe meets a second socket, even one connected yesterday.

**Links** carry a manifest, a tank gauge and one state line:
- *Live:* "Dara buys 1,200 coal/h at 1.75".
- *Partner resting:* "Dara is resting; this link keeps delivering".
- *Caretaker:* "Dara's caretaker keeps this link at the same price".
- *Handed over:* "Old Pete ⚙ keeps buying your coal; terms change on 3 Nov".
- *Capped:* "No one at this gate yet; your coal goes to the Yard and the hub".

**The morning report** starts with people ("Ben's coal kept your Smelter running · Cleo wishes for glass"), then one
line about the world.

**The return** is three screens:
1. **While you were away**, at most 5 items: earnings, the world, **who tended your estate**, your partners, and a
   10-second timelapse.
2. **Your estate is safe**, where you left it if the seat was held.
3. **Where do you land?** Your held seat, your old seat, next to your crew or an old partner, a lively station, or
   the Frontier.

Landing takes a minute. Then come an Era jump kit for each missed Era, Tailwind and a "Back!" ribbon. Lodgers always
send a welcome postcard.

## 4 · The economy

*"Your Yard pays best, partners as you set, Lodgers a little, the hub always."* Prices are × the hub's base price.

| Ring | You sell at | You buy at |
| --- | --- | --- |
| Hub | 1.0 within your Hub Order | 2.5 |
| Lodger ⚙ | 1.25 | 2.0 |
| **Yard** (any human resident) | **1.75 fixed** | **1.75 fixed** |
| Link | Presets: Friend 1.375 · **Fair 1.75** (default) · Premium 2.125 | Same |
| Port | 1.1 for any tier-2+ good, no limit, by pipe route only | — |

**The Hub Order** (r2-4's appetite, framed as a bonus you fill, the way WoW turned XP fatigue into rested XP):
- The hub takes **0.6 × T(L) per minute** of hub value of each good from you, per hub, at full price.
- Above that the price glides to 0.7 at twice the order and stays there.
- **Quiet-district grace:** the order is ×2 with 6–11 lit seats in the district and ×4 with 5 or fewer.
- Nobody's sales move anybody else's price.

**Lodgers:**
- Four archetypes: Reliable, Grower, Collector, Neighbour in need. No Rival.
- Works level = the median level of the station's humans − 3, at least 5.
- Volume at most **25% of their partner's output** of a good.
- Tier 1–3 goods (tier 3 at 2.0 from Era 2, so solo is never walled).
- Never on a board, never more than 25% of a goal, at most one letter digest a day.
- New gold they create: at most ≈6% of a player's output value.

**The Works:**
- Stages are sized to **6 h of T(L) × active members**; Lodgers count as 0.5.
- Contributions are capped per day and can be banked for 3 days.
- **Credit is your share of your own output**: a Level 6 giving 20% equals a Level 30 giving 20%.
- Keystone +20% for the delivery that completes a stage; hub imports count 25%.
- The perk goes to everyone in the station.

**Limits:**
- **Funnelling:** a unit carries at most 1.5 × base above the hub price, and only for goods you made. There are no
  gold gifts. Crates (at most 1 h of the giver's own production) exist only as a housewarming in a newcomer's first
  48 h below Level 10, and as one welcome-back crate per giver. Same-device accounts trade at Fair, don't count for
  goal sizing, and go straight to Lodger terms when they sleep.
- **Contracts:** 1–7 days, auto-renew, a 2 h tank. Only an **active** seller who under-delivers pays the difference
  (capped at 10% of the week's value); an absent seller is never charged. No shortfall clauses below Level 6.
- **Newcomers:** across a 10-level gap they sell at the hub price or more and buy at the hub price +5% or less. No
  offers from players 10+ levels higher in their first 3 days.
- **Hosts** (Level 15+, opt-in): the game pays +15% on what their newcomer sells them for 7 days, capped at 3 h of the
  newcomer's T(L). Host stars need the newcomer at Level 10 on 4 distinct days, and they are cosmetic.

## 5 · The twelve decisions (with what would change them)

1. **Strangers share the Yard: yes, opt-in by laying the pipe.** Same price as crew mates, crew first in a shortage,
   and a private per-company "not with" switch.
   - *Why:* A lonely player's first human trade happens in minute 12 with no request and no wait. Both sides
     consented by choosing the station and laying the pipe. It isn't the old problem: at most 8 named companies,
     nothing flows through anyone's pipes, leaving takes one tap.
   - *Change it if:* in the first fortnight, mixed-station players can't name their buyers (survey), fewer than half
     of the strangers keep their Yard pipe a week, or links outside the Yard stay under 10% of active players by
     day 14. Then strangers would pair by mutual handshake (C's switchboard, on the same solver).
2. **Group size: stations of 4, halts of 2, crews of 1–8.** Couples get halts; 5–8 friends get a station plus halts
   in one pool; corporations stay the non-spatial 30-layer.
   - **Friends who fall out:** leaving the crew is one tap and changes no price, "not with" is private, links pause
     with 1 h notice, a free move. Nobody votes and nobody evicts.
   - *Why:* Dunbar's first layer, today's geometry, a bounded pool.
   - *Change it if:* more than 20% of crews are 5+ (then new districts would use 6-seat stations).
3. **Shared: the Yard pool (an N-way teleporter) and the Works. Never a merged network.**
   - *Why:* The owner's complaint, and the repository's own lesson (a 56-building network solved in 380 ms).
   - *Change it if:* testers say goods "disappear into the Yard". Then the Depot would show and fill explicit pairs,
     on the same solver.
4. **Direct trade only, carried by whoever built the pipe.** Trades are always between two named companies and never
   cross a third company's estate. Trunks carry lanes at a fixed 2%, with no owner-set toll and no upkeep (an upkeep
   bill nobody remembers to pay is Eve's sov-bill trap), and the hub takes over abandoned ones.
   - *Change it if:* there are fewer than 1 trunk per 3 active crews by week 4 (raise the toll to 3%), or tolls exceed
     10% of a veteran's income (lower it).
5. **Prices: the ladder in §4.**
   - *Why:* One sentence and nothing to watch. Banded prices bound what a unit carries, so an alt is worth one real
     partner.
   - *Change it if:* the solo vs crewed income gap falls outside 10–15% (tune Lodger prices and volume), or the human
     trade share falls outside 15–30%.
6. **NPCs: Lodgers ⚙.** A leaver's trades become a **profile**:
   - the 14-day median volume, at the old price capped at Fair, for 14 days;
   - then Lodger terms, shrinking 10% a week to a Lodger's normal size;
   - 7 days' notice before each step.

   **The fake-quitter farm** is bounded: never above 1.75, never more than 14 days at that price, same-device accounts
   go to Lodger terms at once, and an alt must first run real buildings for two weeks.
   - *Change it if:* new gold from profiles exceeds 2% of all faucets, or more than 5% of links lose over 20% of their
     supply at the switch.
7. **Absence:**

   | Days away | What happens |
   | --- | --- |
   | 0–3 | Normal |
   | 3–14, resting | Runs at 100%. The crew sees "resting"; strangers just see dark windows |
   | 14–30, dormant | The caretaker renews contracts at Fair and takes 10%; roles pass to active members; it never builds, spends or demolishes |
   | Day 23 | 7-day notice to everyone linked |
   | Day 30 | **Crew members: asleep in place** (visible, tendable, its trade a profile, not a simulation; seat held 60 days). **Others:** packed to the Quiet Isles, their old seat reserved 7 days, then a successor Lodger takes it with the profile |
   | Day 90 | The held seat ends by itself: packed, successor Lodger |

   No member can open a held seat early (rule 3); there is always room next door, because districts open on demand.
   - *Change it if:* held seats exceed 25% of seats in stations with an active human (then shorten the hold to 30
     days).
8. **Choosing: the picker in §3, with "Join neighbours" as the default.** The lone-human card is shown once after 7
   days as the only lit seat, offering up to 3 livelier stations, with the crew's name, credit and heirlooms moving
   along. After that it is a button.
   - *Change it if:* more than 30% of newcomers move within 3 days (the cards aren't telling enough), or more than 30%
     of solos pick "a station of my own" (then the default isn't trusted).
9. **The testers' world migrates in place, with no wipe (§7).**
10. **Start area: keep 16×16 and the surveyors' ★2 patches**, so every Level-5 chain works alone. For **new
    estates**, the seat's two home goods (★5, ★4) lie in the **first two buyable parcels** (Levels 5 and 7), visible
    from minute one ("your coal ★5 is behind the fence"). Existing estates keep their land.
    - *Why:* A complete first hour, a reason for the first two purchases, and a role that appears once players
      understand chains.
    - *Change it if:* the median time to the first parcel exceeds a day, or the Yard carries less than 5% of output
      on day 1.
11. **The late game:**
    - *Week 2:* Era recipes re-wire the Yard, and the Hub Order nudges one-product estates outwards.
    - *Week 3:* trunks to a second hub (a fresh order), the Port, outposts, and **Discoveries** (6 h notice, lasting
      a week or more) with a **Line Race** marker for the first three.
    - *Week 4:* the Works **finale**: half the station's output value for 6 h, opening by itself when the tank holds an
      hour and pausing instead of failing. The Era Wonder is fed by trunks, drawn as a ledger spine.
    - **Competition:** leagues of about 25 by stage, regrouped weekly with no demotion; the **Station Cup** (the
      station's three best weekly shares of own output, so a quiet member costs nothing); a rotating "Supplier of the
      week".
    - **Next season:** everything persists. The Landmark becomes an heirloom; there is a Line Race to the new Wonder,
      a Homecoming week, a Frontier district, and opt-in crew moves.
    - *Change it if:* crewed income grows less than 5% from day 14 to day 21 (open outposts and trunk lanes earlier).
12. **Performance:**
    - No network crosses a gate, and estates solve alone.
    - Links, the Yard pool and trunk lanes are teleporter entries: O(estates + links + pool members).
    - Asleep estates are profiles; Lodgers are tenants with a build queue.
    - **Districts with no lit seat freeze.**
    - Wonder spines and leagues are ledgers.
    - *Change it if:* a Yard solve exceeds 5 ms at 8 members.

## 6 · The hard cases

| Case | How A passes |
| --- | --- |
| **a. Three friends; a 4th on day 8; Ben quits on day 3, back on day 36** | By minute 5 they live together (Dev's seat held by a code); minute 12 brings the Handshake; minute 20 the Foundation. Ben's absence changes nothing for anyone (resting, then asleep in place on a profile). Dev arrives on day 8 to a starter kit and links, as the copper the crew lacked, with Tailwind. Ben returns on day 36 to "where you left it, tended 17 times" |
| **b. Solo on a 5-player server; a server shrinking from 200 to 20** | Gravity seats Mia beside Jo, and her sand sells to his Glassworks in minute 12. Lodgers fill the rest, the Hub Order gets ×4 grace, leagues pause below 10 actives, and public trunks reach the Port. From 200 to 20: placement into the liveliest districts, the lone-human card, crews moving as a block, frozen districts |
| **c. A newcomer in week 3** | A Host's station with a waiting link (or the Frontier), a starter kit, 1.75 for everything the veterans' Yard wants, the game-paid +15%, newcomer protection, Tailwind ×2.4, and credit by own share |
| **d. A griefer with two alts** | Nothing can be owned, blocked or wrecked (corridors, seats, plaza), hogging and dumping only hit himself, alts trade at Fair with no gifts, a fake quit is bounded (decision 6), spam is rate-limited, and everyone else can mute him, use "not with" or move away for free |
| **e. Two friends fall out on day 10** | Ben leaves the crew in one tap: no price changes and his credit stays. Ana sets "not with Ben" while the Yard keeps trading with everyone else. Links pause after 1 h notice; either may move for free. No vote, no announcement |
| **f. A group of 7** | "We're 7" picks a station plus both halts (8 seats) in a fresh district; Street pipes make one pool; one seat held; corporation beyond 8 |
| **g. Week 4 and the next season** | Decision 11 |
| **h. The testers' migration** | §7 |

## 7 · Migrating the testers' world (no wipe, nobody moves)

1. **Stop the dead pipe first:** `Sleep()` hands trades to a profile or successor Lodger with notice; `WakeUp()` shows
   the landing cards; the quiet-district check counts lit seats.
2. **Links** (TradingRethink A): corridor pipe chains between two gates become links, and the rest are refunded.
   Tenants become Lodgers and keep their trades on Lodger terms.
3. **Stations and halts** are drawn over the existing districts. Plazas take only corner parcels (Levels 35–50).
   Buildings found on one go into crates, and the parcel is refunded in full or given back as the next parcel free.
   Check the database first; it is likely zero or one tester.
4. **Each estate gets its Yard gate,** and Loady shows one card: "Your Yard is ready. Connect?"
5. **No automatic crews.** One card each: "You traded most with X and Y last week: found a crew?". Corporations stay.
6. **Land stays.** Existing specialties count as home goods.
7. **The Hub Order** replaces the global price reaction, and hub prices reset to base.
8. **Players on the Quiet Isles** get the new return flow.
9. **One "what changed" screen,** and a free move for anyone who prefers elsewhere.

## 8 · Build steps (each leaves the game playable)

| Step | What | Builds on | Days |
| --- | --- | --- | --- |
| 1 | No dead pipes: handover in `Sleep()`, trade profiles, link status lines, landing cards, lit-seat district check | Tenants, Quiet Isles, relocation | 2 |
| 2 | Links: networks stop at gates, corridor bundles, presets, tanks, notice, fallback, trust, migration | Teleporters, trade settings | 2 |
| 3 | Stations and Yards: geometry, plazas, Yard gate, Depot pool, ledger, Handshake, picker, corner-parcel migration | District layout, crates | 3 |
| 4 | Crews: start with friends, seat codes, held seats, colours, Crew Lens, leave and "not with", Street pipes, Tailwind, housewarming | Corporations, corp blocks | 2 |
| 5 | Lodgers: archetypes, prices and caps, starter kits, successor terms, placement score, gravity, frozen districts | Tenants | 2 |
| 6 | The Works (stages, finale, keystone, banked caps), Wishes with the NPC fill, tending, report first, the return screens | Wonder code, offline report | 2 |
| 7 | The bank: Hub Order with grace, the Port, public trunks, contracts v2, Hosts, the fair band | Hub market, contracts | 2 |
| 8 | Late game: trunks (segments, lanes, pumps, toll, takeover), Discoveries and the Line Race, leagues and the Station Cup | Outposts, leaderboards | 3 |

**About 18 days.** Steps 1–2 fix today's worst problems on their own, and after step 4 "three friends tonight" works.

---

## 9 · Attacking the rivals

### Concept B · Streets & Lines (r2-1-wild)

**B1. Solo players get a room of their own with bots.**
- **The rule:** "Pick for me" puts a solo on an open lot of an active crew's street, or else founds a **Street of
  one** with two Lodgers. Crews hold *size + 2* lots, and a neighbour who isn't in the crew isn't on the Main, so open
  lots on active streets are rare.
- **Failure scenario:** a server shrinks from 200 to 20. The 20 are mostly solos, each in "Sam's Row", "Mia's Row" or
  "Jo's Row" with two Lodgers. They are lit, in the same district, and share nothing. Their only human contact is a
  link request that waits up to 72 h.
- **Why it fails:** it is WoW's Garrison lesson. Private bases for everyone empty the shared world, and the lonely end
  up in separate empty rooms (r2-3 §3.2). In A, a stranger at Jo's station sells to Jo's Glassworks in minute 12.

**B2. The Main turns a falling-out into a public vote.**
- **The rule:** "A majority of the active members can take a member off the Main." Junctions need majorities of both
  crews.
- **Failure scenario:** on day 10, Ana and Cleo fall out with Ben, and two votes to one take him off the Main. His lot
  still faces the street, the striped crew pipe runs past his gate without stopping, and his coal drops to the hub at
  1.0. Everyone on the district map can see the one lot on Kettle Street that isn't connected.
- **Why it fails:** it is Hay Day's and Egg, Inc.'s kick drama, drawn in pipe. B's own risk list fears it will be used
  "against quiet friends". In A nobody can remove anybody: being in a crew changes no price, and "not with" is
  private.

**B3. A members-only pipe in front of strangers' doors.**
- **The rule:** a street is a corridor with up to 10 lots facing it from both sides, and the crew's Main runs along
  its frontage.
- **Failure scenario:** Kettle Works holds four lots on Ashford Street 2, and Fox & Sons and two solos face the same
  corridor. All day, striped pellets they can't touch roll past their gates, in the corridor lane their own links
  need. The game has drawn a club on their doorstep.
  - Every lot faces four corridors, so "which street am I on?" has four answers.
  - The friend at the far end of a four-lot street is about 68 tiles away on a pipe B makes lossless, which quietly
    removes distance, the one thing pipes are about.
- **Why it fails:** exclusion you see every day breeds resentment (inequity aversion). In A the Yard is a separate
  plaza open to every resident of the station.

### Concept C · Places, Crews, Links (r2-2-friends)

**C1. Strangers share nothing, so the lonely player's first human contact waits for someone else.**
- **The rule:** the plaza "only connects chosen pairs".
- **Failure scenario:** Mia joins Jo's station at 02:40. Jo is asleep, and Mia's offer waits in a tray, so her first
  partners are Houses. Jo accepts at 23:00 the next day. Four seats means up to 6 pair links, each with a preset, a
  tank and a state: bookkeeping in an idle game.
- **Why it fails:** the moment that keeps a solo player depends on another person's timing (checklist 7, for the
  most fragile player). A's pool needs no acceptance, because laying the pipe is the consent on both sides.

**C2. A crew that spans places splits its own project.**
- **The rule:** crews are 1–8 across a station and its halts, but the Works belongs to the *station*, "open to every
  resident".
- **Failure scenario:** six friends take NE (4 seats) and the N halt (2). The Landmark, the finale and the plaque are
  NE's. Dev and Lea in the halt aren't NE residents, so their steel doesn't count there, and they get a two-person
  halt Works instead. They are the second-class fifth and sixth friends C set out to fix (C's own note: "if
  confusing, cap crews at 6").
- **Why it fails:** the place and the people stop being one unit. In A, Street pipes make the halt sockets part of the
  station's pool *and* its Works.

**C3. Held seats keep a full estate running at full terms for up to 120 days.**
- **The rule:** a held seat's asleep estate "stays in place, still run by the caretaker" for 60 days, renewable once,
  with the autopilot reinvest on by default.
- **Failure scenarios:**
  - **The farm:** an alt joins a crew, builds demand at 1.75 for a week and quits. The crew holds the seat, and for
    four months a ghost buys at 1.75 with no decay: the windfall r2-4 §1.3 warned about.
  - **The ghost station:** two ivy-covered held seats and two Houses make NE look full to every newcomer (C's own
    risk 3), and the held estates are still fully simulated.
  - **The caretaker spending:** the caretaker spends Ben's gold on upgrades he never chose.
- **Why it fails:** frozen generosity breeds farms, and simulating the absent costs what the present need. A keeps the
  warmth (the estate stays in place and can be tended) but runs it as a **profile**: capped at Fair for 14 days, then
  Lodger terms with decay, no reinvest, and an end on its own at 60 days.

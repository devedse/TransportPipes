# Cold review: "Playing together" (docs/multiplayer-design.html, v1.2)

I read only this file, once through and then again section by section. I have no other context. Quotes are copied exactly from the document.

---

## 1. What the game is like with friends (5 sentences)

1. Ana founds a crew in a few taps, picks a four-seat station on the district map and sends a share link. Ben and Cleo tap twice, land on the estates next to hers around a small shared "Yard", and a fourth seat is held for Dev and run by an NPC "House" until he arrives.
2. Each friend works through the tutorial alone and then lays one pipe from their network to their Yard gate. From then on, spare goods trade automatically with the others in the station at a fixed 1.75× the hub's base price, which beats the hub for both seller and buyer.
3. Together they feed a station project (the "Works": Foundation, Water Tower, Workshop, Foundry Hall, finale) whose perk they share. They can also post "asks" for goods and "tend" each other's estates once a day, which does nothing beyond the gesture.
4. When someone stops playing, their estate runs on, then a caretaker takes over, then it becomes a frozen "profile" in a held seat, while NPC Houses take over its trades so nobody's factories starve. A returner gets three welcome screens.
5. Later in the month the crew funds trunk pipelines in its colours to other hubs, races to "Discoveries", competes in leagues of 25 and a "Station Cup", and everything carries into the next season.

### Where I had to re-read or guess

- **What the Yard actually does with surplus.** The cover says "your surplus sells to your station at one fixed price", and §10 says "The Depot buys your spare and sells you what you lack". I could not tell whether the Depot buys goods nobody in the station draws. If it does, that is an unlimited faucet at 1.75 against the hub's 1.0. I guessed that it only passes goods between members.
- **The absence timeline.** I read §13 three times. Day 23 notice, day 30 handover, day 30 pack plus a 7-day reserve, "seat held 60 days", "extend once (60 more days)", "the hold ends by itself" at day 90, and §9's separate 28-day hold do not add up to one story.
- **"the caretaker (exists, 10%)".** I could not tell whether that is 10% production, a 10% fee or 10% of something else. The veto rule "at most 10% on any day" has the same problem.
- **The Hub Order formula** ("0.6 × your typical income per minute of each good at full price"). It mixes gold with quantities of goods.
- **The station examples.** I counted seats twice. The Yard diagram puts a stranger (Vex) in the seat held for Dev, and the "crew of 7" legend covers 6 seats.
- **Works baskets.** A Foundation that takes "about 15 minutes" does not fit a basket of "6 h of typical income × residents".
- **Where the Yard gate is.** It sits in corner parcels that unlock "only at Level 35–50", yet I am told to pipe to it at minute 12.
- **How goods get into the Works or fill an ask.** The Works is missing from the flow order in §10.
- **"Relink with Ana? (one tap)".** Did the link die? Does Ana have to accept?
- **"chapter".** It means the tutorial in §8 and a multi-week period in §13 and §15. Is it the same as a season? How long is a season?

### Terms used before they are explained

| Term | First used | Explained |
|---|---|---|
| tenants, `Sleep()`, `WakeUp()`, quiet-district check, district, contracts, shortfall | Contents callout | Never (code and existing systems) |
| held seat, caretaker, asleep in place, Works, trunk lines, Discoveries, leagues | Cover | §9–§16, some never |
| "crewed" player, "offers wait 72 h", "no lost multipliers", "at most 10%" | §1 veto rules | Crew §9, offers §10; multipliers and "10%" never |
| district mega-network | §3 | Never (history the reader doesn't have) |
| frozen district | §4 | §7, inside the SVG legend only |
| Handshake, profiles, usual buyers first, damping, halt, "C's 72-hour card", Main, Line | §5 | Handshake §8, damping §10; "72-hour card" never |
| fake-quitter farm, "the flat week 3" | §5 exam table | §12, §16 ("flat from day 14 to 21") |
| lit | §7 bullet "no lit seat" | Only inside the SVG legend ("played in the last 3 days") |
| Host | §7 default score | §14 |
| heirlooms | §7 lone-human card | §15–16, never properly |
| gate | §7 picker card | §10 |
| Quiet Isles | §9 | Never |
| Loady, chapter | §8 | Never |
| the perk | §8 | Never (what it does) |
| teleporter | §10 | Never |
| tier, tier-2+ good, Era 2, Era jump kit, Era recipes, Era Wonder | §10–§16 | Never |
| Hub Order | §11 price ladder | Table below the ladder |
| the Port, the Exchange, corporation, franchise, toll meters, outposts, surveyors, `TheFirstDay` | §11–§21 | Never |
| Gravity | §14 | Never named before; presumably the §7 default score |
| Frontier | §13 return screen | §14, one clause |
| stage (leagues and brackets), Station Cup, Line Race, Wonder, Landmark, Homecoming week | §14–§16 | Never defined |
| "Works level" (§12) | §12 | Clashes with "the Works" (the station project) |

---

## 2. The owner's principles, scored

| Principle | Score | Why |
|---|---|---|
| Fun with 3 friends who join | **5/10** | The first 3 minutes are well specified. After the Handshake everything is automatic at a fixed price ("Friends think about nothing"), so three friends have nothing to decide or coordinate on day 2, and the only joint act is a tap that "carries no stat bonus". |
| Choosing a station or position | **7/10** | The picker cards show people, activity buckets and land. There is no rule for who takes which seat inside a station, and "position" as a role in the chain is left to emerge from home goods. |
| Lonely players | **5/10** | Houses guarantee a trade partner. But a solo in a House-filled station gets a Works basket about twice a crewed member's, can never compete in a Station Cup that counts "best three shares", and on a small server leagues pause. Most of the late game is closed to them. |
| Players who stop | **6/10** | This is the most worked-out area. It still has three handover dates, three hold rules, a "release" button that works as a kick, and a successor that charges the friend who stays 2.0 instead of 1.75. |
| Early game | **6/10** | The first 20 minutes are concrete and safe to play alone. But the Yard gate sits in land that is locked until Level 35–50, and the build plan ships the Handshake and the Foundation only in steps 3 and 6. |
| Late game | **4/10** | Ten systems are stacked on top of each other (trunks, lanes, tolls, pump adoption, Discoveries, Line Races, leagues, Station Cup, Wonder, finale). Late cooperation mostly means co-funding pipe segments, and most of it needs a population a test phase with a few testers doesn't have. |

---

## 3. The five biggest risks

1. **The crew becomes a label, and the group chat dies by day 4.** The Yard trades by itself at one price and Works credit is an automatic share of your own output. Tending is cosmetic and asks are filled by Houses anyway. Friends never need to talk ("you make glass, I'll make sand"), so playing together feels like playing alone with a 10–15% bonus. The design measures "fun with friends" as an income gap.
2. **Alts.** Guests play with "no account yet", and every guard depends on "same-device" or "different devices", which a second browser profile defeats. The document itself says "An alt is worth at most one real partner". A min-maxer will fill three seats with alts and collect the crewed bonus, Station Cup shares, trunk-toll shares and Host pay.
3. **Drama inside the friend group.** Several features list who did what: a per-person ledger ("Ben → Ana · 12,400 coal"), a ticker that names people ("Dev's copper saves the crew 840 g/h"), a tending counter ("Ana tended your estate 17 times", so Cleo's 0 is implied) and a Cup of "best three shares" (who is the fourth?). Together they are exactly the "list of who didn't deliver" that §23 forbids. On top of that, "any active crew member can … release" a sleeping friend's hold, and the consolidation card moves three friends away from the fourth. Expect "who released my seat?" fights.
4. **Players will find incentives that point the wrong way within a week:**
   - Quiet grace gives ×4 Hub Order to districts with 5 or fewer lit seats, so a crew is paid more for settling away from other people.
   - "An absent seller is never charged a shortfall", so logging out dodges penalties.
   - "Leaving a crew gives one extra free move", so a join/leave loop gives unlimited free moves.
   - You can buy from the Yard at 1.75 and resell on a Premium link at 2.125, and the flow rules don't forbid it.
   - Trunk segments cost "2 h of the funder's income", so cheap alts can fund segments and collect toll shares.
   - Probation packs off the commonest newcomer: the one who plays day 1 and comes back a week later.
5. **A design for 2,000 players meets a few testers.** Most testers' stations will be one human plus three Houses. Leagues (10 actives), the Cup, Line Races in brackets and Hosts never switch on. Testers will judge "multiplayer" by NPCs, while learning about 40 new nouns, five price points plus a gliding Hub Order and four absence stages. The §24 targets ("≥ 60%", "fewer than half the strangers", "20% of crews at 5+") are noise at n≈5.

---

## 4 and 5. Ranked list of concrete changes

Tags: **[DESIGN]** a hole in the fun, **[CONTRA]** two sections disagree, **[DECIDE]** a rule or number is missing or vague, **[EXPLOIT]**, **[CUT]** a simplification.

### Must fix before building

**1. [DESIGN] §15, §10, §11: give friends one thing to decide together.**
Problem: "Friends think about nothing; strangers can't be fleeced; no price war inside a station." (§11). After minute 12 nothing needs a conversation.
Change §15 "The station's Works" intro to:
> "Each Works stage is a recipe drawn from the station's home goods: one part per seat, picked by the game from what the four seats' land holds and shown on the stage card ('Water Tower: steel · glass · concrete · bricks'). Anyone may deliver any part. A part still open 72 h after the stage starts is filled by a House at 2.0, inside the 25% NPC cap. Beyond the cap the stage waits; it never fails."

Add to §10:
> "The Plan board on the Yard fence: each member pins 'I make X' and 'I need Y' (one tap per good). A pinned need gets usual-buyer priority for 7 days, and pins show on the station's card in the picker."

Replace "Friends think about nothing" with:
> "Friends never haggle; they decide who makes what."

**2. [CONTRA] §13, §15: resting estates still count in baskets, and solo players carry double.**
Problem: "Goals count only people who delivered or traded in the last 3 days, so a sleeping friend shrinks the next basket". But a resting estate "runs at 100%" and trades in the Yard automatically, so it counts for 14 days. The basket is "6 h of typical income × residents … (Houses 0.5)" with Houses capped at "never more than 25% of any shared goal". A solo with 3 Houses therefore gets 6 × 2.5 = 15 h, of which Houses may fill 3.75 h, leaving 11.25 h for the human, against 6 h for each crewed member. §8 also says the Foundation takes "about 15 minutes".
Change the "Basket size" row to:
> "6 h of typical income × humans who opened the game in the last 3 days (automatic trade doesn't count; Houses count 0). Houses add their own deliveries on top, up to 25% of the basket. The Foundation is fixed at 15 minutes of a Level-3 income per such human."

**3. [CONTRA] §13, §12, §9, §19 #7: one absence timeline.**
Problems:
- The SVG notice at "23" implies a handover on day 30.
- "on day 30 the estate is packed … its seat reserved 7 days, then a successor House takes the seat" implies day 37.
- §12 says "When an estate goes to sleep and its seat isn't held, a successor House takes over its trades after a 7-day notice", which is again ambiguous.
- The table row "30+ · asleep, held … its trades at the rules of §12" applies the not-held rules to held seats.
- Holds appear as "up to 28 days", "seat held 60 days", "extend a hold once (60 more days)", "the hold ends by itself" and "holds of 60 days decided by the active crew".
- "(exists, 10%)" and "at most 10% on any day" are undefined.

Replace the SVG note and the table with one table:
> "Day 0–3: playing. Day 3–14: resting, the estate runs at 100%. Day 14–30: caretaker, the estate runs at [state %] and renews contracts at Fair; on day 23 partners get a 7-day notice. Day 30: every trade goes to a successor House (§12), held seat or not. Crew member: the estate stays in place as a profile, its seat held to day 90; any member can extend it once, to day 150; holds lapse 14 days after the last crew member played. Not in a crew: the estate is packed to the Quiet Isles on day 30 and the seat opens the same day. A seat held for a friend who never arrived opens on day 28 to the crew's choice of a House or strangers."

Replace the veto line with:
> "Absence costs nobody else: a partner's supply from an absent player never drops more than 10% in any 24 h and never costs more per unit than before the absence."

**4. [CONTRA] §12: the successor charges the friend who stays.**
Problem: "Selling to the partner: the House sells 100% of the leaver's 14-day median at 2.0 for 14 days". That is 14% dearer than 1.75 and breaks rule 2, "Absence costs nobody but the absent player's own growth". The farm only runs in the buying direction (NPC gold flowing to the partner). A House selling to the partner is a gold sink and mints nothing.
Change to:
> "Selling to the partner: the House sells 100% of the leaver's 14-day median at the old price (at most 1.75) for 14 days, then 10% less volume each week at 1.75. Buying from the partner: at 1.25, inside the 25% cap; only here does the 10-day crew-member exception apply."

**5. [CONTRA] §13 vs §9 and §6: "release" is a kick button.**
Problem: "Any active crew member can extend a hold once (60 more days) or release it" contradicts "Nobody can be removed." and "Nobody presses a button that feels like kicking a friend." It also leaves open what happens when two members disagree.
Change to:
> "A hold ends only by the timer or by the absent player's own tap. Any member can extend it once; nobody can shorten it."

**6. [CONTRA] §9, §10, §13, §16 vs §23: lists of who did and didn't deliver.**
Problems: "a crew ticker ('Dev's copper saves the crew 840 g/h')", ledger rows "Ben → Ana · 12,400 coal this week", "Ana tended your estate 17 times" and "best three shares", against §23's "A vote, a kick, or a list of who didn't deliver."
Changes:
- §10 ledger: "The ledger shows your own flows by partner, and the station's totals by good. Other members' flows to each other are not listed."
- §9 ticker: "The crew ticker names goods and totals, never a person: 'the crew's copper saves the crew 840 g/h'."
- §13 return screen: "Your crew looked after your estate" (no counts, no names).
- §16 Station Cup: count "the station's total own-output share divided by its lit humans", or cut it (item 22).

**7. [EXPLOIT] §8, §11, §12, §14, §15: alts.**
Problems: "Tap 1 · no account yet, no level gate". "An alt is worth at most one real partner." Every guard is "same-device" or "different devices", which a browser can't detect reliably.
Add to §11:
> "Resident status. A company counts as a Yard member, a Works resident, a Station Cup share or a trunk funder only once it has a verified account and 3 distinct days of play. A guest who came through a seat code trades in that Yard from minute one. Until verified, its trades with the code's issuer count at Fair and it adds nothing to baskets, cups or tolls. One verified account per email; 'same device' is a second signal, never the only one."

Replace "An alt is worth at most one real partner." with:
> "An alt is worth at most one partner's Yard trade (about 4% of the main's income), and nothing in baskets, cups, tolls or Host pay."

**8. [DECIDE] §10, cover: what the Depot does with surplus, and who sells first.**
Problems: "The Depot buys your spare and sells you what you lack, at one fixed price" leaves open whether it buys spare nobody draws (which would be a 1.75 faucet). "Shortage order: your crew first, then usual buyers" orders buyers only; there is no rule for when sellers outnumber demand.
Change to:
> "The Depot never holds stock and never buys for itself: it passes goods only from members' spare to members' current draw. Spare that no member draws flows on to storage and the hub at hub prices. When sellers offer more than buyers draw, the draw is split among sellers: crew first, then by each seller's 7-day share, then in proportion to spare."

Also add to §10's flow order, which is missing the Works and asks:
> "own consumers → contracts → open asks → the Works → links → the Yard → storage keeps → hub."

**9. [EXPLOIT] §11: quiet grace pays crews to avoid people.**
Problem: "The Hub Order ×2 with 6–11 lit seats in the district, ×4 with 5 or fewer". A crew of 4 in a freshly opened district gets the Yard and ×4; the same crew in a lively district gets ×1.
Change to:
> "Quiet grace counts the lit humans in your own station other than you: ×1 with two or more, ×2 with one, ×4 with none. Solo parity (§11) is measured with quiet grace included."

**10. [CONTRA] §22 vs §8: step 1 doesn't deliver "three friends tonight".**
Problem: "After step 1 'three friends tonight' works" and "This is it, and it's the first thing we build". But minute 12 (Yard, Handshake) is step 3 and minutes 15–20 (Foundation) are step 6. Step 1 also ships "the picker", whose cards show stations, halts and Works that don't exist until steps 3 and 6. "About 19 days" for 8 large steps comes with no exit criteria.
Change to:
> "Step 1: the first 3 minutes of §8 (crews, seat codes, move-in, placement next to your crew, 'moved in next door'). Step 2: minutes 12–20 on today's districts (one Yard per district quarter, Yard gates, the Handshake, the Foundation). Station geometry and the picker cards come after. Every step ends with an e2e script that plays §8 with four browsers."

**11. [DECIDE] new §0 "What the testers will see".**
Problem: §1 says it "Works with 5 players and with 2,000", but leagues, the Cup, Line Race brackets, Hosts, gravity and every §24 percentage need a population that doesn't exist.
Add:
> "Small mode (below 50 active companies): one district; no leagues, Station Cup, Line Race brackets or Hosts; Houses fill every empty seat; each §24 measure is read from an interview with every tester, not from percentages."

**12. [CONTRA] §7, §8, §10, §20: the worked examples contradict the rules.**
- §10's Yard diagram has "Vex · Stone Quarry (a stranger)" in the station whose fourth seat §8 says is held for Dev, and §9 says "A station where one crew holds 3 or more seats is closed to strangers by default". Fix: "Dev · Copper Gulch (seat held; a House warms it)".
- §7 legend "A crew of 7: the NE station and the N halt" covers 6 seats, but §20f says "a station plus both halts … one seat held for the 8th". Fix: "A crew of 7: the NE station and the N and E halts (8 seats, one held)", and draw the second crew pipe.
- §20a "On day 30 Ben's estate sleeps in place", but Ben left on day 3. Fix: "On day 33".
- §8 Ana's screen "Your coal now sells to Dev at 1.75": Ana lives at Windy Heights and Dev already has coal ★4. Fix: "Your energy now sells to Dev at 1.75".

**13. [DECIDE] §7, §8: the Yard gate sits in locked land.**
Problem: The Yard takes "a 4×4 corner from each estate (1.6% of its land; the corner parcels unlock only at Level 35–50)", yet at minute 12 you "lay a pipe from your network to the Yard gate" of "8–16 tiles".
Change to:
> "Your Yard gate sits on the edge of your start area nearest the Yard. The game lays a fixed gate strip from there through locked parcels to the Yard; you lay only the pipe from your network to the strip, and nothing can be built beside it."

**14. [CONTRA, DECIDE] §8 vs §15: the perk and credit.**
Problem: "everyone who delivered gets the perk" (§8) against "The perk | For everyone in the station" (§15). What any perk does, and what "Credit" buys, is never stated.
Change to:
> "Every stage's perk goes to every seat in the station, delivered or not: Foundation [effect], Water Tower [effect], Workshop [effect], Foundry Hall [effect]; the finale leaves the Landmark. Credit buys nothing: it is the number that moves with you and feeds the Cup."

Fill in each [effect] before building.

**15. [CONTRA] §1 veto vs §7, §9, §13: "nobody moved without their own tap".**
Problem: Probation ("packed early to the Quiet Isles"), the day-30 pack, and "a crew moves as a block" all move people without their tap.
Change the veto line to:
> "Nobody is moved while they play. An absent player is moved only to the Quiet Isles, with everything kept, and chooses where to land on return. A crew move takes along only members who tap 'come along' within 72 h."

**16. [CONTRA] §23 vs §16, §11: the gold rule.**
Problem: "Gold moving between players, directly or through prices." But every Yard sale moves gold through a price, and "spare lanes rent at a game-fixed 2% toll split among the segment funders" moves gold for no goods.
Change §23 to:
> "Gold moving between players other than as payment for goods really made, at a preset price."

Then cut tolls (item 22).

**17. [DECIDE] §13: relinking on return.**
Problem: "Relink with Ana? (one tap)". It is not said whether Ana must accept (offers wait 72 h), at what price, or what happens to the successor House.
Change to:
> "On return, every link a successor House took over comes back to you on its old terms the moment you land in your seat. Partners read 'Ben is back'. Nothing to accept."

**18. [DECIDE] §11: Hub Order units.**
Problem: "The hub takes 0.6 × your typical income per minute of each good at full price; above, it glides to 0.7 at twice that" mixes gold and goods, and the window for "typical" is not given.
Change to:
> "For each good, the hub buys at 1.0 up to 60% of your median production of that good per minute over your last 24 h of play. From 60% to 120% the price falls linearly to 0.7; above that it stays at 0.7."

**19. [DECIDE] glossary and renames.**
Add a one-page glossary before §6 covering: lit, station, halt, crew, Yard, Yard gate, Depot, Handshake, link, House, profile, caretaker, held seat, Quiet Isles, Frontier, Works, perk, credit, Hub Order, Port, Era, tier, Loady, gravity, stage, season.
Renames:
- In §13 and §15, replace "chapter" with "season", and state the season's length.
- In §12, replace "Works level" with "building level".
- In §12, rewrite "(tier 3 from Era 2, at 2.0)" as "(tier-3 goods only from Era 2, sold at 2.0)".

### Cuts (Q5): simpler with the same fun

**20. [CUT] §2, §4, §5, the cover byline and the "Found along the way" callout.** Move them to an appendix. Four of the 24 sections and the cover describe how agents argued, and the callout is a code changelog. A builder needs rules, not the debate. Cover line:
> "How three friends who join tonight play together, how a lonely player is never alone, and what happens when people stop and come back."

**21. [CUT] Halts, big stations and crew pipes (§7, §9, §19 #2).**
> "Crews of 1–4 live in one station; a couple takes a station with two Houses. Crews of 5–8 and halts come when more than 20% of crews ask for a fifth seat."

This removes a second Yard type, game-drawn crew pipes, "only shown to couples" and the crew-of-7 errors.

**22. [CUT] The late-game stack (§16).** Keep public trunks, your own pipe to a second hub or the Port, Discoveries and the Works finale. Cut lanes, tolls, segment funding, pump adoption ("can be adopted" is never defined), the Station Cup, Supplier of the week, Line Race brackets and the separate Era Wonder. Fold the Wonder into the finale. Replace:
> "Trunk lines: anyone can lay a steel pipe to another hub or the Port along the public corridor; it carries only their own goods and their links, in their colours."

**23. [CUT] Friend and Premium link presets (§11).**
> "Every player-to-player price is 1.75: the Yard, links, the Exchange and contracts."

This removes the "7+ distinct days on different devices" rule, the 10-level protection rule and the Yard-buy/Premium-resell arbitrage.

**24. [CUT] Hosts (§14).** A +15% bonus for 7 days, capped at 3 h of the newcomer's income, needs four anti-alt conditions. "Find me neighbours" already seats newcomers next to active humans.

**25. [CUT] Probation (§9).** It is redundant. A crew's seats are already protected by seat codes and the 3-seat closure, so probation only punishes the casual newcomer. Delete the sentence that begins "Otherwise free seats are open, and a stranger who takes one…".

**26. [CUT] Tending (§14, §15), or give it a function.** As written it is a daily chore with a counter and no effect. Replace it with:
> "Thank: one tap on any ledger row you received; the sender sees 'Ben thanked you for coal'."

**27. [CUT] Housewarming (§11), Keystone (§15) and the consolidation card (§7).** Merge the consolidation card into the lone-human card:
> "When no other human in your station has played for 5 days, a card offers up to 3 livelier stations; your crew and credit move along."

**28. [CUT] The 7-term placement score (§7).** It refers to Hosts before they are defined, and "matching usual hours" contradicts the async premise. Replace with:
> "Find me neighbours seats you in the station with the most humans lit in the last 3 days within 10 levels of you; ties go to a station with a House that makes a good you lack."

### Smaller holes

**29. [CONTRA] §15 vs §12: asks.** "at the deadline a House fills what's left at 2.0, so nobody is ever 'let down'", but "Their fills of expired asks count inside the cap": at the cap, the ask goes unfilled. Add:
> "An ask is limited to what the cap leaves; the ask screen shows that limit before you post."

**30. [EXPLOIT] §11: absent sellers.** "an absent seller is never charged a shortfall". Add:
> "Absent = no session for 72 h before the shortfall. The contract pauses while the seller is absent (the buyer's tank and fallback cover it), so logging out never beats delivering."

**31. [EXPLOIT] §7: moves.** In "leaving a crew gives one extra free move", add: "(at most once per 30 days)".

**32. [EXPLOIT] §16: Discoveries.** "a Line Race marker for the first three stations to pipe to them", announced 6 h ahead, rewards being online at a given moment, which is the land-rush failure §4 dropped. Change to:
> "Every station that pipes to a Discovery within 72 h gets the marker; the first three are named on it."

**33. [CONTRA] §7: district opening.** "A new district opens when the open ones are 60% lit … or held, … and every district gets one station's worth of new players before the next one opens" gives two triggers. Change to:
> "A new district opens when no open district has a fully free station, or when the open ones are 60% lit or held, whichever comes first."

**34. [DECIDE] §24: a vague target.** "Players who can name who buys their goods (survey) | most". Change to: "≥ 3 of every 4 asked".

**35. [DECIDE] §16: the season.** The season's length, and what resets besides the Works, are never stated. "Everything persists: crews, estates, stations, trunks, held seats" means a newcomer in season 3 starts against permanent veterans. State:
> "A season is [N] weeks; levels and estates persist; leagues, the Works stage and Discoveries reset."

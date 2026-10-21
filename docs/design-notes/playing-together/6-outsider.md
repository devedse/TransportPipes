# Playing together in TransportPipes: an outsider's brainstorm

Written without reading anything about the game, only the brief: a cosy, browser-based multiplayer idle game.
Players build production chains on their own land, connect them with pipes, sell goods and grow, in one
persistent world over seasons of a few weeks, for a few minutes a day.

---

## 1. What a design like this must take into account

**Who plays**
- Group sizes: solo, pairs, the "4 friends" case, bigger clubs (10–30), total strangers.
- Friends who start together, friends who join a week late, one friend who is much more active than the others.
- Players with very different skill: an optimiser next to someone who just likes watching pipes.
- Lonely players: nobody to invite, or a server that's quiet in their time zone.
- Language barriers, accessibility, phone vs desktop, touch input.

**Time**
- Asynchronous by default. Friends are almost never online at the same moment, so nothing may *need* them to be.
- Sessions of a few minutes: every social action has to fit into 30 seconds (one tap to help, one tap to answer).
- Offline progress: what keeps running while you're away, the cap on it, and how it is calculated.
- Time zones: events, deadlines and season ends must not favour one region.
- Season rhythm: day 1 rush, mid-season plateau, the end-of-season crunch, the reset.

**Absence and coming back**
- Players stop: for a weekend, for two weeks, forever. The game can't tell which, at first.
- Dependency risk: if my chain needs your goods, your absence breaks my game. That is the main thing to design against.
- Guilt and obligation: "my friends need me to log in" is poison for a cosy idle game. Social pull is fine, social debt isn't.
- Dead land: abandoned estates clutter the world, block neighbours and make the world feel empty.
- Coming back: catching up, a "while you were away" story, no shame, a reason to stay.
- Seats: can someone else take an empty station? Can the original player get it back?

**Early vs late game**
- Early: the first 10 minutes must be fun alone. Co-op should be *offered*, not required, and simple.
- Late: optimisers need goals that only a group (or the world) can reach, and sinks for their surplus.
- Late joiners must not be useless: a newcomer's small output must still count.
- Power gap between a week-3 veteran and a day-1 newcomer who are neighbours or in the same crew.
- Season reset: what carries over (cosmetics, crew, name, reputation) and what resets (land, buildings).

**Space and pipes (the heart)**
- Where does my land sit relative to my friends' land? Next to each other, or spread out?
- Can pipes cross borders? Who owns a pipe that crosses? Who pays for it, who can remove it?
- Throughput and flow across players: the solver has to handle cross-player networks fairly and cheaply.
- Making collaboration *visible*: seeing your friend's goods flow through your pipes is the best possible feedback.
- Shared vs private buildings, and who may change shared ones.

**Economy**
- One world market or many? Do players' sales move prices? Can one player crash a price for everyone?
- Trading and gifting exploits: alts feeding a main account, multi-accounting, bots.
- Whales (paying or just very active) dominating shared goals or leaderboards.
- Inflation over a season; what to do with surplus late in the season.
- Non-zero-sum rewards: helping someone should never cost the helper more than it gives them.

**Safety and tone**
- Griefing: pipe spam, blocking land, ugly building next to someone, offensive names, chat abuse.
- No loss-based PvP. Competition, if any, is racing and showing off, never taking things away.
- Moderation cost: free text chat is expensive. Prefer non-verbal signals (pings, stamps, requests).
- Consent: nobody can connect into my land or change my things without my say.

**Systems and operations**
- Server cost: simulating thousands of estates and cross-player flows.
- Matchmaking: putting solo players into groups, filling empty seats, balancing world density.
- Notifications that bring people back without nagging.
- Fairness of rankings: per-active-member, per-day, or not ranked at all.
- Monetisation (if any) must not sell co-op advantage.
- Testing and balance: co-op bonuses must be tuned so solo is fine and groups are better but not mandatory.

---

## 2. Brainstorm (30 ideas)

1. **Stations.** A crew of up to four picks stations: *Wellhead* (raw extraction), *Works* (refining), *Assembly*
   (finished goods) and *Dispatch* (market, logistics, contracts). Each station gets a bonus for its stage and unique
   buildings, but every station can still do everything a bit worse, so nobody is ever stuck.

2. **The Junction.** Crew estates are laid out around a small shared plot in the middle, the Junction. Every member
   runs pipes into it, and its "manifold" buildings make high-value goods that need inputs from two, three or four
   different stations. It's the crew's shared town square and the place where you see each other's work.

3. **Caretakers.** When a seat is empty (a solo player, an absent friend, someone who quit), an NPC caretaker runs it
   at reduced efficiency: it keeps the existing chain running and keeps goods flowing to the Junction, but it never
   builds or upgrades. One system covers being alone, being absent and leaving.

4. **Open Seats.** A crew with an empty or caretaker-held station can list it as an open seat. A solo player (or a
   returning player whose old crew is gone) can drop in, take over the caretaker's land and pick up from there. Low
   commitment: you can leave again any time.

5. **The Great Pipe.** Every season the world builds one huge pipeline across the map toward a landmark (a city, a
   lighthouse, a sky-ship). Crews and solo players supply segments and the goods to fill them. Milestones unlock
   world-wide perks and a big finale, and each segment carries the names of the people who built it.

6. **Pipe Post.** Send a sealed capsule of goods to any player. It travels visibly through the world's pipes and
   arrives hours later with a short canned note ("For your furnace!"). A cosy async gift with a little journey.

7. **Neighbour Valves.** Neighbouring strangers can agree (both tap) to put a valve on their shared border. Surplus
   that one of them can't use flows to the other, and both get a small "good neighbour" bonus. Either side can close
   the valve at any time.

8. **Standing Orders board.** Players post standing orders ("50 glass per hour, paying X"). Anyone can fulfil them by
   piping into a public depot, so trading happens without anyone being online at the same time.

9. **Shift Log.** The crew shares a log: whoever logs in "takes the shift" (collects, tunes, answers requests), and
   the crew gets a coverage bonus for every day that *somebody* was there. It's a group streak that forgives gaps,
   with no personal streak to lose.

10. **Flares.** Non-verbal requests: tap a building, "Need copper". A flare goes up over your land on your friends'
    maps and in their next session summary. Answering a flare takes one tap: "send from surplus".

11. **Homecoming.** A returning player gets a short illustrated letter about what the crew did while they were away
    ("Mara built a second smelter, the caretaker kept your wells going"), plus a catch-up boost scaled to how long
    they were gone and how far the season is.

12. **Overgrowth.** Land abandoned for a long time slowly turns into a pretty overgrown ruin. It isn't deleted, and
    it doesn't block neighbours' pipes (pipes may pass through it). If the owner returns, it's cleaned up instantly.

13. **Open House.** Visit anyone's estate, watch their pipes flow and leave a stamp (heart, wrench, wow). Stamps give a
    tiny bonus to both and feed a weekly "prettiest pipework" gallery. Showing off is half the fun of a building game.

14. **Apprentices.** A late-game player can take on a newcomer. The mentor gets rewards when the apprentice reaches
    milestones; the apprentice gets a starter pipe kit and a pinned "ask my mentor" flare.

15. **Quartet Recipes.** Special season items need four different station goods from four different *people*. Making
    one is a crew trophy, and it's the reason to want a full crew without punishing smaller ones (caretakers can't
    supply quartet inputs, only humans can).

16. **Station Rotation.** Each new season the crew rotates stations, and the season's recipes favour different
    stages, so the Wellhead player of season 1 runs Assembly in season 2.

17. **Echoes.** A solo player is quietly paired with the recorded surplus curve of a real, distant player in a
    complementary station. The echo's goods arrive through a "long-distance pipe" as if from a partner. It feels like
    having a crew with nobody depending on you.

18. **Shared Aquifer.** A region sits on a shared underground resource. Everybody pumping from it raises its yield
    together (pooled pressure) instead of draining it: a cosy inversion of the tragedy of the commons.

19. **Co-ops.** Bigger clubs (up to 30) made of several crews share a co-op hall with its own long-term projects and
    a co-op pipe network. Crews stay the basic unit, co-ops are optional.

20. **Grand Flow finale.** At the season's end every crew's Junction is connected to the Great Pipe for a final
    "Grand Flow": an animated event filling the landmark, and everyone who contributed anything gets the season
    keepsake.

21. **Regional Weather.** World events hit regions ("drought in the north"): those neighbours suddenly need water, and
    whoever pipes it to them earns reputation and a regional thank-you. It gives a reason to connect with strangers.

22. **Coloured Flow.** Every player's goods carry a subtle tint in the pipes. You can literally see your friend's
    copper flowing through your factory, and the Junction becomes a mix of all four colours.

23. **Lending.** Lend a building, or a blueprint, to a crewmate for a week. It runs on their land and comes back
    automatically. It's useful before a holiday.

24. **Railway Races.** Crews race (no sabotage) to connect their Junction to a world landmark first, or to be the
    first to deliver a season good. Bragging rights and a cosmetic, but no losses.

25. **Daily Dispatch.** One shared daily order per crew: anyone can fill it, and everyone in the crew gets the
    reward. The first one to log in each day feels useful, and the others feel thanked.

26. **Heirlooms.** The crew's name, crest, Junction cosmetics and one heirloom building skin carry over between
    seasons. Crews feel like a long-term thing even though the land resets.

27. **Bottleneck Bridges.** Pipes between crew estates have limited throughput that the crew upgrades *together*
    (each member funds a share), so there's a light planning puzzle of what to send through the shared bridge.

28. **Postcards.** When you complete something big, a postcard (a screenshot of your estate) goes to your crew's
    and neighbours' inboxes. It gives people something to look at in a 2-minute session.

29. **Night Market.** For one hour in every time zone each day, the world market pays extra for goods that crews
    deliver *together*. It isn't required, but it's a small reason to coordinate if you want to.

30. **The Lighthouse Keeper.** A friendly world NPC who adopts lonely players: gives them a small personal crew
    quest line, introduces them to open seats and neighbours, and stays their "partner" until they find humans.

---

## 3. The three I'd build first, and how they fit together

### The design in one sentence

You play in a **crew of up to four stations around a shared Junction**. Empty or absent seats are run by
**caretakers**, so a crew is always "full". And every Junction, from solo to full crews, feeds the season's
**Great Pipe**, the world project that gives late-game surplus a purpose and makes lonely players part of something
big.

### A. Stations around a Junction (the crew layer)

- When you start, you either found a crew (and pick a station) or join one with an invite code. Solo players found
  a crew of one. The four estates are placed around a small Junction plot; your own land is fully yours.
- A station gives about +25–50% on its stage and 1–2 unique buildings. It gives no exclusive goods for normal
  chains: a Works player can still dig ore, just worse. This means nobody is ever blocked by an absent friend.
- The Junction's manifold buildings take goods from several stations and make the crew's best-paying products, and
  (later) the Quartet trophy items that need four *human* inputs. Coloured Flow and Flares make the Junction the
  place where you see your friends without being online at the same time.
- Early game: a single pipe into the Junction is the first co-op step, a tutorial moment. Late game: manifold tiers
  and bridge upgrades are the group's optimisation puzzle.

**Main risk:** dependency and imbalance. If manifolds pay too well, a full active crew outgrows everyone and an
absent friend feels like a broken machine. Stations may also be unequal in fun (the Wellhead may be dull). The fix
is to tune the manifolds so they're the best option without being needed, give every station its own decisions,
and rotate stations per season.

### B. Caretakers (the safety-net layer)

- Any seat without an active human is held by a caretaker: a named, visible NPC character (a cosy one, with a
  lantern) that keeps the existing chain running at ~50–60% and keeps its share flowing to the Junction. It never
  builds, upgrades or sells smartly; that's what humans are for.
- **Solo:** you found a crew and three caretakers fill the other stations with small starter chains. You get the
  whole crew experience with simple partners and can upgrade them a bit with your own goods.
- **Absent:** after ~2 days offline, your seat's caretaker kicks in automatically. Your friends lose a little, not
  everything. You come back to a Homecoming letter.
- **Gone:** after ~10–14 days, the crew can list the seat as an Open Seat. A newcomer or another solo player takes it
  over, with the land as the caretaker kept it. The original player, on return, gets a new seat in the same crew if
  one is free, or a fresh estate with a catch-up boost; nobody gets kicked out of anything they own while active.
- Early game this is invisible; late game it's what keeps crews alive through the mid-season drop-off.

**Main risk:** getting the strength right. If caretakers are too good, friends feel unnecessary and solo crews feel
hollow ("I'm playing with bots"). If they're too weak, every absence hurts and the guilt problem comes back. The
caretaker must be clearly worse than a human at *decisions* (not just numbers), and the game must celebrate when a
real human replaces one.

### C. The Great Pipe (the world layer)

- Each season has one world landmark and one Great Pipe crossing the map toward it in segments. Each segment needs
  pipe materials to build and then a flow of particular goods to fill. Anyone can pipe their Junction into the
  nearest open segment; solo players and full crews contribute the same way.
- Contributions are shown as names and colours on the segments (Coloured Flow), and rewards are by thresholds you
  can reach personally ("you helped fill 3 segments"), not by rank, so a small player still matters. World
  milestones unlock perks for everyone (faster flow, a new recipe, a cosmetic).
- It is the late-game surplus sink: when your estate is maxed, the Great Pipe always wants more. The finale
  (Grand Flow) is the season's closing event, and the keepsake carries over (Heirlooms).
- It also gives lonely players a reason to feel the server is alive: you see the pipe grow every day, with strangers'
  colours in it, and your Flares and Standing Orders can be fulfilled through it.

**Main risk:** contributions disappearing in a huge total. In a world of thousands, one player's 0.01% feels like
nothing, and big active crews (or whales) do almost all the work. Then it's a progress bar nobody cares about.
Prevent this with local segments that have their own small goals, personal thresholds instead of shares, names on
the pipe, and a per-region pace so the late-season and quiet-time-zone players still finish "their" stretch. A
secondary risk is server cost: the Great Pipe must be an aggregated ledger, not a real flow simulation across all
players.

### How the three fit the owner's principles

| Principle | How it's covered |
|---|---|
| 3 friends joining together, choosing a station | Crew + Stations + Junction (A) |
| Lonely players | Caretakers fill the crew (B); the Great Pipe makes the world feel alive (C) |
| Players who stop and come back | Caretakers hold the seat, Homecoming letter, Open Seats (B) |
| Early and late game | Early: first pipe into the Junction. Late: manifold tiers, Quartet items, the Great Pipe as a sink (A, C) |
| Idle, a few minutes a day, different times | Everything is async: flows, caretakers, Flares, segment thresholds; nothing needs two people online at once |

### Next in line

Flares and Coloured Flow (cheap, and they make A visible), Homecoming (cheap, makes B feel warm), Open House
stamps (cosy spectacle), Neighbour Valves (light stranger co-op once the world is dense).

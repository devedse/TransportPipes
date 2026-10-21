# Brief: how players play together in TransportPipes

## The game today (context, NOT a constraint)

TransportPipes is a browser MMO idle game (isometric, cosy art) about building production chains and connecting
them with pipes. Persistent world, seasons of a few weeks (eras, wonders), testers playing now. You found a company,
get an estate, build generators, mines and factories, connect them with pipes, sell to the hub; levels unlock new
buildings, parcels of land, research. Pipes are the core identity of the game ("TransportPipes").

Current world model (built in v1, all of it can be thrown away):
- The world is made of districts. A district is a 5×5 grid of blocks: the hub city in the middle, 24 estates
  around it. An estate is 32×32 tiles (16 parcels of 8×8; 4 start parcels, the rest bought with levels).
- 2-tile corridors run between the blocks; every estate has a gate on each side. Pipes in corridors connect estates.
  30 tiles of wilderness between districts (outposts can be claimed there).
- A new player is assigned the next free estate; you don't choose where, nor who your neighbours are.
- Empty estates get NPC "tenants" (works that buy what players sell).
- Trading: your trading post sells to the hub; buildings of different companies on one pipe network trade
  automatically at the seller's price; contracts; an exchange; cargo; corporations from Company Level 8 (free trade
  between members, shared weekly goals, a corp HQ); toll meters; teleporters; a franchise (second estate).
- v1.1 rule (questioned by the owner): goods only travel directly between the two companies whose estates are
  piped together, never through a third company's estate.

## What went wrong / what the owner wants

- Pipes from neighbour to neighbour merged whole districts into one giant network: everyone traded with everyone,
  far trades lost money over long pipes, gates made little sense, nobody chose a partner.
- The owner is not sure about "direct trade only". The core principle: it should be FUN TO PLAY WITH 3 FRIENDS THAT
  JOIN. Somehow choosing a station / position would be ideal.
- Don't limit yourself to the current mechanics. Moving estates, anything goes.

## What the design must take into account (the checklist every idea is scored against)

1. **Friends together.** Three friends who start together can be neighbours from minute one, see each other's estates,
   help each other and have something to do together. A friend who joins a week later can still join them.
2. **Choosing where you live.** A player chooses a position (a station, a spot on the map, a group) with the
   information to choose well: who lives there, how active they are, what the land holds. A sensible default for
   those who don't want to choose.
3. **The lonely player.** Someone who plays alone (no friends, quiet server, odd hours) gets a complete, fun game
   and a world that doesn't feel empty. Solo must never be a handicap that makes them quit.
4. **Players who stop.** Most players leave after days or weeks. Their estates must not leave holes, dead partners,
   broken supply chains or blocked land; a player who returns after a month can come back.
5. **The early game.** The first hours: playing together is easy to understand, immediately useful, and nobody can
   hurt a newcomer. No dependency on others to progress.
6. **The late game.** Weeks in: cooperation grows (bigger projects, longer logistics, specialisation) and keeps
   veterans interested; something to work towards together; meaningful (friendly) competition.
7. **Asynchronous idle play.** Friends play at different times and for minutes a day; cooperation must work while
   the other is offline (no "both online" requirement), and an idle partner must not stall you.
8. **Fair and safe.** No griefing, blocking, exploiting or leeching; veterans can't crush newcomers; trade can't be
   abused to funnel gold.
9. **Clear.** A player can always see who they play with, what goes where and why, ideally on the map.
10. **Pipes are the heart.** Playing together should be expressed through pipes and logistics, the game's identity.
11. **Scale and performance.** Works with 5 players and with 2,000; no district-wide mega-networks; cheap to simulate.
12. **Seasons and a persistent world.** Works across seasons and without wiping the testers' worlds (a migration).
13. **Social glue.** Recognition, shared goals, communication, identity (names, colours, banners), a reason to invite.
14. **Buildable.** One developer (an AI) builds it in days, not months; it can ship in steps.

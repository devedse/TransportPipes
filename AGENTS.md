# Working on TransportPipes

The game design is `docs/GameDesign.md`, the progress and the plan `docs/ImplementationStatus.md`, the look
`docs/ArtDirection.md`. Work on `master`; every pushed commit must stay playable (CI builds the Docker image that the
test server runs).

## Every commit goes into the release notes

The game's "What's new" shows players what changed after an update, made from the commits by `scripts/changes.mjs`
(every commit that touches the game, newest first, under the version it came in). So write every commit for them too:
give it a `Player: <line for players>` line in its message, several for several changes, `Player: -` for one players
don't notice (it is left out). Without one, its subject is shown.

Write those lines for someone who plays for the first time today:

- Plain words, short sentences. Say what the player now sees or does differently, and where (which panel, which button).
- Explain every game word again, from scratch, each time it comes up, even if an earlier line explained it: what a Hub
  Order, the Yard, a Storage's line, a want, a level's goods is, and where to find it. Never a word only the code uses.
- No internal names, numbers of tests or playtest runs; one change a line.

A line that is out already and unclear gets better words in `scripts/changes-text.json` (its commit → its lines, or
`"-"` to leave it out): the card shows those instead.

## Before you push

- `dotnet test --project TransportPipes.Game.Tests` and `TransportPipes.Server.Tests` (needs Docker); in
  `TransportPipes.Frontend`: `npm run lint`, `npm run format:check`, `npm run typecheck`, `npm test`
- Give the commit its `Player:` lines (see above)
- Reproduce a reported bug yourself before fixing it (the `e2e/` scripts drive the game in a browser; see
  `e2e/README.md`), and check the fix the same way

## Every new or changed building picture

A building must stand exactly on its footprint, or it looks lifted or too big next to its neighbours:

1. Process it with `cd art && node process.mjs buildings <source dir>`: it puts the top surface of the picture's
   ground plate on the footprint and cuts off what sticks out below the footprint's front edges.
2. Check it with `node fit.mjs <out.png> <id...>` (no ids: all buildings): every sprite is drawn on its footprint
   diamond (red). The plate's top surface must fill the diamond exactly and nothing may stick out below it. Look at
   every cell.
3. When a picture doesn't fit (its plate isn't a diamond, it is off-centre, the building hangs over the plate), make
   it again with a better prompt (`art/make-prompts.py`), don't patch it in the game.
4. Look at it in the game next to other buildings (`e2e/showcase.mjs shots/x <type,...>`).

# Building connector motion

The Yard gate is the transfer point into the shared Yard network. Its striped pad is ground, a receiving box takes
the estate's pipes, and the posts and barrier stand above it. The box joins whichever neighbours are connected;
there is no capped pipe end poking through the pad. The box's raised roof and faces target the gate tile when
drawing a pipe, just like its posts. Pipe routes and the simulation keep the same gate tile.

The short building connectors make the existing flows readable up close:

- Solids travel as the painted resource icons already used in the UI. A belt has two narrow tracks, one inbound and
  one outbound, with moving treads. Ore going in and ingots coming out can pass without overlapping.
- Electricity has small travelling arcs along the actual cable geometry, including its support post.
- Fluids have coloured slugs and glints along a glassy section of the slim pipe. The colour matches the resource.

Traffic comes from the server's existing network flows: `sent` at the source, `delivered` at the destination. Several
flows using one connector are combined by resource and direction; virtual Yard sellers and buyers are not buildings.
When several goods share a track, busier goods appear more often. Displayed packets represent batches rather than
individual inventory units. Density increases with flow and stops at three packets per direction and kind.

Animations use pooled sprites and cached textures, with no additional server requests or per-frame simulation.
They draw with their pipe so foreground buildings occlude them. Only visible connectors update; motion fades with
the connectors when zooming out, stops when traffic stops, and stays still with reduced motion. The full three-kind,
two-direction connector needs at most 28 sprites. Quiet electricity / water inspection sounds share the existing
mute control and are limited to one sound every three seconds for the whole map.

Validation: `src/game/connectors.test.ts` checks losses, aggregation, direction, virtual endpoints, geometry,
weighted goods and the animation budget. `e2e/connector-motion.mjs` builds a working factory and water pump, records
the motion, and checks movement, idle connectors, reduced motion, distance fading and the sprite limit. The existing
gate gesture check covers the four Yard orientations.

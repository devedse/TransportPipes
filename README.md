# TransportPipes

A massively multiplayer idle game: found a company, build wind moilies, mines and factories on your own estate, connect
them with pipes, sell to the hub city and to your neighbours, and grow while you are away. The world keeps running when
nobody is online.

The design for the full game (estates, districts, trading, Eras, the late game) is in
[docs/GameDesign.md](docs/GameDesign.md).

This is the modernized version of the original game (ASP.NET WebForms + SignalR 2 + TypeScript 1.8, imported from TFS in
commit `b2188c0`, where the old sources can still be found).

## Running the game

The game is one Docker image (`ghcr.io/devedse/transportpipes`) plus a PostgreSQL database, see [compose.yaml](compose.yaml):

```bash
docker compose up -d
```

Then open http://localhost:8080 and create an account. The database is stored in `./data/postgres` next to the compose
file. To build the image yourself instead: `docker compose up -d --build`.

compose.yaml runs `beta_nightly`, the newest build of any branch (master included), so a test server with Watchtower
follows every push. Use `latest` (or a version like `1.0.2`) to only run builds of master.

| Environment variable | Default | |
| --- | --- | --- |
| `ConnectionStrings__TransportPipes` | | The PostgreSQL connection string (required) |
| `TransportPipes__StartingGold` | from `progression.yaml` (2000) | The gold a new company starts with |
| `TransportPipes__TickSeconds` | `2` | How often the world is simulated and clients get updates |
| `TransportPipes__SaveSeconds` | `30` | How often gold, storage contents and XP are saved (commands are saved within a tick) |
| `TransportPipes__WorldSeed` | `20261018` | The seed of a new world (terrain, deposits); only used when the database has no world |
| `TransportPipes__SeedTestUsers` | `false` | Creates the users `admin` and `Devedse` when there are no users yet |
| `TransportPipes__TestUserPassword` | `a` | The password of those test users |

The database is created and upgraded (EF Core migrations) when the server starts. `/health` reports whether the server
can reach the database.

## Architecture

```
Browser (React + PixiJS)  --/api/auth (cookie login), /api/content-->  TransportPipes.Server  -->  PostgreSQL
                          <--SignalR /hubs/game (chunks, commands)-->   |- GameHub
                                                                       |- WorldHost: the world in memory, ticks,
                                                                       |  saves snapshots, pushes changes
                                                                       '- serves the frontend from wwwroot
```

The whole world lives in memory in `TransportPipes.Game`: estates, buildings, pipes and the networks they form. Every
command (build, pipe, upgrade, buy a parcel) changes it directly; every tick (2 s) the world runs up to now: constructions
finish, every network delivers its flows (solved by the flow solver only when the network changes) and companies earn
gold and XP. A long catch-up (after a restart) gives exactly the same result as many short ticks. The `WorldStore` saves
what changed (commands within a tick, gold and storage every 30 s) and loads the world on start.

| Project | |
| --- | --- |
| `TransportPipes.Game` | The game itself, without I/O: content (YAML), world layout and generation, the flow solver, the world state and its tick |
| `TransportPipes.Database` | EF Core (PostgreSQL): users, the world snapshot (companies, estates, buildings, pipes), migrations, `WorldStore` |
| `TransportPipes.Server` | ASP.NET Core: cookie accounts, the content endpoint, the SignalR `GameHub`, the `WorldHost`, serves the frontend |
| `TransportPipes.Frontend` | The browser client: React UI on top of a PixiJS isometric map (npm + Vite), with procedural art for every building |
| `TransportPipes.ImageCombiner` | Tool that combines the original building images into the sprite sheet the frontend uses |
| `*.Tests` | MSTest; the server tests run against a real PostgreSQL in a container (Testcontainers) |

The game content (resources, buildings, pipes, progression) is in YAML in
[TransportPipes.Game/Content](TransportPipes.Game/Content) and is served to the client as JSON, so balancing needs no
code changes. How far the design is implemented: [docs/ImplementationStatus.md](docs/ImplementationStatus.md).

## Development

Requirements: the .NET 10 SDK, Node 24 and Docker.

```bash
# 1. The database
docker compose -f compose.dev.yaml up -d

# 2. The server on http://localhost:5080 (Development: creates the test users admin and Devedse, password "a")
dotnet run --project TransportPipes.Server

# 3. The frontend on http://localhost:5173 (proxies /hubs and /api to the server)
#    http://localhost:5173/art.html shows all procedural building art
cd TransportPipes.Frontend
npm ci
npm run dev
```

Tests (Docker must be running, the database tests start their own PostgreSQL container):

```bash
dotnet test --solution TransportPipes.slnx
cd TransportPipes.Frontend && npm run lint && npm run typecheck && npm test
```

Database changes are done with EF Core migrations:

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project TransportPipes.Database
```

The sprite sheet in `TransportPipes.Frontend/public/images/CombinedGameObjects` is made from a directory with one png per
image (animations as `Name-0.png`, `Name-1.png`, ...):

```bash
dotnet run --project TransportPipes.ImageCombiner -- <input directory> TransportPipes.Frontend/public/images/CombinedGameObjects
```

## Builds

[GitHub Actions](.github/workflows/githubactionsbuilds.yml) builds and tests everything on every push, publishes the
multi-architecture (amd64 + arm64) Docker image to GHCR and creates a release for every build of `master`
(`ghcr.io/devedse/transportpipes:latest` and `:1.0.<build number>`). Other branches get the `beta` and `beta_<branch>`
tags, and every build (except Dependabot's) also gets `beta_nightly`.

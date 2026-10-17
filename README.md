# TransportPipes

An online isometric game: build wind mills, solar panels, mines, factories, storages and trading buildings, connect them
with pipes and earn gold by trading the resources with other players and the market.

This is the modernized version of the original game (ASP.NET WebForms + SignalR 2 + TypeScript 1.8, imported from TFS in
commit `b2188c0`, where the old sources can still be found).

## Running the game

The game is one Docker image (`ghcr.io/devedse/transportpipes`) plus a PostgreSQL database, see [compose.yaml](compose.yaml):

```bash
# The image is private (like the repo), log in once with a GitHub token that has read:packages
echo <token> | docker login ghcr.io -u devedse --password-stdin

docker compose up -d
```

Then open http://localhost:8080 and create an account. To build the image yourself instead: `docker compose up -d --build`.

| Environment variable | Default | |
| --- | --- | --- |
| `ConnectionStrings__TransportPipes` | | The PostgreSQL connection string (required) |
| `TransportPipes__StartingGold` | `500` | The gold a new player starts with |
| `TransportPipes__SeedTestUsers` | `false` | Creates the users `admin` and `Devedse` when there are no users yet |
| `TransportPipes__TestUserPassword` | `a` | The password of those test users |
| `TransportPipes__ActionizerIntervalSeconds` | `1` | How often the queued actions (building, deleting) are executed |

The database is created and upgraded (EF Core migrations) when the server starts. `/health` reports whether the server
can reach the database.

## Architecture

```
Browser (React + PixiJS)  <--SignalR /hubs/transport-->  TransportPipes.Server  -->  PostgreSQL
                                                          |- TransportHub (login, chunks, build/delete)
                                                          |- Actionizer (background service, every second)
                                                          '- serves the frontend from wwwroot
```

Players don't change the world directly: building or deleting queues an action. The Actionizer executes the actions whose
time has come, recalculates the pipe networks ("global connections") and the gold of everyone involved, and the server
pushes the changed chunks and incomes to the clients.

| Project | |
| --- | --- |
| `TransportPipes.Server` | ASP.NET Core: the SignalR hub, the Actionizer background service, serves the frontend |
| `TransportPipes.Actionizer` | Executes the queued actions and finds the connections between buildings |
| `TransportPipes.Calculators` | Calculates how resources flow through a connection and who earns what |
| `TransportPipes.Database` | EF Core (PostgreSQL) entities, migrations and all queries |
| `TransportPipes.Common` | Game data (buildings, resources, prices), terrain generation, helpers |
| `TransportPipes.Frontend` | The browser client: React UI on top of a PixiJS isometric map (npm + Vite) |
| `TransportPipes.ImageCombiner` | Tool that combines the building images into the sprite sheet the frontend uses |
| `*.Tests` | MSTest; the database tests run against a real PostgreSQL in a container (Testcontainers) |

## Development

Requirements: the .NET 10 SDK, Node 24 and Docker.

```bash
# 1. The database
docker compose -f compose.dev.yaml up -d

# 2. The server on http://localhost:5080 (Development: creates the test users admin and Devedse, password "a")
dotnet run --project TransportPipes.Server

# 3. The frontend on http://localhost:5173 (proxies /hubs to the server)
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
tags.

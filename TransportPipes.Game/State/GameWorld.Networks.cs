using TransportPipes.Game.Content;
using TransportPipes.Game.Simulation;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>
/// Buildings and pipes that are connected. A pipe group is a connected set of pipe and gate tiles; buildings touching
/// the same pipe group can exchange resources (resources never pass through a building). A network is everything that
/// is connected through pipe groups, and is solved as one.
/// </summary>
public sealed class Network
{
    public int Id { get; init; }
    public HashSet<long> Buildings { get; } = new();
    public HashSet<TilePos> Conductors { get; } = new();
    public List<FlowLink> Links { get; set; } = new();
    public FlowSolution Solution { get; set; } = FlowSolution.Empty;
    public bool NeedsSolve { get; set; } = true;

    /// <summary>Buildings run at this fraction when the network has too few pump stations</summary>
    public double PumpFactor { get; set; } = 1;

    /// <summary>The district (and so the hub market) the network is in</summary>
    public DistrictCoord District { get; set; }

    /// <summary>The companies with buildings in this network</summary>
    public IEnumerable<long> Owners(GameWorld world) => Buildings.Select(t => world.Building(t)!.Owner).Distinct();

    public override string ToString() => $"Network {Id}: {Buildings.Count} buildings, {Conductors.Count} pipes";
}

public sealed partial class GameWorld
{
    private readonly Dictionary<int, Network> _networks = new();
    private readonly Dictionary<TilePos, int> _networkOfTile = new();
    private readonly Dictionary<long, int> _networkOfBuilding = new();
    private readonly HashSet<TilePos> _dirtyTiles = new();
    private readonly HashSet<long> _dirtyBuildings = new();
    private int _nextNetworkId = 1;

    public IEnumerable<Network> Networks
    {
        get
        {
            UpdateNetworks();
            return _networks.Values;
        }
    }

    public Network? NetworkOf(long buildingId)
    {
        UpdateNetworks();
        return _networkOfBuilding.TryGetValue(buildingId, out var id) ? _networks[id] : null;
    }

    public Network? NetworkAt(TilePos pos)
    {
        UpdateNetworks();
        return _networkOfTile.TryGetValue(pos, out var id) ? _networks[id] : null;
    }

    /// <summary>The topology around this tile changed</summary>
    private void MarkDirty(TilePos pos) => _dirtyTiles.Add(pos);

    /// <summary>A building was added, removed or changed: its network has to be rebuilt and solved again</summary>
    private void MarkDirty(Building building)
    {
        _dirtyBuildings.Add(building.Id);
        foreach (var tile in building.Footprint())
        {
            _dirtyTiles.Add(tile);
        }
        foreach (var tile in building.Border())
        {
            _dirtyTiles.Add(tile);
        }
        // A pylon or a Cargo Depot joins (or splits) the networks it reaches
        var kind = Content.Building(building.Type).Kind;
        var remote = kind == BuildingKind.PowerLine ? PylonsInReach(building)
            : kind == BuildingKind.CargoDepot ? _buildings.Values.Where(t => t.Id != building.Id && t.Owner == building.Owner && IsCargoDepot(t))
            : [];
        foreach (var other in remote.ToList())
        {
            _dirtyBuildings.Add(other.Id);
            _dirtyTiles.Add(other.Position);
        }
    }

    /// <summary>Only the flows changed (prices, storage full or empty, construction finished)</summary>
    private void MarkNeedsSolve(long buildingId)
    {
        if (_networkOfBuilding.TryGetValue(buildingId, out var id))
        {
            _networks[id].NeedsSolve = true;
            Changes.Networks.Add(id);
        }
        else
        {
            _dirtyBuildings.Add(buildingId);
        }
    }

    private void MarkCompanyNetworksDirty(long companyId)
    {
        foreach (var building in BuildingsOf(companyId).ToList())
        {
            MarkNeedsSolve(building.Id);
        }
    }

    /// <summary>Whether this conductor tile carries resources (closed gates don't)</summary>
    private bool Conducts(Conductor conductor)
    {
        if (conductor.Kind != ConductorKind.Gate)
        {
            return true;
        }
        // A closed gate blocks the pipe; corporation and contract gates carry it but limit who may trade through them
        return GatePolicyAt(conductor.Pos) != GatePolicy.Closed;
    }

    /// <summary>The policy of the gate on a tile (Open when it is no gate)</summary>
    public GatePolicy GatePolicyAt(TilePos pos)
    {
        if (!_conductors.TryGetValue(pos, out var conductor) || conductor.Kind != ConductorKind.Gate || conductor.Side is not { } side)
        {
            return GatePolicy.Open;
        }
        return EstateAt(pos)?.GatePolicies.GetValueOrDefault(side, GatePolicy.Open) ?? GatePolicy.Open;
    }

    /// <summary>Whether two companies may trade over a path: every gate on it has to allow their trade</summary>
    public bool PathAllowsTrade(IReadOnlyList<TilePos> path, long seller, long buyer, string resource)
    {
        foreach (var tile in path)
        {
            switch (GatePolicyAt(tile))
            {
                case GatePolicy.Contract when !HasContract(seller, buyer, resource):
                case GatePolicy.Corporation when !SameCorporation(seller, buyer):
                case GatePolicy.Closed:
                    return false;
            }
        }
        return true;
    }

    private static readonly (int X, int Y)[] Directions = [(1, 0), (-1, 0), (0, 1), (0, -1)];

    /// <summary>Buildings a network may have per pump station (plus one free batch)</summary>
    public const int BuildingsPerPump = 50;

    /// <summary>A building that sits in a pipe and carries resources like one (valves, toll meters)</summary>
    private bool IsConductorBuilding(Building building) => !building.UnderConstruction && Content.Building(building.Type).IsConductor;

    private bool IsPylon(Building building) => !building.UnderConstruction && Content.Building(building.Type).Kind == BuildingKind.PowerLine;

    private bool IsCargoDepot(Building building) => !building.UnderConstruction && Content.Building(building.Type).Kind == BuildingKind.CargoDepot;

    /// <summary>The other Cargo Depots of a depot's owner: trucks connect them all (estate and outposts)</summary>
    public IEnumerable<Building> CargoPartners(Building depot) =>
        _buildings.Values.Where(t => t.Id != depot.Id && t.Owner == depot.Owner && IsCargoDepot(t));

    /// <summary>The buildings a pylon's cable or a depot's trucks reach</summary>
    private IEnumerable<Building> RemoteLinks(Building building) =>
        IsPylon(building) ? PylonsInReach(building) : IsCargoDepot(building) ? CargoPartners(building) : [];

    /// <summary>The pylons a pylon's cable reaches: the owner's pylons within its reach</summary>
    public IEnumerable<Building> PylonsInReach(Building pylon)
    {
        var reach = Content.Building(pylon.Type).Reach;
        return _buildings.Values.Where(t => t.Id != pylon.Id && t.Owner == pylon.Owner && IsPylon(t)
            && (t.X - pylon.X) * (t.X - pylon.X) + (t.Y - pylon.Y) * (t.Y - pylon.Y) <= reach * reach);
    }

    private bool IsConductorTile(TilePos pos) =>
        (_conductors.TryGetValue(pos, out var conductor) && Conducts(conductor))
        || (_buildingAt.TryGetValue(pos, out var id) && IsConductorBuilding(_buildings[id]));

    /// <summary>Rebuilds the networks around everything that changed (other networks keep their solution)</summary>
    private void UpdateNetworks()
    {
        if (_dirtyTiles.Count == 0 && _dirtyBuildings.Count == 0)
        {
            return;
        }

        // Dissolve every network touching a change, then flood fill again from all their members
        var affected = new HashSet<int>();
        var seedTiles = new HashSet<TilePos>();
        var seedBuildings = new HashSet<long>();
        foreach (var tile in _dirtyTiles)
        {
            if (_networkOfTile.TryGetValue(tile, out var id))
            {
                affected.Add(id);
            }
            if (_buildingAt.TryGetValue(tile, out var buildingId))
            {
                seedBuildings.Add(buildingId);
                if (_networkOfBuilding.TryGetValue(buildingId, out var buildingNetwork))
                {
                    affected.Add(buildingNetwork);
                }
            }
            if (_conductors.ContainsKey(tile))
            {
                seedTiles.Add(tile);
            }
        }
        foreach (var buildingId in _dirtyBuildings)
        {
            if (_networkOfBuilding.TryGetValue(buildingId, out var id))
            {
                affected.Add(id);
            }
            if (_buildings.ContainsKey(buildingId))
            {
                seedBuildings.Add(buildingId);
            }
        }
        _dirtyTiles.Clear();
        _dirtyBuildings.Clear();

        foreach (var id in affected)
        {
            var network = _networks[id];
            _networks.Remove(id);
            Changes.Networks.Add(id);
            foreach (var tile in network.Conductors)
            {
                _networkOfTile.Remove(tile);
                if (_conductors.ContainsKey(tile))
                {
                    seedTiles.Add(tile);
                }
            }
            foreach (var buildingId in network.Buildings)
            {
                _networkOfBuilding.Remove(buildingId);
                if (_buildings.ContainsKey(buildingId))
                {
                    seedBuildings.Add(buildingId);
                }
            }
        }

        foreach (var tile in seedTiles)
        {
            if (!_networkOfTile.ContainsKey(tile) && Conducts(_conductors[tile]))
            {
                Flood(tile, null);
            }
        }
        foreach (var buildingId in seedBuildings)
        {
            if (!_networkOfBuilding.ContainsKey(buildingId))
            {
                Flood(null, buildingId);
            }
        }
    }

    /// <summary>Creates the network that contains this tile or building</summary>
    private void Flood(TilePos? startTile, long? startBuilding)
    {
        var network = new Network { Id = _nextNetworkId++ };
        var tiles = new Queue<TilePos>();
        var buildings = new Queue<long>();

        void VisitTile(TilePos tile)
        {
            if (network.Conductors.Add(tile))
            {
                _networkOfTile[tile] = network.Id;
                tiles.Enqueue(tile);
            }
        }

        void VisitBuilding(long id)
        {
            if (network.Buildings.Add(id))
            {
                _networkOfBuilding[id] = network.Id;
                var building = _buildings[id];
                if (IsConductorBuilding(building))
                {
                    // A valve is part of the pipe (a Cargo Depot with all its tiles)
                    foreach (var tile in building.Footprint())
                    {
                        VisitTile(tile);
                    }
                }
                else
                {
                    buildings.Enqueue(id);
                }
            }
        }

        if (startTile is { } t)
        {
            VisitTile(t);
        }
        if (startBuilding is { } b)
        {
            VisitBuilding(b);
        }

        while (tiles.Count > 0 || buildings.Count > 0)
        {
            while (tiles.TryDequeue(out var tile))
            {
                // A power line reaches the next pylons, trucks the owner's other Cargo Depots
                if (_buildingAt.TryGetValue(tile, out var here) && _buildings[here].Position == tile)
                {
                    foreach (var other in RemoteLinks(_buildings[here]))
                    {
                        VisitBuilding(other.Id);
                    }
                }
                foreach (var (dx, dy) in Directions)
                {
                    var next = tile.Offset(dx, dy);
                    if (_buildingAt.TryGetValue(next, out var buildingId))
                    {
                        VisitBuilding(buildingId);
                    }
                    else if (_conductors.TryGetValue(next, out var conductor) && Conducts(conductor))
                    {
                        VisitTile(next);
                    }
                }
            }
            while (buildings.TryDequeue(out var id))
            {
                foreach (var border in _buildings[id].Border())
                {
                    if (_buildingAt.TryGetValue(border, out var other) && IsConductorBuilding(_buildings[other]))
                    {
                        VisitBuilding(other);
                    }
                    else if (_conductors.TryGetValue(border, out var conductor) && Conducts(conductor))
                    {
                        VisitTile(border);
                    }
                }
            }
        }

        // A building without any pipe is not a network
        if (network.Conductors.Count == 0)
        {
            foreach (var id in network.Buildings)
            {
                _networkOfBuilding.Remove(id);
            }
            return;
        }

        var anchor = network.Conductors.First();
        network.District = WorldLayout.DistrictOf(anchor.X, anchor.Y);
        network.Links = BuildLinks(network);
        _networks[network.Id] = network;
        Changes.Networks.Add(network.Id);
    }

    private (double Loss, double Throughput) ConductorStats(Conductor conductor)
    {
        if (conductor.Kind == ConductorKind.Gate)
        {
            return (0, double.MaxValue);
        }
        var tier = Content.PipeTier(conductor.Tier);
        return (tier.LossPerTile, tier.Throughput * (1 + ThroughputBonus(conductor.Owner)));
    }

    private readonly record struct TileStats(double Loss, double Throughput, IReadOnlySet<string>? Filter, bool Priority);

    private static readonly IReadOnlySet<string> EnergyOnly = new HashSet<string> { "energy" };

    private TileStats StatsOf(TilePos tile)
    {
        if (_conductors.TryGetValue(tile, out var conductor))
        {
            var (loss, throughput) = ConductorStats(conductor);
            return new TileStats(loss, throughput, null, false);
        }
        var building = _buildings[_buildingAt[tile]];
        var definition = Content.Building(building.Type);
        if (definition.Kind == BuildingKind.PowerLine)
        {
            return new TileStats(0, definition.Throughput, EnergyOnly, false);
        }
        if (definition.Kind == BuildingKind.CargoDepot)
        {
            // Trucks carry everything, without losses, more with every level
            return new TileStats(0, definition.Throughput * building.Level, null, false);
        }
        return new TileStats(0, double.MaxValue, definition.Kind == BuildingKind.Valve && building.Filter.Count > 0 ? building.Filter : null, definition.Kind == BuildingKind.PriorityValve);
    }

    /// <summary>
    /// The links between the buildings of a network: for every pair of buildings touching the same pipe group the path
    /// with the least loss, its efficiency and the smallest throughput on it. With valves in the network the links are
    /// made per resource (a valve only lets its resources through).
    /// </summary>
    private List<FlowLink> BuildLinks(Network network)
    {
        var stats = network.Conductors.ToDictionary(t => t, StatsOf);
        // The cables between pylons: extra neighbours besides the four next tiles
        var cables = new Dictionary<TilePos, List<TilePos>>();
        foreach (var tile in network.Conductors)
        {
            if (_buildingAt.TryGetValue(tile, out var id) && _buildings[id].Position == tile && (IsPylon(_buildings[id]) || IsCargoDepot(_buildings[id])))
            {
                cables[tile] = RemoteLinks(_buildings[id]).Select(t => t.Position).Where(network.Conductors.Contains).ToList();
            }
        }
        var members = network.Buildings.Where(t => !IsConductorBuilding(_buildings[t])).OrderBy(t => t).ToList();
        var touching = new Dictionary<TilePos, List<long>>();
        foreach (var id in members)
        {
            foreach (var border in _buildings[id].Border())
            {
                if (network.Conductors.Contains(border))
                {
                    if (!touching.TryGetValue(border, out var list))
                    {
                        touching[border] = list = new List<long>();
                    }
                    if (!list.Contains(id))
                    {
                        list.Add(id);
                    }
                }
            }
        }

        // Without valves one set of links carries everything, otherwise every resource has its own
        List<string?> resources = [null];
        if (stats.Values.Any(t => t.Filter != null))
        {
            resources = members
                .Select(t => Content.Building(_buildings[t].Type))
                .SelectMany(t => t.In.Keys.Concat(t.Out.Keys).Concat(t.OutFromDeposit))
                .Where(t => t != GameContent.ResearchPoints)
                .Distinct()
                .Order(StringComparer.Ordinal)
                .Cast<string?>()
                .ToList();
        }

        var best = new Dictionary<(long, long, string?), FlowLink>();
        foreach (var resource in resources)
        {
            bool Passable(TilePos tile) => resource == null || stats[tile].Filter is not { } filter || filter.Contains(resource);
            IReadOnlySet<string>? only = resource == null ? null : new HashSet<string> { resource };

            foreach (var from in members)
            {
                // Dijkstra over the pipe tiles, cost = -log(efficiency) so the best path has the highest efficiency
                var cost = new Dictionary<TilePos, double>();
                var previous = new Dictionary<TilePos, TilePos>();
                var queue = new PriorityQueue<TilePos, double>();
                foreach (var border in _buildings[from].Border().Where(t => network.Conductors.Contains(t) && Passable(t)).Distinct())
                {
                    var start = -Math.Log(1 - stats[border].Loss);
                    cost[border] = start;
                    queue.Enqueue(border, start);
                }

                var done = new HashSet<TilePos>();
                while (queue.TryDequeue(out var tile, out var c))
                {
                    if (!done.Add(tile))
                    {
                        continue;
                    }
                    var neighbours = Directions.Select(d => tile.Offset(d.X, d.Y));
                    if (cables.TryGetValue(tile, out var cable))
                    {
                        neighbours = neighbours.Concat(cable);
                    }
                    foreach (var next in neighbours)
                    {
                        if (!network.Conductors.Contains(next) || done.Contains(next) || !Passable(next))
                        {
                            continue;
                        }
                        var nextCost = c - Math.Log(1 - stats[next].Loss);
                        if (!cost.TryGetValue(next, out var known) || nextCost < known - 1e-12)
                        {
                            cost[next] = nextCost;
                            previous[next] = tile;
                            queue.Enqueue(next, nextCost);
                        }
                    }
                }

                foreach (var (tile, others) in touching)
                {
                    if (!done.Contains(tile))
                    {
                        continue;
                    }
                    foreach (var to in others)
                    {
                        if (to <= from)
                        {
                            continue;
                        }
                        var path = new List<TilePos> { tile };
                        var capacity = stats[tile].Throughput;
                        var current = tile;
                        while (previous.TryGetValue(current, out var p))
                        {
                            path.Add(p);
                            capacity = Math.Min(capacity, stats[p].Throughput);
                            current = p;
                        }
                        path.Reverse();
                        var link = new FlowLink(from, to, Math.Exp(-cost[tile]), capacity, path, only, path.Any(t => stats[t].Priority));
                        if (!best.TryGetValue((from, to, resource), out var existing) || link.Efficiency > existing.Efficiency + 1e-12)
                        {
                            best[(from, to, resource)] = link;
                        }
                    }
                }
            }
        }
        return best.Values.ToList();
    }

    // ---------------------------------------------------------------- Solving

    public static readonly double[] RichnessMultiplier = [0, 0.6, 1.0, 1.3, 1.6, 2.0];

    /// <summary>How well a building produces at its spot (wind, sun, deposit richness, forest)</summary>
    public double LocationMultiplier(Building building, BuildingDefinition definition)
    {
        var footprint = building.Footprint().Select(Tile).ToList();
        switch (definition.Modifier)
        {
            case ProductionModifier.Wind:
                return footprint.Average(t => t.Wind);
            case ProductionModifier.Sun:
                return footprint.Average(t => t.Sun);
            case ProductionModifier.Richness:
                var deposit = definition.Placement.Deposit;
                var richness = footprint.Where(t => deposit == null || DepositMatches(deposit, t.Deposit)).Select(t => t.Richness).DefaultIfEmpty(0).Max();
                return RichnessMultiplier[Math.Clamp(richness, 0, RichnessMultiplier.Length - 1)];
            case ProductionModifier.Forest:
                return Math.Clamp(ForestAround(building.Position, building.Size) / 12.0, 0.25, 1.5);
            default:
                return 1;
        }
    }

    /// <summary>The outputs of a building at level 1 at its spot (the Deep Mine produces what its deposit holds)</summary>
    private IReadOnlyDictionary<string, double> BaseOutputs(Building building, BuildingDefinition definition)
    {
        if (definition.OutFromDeposit.Count == 0)
        {
            return definition.Out;
        }
        var amount = definition.Out.Values.FirstOrDefault();
        var deposit = building.Footprint().Select(Tile).Where(t => t.Deposit != null && definition.OutFromDeposit.Contains(t.Deposit))
            .OrderByDescending(t => t.Richness).Select(t => t.Deposit).FirstOrDefault() ?? definition.OutFromDeposit[0];
        return new Dictionary<string, double> { [deposit] = amount };
    }

    public double StorageCapacity(Building building, BuildingDefinition definition) =>
        definition.Capacity * Content.Progression.OutputMultiplier(building.Level);

    public double TradingPostThroughput(Building building, BuildingDefinition definition) =>
        definition.Throughput * Content.Progression.OutputMultiplier(building.Level);

    /// <summary>The building as the flow solver sees it, or null when it takes no part in flows</summary>
    public FlowNode? ToFlowNode(Building building)
    {
        if (building.UnderConstruction)
        {
            return null;
        }
        var definition = Content.Building(building.Type);
        var level = Content.Progression.OutputMultiplier(building.Level);
        switch (definition.Kind)
        {
            case BuildingKind.Storage:
                return new FlowNode
                {
                    Id = building.Id,
                    Owner = building.Owner,
                    Kind = FlowNodeKind.Storage,
                    StorageCanAccept = building.StoredTotal < StorageCapacity(building, definition) - StorageEpsilon,
                    StorageContents = building.Contents.Where(t => t.Value > StorageEpsilon).Select(t => t.Key).ToHashSet(),
                };
            case BuildingKind.TradingPost:
                var company = Company(building.Owner);
                return new FlowNode
                {
                    Id = building.Id,
                    Owner = building.Owner,
                    Kind = FlowNodeKind.TradingPost,
                    Throughput = TradingPostThroughput(building, definition),
                    // Hub reputation: +1% per level above 1
                    PriceBonus = definition.PriceBonus + (company == null ? 0 : (ReputationLevel(company) - 1) * 0.01) + ManagerPriceBonus(company),
                    // Imports cost gold: a company without gold can't import
                    Imports = company is { Gold: > 0 } ? building.Imports : new HashSet<string>(),
                };
        }
        if (definition.In.Count == 0 && definition.Out.Count == 0 && definition.ResearchPerMinute <= 0)
        {
            return null;
        }
        var location = LocationMultiplier(building, definition) * EventOutputFactor(building, definition) * WonderOutputFactor(building);
        var owner = Company(building.Owner);
        return new FlowNode
        {
            Id = building.Id,
            Owner = building.Owner,
            Kind = FlowNodeKind.Building,
            In = definition.In.Where(t => t.Key != GameContent.ResearchPoints).ToDictionary(t => t.Key, t => t.Value * level),
            Out = BaseOutputs(building, definition)
                .Where(t => t.Key != GameContent.ResearchPoints)
                .ToDictionary(t => t.Key, t => t.Value * level * location * (1 + OutputBonus(owner, definition, t.Key))),
            ResearchPerMinute = definition.ResearchPerMinute * level * WonderResearchFactor(building),
        };
    }

    /// <summary>Prices and trade rules for a network: the hub market of its district</summary>
    public FlowMarket MarketFor(Network network) => new()
    {
        HubBuyPrice = resource => HubBuyPrice(network.District, resource),
        HubSellPrice = resource => HubSellPrice(network.District, resource),
        SellerPrice = (owner, resource) => Company(owner) is { } company ? SellPrice(company, resource) : HubSellPrice(network.District, resource),
        BuyerMaxPrice = (owner, resource) => Company(owner) is { } company ? BuyMaxPrice(company, resource) : HubSellPrice(network.District, resource),
        TradeAllowed = (seller, buyer, resource) =>
            Company(seller) is { } s && Company(buyer) is { Gold: > 0 } && !s.NoTrade.Contains(resource),
        LinkTradeAllowed = (link, seller, buyer, resource) => PathAllowsTrade(link.Path, seller, buyer, resource),
        IgnoreMaxPrice = MembersTradeFreely,
        Contracts = ActiveContracts(network.Buildings.Select(t => _buildings[t].Owner).ToHashSet()),
    };

    private void Solve(Network network)
    {
        var nodes = network.Buildings.Select(t => ToFlowNode(_buildings[t])).OfType<FlowNode>().ToList();

        // Every 50 buildings need a pump station; without enough of them everything runs slower
        var counted = nodes.Count(t => t.Kind == FlowNodeKind.Building);
        var pumps = network.Buildings.Count(t => _buildings[t] is { UnderConstruction: false } b && Content.Building(b.Type).Kind == BuildingKind.Pump);
        var allowed = BuildingsPerPump * (1 + pumps);
        network.PumpFactor = counted > allowed ? allowed / (double)counted : 1;
        if (network.PumpFactor < 1)
        {
            nodes = nodes.Select(t => t.Kind == FlowNodeKind.Building && Content.Building(_buildings[t.Id].Type).Kind != BuildingKind.Pump ? t.WithMaxRate(t.MaxRate * network.PumpFactor) : t).ToList();
        }

        var ids = nodes.Select(t => t.Id).ToHashSet();
        var links = network.Links.Where(t => ids.Contains(t.A) && ids.Contains(t.B)).ToList();
        network.Solution = nodes.Count < 2 ? FlowSolution.Empty : WithTolls(network, FlowSolver.Solve(nodes, links, MarketFor(network)));
        network.NeedsSolve = false;
        Changes.Networks.Add(network.Id);
    }

    /// <summary>Gold per minute of every company over all its networks</summary>
    public Dictionary<long, double> GoldPerMinute()
    {
        var total = new Dictionary<long, double>();
        foreach (var network in Networks)
        {
            foreach (var (company, gold) in network.Solution.GoldPerMinute)
            {
                total[company] = (total.TryGetValue(company, out var v) ? v : 0) + gold;
            }
        }
        return total;
    }

    /// <summary>Research points per minute of every company</summary>
    public Dictionary<long, double> ResearchPerMinute()
    {
        var total = new Dictionary<long, double>();
        foreach (var network in Networks)
        {
            foreach (var (company, research) in network.Solution.ResearchPerMinute)
            {
                total[company] = (total.TryGetValue(company, out var v) ? v : 0) + research;
            }
        }
        return total;
    }

    /// <summary>Brings all networks up to date and solves the ones that changed</summary>
    public void SolveNetworks()
    {
        UpdateNetworks();
        foreach (var network in _networks.Values.Where(t => t.NeedsSolve).ToList())
        {
            Solve(network);
        }
    }
}

using TransportPipes.Game.Simulation;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>
/// What a change would bring, said as it will happen (the owner, 2026-10-31: "whenever something says x will happen, it
/// actually does, so for example the grow button should match exactly"). A what-if solves a network the way its real
/// solve does: the nodes it was solved with, its pump stations, its Yard gate (what neighbours bring and take this
/// round), and a better pipe laid for real (its loss and its throughput); and every promise reads one measure of it,
/// what the company earns a minute once the market has settled on what it sells.
/// </summary>
public sealed partial class GameWorld
{
    /// <summary>
    /// A network solved with some of its buildings changed (a level), some added (a building piped to the anchor), its
    /// links changed, or some of its pipe tiles a better tier; nothing in the world changes. pumps: false leaves its
    /// buildings at full speed (what a Pump Station would bring)
    /// </summary>
    internal FlowSolution SolveWhatIf(Network network, Func<FlowNode, FlowNode>? change = null, IReadOnlyList<FlowNode>? extra = null, long? anchor = null,
        Func<FlowLink, FlowLink>? changeLink = null, IReadOnlyDictionary<TilePos, int>? pipeTiers = null, bool pumps = true, bool yard = true)
    {
        _whatIfs++;
        if (network.NeedsSolve)
        {
            Solve(network);
        }
        // The nodes it was solved with (a Storage's contents move on between solves: a fresh node would differ)
        var nodes = pumps ? network.Nodes.ToList()
            : network.Buildings.Select(t => ToFlowNode(_buildings[t])).OfType<FlowNode>().ToList();
        if (change != null)
        {
            nodes = nodes.Select(change).ToList();
        }
        // A better pipe, laid for real while it is solved, then taken up again
        var saved = new List<(Conductor Conductor, int Tier)>();
        var gateLinks = network.GateLinks;
        try
        {
            var links = network.Links;
            if (pipeTiers is { Count: > 0 })
            {
                foreach (var (tile, tier) in pipeTiers)
                {
                    if (_conductors.TryGetValue(tile, out var conductor) && conductor.Kind == ConductorKind.Pipe && conductor.Tier < tier)
                    {
                        saved.Add((conductor, conductor.Tier));
                        conductor.Tier = tier;
                    }
                }
                if (saved.Count > 0)
                {
                    links = BuildLinks(network);
                }
            }
            var ids = nodes.Select(t => t.Id).ToHashSet();
            var chosen = links.Where(t => ids.Contains(t.A) && ids.Contains(t.B)).ToList();
            if (changeLink != null)
            {
                chosen = chosen.Select(changeLink).ToList();
            }
            if (extra is { Count: > 0 })
            {
                // A new building piped to the anchor: next to it, and reaching what it reaches
                var near = anchor ?? nodes.FirstOrDefault(t => t.Owner == network.Owner)?.Id;
                foreach (var node in extra)
                {
                    if (near is { } a)
                    {
                        chosen.Add(new FlowLink(node.Id, a, 0.99, double.PositiveInfinity, []));
                        chosen.AddRange(links.Where(t => t.A == a || t.B == a).Select(t => t with { A = node.Id, B = t.A == a ? t.B : t.A }));
                    }
                }
                nodes.AddRange(extra);
            }
            var gateNodes = GateNodes(network, chosen);
            // And the Yard's next round, within a minute: what neighbours have spare comes in, what they lack goes out
            var sells = new Dictionary<long, (Company Buyer, string Resource)>();
            if (yard)
            {
                gateNodes.AddRange(NextRound(network, chosen, sells));
            }
            var all = gateNodes.Count == 0 ? nodes : [.. nodes, .. gateNodes.Select(t => t.Node)];
            if (all.Count < 2)
            {
                return FlowSolution.Empty;
            }
            var raw = FlowSolver.Solve(all, chosen, MarketFor(network));
            var solution = SplitGateFlows(raw, gateNodes, out _);
            if (sells.Count == 0 || Company(network.Owner) is not { } owner)
            {
                return solution;
            }
            // What it would sell them, at its price for them (the Yard pays it apart from the network)
            var gold = solution.GoldPerMinute.ToDictionary(t => t.Key, t => t.Value);
            foreach (var flow in raw.Flows)
            {
                if (sells.TryGetValue(flow.To, out var sale))
                {
                    gold[owner.Id] = gold.GetValueOrDefault(owner.Id) + flow.Delivered * SettlementPrice(owner, sale.Buyer, sale.Resource);
                }
            }
            return new FlowSolution { Rates = solution.Rates, Flows = solution.Flows, GoldPerMinute = gold, StorageNet = solution.StorageNet, Rounds = solution.Rounds };
        }
        finally
        {
            foreach (var (conductor, tier) in saved)
            {
                conductor.Tier = tier;
            }
            network.GateLinks = gateLinks;
        }
    }

    /// <summary>
    /// A company's networks with a new pipe laid (its tiles, of a tier): the network it would make, flooded from the pipe
    /// over the map as the real one will be (the pipe laid while it is worked out, then taken up again) and solved as the
    /// real solve does, with the Yard's next round; and the networks it takes the place of. Null: no new tile.
    /// </summary>
    internal (Network Network, FlowSolution Solution, List<Network> Replaced)? PipeWhatIf(Company company, IReadOnlyList<TilePos> tiles, int tier = 1)
    {
        SolveNetworks();
        var laid = new List<TilePos>();
        try
        {
            foreach (var tile in tiles.Distinct())
            {
                if (!_conductors.ContainsKey(tile) && !_buildingAt.ContainsKey(tile))
                {
                    _conductors[tile] = new Conductor { Pos = tile, Owner = company.Id, Tier = tier };
                    laid.Add(tile);
                }
            }
            if (laid.Count == 0)
            {
                return null;
            }
            var network = new Network { Id = -1 };
            Spread(network, laid[0], null);
            Finish(network);
            network.Nodes = PumpedNodes(network);
            network.NeedsSolve = false;
            var replaced = network.Conductors.Select(t => _networkOfTile.GetValueOrDefault(t))
                .Concat(network.Buildings.Select(t => _networkOfBuilding.GetValueOrDefault(t)))
                .Where(_networks.ContainsKey).Distinct().Select(t => _networks[t]).ToList();
            return (network, SolveWhatIf(network), replaced);
        }
        finally
        {
            foreach (var tile in laid)
            {
                _conductors.Remove(tile);
            }
        }
    }

    private readonly Dictionary<(long Company, int Tiles, int Tier), ((long, long) Stamp, double Gain)> _pipeGains = new();

    /// <summary>
    /// What a new pipe brings a minute, as it will be: the settled income with the network it makes, less without (once
    /// until the world is solved again: the next steps ask for it after every command, and on a big network it is a
    /// tenth of a second)
    /// </summary>
    public double PipeGain(Company company, IReadOnlyList<TilePos> tiles, int tier = 1)
    {
        var hash = new HashCode();
        foreach (var tile in tiles)
        {
            hash.Add(tile);
        }
        var key = (company.Id, hash.ToHashCode(), tier);
        SolveNetworks();
        var stamp = (_solves, _couplings);
        if (_pipeGains.TryGetValue(key, out var known) && known.Stamp == stamp)
        {
            return known.Gain;
        }
        var gain = SolvedPipeGain(company, tiles, tier);
        if (_pipeGains.Count > 5_000)
        {
            _pipeGains.Clear();
        }
        _pipeGains[key] = (stamp, gain);
        return gain;
    }

    private double SolvedPipeGain(Company company, IReadOnlyList<TilePos> tiles, int tier)
    {
        if (PipeWhatIf(company, tiles, tier) is not { } whatIf)
        {
            return 0;
        }
        var replaced = whatIf.Replaced.Select(t => t.Id).ToHashSet();
        var bases = whatIf.Replaced.ToDictionary(t => t.Id, BaseWhatIf);
        return SettledIncome(company, t => replaced.Contains(t.Id) ? FlowSolution.Empty : null, added: (whatIf.Network, whatIf.Solution))
            - SettledIncome(company, t => bases.GetValueOrDefault(t.Id));
    }

    /// <summary>
    /// The Yard's next round for a network's what-if (it comes within a minute): its neighbours' spare it could take, at
    /// their price for its owner, and what they lack that it could sell them (sells: those buyers' nodes)
    /// </summary>
    private List<GateNode> NextRound(Network network, List<FlowLink> links, Dictionary<long, (Company Buyer, string Resource)> sells)
    {
        var nodes = new List<GateNode>();
        if (Company(network.Owner) is not { } owner)
        {
            return nodes;
        }
        var nextId = GateNodeIds - 500_000;
        foreach (var gate in network.Gates)
        {
            if (EstateOfGate(gate) is not { OwnerId: { } id } estate || id != owner.Id)
            {
                continue;
            }
            var key = KeyOf(estate);
            void Add(FlowNode node, string resource, bool import, int priceClass)
            {
                nodes.Add(new GateNode(node, key, resource, import, priceClass));
                foreach (var link in network.GateLinks.Where(t => t.Gate == gate && (t.Resources == null || t.Resources.Contains(resource))))
                {
                    links.Add(new FlowLink(link.Building, node.Id, link.Efficiency, link.Capacity, link.Path, new HashSet<string> { resource }));
                }
            }
            foreach (var other in ResidentsNow(estate.Station).Where(t => t.Connected && t.Company.Id != owner.Id
                         && TradesWith(owner, t.Company) && TradesWith(t.Company, owner)))
            {
                foreach (var (resource, spare) in other.Spare.Where(t => t.Value > 0.01 && !owner.NoBuy.Contains(t.Key)).OrderBy(t => t.Key, StringComparer.Ordinal))
                {
                    var priceClass = GateClass(other.Company, owner, resource);
                    Add(new FlowNode { Id = nextId--, Owner = PriceClassOwner(priceClass), Kind = FlowNodeKind.Building, Out = new Dictionary<string, double> { [resource] = spare } }, resource, true, priceClass);
                }
                if (owner.Gold <= 0)
                {
                    continue;
                }
                foreach (var (resource, lack) in other.Lacks.Where(t => t.Value > 0.01 && !owner.NoTrade.Contains(t.Key)).OrderBy(t => t.Key, StringComparer.Ordinal))
                {
                    var node = new FlowNode { Id = nextId--, Owner = GateBuyer, Kind = FlowNodeKind.Building, In = new Dictionary<string, double> { [resource] = lack } };
                    sells[node.Id] = (other.Company, resource);
                    Add(node, resource, false, 0);
                }
            }
        }
        return nodes;
    }

    private (long Stamp, Dictionary<StationRef, List<YardResident>> Residents)? _residents;

    /// <summary>A Yard's residents with what they have spare and lack now (once per solve of the world)</summary>
    private List<YardResident> ResidentsNow(StationRef station)
    {
        if (_residents is not { } cache || cache.Stamp != _solves)
        {
            cache = (_solves, new());
            _residents = cache;
        }
        if (!cache.Residents.TryGetValue(station, out var residents))
        {
            residents = YardResidents(station);
            // Solving for them (YardResidents solves first) leaves the stamp as it is
            cache = (_solves, cache.Residents);
            _residents = cache;
            cache.Residents[station] = residents;
        }
        return residents;
    }

    private readonly Dictionary<int, ((int, long, long) Stamp, FlowSolution Solution)> _bases = new();

    /// <summary>
    /// A network as a what-if sees it without a change: its own solution, or with the Yard's next round in it when that
    /// brings or takes something (once per solve of the world); every gain is a what-if less this
    /// </summary>
    internal FlowSolution BaseWhatIf(Network network)
    {
        SolveNetworks();
        if (network.Gates.Count == 0)
        {
            return network.Solution;
        }
        var stamp = StampOf(network);
        if (_bases.TryGetValue(network.Id, out var known) && known.Stamp == stamp)
        {
            return known.Solution;
        }
        var solution = SolveWhatIf(network);
        _bases[network.Id] = (stamp, solution);
        return solution;
    }

    /// <summary>What a change of networks brings a minute: the settled income with their what-ifs, less with their bases</summary>
    public double SettledGain(Company company, IReadOnlyDictionary<int, FlowSolution> whatIfs, double orderFactor = 1, Func<string, double>? goodFactor = null)
    {
        var changed = _networks.Values.Where(t => whatIfs.ContainsKey(t.Id)).ToDictionary(t => t.Id, BaseWhatIf);
        return SettledIncome(company, t => whatIfs.GetValueOrDefault(t.Id), orderFactor, goodFactor) - SettledIncome(company, t => changed.GetValueOrDefault(t.Id));
    }

    /// <summary>
    /// What a company earns a minute once the market has settled: each of its networks' gold (as solved, or as a what-if
    /// gives it), what it sells at the hub at the price the market settles on for all of it (each town's market apart; a
    /// Hub Order orderFactor times bigger: trading post levels), and the Yard's pay. This is the top bar's number once
    /// the market has looked again (it does every minute): every promise is a difference of two of these. added: a
    /// network there would be besides (a new pipe's)
    /// </summary>
    public double SettledIncome(Company company, Func<Network, FlowSolution?>? whatIf = null, double orderFactor = 1, Func<string, double>? goodFactor = null,
        (Network Network, FlowSolution Solution)? added = null)
    {
        SolveNetworks();
        var gold = 0.0;
        var sales = new Dictionary<(DistrictCoord District, string Resource), (double Units, double Value)>();
        void Sale(DistrictCoord district, string resource, double units, double value)
        {
            var (u, v) = sales.GetValueOrDefault((district, resource));
            sales[(district, resource)] = (u + units, v + value);
        }
        var own = _networks.Values.Where(t => t.Owner == company.Id).Select(t => (Network: t, Solution: whatIf?.Invoke(t) ?? t.Solution));
        foreach (var (network, solution) in added is { } more ? own.Append(more) : own)
        {
            gold += solution.GoldPerMinute.GetValueOrDefault(company.Id);
            foreach (var flow in solution.Flows)
            {
                if (flow.Kind == FlowKind.Sale && _buildings.TryGetValue(flow.From, out var from) && from.Owner == company.Id)
                {
                    gold -= flow.Delivered * flow.PricePerUnit;
                    Sale(network.District, flow.Resource, flow.Delivered, flow.Delivered * flow.PricePerUnit);
                }
            }
        }
        foreach (var entry in TradeIncomeOfCompany(company.Id))
        {
            // What a neighbour didn't take is paid at the hub's price, and counts for the Hub Order
            if (entry.Floor)
            {
                Sale(company.District, entry.Resource, entry.Units, entry.Gold);
            }
            else
            {
                gold += entry.Gold;
            }
        }
        foreach (var ((district, resource), (units, value)) in sales)
        {
            gold += HubOrder(company, district, resource) <= 0 ? value
                : value / Math.Max(0.01, HubMarketOf(company, district).Multiplier(resource)) * SettledMultiplier(company, district, resource, units, orderFactor * (goodFactor?.Invoke(resource) ?? 1));
        }
        return gold;
    }


    private (long Stamp, Dictionary<long, List<TradeIncomeEntry>> ByCompany)? _tradeIncomeCache;

    /// <summary>The Yard's pay entries of a company (once per solve of the world: every what-if reads them)</summary>
    private List<TradeIncomeEntry> TradeIncomeOfCompany(long companyId)
    {
        var stamp = _solves ^ (LastTick.Ticks << 1);
        if (_tradeIncomeCache is not { } cache || cache.Stamp != stamp)
        {
            cache = (stamp, TradeIncomeEntries().GroupBy(t => t.Seller.Id).ToDictionary(t => t.Key, t => t.ToList()));
            _tradeIncomeCache = cache;
        }
        return cache.ByCompany.GetValueOrDefault(companyId) ?? [];
    }

    /// <summary>How many solves the world has done (what was worked out from them is worked out again after one)</summary>
    private long _solves;

    /// <summary>How many Yard rounds there were</summary>
    private long _couplings;

    /// <summary>
    /// What a network's what-ifs depend on: its own solves, its company's market (re-solved when it moves) and the Yard's
    /// rounds; worked out once for as long as it stays
    /// </summary>
    private (int Network, long Solves, long Couplings) StampOf(Network network) => (network.Id, network.Solves, network.Gates.Count == 0 ? 0 : _couplings);
}

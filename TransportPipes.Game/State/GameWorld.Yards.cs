using TransportPipes.Game.Content;
using TransportPipes.Game.Simulation;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>An estate by its district and slot</summary>
public readonly record struct EstateKey(DistrictCoord District, int Slot)
{
    public override string ToString() => $"E({District.X},{District.Y}:{Slot})";
}

/// <summary>
/// A flow through an estate's Yard gate (v1.2 §7): what a building sends to the gate for the station's buyers, or gets
/// from it. Price class 0: from players at the one player price, 1: from Houses at the House price.
/// </summary>
public sealed record GateFlow(long Building, EstateKey Estate, string Resource, double Sent, double Delivered, bool Import, int PriceClass, double Price);

/// <summary>
/// The Yard (v1.2 §7): every estate trades with its station through its Yard gate. Networks never cross a gate; the
/// gate is a buyer (what the estate exports) and a seller (what it imports) inside the estate's own network. Once a
/// minute at most the Yard matches what every estate has spare with what the others need: the Yard is no buyer of its
/// own, it only passes goods from one resident to another, at one price. What a seller sends arrives at its buyers the
/// round after, and the seller is paid for what the buyer takes. Nothing is ever created: goods nobody takes are lost on
/// the way (the next round sends less).
/// </summary>
public sealed partial class GameWorld
{
    /// <summary>The owner of the gate nodes as the solver sees them (never a company)</summary>
    private const long GateBuyer = -1;
    private const long PlayerSeller = -2;
    private const long HouseSeller = -3;

    /// <summary>The gate nodes have ids below this (buildings and companies are positive)</summary>
    private const long GateNodeIds = -1_000_000;

    /// <summary>Terms change only when they move more than this share (damping, v1.2 §7)</summary>
    private const double TermsTolerance = 0.01;

    public static readonly TimeSpan CouplingInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan MaxCouplingInterval = TimeSpan.FromMinutes(16);

    /// <summary>What an estate's gate trades now</summary>
    private sealed class GateTerms
    {
        /// <summary>What the gate takes from the estate's network per minute, for its buyers</summary>
        public Dictionary<string, double> Export { get; set; } = new();

        /// <summary>What the gate brings per minute, by resource and price class, from which companies</summary>
        public Dictionary<(string Resource, int PriceClass), Dictionary<long, double>> Import { get; set; } = new();

        /// <summary>The last matching: how much of what this estate sends to which buyer (it arrives the round after)</summary>
        public Dictionary<(EstateKey Buyer, string Resource), double> Matched { get; set; } = new();

        public bool IsEmpty => Export.Count == 0 && Import.Count == 0 && Matched.Count == 0;
    }

    private readonly Dictionary<EstateKey, GateTerms> _gateTerms = new();
    private DateTime? _nextCoupling;
    private TimeSpan _couplingInterval = CouplingInterval;

    public static EstateKey KeyOf(Estate estate) => new(estate.District, estate.Slot);

    /// <summary>The estate whose Yard gate is on this tile</summary>
    private Estate? EstateOfGate(TilePos gate) => EstateAt(gate) is { } estate && estate.YardGate == gate ? estate : null;

    /// <summary>The network an estate's Yard gate is in (null: no pipe of its owner reaches the gate)</summary>
    public Network? GateNetwork(Estate estate) => NetworkAt(estate.YardGate) is { } network && network.Buildings.Count > 0 ? network : null;

    // ---------------------------------------------------------------- Gate nodes in a solve

    private sealed record GateNode(FlowNode Node, EstateKey Estate, string Resource, bool Import, int PriceClass);

    /// <summary>The gate nodes of a network for the solver, with their links to the buildings that reach the gate</summary>
    private List<GateNode> GateNodes(Network network, List<FlowLink> links)
    {
        var nodes = new List<GateNode>();
        var nextId = GateNodeIds;
        foreach (var gate in network.Gates)
        {
            if (EstateOfGate(gate) is not { } estate || !_gateTerms.TryGetValue(KeyOf(estate), out var terms))
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
            foreach (var (resource, amount) in terms.Export.Where(t => t.Value > 1e-9).OrderBy(t => t.Key, StringComparer.Ordinal))
            {
                Add(new FlowNode { Id = nextId--, Owner = GateBuyer, Kind = FlowNodeKind.Building, In = new Dictionary<string, double> { [resource] = amount } }, resource, false, 0);
            }
            foreach (var ((resource, priceClass), sources) in terms.Import.OrderBy(t => t.Key.Resource, StringComparer.Ordinal).ThenBy(t => t.Key.PriceClass))
            {
                var total = sources.Values.Sum();
                if (total > 1e-9)
                {
                    Add(new FlowNode { Id = nextId--, Owner = priceClass == 0 ? PlayerSeller : HouseSeller, Kind = FlowNodeKind.Building, Out = new Dictionary<string, double> { [resource] = total } }, resource, true, priceClass);
                }
            }
        }
        return nodes;
    }

    /// <summary>
    /// Takes the gate nodes out of a solution: their flows become the network's gate flows, a seller's pay for what it
    /// sends comes from its buyers (when they take it), and the solver's rates and gold keep only real buildings
    /// </summary>
    private FlowSolution SplitGateFlows(Network network, FlowSolution solution, List<GateNode> gateNodes)
    {
        if (gateNodes.Count == 0)
        {
            network.GateFlows = [];
            return solution;
        }
        var byId = gateNodes.ToDictionary(t => t.Node.Id);
        var gateFlows = new List<GateFlow>();
        var flows = new List<Flow>();
        var gold = solution.GoldPerMinute.Where(t => t.Key > 0).ToDictionary(t => t.Key, t => t.Value);
        foreach (var flow in solution.Flows)
        {
            if (byId.TryGetValue(flow.To, out var buyer))
            {
                gateFlows.Add(new GateFlow(flow.From, buyer.Estate, flow.Resource, flow.Sent, flow.Delivered, false, 0, flow.PricePerUnit));
                // Paid by the buyers when they take it, not by the gate
                if (_buildings.TryGetValue(flow.From, out var from))
                {
                    gold[from.Owner] = gold.GetValueOrDefault(from.Owner) - flow.Delivered * flow.PricePerUnit;
                }
            }
            else if (byId.TryGetValue(flow.From, out var seller))
            {
                gateFlows.Add(new GateFlow(flow.To, seller.Estate, flow.Resource, flow.Sent, flow.Delivered, true, seller.PriceClass, flow.PricePerUnit));
                // The buyer pays for what leaves the Yard: what its own pipes lose on the way is its own
                if (_buildings.TryGetValue(flow.To, out var to))
                {
                    gold[to.Owner] = gold.GetValueOrDefault(to.Owner) - (flow.Sent - flow.Delivered) * flow.PricePerUnit;
                }
            }
            else
            {
                flows.Add(flow);
            }
        }
        network.GateFlows = gateFlows;
        return new FlowSolution
        {
            Rates = solution.Rates.Where(t => t.Key > 0).ToDictionary(t => t.Key, t => t.Value),
            Flows = flows,
            GoldPerMinute = gold,
            ResearchPerMinute = solution.ResearchPerMinute,
            StorageNet = solution.StorageNet,
            Rounds = solution.Rounds,
        };
    }

    /// <summary>What a buyer of this class pays per unit at the gate (the one player price, or the Houses' price)</summary>
    private double GateSellerPrice(long owner, string resource) =>
        owner == HouseSeller ? HouseSellPrice(resource) : PlayerPrice(resource);

    /// <summary>What Houses charge players</summary>
    public double HouseSellPrice(string resource) => Math.Round(BasePrice(resource) * Content.Progression.Together.HouseSell, 4);

    /// <summary>What Houses pay a company for a good: a little more than its hub, never more than the House price times the base</summary>
    public double HouseBuyPrice(Company seller, string resource)
    {
        var together = Content.Progression.Together;
        return Math.Round(Math.Min(HubBuyPrice(seller, resource) * together.HouseBuy, BasePrice(resource) * together.HouseBuy), 4);
    }

    /// <summary>The gold per unit a seller gets for what a buyer took at the gate</summary>
    private double SettlementPrice(Company seller, Company buyer, string resource) =>
        seller.IsNpc ? HouseSellPrice(resource) : buyer.IsNpc ? HouseBuyPrice(seller, resource) : PlayerPrice(resource);

    /// <summary>
    /// The sellers of what a network's gates delivered get their pay (what the buyer paid, in proportion to what each of
    /// them sent); returns the gold per company for the earnings
    /// </summary>
    private void SettleGateFlows(Network network, double minutes, Dictionary<long, double> earnings)
    {
        foreach (var flow in network.GateFlows)
        {
            if (!_buildings.TryGetValue(flow.Building, out var building) || Company(building.Owner) is not { } owner)
            {
                continue;
            }
            if (!flow.Import)
            {
                Add(owner.Report.Produced, flow.Resource, flow.Sent * minutes);
                AddStat(owner, $"produced:{flow.Resource}", flow.Sent * minutes);
                continue;
            }
            if (!_gateTerms.TryGetValue(flow.Estate, out var terms) || !terms.Import.TryGetValue((flow.Resource, flow.PriceClass), out var sources))
            {
                continue;
            }
            var total = sources.Values.Sum();
            if (total <= 1e-12)
            {
                continue;
            }
            foreach (var (sellerId, amount) in sources)
            {
                // What a stand-in sells is NPC gold: it leaves the game, it is never the sleeper's
                if (Company(sellerId) is not { } seller || seller.Asleep)
                {
                    continue;
                }
                var units = flow.Sent * minutes * amount / total;
                var gold = units * SettlementPrice(seller, owner, flow.Resource);
                seller.Gold += gold;
                earnings[seller.Id] = earnings.GetValueOrDefault(seller.Id) + gold;
                Add(seller.Report.Sold, flow.Resource, units);
                ContractDelivered(seller.Id, owner.Id, flow.Resource, units);
                AddStat(seller, "soldToPlayers", units);
                AddStat(owner, "boughtFromPlayers", units);
                seller.Touch();
                Changes.Companies.Add(seller.Id);
            }
        }
    }

    // ---------------------------------------------------------------- The Yard's matching

    /// <summary>A resident of a station that trades through its Yard gate this round</summary>
    private sealed class YardMember
    {
        public required EstateKey Key { get; init; }
        public required Company Company { get; init; }

        /// <summary>The network at its gate (null for a stand-in: an asleep estate makes nothing)</summary>
        public required Network? Network { get; init; }

        /// <summary>Per minute: what it has spare (it would go to the hub), what it lacks, what it makes, what its buildings use</summary>
        public Dictionary<string, double> Spare { get; } = new();
        public Dictionary<string, double> Lacks { get; } = new();
        public Dictionary<string, double> Makes { get; } = new();
        public Dictionary<string, double> Uses { get; } = new();

        /// <summary>What it sent through its gate last round, per resource</summary>
        public Dictionary<string, double> Sent { get; } = new();
    }

    /// <summary>The residents of a station whose Yard gate is connected, with what they have spare and lack</summary>
    private List<YardMember> YardMembers(StationRef station)
    {
        var members = new List<YardMember>();
        foreach (var seat in BigStation(station).SelectMany(StationSeats))
        {
            if (Estate(seat.District, seat.Slot) is not { OwnerId: { } ownerId } estate || Company(ownerId) is not { } company || company.Packed != null)
            {
                continue;
            }
            if (company.Asleep)
            {
                // Its stand-in keeps its trades going
                if (company.StandIn is { } standIn)
                {
                    members.Add(StandInMember(estate, company, standIn));
                }
                continue;
            }
            if (GateNetwork(estate) is not { } network)
            {
                continue;
            }
            var member = new YardMember { Key = KeyOf(estate), Company = company, Network = network };
            Measure(member);
            members.Add(member);
        }
        return members;
    }

    /// <summary>
    /// What a resident has spare and lacks, from its network's last solution, measured at its Yard gate (what its own
    /// pipes lose between a building and the gate is its own): spare is what goes to the hub from buildings that reach
    /// the gate, lacks what the hub brings and what is missing for buildings the gate can supply
    /// </summary>
    private void Measure(YardMember member)
    {
        var network = member.Network!;
        var solution = network.Solution;
        var gate = Estate(member.Key.District, member.Key.Slot)!.YardGate;
        var efficiency = new Dictionary<(long, string), double>();
        double ToGate(long building, string resource)
        {
            if (!efficiency.TryGetValue((building, resource), out var best))
            {
                best = network.GateLinks.Where(t => t.Building == building && t.Gate == gate && (t.Resources == null || t.Resources.Contains(resource)))
                    .Select(t => t.Efficiency).DefaultIfEmpty(0).Max();
                efficiency[(building, resource)] = best;
            }
            return best;
        }

        var received = new Dictionary<(long, string), double>();
        foreach (var flow in solution.Flows)
        {
            received[(flow.To, flow.Resource)] = received.GetValueOrDefault((flow.To, flow.Resource)) + flow.Delivered;
            if (flow.Kind == FlowKind.Sale && ToGate(flow.From, flow.Resource) is > 0 and var out_)
            {
                Add(member.Spare, flow.Resource, flow.Sent * out_);
            }
            else if (flow.Kind == FlowKind.Import && ToGate(flow.To, flow.Resource) is > 0 and var in_)
            {
                Add(member.Lacks, flow.Resource, flow.Delivered / in_);
            }
        }
        foreach (var flow in network.GateFlows.Where(t => t.Estate == member.Key))
        {
            if (flow.Import)
            {
                received[(flow.Building, flow.Resource)] = received.GetValueOrDefault((flow.Building, flow.Resource)) + flow.Delivered;
                Add(member.Lacks, flow.Resource, flow.Sent);
            }
            else
            {
                Add(member.Spare, flow.Resource, flow.Delivered);
                Add(member.Sent, flow.Resource, flow.Delivered);
            }
        }
        foreach (var node in network.Nodes.Where(t => t.Kind == FlowNodeKind.Building && t.Id > 0))
        {
            var rate = solution.Rate(node.Id);
            foreach (var (resource, amount) in node.Out)
            {
                Add(member.Makes, resource, amount * rate);
            }
            if (node.MaxRate <= 1e-9)
            {
                continue;
            }
            // What holds a building back: its scarcest inputs (the others it would use more of only once those come)
            var fractions = node.In.ToDictionary(t => t.Key, t => received.GetValueOrDefault((node.Id, t.Key)) / Math.Max(1e-12, t.Value * node.MaxRate));
            var least = fractions.Count == 0 ? 1 : fractions.Values.Min();
            if (least >= 0.999 && rate < node.MaxRate - 1e-9)
            {
                // It has all it needs but nobody takes all it could make: spare for the station
                foreach (var (resource, amount) in node.Out)
                {
                    if (ToGate(node.Id, resource) is > 0 and var reach)
                    {
                        Add(member.Spare, resource, amount * (node.MaxRate - rate) * reach);
                    }
                }
            }
            if (node.In.Count == 0)
            {
                continue;
            }
            foreach (var (resource, amount) in node.In)
            {
                Add(member.Uses, resource, amount * node.MaxRate);
                if (least < 0.999 && fractions[resource] <= least + 1e-6 && ToGate(node.Id, resource) is > 0 and var reach)
                {
                    Add(member.Lacks, resource, amount * node.MaxRate * (1 - Math.Min(1, fractions[resource])) / reach);
                }
            }
        }
    }

    /// <summary>Whether a seller's goods may go to a buyer: their trade settings, Blocks, and the buyer's gold</summary>
    private bool YardTradeAllowed(Company seller, Company buyer, string resource) =>
        seller.Id != buyer.Id && !seller.NoTrade.Contains(resource) && !buyer.NoBuy.Contains(resource)
        && (buyer.IsNpc || buyer.Asleep || buyer.Gold > 0) && TradesWith(seller, buyer) && TradesWith(buyer, seller)
        // A stand-in keeps a sleeper's trades with players, never with Houses or another stand-in
        && !(seller.Asleep && (buyer.IsNpc || buyer.Asleep)) && !(buyer.Asleep && seller.IsNpc);

    /// <summary>
    /// Shares what sellers have among buyers that lack it, in proportion (every buyer the same share of its need, every
    /// seller the same share of its spare), only between allowed pairs, and within what each seller may still sell and
    /// each buyer may still buy here (caps, used up as it goes)
    /// </summary>
    private static Dictionary<(int Seller, int Buyer), double> Share(double[] spare, double[] lacks, Func<int, int, bool> allowed, double[]? sellerCap = null, double[]? buyerCap = null)
    {
        var flows = new Dictionary<(int, int), double>();
        double Room(int s, int b) => Math.Min(sellerCap?[s] ?? double.MaxValue, buyerCap?[b] ?? double.MaxValue);
        for (var round = 0; round < 20; round++)
        {
            var requests = new Dictionary<(int, int), double>();
            for (var b = 0; b < lacks.Length; b++)
            {
                if (lacks[b] <= 1e-9)
                {
                    continue;
                }
                var options = Enumerable.Range(0, spare.Length).Where(s => spare[s] > 1e-9 && allowed(s, b) && Room(s, b) > 1e-9).ToList();
                var total = options.Sum(s => spare[s]);
                foreach (var s in options)
                {
                    requests[(s, b)] = lacks[b] * spare[s] / total;
                }
            }
            if (requests.Count == 0)
            {
                break;
            }
            var progress = 0.0;
            foreach (var group in requests.GroupBy(t => t.Key.Item1))
            {
                var s = group.Key;
                var asked = group.Sum(t => t.Value);
                var factor = Math.Min(1, Math.Min(spare[s], sellerCap?[s] ?? double.MaxValue) / asked);
                foreach (var ((_, b), amount) in group)
                {
                    var granted = Math.Min(Math.Min(amount * factor, lacks[b]), Room(s, b));
                    if (granted <= 1e-12)
                    {
                        continue;
                    }
                    flows[(s, b)] = flows.GetValueOrDefault((s, b)) + granted;
                    spare[s] -= granted;
                    lacks[b] -= granted;
                    if (sellerCap != null)
                    {
                        sellerCap[s] -= granted;
                    }
                    if (buyerCap != null)
                    {
                        buyerCap[b] -= granted;
                    }
                    progress += granted;
                }
            }
            if (progress <= 1e-9)
            {
                break;
            }
        }
        return flows;
    }

    /// <summary>
    /// The matching of one station for this round: players first (between them), then what is left between players and
    /// Houses, within the one NPC cap (Houses buy at most a quarter of what a player makes of a good, and sell at most a
    /// quarter of what it uses)
    /// </summary>
    private Dictionary<(EstateKey Seller, EstateKey Buyer, string Resource), double> MatchYard(List<YardMember> members)
    {
        var result = new Dictionary<(EstateKey, EstateKey, string), double>();
        var resources = members.SelectMany(t => t.Spare.Keys).Intersect(members.SelectMany(t => t.Lacks.Keys)).Order(StringComparer.Ordinal).ToList();
        var cap = Content.Progression.Together.NpcCap;
        var index = members.Select((t, i) => (t.Company.Id, i)).GroupBy(t => t.Id).ToDictionary(t => t.Key, t => t.First().i);
        foreach (var resource in resources)
        {
            var spare = members.Select(t => t.Spare.GetValueOrDefault(resource)).ToArray();
            var lacks = members.Select(t => t.Lacks.GetValueOrDefault(resource)).ToArray();
            bool Allowed(int s, int b) => YardTradeAllowed(members[s].Company, members[b].Company, resource);

            // Contracts first, up to their rate
            foreach (var contract in _contracts.Values.Where(t => t.State == ContractState.Active && t.Resource == resource).OrderBy(t => t.Id))
            {
                if (index.TryGetValue(contract.Seller, out var s) && index.TryGetValue(contract.Buyer, out var b))
                {
                    var amount = Math.Min(contract.Rate, Math.Min(spare[s], lacks[b]));
                    if (amount > 1e-9)
                    {
                        spare[s] -= amount;
                        lacks[b] -= amount;
                        var key = (members[s].Key, members[b].Key, resource);
                        result[key] = result.GetValueOrDefault(key) + amount;
                    }
                }
            }

            void Record(Dictionary<(int Seller, int Buyer), double> flows)
            {
                foreach (var ((s, b), amount) in flows.Where(t => t.Value > 1e-9))
                {
                    var key = (members[s].Key, members[b].Key, resource);
                    result[key] = result.GetValueOrDefault(key) + amount;
                }
            }

            // Players with players
            Record(Share(spare, lacks, (s, b) => !members[s].Company.IsNpc && !members[b].Company.IsNpc && Allowed(s, b)));
            // Houses buy what players have left, and sell what players still lack, within one cap per player
            var mayBuy = members.Select(t => t.Company.IsNpc ? 0 : cap * t.Makes.GetValueOrDefault(resource)).ToArray();
            Record(Share(spare, lacks, (s, b) => !members[s].Company.IsNpc && members[b].Company.IsNpc && Allowed(s, b), sellerCap: mayBuy));
            var maySell = members.Select(t => t.Company.IsNpc ? 0 : cap * t.Uses.GetValueOrDefault(resource)).ToArray();
            var sold = Share(spare, lacks, (s, b) => members[s].Company.IsNpc && !members[b].Company.IsNpc && Allowed(s, b), buyerCap: maySell);
            Record(sold);
        }
        return result;
    }

    /// <summary>
    /// One round of the Yards (at most once a minute): what was sent last round arrives at its buyers, every station is
    /// matched again, and the estates whose terms moved by more than the tolerance are solved again
    /// </summary>
    private void CoupleYards()
    {
        SolveNetworks();
        var newTerms = new Dictionary<EstateKey, GateTerms>();
        GateTerms TermsOf(EstateKey key) => newTerms.TryGetValue(key, out var t) ? t : newTerms[key] = new GateTerms();

        // A big station matches once, from its corner station
        var stations = _estates.Values
            .Where(t => t.OwnerId != null)
            .Select(t => BigStation(t.Station)[0])
            .Distinct()
            .OrderBy(t => t.District.X).ThenBy(t => t.District.Y).ThenBy(t => t.Index);
        var byStation = stations.Select(t => (Station: t, Members: YardMembers(t))).ToList();
        var all = byStation.SelectMany(t => t.Members).ToDictionary(t => t.Key);
        foreach (var (station, members) in byStation)
        {
            var byKey = members.ToDictionary(t => t.Key);
            // What was sent to the station's project arrives now, in the shares it was matched
            if (ProjectsEnabled && members.Any(t => !t.Company.IsNpc))
            {
                var project = ProjectOf(station);
                project.Incoming = new Dictionary<long, Dictionary<string, double>>();
                foreach (var seller in members)
                {
                    if (!project.Matched.TryGetValue(seller.Company.Id, out var goods) || !_gateTerms.TryGetValue(seller.Key, out var sent))
                    {
                        continue;
                    }
                    foreach (var (good, amount) in goods)
                    {
                        var exported = sent.Export.GetValueOrDefault(good);
                        var ratio = exported <= 1e-12 ? 0 : Math.Min(1, seller.Sent.GetValueOrDefault(good) / exported);
                        if (amount * ratio > 1e-9)
                        {
                            if (!project.Incoming.TryGetValue(seller.Company.Id, out var incoming))
                            {
                                project.Incoming[seller.Company.Id] = incoming = new Dictionary<string, double>();
                            }
                            incoming[good] = amount * ratio;
                        }
                    }
                }
            }
            // What sellers sent last round arrives now, in the shares they were matched (less when they sent less)
            foreach (var seller in members)
            {
                if (!_gateTerms.TryGetValue(seller.Key, out var old))
                {
                    continue;
                }
                foreach (var ((buyerKey, resource), matched) in old.Matched)
                {
                    var exported = old.Export.GetValueOrDefault(resource);
                    var ratio = exported <= 1e-12 ? 0 : Math.Min(1, seller.Sent.GetValueOrDefault(resource) / exported);
                    if (matched * ratio <= 1e-9 || !byKey.ContainsKey(buyerKey))
                    {
                        continue;
                    }
                    var priceClass = seller.Company.IsNpc ? 1 : 0;
                    var import = TermsOf(buyerKey).Import;
                    if (!import.TryGetValue((resource, priceClass), out var sources))
                    {
                        import[(resource, priceClass)] = sources = new Dictionary<long, double>();
                    }
                    sources[seller.Company.Id] = sources.GetValueOrDefault(seller.Company.Id) + matched * ratio;
                }
            }
        }

        // Asks: what was brought last round arrives, and the residents' spare goes to their neighbours' Asks first
        foreach (var (_, members) in byStation)
        {
            DeliverAsks(members);
            MatchAsks(members, TermsOf);
        }

        // Links between stations: what they brought arrives, then they take their share before the Yards
        TimeSpan? limit = null;
        DeliverLinks(all, TermsOf, ref limit);
        MatchLinks(all, TermsOf);

        foreach (var (station, members) in byStation)
        {
            var project = ProjectsEnabled && members.Any(t => !t.Company.IsNpc) ? ProjectOf(station) : null;
            var toProject = new Dictionary<(EstateKey Seller, string Resource), double>();
            void Send(Dictionary<(EstateKey Seller, string Resource), double> sent)
            {
                foreach (var (key, amount) in sent)
                {
                    toProject[key] = toProject.GetValueOrDefault(key) + amount;
                }
            }
            // A seller's "project first" goods go to the project before the Yard
            if (project != null)
            {
                Send(MatchProject(project, members, first: true));
            }
            if (members.Count >= 2)
            {
                var byKey = members.ToDictionary(t => t.Key);
                foreach (var ((sellerKey, buyerKey, resource), amount) in MatchYard(members))
                {
                    var terms = TermsOf(sellerKey);
                    terms.Export[resource] = terms.Export.GetValueOrDefault(resource) + amount;
                    terms.Matched[(buyerKey, resource)] = terms.Matched.GetValueOrDefault((buyerKey, resource)) + amount;
                    Add(byKey[sellerKey].Spare, resource, -amount);
                }
            }
            // Then the project takes what the residents left
            if (project != null)
            {
                Send(MatchProject(project, members, first: false));
                project.Matched = new Dictionary<long, Dictionary<string, double>>();
                var byKey = members.ToDictionary(t => t.Key);
                foreach (var ((sellerKey, resource), amount) in toProject)
                {
                    var terms = TermsOf(sellerKey);
                    terms.Export[resource] = terms.Export.GetValueOrDefault(resource) + amount;
                    var seller = byKey[sellerKey].Company.Id;
                    if (!project.Matched.TryGetValue(seller, out var goods))
                    {
                        project.Matched[seller] = goods = new Dictionary<string, double>();
                    }
                    goods[resource] = goods.GetValueOrDefault(resource) + amount;
                }
            }
        }

        var changed = false;
        foreach (var key in _gateTerms.Keys.Union(newTerms.Keys).ToList())
        {
            var before = _gateTerms.GetValueOrDefault(key);
            var after = newTerms.GetValueOrDefault(key);
            if (after == null || after.IsEmpty)
            {
                if (before != null)
                {
                    _gateTerms.Remove(key);
                    changed |= TermsChanged(before, new GateTerms());
                    MarkGateNetwork(key);
                }
                continue;
            }
            if (before == null || TermsChanged(before, after))
            {
                _gateTerms[key] = after;
                MarkGateNetwork(key);
                changed = true;
            }
            else
            {
                // Within the tolerance: the network keeps solving with what it has; what was matched still counts
                before.Matched = after.Matched;
            }
        }
        _couplingInterval = changed ? CouplingInterval : TimeSpan.FromTicks(Math.Min(MaxCouplingInterval.Ticks, _couplingInterval.Ticks * 2));
        if (limit is { } until && _couplingInterval > until)
        {
            // A tank runs dry or fills up before then
            _couplingInterval = until;
        }
    }

    private static bool TermsChanged(GateTerms before, GateTerms after)
    {
        static bool Differs(double a, double b) => Math.Abs(a - b) > Math.Max(1e-6, TermsTolerance * Math.Max(Math.Abs(a), Math.Abs(b)));

        foreach (var resource in before.Export.Keys.Union(after.Export.Keys))
        {
            if (Differs(before.Export.GetValueOrDefault(resource), after.Export.GetValueOrDefault(resource)))
            {
                return true;
            }
        }
        foreach (var key in before.Import.Keys.Union(after.Import.Keys))
        {
            var a = before.Import.TryGetValue(key, out var x) ? x.Values.Sum() : 0;
            var b = after.Import.TryGetValue(key, out var y) ? y.Values.Sum() : 0;
            if (Differs(a, b))
            {
                return true;
            }
        }
        return false;
    }

    private void MarkGateNetwork(EstateKey key)
    {
        if (Estate(key.District, key.Slot) is { } estate && NetworkAt(estate.YardGate) is { } network)
        {
            network.NeedsSolve = true;
            Changes.Networks.Add(network.Id);
        }
    }

    /// <summary>A company's trade settings changed: its station's Yard matches again at the next round</summary>
    private void MarkTradeDirty(long companyId)
    {
        MarkCompanyNetworksDirty(companyId);
        WakeCoupling();
    }

    /// <summary>Something that matters for the Yards changed: the next round comes within a minute</summary>
    private void WakeCoupling()
    {
        _couplingInterval = CouplingInterval;
        if (_nextCoupling > LastTick + CouplingInterval)
        {
            _nextCoupling = LastTick + CouplingInterval;
        }
    }

    /// <summary>The moments the Yards are coupled between two times (once a minute while terms move, less often when they don't)</summary>
    private DateTime NextCoupling()
    {
        _nextCoupling ??= LastTick + CouplingInterval;
        return _nextCoupling.Value;
    }

    /// <summary>For tests and the dev cheat: the Yards' rounds right now (one round matches, the next delivers)</summary>
    public void CoupleNow(int rounds = 2)
    {
        for (var i = 0; i < rounds; i++)
        {
            CoupleYards();
        }
        _nextCoupling = LastTick + _couplingInterval;
    }

    private void RunCoupling()
    {
        CoupleYards();
        _nextCoupling = LastTick + _couplingInterval;
    }

    // ---------------------------------------------------------------- Queries for the Yard panel

    /// <summary>A resident of a station as the Yard panel shows it: whether its pipes reach its gate, what it has spare and lacks</summary>
    public sealed record YardResident(Company Company, int Slot, bool Connected, IReadOnlyDictionary<string, double> Spare, IReadOnlyDictionary<string, double> Lacks);

    /// <summary>The residents of a station (players and Houses), the connected ones with what they have spare and lack</summary>
    public List<YardResident> YardResidents(StationRef station)
    {
        SolveNetworks();
        var members = YardMembers(station).ToDictionary(t => t.Key.Slot);
        var residents = new List<YardResident>();
        foreach (var slot in BigStation(station).SelectMany(StationSeats).Select(t => t.Slot))
        {
            if (Estate(station.District, slot)?.OwnerId is not { } owner || Company(owner) is not { } company)
            {
                continue;
            }
            if (!members.TryGetValue(slot, out var member))
            {
                residents.Add(new YardResident(company, slot, false, new Dictionary<string, double>(), new Dictionary<string, double>()));
                continue;
            }
            // What is still spare (it goes to the hub) and still missing, after what the Yard passes already
            var gets = member.Network == null
                ? _gateTerms.TryGetValue(member.Key, out var standInTerms) ? standInTerms.Import.GroupBy(t => t.Key.Resource).ToDictionary(t => t.Key, t => t.Sum(f => f.Value.Values.Sum())) : new Dictionary<string, double>()
                : member.Network.GateFlows.Where(t => t.Import && t.Estate == member.Key).GroupBy(t => t.Resource).ToDictionary(t => t.Key, t => t.Sum(f => f.Sent));
            residents.Add(new YardResident(company, slot, true,
                member.Spare.ToDictionary(t => t.Key, t => Math.Max(0, t.Value - member.Sent.GetValueOrDefault(t.Key))),
                member.Lacks.ToDictionary(t => t.Key, t => Math.Max(0, t.Value - gets.GetValueOrDefault(t.Key)))));
        }
        return residents;
    }

    /// <summary>What the price per unit of a Yard trade is, as seen from this company (selling or buying)</summary>
    public double YardPrice(Company me, Company partner, string resource, bool selling) =>
        selling ? SettlementPrice(me, partner, resource) : SettlementPrice(partner, me, resource);

    /// <summary>What goes through a station's Yard per minute, by resource (between its residents, not over links)</summary>
    public Dictionary<string, double> YardTotals(StationRef station)
    {
        var totals = new Dictionary<string, double>();
        var home = BigStation(station)[0];
        foreach (var slot in BigStation(station).SelectMany(StationSeats).Select(t => t.Slot))
        {
            if (_gateTerms.TryGetValue(new EstateKey(station.District, slot), out var terms))
            {
                foreach (var ((resource, _), sources) in terms.Import)
                {
                    Add(totals, resource, sources.Where(t => LivesIn(t.Key, home)).Sum(t => t.Value) - LinkImports(new EstateKey(station.District, slot), resource, home));
                }
            }
        }
        return totals.Where(t => t.Value > 1e-9).ToDictionary();
    }

    /// <summary>Whether a company lives in a (big) station</summary>
    private bool LivesIn(long companyId, StationRef home) =>
        Company(companyId) is { } company && EstateOf(company) is { } estate && BigStation(estate.Station)[0] == home;

    /// <summary>What links from residents of the same station bring an estate (counted as links, not as the Yard)</summary>
    private double LinkImports(EstateKey buyer, string resource, StationRef home) =>
        _links.Values.Where(t => t.Rate > 0 && t.Resource == resource && LivesIn(t.Seller, home)
                && Estate(buyer.District, buyer.Slot)?.OwnerId == t.Buyer)
            .Sum(t => t.Rate);

    /// <summary>What a company's estate sends to and gets from its station's residents per minute, by partner and resource (links aside)</summary>
    public IReadOnlyList<(long Partner, string Resource, double PerMinute, bool Selling)> YardTrades(Estate estate)
    {
        var trades = new List<(long, string, double, bool)>();
        var key = KeyOf(estate);
        var home = BigStation(estate.Station)[0];
        double OverLink(long seller, long buyer, string resource) => LinkBetween(seller, buyer, resource)?.Rate ?? 0;
        if (_gateTerms.TryGetValue(key, out var terms) && estate.OwnerId is { } me)
        {
            foreach (var ((resource, _), sources) in terms.Import)
            {
                foreach (var (seller, amount) in sources.Where(t => LivesIn(t.Key, home)))
                {
                    if (amount - OverLink(seller, me, resource) > 1e-9)
                    {
                        trades.Add((seller, resource, amount - OverLink(seller, me, resource), false));
                    }
                }
            }
        }
        foreach (var (otherKey, other) in _gateTerms.Where(t => t.Key != key))
        {
            if (Estate(otherKey.District, otherKey.Slot)?.OwnerId is not { } buyer || !LivesIn(buyer, home))
            {
                continue;
            }
            foreach (var ((resource, _), sources) in other.Import)
            {
                if (estate.OwnerId is { } owner && sources.TryGetValue(owner, out var amount) && amount - OverLink(owner, buyer, resource) > 1e-9)
                {
                    trades.Add((buyer, resource, amount - OverLink(owner, buyer, resource), true));
                }
            }
        }
        return trades;
    }
}

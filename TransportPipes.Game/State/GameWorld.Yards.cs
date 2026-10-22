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

        /// <summary>The last matching: the share of what this estate's Yard sales took that would have gone to its hub</summary>
        public Dictionary<string, double> HubShare { get; set; } = new();

        /// <summary>
        /// Of what the Yard brings (Import), per minute from each seller: the part that would have gone to the seller's hub.
        /// What the buyer doesn't take of it is paid to the seller at its own hub price (the floor, F1).
        /// </summary>
        public Dictionary<(string Resource, int PriceClass), Dictionary<long, double>> HubBound { get; set; } = new();

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

    /// <summary>
    /// The gold per unit a seller gets for what a buyer took at the gate: a House's price, or the one player price; two
    /// players on one device trade at the seller's hub price (no premium moves between an account and its alt)
    /// </summary>
    private double SettlementPrice(Company seller, Company buyer, string resource) =>
        seller.IsNpc ? HouseSellPrice(resource)
        : buyer.IsNpc ? HouseBuyPrice(seller, resource)
        : SameDevice(seller, buyer) ? HubBuyPrice(seller, resource)
        : PlayerPrice(resource);

    /// <summary>
    /// The sellers of what a network's gates delivered get their pay (what the buyer paid, in proportion to what each of
    /// them sent); returns the gold per company for the earnings
    /// </summary>
    private void SettleGateFlows(Network network, double minutes, Dictionary<long, double> earnings)
    {
        CountUntaken(network, minutes);
        foreach (var flow in network.GateFlows.Where(t => !t.Import))
        {
            if (_buildings.TryGetValue(flow.Building, out var building) && Company(building.Owner) is { } owner)
            {
                CountDiscovery(building, flow.Sent * minutes);
                owner.EraOutput += flow.Sent * minutes * BasePrice(flow.Resource);
                Add(owner.Report.Produced, flow.Resource, flow.Sent * minutes);
                AddStat(owner, $"produced:{flow.Resource}", flow.Sent * minutes);
            }
        }
        foreach (var payment in GatePayments(network))
        {
            var (seller, buyer) = (payment.Seller, payment.Buyer);
            var units = payment.Units * minutes;
            var gold = payment.Gold * minutes;
            if (payment.Floor)
            {
                BookHubSale(seller, payment.Resource, units, gold, earnings, "yardFloor");
                continue;
            }
            seller.Gold += gold;
            earnings[seller.Id] = earnings.GetValueOrDefault(seller.Id) + gold;
            // Measures (the playtest's): gold the Yard paid a player, and gold that came from a House
            AddStat(seller, buyer.IsNpc ? "npcGold" : seller.IsNpc ? "houseSold" : "yardPaid", gold);
            Add(seller.Report.Sold, payment.Resource, units);
            ContractDelivered(seller.Id, buyer.Id, payment.Resource, units);
            AddStat(seller, "soldToPlayers", units);
            AddStat(buyer, "boughtFromPlayers", units);
            CountUsualTrade(seller, buyer, payment.Resource, units, minutes);
            seller.Touch();
            Changes.Companies.Add(seller.Id);
        }
    }

    /// <summary>
    /// A seller's pay per minute for what the Yard brought to a buyer's gate: for what the buyer's buildings took, at the
    /// settlement price (in proportion to what each seller sent), or (Floor) for what they didn't take, at the seller's
    /// own hub price as far as it would have gone to the seller's hub (F1.1)
    /// </summary>
    private sealed record GatePayment(Company Seller, Company Buyer, string Resource, double Units, double Gold, bool Floor);

    /// <summary>
    /// The payments for what the Yard brings to this network's gates, per minute. What a stand-in sells is NPC gold: it
    /// leaves the game, it is never the sleeper's. What isn't taken and wouldn't have gone to the seller's hub (what a
    /// Storage kept, or a building made only for the Yard) is lost on the way, and the next round sends less.
    /// </summary>
    private IEnumerable<GatePayment> GatePayments(Network network)
    {
        foreach (var gate in network.Gates)
        {
            if (EstateOfGate(gate) is not { OwnerId: { } buyerId } estate || Company(buyerId) is not { } buyer || !_gateTerms.TryGetValue(KeyOf(estate), out var terms))
            {
                continue;
            }
            var key = KeyOf(estate);
            foreach (var ((resource, priceClass), sources) in terms.Import)
            {
                var total = sources.Values.Sum();
                if (total <= 1e-12)
                {
                    continue;
                }
                var taken = network.GateFlows.Where(t => t.Import && t.Estate == key && t.Resource == resource && t.PriceClass == priceClass).Sum(t => t.Sent);
                var untaken = Math.Max(0, total - taken);
                var hubBound = terms.HubBound.GetValueOrDefault((resource, priceClass));
                foreach (var (sellerId, amount) in sources)
                {
                    if (Company(sellerId) is not { } seller || seller.Asleep)
                    {
                        continue;
                    }
                    var units = taken * amount / total;
                    if (units > 1e-12)
                    {
                        yield return new GatePayment(seller, buyer, resource, units, units * SettlementPrice(seller, buyer, resource), false);
                    }
                    var floor = seller.IsNpc ? 0 : untaken * Math.Min(amount, hubBound?.GetValueOrDefault(sellerId) ?? 0) / total;
                    if (floor > 1e-12)
                    {
                        yield return new GatePayment(seller, buyer, resource, floor, floor * HubBuyPrice(seller, resource), true);
                    }
                }
            }
        }
    }

    /// <summary>The measures: per seller, the units the Yard sent its buyers and the units they didn't take</summary>
    private void CountUntaken(Network network, double minutes)
    {
        foreach (var gate in network.Gates)
        {
            if (EstateOfGate(gate) is not { } estate || !_gateTerms.TryGetValue(KeyOf(estate), out var terms))
            {
                continue;
            }
            var key = KeyOf(estate);
            foreach (var ((resource, priceClass), sources) in terms.Import)
            {
                var total = sources.Values.Sum();
                if (total <= 1e-12)
                {
                    continue;
                }
                var taken = network.GateFlows.Where(t => t.Import && t.Estate == key && t.Resource == resource && t.PriceClass == priceClass).Sum(t => t.Sent);
                var untaken = Math.Max(0, total - taken);
                foreach (var (sellerId, amount) in sources)
                {
                    if (Company(sellerId) is { IsNpc: false } seller)
                    {
                        AddStat(seller, "yardSent", amount * minutes);
                        AddStat(seller, "yardUntaken", untaken * minutes * amount / total);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Gold a minute that companies get, or pay, besides their networks' own flows (F1.6: the screens show what is
    /// paid): Yard sales (and the floor), deliveries to the project, Asks, and what stand-ins buy
    /// </summary>
    private void AddTradeIncome(Dictionary<long, double> total)
    {
        void Add(long company, double gold) => total[company] = total.GetValueOrDefault(company) + gold;
        foreach (var network in _networks.Values)
        {
            foreach (var payment in GatePayments(network))
            {
                Add(payment.Seller.Id, payment.Gold);
            }
        }
        // A trade the Yard just matched: the seller's goods are on their way (they leave its network now and arrive at
        // the buyer the round after), so the rate counts them now; it would otherwise dip for a round (Ana's replay:
        // 233 → 128 → 298 g/min)
        foreach (var (sellerKey, terms) in _gateTerms)
        {
            if (Estate(sellerKey.District, sellerKey.Slot)?.OwnerId is not { } sellerId || Company(sellerId) is not { Asleep: false } seller)
            {
                continue;
            }
            foreach (var ((buyerKey, resource), matched) in terms.Matched)
            {
                if (Estate(buyerKey.District, buyerKey.Slot)?.OwnerId is not { } buyerId || Company(buyerId) is not { } buyer)
                {
                    continue;
                }
                var arriving = _gateTerms.GetValueOrDefault(buyerKey)?.Import.GetValueOrDefault((resource, seller.IsNpc ? 1 : 0))?.GetValueOrDefault(sellerId) ?? 0;
                if (matched - arriving > 1e-9)
                {
                    Add(sellerId, (matched - arriving) * SettlementPrice(seller, buyer, resource));
                }
            }
        }
        foreach (var project in _projects.Values.Where(t => t.Incoming.Count > 0))
        {
            foreach (var (sellerId, goods) in project.Incoming)
            {
                foreach (var (good, perMinute) in goods.Where(t => ProjectLacks(project, t.Key) > 1e-9))
                {
                    Add(sellerId, perMinute * BasePrice(good));
                }
            }
        }
        foreach (var asker in _companies.Values.Where(t => t.Asks.Count > 0))
        {
            var room = StorageRoom(asker.Id) > 1e-9;
            foreach (var ask in asker.Asks.Where(t => room && t.Left > 1e-9))
            {
                foreach (var (sellerId, perMinute) in ask.Incoming)
                {
                    if (Company(sellerId) is { } seller)
                    {
                        Add(sellerId, perMinute * HubBuyPrice(seller, ask.Resource));
                        Add(asker.Id, -perMinute * HubBuyPrice(seller, ask.Resource));
                    }
                }
            }
        }
        foreach (var sleeper in _companies.Values.Where(t => t.Asleep && t.StandIn != null && t.Packed == null))
        {
            if (EstateOf(sleeper) is not { } estate || !_gateTerms.TryGetValue(KeyOf(estate), out var terms))
            {
                continue;
            }
            foreach (var ((resource, _), sources) in terms.Import)
            {
                foreach (var (sellerId, perMinute) in sources.Where(t => Company(t.Key) is { Asleep: false }))
                {
                    Add(sellerId, perMinute * StandInBuyPrice(sleeper, Company(sellerId)!, resource));
                }
            }
        }
    }

    /// <summary>Gold a company gets for goods as if it had sold them at its hub: its report, its Hub Order and its reputation count them</summary>
    private void BookHubSale(Company seller, string resource, double units, double gold, Dictionary<long, double> earnings, string stat)
    {
        seller.Gold += gold;
        earnings[seller.Id] = earnings.GetValueOrDefault(seller.Id) + gold;
        AddStat(seller, stat, gold);
        Add(seller.Report.Sold, resource, units);
        HubMarketOf(seller, seller.District).AddSold(resource, units);
        var level = ReputationLevel(seller);
        seller.HubReputation += gold / 2000;
        if (ReputationLevel(seller) != level)
        {
            MarkCompanyNetworksDirty(seller.Id);
        }
        seller.Touch();
        Changes.Companies.Add(seller.Id);
    }

    /// <summary>Whether the Yard brings something to one of this network's gates</summary>
    private bool HasGateImports(Network network) =>
        network.Gates.Any(gate => EstateOfGate(gate) is { } estate && _gateTerms.TryGetValue(KeyOf(estate), out var terms)
            && terms.Import.Values.Any(t => t.Values.Sum() > 1e-12));

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

        /// <summary>Of its spare as measured (before anything is matched): the part that goes (or went) to its hub</summary>
        public Dictionary<string, double> HubSpare { get; } = new();
        public Dictionary<string, double> SpareMeasured { get; } = new();

        /// <summary>The share of its spare of a good that would go to its hub</summary>
        public double HubShare(string resource) =>
            SpareMeasured.GetValueOrDefault(resource) is > 1e-12 and var spare ? Math.Min(1, HubSpare.GetValueOrDefault(resource) / spare) : 0;
    }

    /// <summary>
    /// The residents of a station whose Yard gate is connected, with what they have spare and lack. Two passes: first
    /// everyone's spare, then the lacks (a lack counts only as far as the building's other inputs could come, from its
    /// own pipes or from the station's spare).
    /// </summary>
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
            MeasureSpare(member);
            foreach (var (resource, amount) in member.Spare)
            {
                member.SpareMeasured[resource] = amount;
            }
            members.Add(member);
        }
        var stationSpare = new Dictionary<string, double>();
        foreach (var member in members)
        {
            foreach (var (resource, amount) in member.Spare)
            {
                Add(stationSpare, resource, amount);
            }
        }
        foreach (var member in members.Where(t => t.Network != null))
        {
            MeasureLacks(member, stationSpare);
        }
        return members;
    }

    /// <summary>The efficiency of the best path from a building to its estate's Yard gate for a good (0: none)</summary>
    private sealed class GateReach(Network network, TilePos gate)
    {
        private readonly Dictionary<(long, string), (double Efficiency, double Capacity)> _best = new();

        public (double Efficiency, double Capacity) Of(long building, string resource)
        {
            if (!_best.TryGetValue((building, resource), out var best))
            {
                best = network.GateLinks.Where(t => t.Building == building && t.Gate == gate && (t.Resources == null || t.Resources.Contains(resource)))
                    .Select(t => (t.Efficiency, t.Capacity)).OrderByDescending(t => t.Efficiency).FirstOrDefault();
                _best[(building, resource)] = best;
            }
            return best;
        }

        public double Efficiency(long building, string resource) => Of(building, resource).Efficiency;
    }

    /// <summary>
    /// What a resident has spare, from its network's last solution, measured at its Yard gate (what its own pipes lose
    /// between a building and the gate is its own): what goes to the hub from buildings that reach the gate, what goes
    /// into a Storage beyond what the Storage keeps, what it sends through the gate now, and what buildings that have
    /// all they need could make more of if somebody took it (F1.4)
    /// </summary>
    private void MeasureSpare(YardMember member)
    {
        var network = member.Network!;
        var solution = network.Solution;
        var reach = new GateReach(network, Estate(member.Key.District, member.Key.Slot)!.YardGate);
        var nodes = network.Nodes.ToDictionary(t => t.Id);

        var received = new Dictionary<(long, string), double>();
        foreach (var flow in solution.Flows)
        {
            received[(flow.To, flow.Resource)] = received.GetValueOrDefault((flow.To, flow.Resource)) + flow.Delivered;
            var toGate = reach.Efficiency(flow.From, flow.Resource);
            if (toGate <= 0)
            {
                continue;
            }
            if (flow.Kind == FlowKind.Sale)
            {
                Add(member.Spare, flow.Resource, flow.Sent * toGate);
                Add(member.HubSpare, flow.Resource, flow.Sent * toGate);
            }
            else if (flow.Kind == FlowKind.ToStorage && nodes.TryGetValue(flow.To, out var storage) && storage.StorageWants is { } wants && !wants.Contains(flow.Resource))
            {
                // It went into the Storage only because the trading post was full: the Storage doesn't keep it
                Add(member.Spare, flow.Resource, flow.Sent * toGate);
            }
        }
        foreach (var flow in network.GateFlows.Where(t => t.Estate == member.Key))
        {
            if (flow.Import)
            {
                received[(flow.Building, flow.Resource)] = received.GetValueOrDefault((flow.Building, flow.Resource)) + flow.Delivered;
            }
            else
            {
                Add(member.Spare, flow.Resource, flow.Delivered);
                Add(member.Sent, flow.Resource, flow.Delivered);
                if (ReachesOwnPost(network, flow.Building, flow.Resource))
                {
                    // Without the Yard it would go to the hub
                    Add(member.HubSpare, flow.Resource, flow.Delivered);
                }
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
            if (rate >= node.MaxRate - 1e-9 || !_buildings.TryGetValue(node.Id, out var building) || IdleReasonOf(building).IsStuck)
            {
                continue;
            }
            // How fast its inputs would let it run: what they bring now, or what its own suppliers on its pipes could
            // bring (a mine whose ore can only go to the gate runs only once somebody takes the ore, so it gets no energy
            // until then)
            var least = node.In.Count == 0 ? 1 : node.In.Min(t => received.GetValueOrDefault((node.Id, t.Key)) / Math.Max(1e-12, t.Value * node.MaxRate));
            var could = least >= 0.999 ? 1 : Math.Max(least, LevelReach(network, node).InputShare());
            if (could * node.MaxRate - rate > 1e-9)
            {
                // Nobody takes all it could make: spare for the station (and what it makes is what it could make, else
                // the Houses' cap would shrink with every round nobody takes it all)
                foreach (var (resource, amount) in node.Out)
                {
                    Add(member.Makes, resource, amount * (could * node.MaxRate - rate));
                    if (reach.Efficiency(node.Id, resource) is > 0 and var toGate)
                    {
                        Add(member.Spare, resource, amount * (could * node.MaxRate - rate) * toGate);
                    }
                }
            }
        }
    }

    /// <summary>
    /// What a resident lacks, measured at its Yard gate (F1.2): what the hub brings its buildings that reach the gate,
    /// what it takes from the gate now, and what is missing for those buildings, but only what they could use:
    /// lack ≤ need × min(f_out, f_other) − got, with f_out 0 for a building whose output can't leave (its full run
    /// fraction when its own post or Storage is full) and f_other what its other inputs could reach (from its own pipes,
    /// or from the station's spare through its gate). A buyer that took under 80% of what it was matched last round is
    /// matched at most a quarter more than it took (F1.3).
    /// </summary>
    private void MeasureLacks(YardMember member, IReadOnlyDictionary<string, double> stationSpare)
    {
        var network = member.Network!;
        var solution = network.Solution;
        var reach = new GateReach(network, Estate(member.Key.District, member.Key.Slot)!.YardGate);
        var nodes = network.Nodes.ToDictionary(t => t.Id);

        var received = new Dictionary<(long, string), double>();
        var fromGate = new Dictionary<(long, string), double>();
        var takenAtGate = new Dictionary<string, double>();
        foreach (var flow in solution.Flows)
        {
            received[(flow.To, flow.Resource)] = received.GetValueOrDefault((flow.To, flow.Resource)) + flow.Delivered;
            if (flow.Kind == FlowKind.Import && reach.Efficiency(flow.To, flow.Resource) is > 0 and var in_)
            {
                // What the hub brings, the Yard could bring instead
                Add(member.Lacks, flow.Resource, flow.Delivered / in_);
            }
        }
        foreach (var flow in network.GateFlows.Where(t => t.Estate == member.Key && t.Import))
        {
            received[(flow.Building, flow.Resource)] = received.GetValueOrDefault((flow.Building, flow.Resource)) + flow.Delivered;
            fromGate[(flow.Building, flow.Resource)] = fromGate.GetValueOrDefault((flow.Building, flow.Resource)) + flow.Sent;
            Add(member.Lacks, flow.Resource, flow.Sent);
            Add(takenAtGate, flow.Resource, flow.Sent);
        }

        // What a building could get of a good: what it gets now, what its own suppliers on its pipes still make for
        // others than buildings (the hub, a Storage) or could make more of, an own Storage or post that has it without
        // end, and the station's spare through its gate (as far as the gate pipe carries it)
        double Potential(FlowNode node, string resource)
        {
            var potential = received.GetValueOrDefault((node.Id, resource));
            foreach (var link in network.Links.Where(t => (t.A == node.Id || t.B == node.Id) && t.Allows(resource)))
            {
                if (!nodes.TryGetValue(link.A == node.Id ? link.B : link.A, out var other) || other.Owner != node.Owner)
                {
                    continue;
                }
                if ((other.Kind == FlowNodeKind.Storage && other.StorageContents.Contains(resource)) || (other.Kind == FlowNodeKind.TradingPost && other.Imports.Contains(resource)))
                {
                    return double.MaxValue;
                }
                if (other.Kind == FlowNodeKind.Building && other.Out.TryGetValue(resource, out var makes))
                {
                    var toBuildings = solution.Flows.Where(t => t.From == other.Id && t.Resource == resource && t.Kind is FlowKind.Internal or FlowKind.Trade or FlowKind.Contract).Sum(t => t.Sent);
                    potential += Math.Max(0, makes * other.MaxRate - toBuildings) * link.Efficiency;
                }
            }
            var (efficiency, capacity) = reach.Of(node.Id, resource);
            if (efficiency > 0)
            {
                var others = stationSpare.GetValueOrDefault(resource) - member.Spare.GetValueOrDefault(resource);
                var room = Math.Max(0, capacity - fromGate.GetValueOrDefault((node.Id, resource)));
                potential += Math.Min(Math.Max(0, others), room) * efficiency;
            }
            return potential;
        }

        foreach (var node in network.Nodes.Where(t => t.Kind == FlowNodeKind.Building && t.Id > 0))
        {
            foreach (var (resource, amount) in node.In)
            {
                Add(member.Uses, resource, amount * node.MaxRate);
            }
            if (node.MaxRate <= 1e-9 || node.In.Count == 0 || !_buildings.TryGetValue(node.Id, out var building))
            {
                continue;
            }
            var fOut = OutputFraction(building, node, network);
            if (fOut <= 1e-9)
            {
                continue;
            }
            foreach (var (resource, amount) in node.In)
            {
                var toGate = reach.Efficiency(node.Id, resource);
                if (toGate <= 0)
                {
                    continue;
                }
                var fOther = 1.0;
                foreach (var (other, otherAmount) in node.In.Where(t => t.Key != resource))
                {
                    fOther = Math.Min(fOther, Math.Min(1, Potential(node, other) / (otherAmount * node.MaxRate)));
                }
                var lack = amount * node.MaxRate * Math.Min(fOut, fOther) - received.GetValueOrDefault((node.Id, resource));
                if (lack > 1e-9)
                {
                    Add(member.Lacks, resource, lack / toGate);
                }
            }
        }

        // Feedback: what it was matched last round and didn't take shrinks what it is matched now
        if (_gateTerms.TryGetValue(member.Key, out var terms))
        {
            foreach (var ((resource, _), sources) in terms.Import.GroupBy(t => t.Key.Resource).Select(t => (t.First().Key, t.SelectMany(x => x.Value).ToList())))
            {
                var matched = sources.Sum(t => t.Value);
                var taken = takenAtGate.GetValueOrDefault(resource);
                if (matched > 1e-9 && taken < FeedbackShare * matched && member.Lacks.ContainsKey(resource))
                {
                    member.Lacks[resource] = Math.Min(member.Lacks[resource], FeedbackGrowth * taken + FeedbackProbe);
                }
            }
        }
    }

    /// <summary>Feedback (F1.3): a buyer that took less than this share of its match is matched at most this much more than it took, plus a probe a minute</summary>
    private const double FeedbackShare = 0.8;
    private const double FeedbackGrowth = 1.25;
    private const double FeedbackProbe = 0.5;

    /// <summary>
    /// How much of its speed a building's output lets it run at: 0 when what it makes can't leave (stuck), its run
    /// fraction when its only ways out are an own trading post or Storage that is full, else 1
    /// </summary>
    private double OutputFraction(Building building, FlowNode node, Network network)
    {
        if (node.Out.Count == 0)
        {
            return 1;
        }
        if (IdleReasonOf(building).IsStuck)
        {
            return 0;
        }
        var linked = LinkedBuildings(building, network);
        var full = linked.Any(t => t.Owner == building.Owner
            && ((Content.Building(t.Type).Kind == BuildingKind.TradingPost && TradingPostFull(t))
                || (Content.Building(t.Type).Kind == BuildingKind.Storage && StorageCapacity(t, Content.Building(t.Type)) - t.StoredTotal <= StorageEpsilon)));
        var otherWays = network.GateLinks.Any(t => t.Building == building.Id)
            || linked.Any(t => (t.Owner == building.Owner && Content.Building(t.Type).Kind is BuildingKind.TradingPost or BuildingKind.Storage && !(Content.Building(t.Type).Kind == BuildingKind.TradingPost ? TradingPostFull(t) : StorageCapacity(t, Content.Building(t.Type)) - t.StoredTotal <= StorageEpsilon))
                || (ToFlowNode(t) is { Kind: FlowNodeKind.Building } taker && node.Out.Keys.Any(taker.In.ContainsKey)));
        return full && !otherWays ? network.Solution.Rate(building.Id) / Math.Max(1e-12, node.MaxRate) : 1;
    }

    /// <summary>Whether a building's pipes reach a trading post of its owner that can sell this good</summary>
    private static bool ReachesOwnPost(Network network, long building, string resource)
    {
        var owner = network.Nodes.FirstOrDefault(t => t.Id == building)?.Owner;
        var posts = network.Nodes.Where(t => t.Kind == FlowNodeKind.TradingPost && t.Owner == owner && t.Throughput > 0).Select(t => t.Id).ToHashSet();
        return posts.Count > 0 && network.Links.Any(t => t.Allows(resource) && ((t.A == building && posts.Contains(t.B)) || (t.B == building && posts.Contains(t.A))));
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

            // Usual partners next: a player's running trade with a player (its rate over the last 7 days) comes before
            // anything new, so nobody's trade is cut by someone else's arrival (Mia's coal when Dev moves in)
            for (var s = 0; s < members.Count; s++)
            {
                var seller = members[s].Company;
                if (seller.IsNpc || seller.UsualTrades.Count == 0)
                {
                    continue;
                }
                for (var b = 0; b < members.Count; b++)
                {
                    var buyer = members[b].Company;
                    if (buyer.IsNpc || b == s || spare[s] <= 1e-9 || lacks[b] <= 1e-9 || !Allowed(s, b))
                    {
                        continue;
                    }
                    var usual = UsualRate(seller, buyer, resource);
                    if (usual <= 1e-9)
                    {
                        continue;
                    }
                    var amount = Math.Min(usual, Math.Min(spare[s], lacks[b]));
                    spare[s] -= amount;
                    lacks[b] -= amount;
                    var key = (members[s].Key, members[b].Key, resource);
                    result[key] = result.GetValueOrDefault(key) + amount;
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
        foreach (var (_, members) in byStation)
        {
            Handshakes(members);
        }
        foreach (var (station, members) in byStation)
        {
            var byKey = members.ToDictionary(t => t.Key);
            // What was matched for far markets leaves the gates now
            if (TrunksEnabled)
            {
                DeliverTrunks(members);
            }
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
                    if (!seller.Company.IsNpc && old.HubShare.GetValueOrDefault(resource) is > 0 and var hubShare)
                    {
                        var bound = TermsOf(buyerKey).HubBound;
                        if (!bound.TryGetValue((resource, priceClass), out var bySeller))
                        {
                            bound[(resource, priceClass)] = bySeller = new Dictionary<long, double>();
                        }
                        bySeller[seller.Company.Id] = bySeller.GetValueOrDefault(seller.Company.Id) + matched * ratio * hubShare;
                    }
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
                    terms.HubShare[resource] = byKey[sellerKey].HubShare(resource);
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
            // What is still spare can go beyond the own hub (public trunks, the Port, the crew's trunks)
            if (TrunksEnabled)
            {
                MatchTrunks(members, TermsOf);
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
                // Within the tolerance: the network keeps solving with what it has; what was matched still counts, and so
                // does who sends it now (each seller's share, at the total the network was solved with), else a seller
                // the Yard just matched would send for nothing while the old one is paid
                before.Matched = after.Matched;
                before.HubShare = after.HubShare;
                before.HubBound = after.HubBound;
                foreach (var (importKey, sources) in after.Import)
                {
                    var solvedTotal = before.Import.TryGetValue(importKey, out var old) ? old.Values.Sum() : 0;
                    var total = sources.Values.Sum();
                    if (solvedTotal > 1e-12 && total > 1e-12)
                    {
                        before.Import[importKey] = sources.ToDictionary(t => t.Key, t => t.Value * solvedTotal / total);
                    }
                }
            }
        }
        _couplingInterval = changed ? CouplingInterval : TimeSpan.FromTicks(Math.Min(MaxCouplingInterval.Ticks, _couplingInterval.Ticks * 2));
        if (limit is { } until && _couplingInterval > until)
        {
            // A tank runs dry or fills up before then
            _couplingInterval = until;
        }
    }

    /// <summary>
    /// A company's Yard pipe is connected for the first time (Loady's step counts it); when another player of the
    /// station is connected already, the two pipes meet: the Handshake (v1.2 §5), on both first screens
    /// </summary>
    private void Handshakes(List<YardMember> members)
    {
        var players = members.Where(t => t.Network != null && !t.Company.IsNpc).ToList();
        foreach (var member in players.Where(t => !t.Company.Stats.ContainsKey("yard")))
        {
            foreach (var other in players.Where(t => t != member && t.Company.Stats.ContainsKey("yard")))
            {
                Changes.Events.Add(new WorldEvent(member.Company.Id, WorldEventKind.Neighbour, $"🤝 Handshake! Your Yard pipe met {other.Company.Name}'s"));
                Changes.Events.Add(new WorldEvent(other.Company.Id, WorldEventKind.Neighbour, $"🤝 Handshake! {member.Company.Name}'s Yard pipe met yours"));
                member.Company.Report.Notes.Add($"Your Yard pipe met {other.Company.Name}'s: Handshake!");
                other.Company.Report.Notes.Add($"{member.Company.Name}'s Yard pipe met yours: Handshake!");
            }
            AddStat(member.Company, "yard", 1);
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

    /// <summary>
    /// What a company's estate sends to and gets from its station's residents per minute, by partner and resource (links
    /// aside): what the buyers' buildings take, never what was only matched (F1.6)
    /// </summary>
    public IReadOnlyList<(long Partner, string Resource, double PerMinute, bool Selling)> YardTrades(Estate estate)
    {
        var trades = new List<(long, string, double, bool)>();
        var key = KeyOf(estate);
        var home = BigStation(estate.Station)[0];
        double OverLink(long seller, long buyer, string resource) => LinkBetween(seller, buyer, resource)?.Rate ?? 0;
        if (_gateTerms.TryGetValue(key, out var terms) && estate.OwnerId is { } me)
        {
            foreach (var ((resource, priceClass), sources) in terms.Import)
            {
                var share = TakenShare(key, resource, priceClass);
                foreach (var (seller, amount) in sources.Where(t => LivesIn(t.Key, home)))
                {
                    if (amount * share - OverLink(seller, me, resource) > 1e-9)
                    {
                        trades.Add((seller, resource, amount * share - OverLink(seller, me, resource), false));
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
            foreach (var ((resource, priceClass), sources) in other.Import)
            {
                if (estate.OwnerId is { } owner && sources.TryGetValue(owner, out var amount)
                    && amount * TakenShare(otherKey, resource, priceClass) - OverLink(owner, buyer, resource) is > 1e-9 and var taken)
                {
                    trades.Add((buyer, resource, taken, true));
                }
            }
        }
        return trades;
    }

    /// <summary>The share of what the Yard brings an estate's gate that its buildings take (a stand-in takes it all)</summary>
    private double TakenShare(EstateKey key, string resource, int priceClass)
    {
        if (!_gateTerms.TryGetValue(key, out var terms) || !terms.Import.TryGetValue((resource, priceClass), out var sources))
        {
            return 0;
        }
        var total = sources.Values.Sum();
        if (total <= 1e-12)
        {
            return 0;
        }
        if (Estate(key.District, key.Slot) is not { } estate || (estate.OwnerId is { } owner && Company(owner) is { Asleep: true }))
        {
            return 1;
        }
        if (NetworkAt(estate.YardGate) is not { } network)
        {
            return 0;
        }
        var taken = network.GateFlows.Where(t => t.Import && t.Estate == key && t.Resource == resource && t.PriceClass == priceClass).Sum(t => t.Sent);
        return Math.Min(1, taken / total);
    }
}

namespace TransportPipes.Game.Simulation;

/// <summary>
/// Calculates the steady state of one network: how fast every building runs and where every resource goes.
///
/// Every resource is handed out in the flow order of the design (GDD §5.3):
/// 1. buildings of the same company,
/// 2. other companies (cheapest seller first, only up to the buyer's maximum price and when trade is allowed),
/// 3. the company's own storages (as sink for surplus and as source for shortages),
/// 4. the company's own trading posts (selling surplus to the hub, importing shortages when enabled).
/// Within a tier resources are shared in proportion, like the original request/provision algorithm.
///
/// Rates are found in two phases: an optimistic phase in which rates can go up and down (so a building that is limited
/// by one input doesn't keep another input away from others), and a final phase in which rates only go down until
/// everything is consistent, so no resource is ever created from nothing.
/// </summary>
public sealed class FlowSolver
{
    private const double Epsilon = 1e-9;
    private const double Unlimited = double.MaxValue / 4;

    /// <summary>Rates below this are zero (0.001% of the building's speed)</summary>
    private const double MinRate = 1e-5;

    private readonly Dictionary<long, FlowNode> _nodes;
    private readonly List<FlowNode> _buildings;
    private readonly FlowMarket _market;
    private readonly Dictionary<long, List<(long Other, FlowLink Link)>> _neighbours = new();
    private readonly List<string> _resources;

    private FlowSolver(IReadOnlyList<FlowNode> nodes, IReadOnlyList<FlowLink> links, FlowMarket market)
    {
        _nodes = nodes.ToDictionary(t => t.Id);
        _buildings = nodes.Where(t => t.Kind == FlowNodeKind.Building).OrderBy(t => t.Id).ToList();
        _market = market;
        foreach (var node in nodes)
        {
            _neighbours[node.Id] = new List<(long, FlowLink)>();
        }
        // One link per pair of nodes (and set of resources): the one with the least loss
        var best = new Dictionary<string, FlowLink>();
        foreach (var link in links)
        {
            if (link.A == link.B || !_nodes.ContainsKey(link.A) || !_nodes.ContainsKey(link.B))
            {
                continue;
            }
            var key = link.Signature;
            if (!best.TryGetValue(key, out var existing) || link.Efficiency > existing.Efficiency
                || (link.Efficiency == existing.Efficiency && link.Capacity > existing.Capacity))
            {
                best[key] = link;
            }
        }
        foreach (var link in best.Values.OrderBy(t => t.Signature, StringComparer.Ordinal))
        {
            _neighbours[link.A].Add((link.B, link));
            _neighbours[link.B].Add((link.A, link));
        }
        _resources = _buildings.SelectMany(t => t.In.Keys.Concat(t.Out.Keys)).Distinct().OrderBy(t => t, StringComparer.Ordinal).ToList();
    }

    public static FlowSolution Solve(IReadOnlyList<FlowNode> nodes, IReadOnlyList<FlowLink> links, FlowMarket market) =>
        new FlowSolver(nodes, links, market).Solve();

    private FlowSolution Solve()
    {
        // ------------------------------------------------ Optimistic phase
        var inFraction = _buildings.ToDictionary(t => t.Id, t => t.In.Keys.ToDictionary(r => r, _ => 1.0));
        var outFraction = _buildings.ToDictionary(t => t.Id, _ => 1.0);
        var rates = _buildings.ToDictionary(t => t.Id, t => t.MaxRate);

        for (var iteration = 0; iteration < 100; iteration++)
        {
            var offers = new Dictionary<(long, string), double>();
            var demands = new Dictionary<(long, string), double>();
            foreach (var building in _buildings)
            {
                var limitOut = Math.Min(1, MinOrOne(inFraction[building.Id].Values));
                foreach (var (resource, amount) in building.Out)
                {
                    offers[(building.Id, resource)] = amount * building.MaxRate * limitOut;
                }
                foreach (var (resource, amount) in building.In)
                {
                    var limitExcept = Math.Min(outFraction[building.Id], MinOrOne(inFraction[building.Id].Where(t => t.Key != resource).Select(t => t.Value)));
                    demands[(building.Id, resource)] = amount * building.MaxRate * Math.Min(1, limitExcept);
                }
            }

            var allocation = Allocate(offers, demands);

            // Fractions can swing between two buildings that share a scarce input; damping makes them settle
            var damping = iteration < 10 ? 1.0 : 0.5;
            double maxChange = 0;
            foreach (var building in _buildings)
            {
                if (building.MaxRate <= Epsilon)
                {
                    rates[building.Id] = 0;
                    continue;
                }

                // Inputs: what this building could get = what it received + the surplus of suppliers it can reach
                foreach (var (resource, amount) in building.In)
                {
                    var fraction = 1.0;
                    if (!HasUnlimitedSource(building, resource))
                    {
                        var potential = allocation.Received(building.Id, resource);
                        foreach (var (other, link) in Neighbours(building.Id, resource))
                        {
                            var supplier = _nodes[other];
                            if (supplier.Kind == FlowNodeKind.Building && supplier.Out.ContainsKey(resource) && Eligible(supplier, building, resource, link))
                            {
                                potential += Math.Max(0, offers[(other, resource)] - allocation.SentToBuildings(other, resource)) * link.Efficiency;
                            }
                        }
                        fraction = Math.Min(1, potential / (amount * building.MaxRate));
                    }
                    var updated = damping * fraction + (1 - damping) * inFraction[building.Id][resource];
                    maxChange = Math.Max(maxChange, Math.Abs(updated - inFraction[building.Id][resource]));
                    inFraction[building.Id][resource] = updated;
                }

                // Outputs: what could be placed = what went to buildings + what reachable buyers still miss (or everything with a sink)
                if (building.Out.Count != 0)
                {
                    var fraction = 1.0;
                    foreach (var (resource, amount) in building.Out)
                    {
                        if (HasSink(building, resource))
                        {
                            continue;
                        }
                        var potential = allocation.SentToBuildings(building.Id, resource);
                        foreach (var (other, link) in Neighbours(building.Id, resource))
                        {
                            var buyer = _nodes[other];
                            if (buyer.Kind == FlowNodeKind.Building && buyer.In.ContainsKey(resource) && Eligible(building, buyer, resource, link))
                            {
                                potential += Math.Max(0, demands[(other, resource)] - allocation.Received(other, resource)) / link.Efficiency;
                            }
                        }
                        fraction = Math.Min(fraction, Math.Min(1, potential / (amount * building.MaxRate)));
                    }
                    var updated = damping * fraction + (1 - damping) * outFraction[building.Id];
                    maxChange = Math.Max(maxChange, Math.Abs(updated - outFraction[building.Id]));
                    outFraction[building.Id] = updated;
                }

                var rate = building.MaxRate * Math.Min(1, Math.Min(outFraction[building.Id], MinOrOne(inFraction[building.Id].Values)));
                maxChange = Math.Max(maxChange, Math.Abs(rate - rates[building.Id]));
                rates[building.Id] = rate;
            }

            if (iteration > 0 && maxChange < 1e-7)
            {
                break;
            }
        }

        // ------------------------------------------------ Consistency phase: rates only go down
        Allocation final = null!;
        var converged = false;
        var nudged = false;
        var rounds = 0;
        for (var iteration = 0; iteration < 500; iteration++)
        {
            final = AllocateAtRates(rates);
            rounds++;

            var changed = false;
            foreach (var building in _buildings)
            {
                var rate = rates[building.Id];
                if (rate <= Epsilon)
                {
                    rates[building.Id] = 0;
                    continue;
                }
                var factor = 1.0;
                foreach (var (resource, amount) in building.In)
                {
                    var demand = amount * rate;
                    factor = Math.Min(factor, Math.Min(1, final.Received(building.Id, resource) / demand));
                }
                foreach (var (resource, amount) in building.Out)
                {
                    var offer = amount * rate;
                    factor = Math.Min(factor, Math.Min(1, final.Sent(building.Id, resource) / offer));
                }
                if (factor < 1 - 1e-9)
                {
                    // A tiny bit extra so the fixed point is reached quickly; rates that shrink towards zero in a
                    // cycle (two buildings waiting for each other) become zero
                    var reduced = rate * factor * (1 - 1e-12);
                    rates[building.Id] = reduced < MinRate ? 0 : reduced;
                    // Shares between many consumers pass rounding errors around forever: a change of less than a
                    // ten-millionth is kept but needs no other round (it took 500 rounds in a 25-factory network)
                    if (factor < 1 - 1e-7 || rates[building.Id] == 0)
                    {
                        changed = true;
                    }
                    else
                    {
                        nudged = true;
                    }
                }
            }

            if (!changed)
            {
                converged = true;
                break;
            }
        }

        if (!converged || nudged)
        {
            // The flows belong to the final rates
            final = AllocateAtRates(rates);
            rounds++;
        }
        var solution = BuildSolution(rates, final);
        solution.Rounds = rounds;
        return solution;
    }

    private Allocation AllocateAtRates(Dictionary<long, double> rates)
    {
        var offers = new Dictionary<(long, string), double>();
        var demands = new Dictionary<(long, string), double>();
        foreach (var building in _buildings)
        {
            foreach (var (resource, amount) in building.Out)
            {
                offers[(building.Id, resource)] = amount * rates[building.Id];
            }
            foreach (var (resource, amount) in building.In)
            {
                demands[(building.Id, resource)] = amount * rates[building.Id];
            }
        }
        return Allocate(offers, demands);
    }

    /// <summary>Whether a supplier may deliver to a consumer at all (same company, or allowed trade at an acceptable price)</summary>
    private bool Eligible(FlowNode supplier, FlowNode consumer, string resource, FlowLink link)
    {
        if (supplier.Owner == consumer.Owner)
        {
            return true;
        }
        var price = _market.SellerPrice(supplier.Owner, resource);
        return _market.BuyerMaxPrice(consumer.Owner, resource) >= price - Epsilon
            && _market.TradeAllowed(supplier.Owner, consumer.Owner, resource)
            && _market.LinkTradeAllowed(link, supplier.Owner, consumer.Owner, resource);
    }

    private readonly Dictionary<(long, string), List<(long Other, FlowLink Link)>> _neighboursByResource = new();

    /// <summary>
    /// The nodes a resource can reach from this node, each with the link it takes: a priority link when there is one,
    /// otherwise the one with the least loss
    /// </summary>
    private List<(long Other, FlowLink Link)> Neighbours(long node, string resource)
    {
        if (!_neighboursByResource.TryGetValue((node, resource), out var result))
        {
            result = _neighbours[node]
                .Where(t => t.Link.Allows(resource))
                .GroupBy(t => t.Other)
                .Select(t => t.OrderByDescending(n => n.Link.Priority).ThenByDescending(n => n.Link.Efficiency).ThenByDescending(n => n.Link.Capacity).First())
                .ToList();
            _neighboursByResource[(node, resource)] = result;
        }
        return result;
    }

    /// <summary>An own storage with this resource or an own trading post that imports it</summary>
    private bool HasUnlimitedSource(FlowNode consumer, string resource) =>
        Neighbours(consumer.Id, resource).Any(n =>
        {
            var node = _nodes[n.Other];
            return node.Owner == consumer.Owner
                && ((node.Kind == FlowNodeKind.Storage && node.StorageContents.Contains(resource)) || (node.Kind == FlowNodeKind.TradingPost && node.Imports.Contains(resource)));
        });

    /// <summary>
    /// Whether selling to another company over a link pays: the buyer pays for what arrives, so a long lossy pipe can
    /// earn less than the seller's own trading post would. Only then the trade doesn't happen.
    /// </summary>
    private bool WorthSelling(long producer, string resource, double price, FlowLink link)
    {
        var node = _nodes[producer];
        var post = Neighbours(producer, resource)
            .Where(n => _nodes[n.Other].Owner == node.Owner && _nodes[n.Other].Kind == FlowNodeKind.TradingPost)
            .Select(n => n.Link.Efficiency * (1 + _nodes[n.Other].PriceBonus))
            .DefaultIfEmpty(0)
            .Max();
        return price * link.Efficiency >= _market.HubBuyPrice(resource) * post - Epsilon;
    }

    /// <summary>An own storage that is not full or an own trading post the resource can reach</summary>
    private bool HasSink(FlowNode producer, string resource) =>
        Neighbours(producer.Id, resource).Any(n =>
        {
            var node = _nodes[n.Other];
            return node.Owner == producer.Owner
                && ((node.Kind == FlowNodeKind.Storage && node.StorageCanAccept) || node.Kind == FlowNodeKind.TradingPost);
        });

    private static double MinOrOne(IEnumerable<double> values)
    {
        var min = 1.0;
        foreach (var value in values)
        {
            min = Math.Min(min, value);
        }
        return min;
    }

    // ---------------------------------------------------------------- Allocation

    private sealed class Allocation
    {
        public readonly Dictionary<(long From, long To, string Resource, FlowKind Kind), (double Sent, double Delivered, double Price)> Flows = new();
        public readonly Dictionary<(long, string), double> SentTotals = new();
        public readonly Dictionary<(long, string), double> ReceivedTotals = new();
        public readonly Dictionary<(long, string), double> SentToBuildingTotals = new();

        public double SentToBuildings(long node, string resource) => SentToBuildingTotals.TryGetValue((node, resource), out var v) ? v : 0;

        public double Sent(long node, string resource) => SentTotals.TryGetValue((node, resource), out var v) ? v : 0;

        public double Received(long node, string resource) => ReceivedTotals.TryGetValue((node, resource), out var v) ? v : 0;

        public void Add(long from, long to, string resource, FlowKind kind, double sent, double delivered, double price)
        {
            var key = (from, to, resource, kind);
            Flows[key] = Flows.TryGetValue(key, out var existing)
                ? (existing.Sent + sent, existing.Delivered + delivered, price)
                : (sent, delivered, price);
            SentTotals[(from, resource)] = Sent(from, resource) + sent;
            ReceivedTotals[(to, resource)] = Received(to, resource) + delivered;
            if (kind is FlowKind.Internal or FlowKind.Trade)
            {
                SentToBuildingTotals[(from, resource)] = SentToBuildings(from, resource) + sent;
            }
        }
    }

    private Allocation Allocate(Dictionary<(long, string), double> offers, Dictionary<(long, string), double> demands)
    {
        var allocation = new Allocation();
        var capacityLeft = new Dictionary<FlowLink, double>(ReferenceEqualityComparer.Instance);
        double CapacityLeft(FlowLink link) => capacityLeft.TryGetValue(link, out var v) ? v : link.Capacity;

        var offerLeft = new Dictionary<(long, string), double>(offers);
        var demandLeft = new Dictionary<(long, string), double>(demands);

        foreach (var resource in _resources)
        {
            var producers = _buildings.Where(t => t.Out.ContainsKey(resource) && offerLeft[(t.Id, resource)] > Epsilon).Select(t => t.Id).ToList();
            var consumers = _buildings.Where(t => t.In.ContainsKey(resource) && demandLeft[(t.Id, resource)] > Epsilon).Select(t => t.Id).ToList();

            // 1. Within the same company: consumers behind a priority valve first
            Share(resource, producers, consumers, (p, c, _) => _nodes[p].Owner == _nodes[c].Owner, FlowKind.Internal, (_, _) => 0, priorityOnly: true);
            Share(resource, producers, consumers, (p, c, _) => _nodes[p].Owner == _nodes[c].Owner, FlowKind.Internal, (_, _) => 0);

            // 2. Between companies, cheapest sellers first
            var priceLevels = producers
                .Select(p => _market.SellerPrice(_nodes[p].Owner, resource))
                .Distinct()
                .OrderBy(t => t)
                .ToList();
            foreach (var price in priceLevels)
            {
                Share(resource, producers, consumers, (p, c, link) =>
                {
                    var seller = _nodes[p].Owner;
                    var buyer = _nodes[c].Owner;
                    return seller != buyer
                        && _market.SellerPrice(seller, resource) == price
                        && _market.BuyerMaxPrice(buyer, resource) >= price - Epsilon
                        && _market.TradeAllowed(seller, buyer, resource)
                        && _market.LinkTradeAllowed(link, seller, buyer, resource)
                        && WorthSelling(p, resource, price, link);
                }, FlowKind.Trade, (p, _) => _market.SellerPrice(_nodes[p].Owner, resource));
            }

            // 4. Own storages: surplus goes in (what they keep: see FlowNode.StorageWants), shortages come out
            ToSinks(resource, producers, node => node.Kind == FlowNodeKind.Storage && node.StorageTakes(resource), FlowKind.ToStorage, null);
            FromSources(resource, consumers, node => node.Kind == FlowNodeKind.Storage && node.StorageContents.Contains(resource), FlowKind.FromStorage, null);
        }

        // 5. Trading posts: shared throughput over all resources
        TradingPosts();

        // 6. What nobody took (the trading posts are full, or there are none) still goes into own storages, until full
        foreach (var resource in _resources)
        {
            var producers = _buildings.Where(t => t.Out.ContainsKey(resource) && offerLeft[(t.Id, resource)] > Epsilon).Select(t => t.Id).ToList();
            ToSinks(resource, producers, node => node.Kind == FlowNodeKind.Storage && node.StorageCanAccept, FlowKind.ToStorage, null);
        }

        return allocation;

        bool CanUse(FlowLink link) => CapacityLeft(link) > Epsilon;

        void Record(long from, long to, string resource, FlowKind kind, double sent, FlowLink link, double price)
        {
            var delivered = sent * link.Efficiency;
            allocation.Add(from, to, resource, kind, sent, delivered, price);
            capacityLeft[link] = CapacityLeft(link) - sent;
        }

        // Sharing between producers and consumers of one resource: every consumer gets the same fraction of its full need,
        // and what a consumer can't use (it is held back by something else) goes to the others
        void Share(string resource, List<long> producers, List<long> consumers, Func<long, long, FlowLink, bool> eligible, FlowKind kind, Func<long, long, double> price, bool priorityOnly = false, double budget = double.MaxValue)
        {
            // budget: the most that may be delivered in total (a contract's rate)
            var budgetLeft = budget;
            // Reused between consumers and rounds: a big network shares among hundreds of producers and consumers (1.4)
            var requests = new List<(long P, long C, FlowLink Link, double Sent, double Weight)>();
            var options = new List<(long P, FlowLink Link, double Potential)>();
            var byProducer = new Dictionary<long, List<int>>();
            var producerOrder = new List<long>();
            for (var round = 0; round < 25 && budgetLeft > Epsilon; round++)
            {
                requests.Clear();
                foreach (var c in consumers)
                {
                    var need = demandLeft[(c, resource)];
                    if (need <= Epsilon)
                    {
                        continue;
                    }
                    var consumer = _nodes[c];
                    var fullNeed = Math.Max(need, consumer.In[resource] * consumer.MaxRate);
                    options.Clear();
                    var total = 0.0;
                    foreach (var (other, link) in Neighbours(c, resource))
                    {
                        if ((priorityOnly && !link.Priority) || !offerLeft.TryGetValue((other, resource), out var left) || left <= Epsilon || !CanUse(link) || !eligible(other, c, link))
                        {
                            continue;
                        }
                        var potential = Math.Min(left, CapacityLeft(link)) * link.Efficiency;
                        if (potential > Epsilon)
                        {
                            options.Add((other, link, potential));
                            total += potential;
                        }
                    }
                    if (total <= Epsilon)
                    {
                        continue;
                    }
                    foreach (var (p, link, potential) in options)
                    {
                        var deliveredWanted = Math.Min(need * potential / total, potential);
                        requests.Add((p, c, link, Math.Min(deliveredWanted / link.Efficiency, CapacityLeft(link)), fullNeed * potential / total / link.Efficiency));
                    }
                }
                if (requests.Count == 0)
                {
                    return;
                }
                var wanted = 0.0;
                foreach (var request in requests)
                {
                    wanted += request.Sent * request.Link.Efficiency;
                }
                if (wanted > budgetLeft)
                {
                    var scale = budgetLeft / wanted;
                    for (var i = 0; i < requests.Count; i++)
                    {
                        requests[i] = (requests[i].P, requests[i].C, requests[i].Link, requests[i].Sent * scale, requests[i].Weight);
                    }
                }

                // The requests per producer, producers in the order they first asked
                foreach (var indices in byProducer.Values)
                {
                    indices.Clear();
                }
                producerOrder.Clear();
                for (var i = 0; i < requests.Count; i++)
                {
                    if (!byProducer.TryGetValue(requests[i].P, out var indices))
                    {
                        byProducer[requests[i].P] = indices = new List<int>();
                    }
                    if (indices.Count == 0)
                    {
                        producerOrder.Add(requests[i].P);
                    }
                    indices.Add(i);
                }
                var progress = 0.0;
                foreach (var producer in producerOrder)
                {
                    var list = byProducer[producer].Select(i => requests[i]).ToList();
                    var granted = WaterFill(list.Select(t => t.Sent).ToList(), list.Select(t => t.Weight).ToList(), offerLeft[(producer, resource)]);
                    for (var i = 0; i < list.Count; i++)
                    {
                        var (p, c, link, _, _) = list[i];
                        var sent = Math.Min(granted[i], CapacityLeft(link));
                        sent = Math.Min(sent, demandLeft[(c, resource)] / link.Efficiency);
                        if (sent <= Epsilon)
                        {
                            continue;
                        }
                        Record(p, c, resource, kind, sent, link, price(p, c));
                        offerLeft[(p, resource)] -= sent;
                        demandLeft[(c, resource)] -= sent * link.Efficiency;
                        budgetLeft -= sent * link.Efficiency;
                        progress += sent;
                    }
                }
                if (progress <= Epsilon)
                {
                    return;
                }
            }
        }

        // Gives every request the same level × weight, but never more than it asked, until the available amount is used up
        static List<double> WaterFill(List<double> asked, List<double> weights, double available)
        {
            if (asked.Sum() <= available)
            {
                return asked;
            }
            var granted = new double[asked.Count];
            var order = Enumerable.Range(0, asked.Count).OrderBy(i => asked[i] / Math.Max(weights[i], Epsilon)).ToList();
            var weightLeft = order.Sum(i => Math.Max(weights[i], Epsilon));
            var left = available;
            foreach (var i in order)
            {
                var weight = Math.Max(weights[i], Epsilon);
                var level = left / weightLeft;
                granted[i] = Math.Min(asked[i], level * weight);
                left -= granted[i];
                weightLeft -= weight;
            }
            return granted.ToList();
        }

        // Surplus of producers into unlimited sinks of the same company (split evenly over the reachable sinks)
        void ToSinks(string resource, List<long> producers, Func<FlowNode, bool> isSink, FlowKind kind, Func<long, double>? price)
        {
            foreach (var p in producers)
            {
                var left = offerLeft[(p, resource)];
                if (left <= Epsilon)
                {
                    continue;
                }
                var sinks = Neighbours(p, resource).Where(n => isSink(_nodes[n.Other]) && _nodes[n.Other].Owner == _nodes[p].Owner && CanUse(n.Link)).ToList();
                if (sinks.Count == 0)
                {
                    continue;
                }
                foreach (var (sink, link) in sinks)
                {
                    var sent = Math.Min(left / sinks.Count, CapacityLeft(link));
                    if (sent <= Epsilon)
                    {
                        continue;
                    }
                    Record(p, sink, resource, kind, sent, link, price?.Invoke(sink) ?? 0);
                    offerLeft[(p, resource)] -= sent;
                }
            }
        }

        // Shortages of consumers from unlimited sources of the same company
        void FromSources(string resource, List<long> consumers, Func<FlowNode, bool> isSource, FlowKind kind, Func<long, double>? price)
        {
            foreach (var c in consumers)
            {
                var need = demandLeft[(c, resource)];
                if (need <= Epsilon)
                {
                    continue;
                }
                var sources = Neighbours(c, resource).Where(n => isSource(_nodes[n.Other]) && _nodes[n.Other].Owner == _nodes[c].Owner && CanUse(n.Link)).ToList();
                if (sources.Count == 0)
                {
                    continue;
                }
                foreach (var (source, link) in sources)
                {
                    var sent = Math.Min(need / sources.Count / link.Efficiency, CapacityLeft(link));
                    if (sent <= Epsilon)
                    {
                        continue;
                    }
                    Record(source, c, resource, kind, sent, link, price?.Invoke(source) ?? 0);
                    demandLeft[(c, resource)] -= sent * link.Efficiency;
                }
            }
        }

        void TradingPosts()
        {
            var posts = _nodes.Values.Where(t => t.Kind == FlowNodeKind.TradingPost).OrderBy(t => t.Id).ToList();
            for (var index = 0; index < posts.Count; index++)
            {
                var post = posts[index];
                // Everything this post could sell or import, before the throughput limit
                var wanted = new List<(long Other, string Resource, FlowLink Link, double Amount, bool Sale)>();
                foreach (var other in _neighbours[post.Id].Select(t => t.Other).Distinct())
                {
                    var node = _nodes[other];
                    if (node.Owner != post.Owner || node.Kind != FlowNodeKind.Building)
                    {
                        continue;
                    }
                    foreach (var resource in node.Out.Keys)
                    {
                        var left = offerLeft[(other, resource)];
                        var link = Neighbours(post.Id, resource).FirstOrDefault(t => t.Other == other).Link;
                        if (left > Epsilon && link != null && CanUse(link))
                        {
                            wanted.Add((other, resource, link, left, true));
                        }
                    }
                    foreach (var resource in node.In.Keys)
                    {
                        var need = demandLeft[(other, resource)];
                        var link = Neighbours(post.Id, resource).FirstOrDefault(t => t.Other == other).Link;
                        if (need > Epsilon && post.Imports.Contains(resource) && link != null && CanUse(link))
                        {
                            wanted.Add((other, resource, link, need / link.Efficiency, false));
                        }
                    }
                }

                var total = wanted.Sum(t => t.Amount);
                if (total <= Epsilon)
                {
                    continue;
                }
                // A trading post has no limit (1.4 §8): it sells everything that reaches it
                foreach (var (other, resource, link, amount, sale) in wanted)
                {
                    // Several posts can reach the same producer: this one takes its share of what the posts after it haven't
                    // taken yet, so together they place everything
                    var reachingPosts = posts.Skip(index).Count(p => p.Owner == post.Owner && Neighbours(other, resource).Any(n => n.Other == p.Id));
                    var share = sale ? Math.Min(amount, offerLeft[(other, resource)] / Math.Max(1, reachingPosts)) : amount;
                    var sent = Math.Min(share, CapacityLeft(link));
                    if (sent <= Epsilon)
                    {
                        continue;
                    }
                    if (sale)
                    {
                        Record(other, post.Id, resource, FlowKind.Sale, sent, link, _market.HubBuyPrice(resource) * (1 + post.PriceBonus));
                        offerLeft[(other, resource)] -= sent;
                    }
                    else
                    {
                        Record(post.Id, other, resource, FlowKind.Import, sent, link, _market.HubSellPrice(resource));
                        demandLeft[(other, resource)] -= sent * link.Efficiency;
                    }
                }
            }
        }
    }

    // ---------------------------------------------------------------- Result

    private FlowSolution BuildSolution(Dictionary<long, double> rates, Allocation allocation)
    {
        var flows = allocation.Flows
            .Where(t => t.Value.Sent > 1e-9)
            .Select(t => new Flow(t.Key.From, t.Key.To, t.Key.Resource, t.Value.Sent, t.Value.Delivered, t.Key.Kind, t.Value.Price))
            .OrderBy(t => t.From).ThenBy(t => t.To).ThenBy(t => t.Resource, StringComparer.Ordinal)
            .ToList();

        var gold = new Dictionary<long, double>();
        void AddGold(long owner, double amount) => gold[owner] = (gold.TryGetValue(owner, out var v) ? v : 0) + amount;

        var storage = new Dictionary<long, Dictionary<string, double>>();
        void AddStorage(long node, string resource, double amount)
        {
            if (!storage.TryGetValue(node, out var contents))
            {
                storage[node] = contents = new Dictionary<string, double>();
            }
            contents[resource] = (contents.TryGetValue(resource, out var v) ? v : 0) + amount;
        }

        foreach (var flow in flows)
        {
            var from = _nodes[flow.From];
            var to = _nodes[flow.To];
            switch (flow.Kind)
            {
                case FlowKind.Trade:
                    AddGold(from.Owner, flow.Delivered * flow.PricePerUnit);
                    AddGold(to.Owner, -flow.Delivered * flow.PricePerUnit);
                    break;
                case FlowKind.Sale:
                    AddGold(from.Owner, flow.Delivered * flow.PricePerUnit);
                    break;
                case FlowKind.Import:
                    AddGold(to.Owner, -flow.Sent * flow.PricePerUnit);
                    break;
                case FlowKind.ToStorage:
                    AddStorage(flow.To, flow.Resource, flow.Delivered);
                    break;
                case FlowKind.FromStorage:
                    AddStorage(flow.From, flow.Resource, -flow.Sent);
                    break;
            }
        }

        return new FlowSolution
        {
            Rates = rates,
            Flows = flows,
            GoldPerMinute = gold,
            StorageNet = storage.ToDictionary(t => t.Key, t => (IReadOnlyDictionary<string, double>)t.Value),
        };
    }
}

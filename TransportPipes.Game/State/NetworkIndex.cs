using TransportPipes.Game.Simulation;

namespace TransportPipes.Game.State;

/// <summary>
/// A network's links, flows and gate flows by building, made once per solve: what looks at one building (why it
/// idles, what its pipes reach, what it has spare for the station) looks its own up instead of going through all of
/// them. A network of 200 buildings has 20,000 links: going through them for each of its buildings took the advisor
/// 1.5 s and every Yard round as long. Each list keeps the network's order, so sums come out exactly as before.
/// </summary>
public sealed class NetworkIndex
{
    private static readonly IReadOnlyList<FlowLink> NoLinks = [];
    private static readonly IReadOnlyList<Flow> NoFlows = [];
    private static readonly IReadOnlyList<GateFlow> NoGateFlows = [];
    private static readonly IReadOnlyList<GateLink> NoGateLinks = [];

    // What it was made from: a network that got new links, nodes, flows or gate flows gets a new index
    private readonly List<FlowLink> _links;
    private readonly List<FlowNode> _nodes;
    private readonly FlowSolution _solution;
    private readonly List<GateFlow> _gateFlows;
    private readonly List<GateLink> _gateLinks;

    private readonly Dictionary<long, List<FlowLink>> _linksOf = new();
    private readonly Dictionary<long, List<Flow>> _flowsTo = new();
    private readonly Dictionary<long, List<Flow>> _flowsFrom = new();
    private readonly Dictionary<long, List<GateFlow>> _gateFlowsOf = new();
    private readonly Dictionary<long, List<GateLink>> _gateLinksOf = new();
    private Dictionary<long, HashSet<long>>? _postsOf;

    public NetworkIndex(Network network)
    {
        _links = network.Links;
        _nodes = network.Nodes;
        _solution = network.Solution;
        _gateFlows = network.GateFlows;
        _gateLinks = network.GateLinks;
        Nodes = _nodes.ToDictionary(t => t.Id);
        foreach (var link in _links)
        {
            Add(_linksOf, link.A, link);
            if (link.B != link.A)
            {
                Add(_linksOf, link.B, link);
            }
        }
        foreach (var flow in _solution.Flows)
        {
            Add(_flowsTo, flow.To, flow);
            Add(_flowsFrom, flow.From, flow);
        }
        foreach (var flow in _gateFlows)
        {
            Add(_gateFlowsOf, flow.Building, flow);
        }
        foreach (var link in _gateLinks)
        {
            Add(_gateLinksOf, link.Building, link);
        }
    }

    private static void Add<T>(Dictionary<long, List<T>> lists, long key, T item)
    {
        if (!lists.TryGetValue(key, out var list))
        {
            lists[key] = list = new List<T>();
        }
        list.Add(item);
    }

    /// <summary>Whether it still describes the network as it is</summary>
    public bool IsFor(Network network) =>
        ReferenceEquals(_links, network.Links) && ReferenceEquals(_nodes, network.Nodes) && ReferenceEquals(_solution, network.Solution)
        && ReferenceEquals(_gateFlows, network.GateFlows) && ReferenceEquals(_gateLinks, network.GateLinks);

    /// <summary>The buildings as the last solve saw them, by id</summary>
    public IReadOnlyDictionary<long, FlowNode> Nodes { get; }

    /// <summary>The links with this building at one end</summary>
    public IReadOnlyList<FlowLink> LinksOf(long building) => _linksOf.TryGetValue(building, out var links) ? links : NoLinks;

    /// <summary>The last solve's flows into this building</summary>
    public IReadOnlyList<Flow> FlowsTo(long building) => _flowsTo.TryGetValue(building, out var flows) ? flows : NoFlows;

    /// <summary>The last solve's flows out of this building</summary>
    public IReadOnlyList<Flow> FlowsFrom(long building) => _flowsFrom.TryGetValue(building, out var flows) ? flows : NoFlows;

    /// <summary>What went through the Yard gates to and from this building at the last solve</summary>
    public IReadOnlyList<GateFlow> GateFlowsOf(long building) => _gateFlowsOf.TryGetValue(building, out var flows) ? flows : NoGateFlows;

    /// <summary>The paths from this building to the network's Yard gates</summary>
    public IReadOnlyList<GateLink> GateLinksOf(long building) => _gateLinksOf.TryGetValue(building, out var links) ? links : NoGateLinks;

    /// <summary>A company's trading posts in the network that can sell</summary>
    public IReadOnlySet<long> PostsOf(long owner)
    {
        _postsOf ??= _nodes.Where(t => t.Kind == FlowNodeKind.TradingPost && t.Throughput > 0).GroupBy(t => t.Owner).ToDictionary(t => t.Key, t => t.Select(n => n.Id).ToHashSet());
        return _postsOf.TryGetValue(owner, out var posts) ? posts : [];
    }
}

using TransportPipes.Game.World;

namespace TransportPipes.Game.Simulation;

public enum FlowNodeKind
{
    /// <summary>A building with a recipe (generators, extractors, factories, labs, pumps...)</summary>
    Building,

    /// <summary>Stores any resource of its owner: a sink while not full, a source while it has contents</summary>
    Storage,

    /// <summary>Sells its owner's surplus to the hub and can import from the hub</summary>
    TradingPost,
}

/// <summary>A building as the solver sees it. All rates are per minute at rate 1.</summary>
public sealed class FlowNode
{
    public required long Id { get; init; }
    public required long Owner { get; init; }
    public FlowNodeKind Kind { get; init; } = FlowNodeKind.Building;
    public IReadOnlyDictionary<string, double> In { get; init; } = new Dictionary<string, double>();
    public IReadOnlyDictionary<string, double> Out { get; init; } = new Dictionary<string, double>();

    /// <summary>0..1: 0 while under construction, 0.5 when damaged, ...</summary>
    public double MaxRate { get; init; } = 1;

    public double ResearchPerMinute { get; init; }

    /// <summary>Storage: false when it is full</summary>
    public bool StorageCanAccept { get; init; } = true;

    /// <summary>Storage: the resources it has some of</summary>
    public IReadOnlySet<string> StorageContents { get; init; } = new HashSet<string>();

    /// <summary>Trading post: units per minute it can sell plus import</summary>
    public double Throughput { get; init; }

    /// <summary>Trading post: extra on the hub buy price (0.05 = 5%)</summary>
    public double PriceBonus { get; init; }

    /// <summary>Trading post: resources it may buy from the hub</summary>
    public IReadOnlySet<string> Imports { get; init; } = new HashSet<string>();

    public FlowNode WithMaxRate(double maxRate) => new()
    {
        Id = Id,
        Owner = Owner,
        Kind = Kind,
        In = In,
        Out = Out,
        MaxRate = maxRate,
        ResearchPerMinute = ResearchPerMinute,
        StorageCanAccept = StorageCanAccept,
        StorageContents = StorageContents,
        Throughput = Throughput,
        PriceBonus = PriceBonus,
        Imports = Imports,
    };

    public override string ToString() => $"#{Id} ({Kind}, owner {Owner})";
}

/// <summary>
/// Two nodes that share a pipe group: resources can flow between them (in both directions).
/// Resources: only these pass (valves on the way); null = everything. Priority: the path crosses a priority valve, its
/// consumers are served first.
/// </summary>
public sealed record FlowLink(long A, long B, double Efficiency, double Capacity, IReadOnlyList<TilePos> Path, IReadOnlySet<string>? Resources = null, bool Priority = false)
{
    public bool Connects(long x, long y) => (A == x && B == y) || (A == y && B == x);

    public bool Allows(string resource) => Resources == null || Resources.Contains(resource);

    /// <summary>Links that differ only in efficiency (same nodes, resources and priority) compete: the best one wins</summary>
    internal string Signature => $"{Math.Min(A, B)}-{Math.Max(A, B)}-{(Resources == null ? "*" : string.Join(",", Resources.Order(StringComparer.Ordinal)))}-{Priority}";
}

/// <summary>Prices and trade rules the solver needs</summary>
public sealed class FlowMarket
{
    public required Func<string, double> HubBuyPrice { get; init; }
    public required Func<string, double> HubSellPrice { get; init; }

    /// <summary>The price a company asks from other companies</summary>
    public required Func<long, string, double> SellerPrice { get; init; }

    /// <summary>The highest price a company pays other companies</summary>
    public required Func<long, string, double> BuyerMaxPrice { get; init; }

    /// <summary>Whether two different companies may trade (settings, gold)</summary>
    public Func<long, long, string, bool> TradeAllowed { get; init; } = (_, _, _) => true;

    /// <summary>Active contracts: delivered before open trade, at their own price, up to their rate</summary>
    public IReadOnlyList<FlowContract> Contracts { get; init; } = [];

    /// <summary>Members of a corporation (HQ level 1) trade with each other whatever the buyer's maximum price</summary>
    public Func<long, long, bool> IgnoreMaxPrice { get; init; } = (_, _) => false;

    /// <summary>Whether two different companies may trade over this link (the gates on its path)</summary>
    public Func<FlowLink, long, long, string, bool> LinkTradeAllowed { get; init; } = (_, _, _, _) => true;
}

/// <summary>A contract the solver serves: the seller delivers up to Rate per minute of a resource to the buyer at Price</summary>
public sealed record FlowContract(long Id, long Seller, long Buyer, string Resource, double Rate, double Price);

public enum FlowKind
{
    /// <summary>Between two companies under a contract, at the contract price</summary>
    Contract,

    /// <summary>Between buildings of the same company</summary>
    Internal,

    /// <summary>Between two companies, paid at the seller's price</summary>
    Trade,

    /// <summary>Into a storage</summary>
    ToStorage,

    /// <summary>Out of a storage</summary>
    FromStorage,

    /// <summary>Sold to the hub through a trading post</summary>
    Sale,

    /// <summary>Bought from the hub through a trading post</summary>
    Import,
}

/// <summary>A flow of one resource from one node to another, per minute</summary>
public sealed record Flow(long From, long To, string Resource, double Sent, double Delivered, FlowKind Kind, double PricePerUnit);

public sealed class FlowSolution
{
    public required IReadOnlyDictionary<long, double> Rates { get; init; }
    public required IReadOnlyList<Flow> Flows { get; init; }

    /// <summary>Gold per minute per company (hub sales and trades minus imports and purchases)</summary>
    public required IReadOnlyDictionary<long, double> GoldPerMinute { get; init; }

    public required IReadOnlyDictionary<long, double> ResearchPerMinute { get; init; }

    /// <summary>Net change per minute of every storage, per resource</summary>
    public required IReadOnlyDictionary<long, IReadOnlyDictionary<string, double>> StorageNet { get; init; }

    public double Rate(long nodeId) => Rates.TryGetValue(nodeId, out var rate) ? rate : 0;

    public static readonly FlowSolution Empty = new()
    {
        Rates = new Dictionary<long, double>(),
        Flows = [],
        GoldPerMinute = new Dictionary<long, double>(),
        ResearchPerMinute = new Dictionary<long, double>(),
        StorageNet = new Dictionary<long, IReadOnlyDictionary<string, double>>(),
    };
}

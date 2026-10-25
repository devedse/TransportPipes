using TransportPipes.Game.Content;

namespace TransportPipes.Game.State;

/// <summary>One named end of a building's Yard trade: who takes its goods, or who sends it its inputs</summary>
public sealed record TradeEnd(string Resource, Company Partner, Building? PartnerBuilding, double PerMinute, double Price);

/// <summary>A neighbour who could use a good (a bid): the building that needs it, and how much more a minute</summary>
public sealed record GoodBid(string Resource, Company Buyer, Building? Building, double PerMinute);

/// <summary>
/// A building's trade as its owner sees it: who takes its goods, who sends it its inputs, who could use its goods and
/// whether its pipes reach the gate
/// </summary>
public sealed record BuildingTrade(IReadOnlyList<TradeEnd> Takers, IReadOnlyList<TradeEnd> Suppliers, IReadOnlyList<GoodBid> Bids, bool ReachesGate);

/// <summary>A building's trade with both ends named, and who in the Yard could use what it makes</summary>
public sealed partial class GameWorld
{
    /// <summary>A building's trade as its owner sees it: both ends named (the pairs show only to their two ends)</summary>
    public BuildingTrade TradeOf(Building building)
    {
        var empty = new BuildingTrade([], [], [], false);
        if (Company(building.Owner) is not { } owner || EstateAt(building.Position) is not { } estate)
        {
            return empty;
        }
        var definition = Content.Building(building.Type);
        var network = NetworkOf(building.Id);
        // Its own path to the gate (goods never pass through a building, so sharing the gate's pipes isn't enough)
        var reaches = network != null && network.GateLinks.Any(t => t.Building == building.Id);
        var trades = YardTrades(estate);
        // This building's part of its estate's gate trade in a good
        double Share(string resource, bool import)
        {
            var flows = network?.GateFlows.Where(t => t.Import == import && t.Resource == resource && t.Estate == KeyOf(estate)).ToList() ?? [];
            var total = flows.Sum(t => t.Sent);
            return total <= 1e-12 ? 0 : flows.Where(t => t.Building == building.Id).Sum(t => t.Sent) / total;
        }
        var takers = new List<TradeEnd>();
        var bids = new List<GoodBid>();
        foreach (var resource in definition.Out.Keys)
        {
            var share = reaches ? Share(resource, import: false) : 0;
            foreach (var trade in trades.Where(t => t.Selling && t.Resource == resource))
            {
                if (Company(trade.Partner) is { } partner && trade.PerMinute * share > 1e-6)
                {
                    takers.Add(new TradeEnd(resource, partner, NeedOf(partner, resource), trade.PerMinute * share, YardPrice(owner, partner, resource, selling: true)));
                }
            }
            bids.AddRange(Bids(owner, resource));
        }
        var suppliers = new List<TradeEnd>();
        foreach (var resource in definition.In.Keys)
        {
            var share = reaches ? Share(resource, import: true) : 0;
            foreach (var trade in trades.Where(t => !t.Selling && t.Resource == resource))
            {
                if (Company(trade.Partner) is { } partner && trade.PerMinute * share > 1e-6)
                {
                    suppliers.Add(new TradeEnd(resource, partner, null, trade.PerMinute * share, YardPrice(owner, partner, resource, selling: false)));
                }
            }
        }
        return new BuildingTrade(takers, suppliers, bids, reaches);
    }

    /// <summary>A bid needs at least this much of the good a minute at the seller's gate</summary>
    private const double BidSpareAtLeast = 0.5;

    /// <summary>What of a good a company puts through its Yard gate a minute: sold in the Yard, and spare nobody took</summary>
    private double AtGate(Company me, Estate estate, string resource, IReadOnlyList<YardResident> residents) =>
        (residents.FirstOrDefault(t => t.Company.Id == me.Id)?.Spare.GetValueOrDefault(resource) ?? 0)
        + YardTrades(estate).Where(t => t.Selling && t.Resource == resource).Sum(t => t.PerMinute);

    /// <summary>The name of a company's building that needs a good most ("Glassworks"), if any</summary>
    public string? TradeNeed(Company company, string resource) => NeedOf(company, resource) is { } building ? Content.Building(building.Type).Name : null;

    /// <summary>A company's building that needs a good most: one short of it, else the one that takes the most of it from the gate</summary>
    private Building? NeedOf(Company company, string resource)
    {
        var buildings = BuildingsOf(company.Id).Where(t => !t.UnderConstruction && Content.Building(t.Type).In.ContainsKey(resource)).ToList();
        return buildings.FirstOrDefault(t => IdleReasonOf(t) is { Resource: var r, Cause: IdleCause.InputMissing or IdleCause.InputShort } && r == resource)
            ?? buildings.OrderByDescending(t => NetworkOf(t.Id)?.GateFlows.Where(f => f.Import && f.Building == t.Id && f.Resource == resource).Sum(f => f.Delivered) ?? 0).FirstOrDefault();
    }

    /// <summary>
    /// Who in a company's Yard could use a good it makes (bids: "Ana's Glassworks could use your sand"), with what they
    /// still lack; what other players sell them stays private. Only where it would move something: you have spare of
    /// it, and they could use at least half of what one building makes of it (run 2: every bid was energy for 0.3-0.8 a
    /// minute)
    /// </summary>
    public List<GoodBid> Bids(Company me, string resource, IReadOnlyList<YardResident>? residents = null)
    {
        if (EstateOf(me) is not { } estate)
        {
            return [];
        }
        residents ??= YardResidents(estate.Station);
        if (AtGate(me, estate, resource, residents) <= BidSpareAtLeast)
        {
            return [];
        }
        var enough = Math.Max(1, 0.5 * OneBuildingRate(resource));
        var bids = new List<GoodBid>();
        foreach (var resident in residents.Where(t => t.Connected && t.Company.Id != me.Id))
        {
            var buyer = resident.Company;
            if (!TradesWith(me, buyer) || !TradesWith(buyer, me))
            {
                continue;
            }
            var couldUse = resident.Lacks.GetValueOrDefault(resource);
            if (couldUse >= enough)
            {
                bids.Add(new GoodBid(resource, buyer, NeedOf(buyer, resource), couldUse));
            }
        }
        return bids.OrderByDescending(t => t.PerMinute).ToList();
    }

    /// <summary>What a company and each neighbour sold each other this week (units each way, per good), from the trades they took</summary>
    public List<(Company Partner, string Resource, double Given, double Received)> TradesThisWeek(Company me)
    {
        var week = LastTick - TimeSpan.FromDays(UsualTradeDays - 1);
        var given = TakenSince(me, week);
        var gifts = new Dictionary<(long, string), (double Given, double Received)>();
        foreach (var ((buyer, resource), units) in given)
        {
            gifts[(buyer, resource)] = (units, 0);
        }
        foreach (var other in _companies.Values.Where(t => t.Id != me.Id && t.UsualTrades.Keys.Any(k => k.StartsWith($"{me.Id}:", StringComparison.Ordinal))))
        {
            foreach (var ((buyer, resource), units) in TakenSince(other, week).Where(t => t.Key.Buyer == me.Id))
            {
                var (g, _) = gifts.GetValueOrDefault((other.Id, resource));
                gifts[(other.Id, resource)] = (g, units);
            }
        }
        return gifts.Where(t => Company(t.Key.Item1) != null)
            .Select(t => (Company(t.Key.Item1)!, t.Key.Item2, t.Value.Given, t.Value.Received))
            .OrderByDescending(t => (t.Given + t.Received) * BasePrice(t.Item2))
            .ToList();
    }
}

using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>
/// A want (after run 3, the design team's step 2; 1.4 §9): what a building or a pipe still waits for. Its goods go into
/// the build: the owner's own spare first, then the neighbours' at their price; an Order finishes it.
/// </summary>
public sealed class CompanyAsk
{
    public required string Resource { get; init; }
    public double Amount { get; set; }
    public double Got { get; set; }

    /// <summary>Per minute until the next round: who brings what (it arrives and is paid in the meantime), and what the last round matched</summary>
    public Dictionary<long, double> Incoming { get; set; } = new();
    public Dictionary<long, double> Matched { get; set; } = new();

    /// <summary>
    /// A want (after run 3, the design team's step 2): the building whose materials this asks for. Its goods go into the
    /// build, never a Storage; the owner's own spare comes first, then the neighbours' at the one player price; it has no
    /// deadline (an Order finishes it)
    /// </summary>
    public long? Site { get; set; }

    /// <summary>A level step's goods (1.4 §9): the level the building steps up to once they're in (0: a scaffold's materials)</summary>
    public int ToLevel { get; init; }

    /// <summary>A pipe want (after run 3, step 3): the pipe tiles that become a better tier once its materials are in</summary>
    public List<TilePos>? PipeTiles { get; init; }
    public int PipeTier { get; init; }

    /// <summary>A want's own number (a pipe want has no building to name it by)</summary>
    public long WantId { get; init; }

    /// <summary>A want (a building's or a pipe's), not a plain Ask: no deadline, its goods go into the build</summary>
    public bool IsWant => Site != null || PipeTiles != null;

    /// <summary>When the want was raised (its friends' window counts sessions from then)</summary>
    public DateTime Raised { get; init; }

    /// <summary>A want: the gold its owner paid for what others brought (Cancel gives it back), and who brought how much</summary>
    public double Paid { get; set; }
    public Dictionary<long, double> From { get; set; } = new();

    public double Left => Math.Max(0, Amount - Got);
}

public sealed partial class GameWorld
{
    // ---------------------------------------------------------------- The Plan board

    /// <summary>Pins a good on the station's Plan board: "I make X" or "I need Y" (one tap each, again to take it off)</summary>
    public CommandResult SetPin(long companyId, string resource, bool need, bool on)
    {
        if (Company(companyId) is not { } company || !Content.Resources.ContainsKey(resource))
        {
            return CommandResult.Fail("Unknown good.");
        }
        var pins = need ? company.PinNeeds : company.PinMakes;
        if (on)
        {
            if (pins.Count >= 6)
            {
                return CommandResult.Fail("Six pins at most.");
            }
            pins.Add(resource);
        }
        else
        {
            pins.Remove(resource);
        }
        company.Touch();
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }

    /// <summary>The station's Plan board: what its players pinned they make and need</summary>
    public (HashSet<string> Makes, HashSet<string> Needs) PlanBoard(StationRef station)
    {
        var makes = new HashSet<string>();
        var needs = new HashSet<string>();
        foreach (var seat in StationSeats(station))
        {
            if (Estate(seat.District, seat.Slot)?.OwnerId is { } owner && Company(owner) is { } company)
            {
                makes.UnionWith(company.PinMakes);
                needs.UnionWith(company.PinNeeds);
            }
        }
        return (makes, needs);
    }

    /// <summary>The players of a station</summary>
    public List<Company> StationPlayers(StationRef station) =>
        StationSeats(station)
            .Select(t => Estate(t.District, t.Slot)?.OwnerId is { } o ? Company(o) : null)
            .OfType<Company>()
            .ToList();

    // ---------------------------------------------------------------- Wants in the Yard

    /// <summary>The free room in a company's storages</summary>
    public double StorageRoom(long companyId) =>
        BuildingsOf(companyId).Where(t => !t.UnderConstruction && Content.Building(t.Type).Kind == BuildingKind.Storage)
            .Sum(t => Math.Max(0, StorageCapacity(t, Content.Building(t.Type)) - t.StoredTotal));

    /// <summary>Puts goods into a company's storages; returns how much fitted</summary>
    private double PutInStorages(long companyId, string resource, double amount)
    {
        var left = amount;
        foreach (var storage in BuildingsOf(companyId).Where(t => !t.UnderConstruction && Content.Building(t.Type).Kind == BuildingKind.Storage).OrderBy(t => t.Id))
        {
            var put = Math.Min(StorageCapacity(storage, Content.Building(storage.Type)) - storage.StoredTotal, left);
            if (put <= 1e-9)
            {
                continue;
            }
            storage.Contents[resource] = storage.Contents.GetValueOrDefault(resource) + put;
            storage.Touch();
            MarkNeedsSolve(storage.Id);
            left -= put;
        }
        return amount - left;
    }

    /// <summary>
    /// The wants' share of a Yard's round: the owner's own spare goes into its builds first (it costs nothing), then the
    /// neighbours' spare, at their price
    /// </summary>
    private void MatchAsks(StationRef station, List<YardMember> members, Func<EstateKey, GateTerms> termsOf)
    {
        foreach (var asker in Askers(station, members))
        {
            foreach (var ask in asker.Asks)
            {
                ask.Matched = new Dictionary<long, double>();
                var lacks = ask.Left;
                foreach (var seller in members.Where(t => t.Spare.GetValueOrDefault(ask.Resource) > 1e-9
                             && (t.Company.Id == asker.Id || YardTradeAllowed(t.Company, asker, ask.Resource)))
                             .OrderBy(t => t.Company.Id == asker.Id ? 0 : 1).ThenBy(t => t.Company.Id))
                {
                    if (lacks <= 1e-9)
                    {
                        break;
                    }
                    var amount = Math.Min(seller.Spare[ask.Resource], lacks);
                    Add(seller.Spare, ask.Resource, -amount);
                    lacks -= amount;
                    ask.Matched[seller.Company.Id] = amount;
                    var terms = termsOf(seller.Key);
                    terms.Export[ask.Resource] = terms.Export.GetValueOrDefault(ask.Resource) + amount;
                }
            }
        }
    }

    /// <summary>
    /// What a company's own goods going into its wants now would sell for a minute: its income is about that much lower
    /// until they're in (playtest: after Grow, "I went from 3900 to 3400, why the hell did that happen?")
    /// </summary>
    public double OwnGoodsIntoWants(Company company) =>
        company.Asks.Where(t => t.IsWant).Sum(t => t.Incoming.GetValueOrDefault(company.Id) * HubBuyPrice(company, t.Resource));

    /// <summary>The players of a Yard with wants: its members, and a want's owner whose gate has no pipe yet</summary>
    private List<Company> Askers(StationRef station, List<YardMember> members) =>
        members.Select(t => t.Company)
            .Concat(StationPlayers(station))
            .Where(t => t.Asks.Count > 0)
            .DistinctBy(t => t.Id)
            .ToList();

    /// <summary>What the last round matched for the wants arrives now (less when a seller sent less)</summary>
    private void DeliverAsks(StationRef station, List<YardMember> members)
    {
        var byCompany = members.GroupBy(t => t.Company.Id).ToDictionary(t => t.Key, t => t.First());
        foreach (var asker in Askers(station, members))
        {
            foreach (var ask in asker.Asks)
            {
                ask.Incoming = new Dictionary<long, double>();
                foreach (var (sellerId, amount) in ask.Matched)
                {
                    if (!byCompany.TryGetValue(sellerId, out var seller) || !_gateTerms.TryGetValue(seller.Key, out var sent))
                    {
                        continue;
                    }
                    var exported = sent.Export.GetValueOrDefault(ask.Resource);
                    var ratio = exported <= 1e-12 ? 0 : Math.Min(1, seller.Sent.GetValueOrDefault(ask.Resource) / exported);
                    if (amount * ratio > 1e-9)
                    {
                        ask.Incoming[sellerId] = amount * ratio;
                    }
                }
            }
        }
    }

    /// <summary>What the owner and the neighbours brought goes into the builds</summary>
    private void SettleAsks(double minutes)
    {
        foreach (var asker in _companies.Values.Where(t => t.Asks.Count > 0))
        {
            // Its own Storages first (1.4 §9), once for all its wants
            FillWantsFromStorages(asker);
            foreach (var ask in asker.Asks.ToList())
            {
                SettleWant(asker, ask, minutes);
            }
        }
        foreach (var crew in _crews.Values.Where(t => t.Depot.Values.Any(g => g.Count > 0)).ToList())
        {
            FillWantsFromDepot(crew);
        }
    }
}

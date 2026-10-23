using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>
/// An Ask (v1.2 §12): a company asks its station for an amount of a good for 12–72 hours; the neighbours fill it
/// whenever they play (their spare goes to it first, at the one player price, into the asker's storages), and at the
/// deadline a House fills what is left at 2.0 × base, so an Ask always closes. It can renew itself.
/// </summary>
public sealed class CompanyAsk
{
    public required string Resource { get; init; }
    public double Amount { get; set; }
    public double Got { get; set; }
    public double FromHouse { get; set; }
    public DateTime Until { get; set; }
    public double Hours { get; init; }
    public bool Renew { get; set; }

    /// <summary>Per minute until the next round: who brings what (it arrives and is paid in the meantime), and what the last round matched</summary>
    public Dictionary<long, double> Incoming { get; set; } = new();
    public Dictionary<long, double> Matched { get; set; } = new();

    /// <summary>
    /// A want (after run 3, the design team's step 2): the building whose materials this asks for. Its goods go into the
    /// build, never a Storage; the owner's own spare comes first, then the neighbours' at the one player price; it has no
    /// deadline (an Order finishes it)
    /// </summary>
    public long? Site { get; init; }

    /// <summary>A pipe want (after run 3, step 3): the pipe tiles that become a better tier once its materials are in</summary>
    public List<TilePos>? PipeTiles { get; init; }
    public int PipeTier { get; init; }

    /// <summary>A want's own number (a pipe want has no building to name it by)</summary>
    public long WantId { get; init; }

    /// <summary>A want (a building's or a pipe's), not a plain Ask: no deadline, its goods go into the build</summary>
    public bool IsWant => Site != null || PipeTiles != null;

    /// <summary>A want: the gold its owner paid for what others brought (Cancel gives it back), and who brought how much</summary>
    public double Paid { get; set; }
    public Dictionary<long, double> From { get; set; } = new();

    public double Left => Math.Max(0, Amount - Got);
}

public sealed partial class GameWorld
{
    public const double AskMinHours = 12;
    public const double AskMaxHours = 72;
    public const int MaxAsks = 3;
    private const double AskHousePrice = 2.0;

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
        foreach (var seat in BigStation(station).SelectMany(StationSeats))
        {
            if (Estate(seat.District, seat.Slot)?.OwnerId is { } owner && Company(owner) is { IsNpc: false } company)
            {
                makes.UnionWith(company.PinMakes);
                needs.UnionWith(company.PinNeeds);
            }
        }
        return (makes, needs);
    }

    /// <summary>The players (not Houses) of a (big) station</summary>
    public List<Company> StationPlayers(StationRef station) =>
        BigStation(station).SelectMany(StationSeats)
            .Select(t => Estate(t.District, t.Slot)?.OwnerId is { } o ? Company(o) : null)
            .OfType<Company>()
            .Where(t => !t.IsNpc)
            .ToList();

    // ---------------------------------------------------------------- Asks

    public CommandResult Ask(long companyId, string resource, double amount, double hours, bool renew)
    {
        if (Company(companyId) is not { } company || !Content.Resources.TryGetValue(resource, out var definition))
        {
            return CommandResult.Fail("Unknown good.");
        }
        if (definition.Era > Era)
        {
            return CommandResult.Fail($"{definition.Name} comes in a later Era.");
        }
        if (amount <= 0 || double.IsNaN(amount))
        {
            return CommandResult.Fail("Choose an amount.");
        }
        if (company.Asks.Any(t => t.Resource == resource && !t.IsWant))
        {
            return CommandResult.Fail($"You ask for {definition.Name.ToLowerInvariant()} already.");
        }
        if (company.Asks.Count(t => !t.IsWant) >= MaxAsks)
        {
            return CommandResult.Fail($"At most {MaxAsks} Asks at a time.");
        }
        if (StorageRoom(company.Id) < amount)
        {
            return CommandResult.Fail("Your storages haven't room for that much.");
        }
        // Gold for the worst case: a House brings it all at the deadline
        var worst = amount * BasePrice(resource) * AskHousePrice;
        if (company.Gold < worst)
        {
            return CommandResult.Fail($"Keep {worst:N0} gold for it: a House fills what's left at the deadline.");
        }
        hours = Math.Clamp(hours, AskMinHours, AskMaxHours);
        company.Asks.Add(new CompanyAsk { Resource = resource, Amount = amount, Until = LastTick.AddHours(hours), Hours = hours, Renew = renew });
        company.Touch();
        Changes.Companies.Add(company.Id);
        WakeCoupling();
        if (EstateOf(company) is { } estate)
        {
            foreach (var neighbour in StationPlayers(estate.Station).Where(t => t.Id != company.Id))
            {
                Changes.Events.Add(new WorldEvent(neighbour.Id, WorldEventKind.Neighbour,
                    $"{company.Name} asks the station for {amount:N0} {definition.Name.ToLowerInvariant()}: your spare goes there first"));
            }
        }
        return CommandResult.Ok;
    }

    public CommandResult CancelAsk(long companyId, string resource)
    {
        if (Company(companyId) is not { } company || company.Asks.FirstOrDefault(t => t.Resource == resource && !t.IsWant) is not { } ask)
        {
            return CommandResult.Fail("No such Ask.");
        }
        company.Asks.Remove(ask);
        company.Touch();
        Changes.Companies.Add(company.Id);
        WakeCoupling();
        return CommandResult.Ok;
    }

    /// <summary>The free room in a company's storages</summary>
    public double StorageRoom(long companyId) =>
        _buildings.Values.Where(t => t.Owner == companyId && !t.UnderConstruction && Content.Building(t.Type).Kind == BuildingKind.Storage)
            .Sum(t => Math.Max(0, StorageCapacity(t, Content.Building(t.Type)) - t.StoredTotal));

    /// <summary>Puts goods into a company's storages; returns how much fitted</summary>
    private double PutInStorages(long companyId, string resource, double amount)
    {
        var left = amount;
        foreach (var storage in _buildings.Values.Where(t => t.Owner == companyId && !t.UnderConstruction && Content.Building(t.Type).Kind == BuildingKind.Storage).OrderBy(t => t.Id))
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
    /// The Asks' share of a station's round (v1.2 §7: contracts, then Asks, then links and the Yard): the residents' spare
    /// goes first to what their neighbours asked for, at the one player price
    /// </summary>
    private void MatchAsks(StationRef station, List<YardMember> members, Func<EstateKey, GateTerms> termsOf)
    {
        foreach (var asker in Askers(station, members))
        {
            var member = members.FirstOrDefault(t => t.Company.Id == asker.Id);
            foreach (var ask in asker.Asks)
            {
                // A plain Ask fills the asker's storages through its gate; a want goes into the build, gate or not
                if (!ask.IsWant && member == null)
                {
                    continue;
                }
                ask.Matched = new Dictionary<long, double>();
                // An Ask: in an hour at most what is left, never more than its storages hold. A want: all that is left,
                // the owner's own spare first (it costs nothing), then its neighbours'
                var lacks = ask.IsWant ? ask.Left : Math.Min(ask.Left, StorageRoom(asker.Id)) / 60;
                foreach (var seller in members.Where(t => (t.Company.Id != asker.Id || ask.IsWant) && !t.Company.IsNpc && !t.Company.Asleep && t.Spare.GetValueOrDefault(ask.Resource) > 1e-9
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

    /// <summary>The players of a station with Asks or wants: its Yard's members, and a want's owner whose gate has no pipe yet</summary>
    private List<Company> Askers(StationRef station, List<YardMember> members) =>
        members.Select(t => t.Company)
            .Concat(StationPlayers(station).Where(t => t.Asks.Any(a => a.IsWant)))
            .Where(t => !t.IsNpc && !t.Asleep && t.Asks.Count > 0)
            .DistinctBy(t => t.Id)
            .ToList();

    /// <summary>What the last round matched for the Asks arrives now (less when a seller sent less)</summary>
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

    /// <summary>
    /// What the neighbours brought goes into the asker's storages and is paid at the seller's hub price: goods that only
    /// fill a Storage carry no premium (an alt could pass on its bank by reselling them to the hub)
    /// </summary>
    private void SettleAsks(double minutes)
    {
        foreach (var asker in _companies.Values.Where(t => t.Asks.Count > 0))
        {
            foreach (var ask in asker.Asks.ToList())
            {
                if (ask.IsWant)
                {
                    SettleWant(asker, ask, minutes);
                    continue;
                }
                foreach (var (sellerId, perMinute) in ask.Incoming)
                {
                    if (Company(sellerId) is not { } seller)
                    {
                        continue;
                    }
                    var units = PutInStorages(asker.Id, ask.Resource, Math.Min(perMinute * minutes, ask.Left));
                    if (units <= 1e-9)
                    {
                        continue;
                    }
                    var gold = units * HubBuyPrice(seller, ask.Resource);
                    ask.Got += units;
                    asker.Gold -= gold;
                    AddStat(asker, "askPaid", gold);
                    seller.Gold += gold;
                    seller.Report.GoldEarned += gold;
                    Add(seller.Report.Sold, ask.Resource, units);
                    GainXpFromEarnings(seller, gold);
                    seller.Touch();
                    asker.Touch();
                    Changes.Companies.Add(seller.Id);
                    Changes.Companies.Add(asker.Id);
                }
            }
        }
    }

    /// <summary>The moments Asks reach their deadline</summary>
    private IEnumerable<(DateTime At, Action Run)> AskMoments(DateTime from, DateTime to)
    {
        foreach (var company in _companies.Values.Where(t => t.Asks.Count > 0).OrderBy(t => t.Id))
        {
            foreach (var ask in company.Asks.Where(t => !t.IsWant && t.Until <= to).ToList())
            {
                yield return (ask.Until, () => CloseAsk(company, ask));
            }
        }
    }

    /// <summary>The deadline: a House fills what is left at 2.0 × base (as far as the storages hold it); the Ask closes, or renews itself</summary>
    private void CloseAsk(Company company, CompanyAsk ask)
    {
        if (!company.Asks.Contains(ask))
        {
            return;
        }
        var name = Content.Resources[ask.Resource].Name.ToLowerInvariant();
        var neighbours = ask.Got;
        if (ask.Left > 1e-6)
        {
            var units = PutInStorages(company.Id, ask.Resource, ask.Left);
            var gold = units * BasePrice(ask.Resource) * AskHousePrice;
            company.Gold -= gold;
            ask.Got += units;
            ask.FromHouse += units;
        }
        company.Asks.Remove(ask);
        Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Neighbour, ask.FromHouse > 0
            ? $"Your Ask for {name} closed: your neighbours brought {neighbours:N0}, a House the other {ask.FromHouse:N0}"
            : $"Your Ask for {name} closed: your neighbours brought all {neighbours:N0}"));
        if (ask.Renew && StorageRoom(company.Id) >= ask.Amount && company.Gold >= ask.Amount * BasePrice(ask.Resource) * AskHousePrice)
        {
            company.Asks.Add(new CompanyAsk { Resource = ask.Resource, Amount = ask.Amount, Until = LastTick.AddHours(ask.Hours), Hours = ask.Hours, Renew = true });
        }
        company.Touch();
        Changes.Companies.Add(company.Id);
        WakeCoupling();
    }
}

using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>
/// The Era Wonder as the Capital's want (after run 3, the design team's step 5, docs/playtest-run3-design.html): a buyer
/// in every Capital station's Yard round after the station's project, at the base price, so the goods a crew planned for
/// it go in by pipe while everyone sleeps (run 3: the Great Dynamo sat at 0 of 1,800 bricks while Cleo's bricks went to
/// the hub behind a full Storage). Houses bring a quarter of each good at most; orders half at most while a Capital
/// player makes the good (all of it while nobody does), credited a quarter.
/// Each stage is sized when it starts from the Capital's income, and each stage that stands lifts the Capital for the Era.
/// </summary>
public sealed partial class GameWorld
{
    private const double WonderHouseShare = 0.25;
    private const double WonderOrderShare = 0.5;
    private const double WonderOrderCredit = 0.25;

    /// <summary>The Era Wonder buys in the Yards (on in the server; off in tests unless a test sets it)</summary>
    public bool WonderBuysEnabled { get; set; }

    /// <summary>Whether the Era Wonder buys in the Yards now</summary>
    private bool EraWonderBuying => WonderBuysEnabled && EraWonder is { Finished: false } && Content.EraWonders.Count > 0;

    /// <summary>What the Era Wonder still lacks of a good (ordered goods count: they are on their way)</summary>
    private double WonderLacks(Wonder wonder, string good) =>
        Math.Max(0, wonder.Required.GetValueOrDefault(good) - wonder.Delivered.GetValueOrDefault(good)
            - wonder.Orders.Where(t => t.Resource == good).Sum(t => t.Units - t.Shipped));

    /// <summary>The start of a Yard round: what the gates were matched for the Wonder last round arrives now, less when they sent less</summary>
    private void DeliverEraWonder(StationRef station, List<YardMember> members)
    {
        if (!EraWonderBuying || station.District != Capital || EraWonder is not { } wonder)
        {
            return;
        }
        foreach (var seller in members)
        {
            if (!wonder.Matched.TryGetValue(seller.Company.Id, out var goods) || !_gateTerms.TryGetValue(seller.Key, out var sent))
            {
                continue;
            }
            foreach (var (good, amount) in goods)
            {
                var exported = sent.Export.GetValueOrDefault(good);
                var ratio = exported <= 1e-12 ? 0 : Math.Min(1, seller.Sent.GetValueOrDefault(good) / exported);
                if (amount * ratio > 1e-9)
                {
                    if (!wonder.Incoming.TryGetValue(seller.Company.Id, out var incoming))
                    {
                        wonder.Incoming[seller.Company.Id] = incoming = new Dictionary<string, double>();
                    }
                    incoming[good] = incoming.GetValueOrDefault(good) + amount * ratio;
                }
            }
        }
    }

    /// <summary>
    /// After the station's project: the Wonder takes what the residents still have spare of what it lacks; the next round
    /// comes no later than it would be full (rounds space out to 16 minutes while nothing moves)
    /// </summary>
    private Dictionary<(EstateKey Seller, string Resource), double> MatchEraWonder(StationRef station, List<YardMember> members, ref TimeSpan? limit)
    {
        var result = new Dictionary<(EstateKey, string), double>();
        if (!EraWonderBuying || station.District != Capital || EraWonder is not { } wonder)
        {
            return result;
        }
        foreach (var good in wonder.Required.Keys.Order(StringComparer.Ordinal))
        {
            // What arrives this minute is on its way already
            var open = WonderLacks(wonder, good) - wonder.Incoming.Values.Sum(t => t.GetValueOrDefault(good));
            var lacks = open - wonder.Matched.Values.Sum(t => t.GetValueOrDefault(good));
            foreach (var member in members.Where(t => t.Spare.GetValueOrDefault(good) > 1e-9).OrderBy(t => t.Company.Id))
            {
                if (lacks <= 1e-9)
                {
                    break;
                }
                var amount = Math.Min(member.Spare[good], lacks);
                if (amount <= 1e-9)
                {
                    continue;
                }
                Add(member.Spare, good, -amount);
                lacks -= amount;
                result[(member.Key, good)] = amount;
                if (!wonder.Matched.TryGetValue(member.Company.Id, out var goods))
                {
                    wonder.Matched[member.Company.Id] = goods = new Dictionary<string, double>();
                }
                goods[good] = goods.GetValueOrDefault(good) + amount;
            }
            var rate = wonder.Matched.Values.Sum(t => t.GetValueOrDefault(good));
            if (rate > 1e-9)
            {
                var until = TimeSpan.FromMinutes(Math.Clamp(open / rate, CouplingInterval.TotalMinutes, MaxCouplingInterval.TotalMinutes));
                limit = limit is { } l && l < until ? l : until;
            }
        }
        return result;
    }

    /// <summary>What arrived at the Wonder is paid at the base price and counted as its builders' part; orders ship their share</summary>
    private void SettleEraWonder(double minutes)
    {
        if (!EraWonderBuying || EraWonder is not { } wonder)
        {
            return;
        }
        foreach (var (sellerId, goods) in wonder.Incoming)
        {
            if (Company(sellerId) is not { } seller)
            {
                continue;
            }
            foreach (var (good, perMinute) in goods)
            {
                // What left the gate is paid; what the stage still lacks of it counts
                var units = perMinute * minutes;
                var counted = Math.Min(units, Math.Max(0, wonder.Required.GetValueOrDefault(good) - wonder.Delivered.GetValueOrDefault(good)));
                if (units <= 1e-9)
                {
                    continue;
                }
                var worth = units * BasePrice(good);
                wonder.Delivered[good] = wonder.Delivered.GetValueOrDefault(good) + counted;
                seller.Gold += worth;
                seller.Report.GoldEarned += worth;
                Add(seller.Report.Sold, good, units);
                AddStat(seller, "wonderGold", worth);
                if (counted > 1e-9)
                {
                    wonder.Contributions[seller.Id] = wonder.Contributions.GetValueOrDefault(seller.Id) + counted * BasePrice(good);
                    AddStat(seller, "wonder", counted * BasePrice(good));
                    GainXpFromEarnings(seller, worth);
                }
                seller.Touch();
                Changes.Companies.Add(seller.Id);
                Changes.Wonders = true;
            }
        }
        foreach (var order in wonder.Orders.Where(t => t.Shipped < t.Units - 1e-9))
        {
            var units = Math.Min(order.Units / (OrderHours * 60) * minutes, order.Units - order.Shipped);
            units = Math.Min(units, Math.Max(0, wonder.Required.GetValueOrDefault(order.Resource) - wonder.Delivered.GetValueOrDefault(order.Resource)));
            if (units <= 1e-9)
            {
                continue;
            }
            order.Shipped += units;
            wonder.Delivered[order.Resource] = wonder.Delivered.GetValueOrDefault(order.Resource) + units;
            // Bought goods count a quarter, and carry no heart
            wonder.Contributions[order.By] = wonder.Contributions.GetValueOrDefault(order.By) + units * BasePrice(order.Resource) * WonderOrderCredit;
            Changes.Wonders = true;
        }
        if (wonder.Required.Count > 0 && wonder.Required.All(t => wonder.Delivered.GetValueOrDefault(t.Key) >= t.Value - 1e-6))
        {
            CloseWonderOrders(wonder);
            wonder.Matched.Clear();
            wonder.Incoming.Clear();
            wonder.FromHouses.Clear();
            CompleteStage(wonder);
        }
    }

    /// <summary>An order's unshipped part goes back to whoever paid (the stage stood by other goods)</summary>
    private void CloseWonderOrders(Wonder wonder)
    {
        foreach (var order in wonder.Orders)
        {
            var refund = (order.Units - order.Shipped) * order.Price;
            if (refund > 1e-6 && Company(order.By) is { } payer)
            {
                payer.Gold += refund;
                // Taken back: it counts as unspent (the playtest's measure)
                AddStat(payer, "goldSpent", -refund);
                payer.Touch();
                Changes.Companies.Add(payer.Id);
            }
        }
        wonder.Orders.Clear();
    }

    /// <summary>
    /// The Capital's players who make a good: played this week, awake, and own a finished building that makes it. While
    /// one does, half of the Era Wonder's good comes by pipe; while nobody does, gold may order all of it (after run 4:
    /// the Dynamo's bricks and the Cathedral's steel and concrete waited all Era on a pipe half nobody could fill)
    /// </summary>
    public List<Company> WonderMakers(string good)
    {
        var since = LastTick - ActivePeriod;
        return _companies.Values
            .Where(t => t.District == Capital && t.LastSeenAt >= since
                && BuildingsOf(t.Id).Any(b => !b.UnderConstruction && Content.Building(b.Type).Out.ContainsKey(good)))
            .OrderBy(t => t.Id)
            .ToList();
    }

    /// <summary>The share of an Era Wonder good that orders may bring: half while a Capital player makes it, else all of it</summary>
    public double WonderOrderCap(string good) => WonderMakers(good).Count > 0 ? WonderOrderShare : 1;

    /// <summary>What ordering a good of the Era Wonder costs now, and how much (half of it while somebody in the Capital makes it)</summary>
    public (double Units, double Cost)? WonderOrderOffer(string good)
    {
        if (EraWonder is not { Finished: false } wonder || !wonder.Required.TryGetValue(good, out var required))
        {
            return null;
        }
        var ordered = wonder.Orders.Where(t => t.Resource == good).Sum(t => t.Units);
        // Whole units, the last one rounded up (what arrives past the need is clipped, and its gold comes back when the
        // stage stands): never a 0.6 left over that nothing can order
        var units = Math.Ceiling(Math.Min(WonderOrderCap(good) * required - ordered, WonderLacks(wonder, good)) - 1e-6);
        return units < 1 ? null : (units, Math.Ceiling(units * BasePrice(good) * OrderPrice));
    }

    /// <summary>
    /// Anyone in the Capital may order part of the Era Wonder: 2 × base, shipped over six hours, credited a quarter; all of
    /// what may be ordered, or as much as some gold buys (an hour or a day of your income: several players can chip in)
    /// </summary>
    public CommandResult OrderForWonder(long payerId, string good, double? gold = null)
    {
        if (Company(payerId) is not { } payer || payer.District != Capital)
        {
            return CommandResult.Fail("Only the Capital's players can order for its Wonder.");
        }
        if (WonderOrderOffer(good) is not { } offer)
        {
            return CommandResult.Fail(WonderMakers(good) is { Count: > 0 } makers
                ? $"Half of it comes by pipe, from {string.Join(" and ", makers.Take(2).Select(t => t.Name))}: that half is ordered already."
                : "It has all it lacks, or on its way.");
        }
        var price = BasePrice(good) * OrderPrice;
        var units = gold is { } amount ? Math.Min(offer.Units, Math.Floor(amount / price)) : offer.Units;
        if (units < 1)
        {
            return CommandResult.Fail($"One costs {price:N0} gold.");
        }
        var cost = Math.Ceiling(units * price);
        if (payer.Gold < cost)
        {
            return CommandResult.Fail($"It costs {cost:N0} gold.");
        }
        payer.Gold -= cost;
        payer.Report.GoldSpent += cost;
        AddStat(payer, "goldSpent", cost);
        AddStat(payer, "ordered", cost);
        AddStat(payer, "pressed:wonderOrder", 1);
        EraWonder!.Orders.Add(new WonderOrder { By = payer.Id, Resource = good, Units = units, Price = price, At = LastTick });
        payer.Touch();
        Changes.Companies.Add(payer.Id);
        Changes.Wonders = true;
        return CommandResult.Ok;
    }

    /// <summary>The Capital's lift from the Era Wonder's stages that stand this Era: its pipes carry +50% after the first</summary>
    public double WonderPipeBonus(long companyId) =>
        EraWonder is { Stage: >= 1 } && Company(companyId) is { District: var district } && district == Capital ? 0.5 : 0;

    /// <summary>The Capital's pipes carry a different amount: their links are built again</summary>
    private void CapitalPipesChanged()
    {
        var capital = _companies.Values.Where(t => t.District == Capital).Select(t => t.Id).ToHashSet();
        foreach (var conductor in _conductors.Values.Where(t => capital.Contains(t.Owner)))
        {
            MarkDirty(conductor.Pos);
        }
        foreach (var id in capital)
        {
            MarkCompanyNetworksDirty(id);
        }
    }

    /// <summary>The Capital's Hub Orders pay +25% after the Era Wonder's second stage</summary>
    public double WonderOrderBonus(Company company) => EraWonder is { Stage: >= 2 } && company.District == Capital ? 0.25 : 0;
}

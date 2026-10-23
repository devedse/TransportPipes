using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>
/// The Era Wonder as the Capital's want (after run 3, the design team's step 5, docs/playtest-run3-design.html): a buyer
/// in every Capital station's Yard round after the station's project, at the base price, so the goods a crew planned for
/// it go in by pipe while everyone sleeps (run 3: the Great Dynamo sat at 0 of 1,800 bricks while Cleo's bricks went to
/// the hub behind a full Storage). Houses bring a quarter of each good at most; orders half at most, credited a quarter.
/// Each stage is sized when it starts from the Capital's income, and each stage that stands lifts the Capital for the Era.
/// </summary>
public sealed partial class GameWorld
{
    private const double WonderHouseShare = 0.25;
    private const double WonderOrderShare = 0.5;
    private const double WonderOrderCredit = 0.25;

    /// <summary>Whether the Era Wonder buys in the Yards now</summary>
    private bool EraWonderBuying => ProjectsEnabled && EraWonder is { Finished: false } && Content.EraWonders.Count > 0;

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
            foreach (var member in members.Where(t => t.Spare.GetValueOrDefault(good) > 1e-9 && !t.Company.Asleep).OrderBy(t => t.Company.IsNpc ? 1 : 0).ThenBy(t => t.Company.Id))
            {
                if (lacks <= 1e-9)
                {
                    break;
                }
                var allowance = member.Company.IsNpc
                    ? Math.Max(0, WonderHouseShare * wonder.Required[good] - wonder.FromHouses.GetValueOrDefault(good)) / 60
                    : double.MaxValue;
                var amount = Math.Min(Math.Min(member.Spare[good], allowance), lacks);
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
                if (seller.IsNpc)
                {
                    wonder.FromHouses[good] = wonder.FromHouses.GetValueOrDefault(good) + counted;
                }
                else if (counted > 1e-9)
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
                payer.Touch();
                Changes.Companies.Add(payer.Id);
            }
        }
        wonder.Orders.Clear();
    }

    /// <summary>What ordering a good of the Era Wonder costs now, and how much: half of each good at most comes by order</summary>
    public (double Units, double Cost)? WonderOrderOffer(string good)
    {
        if (EraWonder is not { Finished: false } wonder || !wonder.Required.TryGetValue(good, out var required))
        {
            return null;
        }
        var ordered = wonder.Orders.Where(t => t.Resource == good).Sum(t => t.Units);
        var units = Math.Min(WonderOrderShare * required - ordered, WonderLacks(wonder, good));
        return units < 1 ? null : (Math.Floor(units), Math.Ceiling(Math.Floor(units) * BasePrice(good) * OrderPrice));
    }

    /// <summary>Anyone in the Capital may order part of the Era Wonder (half of each good at most): 2 × base, over six hours, credited a quarter</summary>
    public CommandResult OrderForWonder(long payerId, string good)
    {
        if (Company(payerId) is not { } payer || payer.District != Capital)
        {
            return CommandResult.Fail("Only the Capital's players can order for its Wonder.");
        }
        if (WonderOrderOffer(good) is not { } offer)
        {
            return CommandResult.Fail("Half of it comes only by pipe: nothing more to order now.");
        }
        if (payer.Gold < offer.Cost)
        {
            return CommandResult.Fail($"It costs {offer.Cost:N0} gold.");
        }
        payer.Gold -= offer.Cost;
        payer.Report.GoldSpent += offer.Cost;
        AddStat(payer, "ordered", offer.Cost);
        EraWonder!.Orders.Add(new WonderOrder { By = payer.Id, Resource = good, Units = offer.Units, Price = BasePrice(good) * OrderPrice, At = LastTick });
        payer.Touch();
        Changes.Companies.Add(payer.Id);
        Changes.Wonders = true;
        return CommandResult.Ok;
    }

    /// <summary>Once a day: a stage nobody delivered to yet is sized again from the Capital as it is now (the first was sized at world start)</summary>
    private void ResizeIdleWonderStage()
    {
        if (EraWonder is not { Finished: false } wonder || wonder.Delivered.Values.Any(t => t > 1e-6) || wonder.Orders.Count > 0)
        {
            return;
        }
        wonder.Required = StageRequirements(WonderDefinition(wonder), wonder.Stage, null);
        Changes.Wonders = true;
    }

    /// <summary>The Capital's lift from the Era Wonder's stages that stand this Era: its pipes carry +50% after the first</summary>
    public double WonderPipeBonus(long companyId) =>
        EraWonder is { Stage: >= 1 } && Company(companyId) is { District: var district } && district == Capital ? 0.5 : 0;

    /// <summary>The Capital's pipes carry a different amount: their links are built again</summary>
    private void CapitalPipesChanged()
    {
        var capital = _companies.Values.Where(t => t.District == Capital && !t.IsNpc).Select(t => t.Id).ToHashSet();
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

using TransportPipes.Game.Content;
using TransportPipes.Game.Simulation;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>
/// The market (1.4 §8): every town's hub buys every good of the town's Era and older, and sells the same goods at 2.5×.
/// A company's Hub Order of a good is how much of it the market buys from it at the full base price each minute: 1,000
/// gold's worth for an Era-1 good, four times as much a later Era, half as much again for every Era the town is past
/// the good's, +10% for every trading post level, ×1.8 for every tier of its track beyond the fourth. Beyond the order
/// the market pays base × √(order × sold) up to sixteen orders, then a quarter of the base for every further unit: an
/// average price of base × clamp(√(order ÷ sold), 0.25, 1), measured over about an hour. Nobody's sales move anybody
/// else's price, and nothing depends on how many people play (rule 2).
/// </summary>
public sealed partial class GameWorld
{
    /// <summary>
    /// The markets look again this often (the owner, 2026-10-31, for the playtest: "remove all the hour timeouts. For
    /// everything including the price drops ... it all should happen instantly"; it was 15 minutes, over an hour's sales)
    /// </summary>
    public static readonly TimeSpan MarketInterval = TimeSpan.FromMinutes(1);

    /// <summary>The managers work this often</summary>
    public static readonly TimeSpan ManagersEvery = TimeSpan.FromMinutes(15);

    /// <summary>The hub's price follows what was sold over about this many minutes (0: since the last look; it was an hour)</summary>
    public const double MarketAverageMinutes = 0;

    /// <summary>Changes that affect a partner wait this long (a pause, a Block; for the playtest none: it was an hour)</summary>
    public static readonly TimeSpan NoticePeriod = TimeSpan.Zero;

    public double BasePrice(string resource) => Content.Resources.TryGetValue(resource, out var definition) ? definition.Price : 0;

    /// <summary>The one price between players: the Yard, links, contracts and the Exchange</summary>
    public double PlayerPrice(string resource) => Math.Round(BasePrice(resource) * PlayerPriceFactor, 4);

    /// <summary>The one price between players as a multiple of the base (content's; the light bots try others)</summary>
    public double PlayerPriceFactor { get; set; }

    private static string HubKey(DistrictCoord district) => $"{district.X},{district.Y}";

    /// <summary>A company's order at a hub</summary>
    public HubMarket HubMarketOf(Company company, DistrictCoord district)
    {
        var key = HubKey(district);
        if (!company.HubMarkets.TryGetValue(key, out var market))
        {
            market = new HubMarket { LastUpdate = LastTick, MinMultiplier = Content.Progression.Market.Floor, MaxMultiplier = 1 };
            company.HubMarkets[key] = market;
        }
        return market;
    }

    /// <summary>What the hub of a district pays a company for a resource (its own order there)</summary>
    public double HubBuyPrice(Company? company, DistrictCoord district, string resource) =>
        Content.Resources.TryGetValue(resource, out var good) && good.Era > TownEra(district)
            // A good of a later Era than the town's: the market doesn't want it (a quarter of the base, like a flood)
            ? Math.Round(good.Price * Content.Progression.Market.Floor, 4)
            : Math.Round(BasePrice(resource) * (company != null && company.HubMarkets.TryGetValue(HubKey(district), out var market) ? market.Multiplier(resource) : 1)
                * EventPriceFactor(district, resource), 4);

    /// <summary>What a company's home hub pays it for a resource</summary>
    public double HubBuyPrice(Company company, string resource) => HubBuyPrice(company, company.District, resource);

    /// <summary>What the hub of a district charges for a resource (the same for everybody)</summary>
    public double HubSellPrice(DistrictCoord district, string resource) =>
        Math.Round(BasePrice(resource) * Content.Progression.HubSpread * EventPriceFactor(district, resource), 4);

    /// <summary>Whether a company trades freely with another one (neither blocked the other)</summary>
    public bool TradesWith(Company company, Company other) => !company.TradeBlocked.Contains(other.Id);

    /// <summary>
    /// Stops (or starts again) trading with one company (Block, v1.2 §6): private, and after an hour's notice (what is
    /// on its way still arrives); unblocking is right away
    /// </summary>
    public CommandResult SetTradeBlocked(long companyId, long otherId, bool blocked)
    {
        if (Company(companyId) is not { } company || Company(otherId) is not { } other || otherId == companyId)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (blocked)
        {
            if (!company.TradeBlocked.Contains(otherId))
            {
                company.PendingBlocks[otherId] = LastTick + NoticePeriod;
                if (NoticePeriod <= TimeSpan.Zero)
                {
                    ApplyBlock(company, otherId);
                }
            }
        }
        else
        {
            company.TradeBlocked.Remove(otherId);
            company.PendingBlocks.Remove(otherId);
            MarkTradeDirty(company.Id);
        }
        company.Touch();
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }

    /// <summary>
    /// Once after a load: what waited out a notice that is gone now (price rises, blocks, links: an hour each before the
    /// playtest made them count at once) counts on this tick
    /// </summary>
    private void DueNoticesNow()
    {
        foreach (var company in _companies.Values)
        {
            if (PriceRiseNotice <= TimeSpan.Zero)
            {
                foreach (var setting in company.Prices.Values.Where(t => t.PendingAt > LastTick))
                {
                    setting.PendingAt = LastTick;
                }
            }
            if (NoticePeriod <= TimeSpan.Zero)
            {
                foreach (var other in company.PendingBlocks.Keys.ToList())
                {
                    company.PendingBlocks[other] = LastTick;
                }
            }
        }
        if (LinkNotice <= TimeSpan.Zero)
        {
            foreach (var link in _links.Values.Where(t => t.NoticeAt > LastTick))
            {
                link.NoticeAt = LastTick;
            }
        }
    }

    /// <summary>A block's notice is over: no more trade between the two</summary>
    private void ApplyBlock(Company company, long otherId)
    {
        if (!company.PendingBlocks.Remove(otherId))
        {
            return;
        }
        company.TradeBlocked.Add(otherId);
        company.Touch();
        MarkTradeDirty(company.Id);
        Changes.Companies.Add(company.Id);
        if (Company(otherId) is { } other)
        {
            Changes.Events.Add(new WorldEvent(other.Id, WorldEventKind.Neighbour, $"{company.Name} now trades its spare with others"));
        }
    }

    /// <summary>Whether a company buys a resource from other companies</summary>
    public CommandResult SetNoBuy(long companyId, string resource, bool noBuy)
    {
        if (Company(companyId) is not { } company || !Content.Resources.ContainsKey(resource))
        {
            return CommandResult.Fail("Unknown company or resource.");
        }
        if (noBuy)
        {
            company.NoBuy.Add(resource);
        }
        else
        {
            company.NoBuy.Remove(resource);
        }
        company.Touch();
        MarkTradeDirty(company.Id);
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }

    public CommandResult SetNoTrade(long companyId, string resource, bool noTrade)
    {
        if (Company(companyId) is not { } company || !Content.Resources.ContainsKey(resource))
        {
            return CommandResult.Fail("Unknown company or resource.");
        }
        if (noTrade)
        {
            company.NoTrade.Add(resource);
        }
        else
        {
            company.NoTrade.Remove(resource);
        }
        company.Touch();
        MarkTradeDirty(company.Id);
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }

    /// <summary>Who played in this period counts as playing this week (who makes a good, who an order waits for)</summary>
    public static readonly TimeSpan ActivePeriod = TimeSpan.FromDays(7);

    /// <summary>Players who opened the game in the last 3 days (a lit seat, v1.2 §3)</summary>
    public static readonly TimeSpan LitPeriod = TimeSpan.FromDays(3);

    public bool IsActive(Company company) => LastTick - company.LastSeenAt < LitPeriod;

    /// <summary>Active players on the whole server</summary>
    public int ActivePlayers() => _companies.Values.Count(IsActive);

    /// <summary>The level of a company's trading post (its Market Hall): every level raises every Hub Order by 10%</summary>
    public int PostLevel(Company company) =>
        BuildingsOf(company.Id).Where(t => !t.UnderConstruction && Content.Building(t.Type).Kind == BuildingKind.TradingPost).Select(t => t.Level).DefaultIfEmpty(1).Max();

    /// <summary>
    /// A company's Hub Order of a good in a town (1.4 §22): units a minute the market buys from it at the full base price.
    /// None for a good of a later Era than the town's (the market doesn't buy it yet). At home it also takes what the
    /// company sends over a trunk line to another town's market (1.4 §20).
    /// </summary>
    public double HubOrder(Company company, DistrictCoord town, string resource) =>
        BaseHubOrder(company, town, resource) + (town == company.District && company.Exports.Count > 0 ? ExportHubOrder(company, resource) : 0);

    /// <summary>A company's Hub Order of a good at a town's own market</summary>
    public double BaseHubOrder(Company company, DistrictCoord town, string resource)
    {
        if (!Content.Resources.TryGetValue(resource, out var good) || good.Price <= 0)
        {
            return 0;
        }
        var market = Content.Progression.Market;
        var townEra = TownEra(town);
        if (good.Era > townEra)
        {
            return 0;
        }
        return market.HubOrderValue * Math.Pow(market.EraValue, good.Era - 1) / good.Price
            * Math.Pow(market.EraGrowth, townEra - good.Era)
            * Math.Pow(market.PostLevel, PostLevel(company) - 1)
            * TrackHubOrderFactor(company, resource)
            * MonumentHubOrderFactor(company) * WonderHubOrderFactor(town) * ManagerHubOrderFactor(company)
            * EventDemandFactor(town, resource);
    }

    /// <summary>
    /// What a trading post bought up to some more levels adds a minute: the Hub Orders follow the company's highest post,
    /// so only the levels above it count
    /// </summary>
    public double PostLevelsGain(Company company, Building post, int levels) =>
        PostLevelGain(company, Math.Max(0, BoughtLevel(post) + levels - PostLevel(company)));

    /// <summary>
    /// About what more trading post levels add a minute: every good the company sold at its hub lately, paid against Hub
    /// Orders 10% bigger a level
    /// </summary>
    public double PostLevelGain(Company company, int levels)
    {
        var market = Content.Progression.Market;
        var gain = 0.0;
        // What the posts sell now, where the hour's average still lags behind (a plot just grown read "adds nothing")
        var rates = new Dictionary<string, double>(HubMarketOf(company, company.District).SoldRate);
        foreach (var (resource, now) in SellingNow(company))
        {
            rates[resource] = Math.Max(rates.GetValueOrDefault(resource), now);
        }
        foreach (var (resource, rate) in rates.Where(t => t.Value > 1e-9))
        {
            var order = HubOrder(company, resource);
            gain += market.Pays(BasePrice(resource), order * Math.Pow(market.PostLevel, levels), rate) - market.Pays(BasePrice(resource), order, rate);
        }
        return gain;
    }

    /// <summary>What a company's trading posts sell to the hub a minute now, by good (its networks' last solve)</summary>
    public Dictionary<string, double> SellingNow(Company company)
    {
        var selling = new Dictionary<string, double>();
        foreach (var network in _networks.Values.Where(t => t.Owner == company.Id))
        {
            foreach (var flow in network.Solution.Flows.Where(t => t.Kind == FlowKind.Sale && t.Delivered > 1e-9))
            {
                if (_buildings.TryGetValue(flow.To, out var post) && post.Owner == company.Id)
                {
                    selling[flow.Resource] = selling.GetValueOrDefault(flow.Resource) + flow.Delivered;
                }
            }
        }
        return selling;
    }

    /// <summary>A company's Hub Order of a good at its home town's market</summary>
    public double HubOrder(Company company, string resource) => HubOrder(company, company.District, resource);

    /// <summary>
    /// The price multiplier a company's market settles on for selling this much of a good a minute (the rule of
    /// HubMarket.Update): the full price up to its Hub Order (times a factor: more trading post levels), √(order ÷ sold)
    /// beyond it, a quarter at least
    /// </summary>
    public double SettledMultiplier(Company company, string resource, double perMinute, double orderFactor = 1)
    {
        var market = HubMarketOf(company, company.District);
        return perMinute <= 1e-9 ? market.MaxMultiplier
            : Math.Clamp(Math.Sqrt(HubOrder(company, resource) * orderFactor / perMinute), market.MinMultiplier, market.MaxMultiplier);
    }

    /// <summary>
    /// What a company's hub sales pay a minute once its market has settled on them (playtest: Grow promised +504 g/min
    /// and brought about +300: what it would sell more was valued at today's price, though beyond a Hub Order each unit
    /// more lowers the price of all of them)
    /// </summary>
    public double SettledSales(Company company, IEnumerable<Flow> sales, double orderFactor = 1)
    {
        var market = HubMarketOf(company, company.District);
        var gold = 0.0;
        foreach (var good in sales.GroupBy(t => t.Resource))
        {
            var now = good.Sum(t => t.Delivered * t.PricePerUnit);
            // A good the market doesn't buy at its price yet (a later Era's) stays as it is
            gold += HubOrder(company, good.Key) <= 0 ? now
                : now / Math.Max(0.01, market.Multiplier(good.Key)) * SettledMultiplier(company, good.Key, good.Sum(t => t.Delivered), orderFactor);
        }
        return gold;
    }

    /// <summary>What selling more (or less) of a good a minute adds once the market has settled on it</summary>
    public double SettledSaleGain(Company company, string resource, double now, double more)
    {
        if (HubOrder(company, resource) <= 0)
        {
            return more * HubBuyPrice(company, resource);
        }
        var price = BasePrice(resource) * EventPriceFactor(company.District, resource);
        var then = Math.Max(0, now + more);
        return price * (then * SettledMultiplier(company, resource, then) - now * SettledMultiplier(company, resource, now));
    }

    /// <summary>The goods a town's market buys and sells: those of its Era and older</summary>
    public IEnumerable<ResourceDefinition> MarketGoods(DistrictCoord town) => Content.ResourceList.Where(t => t.Era <= TownEra(town));

    private void UpdateMarkets()
    {
        foreach (var company in _companies.Values)
        {
            foreach (var (key, market) in company.HubMarkets)
            {
                var parts = key.Split(',');
                var district = new DistrictCoord(int.Parse(parts[0]), int.Parse(parts[1]));
                // LastTick: during a catch-up this runs at the moment of the update, not at the end
                if (market.Update(LastTick, resource => HubOrder(company, district, resource), MarketGoods(district).Select(t => t.Id), MarketAverageMinutes))
                {
                    MarkCompanyNetworksDirty(company.Id);
                    Changes.Companies.Add(company.Id);
                }
            }
        }
    }

    /// <summary>The next moment the markets are updated after a time (every 15 minutes, on the quarter)</summary>
    public static DateTime NextMarketUpdate(DateTime after)
    {
        var ticks = MarketInterval.Ticks;
        return new DateTime((after.Ticks / ticks + 1) * ticks, DateTimeKind.Utc);
    }
}

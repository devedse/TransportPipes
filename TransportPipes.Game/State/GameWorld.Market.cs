using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>
/// Prices (v1.2 §8): one price between players, and the hub's order for every company. The hub pays a company the full
/// price up to its own Hub Order of a good and less beyond it (base × clamp(√(order ÷ what it sells), 0.25, 1)); nobody's
/// sales move anybody else's price. The hub sells at its spread over the base price.
/// </summary>
public sealed partial class GameWorld
{
    public static readonly TimeSpan MarketInterval = TimeSpan.FromMinutes(15);

    /// <summary>Changes that affect a partner wait this long (a pause, a Block)</summary>
    public static readonly TimeSpan NoticePeriod = TimeSpan.FromHours(1);

    public double BasePrice(string resource) => Content.Resources.TryGetValue(resource, out var definition) ? definition.Price : 0;

    /// <summary>The one price between players: the Yard, links, contracts and the Exchange</summary>
    public double PlayerPrice(string resource) => Math.Round(BasePrice(resource) * Content.Progression.Together.PlayerPrice, 4);

    private static string HubKey(DistrictCoord district) => $"{district.X},{district.Y}";

    /// <summary>A company's order at a hub</summary>
    public HubMarket HubMarketOf(Company company, DistrictCoord district)
    {
        var key = HubKey(district);
        if (!company.HubMarkets.TryGetValue(key, out var market))
        {
            var together = Content.Progression.Together;
            market = new HubMarket { LastUpdate = LastTick, MinMultiplier = together.HubMinMultiplier, MaxMultiplier = together.HubMaxMultiplier };
            company.HubMarkets[key] = market;
        }
        return market;
    }

    /// <summary>What the hub of a district pays a company for a resource (its own order there)</summary>
    public double HubBuyPrice(Company? company, DistrictCoord district, string resource) =>
        Math.Round(BasePrice(resource) * (company != null && company.HubMarkets.TryGetValue(HubKey(district), out var market) ? market.Multiplier(resource) : Content.Progression.Together.HubMaxMultiplier)
            * EventPriceFactor(district, resource) * (company == null ? 1 : ShowroomFactor(company, resource)), 4);

    /// <summary>What a company's home hub pays it for a resource</summary>
    public double HubBuyPrice(Company company, string resource) => HubBuyPrice(company, company.District, resource);

    /// <summary>What the hub of a district charges for a resource (the same for everybody)</summary>
    public double HubSellPrice(DistrictCoord district, string resource) =>
        Math.Round(BasePrice(resource) * Content.Progression.HubSpread * EventPriceFactor(district, resource), 4);

    /// <summary>A Showroom: the hub pays 10% more for the product it shows</summary>
    public double ShowroomFactor(Company company, string resource) =>
        BuildingsOf(company.Id).Any(t => !t.UnderConstruction && Content.Building(t.Type).Kind == BuildingKind.Showroom && t.Filter.Contains(resource)) ? 1.1 : 1;

    /// <summary>Chooses the one product a Showroom shows</summary>
    public CommandResult SetShowroomProduct(long companyId, long buildingId, string resource)
    {
        if (Building(buildingId) is not { } building || building.Owner != companyId || Content.Building(building.Type).Kind != BuildingKind.Showroom)
        {
            return CommandResult.Fail("That is not your Showroom.");
        }
        if (!Content.Resources.ContainsKey(resource))
        {
            return CommandResult.Fail("Unknown resource.");
        }
        building.Filter.Clear();
        building.Filter.Add(resource);
        building.Touch();
        MarkCompanyNetworksDirty(companyId);
        return CommandResult.Ok;
    }

    /// <summary>Market Insight: where a company's hub price of a resource is heading (null without the tech)</summary>
    public double? HubPriceHeading(Company company, string resource) =>
        HasResearch(company, "marketInsight")
            ? Math.Round(BasePrice(resource) * HubMarketOf(company, company.District).Heading(resource, HubDemand(company.District, resource)) * EventPriceFactor(company.District, resource), 4)
            : null;

    /// <summary>Whether a company trades freely (not by contract) with another one: its partner list and choice</summary>
    public bool TradesWith(Company company, Company other) =>
        !company.TradeBlocked.Contains(other.Id) && company.TradeWith switch
        {
            TradeWith.Corporation => SameCorporation(company.Id, other.Id),
            TradeWith.Contracts => false,
            _ => true,
        };

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
        if (Company(otherId) is { IsNpc: false } other)
        {
            Changes.Events.Add(new WorldEvent(other.Id, WorldEventKind.Neighbour, $"{company.Name} now trades its spare with others"));
        }
    }

    /// <summary>Who a company trades with</summary>
    public CommandResult SetTradeWith(long companyId, TradeWith tradeWith)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (tradeWith == TradeWith.Corporation && company.CorporationId == null)
        {
            return CommandResult.Fail("Join or found a corporation first (from Company Level 8, in the Corp panel).");
        }
        company.TradeWith = tradeWith;
        company.Touch();
        MarkTradeDirty(company.Id);
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
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

    /// <summary>Companies that played in the last week, in a district</summary>
    public int ActiveCompanies(DistrictCoord district)
    {
        var since = LastTick - ActivePeriod;
        return _companies.Values.Count(t => t.District == district && !t.IsNpc && t.LastSeenAt >= since);
    }

    /// <summary>Players who opened the game in the last 3 days (a lit seat, v1.2 §3)</summary>
    public static readonly TimeSpan LitPeriod = TimeSpan.FromDays(3);

    public bool IsActive(Company company) => !company.IsNpc && !company.Asleep && LastTick - company.LastSeenAt < LitPeriod;

    /// <summary>Active players on the whole server</summary>
    public int ActivePlayers() => _companies.Values.Count(IsActive);

    /// <summary>Quiet grace (v1.2 §8): on a quiet server every Hub Order is bigger (on in the server; off in tests and the balance runs, which play a busy server)</summary>
    public bool QuietGraceEnabled { get; set; }

    public double QuietGrace() => QuietGraceEnabled ? Content.Progression.Together.GraceFor(ActivePlayers()) : 1;

    /// <summary>A company's Hub Order at a hub: what the hub takes of a resource per minute at the full price</summary>
    public double HubDemand(DistrictCoord district, string resource) =>
        (Content.Resources.TryGetValue(resource, out var definition) ? definition.Demand : 0) * Math.Pow(1.5, Era - 1)
        * EventDemandFactor(district, resource) * WonderDemandFactor(district) * QuietGrace();

    private void UpdateMarkets()
    {
        var resources = Content.ResourceList.Where(t => t.Era <= Era + 1).Select(t => t.Id).ToList();
        foreach (var company in _companies.Values)
        {
            foreach (var (key, market) in company.HubMarkets)
            {
                var parts = key.Split(',');
                var district = new DistrictCoord(int.Parse(parts[0]), int.Parse(parts[1]));
                // LastTick: during a catch-up this runs at the moment of the update, not at the end
                if (market.Update(LastTick, resource => HubDemand(district, resource), resources))
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

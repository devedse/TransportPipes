using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

public sealed partial class GameWorld
{
    public static readonly TimeSpan MarketInterval = TimeSpan.FromMinutes(15);

    /// <summary>Price changes that affect a partner wait this long (GDD §5.6)</summary>
    public static readonly TimeSpan NoticePeriod = TimeSpan.FromHours(1);

    private readonly Dictionary<DistrictCoord, HubMarket> _markets = new();

    public IReadOnlyDictionary<DistrictCoord, HubMarket> Markets => _markets;

    public HubMarket Market(DistrictCoord district)
    {
        if (!_markets.TryGetValue(district, out var market))
        {
            market = new HubMarket { LastUpdate = LastTick };
            _markets[district] = market;
        }
        return market;
    }

    public void LoadMarket(DistrictCoord district, HubMarket market) => _markets[district] = market;

    public double BasePrice(string resource) => Content.Resources.TryGetValue(resource, out var definition) ? definition.Price : 0;

    /// <summary>What the hub of a district pays for a resource</summary>
    public double HubBuyPrice(DistrictCoord district, string resource) =>
        Math.Round(BasePrice(resource) * (_markets.TryGetValue(district, out var market) ? market.Multiplier(resource) : 1) * EventPriceFactor(district, resource), 4);

    /// <summary>What the hub of a district charges for a resource</summary>
    public double HubSellPrice(DistrictCoord district, string resource) => HubBuyPrice(district, resource) * Content.Progression.HubSpread;

    /// <summary>The prices players may ask from each other: between the hub's prices, wider with Haggling</summary>
    public (double Low, double High) FairBand(Company company, string resource)
    {
        var bonus = BandWidthBonus(company);
        return (HubBuyPrice(company.District, resource) * (1 - bonus), HubSellPrice(company.District, resource) * (1 + bonus));
    }

    public double AutoPrice(Company company, string resource) =>
        Math.Round((HubBuyPrice(company.District, resource) + HubSellPrice(company.District, resource)) / 2, 2);

    /// <summary>The price a company asks other companies (kept within today's fair band)</summary>
    public double SellPrice(Company company, string resource)
    {
        if (!company.SellPrices.TryGetValue(resource, out var price))
        {
            return AutoPrice(company, resource);
        }
        var (low, high) = FairBand(company, resource);
        return Math.Round(Math.Clamp(price, low, high), 2);
    }

    /// <summary>The highest price a company pays other companies</summary>
    public double BuyMaxPrice(Company company, string resource)
    {
        if (!company.BuyMaxPrices.TryGetValue(resource, out var price))
        {
            return HubSellPrice(company.District, resource);
        }
        var (low, high) = FairBand(company, resource);
        return Math.Round(Math.Clamp(price, low, high), 2);
    }

    /// <summary>A Showroom: partners pay 10% more for the product it shows</summary>
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

    /// <summary>Market Insight: where the hub price of a resource is heading (null without the tech)</summary>
    public double? HubPriceHeading(Company company, string resource) =>
        HasResearch(company, "marketInsight")
            ? Math.Round(BasePrice(resource) * Market(company.District).Heading(resource, HubDemand(company.District, resource)) * EventPriceFactor(company.District, resource), 4)
            : null;

    /// <summary>The companies that buy this resource from a company right now</summary>
    public HashSet<long> PartnersBuying(long companyId, string resource)
    {
        var buyers = new HashSet<long>();
        foreach (var network in _networks.Values)
        {
            foreach (var flow in network.Solution.Flows)
            {
                if (flow.Kind is Simulation.FlowKind.Trade or Simulation.FlowKind.Contract && flow.Resource == resource
                    && _buildings.TryGetValue(flow.From, out var from) && from.Owner == companyId
                    && _buildings.TryGetValue(flow.To, out var to) && to.Owner != companyId)
                {
                    buyers.Add(to.Owner);
                }
            }
        }
        return buyers;
    }

    /// <summary>Whether a company sells this resource to another company right now</summary>
    public bool SellsToPartners(long companyId, string resource) =>
        _networks.Values.Any(network => network.Solution.Flows.Any(flow =>
            flow.Kind == Simulation.FlowKind.Trade && flow.Resource == resource && _buildings.TryGetValue(flow.From, out var from) && from.Owner == companyId));

    /// <summary>Sets the price for other companies; when a partner buys it now, it applies after the notice period</summary>
    public CommandResult SetSellPrice(long companyId, string resource, double? price)
    {
        if (Company(companyId) is not { } company || !Content.Resources.ContainsKey(resource))
        {
            return CommandResult.Fail("Unknown company or resource.");
        }
        double? value = null;
        if (price is { } p)
        {
            var (low, high) = FairBand(company, resource);
            value = Math.Round(Math.Clamp(p, low, high), 2);
        }
        if (SellsToPartners(companyId, resource))
        {
            company.PendingPrices[resource] = new PendingPrice(value, Now + NoticePeriod);
            var name = Content.Resources[resource].Name.ToLowerInvariant();
            var text = value is { } v ? $"{company.Name} sells {name} at {v:0.##} g from {Now + NoticePeriod:HH:mm} UTC" : $"{company.Name} sells {name} at the automatic price from {Now + NoticePeriod:HH:mm} UTC";
            foreach (var buyer in PartnersBuying(companyId, resource))
            {
                Changes.Events.Add(new WorldEvent(buyer, WorldEventKind.PriceChanged, text));
            }
        }
        else
        {
            ApplySellPrice(company, resource, value);
        }
        company.Touch();
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }

    private void ApplySellPrice(Company company, string resource, double? value)
    {
        if (value is { } v)
        {
            company.SellPrices[resource] = v;
        }
        else
        {
            company.SellPrices.Remove(resource);
        }
        company.PendingPrices.Remove(resource);
        company.Touch();
        Changes.Companies.Add(company.Id);
        MarkCompanyNetworksDirty(company.Id);
    }

    public CommandResult SetBuyMaxPrice(long companyId, string resource, double? price)
    {
        if (Company(companyId) is not { } company || !Content.Resources.ContainsKey(resource))
        {
            return CommandResult.Fail("Unknown company or resource.");
        }
        if (price is { } value)
        {
            var (low, high) = FairBand(company, resource);
            company.BuyMaxPrices[resource] = Math.Round(Math.Clamp(value, low, high), 2);
        }
        else
        {
            company.BuyMaxPrices.Remove(resource);
        }
        company.Touch();
        MarkCompanyNetworksDirty(company.Id);
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }

    /// <summary>Whether a company trades freely (not by contract) with another one: its partner list and choice</summary>
    public bool TradesWith(Company company, Company other) =>
        !company.TradeBlocked.Contains(other.Id) && company.TradeWith switch
        {
            TradeWith.Corporation => SameCorporation(company.Id, other.Id),
            TradeWith.Contracts => false,
            _ => true,
        };

    /// <summary>Stops (or starts again) trading with one company</summary>
    public CommandResult SetTradeBlocked(long companyId, long otherId, bool blocked)
    {
        if (Company(companyId) is not { } company || Company(otherId) is null || otherId == companyId)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (blocked)
        {
            company.TradeBlocked.Add(otherId);
        }
        else
        {
            company.TradeBlocked.Remove(otherId);
        }
        company.Touch();
        MarkCompanyNetworksDirty(company.Id);
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }

    /// <summary>Who a company trades with on its pipes</summary>
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
        MarkCompanyNetworksDirty(company.Id);
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
        MarkCompanyNetworksDirty(company.Id);
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
        MarkCompanyNetworksDirty(company.Id);
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }

    /// <summary>Companies that played in the last week, in a district</summary>
    public int ActiveCompanies(DistrictCoord district)
    {
        var since = LastTick - ActivePeriod;
        return _companies.Values.Count(t => t.District == district && !t.IsNpc && t.LastSeenAt >= since);
    }

    /// <summary>What a hub wants per minute of a resource before its price drops</summary>
    public double HubDemand(DistrictCoord district, string resource) =>
        (Content.Resources.TryGetValue(resource, out var definition) ? definition.Demand : 0) * Math.Max(1, ActiveCompanies(district)) * Math.Pow(1.5, Era - 1)
        * EventDemandFactor(district, resource) * WonderDemandFactor(district);

    private void UpdateMarkets()
    {
        foreach (var district in _openDistricts)
        {
            Market(district);
        }
        foreach (var (district, market) in _markets)
        {
            var resources = Content.ResourceList.Where(t => t.Era <= Era + 1).Select(t => t.Id);
            // LastTick: during a catch-up this runs at the moment of the update, not at the end
            if (market.Update(LastTick, resource => HubDemand(district, resource), resources))
            {
                foreach (var network in _networks.Values.Where(t => t.District == district))
                {
                    network.NeedsSolve = true;
                    Changes.Networks.Add(network.Id);
                }
                Changes.Markets.Add(district);
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

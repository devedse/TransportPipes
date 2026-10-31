using TransportPipes.Game.Content;

namespace TransportPipes.Game.State;

/// <summary>
/// Level steps (1.4 §9): levels stop at 25, a step costs what that copy cost × 1.15 for every level it already has, and
/// from level 11 also goods worth 30% of its gold, made in the building's own Era. The gold is paid at once; the goods
/// come in like a scaffold's materials (your Storages, your own spare as it's made, the Yard at your neighbours' prices,
/// the market at 2.5× if you allow imports of the good), and the building works at its old level under a little
/// scaffolding until they're in. Max buys several steps: their goods come in one after the other.
/// </summary>
public sealed partial class GameWorld
{
    /// <summary>The market brings what a want still lacks of a good you allow imports of in about this many minutes</summary>
    public const double ImportMinutes = 10;

    /// <summary>The level a building was bought up to: its level, or the last step whose goods are still coming</summary>
    public int BoughtLevel(Building building) => Math.Max(building.Level, PendingLevel(building));

    private int PendingLevel(Building building) =>
        Company(building.Owner) is { } owner ? owner.Asks.Where(t => t.Site == building.Id && t.ToLevel > 0).Select(t => t.ToLevel).DefaultIfEmpty(0).Max() : 0;

    /// <summary>Whether a building waits for the goods of a level (it works at its old level under a little scaffolding)</summary>
    public bool WaitsForLevel(Building building) => PendingLevel(building) > building.Level;

    /// <summary>The goods the step up to a level takes (none below level 11), for all members of a complex</summary>
    public IReadOnlyDictionary<string, double> LevelGoods(Building building, int toLevel)
    {
        var definition = Content.Building(building.Type);
        var gold = Content.Progression.UpgradeCost(CopyPrice(building), toLevel - 1) * ComplexMembers(building);
        return Content.Progression.LevelGoodsFor(definition.Unlock.Era, toLevel, gold, BasePrice);
    }

    /// <summary>The goods a building's next steps take, by good (none below level 11)</summary>
    public Dictionary<string, double> StepGoods(Building building, int levels)
    {
        var from = BoughtLevel(building);
        var goods = new Dictionary<string, double>();
        for (var to = from + 1; to <= Math.Min(from + levels, Content.Progression.MaxBuildingLevel); to++)
        {
            foreach (var (resource, amount) in LevelGoods(building, to))
            {
                goods[resource] = goods.GetValueOrDefault(resource) + amount;
            }
        }
        return goods;
    }

    /// <summary>
    /// What the market brings a minute now for a company's wants (its levels', buildings' and pipes'), by good, as
    /// ImportForWant does: a want's whole lack in about ten minutes, less what the Yard brings it, while the company has
    /// gold; and what of its goods is still to come
    /// </summary>
    public List<(string Resource, double PerMinute, double Price, double Left)> MarketForWants(Company company)
    {
        var lines = new Dictionary<string, (double PerMinute, double Left)>();
        foreach (var ask in company.Asks.Where(t => t.Left > 1e-6 && AllowsImports(company, t.Resource)))
        {
            var rate = company.Gold > 0 ? Math.Max(0, ask.Amount / ImportMinutes - ask.Incoming.Values.Sum()) : 0;
            var line = lines.GetValueOrDefault(ask.Resource);
            lines[ask.Resource] = (line.PerMinute + Math.Min(rate, ask.Left), line.Left + ask.Left);
        }
        return lines.OrderBy(t => t.Key, StringComparer.Ordinal)
            .Select(t => (t.Key, t.Value.PerMinute, HubSellPrice(company.District, t.Key), t.Value.Left)).ToList();
    }

    /// <summary>What the goods of a building's next steps are worth at base prices (none below level 11)</summary>
    public double StepGoodsWorth(Building building, int levels)
    {
        var from = BoughtLevel(building);
        var worth = 0.0;
        for (var to = from + 1; to <= Math.Min(from + levels, Content.Progression.MaxBuildingLevel); to++)
        {
            worth += LevelGoods(building, to).Sum(t => t.Value * BasePrice(t.Key));
        }
        return worth;
    }

    /// <summary>Paid steps: those without goods at once, the others as wants that step the building up when they're in</summary>
    private void AddLevelSteps(Company company, Building building, int from, int levels)
    {
        for (var to = from + 1; to <= from + levels; to++)
        {
            var goods = LevelGoods(building, to);
            // Steps from level 11 always take goods, so a step without any never waits behind one that does
            if (goods.Count == 0 && building.Level == to - 1)
            {
                building.Level = to;
                continue;
            }
            foreach (var (resource, amount) in goods)
            {
                company.Asks.Add(new CompanyAsk { Resource = resource, Amount = amount, Site = building.Id, ToLevel = to, Raised = LastTick });
            }
        }
        if (WaitsForLevel(building))
        {
            FillWantsFromStorages(company);
            StepUpIfFull(company, building);
            WakeCoupling();
        }
        company.Touch();
        Changes.Companies.Add(company.Id);
    }

    /// <summary>A step's goods are in: the building steps up, and on while the next steps' goods are in too</summary>
    private void StepUpIfFull(Company owner, Building building)
    {
        var from = building.Level;
        while (true)
        {
            var next = owner.Asks.Where(t => t.Site == building.Id && t.ToLevel == building.Level + 1).ToList();
            if (next.Count == 0 || next.Any(t => t.Left > 1e-6))
            {
                break;
            }
            foreach (var want in next)
            {
                foreach (var (who, units) in want.From)
                {
                    AddStat(owner, who == owner.Id ? "wantOwn" : who == HubSource ? "wantHub" : "wantFriends", units * BasePrice(want.Resource));
                }
            }
            owner.Asks.RemoveAll(t => t.Site == building.Id && t.ToLevel == building.Level + 1);
            building.Level++;
        }
        if (building.Level == from)
        {
            return;
        }
        building.Touch();
        MarkFlowsChanged(building);
        foreach (var tile in building.Footprint())
        {
            Changes.Tiles.Add(tile);
        }
        owner.Touch();
        Changes.Companies.Add(owner.Id);
        if (!WaitsForLevel(building))
        {
            Changes.Events.Add(new WorldEvent(owner.Id, WorldEventKind.ConstructionFinished, $"Your {Content.Building(building.Type).Name} has its goods: level {building.Level}", building.Id, Bell: false));
        }
    }

    /// <summary>Whether the market brings a company's wants of a good (its setting, or a trading post that imports the good)</summary>
    public bool AllowsImports(Company company, string resource) =>
        company.WantsFromMarket || BuildingsOf(company.Id).Any(t => Content.Building(t.Type).Kind == BuildingKind.TradingPost && t.Imports.Contains(resource));

    /// <summary>Whether the market brings what a company's levels and new buildings wait for</summary>
    public CommandResult SetWantsFromMarket(long companyId, bool on)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        company.WantsFromMarket = on;
        company.Touch();
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }

    /// <summary>
    /// What a good for a level or a building costs a company, a unit: its own (what the market would pay it), a
    /// neighbour's (their price), or the market's 2.5×
    /// </summary>
    public double WantGoodPrice(Company company, string resource)
    {
        bool Makes(Company c) => BuildingsOf(c.Id).Any(b => !b.UnderConstruction && Content.Building(b.Type).Out.ContainsKey(resource));
        if (Makes(company))
        {
            return BasePrice(resource);
        }
        return NearPlayers(company).FirstOrDefault(t => t.Id != company.Id && Makes(t)) is { } neighbour
            ? PriceFor(neighbour, company, resource)
            : HubSellPrice(company.District, resource);
    }

    /// <summary>What the goods of a building's next steps cost its owner, as they would come</summary>
    public double StepGoodsCost(Building building, int levels)
    {
        if (Company(building.Owner) is not { } owner)
        {
            return StepGoodsWorth(building, levels);
        }
        var from = BoughtLevel(building);
        var cost = 0.0;
        for (var to = from + 1; to <= Math.Min(from + levels, Content.Progression.MaxBuildingLevel); to++)
        {
            cost += LevelGoods(building, to).Sum(t => t.Value * WantGoodPrice(owner, t.Key));
        }
        return cost;
    }

    /// <summary>
    /// The market's turn in a want (1.4 §9, last after the owner's own goods and the Yard): a good the owner allows imports
    /// of comes at 2.5× base, the whole of it within about ten minutes, as far as its gold goes
    /// </summary>
    private void ImportForWant(Company owner, CompanyAsk ask, double minutes, double brought)
    {
        if (ask.Left <= 1e-6 || owner.Gold <= 0 || !AllowsImports(owner, ask.Resource))
        {
            return;
        }
        var price = HubSellPrice(owner.District, ask.Resource);
        var units = Math.Min(ask.Left, Math.Min(Math.Max(0, ask.Amount / ImportMinutes * minutes - brought), owner.Gold / Math.Max(price, 1e-9)));
        if (units <= 1e-9)
        {
            return;
        }
        var gold = units * price;
        owner.Gold -= gold;
        owner.Report.GoldSpent += gold;
        AddStat(owner, "goldSpent", gold);
        AddStat(owner, "imported", gold);
        ask.Got += units;
        ask.Paid += gold;
        ask.From[HubSource] = ask.From.GetValueOrDefault(HubSource) + units;
        owner.Touch();
    }

    /// <summary>
    /// A building's wants given up (Undo, or the building goes: demolished, evolved, merged into a complex): their goods'
    /// gold back, the owner's own goods back into its Storages; above a level, only the steps above it. The gold back.
    /// </summary>
    private double DropWants(Company owner, long site, int aboveLevel = -1) =>
        GiveBack(owner, owner.Asks.Where(t => t.Site == site && t.ToLevel > aboveLevel).ToList());

    private double GiveBack(Company owner, List<CompanyAsk> asks)
    {
        var gold = 0.0;
        foreach (var ask in asks)
        {
            gold += ask.Paid;
            if (ask.From.GetValueOrDefault(owner.Id) is > 1e-6 and var own)
            {
                PutInStorages(owner.Id, ask.Resource, own);
            }
        }
        owner.Gold += gold;
        AddStat(owner, "goldSpent", -gold);
        owner.Asks.RemoveAll(asks.Contains);
        owner.Touch();
        Changes.Companies.Add(owner.Id);
        return gold;
    }

    /// <summary>
    /// Wants left behind by a building that went or changed are given back (playtest: "Buy the rest" on them said "That has
    /// what it needs."): a gone building's (not one waiting in a crate or a packed plot), a scaffold's materials on one
    /// that isn't a scaffold any more (it was moved: it came up without them), the goods of a level it has already
    /// </summary>
    private void TidyWants(Company company)
    {
        var waiting = company.Crates.Concat(company.Packed?.Buildings ?? []).Select(t => t.OldId).ToHashSet();
        foreach (var site in company.Asks.Where(t => t.Site != null).Select(t => t.Site!.Value).Distinct().ToList())
        {
            if (waiting.Contains(site))
            {
                continue;
            }
            var building = Building(site) is { } b && b.Owner == company.Id ? b : null;
            var stale = company.Asks.Where(t => t.Site == site
                && (building == null || (t.ToLevel == 0 ? !IsSite(building) : t.ToLevel <= building.Level))).ToList();
            if (stale.Count == 0)
            {
                continue;
            }
            var gold = GiveBack(company, stale);
            Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.ConstructionFinished,
                $"Your {(building != null ? Content.Building(building.Type).Name : "building")}'s wants had nothing left to do{(gold >= 1 ? $": +{gold:N0} g back" : "")}", building?.Id, Bell: false));
        }
    }

    /// <summary>The wants of every company, tidied once after a load (wants left behind before the playtest's fix)</summary>
    internal void TidyAllWants()
    {
        foreach (var company in _companies.Values.Where(t => t.Asks.Any(a => a.Site != null)).ToList())
        {
            TidyWants(company);
        }
    }
}

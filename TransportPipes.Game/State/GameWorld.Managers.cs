using TransportPipes.Game.Content;

namespace TransportPipes.Game.State;

/// <summary>
/// Managers (GDD §8.4): hired once with gold into a slot, each with one automation and one bonus. Slots come from the
/// Company Level, the Automation tech, an Engineer's Workshop and a corporation HQ of level 4.
/// </summary>
public sealed partial class GameWorld
{
    public const string EngineerJo = "engineerJo";
    public const string AccountantBea = "accountantBea";
    public const string ForemanRick = "foremanRick";
    public const string TraderLin = "traderLin";
    public const string PlannerSam = "plannerSam";

    /// <summary>Foreman Rick only upgrades what pays back within this time, and keeps this many times the cost</summary>
    private const double RickPaybackMinutes = 120;
    private const double RickReserve = 3;

    public bool HasManager(Company? company, string manager) => company != null && company.Managers.Contains(manager);

    public int ManagerSlots(Company company)
    {
        var slots = Content.Progression.ManagerSlots.Where(t => company.Level >= t.Key).Select(t => t.Value).DefaultIfEmpty(0).Max();
        slots += company.Researched.Sum(t => Content.Research.TryGetValue(t, out var tech) ? tech.ManagerSlots : 0);
        if (HasBuilding(company.Id, BuildingKind.Workshop))
        {
            slots++;
        }
        // Legacy from earlier seasons
        if (company.LegacyLevel >= LegacyManagerLevel)
        {
            slots++;
        }
        return slots;
    }

    public CommandResult HireManager(long companyId, string managerId)
    {
        if (Company(companyId) is not { } company || Content.Managers.FirstOrDefault(t => t.Id == managerId) is not { } manager)
        {
            return CommandResult.Fail("Unknown manager.");
        }
        if (company.Managers.Contains(managerId))
        {
            return CommandResult.Fail($"{manager.Name} works for you already.");
        }
        if (company.Managers.Count >= ManagerSlots(company))
        {
            return CommandResult.Fail("No free manager slot: slots come with Company Levels 10, 20, 30, the Engineer's Workshop and the Automation tech.");
        }
        var payment = Pay(company, manager.Cost, new Dictionary<string, double>());
        if (!payment.Success)
        {
            return payment;
        }
        company.Managers.Add(managerId);
        company.Touch();
        Changes.Companies.Add(company.Id);
        MarkCompanyNetworksDirty(company.Id);
        return CommandResult.Ok;
    }

    /// <summary>Lets a manager go (to make room for another); hiring them again costs again</summary>
    public CommandResult DismissManager(long companyId, string managerId)
    {
        if (Company(companyId) is not { } company || !company.Managers.Remove(managerId))
        {
            return CommandResult.Fail("That manager doesn't work for you.");
        }
        company.Touch();
        Changes.Companies.Add(company.Id);
        MarkCompanyNetworksDirty(company.Id);
        return CommandResult.Ok;
    }

    // ---------------------------------------------------------------- Bonuses

    /// <summary>Engineer Jo: +5% from wind</summary>
    private double ManagerOutputBonus(Company? company, BuildingDefinition building) =>
        building.Modifier == ProductionModifier.Wind && HasManager(company, EngineerJo) ? 0.05 : 0;

    /// <summary>Accountant Bea: +3% hub prices</summary>
    private double ManagerPriceBonus(Company? company) => HasManager(company, AccountantBea) ? 0.03 : 0;

    /// <summary>Foreman Rick: upgrades 5% cheaper</summary>
    private double ManagerUpgradeFactor(Company? company) => HasManager(company, ForemanRick) ? 0.95 : 1;

    /// <summary>Planner Sam: research 10% faster</summary>
    private double ManagerResearchFactor(Company? company) => HasManager(company, PlannerSam) ? 0.9 : 1;

    // ---------------------------------------------------------------- Automations (every market update)

    private void RunManagers()
    {
        foreach (var company in _companies.Values.Where(t => t.Managers.Count != 0).ToList())
        {
            if (HasManager(company, AccountantBea))
            {
                SellOverflow(company);
            }
            if (HasManager(company, ForemanRick))
            {
                UpgradeBestBuilding(company);
            }
        }
    }

    /// <summary>Accountant Bea sells 5% of a full storage's capacity to the hub</summary>
    private void SellOverflow(Company company)
    {
        foreach (var storage in FullStorages(company.Id).ToList())
        {
            var capacity = StorageCapacity(storage, Content.Building(storage.Type));
            var total = storage.StoredTotal;
            var share = capacity * 0.05 / total;
            var gold = 0.0;
            foreach (var resource in storage.Contents.Keys.ToList())
            {
                var amount = storage.Contents[resource] * share;
                storage.Contents[resource] -= amount;
                gold += amount * HubBuyPrice(company, resource);
            }
            company.Gold += gold;
            company.LifetimeEarnings += gold;
            company.Report.GoldEarned += gold;
            storage.Touch();
            Changes.Storages.Add(storage.Id);
            MarkNeedsSolve(storage.Id);
        }
        company.Touch();
        Changes.Companies.Add(company.Id);
    }

    /// <summary>Foreman Rick upgrades the building that pays back soonest, when it pays back within two hours</summary>
    private void UpgradeBestBuilding(Company company)
    {
        Building? best = null;
        var bestPayback = RickPaybackMinutes;
        foreach (var building in BuildingsOf(company.Id))
        {
            var definition = Content.Building(building.Type);
            if (building.UnderConstruction || definition.Out.Count == 0 || building.Level >= Content.Progression.MaxBuildingLevel
                || NetworkOf(building.Id) is not { } network)
            {
                continue;
            }
            // One level more makes about 1/level more of what it makes now
            var rate = network.Solution.Rates.GetValueOrDefault(building.Id);
            var value = definition.Out.Sum(t => t.Value * HubBuyPrice(company, t.Key)) * rate;
            if (value <= 0)
            {
                continue;
            }
            var extra = value / building.Level;
            var cost = UpgradeCost(building, 1);
            var payback = cost / extra;
            if (payback < bestPayback && company.Gold >= cost * RickReserve)
            {
                best = building;
                bestPayback = payback;
            }
        }
        if (best != null && Upgrade(company.Id, best.Id, 1).Success)
        {
            Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.ConstructionFinished, $"Foreman Rick upgraded your {Content.Building(best.Type).Name} to level {best.Level}", best.Id));
        }
    }

    /// <summary>Planner Sam starts the cheapest tech the company can afford</summary>
    private void PlanNextResearch(Company company)
    {
        if (!HasManager(company, PlannerSam) || company.CurrentResearch != null)
        {
            return;
        }
        var next = Content.ResearchList
            .Where(t => !company.Researched.Contains(t.Id) && ResearchBlocker(company, t) == null && ResearchCost(company, t) <= company.ResearchPoints)
            .OrderBy(t => ResearchCost(company, t))
            .FirstOrDefault();
        if (next != null && StartResearch(company.Id, next.Id).Success)
        {
            Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.ResearchFinished, $"Planner Sam started researching {next.Name}"));
        }
    }

    /// <summary>Trader Lin offers a contract that ended well again</summary>
    private void RenewContract(Contract contract)
    {
        if (Company(contract.Proposer) is not { } proposer || !HasManager(proposer, TraderLin) || contract.Fulfillment < 0.9)
        {
            return;
        }
        var selling = contract.Seller == proposer.Id;
        var partner = selling ? contract.Buyer : contract.Seller;
        if (ProposeContract(proposer.Id, partner, selling, contract.Resource, contract.Rate, contract.Price, contract.Days).Success)
        {
            Changes.Events.Add(new WorldEvent(proposer.Id, WorldEventKind.ContractChanged, $"Trader Lin offered the {Content.Resources[contract.Resource].Name.ToLowerInvariant()} contract again"));
        }
    }
}

using TransportPipes.Game.Content;

namespace TransportPipes.Game.State;

/// <summary>A Yard's Monument site (1.4 §13): one per Era of its town; the kind its crew picked, the stages built</summary>
public sealed class Monument
{
    public int Era { get; init; }

    /// <summary>The kind built on the site (null: nobody picked one yet)</summary>
    public string? Kind { get; set; }

    public int Stages { get; set; }

    /// <summary>The gold paid towards the next stage, and what every resident paid in all</summary>
    public double Paid { get; set; }
    public Dictionary<long, double> PaidBy { get; set; } = new();
}

/// <summary>
/// The Monuments (1.4 §13): every Era gives each Yard a Monument site in its square, and its crew picks which of five to
/// build there. A Monument is built with gold by any resident, in stages without end, each 40% dearer than the last;
/// every stage makes every resident a little better. Four friends pay for one Monument and all four get it, and a stage
/// costs a lone resident what it costs a crew (rule 2).
/// </summary>
public sealed partial class GameWorld
{
    /// <summary>A Yard's Monument sites, one for every Era its town reached</summary>
    public List<Monument> MonumentsOf(Crew crew)
    {
        var era = TownEra(crew.Yard.District);
        for (var site = crew.Monuments.Count + 1; site <= era; site++)
        {
            crew.Monuments.Add(new Monument { Era = site });
        }
        return crew.Monuments;
    }

    public MonumentKind? MonumentKind(string? id) => Content.Monuments.Kinds.FirstOrDefault(t => t.Id == id);

    /// <summary>What a site's next stage costs</summary>
    public double MonumentStageCost(Monument monument) => Content.Monuments.StageCost(monument.Era, monument.Stages + 1);

    /// <summary>Whether a site's kind still gives its perk for another stage</summary>
    public bool MonumentCapped(Monument monument) => MonumentKind(monument.Kind) is { Cap: > 0 } kind && monument.Stages >= kind.Cap;

    /// <summary>A resident picks the kind for a site that has none yet (each kind once per Yard)</summary>
    public CommandResult ChooseMonument(long companyId, int era, string kind)
    {
        if (Company(companyId) is not { } company || CrewOf(company) is not { } crew)
        {
            return CommandResult.Fail("Only a Yard's residents build its Monuments.");
        }
        if (MonumentKind(kind) is not { } definition)
        {
            return CommandResult.Fail("Unknown Monument.");
        }
        var sites = MonumentsOf(crew);
        if (sites.FirstOrDefault(t => t.Era == era) is not { } site)
        {
            return CommandResult.Fail($"Your Yard gets that site with Era {era}.");
        }
        if (site.Kind != null)
        {
            return CommandResult.Fail($"Your Yard builds its {MonumentKind(site.Kind)?.Name} there.");
        }
        if (sites.Any(t => t.Kind == kind))
        {
            return CommandResult.Fail($"Your Yard has a {definition.Name} already.");
        }
        site.Kind = kind;
        CrewChanged(crew);
        foreach (var mate in CrewMembers(crew).Where(t => t.Id != company.Id))
        {
            Changes.Events.Add(new WorldEvent(mate.Id, WorldEventKind.Neighbour, $"{NameFor(company, mate)} chose a {definition.Name} for your Yard's Era {era} site"));
        }
        return CommandResult.Ok;
    }

    /// <summary>A resident pays gold into a site's Monument: every stage it completes is built at once</summary>
    public CommandResult PayMonument(long companyId, int era, double gold)
    {
        if (Company(companyId) is not { } company || CrewOf(company) is not { } crew)
        {
            return CommandResult.Fail("Only a Yard's residents build its Monuments.");
        }
        if (company.Level < Content.Monuments.Level)
        {
            return CommandResult.Fail($"Monuments need Company Level {Content.Monuments.Level}.");
        }
        if (MonumentsOf(crew).FirstOrDefault(t => t.Era == era) is not { } site)
        {
            return CommandResult.Fail($"Your Yard gets that site with Era {era}.");
        }
        if (MonumentKind(site.Kind) is not { } kind)
        {
            return CommandResult.Fail("Choose what to build there first.");
        }
        if (MonumentCapped(site))
        {
            return CommandResult.Fail($"Your {kind.Name} gives all it can.");
        }
        gold = Math.Min(Math.Floor(gold), company.Gold);
        if (gold < 1)
        {
            return CommandResult.Fail("You have no gold to give.");
        }
        company.Gold -= gold;
        company.Report.GoldSpent += gold;
        AddStat(company, "goldSpent", gold);
        AddStat(company, "monumentGold", gold);
        site.PaidBy[company.Id] = site.PaidBy.GetValueOrDefault(company.Id) + gold;
        site.Paid += gold;
        var built = 0;
        while (!MonumentCapped(site) && site.Paid >= MonumentStageCost(site) - 1e-6)
        {
            site.Paid -= MonumentStageCost(site);
            site.Stages++;
            built++;
        }
        if (MonumentCapped(site) && site.Paid > 0)
        {
            // Nothing more to build: what's over comes back
            var back = Math.Min(site.Paid, gold);
            company.Gold += back;
            AddStat(company, "goldSpent", -back);
            site.PaidBy[company.Id] -= back;
            site.Paid -= back;
        }
        company.Touch();
        Changes.Companies.Add(company.Id);
        CrewChanged(crew);
        if (built > 0)
        {
            foreach (var resident in CrewMembers(crew))
            {
                Changes.Events.Add(new WorldEvent(resident.Id, WorldEventKind.Wonder, $"Your Yard's {kind.Name} reached stage {site.Stages}: {MonumentPerkText(kind, site.Stages)} for every resident"));
                MarkCompanyNetworksDirty(resident.Id);
            }
            Changes.Markets.Add(crew.Yard.District);
        }
        return CommandResult.Ok;
    }

    private static string MonumentPerkText(MonumentKind kind, int stages)
    {
        var amount = kind.Amount * (kind.Cap > 0 ? Math.Min(stages, kind.Cap) : stages);
        return kind.Perk switch
        {
            MonumentPerk.Production => $"+{amount:P0} production",
            MonumentPerk.HubOrders => $"Hub Orders +{amount:P0}",
            MonumentPerk.Research => $"research {amount:P0} faster",
            MonumentPerk.Cheaper => $"buildings, levels and parcels {amount:P0} cheaper",
            _ => $"+{amount:P0} on what you sell in the Yard and on links",
        };
    }

    /// <summary>What a company's Yard Monuments give it of a perk (stages × amount, each kind up to its cap)</summary>
    public double MonumentPerkOf(Company? company, MonumentPerk perk)
    {
        if (company == null || CrewOf(company) is not { } crew)
        {
            return 0;
        }
        var total = 0.0;
        foreach (var site in crew.Monuments)
        {
            if (MonumentKind(site.Kind) is { } kind && kind.Perk == perk)
            {
                total += kind.Amount * (kind.Cap > 0 ? Math.Min(site.Stages, kind.Cap) : site.Stages);
            }
        }
        return total;
    }

    /// <summary>The Grand Fountain: more production</summary>
    private double MonumentProductionFactor(Company? company) => 1 + MonumentPerkOf(company, MonumentPerk.Production);

    /// <summary>The Clock Tower: bigger Hub Orders</summary>
    private double MonumentHubOrderFactor(Company company) => 1 + MonumentPerkOf(company, MonumentPerk.HubOrders);

    /// <summary>The Observatory: research takes less time</summary>
    private double MonumentResearchFactor(Company company) => 1 - MonumentPerkOf(company, MonumentPerk.Research);

    /// <summary>The Workshop: buildings, levels and parcels cost less</summary>
    public double CostFactor(Company? company) => 1 - MonumentPerkOf(company, MonumentPerk.Cheaper);

    /// <summary>The Lighthouse: what a resident sells in the Yard and on links pays this much on top, paid by the market</summary>
    public double LighthouseBonus(Company company) => MonumentPerkOf(company, MonumentPerk.YardSales);
}

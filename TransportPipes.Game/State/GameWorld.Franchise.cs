using TransportPipes.Game.Content;

namespace TransportPipes.Game.State;

/// <summary>
/// The Franchise (GDD §8.7): from Company Level 25 in Era 2 a company sells itself to the Board for Shares, each +1%
/// production for the rest of the season. Land, decorations, corporation, managers, outposts and hub reputation stay;
/// buildings, levels, gold and three quarters of the research go. Partners get 24 hours of notice.
/// </summary>
public sealed partial class GameWorld
{
    public const int FranchiseLevel = 25;
    public const int FranchiseEra = 2;
    public static readonly TimeSpan FranchiseNotice = TimeSpan.FromHours(24);

    /// <summary>The Shares a franchise would give now: the square root of what was earned since the last one, in thousands</summary>
    public int FranchiseShares(Company company) =>
        (int)Math.Floor(Math.Sqrt(Math.Max(0, company.LifetimeEarnings - company.EarningsAtFranchise) / 1000));

    public string? FranchiseBlocker(Company company)
    {
        if (company.Level < FranchiseLevel)
        {
            return $"A franchise needs Company Level {FranchiseLevel}.";
        }
        if (Era < FranchiseEra)
        {
            return $"Franchises start in Era {FranchiseEra}.";
        }
        if (FranchiseShares(company) < 1)
        {
            return "Earn more first: a franchise would give no Shares.";
        }
        return null;
    }

    /// <summary>Announces a franchise: it happens after 24 hours, so partners can prepare</summary>
    public CommandResult StartFranchise(long companyId)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (company.FranchiseAt != null)
        {
            return CommandResult.Fail("Your franchise is announced already.");
        }
        if (FranchiseBlocker(company) is { } blocker)
        {
            return CommandResult.Fail(blocker);
        }
        company.FranchiseAt = Now + FranchiseNotice;
        company.Touch();
        Changes.Companies.Add(company.Id);
        foreach (var partner in PartnersOf(company.Id))
        {
            Changes.Events.Add(new WorldEvent(partner, WorldEventKind.ContractChanged, $"{company.Name} franchises in 24 hours: its buildings, trade and contracts stop then"));
        }
        return CommandResult.Ok;
    }

    public CommandResult CancelFranchise(long companyId)
    {
        if (Company(companyId) is not { FranchiseAt: not null } company)
        {
            return CommandResult.Fail("There is no franchise to cancel.");
        }
        company.FranchiseAt = null;
        company.Touch();
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }

    /// <summary>The companies that trade with a company right now (both ways)</summary>
    private HashSet<long> PartnersOf(long companyId)
    {
        var partners = new HashSet<long>();
        foreach (var network in _networks.Values)
        {
            foreach (var flow in network.Solution.Flows.Where(t => t.Kind is Simulation.FlowKind.Trade or Simulation.FlowKind.Contract))
            {
                var from = _buildings[flow.From].Owner;
                var to = _buildings[flow.To].Owner;
                if (from == companyId && to != companyId)
                {
                    partners.Add(to);
                }
                if (to == companyId && from != companyId)
                {
                    partners.Add(from);
                }
            }
        }
        return partners;
    }

    private IEnumerable<(DateTime At, Action Run)> FranchiseMoments(DateTime from, DateTime to)
    {
        foreach (var company in _companies.Values.Where(t => t.FranchiseAt is { } at && at <= to).ToList())
        {
            yield return (company.FranchiseAt!.Value > from ? company.FranchiseAt.Value : from, () => Franchise(company));
        }
    }

    /// <summary>The franchise itself: Shares in, the business back to the start</summary>
    private void Franchise(Company company)
    {
        if (company.FranchiseAt == null)
        {
            return;
        }
        company.FranchiseAt = null;
        var shares = FranchiseShares(company);
        company.Shares += shares;
        company.Franchises++;
        company.EarningsAtFranchise = company.LifetimeEarnings;

        // Buildings go (but the HQ, the trading post and decorations stay), so do contracts and Exchange orders
        foreach (var building in BuildingsOf(company.Id).ToList())
        {
            if (Content.Building(building.Type).Kind is not (BuildingKind.Hq or BuildingKind.TradingPost or BuildingKind.Decoration))
            {
                RemoveBuilding(building);
            }
            else if (building.Level > 1)
            {
                building.Level = 1;
                building.Touch();
                MarkDirty(building);
            }
        }
        foreach (var contract in _contracts.Values.Where(t => t.IsOpen && (t.Seller == company.Id || t.Buyer == company.Id)).ToList())
        {
            contract.State = ContractState.Ended;
            ContractChanged(contract);
        }
        foreach (var order in _orders.Where(t => t.Company == company.Id).ToList())
        {
            Refund(order);
            _orders.Remove(order);
            Changes.Exchange = true;
        }

        // A quarter of the research stays: the cheapest techs
        var keep = company.Researched
            .Select(t => Content.Research.TryGetValue(t, out var tech) ? tech : null)
            .OfType<ResearchDefinition>()
            .OrderBy(t => t.Cost)
            .Take(company.Researched.Count / 4)
            .Select(t => t.Id)
            .ToList();
        company.Researched.Clear();
        company.Researched.UnionWith(keep);
        company.CurrentResearch = null;
        company.ResearchEndsAt = null;
        company.ResearchPoints = 0;

        company.Gold = StartingGold;
        company.Level = 1;
        company.Xp = 0;
        company.BuiltTypes.Clear();
        company.Touch();
        Changes.Companies.Add(company.Id);
        MarkCompanyNetworksDirty(company.Id);
        Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Achievement, $"Franchised! +{shares} Shares: everything you build makes {company.Shares}% more"));
    }

    /// <summary>Shares: +1% production each</summary>
    private static double ShareBonus(Company? company) => company == null ? 0 : company.Shares * 0.01;
}

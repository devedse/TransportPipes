using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>
/// The Franchise (GDD §8.7, changed for v1): from Company Level 25 in Era 2 a company cashes in its Shares (each +1%
/// production, for good) and opens a new estate, a branch in another district that starts small and grows like the
/// first. Nothing is torn down: expansion instead of a reset.
/// </summary>
public sealed partial class GameWorld
{
    public const int FranchiseLevel = 25;
    public const int FranchiseEra = 2;

    /// <summary>Shares a franchise needs at least (so it is a milestone, not a click)</summary>
    public const int FranchiseMinShares = 10;

    /// <summary>The first estate and two branches</summary>
    public const int MaxEstates = 3;

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
        if (EstatesOf(company).Count() >= MaxEstates)
        {
            return $"You run {MaxEstates} estates already.";
        }
        if (EstateOf(company) == null)
        {
            return "Settle first.";
        }
        var shares = FranchiseShares(company);
        if (shares < FranchiseMinShares)
        {
            return $"Earn more first: {shares} of the {FranchiseMinShares} Shares a franchise needs.";
        }
        return null;
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

    /// <summary>
    /// Opens a franchise: the Shares go in and a new estate opens, by default in the newest district with room (the
    /// frontier, where veterans bring demand to newcomers)
    /// </summary>
    public CommandResult OpenFranchise(long companyId, DistrictCoord? district = null)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (FranchiseBlocker(company) is { } blocker)
        {
            return CommandResult.Fail(blocker);
        }
        var taken = EstatesOf(company).Select(t => t.District).ToHashSet();
        Estate? estate;
        if (district is { } chosen)
        {
            if (taken.Contains(chosen))
            {
                return CommandResult.Fail("Open your franchise in a district where you have no estate yet.");
            }
            estate = FreeEstateIn(chosen, company);
            if (estate == null)
            {
                return CommandResult.Fail("There is no free estate in that district.");
            }
        }
        else
        {
            estate = FrontierEstate(company, taken);
        }

        var shares = FranchiseShares(company);
        company.Shares += shares;
        company.Franchises++;
        company.EarningsAtFranchise = company.LifetimeEarnings;
        SettleBranch(company, estate);
        company.Touch();
        Changes.Companies.Add(company.Id);
        MarkCompanyNetworksDirty(company.Id);
        Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Achievement,
            $"Franchised! +{shares} Shares (everything you build makes {company.Shares}% more), and a new estate in {WorldNames.District(estate.District)}"));
        return CommandResult.Ok;
    }

    /// <summary>A free estate in the newest district with room that the company isn't in yet</summary>
    private Estate FrontierEstate(Company company, HashSet<DistrictCoord> taken)
    {
        foreach (var district in _openDistricts.Reverse().Where(t => !taken.Contains(t)))
        {
            if (FreeEstateIn(district, company) is { } free)
            {
                return free;
            }
        }
        OpenNextDistrict();
        return FrontierEstate(company, taken);
    }

    /// <summary>A branch estate: the start parcels, gates and a trading post (the HQ stays at the first estate)</summary>
    private void SettleBranch(Company company, Estate estate)
    {
        if (estate.OwnerId is { } tenant && Company(tenant) is { IsNpc: true })
        {
            EvictTenant(estate);
        }
        estate.OwnerId = company.Id;
        estate.UnlockedParcels = Content.Progression.StartParcels.Aggregate(0, (mask, parcel) => mask | (1 << parcel));
        estate.ReservedFor = null;
        estate.ReservedUntil = null;
        estate.Touch();
        Changes.Estates.Add((estate.District, estate.Slot));
        company.Branches.Add(new EstateRef(estate.District.X, estate.District.Y, estate.Slot));
        OpenDistrict(estate.District);
        foreach (var side in Enum.GetValues<GateSide>())
        {
            var pos = WorldLayout.GatePosition(estate.District, estate.Slot, side);
            _conductors[pos] = new Conductor { Pos = pos, Kind = ConductorKind.Gate, Owner = company.Id, Side = side };
            MarkDirty(pos);
            Changes.Tiles.Add(pos);
        }
        AddBuilding(company, "tradingPost", estate.Origin.Offset(TradingPostLocal.X, TradingPostLocal.Y), finished: true);
    }

    /// <summary>Every estate of a company: the first one and its branches</summary>
    public IEnumerable<Estate> EstatesOf(Company company)
    {
        if (EstateOf(company) is { } main)
        {
            yield return main;
        }
        foreach (var branch in company.Branches)
        {
            if (Estate(new DistrictCoord(branch.DistrictX, branch.DistrictY), branch.Slot) is { } estate && estate.OwnerId == company.Id)
            {
                yield return estate;
            }
        }
    }

    /// <summary>The company's estate a tile belongs to (the Deep Level counts as the first estate), or null</summary>
    public Estate? OwnEstateAt(Company company, TilePos pos)
    {
        if (WorldLayout.IsDeep(pos))
        {
            return EstateOf(company) is { } main && main.Contains(WorldLayout.Surface(pos)) ? main : null;
        }
        return EstatesOf(company).FirstOrDefault(t => t.Contains(pos));
    }

    /// <summary>Shares: +1% production each</summary>
    private static double ShareBonus(Company? company) => company == null ? 0 : company.Shares * 0.01;
}

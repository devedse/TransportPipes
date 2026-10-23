using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

public enum Activity
{
    Active,
    Resting,
    Dormant,
    Asleep,
}

/// <summary>
/// Inactive players, moving and space in the world (GDD §12). Nothing anyone built is lost: estates are packed up for
/// a move or for the Quiet Isles and unpacked again with their layout; what doesn't fit the new land goes into crates.
/// </summary>
public sealed partial class GameWorld
{
    public static readonly TimeSpan RestingAfter = TimeSpan.FromDays(3);
    public static readonly TimeSpan DormantAfter = TimeSpan.FromDays(14);
    public static readonly TimeSpan AsleepAfter = TimeSpan.FromDays(30);
    /// <summary>How long a company waits between moves, and how long its estate is in the air (content: together)</summary>
    public TimeSpan MoveEvery => TimeSpan.FromMinutes(Content.Progression.Together.MoveCooldownMinutes);
    public TimeSpan MoveTime => TimeSpan.FromMinutes(Content.Progression.Together.MoveFlightMinutes);
    public static readonly TimeSpan BlockTime = TimeSpan.FromHours(72);
    private const int BlockMinMembers = 5;
    private readonly Dictionary<DistrictCoord, int> _quietWeeks = new();

    public Activity ActivityOf(Company company)
    {
        if (company.Asleep)
        {
            return Activity.Asleep;
        }
        var away = LastTick - company.LastSeenAt;
        return away >= DormantAfter ? Activity.Dormant : away >= RestingAfter ? Activity.Resting : Activity.Active;
    }

    /// <summary>What a move costs (content: together; free moves are free)</summary>
    public double MoveCost(Company company) => company.FreeMoves > 0 ? 0 : Content.Progression.Together.MoveCostPerLevel * company.Level;

    // ---------------------------------------------------------------- Packing

    /// <summary>Packs an estate: its buildings, pipes, parcels and gate policies; the land becomes free</summary>
    private void Pack(Company company)
    {
        if (EstateOf(company) is not { } estate)
        {
            return;
        }
        var packed = new PackedEstate { UnlockedParcels = estate.UnlockedParcels };
        foreach (var building in BuildingsOf(company.Id).Where(t => InEstateOrBelow(estate, t.Position)).ToList())
        {
            // A lift's shaft comes back with its lift
            if (Content.Building(building.Type).Buildable)
            {
                packed.Buildings.Add(PackBuilding(building, estate.Origin));
            }
            RemoveBuilding(building);
        }
        foreach (var conductor in _conductors.Values.Where(t => t.Owner == company.Id && InEstateOrBelow(estate, t.Pos)).ToList())
        {
            if (conductor.Kind == ConductorKind.Pipe)
            {
                packed.Pipes.Add(new PackedPipe(conductor.Pos.X - estate.Origin.X, conductor.Pos.Y - estate.Origin.Y, conductor.Tier));
            }
            _conductors.Remove(conductor.Pos);
            Deleted.Add(conductor);
            MarkDirty(conductor.Pos);
            Changes.Tiles.Add(conductor.Pos);
        }
        RemoveYardGate(estate);
        estate.OwnerId = null;
        estate.UnlockedParcels = 0;
        estate.Touch();
        Changes.Estates.Add((estate.District, estate.Slot));
        company.Packed = packed;
        company.Slot = -1;
        company.Touch();
        Changes.Companies.Add(company.Id);
        MarkCompanyNetworksDirty(company.Id);
    }

    private static PackedBuilding PackBuilding(Building building, TilePos origin) => new()
    {
        Type = building.Type,
        Dx = building.X - origin.X,
        Dy = building.Y - origin.Y,
        Level = building.Level,
        Contents = new(building.Contents),
        Imports = new(building.Imports),
        Filter = new(building.Filter),
        Setting = building.Setting,
    };

    /// <summary>Unpacks onto an estate: the same layout where the land allows it, the rest in crates</summary>
    private void Unpack(Company company, Estate estate)
    {
        var packed = company.Packed ?? new PackedEstate();
        estate.OwnerId = company.Id;
        estate.UnlockedParcels = packed.UnlockedParcels != 0 ? packed.UnlockedParcels : Content.Progression.StartParcels.Aggregate(0, (mask, parcel) => mask | (1 << parcel));
        estate.ReservedFor = null;
        estate.ReservedUntil = null;
        if (company.MergeBonus)
        {
            // A free parcel for moving away from a quiet district
            company.MergeBonus = false;
            var next = Enumerable.Range(0, WorldLayout.ParcelCount).FirstOrDefault(t => !estate.IsUnlocked(t), -1);
            if (next >= 0)
            {
                estate.UnlockedParcels |= 1 << next;
            }
        }
        estate.Touch();
        Changes.Estates.Add((estate.District, estate.Slot));
        company.District = estate.District;
        company.Slot = estate.Slot;
        company.Packed = null;
        company.ArrivesAt = null;
        OpenDistrict(estate.District);

        AddYardGate(company, estate);
        // HQ and trading post first: they always fit
        // The Deep Level after the surface: the lifts' shafts first
        foreach (var item in packed.Buildings.OrderBy(t => Content.Building(t.Type).Kind is BuildingKind.Hq or BuildingKind.TradingPost ? 0 : 1)
            .ThenBy(t => t.Dy >= WorldLayout.DeepOffset / 2 ? 1 : 0))
        {
            var pos = estate.Origin.Offset(item.Dx, item.Dy);
            var definition = Content.Building(item.Type);
            var free = Enumerable.Range(0, definition.Size * definition.Size).All(i => !_buildingAt.ContainsKey(pos.Offset(i % definition.Size, i / definition.Size)));
            if (definition.Kind is BuildingKind.Hq or BuildingKind.TradingPost && free || CanPlace(company, definition, pos).Success)
            {
                Restore(company, item, pos);
            }
            else
            {
                company.Crates.Add(item);
            }
        }
        // The HQ isn't packed (nobody builds one), the trading post is: only what's missing is added, never a second
        // trading post on the first one's tile (its networks would never settle)
        if (!BuildingsOf(company.Id).Any(t => Content.Building(t.Type).Kind == BuildingKind.Hq))
        {
            AddBuilding(company, "hq", estate.Origin.Offset(HqLocal.X, HqLocal.Y), finished: true);
        }
        var postAt = estate.Origin.Offset(TradingPostLocal.X, TradingPostLocal.Y);
        if (!BuildingsOf(company.Id).Any(t => Content.Building(t.Type).Kind == BuildingKind.TradingPost) && !IsOccupied(postAt))
        {
            AddBuilding(company, "tradingPost", postAt, finished: true);
        }
        foreach (var pipe in packed.Pipes)
        {
            var pos = estate.Origin.Offset(pipe.Dx, pipe.Dy);
            if (!IsOccupied(pos) && Tile(pos).Terrain is not (TerrainType.Mountain or TerrainType.Rock))
            {
                _conductors[pos] = new Conductor { Pos = pos, Tier = pipe.Tier, Owner = company.Id };
                MarkDirty(pos);
                Changes.Tiles.Add(pos);
            }
        }
        company.Touch();
        Changes.Companies.Add(company.Id);
        MarkCompanyNetworksDirty(company.Id);
    }

    private Building Restore(Company company, PackedBuilding item, TilePos pos)
    {
        var building = AddBuilding(company, item.Type, pos, finished: true);
        building.Level = item.Level;
        foreach (var (resource, amount) in item.Contents)
        {
            building.Contents[resource] = amount;
        }
        building.Imports.UnionWith(item.Imports);
        building.Filter.UnionWith(item.Filter);
        building.Setting = item.Setting;
        EnsureShaft(building);
        return building;
    }

    /// <summary>A free estate in a district for a company (reserved ones only for their corporation); null when none</summary>
    private Estate? FreeEstateIn(DistrictCoord district, Company company)
    {
        OpenDistrict(district);
        Estate? tenantEstate = null;
        for (var slot = 0; slot < WorldLayout.EstatesPerDistrict; slot++)
        {
            var estate = _estates[(district, slot)];
            if (estate.ReservedFor is { } corporation && estate.ReservedUntil > LastTick && corporation != company.CorporationId)
            {
                continue;
            }
            if (estate.OwnerId == null)
            {
                return estate;
            }
            if (tenantEstate == null && estate.OwnerId is { } owner && Company(owner) is { IsNpc: true })
            {
                tenantEstate = estate;
            }
        }
        if (tenantEstate != null)
        {
            EvictTenant(tenantEstate);
        }
        return tenantEstate;
    }

    // ---------------------------------------------------------------- Moving

    public string? MoveBlocker(Company company, DistrictCoord district)
    {
        if (company.Packed != null)
        {
            return "Your estate is on its way already.";
        }
        if (company.LastMovedAt is { } last && last + MoveEvery > LastTick && company.FreeMoves == 0)
        {
            return $"You can move again in {Math.Ceiling((last + MoveEvery - LastTick).TotalMinutes):N0} minutes.";
        }
        if (district == company.District)
        {
            return "You live there already.";
        }
        if (!_openDistricts.Contains(district))
        {
            return "That district isn't open.";
        }
        if (company.Gold < MoveCost(company))
        {
            return $"A move costs {MoveCost(company):N0} gold.";
        }
        return null;
    }

    /// <summary>Moves the whole estate to another district: balloons lift it, an hour later it lands</summary>
    public CommandResult Relocate(long companyId, DistrictCoord district)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (MoveBlocker(company, district) is { } blocker)
        {
            return CommandResult.Fail(blocker);
        }
        if (FreeEstateIn(district, company) is not { } target)
        {
            return CommandResult.Fail("That district has no free estate.");
        }
        var cost = MoveCost(company);
        if (company.FreeMoves > 0)
        {
            company.FreeMoves--;
        }
        else
        {
            company.Gold -= cost;
            company.Report.GoldSpent += cost;
            AddStat(company, "goldSpent", cost);
        }
        EndTradeOf(company);
        Pack(company);
        // The new spot is kept for the company while it travels
        target.OwnerId = company.Id;
        target.Touch();
        Changes.Estates.Add((target.District, target.Slot));
        company.District = target.District;
        company.ArrivesAt = LastTick + MoveTime;
        company.LastMovedAt = LastTick;
        _arrivals[company.Id] = target.Slot;
        return CommandResult.Ok;
    }

    /// <summary>Where moving companies land (the slot in their new district)</summary>
    private readonly Dictionary<long, int> _arrivals = new();

    private IEnumerable<(DateTime At, Action Run)> ArrivalMoments(DateTime from, DateTime to)
    {
        foreach (var company in _companies.Values.Where(t => t.ArrivesAt is { } at && at <= to).ToList())
        {
            yield return (company.ArrivesAt!.Value > from ? company.ArrivesAt.Value : from, () => Arrive(company));
        }
    }

    private void Arrive(Company company)
    {
        if (company.ArrivesAt == null)
        {
            return;
        }
        var slot = _arrivals.Remove(company.Id, out var reserved) ? reserved
            : Enumerable.Range(0, WorldLayout.EstatesPerDistrict).FirstOrDefault(t => Estate(company.District, t)?.OwnerId == company.Id, -1);
        var estate = slot >= 0 ? Estate(company.District, slot) : null;
        if (estate == null || (estate.OwnerId != null && estate.OwnerId != company.Id))
        {
            estate = FindFreeEstate();
        }
        Unpack(company, estate);
        RerouteLinks(company);
        Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Neighbour, company.Crates.Count == 0
            ? "Your estate has landed in its new spot"
            : $"Your estate has landed; {company.Crates.Count} buildings didn't fit and wait in crates"));
    }

    /// <summary>Ends what ties a company to its neighbours: contracts and Exchange orders</summary>
    private void EndTradeOf(Company company)
    {
        // A contract that runs over a link goes on (the link takes the new way); one through the Yard ends
        foreach (var contract in _contracts.Values.Where(t => t.IsOpen && (t.Seller == company.Id || t.Buyer == company.Id)
                     && LinkBetween(t.Seller, t.Buyer, t.Resource) == null).ToList())
        {
            contract.State = ContractState.Ended;
            ContractChanged(contract);
            var partner = contract.Seller == company.Id ? contract.Buyer : contract.Seller;
            Changes.Events.Add(new WorldEvent(partner, WorldEventKind.ContractChanged, $"Your contract with {company.Name} ended: they moved away"));
        }
        foreach (var order in _orders.Where(t => t.Company == company.Id).ToList())
        {
            Refund(order);
            _orders.Remove(order);
            Changes.Exchange = true;
        }
    }

    // ---------------------------------------------------------------- Crates

    public CommandResult PlaceCrate(long companyId, int crate, TilePos pos)
    {
        if (Company(companyId) is not { } company || crate < 0 || crate >= company.Crates.Count)
        {
            return CommandResult.Fail("Unknown crate.");
        }
        var item = company.Crates[crate];
        var placement = CanPlace(company, Content.Building(item.Type), pos);
        if (!placement.Success)
        {
            return placement;
        }
        company.Crates.RemoveAt(crate);
        Restore(company, item, pos);
        company.Touch();
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }

    /// <summary>Scraps a crate for 75% of what the building cost</summary>
    public CommandResult ScrapCrate(long companyId, int crate)
    {
        if (Company(companyId) is not { } company || crate < 0 || crate >= company.Crates.Count)
        {
            return CommandResult.Fail("Unknown crate.");
        }
        var item = company.Crates[crate];
        company.Crates.RemoveAt(crate);
        company.Gold += Math.Round(Content.Building(item.Type).Cost.Gold * 0.75);
        company.Touch();
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }

    // ---------------------------------------------------------------- Swaps

    /// <summary>Proposes to swap estates; when the other company proposed it too, they swap right away</summary>
    public CommandResult ProposeSwap(long companyId, long otherId)
    {
        if (Company(companyId) is not { } company || Company(otherId) is not { IsNpc: false } other || other.Id == company.Id)
        {
            return CommandResult.Fail("Choose another player.");
        }
        if (EstateOf(company) is not { } mine || EstateOf(other) is not { } theirs)
        {
            return CommandResult.Fail("Both estates have to be in place.");
        }
        if (other.SwapWith == company.Id)
        {
            other.SwapWith = null;
            company.SwapWith = null;
            EndTradeOf(company);
            EndTradeOf(other);
            Pack(company);
            Pack(other);
            Unpack(company, theirs);
            Unpack(other, mine);
            company.LastMovedAt = other.LastMovedAt = LastTick;
            Changes.Events.Add(new WorldEvent(other.Id, WorldEventKind.Neighbour, $"You swapped estates with {company.Name}"));
            Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Neighbour, $"You swapped estates with {other.Name}"));
            return CommandResult.Ok;
        }
        company.SwapWith = other.Id;
        company.Touch();
        Changes.Events.Add(new WorldEvent(other.Id, WorldEventKind.Neighbour, $"{company.Name} proposes to swap estates with you: visit them to accept"));
        return CommandResult.Ok;
    }

    // ---------------------------------------------------------------- Corporation blocks

    /// <summary>A corporation of five or more keeps free estates of one district for its members for 72 hours</summary>
    public CommandResult ReserveBlock(long officerId, DistrictCoord district)
    {
        if (Company(officerId) is not { Role: CorporationRole.Leader or CorporationRole.Officer } officer || CorporationOf(officer) is not { } corporation)
        {
            return CommandResult.Fail("Officers reserve blocks.");
        }
        var members = Members(corporation).Count();
        if (members < BlockMinMembers)
        {
            return CommandResult.Fail($"A corporation block needs {BlockMinMembers} members.");
        }
        OpenDistrict(district);
        var free = Enumerable.Range(0, WorldLayout.EstatesPerDistrict)
            .Select(t => _estates[(district, t)])
            .Where(t => t.OwnerId == null && (t.ReservedFor == null || t.ReservedUntil <= LastTick))
            .Take(members)
            .ToList();
        if (free.Count == 0)
        {
            return CommandResult.Fail("That district has no free estates.");
        }
        foreach (var estate in free)
        {
            estate.ReservedFor = corporation.Id;
            estate.ReservedUntil = LastTick + BlockTime;
            estate.Touch();
            Changes.Estates.Add((estate.District, estate.Slot));
        }
        foreach (var member in Members(corporation))
        {
            Changes.Events.Add(new WorldEvent(member.Id, WorldEventKind.Corporation, $"{corporation.Name} keeps {free.Count} estates in {WorldNames.District(district)} for you for 72 hours: move in together"));
        }
        return CommandResult.Ok;
    }

    // ---------------------------------------------------------------- Activity

    /// <summary>Once a day: dormant leaders hand over, the long gone go to sleep on the Quiet Isles</summary>
    private void CheckActivity()
    {
        foreach (var company in _companies.Values.Where(t => !t.IsNpc && !t.Asleep).ToList())
        {
            var activity = ActivityOf(company);
            if (activity >= Activity.Dormant && company.Role == CorporationRole.Leader && CorporationOf(company) is { } corporation)
            {
                var next = Members(corporation).Where(t => t.Id != company.Id && ActivityOf(t) == Activity.Active)
                    .OrderByDescending(t => t.Role).ThenByDescending(t => t.LastSeenAt).FirstOrDefault();
                if (next != null)
                {
                    next.Role = CorporationRole.Leader;
                    company.Role = CorporationRole.Officer;
                    next.Touch();
                    company.Touch();
                    CorporationChanged(corporation);
                    Changes.Events.Add(new WorldEvent(next.Id, WorldEventKind.Corporation, $"You lead {corporation.Name} now: {company.Name} has been away for two weeks"));
                }
            }
            var away = LastTick - company.LastSeenAt;
            if (away >= StandInNoticeAfter && !company.StandInAnnounced && company.Packed == null)
            {
                AnnounceStandIn(company);
            }
            else if (away < StandInNoticeAfter && company.StandInAnnounced)
            {
                company.StandInAnnounced = false;
                company.Touch();
            }
            if (away >= AsleepAfter && company.Packed == null)
            {
                Sleep(company);
            }
        }
        CheckAsleepHolds();
    }

    /// <summary>For testing: to the Quiet Isles right away</summary>
    public void PutToSleep(Company company) => Sleep(company);

    /// <summary>For testing: a moving estate lands right away</summary>
    public void LandNow(Company company)
    {
        if (company.ArrivesAt != null)
        {
            Arrive(company);
        }
    }

    /// <summary>
    /// Asleep in place (v1.2 §10): the estate stays where it is and makes nothing, a stand-in House keeps its trades going
    /// (contracts and Exchange orders end); on day 90 it is packed for the Quiet Isles
    /// </summary>
    private void Sleep(Company company)
    {
        company.StandIn = NewStandIn(company);
        EndTradeOf(company);
        company.Asleep = true;
        company.Touch();
        Changes.Companies.Add(company.Id);
        MarkCompanyNetworksDirty(company.Id);
        WakeCoupling();
        foreach (var partner in PartnersOf(company))
        {
            Changes.Events.Add(new WorldEvent(partner.Id, WorldEventKind.Neighbour,
                $"{company.StandIn.Name} ⚙ stands in for {company.Name} now: your trades with them go on"));
        }
    }

    /// <summary>
    /// Back from the Quiet Isles: the estate lands in the newest district with room (a free move), with a welcome-back
    /// package
    /// </summary>
    public void WakeUp(Company company)
    {
        if (!company.Asleep)
        {
            return;
        }
        company.Asleep = false;
        company.StandIn = null;
        company.StandInAnnounced = false;
        company.HoldUntil = null;
        company.KeepAsked = false;
        if (CrewOf(company) is { } crew && crew.KeepCards.Remove(company.Id))
        {
            CrewChanged(crew);
        }
        if (company.Packed == null && EstateOf(company) is { } home)
        {
            // Asleep in place: it wakes where it is, its stand-in steps aside
            MarkCompanyNetworksDirty(company.Id);
            WakeCoupling();
            company.Touch();
            Changes.Companies.Add(company.Id);
            foreach (var partner in PartnersOf(company))
            {
                Changes.Events.Add(new WorldEvent(partner.Id, WorldEventKind.Neighbour, $"{company.Name} is back: your trades with them are theirs again"));
            }
            Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Neighbour,
                $"Welcome back! Your estate in {WorldNames.Station(home.Station)} wakes up; a stand-in kept your trades going"));
            return;
        }
        // Its old seat when it's still free, else next to its crew, else next to people (the landing cards offer the rest)
        var seat = PreferredLanding(company);
        OpenDistrict(seat.District);
        var estate = Estate(seat.District, seat.Slot)!;
        if (estate.OwnerId is { } owner && Company(owner) is { IsNpc: true })
        {
            EvictTenant(estate);
        }
        if (estate.OwnerId != null)
        {
            estate = FindFreeEstate();
        }
        Unpack(company, estate);
        RerouteLinks(company);
        // A seat its crew kept for it (after the crew moved) is its own now
        if (HoldOn(KeyOf(estate)) is { } kept && kept.Hold.For == company.Id)
        {
            RemoveHold(kept.Crew, kept.Hold);
        }
        var gold = 1000.0 * company.Level;
        company.Gold += gold;
        company.ResearchPoints += 500 * company.Level;
        company.FreeMoves++;
        company.Touch();
        Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Neighbour,
            $"Welcome back! Your estate is unpacked in {WorldNames.District(estate.District)}: +{gold:N0} gold, research points and a free move"));
    }

    /// <summary>The Quiet Isles: the companies that are asleep</summary>
    public IEnumerable<Company> QuietIsles => _companies.Values.Where(t => t.Asleep);

    /// <summary>Weekly: a district that has been quiet for two weeks offers its active players a free move with a bonus</summary>
    private void CheckQuietDistricts()
    {
        foreach (var district in _openDistricts.ToList())
        {
            var players = _companies.Values.Where(t => !t.IsNpc && !t.Asleep && t.District == district).ToList();
            if (players.Count < 4)
            {
                _quietWeeks.Remove(district);
                continue;
            }
            var active = players.Count(t => ActivityOf(t) == Activity.Active);
            if (active >= players.Count * 0.4)
            {
                _quietWeeks.Remove(district);
                continue;
            }
            _quietWeeks[district] = _quietWeeks.GetValueOrDefault(district) + 1;
            if (_quietWeeks[district] < 2)
            {
                continue;
            }
            _quietWeeks.Remove(district);
            foreach (var company in players.Where(t => ActivityOf(t) == Activity.Active))
            {
                company.FreeMoves++;
                company.MergeBonus = true;
                company.Touch();
                Changes.Companies.Add(company.Id);
                Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Neighbour,
                    $"{WorldNames.District(district)} has been quiet for two weeks: move to a busy district for free, with a free parcel"));
            }
        }
    }
}

using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>How lately a player played: in the last three days, the last two weeks, or longer ago</summary>
public enum Activity
{
    Active,
    Resting,
    Dormant,
}

/// <summary>
/// Moving (1.4 §18). Nothing anyone built is lost: a plot is packed up for a move and unpacked again with its layout;
/// what doesn't fit the new land goes into crates. Being away changes nothing: a plot stays its owner's however long
/// they're gone.
/// </summary>
public sealed partial class GameWorld
{
    public static readonly TimeSpan RestingAfter = TimeSpan.FromDays(3);
    public static readonly TimeSpan DormantAfter = TimeSpan.FromDays(14);

    /// <summary>How long a company waits between moves, and how long its estate is in the air (content: together)</summary>
    public TimeSpan MoveEvery => TimeSpan.FromMinutes(Content.Progression.Together.MoveCooldownMinutes);
    public TimeSpan MoveTime => TimeSpan.FromMinutes(Content.Progression.Together.MoveFlightMinutes);

    public Activity ActivityOf(Company company)
    {
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
        // The outskirts stay with the plot: bought back, their buildings in crates; workshops come along in crates too
        SellOutskirts(company, estate);
        PackWorkshops(company);
        TakeAllFromDepot(company);
        var packed = new PackedEstate { Side = WorldLayout.SideOf(estate.Slot), UnlockedParcels = estate.UnlockedParcels };
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
        LeaveYard(company);
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
        Price = building.Price,
        OldId = building.Id,
        Site = IsSite(building),
        Contents = new(building.Contents),
        Imports = new(building.Imports),
        Filter = new(building.Filter),
        Setting = building.Setting,
        Keeps = new(building.Keeps),
    };

    /// <summary>
    /// Unpacks onto a plot: the same layout where the land allows it, turned to face the new plot's Yard (what stood by
    /// the Yard gate stands by it again), the rest in crates
    /// </summary>
    private void Unpack(Company company, Estate estate)
    {
        var packed = company.Packed ?? new PackedEstate { Side = WorldLayout.SideOf(estate.Slot) };
        var (from, to) = (packed.Side, WorldLayout.SideOf(estate.Slot));
        (int Dx, int Dy) Turn(int dx, int dy, int size)
        {
            var deep = dy >= WorldLayout.DeepOffset / 2 ? WorldLayout.DeepOffset : 0;
            var (x, y) = WorldLayout.MoveFootprint(from, to, dx, dy - deep, size);
            return (x, y + deep);
        }
        estate.OwnerId = company.Id;
        estate.UnlockedParcels = packed.UnlockedParcels != 0 ? WorldLayout.MoveParcels(from, to, packed.UnlockedParcels) : WorldLayout.StartMask(estate.Slot);
        company.DeepParcels = WorldLayout.MoveParcels(from, to, company.DeepParcels);
        estate.Touch();
        Changes.Estates.Add((estate.District, estate.Slot));
        company.District = estate.District;
        company.Slot = estate.Slot;
        company.Packed = null;
        company.ArrivesAt = null;
        OpenDistrict(estate.District);

        AddYardGate(company, estate);
        SettleInYard(company, estate);
        // HQ and trading post first: they always fit
        // The Deep Level after the surface: the lifts' shafts first
        foreach (var item in packed.Buildings.OrderBy(t => Content.Building(t.Type).Kind is BuildingKind.Hq or BuildingKind.TradingPost ? 0 : 1)
            .ThenBy(t => t.Dy >= WorldLayout.DeepOffset / 2 ? 1 : 0))
        {
            var definition = Content.Building(item.Type);
            var (dx, dy) = Turn(item.Dx, item.Dy, definition.Size);
            var pos = estate.Origin.Offset(dx, dy);
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
            var (hqX, hqY) = WorldLayout.HqLocal(estate.Slot);
            AddBuilding(company, "hq", estate.Origin.Offset(hqX, hqY), finished: true);
        }
        var (postX, postY) = WorldLayout.TradingPostLocal(estate.Slot);
        var postAt = estate.Origin.Offset(postX, postY);
        if (!BuildingsOf(company.Id).Any(t => Content.Building(t.Type).Kind == BuildingKind.TradingPost) && !IsOccupied(postAt))
        {
            AddBuilding(company, "tradingPost", postAt, finished: true);
        }
        foreach (var pipe in packed.Pipes)
        {
            var (dx, dy) = Turn(pipe.Dx, pipe.Dy, 1);
            var pos = estate.Origin.Offset(dx, dy);
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
        var building = AddBuilding(company, item.Type, pos, finished: true, item.Price > 0 ? item.Price : null);
        building.Level = item.Level;
        // A scaffold comes back as one (playtest: it came up built, without its materials); what it and its levels still
        // wait for follows it
        if (item.Site)
        {
            building.ConstructionEndsAt = SiteWaiting;
        }
        foreach (var ask in company.Asks.Where(t => t.Site == item.OldId))
        {
            ask.Site = building.Id;
        }
        foreach (var (resource, amount) in item.Contents)
        {
            building.Contents[resource] = amount;
        }
        building.Imports.UnionWith(item.Imports);
        building.Filter.UnionWith(item.Filter);
        building.Setting = item.Setting;
        foreach (var (resource, amount) in item.Keeps)
        {
            building.Keeps[resource] = amount;
        }
        EnsureShaft(building);
        return building;
    }

    /// <summary>A free estate in a district; null when none</summary>
    private Estate? FreeEstateIn(DistrictCoord district)
    {
        OpenDistrict(district);
        return Enumerable.Range(0, WorldLayout.EstatesPerDistrict).Select(t => _estates[(district, t)]).FirstOrDefault(t => t.OwnerId == null);
    }

    // ---------------------------------------------------------------- Moving

    /// <summary>
    /// The Era rule for moving (1.4 §11): towns of your own Era or one below, never higher (to reach an Era you help a
    /// town get there); null when the town is fine
    /// </summary>
    public string? EraMoveBlocker(Company company, DistrictCoord town)
    {
        var era = TownEra(town);
        var yours = YourEra(company);
        return town == company.District || era == yours || era == yours - 1 ? null
            : era > yours ? $"That town is in Era {era}: you can move to towns of your Era ({yours}) or one below."
            : $"That town is in Era {era}: you can move to towns of your Era ({yours}) or one below, where your newer buildings sleep.";
    }

    public string? MoveBlocker(Company company, DistrictCoord district)
    {
        if (company.Packed != null)
        {
            return "Your plot is on its way already.";
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
            return "That town isn't open.";
        }
        if (EraMoveBlocker(company, district) is { } era)
        {
            return era;
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
        if (FreeEstateIn(district) is not { } target)
        {
            return CommandResult.Fail("That town has no free plot.");
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
            var seat = PickForMe(company);
            estate = Estate(seat.District, seat.Slot)!;
        }
        Unpack(company, estate);
        RerouteLinks(company);
        AddMoment(company, "move", $"Moved to {WorldNames.District(company.District)}", true);
        Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Neighbour, company.Crates.Count == 0
            ? "Your plot has landed in its new spot"
            : $"Your plot has landed; {company.Crates.Count} buildings didn't fit and wait in crates"));
    }

    /// <summary>Ends what ties a company to where it lived: its Exchange orders (links take the new way)</summary>
    private void EndTradeOf(Company company)
    {
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
        // What it still waited for goes back
        DropWants(company, item.OldId);
        company.Touch();
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }

    /// <summary>For testing: a moving estate lands right away</summary>
    public void LandNow(Company company)
    {
        if (company.ArrivesAt != null)
        {
            Arrive(company);
        }
    }


    /// <summary>How many of a company's buildings wouldn't fit another plot's land and would wait in crates</summary>
    public int CratesIfMovedTo(Company company, EstateKey target)
    {
        var origin = WorldLayout.EstateOrigin(target.District, target.Slot);
        if (EstateOf(company) is not { } home)
        {
            return company.Crates.Count;
        }
        var crates = company.Crates.Count;
        foreach (var building in BuildingsOf(company.Id).Where(t => home.Contains(t.Position)))
        {
            var definition = Content.Building(building.Type);
            if (definition.Kind is BuildingKind.Hq or BuildingKind.TradingPost)
            {
                continue;
            }
            var deep = WorldLayout.IsDeep(building.Position) ? WorldLayout.DeepOffset : 0;
            var (dx, dy) = WorldLayout.MoveFootprint(WorldLayout.SideOf(home.Slot), WorldLayout.SideOf(target.Slot),
                building.Position.X - home.Origin.X, building.Position.Y - deep - home.Origin.Y, definition.Size);
            var pos = origin.Offset(dx, dy + deep);
            if (!FitsTheLand(definition, pos))
            {
                crates++;
            }
        }
        return crates;
    }

    /// <summary>Whether a building's footprint suits the land there (terrain, deposit, shore, forest), whoever owns it</summary>
    private bool FitsTheLand(BuildingDefinition definition, TilePos pos)
    {
        var footprint = Enumerable.Range(0, definition.Size).SelectMany(dy => Enumerable.Range(0, definition.Size).Select(dx => pos.Offset(dx, dy))).ToList();
        if (footprint.Any(t => WorldLayout.ZoneAt(t.X, t.Y).Kind == ZoneKind.Yard || !definition.Placement.Terrain.Contains(Tile(t).Terrain)))
        {
            return false;
        }
        if (definition.Placement.Deposit is { } deposit
            && footprint.Select(Tile).Where(t => DepositMatches(deposit, t.Deposit)).Select(t => t.Richness).DefaultIfEmpty(0).Max() < Math.Max(1, definition.Placement.MinRichness))
        {
            return false;
        }
        if (definition.Placement.Shore && !BorderTiles(pos, definition.Size).Any(t => Tile(t).Terrain == TerrainType.Water))
        {
            return false;
        }
        return !definition.Placement.Forest || ForestAround(pos, definition.Size) >= MinForest;
    }
}

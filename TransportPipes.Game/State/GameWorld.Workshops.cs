using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>
/// Building in the Yard (1.4 §7). Workshops: from Level 10 each resident may put their own buildings on up to 64 tiles of
/// the Yard's ends; a building there is joined to its owner's Yard gate without pipes, so the Yard brings it what it
/// lacks from every neighbour and takes its spare. Workshops belong to whoever built them and go along on a move. The
/// Depot (from Level 10): the crew's shared storage. A resident puts goods in for the crew; a neighbour's levels and new
/// buildings take them at that resident's crew price, and the town's Wonder takes them as gifts.
/// </summary>
public sealed partial class GameWorld
{
    public const int WorkshopLevel = 10;

    /// <summary>The tiles of a Yard each resident may build on</summary>
    public const int WorkshopTiles = 64;

    /// <summary>How many columns at each end of a Yard are its workshops</summary>
    private const int WorkshopColumns = 8;

    /// <summary>The Yard a tile is in, or null</summary>
    private static StationRef? YardAt(TilePos pos)
    {
        var zone = WorldLayout.ZoneAt(pos.X, pos.Y);
        return zone.Kind == ZoneKind.Yard ? new StationRef(zone.District, zone.Slot) : null;
    }

    /// <summary>Whether a tile is at one of its Yard's ends, where the workshops stand</summary>
    public static bool InWorkshops(TilePos pos) =>
        YardAt(pos) is { } yard && WorldLayout.Yard(yard) is var rect && (pos.X < rect.X0 + WorkshopColumns || pos.X > rect.X1 - WorkshopColumns);

    /// <summary>Whether a building stands in a Yard (a workshop or the Depot)</summary>
    public static bool InYard(Building building) => YardAt(building.Position) != null;

    /// <summary>The tiles of the Yard a company's workshops cover</summary>
    public int WorkshopTilesUsed(Company company) =>
        BuildingsOf(company.Id).Where(t => InYard(t) && Content.Building(t.Type).Kind != BuildingKind.Depot).Sum(t => t.Size * t.Size);

    /// <summary>Why a building can't stand on these Yard tiles, or null</summary>
    private string? WorkshopBlocker(Company company, BuildingDefinition definition, IReadOnlyList<TilePos> footprint)
    {
        if (EstateOf(company) is not { } estate || footprint.Any(t => YardAt(t) != estate.Station))
        {
            return "Only in your own Yard.";
        }
        if (company.Level < WorkshopLevel)
        {
            return $"Workshops in the Yard come at Company Level {WorkshopLevel}.";
        }
        if (!footprint.All(InWorkshops))
        {
            return "Workshops stand at the Yard's ends: its middle holds the stalls and the Monuments.";
        }
        if (definition.Kind == BuildingKind.Depot)
        {
            return DepotOf(estate.Station) != null ? "Your Yard has its Depot." : null;
        }
        if (definition.Kind is not (BuildingKind.Producer or BuildingKind.Storage))
        {
            return $"A {definition.Name} doesn't stand in the Yard.";
        }
        if (WorkshopTilesUsed(company) + definition.Size * definition.Size > WorkshopTiles)
        {
            return $"Your workshops cover {WorkshopTilesUsed(company)} of your {WorkshopTiles} tiles of the Yard.";
        }
        return null;
    }

    /// <summary>A workshop's link to its owner's network: its owner's Yard gate, as if a pipe ran there</summary>
    private TilePos? WorkshopGate(Building building) =>
        InYard(building) && Content.Building(building.Type).Kind != BuildingKind.Depot && Company(building.Owner) is { } owner && EstateOf(owner) is { } estate
            ? estate.YardGate
            : null;

    /// <summary>The tiles a building joins pipes at: around it, and a workshop also at its owner's gate</summary>
    private IEnumerable<TilePos> ConnectionTiles(Building building) =>
        WorkshopGate(building) is { } gate ? building.Border().Append(gate) : building.Border();

    /// <summary>The workshops joined to a Yard gate</summary>
    private IEnumerable<Building> WorkshopsAt(TilePos gate) =>
        EstateOfGate(gate) is { OwnerId: { } owner } estate
            ? BuildingsOf(owner).Where(t => InYard(t) && WorkshopGate(t) == gate)
            : [];

    // ---------------------------------------------------------------- The Depot

    /// <summary>The Depot of a Yard (null: nobody built it yet)</summary>
    public Building? DepotOf(StationRef yard) =>
        _buildings.Values.FirstOrDefault(t => Content.Building(t.Type).Kind == BuildingKind.Depot && YardAt(t.Position) == yard);

    /// <summary>What the Depot holds in all</summary>
    public double DepotHolds(Crew crew) => crew.Depot.Values.Sum(t => t.Values.Sum());

    /// <summary>What a Yard's Depot holds at most</summary>
    public double DepotCapacity(StationRef yard) =>
        DepotOf(yard) is { } depot ? Content.Building(depot.Type).Capacity * Content.Progression.OutputMultiplier(depot.Level) : 0;

    /// <summary>Puts goods from a resident's Storages into the Yard's Depot, for the crew</summary>
    public CommandResult PutInDepot(long companyId, string resource, double amount)
    {
        if (Company(companyId) is not { } company || CrewOf(company) is not { } crew || EstateOf(company) is not { } estate)
        {
            return CommandResult.Fail("Only a Yard's residents use its Depot.");
        }
        if (DepotOf(estate.Station) == null)
        {
            return CommandResult.Fail("Your Yard has no Depot yet.");
        }
        amount = Math.Min(Math.Floor(amount), Math.Min(Materials(company.Id).GetValueOrDefault(resource), DepotCapacity(estate.Station) - DepotHolds(crew)));
        if (amount < 1)
        {
            return CommandResult.Fail(DepotHolds(crew) >= DepotCapacity(estate.Station) - 1 ? "The Depot is full." : "Your Storages hold none of it.");
        }
        TakeFromStorages(company.Id, resource, amount);
        if (!crew.Depot.TryGetValue(company.Id, out var mine))
        {
            crew.Depot[company.Id] = mine = new Dictionary<string, double>();
        }
        mine[resource] = mine.GetValueOrDefault(resource) + amount;
        CrewChanged(crew);
        FillWantsFromDepot(crew);
        return CommandResult.Ok;
    }

    /// <summary>Takes a resident's own goods back from the Depot into their Storages</summary>
    public CommandResult TakeFromDepot(long companyId, string resource, double amount)
    {
        if (Company(companyId) is not { } company || CrewOf(company) is not { } crew || !crew.Depot.TryGetValue(company.Id, out var mine))
        {
            return CommandResult.Fail("You have nothing in the Depot.");
        }
        amount = Math.Min(amount, mine.GetValueOrDefault(resource));
        var put = PutInStorages(company.Id, resource, amount);
        if (put < 1e-6)
        {
            return CommandResult.Fail("Your Storages have no room.");
        }
        mine[resource] -= put;
        if (mine[resource] <= 1e-6)
        {
            mine.Remove(resource);
        }
        CrewChanged(crew);
        return CommandResult.Ok;
    }

    /// <summary>
    /// What residents put in the Depot goes into the crew's wants (levels, new buildings, pipes): a resident's own goods
    /// free, a neighbour's at that neighbour's crew price
    /// </summary>
    private void FillWantsFromDepot(Crew crew)
    {
        foreach (var owner in CrewMembers(crew).ToList())
        {
            foreach (var ask in owner.Asks.Where(t => t.IsWant && t.Left > 1e-6).ToList())
            {
                foreach (var (depositor, goods) in crew.Depot.OrderBy(t => t.Key == owner.Id ? 0 : 1).ThenBy(t => t.Key).ToList())
                {
                    if (ask.Left <= 1e-6 || goods.GetValueOrDefault(ask.Resource) <= 1e-6 || Company(depositor) is not { } seller)
                    {
                        continue;
                    }
                    var units = Math.Min(ask.Left, goods[ask.Resource]);
                    goods[ask.Resource] -= units;
                    ask.Got += units;
                    ask.From[depositor] = ask.From.GetValueOrDefault(depositor) + units;
                    if (seller.Id != owner.Id)
                    {
                        PayForWant(owner, seller, ask, units);
                    }
                }
                if (ask.Site is { } id && Building(id) is { } site)
                {
                    FinishWantsIfFull(owner, site);
                    StepUpIfFull(owner, site);
                }
                else if (ask.PipeTiles != null)
                {
                    FinishPipeWantIfFull(owner, ask);
                }
            }
        }
        foreach (var goods in crew.Depot.Values)
        {
            foreach (var empty in goods.Where(t => t.Value <= 1e-6).Select(t => t.Key).ToList())
            {
                goods.Remove(empty);
            }
        }
    }

    /// <summary>Gives the town's Wonder what a resident's own goods in the Depot hold of what it lacks; returns the units given</summary>
    private double GiveFromDepot(Company company, TownWonder wonder)
    {
        if (CrewOf(company) is not { } crew || !crew.Depot.TryGetValue(company.Id, out var mine))
        {
            return 0;
        }
        var given = 0.0;
        foreach (var good in wonder.Required.Keys)
        {
            var units = Math.Min(mine.GetValueOrDefault(good), WonderLacks(wonder, good));
            if (units > 1e-6)
            {
                mine[good] -= units;
                CountGift(company, wonder, good, units);
                given += units;
            }
        }
        CrewChanged(crew);
        return given;
    }

    /// <summary>On a move: a resident's goods in the Depot go back into its Storages (what fits), to be packed with them</summary>
    private void TakeAllFromDepot(Company company)
    {
        if (CrewOf(company) is not { } crew || !crew.Depot.TryGetValue(company.Id, out var mine))
        {
            return;
        }
        foreach (var (resource, amount) in mine.ToList())
        {
            mine[resource] = amount - PutInStorages(company.Id, resource, amount);
            if (mine[resource] <= 1e-6)
            {
                mine.Remove(resource);
            }
        }
        CrewChanged(crew);
    }

    /// <summary>On a move: a resident's workshops go into crates</summary>
    private void PackWorkshops(Company company)
    {
        foreach (var building in BuildingsOf(company.Id).Where(InYard).ToList())
        {
            if (Content.Building(building.Type).Kind != BuildingKind.Depot)
            {
                company.Crates.Add(PackBuilding(building, building.Position));
                RemoveBuilding(building);
            }
        }
    }
}

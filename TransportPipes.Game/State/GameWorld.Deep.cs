using System.Numerics;
using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>
/// The Deep Level (GDD §11.2): a second layer of 32×32 tiles under every estate, reached by Deep Lifts and dug out
/// parcel by parcel. Deep tiles are surface tiles shifted by <see cref="WorldLayout.DeepOffset"/>; what is dug out
/// belongs to the company (it moves along), not to the land.
/// </summary>
public sealed partial class GameWorld
{
    /// <summary>What the miners bring up per minute of digging (into the storages)</summary>
    public const double DigStonePerMinute = 30;
    public const double DigOrePerMinute = 10;

    /// <summary>Hours for the first parcel, days for the last ones</summary>
    public static TimeSpan DigTime(int dug) => TimeSpan.FromHours(Math.Round(2 * Math.Pow(1.3, dug), 1));

    public static double DigCost(int dug) => Math.Round(100 * Math.Pow(dug + 1, 1.5)) * 100;

    public static int DugCount(Company company) => BitOperations.PopCount((uint)company.DeepParcels);

    /// <summary>Whether a company has a finished Deep Lift (and so a Deep Level)</summary>
    public bool HasDeepLevel(Company company) => HasBuilding(company.Id, BuildingKind.DeepLift);

    private bool IsLift(Building building) => !building.UnderConstruction && Content.Building(building.Type).Kind == BuildingKind.DeepLift;

    /// <summary>The other end of a lift: the shaft under a Deep Lift, or the Deep Lift above a shaft</summary>
    public Building? LiftPartner(Building lift)
    {
        var other = WorldLayout.IsDeep(lift.Position) ? WorldLayout.Surface(lift.Position) : WorldLayout.Deep(lift.Position);
        return _buildingAt.TryGetValue(other, out var id) && _buildings[id] is var partner && partner.Owner == lift.Owner
            && partner.Position == other && Content.Building(partner.Type).Kind == BuildingKind.DeepLift
            ? partner
            : null;
    }

    /// <summary>A lift carries what its level allows: the Deep Lift's throughput times its level</summary>
    private double LiftThroughput(Building lift)
    {
        var top = WorldLayout.IsDeep(lift.Position) ? LiftPartner(lift) ?? lift : lift;
        return Content.Building(top.Type).Throughput * top.Level;
    }

    /// <summary>Whether a deep tile is dug out: its parcel by the estate's owner, or the pocket at the bottom of a lift</summary>
    public bool IsDug(TilePos pos)
    {
        var surface = WorldLayout.Surface(pos);
        if (EstateAt(surface) is not { OwnerId: { } owner } estate || Company(owner) is not { } company)
        {
            return false;
        }
        return (company.DeepParcels & (1 << estate.ParcelAt(surface))) != 0
            || (_buildingAt.TryGetValue(pos, out var id) && Content.Building(_buildings[id].Type).Kind == BuildingKind.DeepLift);
    }

    private TileInfo DeepTile(TilePos pos)
    {
        var info = Generator.Tile(pos);
        return IsDug(pos) ? info with { Terrain = TerrainType.Cavern } : info;
    }

    /// <summary>Whether a tile is part of the company's estate or the Deep Level under it</summary>
    private static bool InEstateOrBelow(Estate estate, TilePos pos) => estate.Contains(WorldLayout.Surface(pos));

    // ---------------------------------------------------------------- Lifts

    /// <summary>A finished Deep Lift reaches down: its shaft appears in the Deep Level, in a small dug out pocket</summary>
    private void EnsureShaft(Building lift)
    {
        if (Content.Building(lift.Type).Kind != BuildingKind.DeepLift || WorldLayout.IsDeep(lift.Position) || lift.UnderConstruction
            || Company(lift.Owner) is not { } owner || LiftPartner(lift) != null)
        {
            return;
        }
        var bottom = WorldLayout.Deep(lift.Position);
        foreach (var tile in lift.Footprint().Select(WorldLayout.Deep))
        {
            // Pipes that were laid there make room (the placement rules keep buildings away)
            if (_conductors.Remove(tile, out var pipe))
            {
                Deleted.Add(pipe);
                MarkDirty(tile);
            }
            if (_buildingAt.ContainsKey(tile))
            {
                return;
            }
        }
        AddBuilding(owner, "liftShaft", bottom, finished: true);
    }

    // ---------------------------------------------------------------- Digging

    /// <summary>Whether a parcel may be dug: it holds a lift's pocket or lies next to a dug out parcel</summary>
    private bool CanDig(Company company, Estate estate, int parcel)
    {
        var (px, py) = WorldLayout.ParcelOrigin(parcel);
        var holdsLift = BuildingsOf(company.Id).Any(t => WorldLayout.IsDeep(t.Position) && Content.Building(t.Type).Kind == BuildingKind.DeepLift
            && t.Footprint().Any(f => estate.ParcelAt(WorldLayout.Surface(f)) == parcel));
        if (holdsLift)
        {
            return true;
        }
        var x = px / WorldLayout.ParcelSize;
        var y = py / WorldLayout.ParcelSize;
        return new[] { (x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1) }
            .Where(t => t.Item1 >= 0 && t.Item2 >= 0 && t.Item1 < WorldLayout.ParcelsPerSide && t.Item2 < WorldLayout.ParcelsPerSide)
            .Any(t => (company.DeepParcels & (1 << (t.Item2 * WorldLayout.ParcelsPerSide + t.Item1))) != 0);
    }

    /// <summary>Starts digging out a parcel of the Deep Level: a timer that brings up stone and ore</summary>
    public CommandResult Dig(long companyId, int parcel)
    {
        if (Company(companyId) is not { } company || EstateOf(company) is not { } estate)
        {
            return CommandResult.Fail("You don't have a plot.");
        }
        if (!HasDeepLevel(company))
        {
            return CommandResult.Fail("Build a Deep Lift first.");
        }
        if (parcel < 0 || parcel >= WorldLayout.ParcelCount)
        {
            return CommandResult.Fail("Unknown parcel.");
        }
        if ((company.DeepParcels & (1 << parcel)) != 0)
        {
            return CommandResult.Fail("This parcel is dug out already.");
        }
        if (company.DigParcel != null)
        {
            return CommandResult.Fail("Your miners are still digging.");
        }
        if (!CanDig(company, estate, parcel))
        {
            return CommandResult.Fail("Dig next to a lift or a parcel that is dug out.");
        }
        var dug = DugCount(company);
        var payment = Pay(company, DigCost(dug), new Dictionary<string, double>());
        if (!payment.Success)
        {
            return payment;
        }
        company.DigParcel = parcel;
        company.DigStartedAt = Now;
        company.DigEndsAt = Now + DigTime(dug);
        company.Touch();
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }

    /// <summary>The digs that end in a span</summary>
    private IEnumerable<(DateTime At, Action Run)> DigMoments(DateTime from, DateTime to)
    {
        foreach (var company in _companies.Values)
        {
            if (company.DigEndsAt is { } end && end <= to)
            {
                yield return (end > from ? end : from, () => FinishDig(company));
            }
        }
    }

    private void FinishDig(Company company)
    {
        if (company.DigParcel is not { } parcel)
        {
            return;
        }
        var minutes = ((company.DigEndsAt ?? Now) - (company.DigStartedAt ?? Now)).TotalMinutes;
        company.DeepParcels |= 1 << parcel;
        company.DigParcel = null;
        company.DigStartedAt = null;
        company.DigEndsAt = null;
        // The ore the miners hit most
        var ore = "ironOre";
        if (EstateOf(company) is { } estate)
        {
            var (px, py) = WorldLayout.ParcelOrigin(parcel);
            var deposits = new List<string>();
            for (var y = 0; y < WorldLayout.ParcelSize; y++)
            {
                for (var x = 0; x < WorldLayout.ParcelSize; x++)
                {
                    var deposit = Generator.Tile(WorldLayout.Deep(estate.Origin.Offset(px + x, py + y))).Deposit;
                    if (deposit is "ironOre" or "copperOre" or "coal")
                    {
                        deposits.Add(deposit);
                    }
                }
            }
            ore = deposits.GroupBy(t => t).OrderByDescending(t => t.Count()).Select(t => t.Key).FirstOrDefault() ?? ore;
        }
        var stone = StoreGoods(company, "stone", minutes * DigStonePerMinute);
        var ores = StoreGoods(company, ore, minutes * DigOrePerMinute);
        AddStat(company, "dug", 1);
        company.Touch();
        Changes.Companies.Add(company.Id);
        Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.ConstructionFinished, stone + ores < 1
            ? "A new cavern is dug out in the Deep Level; your storages were full, so the stone and ore stayed down there"
            : $"A new cavern is dug out in the Deep Level; the miners brought up {Math.Round(stone):N0} stone and {Math.Round(ores):N0} {Content.Resources[ore].Name.ToLowerInvariant()}"));
    }

    /// <summary>Puts goods into a company's storages as far as they fit; returns what fit</summary>
    private double StoreGoods(Company company, string resource, double amount)
    {
        var left = amount;
        foreach (var storage in _buildings.Values.Where(t => t.Owner == company.Id && !t.UnderConstruction && Content.Building(t.Type).Kind == BuildingKind.Storage).OrderBy(t => t.Id))
        {
            var put = Math.Min(StorageCapacity(storage, Content.Building(storage.Type)) - storage.StoredTotal, left);
            if (put <= 1e-9)
            {
                continue;
            }
            storage.Contents[resource] = (storage.Contents.TryGetValue(resource, out var v) ? v : 0) + put;
            storage.Touch();
            MarkNeedsSolve(storage.Id);
            left -= put;
        }
        return amount - left;
    }
}

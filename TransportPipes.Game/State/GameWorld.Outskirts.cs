using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>
/// The outskirts (1.4 §5): nine 8×8 parcels set aside for every plot in the corners of its Yard group, bought from Era 2
/// once its owner researched Survey the Outskirts, in any order, at 100 million × 1.4 for every one owned before. On a
/// move the market buys them back for half their price, and what stood on them goes into crates.
/// </summary>
public sealed partial class GameWorld
{
    /// <summary>The outskirts parcel a tile belongs to: (its plot, its index 0..8), or null</summary>
    public static (Estate Plot, int Index)? OutskirtsParcelAt(Func<DistrictCoord, int, Estate?> estate, TilePos pos)
    {
        var zone = WorldLayout.ZoneAt(pos.X, pos.Y);
        if (zone.Kind != ZoneKind.Outskirts || WorldLayout.OutskirtsOwner(zone.LocalX, zone.LocalY) is not { } owner || estate(zone.District, owner.Slot) is not { } plot)
        {
            return null;
        }
        return (plot, owner.Index);
    }

    /// <summary>Whether a tile is an outskirts parcel the company bought</summary>
    public bool OwnsOutskirts(Company company, TilePos pos) =>
        EstateOf(company) is { } home && OutskirtsParcelAt(Estate, WorldLayout.Surface(pos)) is { } parcel && parcel.Plot == home && home.HasOutskirts(parcel.Index);

    /// <summary>The world tiles of a plot's outskirts parcel</summary>
    public static IEnumerable<TilePos> OutskirtsTiles(Estate estate, int index)
    {
        var origin = WorldLayout.DistrictOrigin(estate.District);
        var (x, y) = WorldLayout.OutskirtsParcels(estate.Slot)[index];
        for (var dy = 0; dy < WorldLayout.ParcelSize; dy++)
        {
            for (var dx = 0; dx < WorldLayout.ParcelSize; dx++)
            {
                yield return origin.Offset(x + dx, y + dy);
            }
        }
    }

    /// <summary>What a company's next outskirts parcel costs (the Workshop Monument counts)</summary>
    public double NextOutskirtsCost(Company company) =>
        Math.Round(Content.Progression.OutskirtsCost(EstateOf(company)?.OutskirtsCount ?? 0) * CostFactor(company));

    /// <summary>Why a company can't buy outskirts now, or null</summary>
    public string? OutskirtsBlocker(Company company)
    {
        if (EstateOf(company) is not { } estate)
        {
            return "You don't have a plot.";
        }
        if (TownEra(estate.District) < 2)
        {
            return "The outskirts open with your town's Era 2.";
        }
        if (!HasResearch(company, "survey"))
        {
            return "Research Survey the Outskirts first.";
        }
        if (estate.OutskirtsCount >= WorldLayout.OutskirtsParcels(estate.Slot).Count)
        {
            return "You own all your outskirts.";
        }
        return null;
    }

    /// <summary>Buys one of the nine outskirts parcels set aside for the company's plot</summary>
    public CommandResult BuyOutskirts(long companyId, int index)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (OutskirtsBlocker(company) is { } blocker)
        {
            return CommandResult.Fail(blocker);
        }
        var estate = EstateOf(company)!;
        if (index < 0 || index >= WorldLayout.OutskirtsParcels(estate.Slot).Count)
        {
            return CommandResult.Fail("Unknown parcel.");
        }
        if (estate.HasOutskirts(index))
        {
            return CommandResult.Fail("You own this parcel already.");
        }
        var cost = NextOutskirtsCost(company);
        var payment = Pay(company, cost, new Dictionary<string, double>());
        if (!payment.Success)
        {
            return payment;
        }
        AddStat(company, "outskirtsGold", cost);
        AddStat(company, "landGold", cost);
        AddMoment(company, "land", $"Bought outskirts parcel {estate.OutskirtsCount + 1}", true);
        estate.OutskirtsParcels |= 1 << index;
        estate.Touch();
        Changes.Estates.Add((estate.District, estate.Slot));
        foreach (var tile in OutskirtsTiles(estate, index))
        {
            Changes.Tiles.Add(tile);
        }
        return CommandResult.Ok;
    }

    /// <summary>
    /// A move (1.4 §18): the outskirts are bought back for half their price (not earned: a refund), what stood on them goes
    /// into crates, their pipes go
    /// </summary>
    private void SellOutskirts(Company company, Estate estate)
    {
        if (estate.OutskirtsCount == 0)
        {
            return;
        }
        var tiles = Enumerable.Range(0, 9).Where(estate.HasOutskirts).SelectMany(t => OutskirtsTiles(estate, t)).ToHashSet();
        foreach (var building in BuildingsOf(company.Id).Where(t => tiles.Contains(t.Position)).ToList())
        {
            if (Content.Building(building.Type).Buildable)
            {
                company.Crates.Add(PackBuilding(building, building.Position));
            }
            RemoveBuilding(building);
        }
        foreach (var conductor in _conductors.Values.Where(t => t.Owner == company.Id && tiles.Contains(t.Pos)).ToList())
        {
            _conductors.Remove(conductor.Pos);
            Deleted.Add(conductor);
            MarkDirty(conductor.Pos);
            Changes.Tiles.Add(conductor.Pos);
        }
        var paid = Enumerable.Range(0, estate.OutskirtsCount).Sum(t => Content.Progression.OutskirtsCost(t));
        var back = Math.Round(paid * Content.Progression.OutskirtsBuyBack);
        company.Gold += back;
        AddStat(company, "goldSpent", -back);
        AddStat(company, "landGold", -back);
        Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Neighbour, $"Your {estate.OutskirtsCount} outskirts parcels were bought back for {Gold(back)}: what stood there waits in crates"));
        estate.OutskirtsParcels = 0;
        foreach (var tile in tiles)
        {
            Changes.Tiles.Add(tile);
        }
    }
}

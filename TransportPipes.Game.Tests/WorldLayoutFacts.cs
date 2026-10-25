using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

/// <summary>The town of 1.4 §4: four Yard groups around the hub, the roads, the outskirts and the wilderness</summary>
[TestClass]
public class WorldLayoutFacts
{
    private static readonly DistrictCoord Capital = new(0, 0);

    [TestMethod]
    public void TheCapitalsRoadCrossingIsTheOriginInTheMiddleOfTheHub()
    {
        var zone = WorldLayout.ZoneAt(0, 0);

        Assert.AreEqual(ZoneKind.Hub, zone.Kind);
        Assert.AreEqual(Capital, zone.District);
        Assert.AreEqual(new TilePos(-WorldLayout.OriginOffsetX, -WorldLayout.OriginOffsetY), WorldLayout.DistrictOrigin(Capital));
        Assert.AreEqual(68, WorldLayout.HubLocal.Width);
        Assert.AreEqual(52, WorldLayout.HubLocal.Height);
    }

    [TestMethod]
    public void ATownIsFourYardGroupsWithRoadsBetweenThemAndWildernessAround()
    {
        var origin = WorldLayout.DistrictOrigin(Capital);
        Zone At(int lx, int ly) => WorldLayout.ZoneAt(origin.X + lx, origin.Y + ly);

        Assert.AreEqual(196, WorldLayout.DistrictWidth);
        Assert.AreEqual(164, WorldLayout.DistrictHeight);
        Assert.AreEqual(ZoneKind.Corridor, At(97, 10).Kind, "the vertical road");
        Assert.AreEqual(ZoneKind.Corridor, At(10, 81).Kind, "the horizontal road");
        Assert.AreEqual(ZoneKind.Wilderness, At(196, 10).Kind);
        Assert.AreEqual(ZoneKind.Wilderness, At(-1, 10).Kind);
        Assert.AreEqual(ZoneKind.Wilderness, At(10, 164).Kind);
        // The next town lies 128 tiles of wilderness further
        Assert.AreEqual(new DistrictCoord(1, 0), WorldLayout.ZoneAt(origin.X + 196 + 128, origin.Y + 10).District);
        Assert.AreEqual(ZoneKind.Wilderness, WorldLayout.ZoneAt(origin.X + 196 + 127, origin.Y + 10).Kind);
    }

    [TestMethod]
    public void EveryPlotAndYardIsWhereItsGroupPutsIt()
    {
        foreach (var station in WorldLayout.Stations(new DistrictCoord(2, -1)))
        {
            var yard = WorldLayout.Yard(station);
            Assert.AreEqual(32, yard.Width);
            Assert.AreEqual(16, yard.Height);
            var zone = WorldLayout.ZoneAt(yard.X0 + 3, yard.Y0 + 5);
            Assert.AreEqual(ZoneKind.Yard, zone.Kind);
            Assert.AreEqual(station.Index, zone.Slot);
            foreach (var slot in WorldLayout.StationSlots(station.Index))
            {
                var plot = WorldLayout.EstateOrigin(station.District, slot);
                var inside = WorldLayout.ZoneAt(plot.X + 5, plot.Y + 7);
                Assert.AreEqual(ZoneKind.Estate, inside.Kind);
                Assert.AreEqual(slot, inside.Slot);
                Assert.AreEqual((5, 7), (inside.LocalX, inside.LocalY));
                Assert.AreEqual(station, WorldLayout.StationOf(station.District, slot));
            }
        }
    }

    [TestMethod]
    public void EveryGateIsOnItsPlotsEdgeAgainstTheYardInTheMiddleOfTheStartArea()
    {
        for (var slot = 0; slot < WorldLayout.EstatesPerDistrict; slot++)
        {
            var gate = WorldLayout.YardGate(Capital, slot);
            var zone = WorldLayout.ZoneAt(gate.X, gate.Y);
            Assert.AreEqual(ZoneKind.Estate, zone.Kind);
            Assert.AreEqual(slot, zone.Slot);
            var yard = WorldLayout.Yard(WorldLayout.StationOf(Capital, slot));
            var touches = new[] { gate.Offset(1, 0), gate.Offset(-1, 0), gate.Offset(0, 1), gate.Offset(0, -1) }.Count(yard.Contains);
            Assert.AreEqual(1, touches, $"slot {slot}'s gate touches its Yard");
            Assert.IsTrue(WorldLayout.StartParcels(slot).Contains(WorldLayout.ParcelOf(zone.LocalX, zone.LocalY)), $"slot {slot}'s gate is in its start area");
        }
    }

    [TestMethod]
    public void TheStartAreaLiesAgainstTheYardWithTheHqAndTheTradingPostByTheGate()
    {
        for (var slot = 0; slot < WorldLayout.EstatesPerDistrict; slot++)
        {
            var start = WorldLayout.StartParcels(slot);
            Assert.HasCount(4, start);
            var (hx, hy) = WorldLayout.HqLocal(slot);
            var (px, py) = WorldLayout.TradingPostLocal(slot);
            var (gx, gy) = WorldLayout.YardGateLocal(slot);
            foreach (var (x, y) in new[] { (hx, hy), (hx + 1, hy + 1), (px, py) })
            {
                Assert.IsTrue(start.Contains(WorldLayout.ParcelOf(x, y)), $"slot {slot}: ({x},{y}) in the start area");
            }
            Assert.IsLessThanOrEqualTo(5, Math.Abs(hx - gx) + Math.Abs(hy - gy), "the HQ next to the gate");
            Assert.IsLessThanOrEqualTo(4, Math.Abs(px - gx) + Math.Abs(py - gy), "the trading post next to the gate");
        }
    }

    [TestMethod]
    public void EachPlotHasNineOutskirtsParcelsOfItsOwnNextToItsLand()
    {
        var origin = WorldLayout.DistrictOrigin(Capital);
        var all = new HashSet<(int, int)>();
        for (var slot = 0; slot < WorldLayout.EstatesPerDistrict; slot++)
        {
            var parcels = WorldLayout.OutskirtsParcels(slot);
            Assert.HasCount(9, parcels);
            foreach (var (x, y) in parcels)
            {
                Assert.IsTrue(all.Add((x, y)), "no parcel is set aside twice");
                var zone = WorldLayout.ZoneAt(origin.X + x + 3, origin.Y + y + 3);
                Assert.AreEqual(ZoneKind.Outskirts, zone.Kind);
                Assert.AreEqual(slot, zone.Slot);
            }
            // Next to its own land: the nearest of its parcels touches the plot
            var (ex, ey) = WorldLayout.EstateLocal(slot);
            var plot = new TileRect(ex, ey, ex + 31, ey + 31);
            Assert.IsTrue(parcels.Any(t => Touches(plot, new TileRect(t.X, t.Y, t.X + 7, t.Y + 7))), $"slot {slot}'s outskirts touch it");
        }
        Assert.HasCount(144, all, "12 patches of 32×24 tiles");
    }

    private static bool Touches(TileRect a, TileRect b) =>
        a.X0 <= b.X1 + 1 && b.X0 <= a.X1 + 1 && a.Y0 <= b.Y1 + 1 && b.Y0 <= a.Y1 + 1;

    [TestMethod]
    public void TownGatesStandWhereTheRoadsLeaveTheTown()
    {
        Assert.HasCount(4, WorldLayout.TownGatesLocal);
        foreach (var gate in WorldLayout.TownGatesLocal)
        {
            Assert.IsTrue(gate.X0 == 0 || gate.Y0 == 0 || gate.X1 == WorldLayout.DistrictWidth - 1 || gate.Y1 == WorldLayout.DistrictHeight - 1, "at the town's edge");
        }
    }

    [TestMethod]
    public void ALayoutTurnsToFaceItsNewYard()
    {
        // What stood by the gate stands by the gate on every side
        for (var to = 0; to < 4; to++)
        {
            var side = (PlotSide)to;
            var slot = to;
            Assert.AreEqual(WorldLayout.HqLocal(slot), WorldLayout.MoveFootprint(PlotSide.South, side, WorldLayout.HqLocal(3).X, WorldLayout.HqLocal(3).Y, 2));
            Assert.AreEqual(WorldLayout.TradingPostLocal(slot), WorldLayout.MoveFootprint(PlotSide.South, side, WorldLayout.TradingPostLocal(3).X, WorldLayout.TradingPostLocal(3).Y, 1));
            Assert.AreEqual(WorldLayout.StartMask(slot), WorldLayout.MoveParcels(PlotSide.South, side, WorldLayout.StartMask(3)));
        }
    }

    [TestMethod]
    public void TheDeepLevelLiesFarBelowTheSurface()
    {
        var surface = new TilePos(123, -45);
        var deep = WorldLayout.Deep(surface);

        Assert.IsTrue(WorldLayout.IsDeep(deep));
        Assert.IsFalse(WorldLayout.IsDeep(surface));
        Assert.AreEqual(surface, WorldLayout.Surface(deep));
        Assert.AreEqual(WorldLayout.ZoneAt(surface.X, surface.Y) with { District = default }, WorldLayout.ZoneAt(deep.X, deep.Y) with { District = default });
        Assert.IsLessThan(1 << 20, deep.Y, "tile keys of the client stay unique");
    }

    [TestMethod]
    public void TownsOpenFromTheCapitalOutwards()
    {
        var order = WorldLayout.DistrictOrder().Take(9).ToList();

        Assert.AreEqual(Capital, order[0]);
        Assert.HasCount(9, order.Distinct());
        Assert.IsTrue(order.All(t => Math.Abs(t.X) <= 1 && Math.Abs(t.Y) <= 1));
    }
}

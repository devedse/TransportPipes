using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

[TestClass]
public class WorldLayoutFacts
{
    [TestMethod]
    public void TheCapitalHubIsAroundTheOrigin()
    {
        var zone = WorldLayout.ZoneAt(0, 0);

        Assert.AreEqual(ZoneKind.Hub, zone.Kind);
        Assert.AreEqual(new DistrictCoord(0, 0), zone.District);
        Assert.AreEqual(16, zone.LocalX);
        Assert.AreEqual(16, zone.LocalY);
        Assert.AreEqual(new TilePos(-16, -16), WorldLayout.HubOrigin(new DistrictCoord(0, 0)));
    }

    [TestMethod]
    public void EstatesCorridorsAndWildernessAlternate()
    {
        var district = new DistrictCoord(0, 0);
        var hub = WorldLayout.HubOrigin(district);

        // Right of the hub: two corridor tiles, then the estate in slot 12 (slot numbers skip the hub)
        Assert.AreEqual(ZoneKind.Hub, WorldLayout.ZoneAt(hub.X + 31, hub.Y).Kind);
        Assert.AreEqual(ZoneKind.Corridor, WorldLayout.ZoneAt(hub.X + 32, hub.Y).Kind);
        Assert.AreEqual(ZoneKind.Corridor, WorldLayout.ZoneAt(hub.X + 33, hub.Y).Kind);
        var estate = WorldLayout.ZoneAt(hub.X + 34, hub.Y);
        Assert.AreEqual(ZoneKind.Estate, estate.Kind);
        Assert.AreEqual(12, estate.Slot);
        Assert.AreEqual(0, estate.LocalX);

        // The district edge: corridor, then wilderness
        var originX = WorldLayout.DistrictOriginX(0);
        Assert.AreEqual(ZoneKind.Corridor, WorldLayout.ZoneAt(originX, hub.Y).Kind);
        Assert.AreEqual(ZoneKind.Corridor, WorldLayout.ZoneAt(originX + WorldLayout.DistrictSize - 1, hub.Y).Kind);
        Assert.AreEqual(ZoneKind.Wilderness, WorldLayout.ZoneAt(originX + WorldLayout.DistrictSize, hub.Y).Kind);
        Assert.AreEqual(ZoneKind.Wilderness, WorldLayout.ZoneAt(originX - 1, hub.Y).Kind);
    }

    [TestMethod]
    public void SlotsAndBlocksRoundTrip()
    {
        var slots = Enumerable.Range(0, WorldLayout.EstatesPerDistrict).ToList();
        foreach (var slot in slots)
        {
            var (bx, by) = WorldLayout.BlockOfSlot(slot);
            Assert.IsFalse(bx == WorldLayout.HubBlock && by == WorldLayout.HubBlock);
            Assert.AreEqual(slot, WorldLayout.SlotOf(bx, by));

            var origin = WorldLayout.EstateOrigin(new DistrictCoord(2, -1), slot);
            var zone = WorldLayout.ZoneAt(origin.X + 5, origin.Y + 7);
            Assert.AreEqual(ZoneKind.Estate, zone.Kind);
            Assert.AreEqual(slot, zone.Slot);
            Assert.AreEqual(new DistrictCoord(2, -1), zone.District);
            Assert.AreEqual((5, 7), (zone.LocalX, zone.LocalY));
        }
    }

    [TestMethod]
    public void EveryDistrictHasFourStationsOfFourAndFourOfTwo()
    {
        var slots = Enumerable.Range(0, WorldLayout.StationsPerDistrict).Select(WorldLayout.StationSlots).ToList();

        CollectionAssert.AreEquivalent(Enumerable.Range(0, WorldLayout.EstatesPerDistrict).ToList(), slots.SelectMany(t => t).ToList(), "every seat is in one station");
        CollectionAssert.AreEqual(new[] { 4, 4, 4, 4, 2, 2, 2, 2 }, slots.Select(t => t.Count).ToArray());
        foreach (var (station, i) in slots.Select((t, i) => (t, i)))
        {
            Assert.IsTrue(station.All(slot => WorldLayout.StationOfSlot(slot) == i));
        }
    }

    [TestMethod]
    public void AYardIsTheCorridorAndFourTilesOfEachOfItsEstates()
    {
        var district = new DistrictCoord(-1, 3);
        foreach (var station in WorldLayout.Stations(district))
        {
            var yard = WorldLayout.Yard(station);
            Assert.AreEqual(WorldLayout.YardSize, yard.Width);
            Assert.AreEqual(WorldLayout.YardSize, yard.Height);
            foreach (var tile in yard.Tiles())
            {
                var zone = WorldLayout.ZoneAt(tile.X, tile.Y);
                Assert.AreEqual(ZoneKind.Yard, zone.Kind, $"{tile}");
                Assert.AreEqual(station.Index, zone.Slot);
            }
            foreach (var slot in WorldLayout.StationSlots(station.Index))
            {
                var part = WorldLayout.YardPart(slot);
                Assert.AreEqual(station.IsSmall ? 40 : 16, part.Width * part.Height, "a corner of 4x4, or 10x4 along a side");
                var origin = WorldLayout.EstateOrigin(district, slot);
                Assert.IsTrue(yard.Contains(origin.Offset(part.X0, part.Y0)));
            }
        }
    }

    [TestMethod]
    public void TheYardGateIsOnTheEstateNextToItsYard()
    {
        var district = new DistrictCoord(2, -1);
        for (var slot = 0; slot < WorldLayout.EstatesPerDistrict; slot++)
        {
            var gate = WorldLayout.YardGate(district, slot);
            var zone = WorldLayout.ZoneAt(gate.X, gate.Y);
            Assert.AreEqual(ZoneKind.Estate, zone.Kind, $"slot {slot}");
            Assert.AreEqual(slot, zone.Slot);
            var yard = WorldLayout.Yard(WorldLayout.StationOf(district, slot));
            var next = TestGame.Neighbours(gate).Count(yard.Contains);
            Assert.AreEqual(1, next, $"slot {slot}: right next to the Yard");
            Assert.IsTrue(zone.LocalX is >= 3 and <= 28 && zone.LocalY is >= 3 and <= 28, "a short pipe from the start parcels");
        }
    }

    [TestMethod]
    public void ParcelsAreNumberedRowByRow()
    {
        Assert.AreEqual(0, WorldLayout.ParcelOf(0, 0));
        Assert.AreEqual(3, WorldLayout.ParcelOf(31, 0));
        Assert.AreEqual(5, WorldLayout.ParcelOf(8, 8));
        Assert.AreEqual(15, WorldLayout.ParcelOf(31, 31));
        Assert.AreEqual((16, 8), WorldLayout.ParcelOrigin(6));
    }

    [TestMethod]
    public void DistrictsOpenInRingsAroundTheCapital()
    {
        var order = WorldLayout.DistrictOrder().Take(25).ToList();

        Assert.AreEqual(new DistrictCoord(0, 0), order[0]);
        Assert.HasCount(25, order.Distinct().ToList());
        Assert.IsTrue(order.Skip(1).Take(8).All(t => Math.Max(Math.Abs(t.X), Math.Abs(t.Y)) == 1));
        Assert.IsTrue(order.Skip(9).All(t => Math.Max(Math.Abs(t.X), Math.Abs(t.Y)) == 2));
    }
}

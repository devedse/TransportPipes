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
    public void GatesAreOnTheEstateBorderNextToACorridor()
    {
        var district = new DistrictCoord(-1, 3);
        foreach (var side in Enum.GetValues<GateSide>())
        {
            var gate = WorldLayout.GatePosition(district, 7, side);
            var outside = WorldLayout.OutsideGate(district, 7, side);
            Assert.AreEqual(ZoneKind.Estate, WorldLayout.ZoneAt(gate.X, gate.Y).Kind);
            Assert.AreEqual(ZoneKind.Corridor, WorldLayout.ZoneAt(outside.X, outside.Y).Kind);
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

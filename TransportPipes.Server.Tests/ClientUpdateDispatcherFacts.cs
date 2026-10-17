using TransportPipes.Common.Helpers;
using TransportPipes.Server.Services;

namespace TransportPipes.Server.Tests;

[TestClass]
public class ClientUpdateDispatcherFacts
{
    [TestMethod]
    public void ATileInTheMiddleOfAChunkOnlyRefreshesThatChunk()
    {
        var chunks = new HashSet<Point>();

        ClientUpdateDispatcher.AddChunksThatShowThisTile(chunks, 12, 13);

        CollectionAssert.AreEquivalent(new[] { new Point(1, 1) }, chunks.ToList());
    }

    [TestMethod]
    public void ATileInTheCornerOfAChunkAlsoRefreshesTheNeighbouringChunks()
    {
        var chunks = new HashSet<Point>();

        ClientUpdateDispatcher.AddChunksThatShowThisTile(chunks, 0, 0);
        //Adding the same tile twice does not add duplicate chunks
        ClientUpdateDispatcher.AddChunksThatShowThisTile(chunks, 0, 0);

        CollectionAssert.AreEquivalent(new[] { new Point(0, 0), new Point(-1, 0), new Point(0, -1) }, chunks.ToList());
    }

    [TestMethod]
    public void NegativeTilesAreInTheRightChunk()
    {
        var chunks = new HashSet<Point>();

        ClientUpdateDispatcher.AddChunksThatShowThisTile(chunks, -1, -9);

        CollectionAssert.AreEquivalent(new[] { new Point(-1, -2), new Point(0, -2), new Point(-1, -1) }, chunks.ToList());
    }
}

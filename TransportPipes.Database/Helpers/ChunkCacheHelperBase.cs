using TransportPipes.Database.Entities;
using TransportPipes.Database.Entities.ExtraObjects;

namespace TransportPipes.Database.Helpers;

public abstract class ChunkCacheHelperBase
{
    public abstract Chunk GetChunk(int xChunk, int yChunk);
    public abstract DbGameObject? GetDbGameObject(int x, int y);

    public List<DbGameObject?> GetNeighbouringTiles(int curX, int curY)
    {
        var left = GetDbGameObject(curX - 1, curY);
        var top = GetDbGameObject(curX, curY - 1);
        var right = GetDbGameObject(curX + 1, curY);
        var bottom = GetDbGameObject(curX, curY + 1);

        var listOfNeighbours = new List<DbGameObject?> { left, top, right, bottom };

        return listOfNeighbours;
    }
}

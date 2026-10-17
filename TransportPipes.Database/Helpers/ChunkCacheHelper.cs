using TransportPipes.Common.Helpers;
using TransportPipes.Database.Entities;
using TransportPipes.Database.Entities.ExtraObjects;

namespace TransportPipes.Database.Helpers;

/// <summary>
/// This class is made to cache chunks when requesting data
/// </summary>
public class ChunkCacheHelper : ChunkCacheHelperBase
{
    private readonly ITransportDatabase _transportDatabase;
    private readonly Dictionary<(int X, int Y), Chunk> _cachedChunks = new Dictionary<(int X, int Y), Chunk>();

    public ChunkCacheHelper(ITransportDatabase transportDatabase)
    {
        this._transportDatabase = transportDatabase;
    }

    public override Chunk GetChunk(int xChunk, int yChunk)
    {
        var chunkKey = (xChunk, yChunk);

        if (_cachedChunks.TryGetValue(chunkKey, out var chunkRetrieved))
        {
            return chunkRetrieved;
        }

        var chunk = _transportDatabase.GetChunk(xChunk, yChunk);
        _cachedChunks.Add(chunkKey, chunk);
        return chunk;
    }

    public override DbGameObject? GetDbGameObject(int x, int y)
    {
        var pos = GenericMathHelper.GetChunkForPos(x, y);
        var chunk = GetChunk(pos.X, pos.Y);

        var posInChunk = GenericMathHelper.ConvertPosToPosInChunk(x, y);
        return chunk.Data[posInChunk.X, posInChunk.Y];
    }
}

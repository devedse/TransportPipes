using TransportPipes.Common;
using TransportPipes.Common.Helpers;

namespace TransportPipes.Database.Entities.ExtraObjects;

public class Chunk
{
    private readonly DbGameObject?[,] _data;

    public DbGameObject?[,] Data { get { return _data; } }

    public Chunk(List<DbGameObject> gameObjects)
    {
        _data = new DbGameObject?[StaticConfigInfo.ChunkWidth, StaticConfigInfo.ChunkHeight];

        foreach (var gameObject in gameObjects)
        {
            var posInChunk = GenericMathHelper.ConvertPosToPosInChunk(gameObject.X, gameObject.Y);

            _data[posInChunk.X, posInChunk.Y] = gameObject;
        }
    }
}

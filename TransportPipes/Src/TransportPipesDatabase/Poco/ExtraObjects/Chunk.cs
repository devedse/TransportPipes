using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransportPipesCommon;
using TransportPipesCommon.Helpers;

namespace TransportPipesDatabase.Poco.ExtraObjects
{
    public class Chunk
    {
        private readonly DbGameObject[,] _data;

        public DbGameObject[,] Data { get { return _data; } }

        public Chunk(List<DbGameObject> gameObjects)
        {
            _data = new DbGameObject[StaticConfigInfo.ChunkWidth, StaticConfigInfo.ChunkHeight];

            foreach (var gameObject in gameObjects)
            {
                var posInChunk = GenericMathHelper.ConvertPosToPosInChunk(gameObject.X, gameObject.Y);

                _data[posInChunk.X, posInChunk.Y] = gameObject;
            }
        }
    }
}

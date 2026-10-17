using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransportPipesCommon.Helpers;
using TransportPipesDatabase;
using TransportPipesDatabase.Poco;
using TransportPipesDatabase.Poco.ExtraObjects;

namespace TransportPipesDatabase.Helpers
{
    /// <summary>
    /// This class is made to cache chunks when requesting data
    /// </summary>
    public class ChunkCacheHelper : ChunkCacheHelperBase
    {
        private ITransportDatabase _transportDatabase;
        private Dictionary<string, Chunk> _cachedChunks = new Dictionary<string, Chunk>();

        public ChunkCacheHelper(ITransportDatabase transportDatabase)
        {
            this._transportDatabase = transportDatabase;
        }

        public override Chunk GetChunk(int xChunk, int yChunk)
        {
            //TODO: Optimize the string key to something better
            string chunkKey = xChunk + "_" + yChunk;

            Chunk chunkRetrieved;
            var obtainedChunk = _cachedChunks.TryGetValue(chunkKey, out chunkRetrieved);

            if (obtainedChunk)
            {
                return chunkRetrieved;
            }
            else
            {
                var chunk = _transportDatabase.GetChunk(xChunk, yChunk);
                _cachedChunks.Add(chunkKey, chunk);
                return chunk;
            }
        }

        public override DbGameObject GetDbGameObject(int x, int y)
        {
            var pos = GenericMathHelper.GetChunkForPos(x, y);
            var chunk = GetChunk(pos.X, pos.Y);

            var posInChunk = GenericMathHelper.ConvertPosToPosInChunk(x, y);
            return chunk.Data[posInChunk.X, posInChunk.Y];
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransportPipesDatabase.Poco;
using TransportPipesDatabase.Poco.ExtraObjects;

namespace TransportPipesDatabase.Helpers
{
    public abstract class ChunkCacheHelperBase
    {
        public abstract Chunk GetChunk(int xChunk, int yChunk);
        public abstract DbGameObject GetDbGameObject(int x, int y);

        public List<DbGameObject> GetNeighbouringTiles(int curX, int curY)
        {
            var left = GetDbGameObject(curX - 1, curY);
            var top = GetDbGameObject(curX, curY - 1);
            var right = GetDbGameObject(curX + 1, curY);
            var bottom = GetDbGameObject(curX, curY + 1);

            var listOfNeighbours = new List<DbGameObject> { left, top, right, bottom };

            return listOfNeighbours;
        }
    }
}

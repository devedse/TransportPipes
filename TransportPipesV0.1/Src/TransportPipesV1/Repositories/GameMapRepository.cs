using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TransportPipesV1.Helpers;
using TransportPipesV1.StaticConfig;

namespace TransportPipesV1.Repositories
{
    public class GameMapRepository
    {
        public int[,] GenerateChunkMap(int xChunk, int yChunk)
        {
            int[,] maparray = new int[StaticConfigInfo.ChunkWidth, StaticConfigInfo.ChunkHeight];

            for (int y = 0; y < StaticConfigInfo.ChunkHeight; y++)
            {
                for (int x = 0; x < StaticConfigInfo.ChunkWidth; x++)
                {
                    float xInMap = xChunk * StaticConfigInfo.ChunkWidth + x;
                    float yInMap = yChunk * StaticConfigInfo.ChunkHeight + y;

                    int noiseValue = Noise.GenerateTile(xInMap, yInMap);
                    maparray[x, y] = noiseValue;
                }
            }

            if (xChunk == 0 && yChunk == 0)
            {
                maparray[0, 0] = 3;
            }

            return maparray;
        }
    }
}
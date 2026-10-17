using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TransportPipesV1.Database;
using TransportPipesV1.Enums;
using TransportPipesV1.StaticConfig;

namespace TransportPipesV1.Repositories
{
    public class GameObjectsRepository
    {
        public GameObjectsEnumDb[,] GetGameObjectChunk(int chunkX, int chunkY)
        {
            var chunkname = chunkX + "," + chunkY;
            //lock (GameObjectsMockDb.GameObjectMap)
            //{
            if (!GameObjectsMockDb.GameObjectMap.ContainsKey(chunkname))
            {
                GameObjectsEnumDb[,] chunkData = new GameObjectsEnumDb[StaticConfigInfo.ChunkWidth, StaticConfigInfo.ChunkHeight];
                GameObjectsMockDb.GameObjectMap.Add(chunkname, chunkData);
            }
            return GameObjectsMockDb.GameObjectMap[chunkname];
            //}
        }

        public GameObjectsEnumDb GetGameObjectTile(int x, int y)
        {
            int chunkX = (int)Math.Floor(x / (float)StaticConfigInfo.ChunkWidth);
            int chunkY = (int)Math.Floor(y / (float)StaticConfigInfo.ChunkHeight);

            var chunk = GetGameObjectChunk(chunkX, chunkY);

            int xInChunk = x % StaticConfigInfo.ChunkWidth;
            int yInChunk = y % StaticConfigInfo.ChunkHeight;

            if (xInChunk < 0)
                xInChunk += StaticConfigInfo.ChunkWidth;

            if (yInChunk < 0)
                yInChunk += StaticConfigInfo.ChunkHeight;

            return chunk[xInChunk, yInChunk];
        }

        public void SetGameObjectTile(int x, int y, GameObjectsEnumDb tile)
        {
            int chunkX = (int)Math.Floor(x / (float)StaticConfigInfo.ChunkWidth);
            int chunkY = (int)Math.Floor(y / (float)StaticConfigInfo.ChunkHeight);

            var chunk = GetGameObjectChunk(chunkX, chunkY);

            int xInChunk = x % StaticConfigInfo.ChunkWidth;
            int yInChunk = y % StaticConfigInfo.ChunkHeight;

            if (xInChunk < 0)
                xInChunk += StaticConfigInfo.ChunkWidth;

            if (yInChunk < 0)
                yInChunk += StaticConfigInfo.ChunkHeight;

            chunk[xInChunk, yInChunk] = tile;
        }
    }
}
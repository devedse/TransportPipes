using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using TransportPipesV1.Enums;
using TransportPipesV1.Helpers;
using TransportPipesV1.Helpers.AStarPathfinding;
using TransportPipesV1.Helpers.AStarPathfindingV2;
using TransportPipesV1.Mappers;
using TransportPipesV1.Repositories;
using TransportPipesV1.StaticConfig;

namespace TransportPipesV1.Hubs
{
    public static class StaticInfo
    {
        public static List<string> ConnectedIds = new List<string>();
        public static int NumberOfMessages = 0;
    }

    public class DefaultHub : Hub
    {
        private GameMapRepository gameMapRepository;
        private GameObjectsRepository gameObjectsRepository;
        private DatabaseToGameObjectsMapper databaseToGameObjectsMapper;
        public DefaultHub()
        {
            gameMapRepository = new GameMapRepository();
            gameObjectsRepository = new GameObjectsRepository();
            databaseToGameObjectsMapper = new DatabaseToGameObjectsMapper(gameObjectsRepository);
        }

        public override System.Threading.Tasks.Task OnConnected()
        {
            StaticInfo.ConnectedIds.Add(Context.ConnectionId);
            return base.OnConnected();
        }

        public override System.Threading.Tasks.Task OnDisconnected()
        {
            StaticInfo.ConnectedIds.Remove(Context.ConnectionId);
            return base.OnDisconnected();
        }

        public void RequestChunk(int xChunk, int yChunk)
        {
            RequestChunkNow(xChunk, yChunk, false);
        }

        private void RequestChunkNow(int xChunk, int yChunk, Boolean updateForAll)
        {
            StaticInfo.NumberOfMessages++;

            int yMax = yChunk * StaticConfigInfo.ChunkHeight + StaticConfigInfo.ChunkHeight;
            int xMax = xChunk * StaticConfigInfo.ChunkWidth + StaticConfigInfo.ChunkWidth;


            var maparray = gameMapRepository.GenerateChunkMap(xChunk, yChunk);
            var gameObjectsData = databaseToGameObjectsMapper.LoadGameObjectsAndMap(xChunk, yChunk);


            String serverString = "Clients connected: " + StaticInfo.ConnectedIds.Count + "    Number of messages: " + StaticInfo.NumberOfMessages;

            if (updateForAll)
            {
                Clients.All.ServerHasSentBackChunkData(xChunk, yChunk, maparray, gameObjectsData, serverString);
            }
            else
            {
                Clients.Caller.ServerHasSentBackChunkData(xChunk, yChunk, maparray, gameObjectsData, serverString);
            }
        }

        public void PressingSpace(float x, float y)
        {
            AStarPathFinder pathFinder = new AStarPathFinder(new Noise());

            var start = new AStarPoint(0, 0);
            var end = new AStarPoint((int)Math.Floor(x), (int)Math.Floor(y));
            //var path = pathFinder.FindPath(start, end);

            FindPath(start, end);
        }

        public void PressingF(float x, float y)
        {
            SetTileAndUpdate(x, y, GameObjectsEnumDb.FactorySmall);
        }

        public void PressingW(float x, float y)
        {
            SetTileAndUpdate(x, y, GameObjectsEnumDb.WindMoily);
        }

        private void FindPath(AStarPoint start, AStarPoint end)
        {
            int startx = Math.Min(start.X, end.X);
            int starty = Math.Min(start.Y, end.Y);
            int endx = Math.Max(start.X, end.X);
            int endy = Math.Max(start.Y, end.Y);


            int diffx = endx - startx;
            int diffy = endy - starty;

            int nextwidth = IncreaseSize(diffx);
            int nextheight = IncreaseSize(diffy);

            if (nextwidth - diffx < 50)
            {
                nextwidth = IncreaseSize(diffx + 50);
            }
            if (nextheight - diffy < 50)
            {
                nextheight = IncreaseSize(diffy + 50);
            }

            //nextwidth = nextheight = Math.Max(nextwidth, nextheight);


            int newstartx = startx - ((nextwidth - diffx) / 2);
            int newstarty = starty - ((nextheight - diffy) / 2);

            //byte[,] data = new byte[nextwidth, nextheight];
            MyPathNode[,] pathNodes = new MyPathNode[nextwidth, nextheight];

            for (int y = 0; y < nextheight; y++)
            {
                for (int x = 0; x < nextwidth; x++)
                {
                    int curx = x + newstartx;
                    int cury = y + newstarty;
                    //data[x, y] = (byte)Noise.GenerateAStarTile(curx, cury);
                    pathNodes[x, y] = new MyPathNode() { X = x, Y = y, IsWall = Noise.GenerateAStarTile(curx, cury) == 0 };
                }
            }

            var pathFinder = new Helpers.AStarPathfindingV2.SpatialAStar<MyPathNode, object>(pathNodes);
            //var pathFinderFast = new AStarPathFinderFast(data);

            var newstart = new AStarPoint(start.X - newstartx, start.Y - newstarty);
            var newend = new AStarPoint(end.X - newstartx, end.Y - newstarty);

            var path = pathFinder.Search(newstart, newend, null);

            if (path != null)
            {
                foreach (var pathnode in path)
                {
                    SetTileAndUpdate(pathnode.X + newstartx, pathnode.Y + newstarty, GameObjectsEnumDb.GreyPipe);
                }
            }
        }

        private int IncreaseSize(int v)
        {

            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;
            //v = next power of 2 now;

            return v;
        }

        private void SetTileAndUpdate(float x, float y, GameObjectsEnumDb gameObject)
        {
            int floorx = (int)Math.Floor(x);
            int floory = (int)Math.Floor(y);


            int chunkX = (int)Math.Floor(x / (float)StaticConfigInfo.ChunkWidth);
            int chunkY = (int)Math.Floor(y / (float)StaticConfigInfo.ChunkHeight);

            gameObjectsRepository.SetGameObjectTile(floorx, floory, gameObject);
            RequestChunkNow(chunkX, chunkY, true);

            int procx = floorx % StaticConfigInfo.ChunkWidth;
            int procy = floory % StaticConfigInfo.ChunkHeight;

            if (procx < 0)
                procx += StaticConfigInfo.ChunkWidth;

            if (procy < 0)
                procy += StaticConfigInfo.ChunkHeight;

            if (procx == 0)
            {
                RequestChunkNow(chunkX - 1, chunkY, true);
            }
            if (procy == 0)
            {
                RequestChunkNow(chunkX, chunkY - 1, true);
            }
            if (procx == 7)
            {
                RequestChunkNow(chunkX + 1, chunkY, true);
            }
            if (procy == 7)
            {
                RequestChunkNow(chunkX, chunkY + 1, true);
            }
        }
    }

    public class MyPathNode : IPathNode<Object>
    {
        public Int32 X { get; set; }
        public Int32 Y { get; set; }
        public Boolean IsWall { get; set; }

        public bool IsWalkable(Object unused)
        {
            return !IsWall;
        }
    }
}
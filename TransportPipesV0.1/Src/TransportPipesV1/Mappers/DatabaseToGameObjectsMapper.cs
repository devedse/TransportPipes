using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TransportPipesV1.Enums;
using TransportPipesV1.Repositories;
using TransportPipesV1.StaticConfig;

namespace TransportPipesV1.Mappers
{
    public class DatabaseToGameObjectsMapper
    {
        private GameObjectsRepository gameObjectsRepository;
        public DatabaseToGameObjectsMapper(GameObjectsRepository gameObjectsRepository)
        {
            this.gameObjectsRepository = gameObjectsRepository;
        }

        public String[,] LoadGameObjectsAndMap(int xChunk, int yChunk)
        {
            var curChunkGameObjects = gameObjectsRepository.GetGameObjectChunk(xChunk, yChunk);
            String[,] gameObjectsData = new String[StaticConfigInfo.ChunkWidth, StaticConfigInfo.ChunkHeight];

            for (int y = 0; y < curChunkGameObjects.GetLength(1); y++)
            {
                for (int x = 0; x < curChunkGameObjects.GetLength(0); x++)
                {
                    var curGameObject = curChunkGameObjects[x, y];

                    int realX = xChunk * StaticConfigInfo.ChunkWidth + x;
                    int realY = yChunk * StaticConfigInfo.ChunkHeight + y;

                    GameObjectsEnumClient curGameObjectForStringification = GameObjectsEnumClient.none;
                    switch (curGameObject)
                    {
                        case GameObjectsEnumDb.FactorySmall:
                            curGameObjectForStringification = GameObjectsEnumClient.factorysmall;
                            break;
                        case GameObjectsEnumDb.WindMoily:
                            curGameObjectForStringification = GameObjectsEnumClient.windmoily;
                            break;
                        case GameObjectsEnumDb.GreyPipe:
                            String toadd = "greypipe";

                            if (gameObjectsRepository.GetGameObjectTile(realX, realY - 1) == GameObjectsEnumDb.GreyPipe)
                            {
                                toadd += "u";
                            }
                            if (gameObjectsRepository.GetGameObjectTile(realX + 1, realY) == GameObjectsEnumDb.GreyPipe)
                            {
                                toadd += "r";
                            }
                            if (gameObjectsRepository.GetGameObjectTile(realX, realY + 1) == GameObjectsEnumDb.GreyPipe)
                            {
                                toadd += "d";
                            }
                            if (gameObjectsRepository.GetGameObjectTile(realX - 1, realY) == GameObjectsEnumDb.GreyPipe)
                            {
                                toadd += "l";
                            }

                            if (toadd == "greypipeu" || toadd == "greypiped" || toadd == "greypipe")
                                toadd = "greypipeud";

                            if (toadd == "greypiper" || toadd == "greypipel")
                                toadd = "greypiperl";

                            curGameObjectForStringification = (GameObjectsEnumClient)Enum.Parse(typeof(GameObjectsEnumClient), toadd);
                            break;
                        default:
                            break;
                    }

                    gameObjectsData[x, y] = curGameObjectForStringification.ToString();
                }
            }

            return gameObjectsData;
        }
    }
}
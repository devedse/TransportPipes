using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TransportPipes.Backend.Database;
using TransportPipes.Backend.Helpers;
using TransportPipesCommon;
using TransportPipesCommon.Helpers;

namespace TransportPipes.Backend.GameLogicObjects
{
    public class BackendChunk
    {
        private BackendGameObject[,] _data;

        public BackendGameObject[,] Data { get { return _data; } }

        public BackendChunk(List<DbGameObject> gameObjects)
        {
            _data = new BackendGameObject[StaticConfigInfo.ChunkWidth, StaticConfigInfo.ChunkHeight];

            foreach (var gameObject in gameObjects)
            {
                BackendGameObject backendGameObject = new BackendGameObject(gameObject);

                var posInChunk = GenericMathHelper.ConvertPosToPosInChunk(gameObject.X, gameObject.Y);

                _data[posInChunk.X, posInChunk.Y] = backendGameObject;
            }
        }
    }
}
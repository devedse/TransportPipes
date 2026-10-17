using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TransportPipes.Backend.Database;
using TransportPipes.Backend.GameLogicObjects;
using TransportPipes.Backend.Helpers;
using TransportPipesCommon.Enums;
using TransportPipesCommon.Helpers;

namespace TransportPipes.Backend.Repositories
{
    public class CachedGameObjectsRepository
    {
        private Dictionary<String, BackendChunk> CachedChunks = new Dictionary<string, BackendChunk>();

        private GameObjectsDatabaseRepository _gameObjectsDatabaseRepository;

        public CachedGameObjectsRepository(GameObjectsDatabaseRepository gameObjectsDatabaseRepository)
        {
            this._gameObjectsDatabaseRepository = gameObjectsDatabaseRepository;
        }

        public BackendChunk GetChunk(int chunkX, int chunkY, SuperLocker superLock)
        {
            superLock.RequestReadLock();

            var key = chunkX + "_" + chunkY;
            BackendChunk chunkRetrieved;
            var obtainedChunk = CachedChunks.TryGetValue(key, out chunkRetrieved);
            if (!obtainedChunk)
            {
                superLock.RequestWriteLock(); //Request the write lock and verify if the chunk still isnt here

                obtainedChunk = CachedChunks.TryGetValue(key, out chunkRetrieved); //TODO: We need to check if this functionality is needed somewhere else (and maybe also extract to other method to make it more clear)
                if (!obtainedChunk)
                {
                    chunkRetrieved = _gameObjectsDatabaseRepository.GetChunk(chunkX, chunkY, superLock);

                    foreach (var backendGameObject in chunkRetrieved.Data)
                    {
                        if (backendGameObject != null) //Don't do this for empty tiles
                        {
                            RecursiveConnectionLoader(backendGameObject.DbGameObject, superLock);
                        }
                    }

                    CachedChunks.Add(key, chunkRetrieved);
                }
            }
            return chunkRetrieved;
        }

        /// <summary>
        /// This method loops through all 
        /// </summary>
        /// <param name="dbGameObject"></param>
        private void RecursiveConnectionLoader(DbGameObject dbGameObject, SuperLocker superLock)
        {
            superLock.RequestReadLock();

            var allDestinationSources = dbGameObject.Destinations.SelectMany(t => t.Sources).Where(t => t.BackendGameObject == null);
            var allSourceDestinations = dbGameObject.Sources.SelectMany(t => t.Destinations).Where(t => t.BackendGameObject == null);

            foreach (var unloadedDbGameObject in allDestinationSources)
            {
                GetGameObject(unloadedDbGameObject.X, unloadedDbGameObject.Y, superLock);
            }

            foreach (var unloadedDbGameObject in allSourceDestinations)
            {
                GetGameObject(unloadedDbGameObject.X, unloadedDbGameObject.Y, superLock);
            }
        }

        public BackendGameObject GetGameObject(int x, int y, SuperLocker superLock)
        {
            var chunkToGet = GenericMathHelper.GetChunkForPos(x, y);
            var chunk = GetChunk(chunkToGet.X, chunkToGet.Y, superLock);

            var posInChunk = GenericMathHelper.ConvertPosToPosInChunk(x, y);
            superLock.RequestReadLock(); //Not needed but we do this for consistency (the lock is already requested with GetChunk but still)
            return chunk.Data[posInChunk.X, posInChunk.Y];
        }

        public BackendGameObject SetGameObject(int x, int y, GameObjectType gameObjectType, DbUser owner, SuperLocker superLock)
        {
            var chunkToGet = GenericMathHelper.GetChunkForPos(x, y);

            superLock.RequestWriteLock(); //Request write lock here so we are sure the thing we are going to modify isn't getting modified by something else.
            var chunk = GetChunk(chunkToGet.X, chunkToGet.Y, superLock);

            var posInChunk = GenericMathHelper.ConvertPosToPosInChunk(x, y);
            if (chunk.Data[posInChunk.X, posInChunk.Y] == null)
            {
                var newObject = _gameObjectsDatabaseRepository.SetGameObject(x, y, gameObjectType, owner, superLock); //Will request write lock itself
                //We already have writer lock here
                chunk.Data[posInChunk.X, posInChunk.Y] = newObject;
                return newObject;
            }
            return null;
        }

        public BackendGameObject DeleteGameObject(int x, int y, SuperLocker superLock)
        {
            var chunkToGet = GenericMathHelper.GetChunkForPos(x, y);

            //Request writer lock here because we are going to modify this chunk
            superLock.RequestWriteLock();

            var chunk = GetChunk(chunkToGet.X, chunkToGet.Y, superLock);

            var posInChunk = GenericMathHelper.ConvertPosToPosInChunk(x, y);
            var possibleGameObject = chunk.Data[posInChunk.X, posInChunk.Y];

            if (possibleGameObject != null)
            {
                _gameObjectsDatabaseRepository.DeleteGameObject(x, y, superLock); //Will request write lock itself

                superLock.RequestWriteLock();
                chunk.Data[posInChunk.X, posInChunk.Y] = null;
                return possibleGameObject;
            }
            return null;
        }
    }
}
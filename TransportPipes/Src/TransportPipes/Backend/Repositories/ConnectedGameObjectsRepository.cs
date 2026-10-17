using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TransportPipes.Backend.Database;
using TransportPipes.Backend.GameLogicObjects;
using TransportPipes.Backend.Helpers;
using TransportPipesCommon.Enums;

namespace TransportPipes.Backend.Repositories
{
    public class ConnectedGameObjectsRepository
    {
        private CachedGameObjectsRepository _cachedGameObjectsRepository;
        private ConnectionFinder _connectionFinder;

        public ConnectedGameObjectsRepository(CachedGameObjectsRepository cachedGameObjectsRepository, ConnectionFinder connectionFinder)
        {
            this._cachedGameObjectsRepository = cachedGameObjectsRepository;
            this._connectionFinder = connectionFinder;
        }

        public BackendChunk GetChunk(int chunkX, int chunkY, SuperLocker superLock)
        {
            superLock.RequestReadLock();

            var foundChunk = this._cachedGameObjectsRepository.GetChunk(chunkX, chunkY, superLock);

            foreach (var gameObject in foundChunk.Data)
            {
                //Check for all game objects that are not pipes if all connections are loaded
                if (gameObject != null && !gameObject.IsPipe && !gameObject.HasLoadedConnections)
                {
                    superLock.RequestWriteLock(); //This locking stuff is needed because it could theoretically happen that we have 2 read threads that obtain the same chunk but then
                    //we find out that the connections are missing. This means we need to determine them, but only once, not twice, so we want to make sure we only have one thread 
                    //running here and then start determining this stuff. The second thread will then wait till this is finished and boom it works :)
                    if (gameObject != null && !gameObject.IsPipe && !gameObject.HasLoadedConnections)
                    {
                        _connectionFinder.DetermineConnections(gameObject, false, superLock);
                    }
                }
            }


            //Finally do another check to fix broken pipes
            foreach (var gameObject in foundChunk.Data)
            {
                //Check pipes to resolve any issues there that might occur due to saving in the database
                if (gameObject != null && gameObject.IsPipe && !gameObject.HasLoadedConnections)
                {
                    superLock.RequestWriteLock();
                    if (gameObject != null && gameObject.IsPipe && !gameObject.HasLoadedConnections)
                    {
                        Console.WriteLine("Detected unnasigned pipe, trying to see if this happened due to partial save in the database");
                        _connectionFinder.DetermineConnections(gameObject, false, superLock);
                    }
                }
            }

            return foundChunk;
        }

        public void SetGameObject(int x, int y, GameObjectType gameObjectType, DbUser owner, SuperLocker superLock)
        {
            var newGameObject = _cachedGameObjectsRepository.SetGameObject(x, y, gameObjectType, owner, superLock);

            if (newGameObject != null)
            {
                _connectionFinder.DetermineConnections(newGameObject, true, superLock);
            }
        }

        public BackendGameObject GetGameObject(int x, int y, SuperLocker superLock)
        {
            var obtainedGameObject = _cachedGameObjectsRepository.GetGameObject(x, y, superLock);
            if (obtainedGameObject != null && !obtainedGameObject.HasLoadedConnections)
            {
                _connectionFinder.DetermineConnections(obtainedGameObject, false, superLock);
            }

            return obtainedGameObject;
        }

        public void DeleteGameObject(int x, int y, SuperLocker superLock)
        {
            DbGlobalConnection globalConnectionToDelete = null;
            var obtainedGameObject = _cachedGameObjectsRepository.GetGameObject(x, y, superLock);
            if (obtainedGameObject != null)
            {
                //We need to manually obtain the GlobalConnectionId because if we remove it first this thing is not filled in in the database anymore
                globalConnectionToDelete = obtainedGameObject.DbGameObject.GlobalConnection;

                var deletedGameObject = _cachedGameObjectsRepository.DeleteGameObject(x, y, superLock);

                if (deletedGameObject != null)
                {
                    _connectionFinder.RemoveConnectionsForGameObject(deletedGameObject.DbGameObject, globalConnectionToDelete, superLock);
                }
                else
                {
                    throw new Exception("This can't happen: 18231823821");
                }
            }
        }
    }
}
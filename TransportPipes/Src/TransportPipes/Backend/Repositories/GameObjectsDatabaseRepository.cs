using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TransportPipes.Backend.Database;
using TransportPipes.Backend.GameLogicObjects;
using TransportPipesCommon.Enums;

namespace TransportPipes.Backend.Repositories
{
    public class GameObjectsDatabaseRepository
    {
        private TransportDb _transportDb;

        public GameObjectsDatabaseRepository(TransportDb transportDb)
        {
            this._transportDb = transportDb;
        }

        public BackendChunk GetChunk(int chunkX, int chunkY, SuperLocker superLock)
        {
            superLock.RequestWriteLock(); //Any database action needs WriterLock currently

            var foundGameObjects = _transportDb.GameObjects.Include("Sources.Destinations").Include("Destinations.Sources").Include("Owner").Include("GlobalConnection").Where(t => t.ChunkX == chunkX && t.ChunkY == chunkY).ToList();

            var createdChunk = new BackendChunk(foundGameObjects);
            return createdChunk;
        }

        public BackendGameObject SetGameObject(int x, int y, GameObjectType gameObjectType, DbUser owner, SuperLocker superLock)
        {
            superLock.RequestWriteLock(); //Any database action needs WriterLock currently

            var currentObject = _transportDb.GameObjects.FirstOrDefault(t => t.X == x && t.Y == y);

            if (currentObject == null)
            {
                var newGameObject = new DbGameObject(x, y, gameObjectType, owner);
                _transportDb.GameObjects.Add(newGameObject);
                _transportDb.SaveChanges();
                return new BackendGameObject(newGameObject);
            }
            return null;
        }

        public void DeleteGameObject(int x, int y, SuperLocker superLock)
        {
            superLock.RequestWriteLock(); //Any database action needs WriterLock currently

            var foundObject = _transportDb.GameObjects.FirstOrDefault(t => t.X == x && t.Y == y);
            if (foundObject != null)
            {
                _transportDb.GameObjects.Remove(foundObject);
                _transportDb.SaveChanges();
            }
        }
    }
}
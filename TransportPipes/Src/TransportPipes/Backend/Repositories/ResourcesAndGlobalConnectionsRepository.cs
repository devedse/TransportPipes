using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TransportPipes.Backend.Calculations;
using TransportPipes.Backend.Database;
using TransportPipes.Backend.Database.HelperMappers;
using TransportPipes.Backend.GameLogicObjects;
using TransportPipes.Backend.Helpers;

namespace TransportPipes.Backend.Repositories
{
    public class ResourcesAndGlobalConnectionsRepository
    {
        private TransportDb _transportDb;
        private ConnectedGameObjectsRepository _connectedGameObjectsRepository;

        public ResourcesAndGlobalConnectionsRepository(TransportDb transportDb, ConnectedGameObjectsRepository connectedGameObjectsRepository)
        {
            this._transportDb = transportDb;
            this._connectedGameObjectsRepository = connectedGameObjectsRepository;
        }

        public List<DbUser> GetAffectedUsersBasedOnDeletion(int x, int y, SuperLocker superLock)
        {
            superLock.RequestWriteLock();
            var gameObjectThatWillBeDeleted = _connectedGameObjectsRepository.GetGameObject(x, y, superLock);
            if (gameObjectThatWillBeDeleted == null || gameObjectThatWillBeDeleted.DbGameObject.GlobalConnection == null)
            {
                return new List<DbUser>();
            }
            var globalConnectionThatWillBeDeleted = gameObjectThatWillBeDeleted.DbGameObject.GlobalConnection.ID;
            var affectedUsers = StaticRepositoryProvider.TransportDb.GlobalConnections.Where(t => t.ID == globalConnectionThatWillBeDeleted).SelectMany(t => t.DbGameObjects.Select(z => z.Owner)).Distinct();
            return affectedUsers.ToList();
        }

        public List<DbUser> GetAffectedUsersBasedOnAdd(int x, int y, SuperLocker superLock)
        {
            superLock.RequestWriteLock();

            var globalConnectionsOfSurroundingTiles = new List<BackendGameObject>()
                {
                    _connectedGameObjectsRepository.GetGameObject(x - 1, y, superLock),
                    _connectedGameObjectsRepository.GetGameObject(x + 1, y, superLock),
                    _connectedGameObjectsRepository.GetGameObject(x, y - 1, superLock),
                    _connectedGameObjectsRepository.GetGameObject(x, y + 1, superLock)
                }
                .Where(t => t != null && t.DbGameObject.GlobalConnection != null) //GlobalConnection can be null when there is a lonely windmill in the desert :)
                .Select(t => t.DbGameObject.GlobalConnection.ID).ToList();

            var affectedUsers = StaticRepositoryProvider.TransportDb.GlobalConnections
                .Where(t => globalConnectionsOfSurroundingTiles.Any(z => z == t.ID))
                .SelectMany(t => t.DbGameObjects.Select(z => z.Owner))
                .Distinct();

            return affectedUsers.ToList();
        }

        public void UpdateGoldForGlobalConnectionOfThisGameObject(DateTime currentDateTime, BackendGameObject gameObject, SuperLocker superLock)
        {
            superLock.RequestWriteLock();

            var totalCalculationForThis = new TotalCalculationObject();

            var previousUpdateDateTime = gameObject.DbGameObject.GlobalConnection.LastCalculationUpdate.Value;
            var timePassed = currentDateTime - previousUpdateDateTime;
            var secondsPassedRoundedDown = (int)timePassed.TotalSeconds;

            if (secondsPassedRoundedDown != 0)
            {
                gameObject.SendOutMyRequestsIfNeeded(secondsPassedRoundedDown, totalCalculationForThis);
                foreach (var user in totalCalculationForThis.PlayersAndMoney)
                {
                    var userThatObtainedGold = user.Key;
                    var obtainedGoldValue = user.Value;
                    userThatObtainedGold.Gold += obtainedGoldValue;

                    //Clients.Caller.NotifyMessage2(userThatObtainedGold.UserName + " obtained " + obtainedGoldValue + " gold.", 5000);
                }

                var newLastUpdateBasedOnRoundedSeconds = previousUpdateDateTime + TimeSpan.FromSeconds(secondsPassedRoundedDown);
                gameObject.DbGameObject.GlobalConnection.LastCalculationUpdate = newLastUpdateBasedOnRoundedSeconds;

                StaticRepositoryProvider.TransportDb.SaveChanges();
            }
        }

        public Dictionary<DbUser, double> UpdateGoldGlobalConnectionsForGameObjectsAndDetermineMoneyPerSecond(DateTime currentDateTime, List<RandomGameObjectInGlobalConnection> listWithOneGameObjectPerGlobalConnection, SuperLocker superLock)
        {
            var dictOfPlayersMoneyPerSecond = new Dictionary<DbUser, double>();

            foreach (var foundGameObjectInGlobalConnection in listWithOneGameObjectPerGlobalConnection)
            {
                var foundGameObject = _connectedGameObjectsRepository.GetGameObject(foundGameObjectInGlobalConnection.X, foundGameObjectInGlobalConnection.Y, superLock);

                UpdateGoldForGlobalConnectionOfThisGameObject(currentDateTime, foundGameObject, superLock);




                //also calculate how much I earn per tick after this
                var totalCalculation = new TotalCalculationObject();
                foundGameObject.SendOutMyRequestsIfNeeded(1, totalCalculation);

                foreach (var kvpMoney in totalCalculation.PlayersAndMoney)
                {
                    MoneyInDictionaryHelperOld.AddPlayerMoney(dictOfPlayersMoneyPerSecond, kvpMoney.Key, kvpMoney.Value);
                }
            }

            return dictOfPlayersMoneyPerSecond;
        }

        public List<RandomGameObjectInGlobalConnection> DetermineGlobalConnectionsGameObjectsForTheseUsers(List<DbUser> users, SuperLocker superLock)
        {
            var usersAsPrimitiveList = users.Select(t => t.ID);

            superLock.RequestWriteLock();
            var foundGameObjects = _transportDb.GlobalConnections
                .Where(t => t.DbGameObjects
                    .Any(z =>
                        usersAsPrimitiveList.Any(g => g == z.Owner.ID) &&  //Find globalconnections where any of the provided users match
                        ((int)z.GameObjectType) < 10000)) //Is not pipe
                .Select(t => t.DbGameObjects
                    .Where(z => ((int)z.GameObjectType) < 10000) //Check in where that it is not pipe too
                    .Select(z => new RandomGameObjectInGlobalConnection { X = z.X, Y = z.Y })
                    .FirstOrDefault()); //First or default required else it whines about that I can't do it :(

            var foundGameObjectsToList = foundGameObjects.ToList();
            return foundGameObjectsToList;
        }
    }
}
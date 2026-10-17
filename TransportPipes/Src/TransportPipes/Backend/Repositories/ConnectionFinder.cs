using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TransportPipes.Backend.Database;
using TransportPipes.Backend.GameLogicObjects;
using TransportPipes.Backend.Repositories;
using TransportPipesCommon.Repositories;

namespace TransportPipes.Backend.Repositories
{
    public class ConnectionFinder
    {
        private CachedGameObjectsRepository _cachedGameObjectsRepository;
        private TransportDb _transportDb;

        public ConnectionFinder(CachedGameObjectsRepository cachedGameObjectsRepository, TransportDb transportDb)
        {
            this._cachedGameObjectsRepository = cachedGameObjectsRepository;
            this._transportDb = transportDb;
        }

        private DbGlobalConnection CreateNewGlobalConnection()
        {
            return new DbGlobalConnection()
            {
                LastCalculationUpdate = DateTime.Now
            };
        }

        /// <summary>
        /// This does search to find out what game objects are connected to a specific game object
        /// </summary>
        /// <param name="gameObjectToStartWith"></param>
        public void DetermineConnections(BackendGameObject gameObjectToStartWith, Boolean isNewlyCreatedGameObject, SuperLocker superLock)
        {
            superLock.RequestWriteLock();

            var globalConnectionToUse = CreateNewGlobalConnection();

            var needsDetermineConnectionRecursive = CleanAllConnectionsOrAssign(gameObjectToStartWith, isNewlyCreatedGameObject, superLock);

            _transportDb.SaveChanges();

            //If we were able to fast assign a pipe to an existing global connection id we don't need to kick of this expensive connection finder
            if (needsDetermineConnectionRecursive)
            {
                DetermineConnectionsRecursive(gameObjectToStartWith, globalConnectionToUse, superLock);
            }

            _transportDb.SaveChanges();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="isNewlyCreatedGameObject">If true it will try to assign a pipe to the nearest pipes global connection id.</param>
        /// <param name="superLock">The locker</param>
        /// <returns></returns>
        private Boolean CleanAllConnectionsOrAssign(BackendGameObject gameObject, Boolean isNewlyCreatedGameObject, SuperLocker superLock)
        {
            var globalConnection = gameObject.DbGameObject.GlobalConnection;


            if (globalConnection != null)
            {
                //If we are not -1 we should clear our own connections
                CleanOutGlobalConnectionId(globalConnection, superLock);
            }
            else
            {

                //TODO somehow assign a pipe to the same global Connection id if you place it next to another pipe
                //TODO when you place a building next to a pipe with a connection id also clean out that connection ID
                //So basically make this method something like -> Cleanout Global Connection ID

                if (gameObject.IsPipe)
                {
                    int curX = gameObject.X;
                    int curY = gameObject.Y;

                    var listOfNeighbours = GetNeighbouringTiles(curX, curY, superLock);

                    if (isNewlyCreatedGameObject)
                    {

                        var neighbouringGlobalConnection = listOfNeighbours.Where(t => t != null).Select(t => t.DbGameObject.GlobalConnection).Distinct().ToList();
                        if (neighbouringGlobalConnection.Count == 1 && false) //Currently disabled because it checks for global connection id not connection id.
                        {
                            gameObject.HasLoadedConnections = true;
                            gameObject.DbGameObject.GlobalConnection = neighbouringGlobalConnection.First();
                            return false;
                        }
                        else
                        {
                            //Clean our neighbours now that are not in any connection
                            foreach (var neightbourGameObject in listOfNeighbours.Where(t => t != null && t.DbGameObject.GlobalConnection != null))
                            {
                                CleanOutGlobalConnectionId(neightbourGameObject.DbGameObject.GlobalConnection, superLock);
                            }
                        }
                    }
                    else
                    {
                        //Clean our neighbours now that are not in any connection
                        foreach (var neightbourGameObject in listOfNeighbours.Where(t => t != null && t.DbGameObject.GlobalConnection != null))
                        {
                            CleanOutGlobalConnectionId(neightbourGameObject.DbGameObject.GlobalConnection, superLock);
                        }
                    }
                }
                else
                {
                    int curX = gameObject.X;
                    int curY = gameObject.Y;

                    var listOfNeighbours = GetNeighbouringTiles(curX, curY, superLock);

                    //Only clean our neighbouring pipes here because 2 buildings next to eachother are not connected as by design
                    foreach (var neightbourGameObject in listOfNeighbours.Where(t => t != null && t.IsPipe && t.DbGameObject.GlobalConnection != null))
                    {
                        CleanOutGlobalConnectionId(neightbourGameObject.DbGameObject.GlobalConnection, superLock);
                    }
                }
            }

            //Apparently we weren't able to quickly resolve the newly found item so we have to redetermine all connections
            return true;


        }

        public void RemoveConnectionsForGameObject(DbGameObject deletedGameObject, DbGlobalConnection globalConnectionToCleanOut, SuperLocker superLock)
        {
            superLock.RequestWriteLock();

            if (globalConnectionToCleanOut != null)
            {
                CleanOutGlobalConnectionId(globalConnectionToCleanOut, superLock);
                _transportDb.SaveChanges();
            }

            var neighbouringTiles = GetNeighbouringTiles(deletedGameObject.X, deletedGameObject.Y, superLock);

            foreach (var neighbourTile in neighbouringTiles)
            {
                //This works for all neighbouring tiles that have no connection yet (so that means only the ones we have just cleared)
                if (neighbourTile != null && !neighbourTile.HasLoadedConnections)
                {
                    var nextGlobalConnection = CreateNewGlobalConnection();
                    DetermineConnectionsRecursive(neighbourTile, nextGlobalConnection, superLock);
                }
            }

            _transportDb.SaveChanges();
        }

        private List<BackendGameObject> GetNeighbouringTiles(int curX, int curY, SuperLocker superLock)
        {
            var left = _cachedGameObjectsRepository.GetGameObject(curX - 1, curY, superLock);
            var top = _cachedGameObjectsRepository.GetGameObject(curX, curY - 1, superLock);
            var right = _cachedGameObjectsRepository.GetGameObject(curX + 1, curY, superLock);
            var bottom = _cachedGameObjectsRepository.GetGameObject(curX, curY + 1, superLock);

            var listOfNeighbours = new List<BackendGameObject>() { left, top, right, bottom };

            return listOfNeighbours;
        }

        private void CleanOutGlobalConnectionId(DbGlobalConnection globalConnection, SuperLocker superLock)
        {
            var allConnectionsWithThisGlobalId = _transportDb.Connections.Where(t => t.GlobalConnection.ID == globalConnection.ID);
            var gameObjectsWithThisGlobalId = _transportDb.GameObjects.Where(t => t.GlobalConnection.ID == globalConnection.ID);





            foreach (var itemWithThisGlobalConnectionId in gameObjectsWithThisGlobalId)
            {
                //itemWithThisGlobalConnectionId.GlobalConnectionId = -1;

                //Dirty and slow possibly:
                var matchingGameObject = _cachedGameObjectsRepository.GetGameObject(itemWithThisGlobalConnectionId.X, itemWithThisGlobalConnectionId.Y, superLock);
                matchingGameObject.HasLoadedConnections = false;
            }


            _transportDb.Connections.RemoveRange(allConnectionsWithThisGlobalId);

            _transportDb.SaveChanges();


            //foreach (var connection in allConnectionsWithThisGlobalId)
            //{
            //    foreach (var sourceChild in connection.Source)
            //    {
            //        sourceChild.GlobalConnectionId = -1;
            //        sourceChild.Source = null;
            //        sourceChild.Destination = null;
            //    }

            //    foreach (var destChild in connection.Destination)
            //    {
            //        destChild.GlobalConnectionId = -1;
            //        destChild.Source = null;
            //        destChild.Destination = null;
            //    }
            //}

            _transportDb.SaveChanges();

            _transportDb.GlobalConnections.Remove(globalConnection);

            _transportDb.SaveChanges();
        }

        private void DetermineConnectionsRecursive(BackendGameObject gameObjectCurrent, DbGlobalConnection globalConnection, SuperLocker superLock)
        {
            BackendGameObject startPoint;

            if (gameObjectCurrent.IsPipe)
            {
                //We have a pipe so we can start looking for any connections
                ExecuteForThisSpecificGameObject(globalConnection, gameObjectCurrent, superLock);
            }
            else
            {
                //Set the hasloaded to true so that we aren't going to execute this method again for this game object
                gameObjectCurrent.HasLoadedConnections = true;

                //Make sure we get pipes around us and go all directions to find our connections
                while ((startPoint = DetermineStartPoint(gameObjectCurrent, superLock)) != null)
                {

                    ExecuteForThisSpecificGameObject(globalConnection, startPoint, superLock);
                }
            }
        }

        private void ExecuteForThisSpecificGameObject(DbGlobalConnection globalConnection, BackendGameObject startPoint, SuperLocker superLock)
        {
            var gameObjectsToSearch = new List<BackendGameObject>();

            int pos = 0;
            gameObjectsToSearch.Add(startPoint);


            while (pos < gameObjectsToSearch.Count)
            {
                var gameObject = gameObjectsToSearch[pos];
                //If the gameobject has not loaded any connections or the GlobalConnection is still set to null we can use this object as a connection
                if ((!gameObject.HasLoadedConnections || gameObject.DbGameObject.GlobalConnection == null) && gameObject.IsPipe)
                {
                    if (gameObject.IsPipe) //Only set has loaded connections if it's a pipe because we don't know for sure for buildings untill we've finished the complete while loop above
                    {
                        gameObject.HasLoadedConnections = true;
                    }

                    var curX = gameObject.X;
                    var curY = gameObject.Y;
                    var left = _cachedGameObjectsRepository.GetGameObject(curX - 1, curY, superLock);
                    var top = _cachedGameObjectsRepository.GetGameObject(curX, curY - 1, superLock);
                    var right = _cachedGameObjectsRepository.GetGameObject(curX + 1, curY, superLock);
                    var bottom = _cachedGameObjectsRepository.GetGameObject(curX, curY + 1, superLock);

                    SeeIfNeedToAdd(left, gameObjectsToSearch);
                    SeeIfNeedToAdd(top, gameObjectsToSearch);
                    SeeIfNeedToAdd(right, gameObjectsToSearch);
                    SeeIfNeedToAdd(bottom, gameObjectsToSearch);
                }

                //Assign the globalConnectionId so that we know what connection this thing belongs to
                gameObject.DbGameObject.GlobalConnection = globalConnection;

                pos++;
            }

            var onlyGameObjects = gameObjectsToSearch.Where(t => t.IsPipe == false).ToList();

            //If we have no game objects, apparently there's nothing connected so we can leave the global connection ID at -1;
            if (onlyGameObjects.Count > 0)
            {
                var providers = new List<BackendGameObject>();
                var consumers = new List<BackendGameObject>();

                //TODO: This needs to be improved, it will start acting strange probably when a factory provides Iron that is connected to both a Solar Panel and something that consumer iron because it will be both a provider and a consumer then
                //Actually not sure but can't be bothered to improve right now
                foreach (var gameObject in onlyGameObjects)
                {
                    //if (gameObject.GenerationInfo.Count == 0)
                    //{
                    //    consumers.Add(gameObject);
                    //}

                    //if (

                    var gameObjectProductionInfo = GameResourcesRepository.DetermineGameObjectInfo(gameObject.GameObjectType).ProductionInfo;
                    var gameObjectConsumptionInfo= GameResourcesRepository.DetermineGameObjectInfo(gameObject.GameObjectType).ConsumptionInfo;

                    foreach (var resourceThisGenerates in gameObjectProductionInfo)
                    {
                        var destinations = onlyGameObjects.Where(t => GameResourcesRepository.DetermineGameObjectInfo(t.GameObjectType).ConsumptionInfo.Any(z => z.ResourceType == resourceThisGenerates.ResourceType)).ToList();

                        if (destinations.Count > 0)
                        {
                            if (!providers.Contains(gameObject))
                            {
                                providers.Add(gameObject);
                            }
                        }
                    }

                    foreach (var resourceThisNeeds in gameObjectConsumptionInfo)
                    {
                        var sources = onlyGameObjects.Where(t => GameResourcesRepository.DetermineGameObjectInfo(t.GameObjectType).ProductionInfo.Any(z => z.ResourceType == resourceThisNeeds.ResourceType)).ToList();

                        if (sources.Count > 0)
                        {
                            if (!consumers.Contains(gameObject))
                            {
                                consumers.Add(gameObject);
                            }
                        }
                    }
                }

                //Only create a connection if we actually can connect something
                if (providers.Any() && consumers.Any())
                {

                    var newConnection = new DbConnection();

                    newConnection.Sources = providers.Select(t => t.DbGameObject).ToList();
                    //foreach (var source in providers)
                    //{                        
                    //    newConnection.Sources.Add(source.DbGameObject);
                    //}

                    newConnection.Destinations = consumers.Select(t => t.DbGameObject).ToList();

                    //foreach (var dest in consumers)
                    //{
                    //    newConnection.Destinations.Add(dest.DbGameObject);
                    //}


                    newConnection.GlobalConnection = globalConnection;

                    _transportDb.Connections.Add(newConnection);

                }
                //Because the first thing we do is setting HasLoadedConnections to true we make sure that we don't loop through things multiple times here
                foreach (var gameObject in onlyGameObjects.Where(t => !t.HasLoadedConnections))
                {
                    DetermineConnectionsRecursive(gameObject, globalConnection, superLock);
                }
            }
            else
            {
                //If we didn't find any game object we revert the changes so these things can be reused
                gameObjectsToSearch.ForEach(t => t.DbGameObject.GlobalConnection = null);
            }
        }

        private BackendGameObject DetermineStartPoint(BackendGameObject gameObject, SuperLocker superLock)
        {
            var curX = gameObject.X;
            var curY = gameObject.Y;
            var left = _cachedGameObjectsRepository.GetGameObject(curX - 1, curY, superLock);
            var top = _cachedGameObjectsRepository.GetGameObject(curX, curY - 1, superLock);
            var right = _cachedGameObjectsRepository.GetGameObject(curX + 1, curY, superLock);
            var bottom = _cachedGameObjectsRepository.GetGameObject(curX, curY + 1, superLock);

            if (IsValidStartPoint(left)) return left;
            if (IsValidStartPoint(top)) return top;
            if (IsValidStartPoint(right)) return right;
            if (IsValidStartPoint(bottom)) return bottom;

            return null;
        }

        private Boolean IsValidStartPoint(BackendGameObject gameObject)
        {
            if (gameObject != null && gameObject.IsPipe == true && (gameObject.HasLoadedConnections == false || gameObject.DbGameObject.GlobalConnection == null))
            {
                return true;
            }
            return false;
        }


        private void SeeIfNeedToAdd(BackendGameObject gameObject, List<BackendGameObject> gameObjectList)
        {
            if (gameObject != null)
            {
                if (!gameObject.IsPipe || (gameObject.IsPipe && (!gameObject.HasLoadedConnections || gameObject.DbGameObject.GlobalConnection == null)))
                {
                    gameObjectList.Add(gameObject);
                }
            }
        }
    }
}
using TransportPipes.Common.Enums;
using TransportPipes.Common.Repositories;
using TransportPipes.Database;
using TransportPipes.Database.Entities;
using TransportPipes.Database.Entities.ExtraObjects;
using TransportPipes.Database.Helpers;

namespace TransportPipes.Actionizer.Calculators;

public class ConnectionFinder
{
    private readonly ITransportDatabase _transportDatabase;

    public ConnectionFinder(ITransportDatabase transportDatabase)
    {
        this._transportDatabase = transportDatabase;
    }

    /// <summary>
    /// Redetermines connections for GameObjects around this GameObject depending on whether it was a Pipe or not
    /// </summary>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="isPipe">If the removed GameObject was a pipe</param>
    /// <param name="timeOfCreation">The time these connections were created</param>
    /// <param name="chunkCache">The ChunkCache to use to obtain the tiles with, if empty, use the internal transport database to create a new ChunkCache</param>
    /// <returns>Returns a list of all created GlobalConnections (Does not have to be used)</returns>
    public List<List<HashSet<DbGameObject>>> RedetermineAllConnectionsForDeletedGameObject(int x, int y, bool isPipe, DateTime timeOfCreation, ChunkCacheHelperBase? chunkCache = null)
    {
        if (chunkCache == null)
        {
            chunkCache = new ChunkCacheHelper(_transportDatabase);
        }

        //Global Connection should already be cleared out before this method is called
        return RedetermineAllConnectionsInternal(chunkCache, x, y, isPipe, false, timeOfCreation);
    }

    /// <summary>
    /// Redetermines all connections for this tile.
    /// </summary>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="timeOfCreation">The time these connections were created</param>
    /// <param name="chunkCache">The ChunkCache to use to obtain the tiles with, if empty, use the internal transport database to create a new ChunkCache</param>
    /// <returns>Returns a list of all created GlobalConnections (Does not have to be used)</returns>
    public List<List<HashSet<DbGameObject>>> RedetermineAllConnectionsForGameObject(int x, int y, DateTime timeOfCreation, ChunkCacheHelperBase? chunkCache = null)
    {
        if (chunkCache == null)
        {
            chunkCache = new ChunkCacheHelper(_transportDatabase);
        }

        //Always work with latest object of db
        var gameObjectFromDb = chunkCache.GetDbGameObject(x, y)
            ?? throw new InvalidOperationException($"There is no GameObject at <{x},{y}>.");

        //The GlobalConnection needs to be cleared because it needs to make a new one
        return RedetermineAllConnectionsInternal(chunkCache, x, y, gameObjectFromDb.GameObjectType.IsPipe(), true, timeOfCreation);
    }


    internal List<List<HashSet<DbGameObject>>> RedetermineAllConnectionsInternal(ChunkCacheHelperBase chunkCache, int x, int y, bool isPipe, bool shouldClearOutGlobalConnection, DateTime timeOfCreation)
    {
        var gameObjectsAffected = new List<DbGameObject>();

        var thisGameObject = chunkCache.GetDbGameObject(x, y);
        if (thisGameObject != null)
        {
            //Also add the GameObject itself to ensure that when placing a lonely windmoily it will also get a GlobalConnection
            gameObjectsAffected.Add(thisGameObject);
        }

        if (isPipe)
        {
            //The removed GameObject was a pipe, this means that all surrounding game objects are affected
            gameObjectsAffected.AddRange(chunkCache.GetNeighbouringTiles(x, y).OfType<DbGameObject>());
        }
        else
        {
            //The removed GameObject was no pipe, this means that only pipe game objects are affected
            gameObjectsAffected.AddRange(chunkCache.GetNeighbouringTiles(x, y).OfType<DbGameObject>().Where(t => t.GameObjectType.IsPipe()));
        }

        if (shouldClearOutGlobalConnection)
        {
            _transportDatabase.ClearCompleteGlobalConnections(gameObjectsAffected);
        }

        //This method can return multiple global connections, e.g. if we remove a pipe in between 2 connections which will split them into 2 different ones
        var listOfGlobalConnections = ObtainListOfGlobalConnections(chunkCache, gameObjectsAffected);

        foreach (var listOfConnections in listOfGlobalConnections)
        {
            //For every global connection add it to the database
            var connectionsWithProvidersAndConsumersDetermined = DetermineProvidersAndConsumers(listOfConnections);

            _transportDatabase.AddConnections(connectionsWithProvidersAndConsumersDetermined, timeOfCreation);
        }

        return listOfGlobalConnections;
    }

    internal List<DbGameObjectsConnection> DetermineProvidersAndConsumers(List<HashSet<DbGameObject>> listOfConnection)
    {
        var allDbGameObjectsConnections = new List<DbGameObjectsConnection>();

        foreach (var connection in listOfConnection)
        {
            var providers = new List<DbGameObject>();
            var consumers = new List<DbGameObject>();


            var onlyGameObjects = connection.Where(t => !t.GameObjectType.IsPipe()).ToList();

            //TODO: This needs to be improved, it will start acting strange probably when a factory provides Iron that is connected to both a Solar Panel and something that consumer iron because it will be both a provider and a consumer then
            //Actually not sure but can't be bothered to improve right now
            foreach (var gameObject in onlyGameObjects)
            {
                var resourcesInfo = GameResourcesRepository.DetermineGameObjectInfo(gameObject.GameObjectType);

                foreach (var resourceThisGenerates in resourcesInfo.ProductionInfo)
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

                foreach (var resourceThisNeeds in resourcesInfo.ConsumptionInfo)
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

            if ((providers.Count == 0 && consumers.Count > 0) || (providers.Count > 0 && consumers.Count == 0))
            {
                throw new InvalidOperationException("Should not ever happen, Providers Count: " + providers.Count + " Consumers Count: " + consumers.Count);
            }

            var nonProviderConsumerBuildings = new List<DbGameObject>();
            if (providers.Count == 0 && consumers.Count == 0)
            {
                nonProviderConsumerBuildings.AddRange(onlyGameObjects);
            }

            var dbGameObjectsConnection = new DbGameObjectsConnection(providers, consumers, nonProviderConsumerBuildings, connection.Where(t => t.GameObjectType.IsPipe()).ToList());
            allDbGameObjectsConnections.Add(dbGameObjectsConnection);
        }
        return allDbGameObjectsConnections;
    }

    /// <summary>
    /// Obtains a list of connected game objects to the list of GameObjects provided
    /// </summary>
    /// <param name="chunkCache">The Caching mechanism to use to obtain GameObjects</param>
    /// <param name="gameObjects">The GameObjects to search connections for</param>
    /// <returns></returns>
    internal List<List<HashSet<DbGameObject>>> ObtainListOfGlobalConnections(ChunkCacheHelperBase chunkCache, List<DbGameObject> gameObjects)
    {
        var globalConnectionList = new List<List<HashSet<DbGameObject>>>();

        //Loop through all GameObjects provided and obtain connections for every one of them that has not been redetermined by this loop
        foreach (var gameObject in gameObjects)
        {
            var containsThisGameObjectAlready = globalConnectionList.Any(t => t.Any(z => z.Any(g => g.X == gameObject.X && g.Y == gameObject.Y)));
            if (!containsThisGameObjectAlready)
            {
                var obtainedConnections = ObtainListOfConnections(chunkCache, gameObject);
                globalConnectionList.Add(obtainedConnections);
            }
        }

        return globalConnectionList;
    }

    internal List<HashSet<DbGameObject>> ObtainListOfConnections(ChunkCacheHelperBase chunkCache, DbGameObject gameObject)
    {
        //We get the db object from the database to ensure we always work on the latest version of this game object
        var gameObjectFromDb = chunkCache.GetDbGameObject(gameObject.X, gameObject.Y)
            ?? throw new InvalidOperationException($"There is no GameObject at <{gameObject.X},{gameObject.Y}>.");

        var pipesToCheck = new Queue<DbGameObject>();

        //Either add this pipe to the pipesqueue or add all neighbouring pipes to it if it is not a pipe
        if (gameObjectFromDb.GameObjectType.IsPipe())
        {
            pipesToCheck.Enqueue(gameObjectFromDb);
        }
        else
        {
            var neighbouringPipes = chunkCache.GetNeighbouringTiles(gameObjectFromDb.X, gameObjectFromDb.Y).OfType<DbGameObject>().Where(t => t.GameObjectType.IsPipe());
            foreach (var pipe in neighbouringPipes)
            {
                pipesToCheck.Enqueue(pipe);
            }
        }

        //Walk through all these pipes and obtain all connections
        var allWalkedThroughGameObjectsTotal = new List<HashSet<DbGameObject>>();

        while (pipesToCheck.Count > 0)
        {
            var curPipe = pipesToCheck.Dequeue();

            //Ensure that we have not handled this pipe in any connection
            if (!allWalkedThroughGameObjectsTotal.Any(t => t.Any(z => z == curPipe)))
            {
                var walkedThroughGameObjects = FindGameObjectsInThisConnection(chunkCache, curPipe);
                allWalkedThroughGameObjectsTotal.Add(walkedThroughGameObjects);

                //Now add all potential new pipes to the list of pipes to check (these will be filtered if they are already processed)
                foreach (var gameObjectThatIsNotPipe in walkedThroughGameObjects.Where(t => !t.GameObjectType.IsPipe()))
                {
                    var neighbouringPipes = chunkCache.GetNeighbouringTiles(gameObjectThatIsNotPipe.X, gameObjectThatIsNotPipe.Y).OfType<DbGameObject>().Where(t => t.GameObjectType.IsPipe());
                    foreach (var pipe in neighbouringPipes)
                    {
                        pipesToCheck.Enqueue(pipe);
                    }
                }
            }
        }

        return allWalkedThroughGameObjectsTotal;
    }


    internal HashSet<DbGameObject> FindGameObjectsInThisConnection(ChunkCacheHelperBase chunkCache, DbGameObject pipe)
    {
        var pipeFromDatabase = chunkCache.GetDbGameObject(pipe.X, pipe.Y)
            ?? throw new InvalidOperationException($"There is no pipe at <{pipe.X},{pipe.Y}>.");

        //This is used to know what gameobjects we travelled
        var walkedThroughGameObjects = new HashSet<DbGameObject>();
        walkedThroughGameObjects.Add(pipeFromDatabase);

        //The current gameobject we are handling
        var gameObjectsQueue = new Queue<DbGameObject>();
        gameObjectsQueue.Enqueue(pipe);

        while (gameObjectsQueue.Count > 0)
        {
            var currentGameObject = gameObjectsQueue.Dequeue();
            var neighbours = chunkCache.GetNeighbouringTiles(currentGameObject.X, currentGameObject.Y);

            foreach (var neighbour in neighbours)
            {
                if (neighbour != null && !walkedThroughGameObjects.Contains(neighbour))
                {
                    walkedThroughGameObjects.Add(neighbour);
                    if (neighbour.GameObjectType.IsPipe())
                    {
                        gameObjectsQueue.Enqueue(neighbour);
                    }
                }
            }
        }

        return walkedThroughGameObjects;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TransportPipes.Backend.Database;
using TransportPipes.Backend.GameLogicObjects.Resources;
using TransportPipes.Backend.Helpers;
using TransportPipes.Backend.Repositories;
using TransportPipesCommon.Enums;
using TransportPipesCommon.GameLogicObjects.GameObjectInfo;
using TransportPipesCommon.Helpers;
using TransportPipesCommon.Repositories;

namespace TransportPipes.Backend.GameLogicObjects
{
    public partial class BackendGameObject
    {
        private DbGameObject _dbGameObject;

        public DbGameObject DbGameObject { get { return _dbGameObject; } }

        public GameObjectType GameObjectType { get { return _dbGameObject.GameObjectType; } }
        public int X { get { return _dbGameObject.X; } }
        public int Y { get { return _dbGameObject.Y; } }
        public int XinChunk { get; private set; }
        public int YinChunk { get; private set; }

        //Connections
        public Boolean HasLoadedConnections { get; set; }
        //public List<BackendConnection> Connections { get; private set; }

        public IEnumerable<BackendGameObject> AllDestinations
        {
            get
            {
                if (_dbGameObject.Sources != null)
                {
                    if (_dbGameObject.Sources.Any(t => t.Destinations == null))
                    {
                        Console.WriteLine("One of the destinations is null, I need to research if this is normal");
                    }
                    var sourcesDestinations = _dbGameObject.Sources.Where(t => t.Destinations != null).SelectMany(t => t.Destinations).ToList();

                    var objectsToReturn = sourcesDestinations.Select(t =>
                    {
                        if (t.BackendGameObject != null)
                        {
                            return t.BackendGameObject;
                        }
                        else
                        {
                            throw new Exception("This should not happen anymore because we preload all connected objects for every found object");
                            //This happens when chunk of a connected game object is not loaded into memory yet, this method will then try to also load that game object from the database
                            //return StaticRepositoryProvider.CachedGameObjectsRepository.GetGameObject(t.X, t.Y);
                        }
                    })
                    .Where(t => t != this) //Make sure we don't provide to ourself
                    .Where(t => GameResourcesRepository.DetermineGameObjectInfo(t.GameObjectType).ConsumptionInfo.Any(z => GameResourcesRepository.DetermineGameObjectInfo(this.GameObjectType).ProductionInfo.Any(g => g.ResourceType == z.ResourceType))) //Here we make sure that we only return destinations that can actually eat our resources. Other ones won't send requests.
                    .ToList();

                    return objectsToReturn;

                }

                return Enumerable.Empty<BackendGameObject>();
            }
        }

        public IEnumerable<BackendGameObject> AllSources
        {
            get
            {

                if (_dbGameObject.Destinations != null)
                {
                    if (_dbGameObject.Destinations.Any(t => t.Sources == null))
                    {
                        Console.WriteLine("One of the sources is null, I need to research if this is normal");
                    }
                    var destinationsSources = _dbGameObject.Destinations.Where(t => t.Sources != null).SelectMany(t => t.Sources).ToList();

                    var objectsToReturn = destinationsSources.Select(t =>
                    {
                        if (t.BackendGameObject != null)
                        {
                            return t.BackendGameObject;
                        }
                        else
                        {
                            throw new Exception("This should not happen anymore because we preload all connected objects for every found object");
                            //This happens when chunk of a connected game object is not loaded into memory yet, this method will then try to also load that game object from the database                            
                            //return StaticRepositoryProvider.CachedGameObjectsRepository.GetGameObject(t.X, t.Y);
                        }
                    })
                    .Where(t => t != this) //Make sure we don't provide to ourself
                    .Where(t => GameResourcesRepository.DetermineGameObjectInfo(t.GameObjectType).ProductionInfo.Any(z => GameResourcesRepository.DetermineGameObjectInfo(this.GameObjectType).ConsumptionInfo.Any(g => g.ResourceType == z.ResourceType))) //Here we make sure that we only return destinations that can actually eat our resources. Other ones won't send requests.
                    .ToList();

                    return objectsToReturn;

                }

                return Enumerable.Empty<BackendGameObject>();
            }
        }

        //public List<BackendResourceInfo> ConsumptionInfo { get; private set; }
        //public List<BackendResourceInfo> GenerationInfo { get; private set; }

        public Boolean IsPipe
        {
            get
            {
                return ((int)_dbGameObject.GameObjectType) >= 10000;
            }
        }

        public BackendGameObject(DbGameObject dbGameObject)
        {
            this._dbGameObject = dbGameObject;
            _dbGameObject.BackendGameObject = this;

            if (_dbGameObject.GlobalConnection != null)
            {
                //Set connections loaded to true so the program won't re-determine everything
                //If global connection id is not == null it will redetermine all connections for this thing
                this.HasLoadedConnections = true;
            }

            var posInChunk = GenericMathHelper.ConvertPosToPosInChunk(this.X, this.Y);
            this.XinChunk = posInChunk.X;
            this.YinChunk = posInChunk.Y;
        }
    }
}
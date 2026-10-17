using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransportPipesActionizer.Calculators;
using TransportPipesCalculators.Calculators;
using TransportPipesCommon.Enums;
using TransportPipesDatabase;
using TransportPipesDatabase.Helpers;
using TransportPipesDatabase.Poco;
using TransportPipesDatabase.Poco.ActionDetails;
using TransportPipesDatabase.Poco.ClientUpdateDetails;

namespace TransportPipesActionizer
{
    public class TransportActionRunner
    {
        private ITransportDatabase _transportDatabase;
        private ConnectionFinder _connectionFinder;

        public TransportActionRunner(ITransportDatabase transportDatabase)
        {
            _transportDatabase = transportDatabase;
            _connectionFinder = new ConnectionFinder(transportDatabase);
        }

        public void ExecuteSingleAction(DbAction action)
        {
            Console.WriteLine("{0} - Action from '{1}': {2}", action.DateToBeExecuted, action.ExecutedById, action.ActionDetails);

            var w = Stopwatch.StartNew();

            switch (action.TransportAction)
            {
                case TransportAction.CreateBuilding:
                    {
                        var actionDetails = (ActionDetailsCreateBuilding)action.ActionDetails;

                        var chunkCacheBeforeAdd = new ChunkCacheHelper(_transportDatabase);
                        var buildingAtThisLocation = chunkCacheBeforeAdd.GetDbGameObject(action.X, action.Y);

                        if (buildingAtThisLocation == null)
                        {
                            CalculateCurrentGoldForAllAffectedUsersOnAdd(chunkCacheBeforeAdd, action.X, action.Y, actionDetails.GameObject.IsPipe(), action.DateToBeExecuted);

                            var newGameObject = new DbGameObject(action.X, action.Y, actionDetails.GameObject, action.ExecutedById);
                            _transportDatabase.AddGameObject(newGameObject);

                            var connections = _connectionFinder.RedetermineAllConnectionsForGameObject(newGameObject.X, newGameObject.Y, action.DateToBeExecuted);
                            var affectedPlayers = connections.SelectMany(t => t).SelectMany(t => t).Where(t => !t.GameObjectType.IsPipe()).Select(t => t.DbOwnerId).Distinct().ToList();

                            _transportDatabase.AddClientUpdate(new DbClientUpdate() { ExecutedById = action.ExecutedById, ClientUpdateDetails = new ClientUpdateDetailsTileNeedsRefresh() { X = action.X, Y = action.Y } });
                            //TODO: Verify if this is correct + only update if gold income has changed (seems to be quite hard though, so now we only provide all users in the changed global connection)
                            _transportDatabase.AddClientUpdate(new DbClientUpdate() { ExecutedById = action.ExecutedById, ClientUpdateDetails = new ClientUpdateDetailsIncomeChanged() { DbUserIds = affectedPlayers } });
                        }
                    }
                    break;
                case TransportAction.DeleteBuilding:
                    {
                        var actionDetails = (ActionDetailsDeleteBuilding)action.ActionDetails;

                        var chunkCacheBeforeAdd = new ChunkCacheHelper(_transportDatabase);
                        var buildingAtThisLocation = chunkCacheBeforeAdd.GetDbGameObject(action.X, action.Y);

                        if (buildingAtThisLocation != null)
                        {
                            var affectedPlayers = CalculateCurrentGoldForAllAffectedUsersOnDelete(chunkCacheBeforeAdd, action.X, action.Y, buildingAtThisLocation, action.DateToBeExecuted);

                            //We have to clear out this first, then delete it and then redetermine the connections
                            _transportDatabase.ClearCompleteGlobalConnections(new List<DbGameObject>() { buildingAtThisLocation });
                            _transportDatabase.DeleteBuilding(buildingAtThisLocation.X, buildingAtThisLocation.Y);
                            _connectionFinder.RedetermineAllConnectionsForDeletedGameObject(buildingAtThisLocation.X, buildingAtThisLocation.Y, buildingAtThisLocation.GameObjectType.IsPipe(), action.DateToBeExecuted);

                            _transportDatabase.AddClientUpdate(new DbClientUpdate() { ExecutedById = action.ExecutedById, ClientUpdateDetails = new ClientUpdateDetailsTileNeedsRefresh() { X = action.X, Y = action.Y } });
                            //TODO: Verify if this is correct + only update if gold income has changed (seems to be quite hard though, so now we only provide all users in the changed global connection)
                            _transportDatabase.AddClientUpdate(new DbClientUpdate() { ExecutedById = action.ExecutedById, ClientUpdateDetails = new ClientUpdateDetailsIncomeChanged() { DbUserIds = affectedPlayers } });
                        }
                    }
                    break;
                default:
                    //Mag niet
                    break;
            }

            Console.WriteLine("\tTime Taken: " + Math.Round(w.Elapsed.TotalSeconds, 3));
        }

        public void CalculateCurrentGoldForAllAffectedUsersOnAdd(ChunkCacheHelperBase chunkCacheHelperBase, int x, int y, bool newObjectIsPipe, DateTime dateToBeExecuted)
        {
            var affectedNeighbours = new List<DbGameObject>();

            if (newObjectIsPipe)
            {
                //If this is a pipe add all GameObjects around it
                affectedNeighbours.AddRange(chunkCacheHelperBase.GetNeighbouringTiles(x, y).Where(t => t != null));
            }
            else
            {
                //If this is not a pipe only add Pipes around it
                affectedNeighbours.AddRange(chunkCacheHelperBase.GetNeighbouringTiles(x, y).Where(t => t != null && t.GameObjectType.IsPipe()));
            }

            CalculateCurrentGoldForAllAffectedUsers(chunkCacheHelperBase, affectedNeighbours, dateToBeExecuted);
        }

        /// <summary>
        /// Calculates the gold for users on delete
        /// </summary>
        /// <param name="chunkCacheHelperBase"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="oldGameObject"></param>
        /// <param name="dateToBeExecuted"></param>
        /// <returns>A list of affected user ids</returns>
        public List<long> CalculateCurrentGoldForAllAffectedUsersOnDelete(ChunkCacheHelperBase chunkCacheHelperBase, int x, int y, DbGameObject oldGameObject, DateTime dateToBeExecuted)
        {
            //This GameObject is the only one affected because all pipes next to it are in the same GlobalConnection so they are automatically also affected
            return CalculateCurrentGoldForAllAffectedUsers(chunkCacheHelperBase, new List<DbGameObject>() { oldGameObject }, dateToBeExecuted);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="chunkCacheHelperBase"></param>
        /// <param name="affectedGameObjects"></param>
        /// <param name="dateToBeExecuted"></param>
        /// <returns>A list of affected user ids</returns>
        public List<long> CalculateCurrentGoldForAllAffectedUsers(ChunkCacheHelperBase chunkCacheHelperBase, List<DbGameObject> affectedGameObjects, DateTime dateToBeExecuted)
        {
            //Only select non 0 GlobalConnectionIds because 0 means no GlobalConnection
            var allGlobalConnectionIds = affectedGameObjects.Select(t => t.GlobalConnectionId).Where(t => t != 0).Distinct().ToList();
            var calculationPackages = _transportDatabase.FindAllCalculationsForTheseGlobalConnectionIds(allGlobalConnectionIds);

            var dictOfMoneyForPlayers = new Dictionary<DbUser, double>();

            var completedCalculationsGlobalConnectionIds = MoneyForAllPlayersDeterminer.AddMoneyForPlayersToDictionary(calculationPackages, dictOfMoneyForPlayers, dateToBeExecuted);

            _transportDatabase.SetLastCalculationTimeForGlobalConnections(completedCalculationsGlobalConnectionIds, dateToBeExecuted);
            _transportDatabase.UpdateGoldForUsers(dictOfMoneyForPlayers);

            return calculationPackages.SelectMany(t => t.ProvidersAndConsumersRepository.ConnectedGameObjects).Select(t => t.DbOwnerId).Distinct().ToList();
        }
    }
}

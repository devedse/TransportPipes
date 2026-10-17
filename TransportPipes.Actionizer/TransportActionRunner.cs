using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TransportPipes.Actionizer.Calculators;
using TransportPipes.Calculators.Calculators;
using TransportPipes.Common.Enums;
using TransportPipes.Database;
using TransportPipes.Database.Entities;
using TransportPipes.Database.Entities.ActionDetails;
using TransportPipes.Database.Entities.ClientUpdateDetails;
using TransportPipes.Database.Helpers;

namespace TransportPipes.Actionizer;

public class TransportActionRunner
{
    private readonly ITransportDatabase _transportDatabase;
    private readonly ConnectionFinder _connectionFinder;
    private readonly ILogger _logger;

    public TransportActionRunner(ITransportDatabase transportDatabase, ILogger? logger = null)
    {
        _transportDatabase = transportDatabase;
        _connectionFinder = new ConnectionFinder(transportDatabase);
        _logger = logger ?? NullLogger.Instance;
    }

    public void ExecuteSingleAction(DbAction action)
    {
        _logger.LogInformation("{DateToBeExecuted} - Action from '{ExecutedById}' at <{X},{Y}>: {ActionDetails}", action.DateToBeExecuted, action.ExecutedById, action.X, action.Y, action.ActionDetails);

        var w = Stopwatch.StartNew();

        switch (action.TransportAction)
        {
            case TransportAction.CreateBuilding:
                {
                    var actionDetails = (ActionDetailsCreateBuilding)action.ActionDetails!;

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

        _logger.LogInformation("Action {ActionId} took {Seconds}s", action.Id, Math.Round(w.Elapsed.TotalSeconds, 3));
    }

    public void CalculateCurrentGoldForAllAffectedUsersOnAdd(ChunkCacheHelperBase chunkCacheHelperBase, int x, int y, bool newObjectIsPipe, DateTime dateToBeExecuted)
    {
        var affectedNeighbours = new List<DbGameObject>();

        if (newObjectIsPipe)
        {
            //If this is a pipe add all GameObjects around it
            affectedNeighbours.AddRange(chunkCacheHelperBase.GetNeighbouringTiles(x, y).OfType<DbGameObject>());
        }
        else
        {
            //If this is not a pipe only add Pipes around it
            affectedNeighbours.AddRange(chunkCacheHelperBase.GetNeighbouringTiles(x, y).OfType<DbGameObject>().Where(t => t.GameObjectType.IsPipe()));
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

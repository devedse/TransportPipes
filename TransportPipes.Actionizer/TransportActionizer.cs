using System.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TransportPipes.Database;
using TransportPipes.Database.Entities;

namespace TransportPipes.Actionizer;

/// <summary>
/// Executes all actions (building, deleting, ...) whose time has come. This runs every second in the server.
/// </summary>
public class TransportActionizer
{
    private readonly TransportPipesDbFactory _transportPipesDbFactory;
    private readonly ILogger _logger;

    public TransportActionizer(TransportPipesDbFactory transportPipesDbFactory, ILogger<TransportActionizer>? logger = null)
    {
        _transportPipesDbFactory = transportPipesDbFactory;
        _logger = logger ?? NullLogger<TransportActionizer>.Instance;
    }

    /// <returns>The amount of executed actions</returns>
    public int ExecuteAllOpenActions(DateTime beforeThisTime)
    {
        using var transportDatabase = _transportPipesDbFactory.CreateDatabaseWithOpenConnection();
        return ExecuteAllOpenActionsInternal(transportDatabase, beforeThisTime);
    }

    /// <returns>The amount of executed actions</returns>
    public int ExecuteAllOpenActionsInternal(ITransportDatabase transportDatabase, DateTime beforeThisTime)
    {
        var transportActionRunner = new TransportActionRunner(transportDatabase, _logger);
        var actions = transportDatabase.ObtainUnexecutedActions(beforeThisTime);

        foreach (var action in actions)
        {
            using var transaction = transportDatabase.OpenTransaction(IsolationLevel.ReadCommitted);

            transportActionRunner.ExecuteSingleAction(action);
            //Only complete this action so we can do 1 action per transaction
            transportDatabase.ChangeGoldOfPlayer(action.ExecutedById, -action.GoldClaimed);
            transportDatabase.CompleteActions(new List<DbAction> { action });

            transaction.Commit();
            _logger.LogDebug("Transaction for action {ActionId} committed", action.Id);
        }

        return actions.Count;
    }
}

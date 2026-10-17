using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using TransportPipes.Common;
using TransportPipes.Common.Enums;
using TransportPipes.Common.Helpers;
using TransportPipes.Database.Entities;
using TransportPipes.Database.Entities.CombinedPoco;
using TransportPipes.Database.Entities.ExtraObjects;
using TransportPipes.Database.Repositories;

namespace TransportPipes.Database;

/// <summary>
/// All database access of the game. Queries are not tracked (see <see cref="TransportPipesDbFactory"/>) and all updates and
/// deletes are executed directly in the database, so this behaves like the micro ORM the game logic was written for.
/// </summary>
public class TransportDatabase : ITransportDatabase
{
    public TransportPipesDbContext DbContext { get; }

    public TransportDatabase(TransportPipesDbContext dbContext)
    {
        DbContext = dbContext;
    }

    /// <summary>
    /// Creates the database or upgrades it to the latest migration
    /// </summary>
    public void MigrateDatabase()
    {
        DbContext.Database.Migrate();
    }

    public IDbContextTransaction OpenTransaction(IsolationLevel isolationLevel)
    {
        return DbContext.Database.BeginTransaction(isolationLevel);
    }

    public List<DbClientUpdate> ObtainAndDeleteOpenClientUpdates()
    {
        var clientUpdates = DbContext.ClientUpdates.OrderBy(t => t.Id).ToList();

        if (clientUpdates.Count != 0)
        {
            var clientUpdateIds = clientUpdates.Select(t => t.Id).ToList();
            DbContext.ClientUpdates.Where(t => clientUpdateIds.Contains(t.Id)).ExecuteDelete();
        }

        return clientUpdates;
    }

    public Chunk GetChunk(int x, int y)
    {
        var dbGameObjects = DbContext.GameObjects.Where(t => t.ChunkX == x && t.ChunkY == y).OrderBy(t => t.Id).ToList();
        var chunk = new Chunk(dbGameObjects);
        return chunk;
    }

    public void ClearConnections(List<DbGameObject> gameObjects)
    {
        //If there are no game objects don't even start
        if (gameObjects.Count != 0)
        {
            var gameObjectsIds = gameObjects.Select(t => t.Id).ToList();

            var consumerConnectionsToClear = DbContext.ConsumerConnections.Where(t => gameObjectsIds.Contains(t.DestinationId)).Select(t => t.ConnectionId).Distinct().ToList();
            var providerConnectionsToClear = DbContext.ProviderConnections.Where(t => gameObjectsIds.Contains(t.SourceId)).Select(t => t.ConnectionId).Distinct().ToList();

            var combinedConnectionsToClear = consumerConnectionsToClear.Concat(providerConnectionsToClear).Distinct().ToList();

            //Only delete if there are things to delete
            if (combinedConnectionsToClear.Count != 0)
            {
                DbContext.ConsumerConnections.Where(t => combinedConnectionsToClear.Contains(t.ConnectionId)).ExecuteDelete();
                DbContext.ProviderConnections.Where(t => combinedConnectionsToClear.Contains(t.ConnectionId)).ExecuteDelete();
            }
        }
    }

    public void ClearCompleteGlobalConnections(List<DbGameObject> gameObjects)
    {
        //If there are no game objects don't even start
        if (gameObjects.Count != 0)
        {
            var gameObjectIds = gameObjects.Select(z => z.Id).ToList();

            var consumerGlobalConnectionsToClear = DbContext.ConsumerConnections.Where(t => gameObjectIds.Contains(t.DestinationId)).Select(t => t.GlobalConnectionId).Distinct().ToList();
            var providerGlobalConnectionsToClear = DbContext.ProviderConnections.Where(t => gameObjectIds.Contains(t.SourceId)).Select(t => t.GlobalConnectionId).Distinct().ToList();

            //Also add the global connections from the game objects themselves
            var gameObjectsProvidedGlobalConnections = gameObjects.Select(t => t.GlobalConnectionId);

            var combinedGlobalConnectionsToClear = consumerGlobalConnectionsToClear.Concat(providerGlobalConnectionsToClear).Concat(gameObjectsProvidedGlobalConnections).Where(t => t != 0).Distinct().ToList();

            //Only delete if there are things to delete
            if (combinedGlobalConnectionsToClear.Count != 0)
            {
                //Clear connections based on global connection id's
                DbContext.ConsumerConnections.Where(t => combinedGlobalConnectionsToClear.Contains(t.GlobalConnectionId)).ExecuteDelete();
                DbContext.ProviderConnections.Where(t => combinedGlobalConnectionsToClear.Contains(t.GlobalConnectionId)).ExecuteDelete();

                //Set game objects global connections to 0 for all found global connections
                DbContext.GameObjects
                    .Where(t => combinedGlobalConnectionsToClear.Contains(t.GlobalConnectionId))
                    .ExecuteUpdate(s => s
                        .SetProperty(t => t.GlobalConnectionId, 0)
                        .SetProperty(t => t.DbGlobalConnectionId, (long?)null));

                //Also delete DbGlobalConnections in Database
                DbContext.GlobalConnections.Where(t => combinedGlobalConnectionsToClear.Contains(t.GlobalConnectionId)).ExecuteDelete();
            }
        }
    }

    public void AddConnections(List<DbGameObjectsConnection> connections, DateTime timeOfCreation)
    {
        //Not Thread Safe (even with multiple instances due to the ConnectionId and GlobalConnectionId being obtained here)
        //This is fine because only the Actionizer creates connections.
        var curMaxConnectionId = Math.Max(
            DbContext.ConsumerConnections.Max(t => (int?)t.ConnectionId) ?? 0,
            DbContext.ProviderConnections.Max(t => (int?)t.ConnectionId) ?? 0);

        var curMaxGlobalConnectionId = new[]
        {
            DbContext.ConsumerConnections.Max(t => (int?)t.GlobalConnectionId) ?? 0,
            DbContext.ProviderConnections.Max(t => (int?)t.GlobalConnectionId) ?? 0,
            DbContext.GameObjects.Max(t => (int?)t.GlobalConnectionId) ?? 0
        }.Max();

        curMaxGlobalConnectionId++;

        var newDbGlobalConnection = new DbGlobalConnection { GlobalConnectionId = curMaxGlobalConnectionId, LastCalculated = timeOfCreation };
        DbContext.GlobalConnections.Add(newDbGlobalConnection);

        foreach (var connection in connections)
        {
            curMaxConnectionId++;
            foreach (var provider in connection.Providers)
            {
                DbContext.ProviderConnections.Add(new DbProviderConnection
                {
                    ConnectionId = curMaxConnectionId,
                    GlobalConnectionId = curMaxGlobalConnectionId,
                    SourceId = provider.Id
                });
            }

            foreach (var consumer in connection.Consumers)
            {
                DbContext.ConsumerConnections.Add(new DbConsumerConnection
                {
                    ConnectionId = curMaxConnectionId,
                    GlobalConnectionId = curMaxGlobalConnectionId,
                    DestinationId = consumer.Id
                });
            }
        }

        SaveChangesAndClearTracker();

        //Set game objects global connections to new global connection
        var allGameObjectIds = connections.SelectMany(t => t.Consumers.Concat(t.Pipes).Concat(t.Providers).Concat(t.NonConsumerProviderBuildings)).Select(t => t.Id).Distinct().ToList();
        if (allGameObjectIds.Count != 0)
        {
            DbContext.GameObjects
                .Where(t => allGameObjectIds.Contains(t.Id))
                .ExecuteUpdate(s => s
                    .SetProperty(t => t.GlobalConnectionId, curMaxGlobalConnectionId)
                    .SetProperty(t => t.DbGlobalConnectionId, newDbGlobalConnection.Id));
        }
    }

    public List<ConsumerProviderConnection> ObtainListOfConsumerProviderConnections(int globalConnectionId)
    {
        return ObtainListOfConsumerProviderConnections(new List<int> { globalConnectionId });
    }

    public List<ConsumerProviderConnection> ObtainListOfConsumerProviderConnections(IEnumerable<int> globalConnectionIds)
    {
        var globalConnectionIdsList = globalConnectionIds.ToList();
        if (globalConnectionIdsList.Count == 0)
        {
            return new List<ConsumerProviderConnection>();
        }

        var query = from cons in DbContext.ConsumerConnections
                    join prov in DbContext.ProviderConnections on cons.ConnectionId equals prov.ConnectionId
                    where globalConnectionIdsList.Contains(cons.GlobalConnectionId) && cons.DestinationId != prov.SourceId
                    orderby cons.Id, prov.Id
                    select new ConsumerProviderConnection
                    {
                        ConnectionId = cons.ConnectionId,
                        SourceId = prov.SourceId,
                        DestinationId = cons.DestinationId,
                        GlobalConnectionId = cons.GlobalConnectionId
                    };

        return query.ToList();
    }

    /// <summary>
    /// Adds an action, there can only be one unexecuted action per tile.
    /// </summary>
    /// <returns>True if the action was added, false if there already is an action queued for this tile</returns>
    public bool AddDbAction(DbAction action)
    {
        var itemsAtThisLocation = DbContext.Actions.Any(t => t.X == action.X && t.Y == action.Y && !t.Executed);
        if (itemsAtThisLocation)
        {
            return false;
        }

        DbContext.Actions.Add(action);
        try
        {
            DbContext.SaveChanges();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            //Another action was queued on this tile at exactly the same moment
            return false;
        }
        finally
        {
            DbContext.ChangeTracker.Clear();
        }

        return true;
    }

    public List<DbAction> ObtainUnexecutedActions(DateTime beforeThisTime)
    {
        var actions = DbContext.Actions
            .Where(t => !t.Executed && t.DateToBeExecuted < beforeThisTime)
            .OrderBy(t => t.DateToBeExecuted)
            .ThenBy(t => t.Id)
            .ToList();
        return actions;
    }

    public void CompleteActions(List<DbAction> actionsToBeCompleted)
    {
        //If there are no game objects don't even start
        if (actionsToBeCompleted.Count != 0)
        {
            var actionIds = actionsToBeCompleted.Select(z => z.Id).ToList();
            DbContext.Actions.Where(t => actionIds.Contains(t.Id)).ExecuteUpdate(s => s.SetProperty(t => t.Executed, true));
        }
    }

    public double GetClaimedGoldForUser(long userId)
    {
        var summedValue = DbContext.Actions.Where(t => !t.Executed && t.ExecutedById == userId).Sum(t => (double?)t.GoldClaimed) ?? 0;
        return summedValue;
    }

    public DbGameObject? GetGameObject(int x, int y)
    {
        var foundGameObject = DbContext.GameObjects.FirstOrDefault(t => t.X == x && t.Y == y);
        return foundGameObject;
    }

    public void AddGameObject(DbGameObject gameObject)
    {
        DbContext.GameObjects.Add(gameObject);
        SaveChangesAndClearTracker();
    }

    public void DeleteBuilding(int x, int y)
    {
        //The connections of this building are removed by the database (cascading delete)
        DbContext.GameObjects.Where(t => t.X == x && t.Y == y).ExecuteDelete();
    }

    public List<int> FindAllGlobalConnectionIdsThesePlayersAreInvolvedIn(List<long> playerIds)
    {
        if (playerIds.Count != 0)
        {
            var foundGlobalConnectionIds = DbContext.GameObjects.Where(t => playerIds.Contains(t.DbOwnerId)).Select(t => t.GlobalConnectionId).Distinct().ToList();
            return foundGlobalConnectionIds;
        }
        return new List<int>();
    }

    public List<CalculationPackage> FindAllCalculationsForTheseGlobalConnectionIds(List<int> globalConnectionIds)
    {
        //Because we use multiple selects here it should be run in a RepeatableRead (snapshot) transaction
        if (globalConnectionIds.Count != 0)
        {
            //Identity resolution makes sure GameObjects with the same owner share the same DbUser instance (like OrmLite's LoadSelect did)
            var dbGameObjects = DbContext.GameObjects
                .AsNoTrackingWithIdentityResolution()
                .Include(t => t.Owner)
                .Where(t => globalConnectionIds.Contains(t.GlobalConnectionId) && t.GlobalConnectionId != 0)
                .OrderBy(t => t.Id)
                .ToList();

            var groupedDbGameObjects = dbGameObjects.GroupBy(t => t.GlobalConnectionId);
            var globalConnections = DbContext.GlobalConnections.Where(t => globalConnectionIds.Contains(t.GlobalConnectionId)).ToList();

            var allGlobalConnectionIds = dbGameObjects.Select(t => t.GlobalConnectionId).Distinct().ToList();
            var allConsumerProvidersConnections = ObtainListOfConsumerProviderConnections(allGlobalConnectionIds);

            var calculationPackages = new List<CalculationPackage>();
            foreach (var globalConnectionGameObjects in groupedDbGameObjects)
            {
                var startGameObject = globalConnectionGameObjects.FirstOrDefault(t => !t.GameObjectType.IsPipe());

                //Only add it if we actually have a non pipe Game Object
                if (startGameObject != null)
                {
                    var providersAndConsumersRepository = new ProvidersAndConsumersRepository(globalConnectionGameObjects.Where(t => !t.GameObjectType.IsPipe()).ToList(), allConsumerProvidersConnections.Where(t => t.GlobalConnectionId == globalConnectionGameObjects.Key).ToList());
                    var matchingGlobalConnection = globalConnections.First(t => t.GlobalConnectionId == globalConnectionGameObjects.Key);
                    var calculationPackage = new CalculationPackage(globalConnectionGameObjects.Key, providersAndConsumersRepository, startGameObject, matchingGlobalConnection.LastCalculated!.Value);
                    calculationPackages.Add(calculationPackage);
                }
            }

            return calculationPackages;
        }
        return new List<CalculationPackage>();
    }

    public void UpdateGoldForUsers(Dictionary<DbUser, double> moneyForPlayers)
    {
        foreach (var moneyForPlayer in moneyForPlayers)
        {
            ChangeGoldOfPlayer(moneyForPlayer.Key.Id, moneyForPlayer.Value);
        }
    }

    public void SetLastCalculationTimeForGlobalConnections(List<int> globalConnectionIds, DateTime executedCalculationDateTime)
    {
        if (globalConnectionIds.Count != 0)
        {
            DbContext.GlobalConnections
                .Where(t => globalConnectionIds.Contains(t.GlobalConnectionId))
                .ExecuteUpdate(s => s.SetProperty(t => t.LastCalculated, executedCalculationDateTime));
        }
    }

    /// <summary>
    /// Adds a user, the NormalizedUserName is determined from the UserName
    /// </summary>
    public DbUser AddUser(DbUser user)
    {
        user.NormalizedUserName = DbUser.NormalizeUserName(user.UserName);
        DbContext.Users.Add(user);
        SaveChangesAndClearTracker();
        return user;
    }

    public DbUser RegisterUser(string userName, string passwordHash, string email)
    {
        var newUser = new DbUser
        {
            UserName = userName.Trim(),
            PasswordHash = passwordHash,
            Email = email.Trim().ToLowerInvariant()
        };

        return AddUser(newUser);
    }

    public DbUser? GetUser(string userName)
    {
        var normalizedUserName = DbUser.NormalizeUserName(userName);
        return DbContext.Users.SingleOrDefault(t => t.NormalizedUserName == normalizedUserName);
    }

    public DbUser? GetUserById(long userId)
    {
        return DbContext.Users.SingleOrDefault(t => t.Id == userId);
    }

    public void AddClientUpdate(DbClientUpdate clientUpdate)
    {
        DbContext.ClientUpdates.Add(clientUpdate);
        SaveChangesAndClearTracker();
    }

    public void ChangeGoldOfPlayer(long userId, double amount)
    {
        //This is executed as a single "Gold = Gold + amount" update in the database so concurrent changes can't get lost
        DbContext.Users.Where(t => t.Id == userId).ExecuteUpdate(s => s.SetProperty(t => t.Gold, t => t.Gold + amount));
    }

    public List<DbAction> GetBuildActionsInChunk(int xChunk, int yChunk)
    {
        //TODO: Also save chunk in actions
        var startXY = GenericMathHelper.GetXYStartForChunkCoordinates(xChunk, yChunk);
        var endXY = new Point(startXY.X + StaticConfigInfo.ChunkWidth, startXY.Y + StaticConfigInfo.ChunkHeight);

        var actions = DbContext.Actions
            .Where(t => !t.Executed && t.TransportAction == TransportAction.CreateBuilding && t.X >= startXY.X && t.Y >= startXY.Y && t.X < endXY.X && t.Y < endXY.Y)
            .ToList();
        return actions;
    }

    private void SaveChangesAndClearTracker()
    {
        DbContext.SaveChanges();
        DbContext.ChangeTracker.Clear();
    }

    public void Dispose()
    {
        DbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}

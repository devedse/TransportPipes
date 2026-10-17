using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using TransportPipesCommon.Enums;
using TransportPipesCommon.Helpers;
using TransportPipesDatabase.Poco;
using TransportPipesDatabase.Poco.CombinedPoco;
using TransportPipesDatabase.Poco.ExtraObjects;
using TransportPipesDatabase.Repositories;

namespace TransportPipesDatabase
{
    public class TransportPipesDatabaseOrmLite : ITransportDatabase, IDisposable
    {
        public IDbConnection DbConnection { get; private set; }

        public TransportPipesDatabaseOrmLite(IDbConnection dbConnection)
        {
            DbConnection = dbConnection;
        }

        public void SetDatabaseBrokerAndSnapshotIsolation()
        {
            int isBrokerEnabled = DbConnection.Single<int>($"SELECT is_broker_enabled FROM sys.databases WHERE name = '{DbConstants.DbName}';");

            if (isBrokerEnabled == 0)
            {
                int result = DbConnection.ExecuteNonQuery($"ALTER DATABASE {DbConstants.DbName} SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE");
                Console.WriteLine($"Enabled broker, result: {result}");
            }

            int isSnapshotIsolationEnabled = DbConnection.Single<int>($"SELECT snapshot_isolation_state FROM sys.databases WHERE name = '{DbConstants.DbName}';");

            if (isSnapshotIsolationEnabled == 0)
            {
                int result = DbConnection.ExecuteNonQuery($"ALTER DATABASE {DbConstants.DbName} SET ALLOW_SNAPSHOT_ISOLATION ON");
                Console.WriteLine($"Enabled Snapshot Isolation, result: {result}");
            }
        }

        public void DropDb()
        {
            DbConnection.DropTable<DbClientUpdate>();
            DbConnection.DropTable<DbConsumerConnection>();
            DbConnection.DropTable<DbProviderConnection>();
            DbConnection.DropTable<DbGameObject>();
            DbConnection.DropTable<DbAction>();
            DbConnection.DropTable<DbGlobalConnection>();
            DbConnection.DropTable<DbUser>();
        }

        public bool CreateDbIfNotExists()
        {
            bool createdDbUsersTable;

            createdDbUsersTable = DbConnection.CreateTableIfNotExists<DbUser>();
            DbConnection.CreateTableIfNotExists<DbGlobalConnection>();
            DbConnection.CreateTableIfNotExists<DbGameObject>();
            DbConnection.CreateTableIfNotExists<DbConsumerConnection>();
            DbConnection.CreateTableIfNotExists<DbProviderConnection>();
            DbConnection.CreateTableIfNotExists<DbAction>();
            DbConnection.CreateTableIfNotExists<DbClientUpdate>();

            return createdDbUsersTable;
        }

        public void CreateTestUsers()
        {
            var adminuser = new DbUser { UserName = "admin", Password = "1f40fc92da241694750979ee6cf582f2d5d7d28e18335de05abc54d0560e0f5302860c652bf08d560252aa5e74210546f369fbbbce8c12cfc7957b2652fe9a75", Email = "admin@test.com", Gold = 500 };
            var otheruser = new DbUser { UserName = "Devedse", Password = "1f40fc92da241694750979ee6cf582f2d5d7d28e18335de05abc54d0560e0f5302860c652bf08d560252aa5e74210546f369fbbbce8c12cfc7957b2652fe9a75", Email = "davydavidse@gmail.com", Gold = 500 };
            adminuser.Id = DbConnection.Insert(adminuser, selectIdentity: true); //Select Identity to obtain the ID of these users
            otheruser.Id = DbConnection.Insert(otheruser, selectIdentity: true); //Select Identity to obtain the ID of these users
        }

        public IDbTransaction OpenTransaction(IsolationLevel isolationLevel)
        {
            return DbConnection.OpenTransaction(isolationLevel);
        }


        public List<DbClientUpdate> ObtainAndDeleteOpenClientUpdates()
        {
            var clientUpdates = DbConnection.Select<DbClientUpdate>();
            if (clientUpdates.Any())
            {
                var clientUpdateIds = clientUpdates.Select(t => t.Id);
                DbConnection.Delete<DbClientUpdate>(DbConnection.From<DbClientUpdate>().Where(t => Sql.In(t.Id, clientUpdateIds)));
            }
            return clientUpdates;
        }

        public Chunk GetChunk(int x, int y)
        {
            var dbGameObjects = DbConnection.Select<DbGameObject>(t => t.ChunkX == x && t.ChunkY == y);
            var chunk = new Chunk(dbGameObjects);
            return chunk;
        }

        public void ClearConnections(List<DbGameObject> gameObjects)
        {
            //If there are no game objects don't even start
            if (gameObjects.Any())
            {
                var gameObjectsIds = gameObjects.Select(t => t.Id).ToList();
                var consumerConnectionsToClear = DbConnection.Select<int>(DbConnection.From<DbConsumerConnection>().Where(t => Sql.In(t.DestinationId, gameObjectsIds)).SelectDistinct(t => t.ConnectionId));
                var providerConnectionsToClear = DbConnection.Select<int>(DbConnection.From<DbProviderConnection>().Where(t => Sql.In(t.SourceId, gameObjectsIds)).SelectDistinct(t => t.ConnectionId));

                var combinedConnectionsToClear = consumerConnectionsToClear.Concat(providerConnectionsToClear).Distinct().ToList();

                //Only delete if there are things to delete
                if (combinedConnectionsToClear.Any())
                {
                    DbConnection.Delete<DbConsumerConnection>(DbConnection.From<DbConsumerConnection>().Where(t => Sql.In(t.ConnectionId, combinedConnectionsToClear)));
                    DbConnection.Delete<DbProviderConnection>(DbConnection.From<DbProviderConnection>().Where(t => Sql.In(t.ConnectionId, combinedConnectionsToClear)));
                }
            }
        }

        public void ClearCompleteGlobalConnections(List<DbGameObject> gameObjects)
        {
            //If there are no game objects don't even start
            if (gameObjects.Any())
            {
                var gameObjectIds = gameObjects.Select(z => z.Id).ToList();
                var consumerGlobalConnectionsToClear = DbConnection.Select<int>(DbConnection.From<DbConsumerConnection>().Where(t => Sql.In(t.DestinationId, gameObjectIds)).SelectDistinct(t => t.GlobalConnectionId));
                var providerGlobalConnectionsToClear = DbConnection.Select<int>(DbConnection.From<DbProviderConnection>().Where(t => Sql.In(t.SourceId, gameObjectIds)).SelectDistinct(t => t.GlobalConnectionId));

                //Also add the global connections from 
                var gameObjectsProvidedGlobalConnections = gameObjects.Select(t => t.GlobalConnectionId);

                var combinedGlobalConnectionsToClear = consumerGlobalConnectionsToClear.Concat(providerGlobalConnectionsToClear).Concat(gameObjectsProvidedGlobalConnections).Where(t => t != 0).Distinct().ToList();

                //Only delete if there are things to delete
                if (combinedGlobalConnectionsToClear.Any())
                {
                    //Clear connections based on global connection id's
                    DbConnection.Delete<DbConsumerConnection>(DbConnection.From<DbConsumerConnection>().Where(t => Sql.In(t.GlobalConnectionId, combinedGlobalConnectionsToClear)));
                    DbConnection.Delete<DbProviderConnection>(DbConnection.From<DbProviderConnection>().Where(t => Sql.In(t.GlobalConnectionId, combinedGlobalConnectionsToClear)));

                    //Set game objects global connections to 0 for all found global connections
                    DbConnection.UpdateOnly<DbGameObject>(new DbGameObject { GlobalConnectionId = 0, DbGlobalConnectionId = null }, DbConnection.From<DbGameObject>().Where(t => Sql.In(t.GlobalConnectionId, combinedGlobalConnectionsToClear)).Update(t => new { t.GlobalConnectionId, t.DbGlobalConnectionId }));

                    //Also delete DbGlobalConnections in Database
                    DbConnection.Delete<DbGlobalConnection>(DbConnection.From<DbGlobalConnection>().Where(t => Sql.In(t.GlobalConnectionId, combinedGlobalConnectionsToClear)));
                }
            }
        }


        public void AddConnections(List<DbGameObjectsConnection> connections, DateTime timeOfCreation)
        {
            //Not Thread Safe (even with multiple instances due to the ConnectionId and GlobalConnectionId being obtained here)

            //var blah = db.Scalar<int>(db.From<DbConsumerConnection>().Join<DbProviderConnection>((cons, prov) => cons.ConnectionId == prov.ConnectionId).Select(t => t.ConnectionId));
            var curMaxConnectionId = DbConnection.Scalar<int>("SELECT MAX(ConId) from (select MAX(ConnectionId) as ConId from DbConsumerConnection UNION select MAX(ConnectionId) as ConId from DbProviderConnection) as s");
            var curMaxGlobalConnectionId = DbConnection.Scalar<int>("SELECT MAX(GlobConId) from (select MAX(GlobalConnectionId) as GlobConId from DbConsumerConnection UNION select MAX(GlobalConnectionId) as GlobConId from DbProviderConnection UNION select MAX(GlobalConnectionId) as GlobConId from DbGameObject) as s");
            curMaxGlobalConnectionId++;

            var newDbGlobalConnection = new DbGlobalConnection { GlobalConnectionId = curMaxGlobalConnectionId, LastCalculated = timeOfCreation };

            newDbGlobalConnection.Id = DbConnection.Insert(newDbGlobalConnection, selectIdentity: true);

            foreach (var connection in connections)
            {
                curMaxConnectionId++;

                foreach (var provider in connection.Providers)
                {
                    DbConnection.Insert<DbProviderConnection>(new DbProviderConnection
                    {
                        ConnectionId = curMaxConnectionId,
                        GlobalConnectionId = curMaxGlobalConnectionId,
                        SourceId = provider.Id
                    });
                }

                foreach (var consumer in connection.Consumers)
                {
                    DbConnection.Insert<DbConsumerConnection>(new DbConsumerConnection
                    {
                        ConnectionId = curMaxConnectionId,
                        GlobalConnectionId = curMaxGlobalConnectionId,
                        DestinationId = consumer.Id
                    });
                }
            }

            //Set game objects global connections to new global connection
            var allGameObjectIds = connections.SelectMany(t => t.Consumers.Concat(t.Pipes).Concat(t.Providers).Concat(t.NonConsumerProviderBuildings)).Select(t => t.Id).Distinct();

            if (allGameObjectIds.Any())
            {
                var query = DbConnection.From<DbGameObject>().Where(t => Sql.In(t.Id, allGameObjectIds)).Update(t => new { t.GlobalConnectionId, t.DbGlobalConnectionId });
                DbConnection.UpdateOnly<DbGameObject>(new DbGameObject { GlobalConnectionId = curMaxGlobalConnectionId, DbGlobalConnectionId = newDbGlobalConnection.Id }, query);
            }
        }


        public List<ConsumerProviderConnection> ObtainListOfConsumerProviderConnections(int globalConnectionId)
        {
            return ObtainListOfConsumerProviderConnections(new List<int> { globalConnectionId });
        }

        public List<ConsumerProviderConnection> ObtainListOfConsumerProviderConnections(IEnumerable<int> globalConnectionIds)
        {
            if (globalConnectionIds.Any())
            {
                var query = DbConnection.From<DbConsumerConnection>().Join<DbProviderConnection>((cons, prov) => cons.ConnectionId == prov.ConnectionId).Where(t => Sql.In(t.GlobalConnectionId, globalConnectionIds)).And<DbConsumerConnection, DbProviderConnection>((cons, prov) => cons.DestinationId != prov.SourceId);
                var listOfConsumerProviderConnections = DbConnection.Select<ConsumerProviderConnection>(query).ToList();
                return listOfConsumerProviderConnections;
            }
            return new List<ConsumerProviderConnection>();
        }

        public bool AddDbAction(DbAction action)
        {

            var itemsAtThisLocation = DbConnection.Select<DbAction>(t => t.X == action.X && t.Y == action.Y && t.Executed == false);

            if (itemsAtThisLocation.Any())
            {
                return false;
            }
            DbConnection.Insert(action);

            return true;
        }

        public List<DbAction> ObtainUnexecutedActions(DateTime beforeThisTime)
        {
            var actions = DbConnection.Select(DbConnection.From<DbAction>().Where(t => t.Executed == false && t.DateToBeExecuted < beforeThisTime).OrderBy(t => t.DateToBeExecuted));
            return actions;
        }

        public void CompleteActions(List<DbAction> actionsToBeCompleted)
        {
            //If there are no game objects don't even start
            if (actionsToBeCompleted.Any())
            {
                DbConnection.UpdateOnly<DbAction>(new DbAction { Executed = true }, DbConnection.From<DbAction>().Where(t => Sql.In(t.Id, actionsToBeCompleted.Select(z => z.Id))).Update(t => t.Executed));
            }
        }

        public double GetClaimedGoldForUser(long userId)
        {
            var summedValue = DbConnection.Scalar<double>(DbConnection.From<DbAction>().Where(t => t.Executed == false && t.ExecutedById == userId).Select(t => Sql.Sum(t.GoldClaimed)));
            return summedValue;
        }

        public DbGameObject GetGameObject(int x, int y)
        {
            var foundGameObject = DbConnection.Select<DbGameObject>(t => t.X == x && t.Y == y).FirstOrDefault();
            return foundGameObject;
        }

        public void AddGameObject(DbGameObject gameObject)
        {
            DbConnection.Insert(gameObject);
        }

        public void DeleteBuilding(int x, int y)
        {
            DbConnection.Delete<DbGameObject>(t => t.X == x && t.Y == y);
        }

        public List<int> FindAllGlobalConnectionIdsThesePlayersAreInvolvedIn(List<long> playerIds)
        {
            if (playerIds.Any())
            {
                var foundGlobalConnectionIds = DbConnection.Select<int>(DbConnection.From<DbGameObject>().Where(t => Sql.In(t.DbOwnerId, playerIds)).SelectDistinct(t => t.GlobalConnectionId));
                return foundGlobalConnectionIds;
            }
            return new List<int>();
        }


        public List<CalculationPackage> FindAllCalculationsForTheseGlobalConnectionIds(List<int> globalConnectionIds)
        {
            //Because we use 2 selects here it needs to be run in Snapshot isolution :/

            if (globalConnectionIds.Any())
            {
                //Simple LoadSelectQuery
                var dbGameObjects = DbConnection.LoadSelect<DbGameObject>(DbConnection.From<DbGameObject>().Where(t => Sql.In(t.GlobalConnectionId, globalConnectionIds)).And(t => t.GlobalConnectionId != 0));

                //Same Query but now with a join. It's not really faster though so I commented it out.
                //var dbGameObjectsCombinedQuery = DbConnection
                //    .From<DbGameObject>()
                //    .Join<DbUser>((gameObject, user) => gameObject.DbOwnerId == user.Id)
                //    .Where(t => Sql.In(t.GlobalConnectionId, globalConnectionIds))
                //    .And(t => t.GlobalConnectionId != 0);
                //var dbGameObjects = DbConnection.Select<DbGameObjectUserCombination>(dbGameObjectsCombinedQuery).Select(t => t.ToDbGameObject()).ToList();


                //var dbGameObjectsCombinedQuery = DbConnection
                //    .From<DbGameObject>()
                //    .Join<DbUser>((gameObject, user) => gameObject.DbOwnerId == user.Id)
                //    .Where(t => Sql.In(t.GlobalConnectionId, globalConnectionIds))
                //    .And(t => t.GlobalConnectionId != 0);
                //var dbGameObjects2 = DbConnection.Multi

                var groupedDbGameObjects = dbGameObjects.GroupBy(t => t.GlobalConnectionId);

                var globalConnections = DbConnection.Select<DbGlobalConnection>(DbConnection.From<DbGlobalConnection>().Where(t => Sql.In(t.GlobalConnectionId, globalConnectionIds)));

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
                        var calculationPackage = new CalculationPackage(globalConnectionGameObjects.Key, providersAndConsumersRepository, startGameObject, matchingGlobalConnection.LastCalculated.Value);

                        calculationPackages.Add(calculationPackage);
                    }
                }

                return calculationPackages;
            }
            return new List<CalculationPackage>();
        }

        public void UpdateGoldForUsers(Dictionary<DbUser, double> moneyForPlayers)
        {
            if (moneyForPlayers.Any())
            {
                foreach (var moneyForPlayer in moneyForPlayers)
                {
                    DbConnection.UpdateAdd(() => new DbUser { Gold = moneyForPlayer.Value }, t => t.Id == moneyForPlayer.Key.Id);
                }
            }
        }

        public void SetLastCalculationTimeForGlobalConnections(List<int> globalConnectionIds, DateTime executedCalculationDateTime)
        {
            if (globalConnectionIds.Any())
            {
                var predicateIsInGlobalConnectionIdsForDbGlobalConnection = PredicateBuilder.False<DbGlobalConnection>();

                foreach (var globalConnectionId in globalConnectionIds)
                {
                    predicateIsInGlobalConnectionIdsForDbGlobalConnection = predicateIsInGlobalConnectionIdsForDbGlobalConnection.Or(t => t.GlobalConnectionId == globalConnectionId);
                }

                DbConnection.UpdateOnly<DbGlobalConnection>(new DbGlobalConnection { LastCalculated = executedCalculationDateTime }, DbConnection.From<DbGlobalConnection>().Where(predicateIsInGlobalConnectionIdsForDbGlobalConnection).Update(t => new { t.LastCalculated }));
            }
        }

        public DbUser RegisterUser(string userName, string password, string email)
        {
            var newUser = new DbUser
            {
                UserName = userName,
                Password = password.ToLowerInvariant(),
                Email = email.ToLowerInvariant()
            };

            newUser.Id = DbConnection.Insert(newUser, selectIdentity: true);

            return newUser;
        }

        public DbUser GetUser(string userName, string password)
        {
            var userNameLower = userName.ToLowerInvariant();
            var passwordLower = password.ToLowerInvariant();
            return DbConnection.Select<DbUser>(DbConnection.From<DbUser>().Where(t => t.UserName.ToLower() == userNameLower && t.Password.ToLower() == passwordLower)).SingleOrDefault();
        }

        public DbUser GetUser(string userName)
        {
            var userNameLower = userName.ToLowerInvariant();
            return DbConnection.Select<DbUser>(DbConnection.From<DbUser>().Where(t => t.UserName.ToLower() == userNameLower)).SingleOrDefault();
        }

        public void AddClientUpdate(DbClientUpdate clientUpdate)
        {
            DbConnection.Insert(clientUpdate);
        }

        public void ChangeGoldOfPlayer(long userId, double amount)
        {
            //TODO: Test this if it works with nested transactions and stuff
            //Well it doesn't lol
            //It shouldn't matter too much anymore though because of the updateadd operation we have now added below



            DbConnection.UpdateAdd(() => new DbUser { Gold = amount }, t => t.Id == userId);

            //What could go wrong:

            //I think this can happen when we try to change the gold of a player twice, this could potentially be an issue but we still don't want to give users free gold
            //TODO: Investigate
            //Note: Due to the UpdateAdd this shouldn't be able to happen anymore :)
            //However it might still be better to do the thing below because we can then cancel actions and reclaim the gold easier
            //I would in fact rather only lie a claim on the money of some sorts and then deduct it in the Actionizer

            //Also not important anymore:
            //However we use a serialized transaction so it should be fine :)
        }

        public List<DbAction> GetBuildActionsInChunk(int xChunk, int yChunk)
        {
            //TODO: Also save chunk in actions
            var startXY = GenericMathHelper.GetXYStartForChunkCoordinates(xChunk, yChunk);
            var endXY = new Point(startXY.X + 8, startXY.Y + 8);

            var actions = DbConnection.Select(DbConnection.From<DbAction>().Where(t => t.Executed == false && t.TransportAction == TransportAction.CreateBuilding && t.X >= startXY.X && t.Y >= startXY.Y && t.X < endXY.X && t.Y < endXY.Y));
            return actions;
        }


        #region IDisposable Support
        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    DbConnection.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~TransportPipesDatabaseOrmLite() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
using System.Diagnostics;
using TransportPipes.Actionizer;
using TransportPipes.Common.Enums;
using TransportPipes.Common.GameLogicObjects.GameObjectInfo;
using TransportPipes.Common.Repositories;
using TransportPipes.Database;
using TransportPipes.Database.Entities;
using TransportPipes.Database.Entities.ActionDetails;
using TransportPipes.Database.Entities.ClientUpdateDetails;
using TransportPipes.TestsCommon.Helpers;

namespace TransportPipes.Actionizer.Tests.Database.TransportActionizerFacts;

[TestClass]
public class TheExecuteAllOpenActionsMethod
{
    [TestMethod]
    public void CorrectlyProcessesThreeBuildActions()
    {
        //Arrange
        var dbFactory = DbSetupHelper.CreateTestDbFactory();

        using (var transportDb = dbFactory.CreateDatabaseWithOpenConnection())
        {

            var users = DbSetupHelper.AddDefaultUsers(transportDb.DbContext);
            var adminUser = users.First(t => t.UserName.ToLower() == "admin");

            //Add actions to Database
            var action1 = new DbAction()
            {
                TransportAction = TransportAction.CreateBuilding,
                DateToBeExecuted = new DateTime(2015, 1, 1, 0, 0, 0),
                ExecutedById = adminUser.Id,
                X = 0,
                Y = 0,
                ActionDetails = new ActionDetailsCreateBuilding() { GameObject = GameObjectType.WindMoily }
            };
            transportDb.DbContext.Insert(action1);

            var action2 = new DbAction()
            {
                TransportAction = TransportAction.CreateBuilding,
                DateToBeExecuted = new DateTime(2015, 1, 1, 0, 1, 0),
                ExecutedById = adminUser.Id,
                X = 1,
                Y = 0,
                ActionDetails = new ActionDetailsCreateBuilding() { GameObject = GameObjectType.GreyPipe }
            };
            transportDb.DbContext.Insert(action2);

            var action3 = new DbAction()
            {
                TransportAction = TransportAction.CreateBuilding,
                DateToBeExecuted = new DateTime(2015, 1, 1, 0, 2, 0),
                ExecutedById = adminUser.Id,
                X = 2,
                Y = 0,
                ActionDetails = new ActionDetailsCreateBuilding() { GameObject = GameObjectType.Storage }
            };
            transportDb.DbContext.Insert(action3);



            //Create actionizer
            var actionizer = new TransportActionizer(dbFactory);

            //Act
            actionizer.ExecuteAllOpenActionsInternal(transportDb, new DateTime(2015, 1, 1, 0, 3, 0));

            //Assert

            int theGlobalConnectionId;

            Assert.AreEqual(0, transportDb.DbContext.Select<DbAction>(t => t.Executed == false).Count);
            Assert.AreEqual(3, transportDb.DbContext.Select<DbAction>(t => t.Executed == true).Count);

            Assert.AreEqual(1, transportDb.DbContext.Select<DbGameObject>(t => t.X == 0 && t.Y == 0 && t.GameObjectType == GameObjectType.WindMoily).Count);
            Assert.AreEqual(1, transportDb.DbContext.Select<DbGameObject>(t => t.X == 1 && t.Y == 0 && t.GameObjectType == GameObjectType.GreyPipe).Count);
            Assert.AreEqual(1, transportDb.DbContext.Select<DbGameObject>(t => t.X == 2 && t.Y == 0 && t.GameObjectType == GameObjectType.Storage).Count);

            theGlobalConnectionId = transportDb.DbContext.Select<DbGameObject>().Select(t => t.GlobalConnectionId).Distinct().Single();

            var allClientUpdates = transportDb.DbContext.Select<DbClientUpdate>();
            Assert.AreEqual(6, allClientUpdates.Count);

            Assert.AreEqual(6, allClientUpdates.Count(t => t.ExecutedById == 1));

            var theRefreshesRequired = allClientUpdates.Select(t => t.ClientUpdateDetails).OfType<ClientUpdateDetailsTileNeedsRefresh>().ToList();
            Assert.AreEqual(3, theRefreshesRequired.Count);

            Assert.AreEqual(1, theRefreshesRequired.Count(t => t.X == 0 && t.Y == 0));
            Assert.AreEqual(1, theRefreshesRequired.Count(t => t.X == 1 && t.Y == 0));
            Assert.AreEqual(1, theRefreshesRequired.Count(t => t.X == 2 && t.Y == 0));


            var consumerProviderConnections = transportDb.ObtainListOfConsumerProviderConnections(theGlobalConnectionId);
            Assert.AreEqual(1, consumerProviderConnections.Count);
        }
    }


    [TestMethod]
    public void CorrentlyDeterminesGoldTransfer()
    {
        //Arrange
        var dbFactory = DbSetupHelper.CreateTestDbFactory();

        DbUser otherUser;

        using (var transportDb = dbFactory.CreateDatabaseWithOpenConnection())
        {
            //var transportDb = new TransportPipesDatabaseOrmLite();
            //var inMemoryConnectionFactory = transportDb.ConnectionFactory;
            //transportDb.DropDb();


            var users = DbSetupHelper.AddDefaultUsers(transportDb.DbContext);
            var adminUser = users.First(t => t.UserName.ToLower() == "admin");
            otherUser = users.First(t => t.UserName.ToLower() == "otheruser");

            //Add actions to Database
            var action1 = new DbAction()
            {
                TransportAction = TransportAction.CreateBuilding,
                DateToBeExecuted = new DateTime(2015, 1, 1, 0, 0, 0),
                ExecutedById = adminUser.Id,
                X = 0,
                Y = 0,
                ActionDetails = new ActionDetailsCreateBuilding() { GameObject = GameObjectType.WindMoily }
            };
            transportDb.DbContext.Insert(action1);

            var action2 = new DbAction()
            {
                TransportAction = TransportAction.CreateBuilding,
                DateToBeExecuted = new DateTime(2015, 1, 1, 0, 1, 0),
                ExecutedById = adminUser.Id,
                X = 1,
                Y = 0,
                ActionDetails = new ActionDetailsCreateBuilding() { GameObject = GameObjectType.GreyPipe }
            };
            transportDb.DbContext.Insert(action2);

            var action3 = new DbAction()
            {
                TransportAction = TransportAction.CreateBuilding,
                DateToBeExecuted = new DateTime(2015, 1, 1, 0, 2, 0),
                ExecutedById = otherUser.Id,
                X = 2,
                Y = 0,
                ActionDetails = new ActionDetailsCreateBuilding() { GameObject = GameObjectType.Storage }
            };
            transportDb.DbContext.Insert(action3);



            //Create actionizer
            var actionizer = new TransportActionizer(dbFactory);

            //Execute Actions
            actionizer.ExecuteAllOpenActionsInternal(transportDb, new DateTime(2015, 1, 1, 0, 3, 0));

            int theGlobalConnectionId;

            Assert.AreEqual(0, transportDb.DbContext.Select<DbAction>(t => t.Executed == false).Count);
            Assert.AreEqual(3, transportDb.DbContext.Select<DbAction>(t => t.Executed == true).Count);

            Assert.AreEqual(1, transportDb.DbContext.Select<DbGameObject>(t => t.X == 0 && t.Y == 0 && t.GameObjectType == GameObjectType.WindMoily).Count);
            Assert.AreEqual(1, transportDb.DbContext.Select<DbGameObject>(t => t.X == 1 && t.Y == 0 && t.GameObjectType == GameObjectType.GreyPipe).Count);
            Assert.AreEqual(1, transportDb.DbContext.Select<DbGameObject>(t => t.X == 2 && t.Y == 0 && t.GameObjectType == GameObjectType.Storage).Count);

            theGlobalConnectionId = transportDb.DbContext.Select<DbGameObject>().Select(t => t.GlobalConnectionId).Distinct().Single();

            //Verify that gold is still 0 now
            var users2 = transportDb.DbContext.Select<DbUser>();
            Assert.AreEqual(2, users2.Count(t => t.Gold == 0));

            //Add another building 1 minute later
            var action4 = new DbAction()
            {
                TransportAction = TransportAction.CreateBuilding,
                DateToBeExecuted = new DateTime(2015, 1, 1, 0, 3, 0),
                ExecutedById = otherUser.Id,
                X = 3,
                Y = 0,
                ActionDetails = new ActionDetailsCreateBuilding() { GameObject = GameObjectType.GreyPipe }
            };
            transportDb.DbContext.Insert(action4);


            //Execute Actions again
            actionizer.ExecuteAllOpenActionsInternal(transportDb, new DateTime(2015, 1, 1, 0, 4, 0));


            //Gold should not not be 0 anymore because a new building was added
            var users3 = transportDb.DbContext.Select<DbUser>();

            var energyGeneratedPerTickByWindMoily = GameResourcesRepository.DetermineResourceOfTypeProduced(GameObjectType.WindMoily, BackendResourceType.Energy);
            var expectedGold = BackendResourceWorthDeterminer.AllResourceTypeValues[BackendResourceType.Energy].ValueForPlayers * 60 * energyGeneratedPerTickByWindMoily; //60 seconds passed and trade to player for energy

            Assert.IsTrue(TestHelper.AlmostEqual(expectedGold, users3.Single(t => t.UserName.ToLower() == "admin").Gold));
            Assert.IsTrue(TestHelper.AlmostEqual(expectedGold * -1, users3.Single(t => t.UserName.ToLower() == "otheruser").Gold));

            //Also the all GlobalConnections should have updated DateTime's
            var globalConnections = transportDb.DbContext.Select<DbGlobalConnection>();
            Assert.AreEqual(0, globalConnections.Count(t => t.LastCalculated != new DateTime(2015, 1, 1, 0, 3, 0)));
            Assert.AreEqual(1, globalConnections.Count(t => t.LastCalculated == new DateTime(2015, 1, 1, 0, 3, 0)));

            var consumerProviderConnections = transportDb.ObtainListOfConsumerProviderConnections(theGlobalConnectionId);
            Assert.AreEqual(1, consumerProviderConnections.Count);
        }
    }

    /// <summary>
    /// A test for testing stuff
    /// </summary>
    [TestMethod]
    public void Testje()
    {
        var dbFactory = DbSetupHelper.CreateTestDbFactory();
        using (var transportDb = dbFactory.CreateDatabaseWithOpenConnection())
        {

            //var transportDb = new TransportPipesDatabaseOrmLite();
            //var inMemoryConnectionFactory = transportDb.ConnectionFactory;


            bool recreateDb = true;

            DbUser adminUser = null!;
            if (recreateDb)
            {


                var users = DbSetupHelper.AddDefaultUsers(transportDb.DbContext);
                adminUser = users.Single(t => t.UserName.ToLower() == "admin");
            }

            if (recreateDb)
            {
                transportDb.DbContext.Insert(new DbGameObject(5, 6, GameObjectType.WindMoily, adminUser.Id));
                transportDb.DbContext.Insert(new DbGameObject(5, 8, GameObjectType.Storage, adminUser.Id));

                transportDb.DbContext.Insert(new DbConsumerConnection() { ConnectionId = 1, DestinationId = 2, GlobalConnectionId = 1 });
                transportDb.DbContext.Insert(new DbProviderConnection() { ConnectionId = 1, SourceId = 1, GlobalConnectionId = 1 });

                transportDb.DbContext.Insert(new DbConsumerConnection() { ConnectionId = 2, DestinationId = 2, GlobalConnectionId = 3 });
                transportDb.DbContext.Insert(new DbProviderConnection() { ConnectionId = 2, SourceId = 1, GlobalConnectionId = 3 });

                transportDb.DbContext.Insert(new DbProviderConnection() { ConnectionId = 12, SourceId = 1, GlobalConnectionId = 15 });


                var r = new Random(1337);
                var consumerConnections = new List<DbConsumerConnection>();
                var providerConnections = new List<DbProviderConnection>();
                for (int i = 0; i < 20000; i++)
                {
                    consumerConnections.Add(new DbConsumerConnection() { ConnectionId = r.Next(1, 1000000), DestinationId = r.Next(1, 2), GlobalConnectionId = r.Next(1, 1000000) });
                    providerConnections.Add(new DbProviderConnection() { ConnectionId = r.Next(1, 1000000), SourceId = r.Next(1, 2), GlobalConnectionId = r.Next(1, 1000000) });
                }
                transportDb.DbContext.InsertAll(consumerConnections);
                transportDb.DbContext.InsertAll(providerConnections);
            }

            var w1 = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
            {

                //var query = db.From<DbConsumerConnection>().FullJoin<DbProviderConnection>((cons, prov) => cons.ConnectionId == prov.ConnectionId).Select<DbConsumerConnection, DbProviderConnection>((cons, prov) => new { MaxCons = Sql.Max<int>(cons.ConnectionId), MaxProv = Sql.Max<int>(prov.ConnectionId) });
                //var queryString = query.ToSelectStatement();

                //var blah = db.Select<List<object>>(query);
                //var curMaxConnectionId = blah.Single().Max();
                //Assert.AreNotEqual(-1, curMaxConnectionId);
            }
            w1.Stop();

            var w2 = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
            {
                var curMaxConnectionId = DbSetupHelper.GetMaxConnectionId(transportDb.DbContext);
                Assert.AreNotEqual(-1, curMaxConnectionId);
            }
            w2.Stop();

            var w3 = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
            {

                var curMaxConnectionIdCons = (transportDb.DbContext.ConsumerConnections.Max(t => (int?)t.ConnectionId) ?? 0);
                var curMaxConnectionIdProv = (transportDb.DbContext.ProviderConnections.Max(t => (int?)t.ConnectionId) ?? 0);
                Assert.AreNotEqual(-1, Math.Max(curMaxConnectionIdCons, curMaxConnectionIdProv));
            }
            w3.Stop();


            Trace.WriteLine("W1: " + w1.Elapsed.TotalSeconds);
            Trace.WriteLine("W2: " + w2.Elapsed.TotalSeconds);
            Trace.WriteLine("W3: " + w3.Elapsed.TotalSeconds);

            var curMaxGlobalConnectionId = DbSetupHelper.GetMaxGlobalConnectionIdOfConnections(transportDb.DbContext);
        }
    }

    [TestMethod]
    public void DeductsGoldWhenExecutingAction()
    {
        //Arrange
        var dbFactory = DbSetupHelper.CreateTestDbFactory();

        DbUser otherUser;

        using (var transportDb = dbFactory.CreateDatabaseWithOpenConnection())
        {
            //var transportDb = new TransportPipesDatabaseOrmLite();
            //var inMemoryConnectionFactory = transportDb.ConnectionFactory;
            //transportDb.DropDb();


            var users = DbSetupHelper.AddDefaultUsers(transportDb.DbContext);
            var adminUser = users.First(t => t.UserName.ToLower() == "admin");
            otherUser = users.First(t => t.UserName.ToLower() == "otheruser");

            //Add actions to Database
            var action1 = new DbAction()
            {
                TransportAction = TransportAction.CreateBuilding,
                DateToBeExecuted = new DateTime(2015, 1, 1, 0, 0, 0),
                ExecutedById = adminUser.Id,
                X = 0,
                Y = 0,
                ActionDetails = new ActionDetailsCreateBuilding() { GameObject = GameObjectType.WindMoily },
                GoldClaimed = 100
            };
            transportDb.DbContext.Insert(action1);

            var action2 = new DbAction()
            {
                TransportAction = TransportAction.CreateBuilding,
                DateToBeExecuted = new DateTime(2015, 1, 1, 0, 1, 0),
                ExecutedById = adminUser.Id,
                X = 1,
                Y = 0,
                ActionDetails = new ActionDetailsCreateBuilding() { GameObject = GameObjectType.GreyPipe },
                GoldClaimed = 150
            };
            transportDb.DbContext.Insert(action2);

            var action3 = new DbAction()
            {
                TransportAction = TransportAction.CreateBuilding,
                DateToBeExecuted = new DateTime(2015, 1, 1, 0, 2, 0),
                ExecutedById = otherUser.Id,
                X = 2,
                Y = 0,
                ActionDetails = new ActionDetailsCreateBuilding() { GameObject = GameObjectType.Storage },
                GoldClaimed = 450
            };
            transportDb.DbContext.Insert(action3);



            //Create actionizer
            var actionizer = new TransportActionizer(dbFactory);

            //Act
            //Execute Actions
            actionizer.ExecuteAllOpenActionsInternal(transportDb, new DateTime(2015, 1, 1, 0, 3, 0));

            //Assert
            int theGlobalConnectionId;

            Assert.AreEqual(0, transportDb.DbContext.Select<DbAction>(t => t.Executed == false).Count);
            Assert.AreEqual(3, transportDb.DbContext.Select<DbAction>(t => t.Executed == true).Count);

            Assert.AreEqual(1, transportDb.DbContext.Select<DbGameObject>(t => t.X == 0 && t.Y == 0 && t.GameObjectType == GameObjectType.WindMoily).Count);
            Assert.AreEqual(1, transportDb.DbContext.Select<DbGameObject>(t => t.X == 1 && t.Y == 0 && t.GameObjectType == GameObjectType.GreyPipe).Count);
            Assert.AreEqual(1, transportDb.DbContext.Select<DbGameObject>(t => t.X == 2 && t.Y == 0 && t.GameObjectType == GameObjectType.Storage).Count);

            theGlobalConnectionId = transportDb.DbContext.Select<DbGameObject>().Select(t => t.GlobalConnectionId).Distinct().Single();

            //Verify that gold is still 0 now
            var users2 = transportDb.DbContext.Select<DbUser>();
            var adminUser2 = users2.First(t => t.UserName.ToLower() == "admin");
            var otherUser2 = users2.First(t => t.UserName.ToLower() == "otheruser");
            Assert.AreEqual(adminUser.Gold - 250, adminUser2.Gold);
            Assert.AreEqual(adminUser.Gold - 450, otherUser2.Gold);
        }
    }
}

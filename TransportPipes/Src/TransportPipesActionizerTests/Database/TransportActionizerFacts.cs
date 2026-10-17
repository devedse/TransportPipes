using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransportPipesActionizer;
using TransportPipesCommon.Enums;
using TransportPipesCommon.GameLogicObjects.GameObjectInfo;
using TransportPipesCommon.Repositories;
using TransportPipesDatabase;
using TransportPipesDatabase.Poco;
using TransportPipesDatabase.Poco.ActionDetails;
using TransportPipesDatabase.Poco.ClientUpdateDetails;
using TransportPipesTestsCommon.Helpers;

namespace TransportPipesActionizerTests.Database
{
    namespace TransportActionizerFacts
    {
        [TestClass]
        public class TheExecuteAllOpenActionsMethod
        {
            [TestMethod]
            public void CorrectlyProcessesThreeBuildActions()
            {
                //Arrange
                var dbFactory = new TransportPipesDbFactory(DbSetupHelper.CreateInMemoryConnectionFactory());

                using (var transportDb = dbFactory.CreateDatabaseWithOpenConnection())
                {
                    transportDb.CreateDbIfNotExists();

                    var users = DbSetupHelper.AddDefaultUsers(transportDb.DbConnection);
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
                    transportDb.DbConnection.Insert(action1);

                    var action2 = new DbAction()
                    {
                        TransportAction = TransportAction.CreateBuilding,
                        DateToBeExecuted = new DateTime(2015, 1, 1, 0, 1, 0),
                        ExecutedById = adminUser.Id,
                        X = 1,
                        Y = 0,
                        ActionDetails = new ActionDetailsCreateBuilding() { GameObject = GameObjectType.GreyPipe }
                    };
                    transportDb.DbConnection.Insert(action2);

                    var action3 = new DbAction()
                    {
                        TransportAction = TransportAction.CreateBuilding,
                        DateToBeExecuted = new DateTime(2015, 1, 1, 0, 2, 0),
                        ExecutedById = adminUser.Id,
                        X = 2,
                        Y = 0,
                        ActionDetails = new ActionDetailsCreateBuilding() { GameObject = GameObjectType.Storage }
                    };
                    transportDb.DbConnection.Insert(action3);



                    //Create actionizer
                    var actionizer = new TransportActionizer(dbFactory);

                    //Act
                    actionizer.ExecuteAllOpenActionsInternal(transportDb, new DateTime(2015, 1, 1, 0, 3, 0));

                    //Assert

                    int theGlobalConnectionId;

                    Assert.AreEqual(0, transportDb.DbConnection.Select<DbAction>(t => t.Executed == false).Count);
                    Assert.AreEqual(3, transportDb.DbConnection.Select<DbAction>(t => t.Executed == true).Count);

                    Assert.AreEqual(1, transportDb.DbConnection.Select<DbGameObject>(t => t.X == 0 && t.Y == 0 && t.GameObjectType == GameObjectType.WindMoily).Count);
                    Assert.AreEqual(1, transportDb.DbConnection.Select<DbGameObject>(t => t.X == 1 && t.Y == 0 && t.GameObjectType == GameObjectType.GreyPipe).Count);
                    Assert.AreEqual(1, transportDb.DbConnection.Select<DbGameObject>(t => t.X == 2 && t.Y == 0 && t.GameObjectType == GameObjectType.Storage).Count);

                    theGlobalConnectionId = transportDb.DbConnection.Select<DbGameObject>().Select(t => t.GlobalConnectionId).Distinct().Single();

                    var allClientUpdates = transportDb.DbConnection.Select<DbClientUpdate>();
                    Assert.AreEqual(6, allClientUpdates.Count);

                    Assert.AreEqual(6, allClientUpdates.Count(t => t.ExecutedById == 1));

                    var theRefreshesRequired = allClientUpdates.Select(t => t.ClientUpdateDetails as ClientUpdateDetailsTileNeedsRefresh).Where(t => t != null).ToList();
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
                var dbFactory = new TransportPipesDbFactory(DbSetupHelper.CreateInMemoryConnectionFactory());

                DbUser otherUser;

                using (var transportDb = dbFactory.CreateDatabaseWithOpenConnection())
                {
                    //var transportDb = new TransportPipesDatabaseOrmLite();
                    //var inMemoryConnectionFactory = transportDb.ConnectionFactory;
                    //transportDb.DropDb();

                    transportDb.CreateDbIfNotExists();

                    var users = DbSetupHelper.AddDefaultUsers(transportDb.DbConnection);
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
                    transportDb.DbConnection.Insert(action1);

                    var action2 = new DbAction()
                    {
                        TransportAction = TransportAction.CreateBuilding,
                        DateToBeExecuted = new DateTime(2015, 1, 1, 0, 1, 0),
                        ExecutedById = adminUser.Id,
                        X = 1,
                        Y = 0,
                        ActionDetails = new ActionDetailsCreateBuilding() { GameObject = GameObjectType.GreyPipe }
                    };
                    transportDb.DbConnection.Insert(action2);

                    var action3 = new DbAction()
                    {
                        TransportAction = TransportAction.CreateBuilding,
                        DateToBeExecuted = new DateTime(2015, 1, 1, 0, 2, 0),
                        ExecutedById = otherUser.Id,
                        X = 2,
                        Y = 0,
                        ActionDetails = new ActionDetailsCreateBuilding() { GameObject = GameObjectType.Storage }
                    };
                    transportDb.DbConnection.Insert(action3);



                    //Create actionizer
                    var actionizer = new TransportActionizer(dbFactory);

                    //Execute Actions
                    actionizer.ExecuteAllOpenActionsInternal(transportDb, new DateTime(2015, 1, 1, 0, 3, 0));

                    int theGlobalConnectionId;

                    Assert.AreEqual(0, transportDb.DbConnection.Select<DbAction>(t => t.Executed == false).Count);
                    Assert.AreEqual(3, transportDb.DbConnection.Select<DbAction>(t => t.Executed == true).Count);

                    Assert.AreEqual(1, transportDb.DbConnection.Select<DbGameObject>(t => t.X == 0 && t.Y == 0 && t.GameObjectType == GameObjectType.WindMoily).Count);
                    Assert.AreEqual(1, transportDb.DbConnection.Select<DbGameObject>(t => t.X == 1 && t.Y == 0 && t.GameObjectType == GameObjectType.GreyPipe).Count);
                    Assert.AreEqual(1, transportDb.DbConnection.Select<DbGameObject>(t => t.X == 2 && t.Y == 0 && t.GameObjectType == GameObjectType.Storage).Count);

                    theGlobalConnectionId = transportDb.DbConnection.Select<DbGameObject>().Select(t => t.GlobalConnectionId).Distinct().Single();

                    //Verify that gold is still 0 now
                    var users2 = transportDb.DbConnection.Select<DbUser>();
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
                    transportDb.DbConnection.Insert(action4);


                    //Execute Actions again
                    actionizer.ExecuteAllOpenActionsInternal(transportDb, new DateTime(2015, 1, 1, 0, 4, 0));


                    //Gold should not not be 0 anymore because a new building was added
                    var users3 = transportDb.DbConnection.Select<DbUser>();

                    var energyGeneratedPerTickByWindMoily = GameResourcesRepository.DetermineResourceOfTypeProduced(GameObjectType.WindMoily, BackendResourceType.Energy);
                    var expectedGold = BackendResourceWorthDeterminer.AllResourceTypeValues[BackendResourceType.Energy].ValueForPlayers * 60 * energyGeneratedPerTickByWindMoily; //60 seconds passed and trade to player for energy

                    Assert.IsTrue(TestHelper.AlmostEqual(expectedGold, users3.Single(t => t.UserName.ToLower() == "admin").Gold));
                    Assert.IsTrue(TestHelper.AlmostEqual(expectedGold * -1, users3.Single(t => t.UserName.ToLower() == "otheruser").Gold));

                    //Also the all GlobalConnections should have updated DateTime's
                    var globalConnections = transportDb.DbConnection.Select<DbGlobalConnection>();
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
                var dbFactory = new TransportPipesDbFactory(DbSetupHelper.CreateInMemoryConnectionFactory());
                using (var transportDb = dbFactory.CreateDatabaseWithOpenConnection())
                {
                    transportDb.CreateDbIfNotExists();

                    //var transportDb = new TransportPipesDatabaseOrmLite();
                    //var inMemoryConnectionFactory = transportDb.ConnectionFactory;


                    bool recreateDb = true;

                    DbUser adminUser = null;
                    if (recreateDb)
                    {
                        transportDb.DropDb();

                        transportDb.CreateDbIfNotExists();

                        var users = DbSetupHelper.AddDefaultUsers(transportDb.DbConnection);
                        adminUser = users.Single(t => t.UserName.ToLower() == "admin");
                    }

                    if (recreateDb)
                    {
                        transportDb.DbConnection.Insert(new DbGameObject(5, 6, GameObjectType.WindMoily, adminUser.Id));
                        transportDb.DbConnection.Insert(new DbGameObject(5, 8, GameObjectType.Storage, adminUser.Id));

                        transportDb.DbConnection.Insert(new DbConsumerConnection() { ConnectionId = 1, DestinationId = 2, GlobalConnectionId = 1 });
                        transportDb.DbConnection.Insert(new DbProviderConnection() { ConnectionId = 1, SourceId = 1, GlobalConnectionId = 1 });

                        transportDb.DbConnection.Insert(new DbConsumerConnection() { ConnectionId = 2, DestinationId = 2, GlobalConnectionId = 3 });
                        transportDb.DbConnection.Insert(new DbProviderConnection() { ConnectionId = 2, SourceId = 1, GlobalConnectionId = 3 });

                        transportDb.DbConnection.Insert(new DbProviderConnection() { ConnectionId = 12, SourceId = 1, GlobalConnectionId = 15 });


                        var r = new Random(1337);
                        for (int i = 0; i < 20000; i++)
                        {
                            transportDb.DbConnection.Insert(new DbConsumerConnection() { ConnectionId = r.Next(1, 1000000), DestinationId = r.Next(1, 2), GlobalConnectionId = r.Next(1, 1000000) });
                            transportDb.DbConnection.Insert(new DbProviderConnection() { ConnectionId = r.Next(1, 1000000), SourceId = r.Next(1, 2), GlobalConnectionId = r.Next(1, 1000000) });
                        }
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
                        var curMaxConnectionId = transportDb.DbConnection.Scalar<int>("SELECT MAX(ConId) from (select Max(ConnectionId) as ConId from DbConsumerConnection UNION select Max(ConnectionId) as ConId from DbProviderConnection) as s");
                        Assert.AreNotEqual(-1, curMaxConnectionId);
                    }
                    w2.Stop();

                    var w3 = Stopwatch.StartNew();
                    for (int i = 0; i < 100; i++)
                    {

                        var curMaxConnectionIdCons = transportDb.DbConnection.Scalar<int>("SELECT MAX(ConnectionId) from DbConsumerConnection");
                        var curMaxConnectionIdProv = transportDb.DbConnection.Scalar<int>("SELECT MAX(ConnectionId) from DbProviderConnection");
                        Assert.AreNotEqual(-1, Math.Max(curMaxConnectionIdCons, curMaxConnectionIdProv));
                    }
                    w3.Stop();


                    Trace.WriteLine("W1: " + w1.Elapsed.TotalSeconds);
                    Trace.WriteLine("W2: " + w2.Elapsed.TotalSeconds);
                    Trace.WriteLine("W3: " + w3.Elapsed.TotalSeconds);

                    var curMaxGlobalConnectionId = transportDb.DbConnection.Scalar<int>("SELECT MAX(GlobConId) from (select GlobalConnectionId as GlobConId from DbConsumerConnection UNION select GlobalConnectionId as GlobConId from DbProviderConnection) as s");
                }
            }

            [TestMethod]
            public void DeductsGoldWhenExecutingAction()
            {
                //Arrange
                var dbFactory = new TransportPipesDbFactory(DbSetupHelper.CreateInMemoryConnectionFactory());

                DbUser otherUser;

                using (var transportDb = dbFactory.CreateDatabaseWithOpenConnection())
                {
                    //var transportDb = new TransportPipesDatabaseOrmLite();
                    //var inMemoryConnectionFactory = transportDb.ConnectionFactory;
                    //transportDb.DropDb();

                    transportDb.CreateDbIfNotExists();

                    var users = DbSetupHelper.AddDefaultUsers(transportDb.DbConnection);
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
                    transportDb.DbConnection.Insert(action1);

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
                    transportDb.DbConnection.Insert(action2);

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
                    transportDb.DbConnection.Insert(action3);



                    //Create actionizer
                    var actionizer = new TransportActionizer(dbFactory);

                    //Act
                    //Execute Actions
                    actionizer.ExecuteAllOpenActionsInternal(transportDb, new DateTime(2015, 1, 1, 0, 3, 0));

                    //Assert
                    int theGlobalConnectionId;

                    Assert.AreEqual(0, transportDb.DbConnection.Select<DbAction>(t => t.Executed == false).Count);
                    Assert.AreEqual(3, transportDb.DbConnection.Select<DbAction>(t => t.Executed == true).Count);

                    Assert.AreEqual(1, transportDb.DbConnection.Select<DbGameObject>(t => t.X == 0 && t.Y == 0 && t.GameObjectType == GameObjectType.WindMoily).Count);
                    Assert.AreEqual(1, transportDb.DbConnection.Select<DbGameObject>(t => t.X == 1 && t.Y == 0 && t.GameObjectType == GameObjectType.GreyPipe).Count);
                    Assert.AreEqual(1, transportDb.DbConnection.Select<DbGameObject>(t => t.X == 2 && t.Y == 0 && t.GameObjectType == GameObjectType.Storage).Count);

                    theGlobalConnectionId = transportDb.DbConnection.Select<DbGameObject>().Select(t => t.GlobalConnectionId).Distinct().Single();

                    //Verify that gold is still 0 now
                    var users2 = transportDb.DbConnection.Select<DbUser>();
                    var adminUser2 = users2.First(t => t.UserName.ToLower() == "admin");
                    var otherUser2 = users2.First(t => t.UserName.ToLower() == "otheruser");
                    Assert.AreEqual(adminUser.Gold - 250, adminUser2.Gold);
                    Assert.AreEqual(adminUser.Gold - 450, otherUser2.Gold);
                }
            }
        }
    }
}

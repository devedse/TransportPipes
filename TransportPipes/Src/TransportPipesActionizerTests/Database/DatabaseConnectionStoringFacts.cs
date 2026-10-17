using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransportPipesActionizer.Calculators;
using TransportPipesCommon.Enums;
using TransportPipesDatabase;
using TransportPipesDatabase.Poco;
using TransportPipesDatabase.Poco.CombinedPoco;

namespace TransportPipesActionizerTests.Database
{
    namespace DatabaseConnectionFinderFacts
    {
        [TestClass]
        public class DatabaseStoringCorrectConnections
        {
            [TestMethod]
            public void WorksForASimpleConnection()
            {
                var dbFactory = new TransportPipesDbFactory(DbSetupHelper.CreateInMemoryConnectionFactory());
                using (var database = dbFactory.CreateDatabaseWithOpenConnection())
                {


                    database.CreateDbIfNotExists();
                    var defaultUsers = DbSetupHelper.AddDefaultUsers(database.DbConnection);

                    var mapString = "" +
                        "          " + Environment.NewLine +
                        "  W       " + Environment.NewLine +
                        "  +       " + Environment.NewLine +
                        "  +       " + Environment.NewLine +
                        "  +M      " + Environment.NewLine +
                        "   +      " + Environment.NewLine +
                        "   +      " + Environment.NewLine +
                        "   +      " + Environment.NewLine +
                        "   O      " + Environment.NewLine +
                        "          ";

                    DbSetupHelper.AddDbGameObjectsByStringDefinition(database.DbConnection, mapString, defaultUsers[0].Id);


                    //Act on connectionfinder
                    var connectionFinder = new ConnectionFinder(database);

                    connectionFinder.RedetermineAllConnectionsForGameObject(2, 2, new DateTime(2015, 1, 1)); //Start pipe

                    var allGameObjects = DbSetupHelper.GetAllDbGameObjects(database.DbConnection);

                    //Assert the result
                    var windmoily = allGameObjects.Single(t => t.GameObjectType == GameObjectType.WindMoily);
                    var ironmine = allGameObjects.Single(t => t.GameObjectType == GameObjectType.IronMine);
                    var storage = allGameObjects.Single(t => t.GameObjectType == GameObjectType.Storage);

                    var result = database.ObtainListOfConsumerProviderConnections(windmoily.GlobalConnectionId);


                    var resultTest = database.DbConnection.Select<ConsumerProviderConnection>(database.DbConnection.From<DbConsumerConnection>().Join<DbProviderConnection>((cons, prov) => cons.ConnectionId == prov.ConnectionId));
                    Assert.AreEqual(1, resultTest.Select(t => t.GlobalConnectionId).Distinct().Count());
                    Assert.AreEqual(2, resultTest.Select(t => t.ConnectionId).Distinct().Count());
                    Assert.AreEqual(2, resultTest.Count);

                    Assert.AreEqual(1, result.Select(t => t.GlobalConnectionId).Distinct().Count());
                    Assert.AreEqual(2, result.Select(t => t.ConnectionId).Distinct().Count());
                    Assert.AreEqual(2, result.Count);

                    //Could be multiple but is just one in this case because we only have 1 provider and 1 consumer here
                    var conn1 = result.Single(t => t.DestinationId == ironmine.Id);
                    var conn2 = result.Single(t => t.SourceId == ironmine.Id);

                    Assert.AreNotEqual(conn1.ConnectionId, conn2.ConnectionId);
                    Assert.AreEqual(conn1.GlobalConnectionId, conn2.GlobalConnectionId);

                    Assert.AreEqual(windmoily.Id, conn1.SourceId);
                    Assert.AreEqual(ironmine.Id, conn1.DestinationId);

                    Assert.AreEqual(ironmine.Id, conn2.SourceId);
                    Assert.AreEqual(storage.Id, conn2.DestinationId);

                    Assert.IsTrue(allGameObjects.All(t => t.GlobalConnectionId == conn1.GlobalConnectionId));
                    Assert.AreEqual(1, allGameObjects.Select(t => t.GlobalConnectionId).Distinct().Count());

                }
            }

            [TestMethod]
            public void WorksWithAnUnconnectedBunchOfPipes()
            {
                var dbFactory = new TransportPipesDbFactory(DbSetupHelper.CreateInMemoryConnectionFactory());
                using (var database = dbFactory.CreateDatabaseWithOpenConnection())
                {

                    database.CreateDbIfNotExists();
                    var defaultUsers = DbSetupHelper.AddDefaultUsers(database.DbConnection);

                    var mapString = "" +
                        "          " + Environment.NewLine +
                        "  W       " + Environment.NewLine +
                        "  +       " + Environment.NewLine +
                        "  +       " + Environment.NewLine +
                        "  +M++++++" + Environment.NewLine +
                        "   +      " + Environment.NewLine +
                        "   +      " + Environment.NewLine +
                        "   +      " + Environment.NewLine +
                        "   O      " + Environment.NewLine +
                        "          ";

                    DbSetupHelper.AddDbGameObjectsByStringDefinition(database.DbConnection, mapString, defaultUsers[0].Id);


                    //Act on connectionfinder
                    var connectionFinder = new ConnectionFinder(database);

                    connectionFinder.RedetermineAllConnectionsForGameObject(2, 2, new DateTime(2015, 1, 1)); //Start pipe

                    var allGameObjects = DbSetupHelper.GetAllDbGameObjects(database.DbConnection);

                    //Assert the result
                    var windmoily = allGameObjects.Single(t => t.GameObjectType == GameObjectType.WindMoily);
                    var ironmine = allGameObjects.Single(t => t.GameObjectType == GameObjectType.IronMine);
                    var storage = allGameObjects.Single(t => t.GameObjectType == GameObjectType.Storage);

                    var result = database.ObtainListOfConsumerProviderConnections(windmoily.GlobalConnectionId);

                    var resultTest = database.DbConnection.Select<ConsumerProviderConnection>(database.DbConnection.From<DbConsumerConnection>().Join<DbProviderConnection>((cons, prov) => cons.ConnectionId == prov.ConnectionId));
                    Assert.AreEqual(1, resultTest.Select(t => t.GlobalConnectionId).Distinct().Count());
                    Assert.AreEqual(2, resultTest.Select(t => t.ConnectionId).Distinct().Count());
                    Assert.AreEqual(2, resultTest.Count);

                    Assert.AreEqual(1, result.Select(t => t.GlobalConnectionId).Distinct().Count());
                    Assert.AreEqual(2, result.Select(t => t.ConnectionId).Distinct().Count());
                    Assert.AreEqual(2, result.Count);

                    //Could be multiple but is just one in this case because we only have 1 provider and 1 consumer here
                    var conn1 = result.Single(t => t.DestinationId == ironmine.Id);
                    var conn2 = result.Single(t => t.SourceId == ironmine.Id);

                    Assert.AreNotEqual(conn1.ConnectionId, conn2.ConnectionId);
                    Assert.AreEqual(conn1.GlobalConnectionId, conn2.GlobalConnectionId);

                    Assert.AreEqual(windmoily.Id, conn1.SourceId);
                    Assert.AreEqual(ironmine.Id, conn1.DestinationId);

                    Assert.AreEqual(ironmine.Id, conn2.SourceId);
                    Assert.AreEqual(storage.Id, conn2.DestinationId);

                    Assert.IsTrue(allGameObjects.All(t => t.GlobalConnectionId == conn1.GlobalConnectionId));
                    Assert.AreEqual(1, allGameObjects.Select(t => t.GlobalConnectionId).Distinct().Count());
                }
            }




            [TestMethod]
            public void WorksForAComplexConnection()
            {
                var dbFactory = new TransportPipesDbFactory(DbSetupHelper.CreateInMemoryConnectionFactory());
                using (var database = dbFactory.CreateDatabaseWithOpenConnection())
                {
                    database.CreateDbIfNotExists();
                    var defaultUsers = DbSetupHelper.AddDefaultUsers(database.DbConnection);

                    var mapString = "" +
                        "                     " + Environment.NewLine +
                        "  S  S        W      " + Environment.NewLine +
                        "  +  +        +      " + Environment.NewLine +
                        "  +++++++M++++F++O   " + Environment.NewLine +
                        "  +                  " + Environment.NewLine +
                        "  +++++++M++         " + Environment.NewLine +
                        "  +        +         " + Environment.NewLine +
                        "  +++++++M++++F++O   " + Environment.NewLine +
                        "           +         " + Environment.NewLine +
                        "           W         ";

                    DbSetupHelper.AddDbGameObjectsByStringDefinition(database.DbConnection, mapString, defaultUsers[0].Id);


                    //Act on connectionfinder
                    var connectionFinder = new ConnectionFinder(database);

                    connectionFinder.RedetermineAllConnectionsForGameObject(2, 2, new DateTime(2015, 1, 1)); //Start pipe

                    var allGameObjects = DbSetupHelper.GetAllDbGameObjects(database.DbConnection);

                    //Assert the result
                    var solar1 = allGameObjects.Single(t => t.X == 2 && t.Y == 1);
                    var solar2 = allGameObjects.Single(t => t.X == 5 && t.Y == 1);

                    var windmoily1 = allGameObjects.Single(t => t.X == 14 && t.Y == 1);
                    var windmoily2 = allGameObjects.Single(t => t.X == 11 && t.Y == 9);

                    var ironmine1 = allGameObjects.Single(t => t.X == 9 && t.Y == 3);
                    var ironmine2 = allGameObjects.Single(t => t.X == 9 && t.Y == 5);
                    var ironmine3 = allGameObjects.Single(t => t.X == 9 && t.Y == 7);

                    var factory1 = allGameObjects.Single(t => t.X == 14 && t.Y == 3);
                    var factory2 = allGameObjects.Single(t => t.X == 14 && t.Y == 7);

                    var storage1 = allGameObjects.Single(t => t.X == 17 && t.Y == 3);
                    var storage2 = allGameObjects.Single(t => t.X == 17 && t.Y == 7);

                    Assert.AreEqual(GameObjectType.SolarPanel, solar1.GameObjectType);
                    Assert.AreEqual(GameObjectType.SolarPanel, solar2.GameObjectType);
                    Assert.AreEqual(GameObjectType.WindMoily, windmoily1.GameObjectType);
                    Assert.AreEqual(GameObjectType.WindMoily, windmoily2.GameObjectType);
                    Assert.AreEqual(GameObjectType.IronMine, ironmine1.GameObjectType);
                    Assert.AreEqual(GameObjectType.IronMine, ironmine2.GameObjectType);
                    Assert.AreEqual(GameObjectType.IronMine, ironmine3.GameObjectType);
                    Assert.AreEqual(GameObjectType.FactorySmall, factory1.GameObjectType);
                    Assert.AreEqual(GameObjectType.FactorySmall, factory2.GameObjectType);
                    Assert.AreEqual(GameObjectType.Storage, storage1.GameObjectType);
                    Assert.AreEqual(GameObjectType.Storage, storage2.GameObjectType);

                    var result = database.ObtainListOfConsumerProviderConnections(solar1.GlobalConnectionId);
                    var result2 = database.ObtainListOfConsumerProviderConnections(new List<int>() { solar1.GlobalConnectionId, solar2.GlobalConnectionId });

                    Assert.AreEqual(1, result.Select(t => t.GlobalConnectionId).Distinct().Count());
                    Assert.AreEqual(6, result.Select(t => t.ConnectionId).Distinct().Count());
                    Assert.AreEqual(17, result.Count);

                    Assert.AreEqual(1, result2.Select(t => t.GlobalConnectionId).Distinct().Count());
                    Assert.AreEqual(6, result2.Select(t => t.ConnectionId).Distinct().Count());
                    Assert.AreEqual(17, result2.Count);


                    //The where is also important here else in this case the IronMine's will provide to themselves because they are both provider and consumer
                    //TODO: Move this select statement to the database once I know exactly what we need here
                    var resultTest = database.DbConnection.Select<ConsumerProviderConnection>(database.DbConnection.From<DbConsumerConnection>().Join<DbProviderConnection>((cons, prov) => cons.ConnectionId == prov.ConnectionId)).Where(t => t.SourceId != t.DestinationId).ToList();
                    Assert.AreEqual(1, resultTest.Select(t => t.GlobalConnectionId).Distinct().Count());
                    Assert.AreEqual(6, resultTest.Select(t => t.ConnectionId).Distinct().Count());
                    Assert.AreEqual(17, resultTest.Count);

                    //Obtain the connection ID's by just taking one of the connections inside and then obtaining the id
                    var conn1id = result.Single(t => t.SourceId == solar1.Id && t.DestinationId == ironmine1.Id).ConnectionId; //Solars to Mines
                    var conn2id = result.Single(t => t.SourceId == windmoily1.Id && t.DestinationId == factory1.Id).ConnectionId; //Windmoily1 to Factory1
                    var conn3id = result.Single(t => t.SourceId == ironmine1.Id && t.DestinationId == factory1.Id).ConnectionId; //Mine1 to Factory1
                    var conn4id = result.Single(t => t.SourceId == ironmine2.Id && t.DestinationId == factory2.Id).ConnectionId; //Mine2/3 and Windmoily2 to Factory2 + WindMoily provides to mines
                    var conn5id = result.Single(t => t.SourceId == factory1.Id && t.DestinationId == storage1.Id).ConnectionId; //Factory1 to Storage1
                    var conn6id = result.Single(t => t.SourceId == factory2.Id && t.DestinationId == storage2.Id).ConnectionId; //Factory2 to Storage2

                    //Ensure we got all the 6 different connection ID's
                    var connList = new List<int>() { conn1id, conn2id, conn3id, conn4id, conn5id, conn6id };
                    Assert.AreEqual(6, connList.Distinct().Count());

                    //Ensure the connection list is actually in the database like this
                    foreach (var connId in connList)
                    {
                        var conConsumer = database.DbConnection.Select<DbConsumerConnection>(t => t.ConnectionId == connId);
                        var conProvider = database.DbConnection.Select<DbProviderConnection>(t => t.ConnectionId == connId);

                        Assert.IsTrue(conConsumer.Count > 0);
                        Assert.IsTrue(conProvider.Count > 0);
                    }

                    //Ensure that all the connections got the right number of ConsumerProviderConnections
                    Assert.AreEqual(6, result.Count(t => t.ConnectionId == conn1id));
                    Assert.AreEqual(1, result.Count(t => t.ConnectionId == conn2id));
                    Assert.AreEqual(1, result.Count(t => t.ConnectionId == conn3id));
                    Assert.AreEqual(7, result.Count(t => t.ConnectionId == conn4id));
                    Assert.AreEqual(1, result.Count(t => t.ConnectionId == conn5id));
                    Assert.AreEqual(1, result.Count(t => t.ConnectionId == conn6id));

                    //Just to be sure also test this one for result2
                    Assert.AreEqual(6, result2.Count(t => t.ConnectionId == conn1id));
                    Assert.AreEqual(1, result2.Count(t => t.ConnectionId == conn2id));
                    Assert.AreEqual(1, result2.Count(t => t.ConnectionId == conn3id));
                    Assert.AreEqual(7, result2.Count(t => t.ConnectionId == conn4id));
                    Assert.AreEqual(1, result2.Count(t => t.ConnectionId == conn5id));
                    Assert.AreEqual(1, result2.Count(t => t.ConnectionId == conn6id));


                    //Verify if all ConsumerProvidersConnections are as expected
                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn1id && t.SourceId == solar1.Id && t.DestinationId == ironmine1.Id));
                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn1id && t.SourceId == solar1.Id && t.DestinationId == ironmine2.Id));
                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn1id && t.SourceId == solar1.Id && t.DestinationId == ironmine3.Id));
                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn1id && t.SourceId == solar2.Id && t.DestinationId == ironmine1.Id));
                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn1id && t.SourceId == solar2.Id && t.DestinationId == ironmine2.Id));
                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn1id && t.SourceId == solar2.Id && t.DestinationId == ironmine3.Id));

                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn2id && t.SourceId == windmoily1.Id && t.DestinationId == factory1.Id));

                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn3id && t.SourceId == ironmine1.Id && t.DestinationId == factory1.Id));

                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn4id && t.SourceId == ironmine2.Id && t.DestinationId == factory2.Id));
                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn4id && t.SourceId == ironmine3.Id && t.DestinationId == factory2.Id));
                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn4id && t.SourceId == windmoily2.Id && t.DestinationId == factory2.Id));
                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn4id && t.SourceId == windmoily2.Id && t.DestinationId == ironmine2.Id));
                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn4id && t.SourceId == windmoily2.Id && t.DestinationId == ironmine2.Id));
                    //Ironmines are providing to eachother but the calculation part will ensure this still keeps on working because they don't provide resources that they can consumer form eachother
                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn4id && t.SourceId == ironmine2.Id && t.DestinationId == ironmine3.Id));
                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn4id && t.SourceId == ironmine3.Id && t.DestinationId == ironmine2.Id));

                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn5id && t.SourceId == factory1.Id && t.DestinationId == storage1.Id));

                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn6id && t.SourceId == factory2.Id && t.DestinationId == storage2.Id));

                    //Verify if all gameobjects were set to the right GlobalConnectionId
                    Assert.IsTrue(allGameObjects.All(t => t.GlobalConnectionId == result.First().GlobalConnectionId));
                    Assert.AreEqual(1, allGameObjects.Select(t => t.GlobalConnectionId).Distinct().Count());
                }
            }


            [TestMethod]
            public void ClearsOutComplexExistingConnectionBeforeApplyingNewConnections()
            {
                var dbFactory = new TransportPipesDbFactory(DbSetupHelper.CreateInMemoryConnectionFactory());
                using (var database = dbFactory.CreateDatabaseWithOpenConnection())
                {
                    database.CreateDbIfNotExists();
                    var defaultUsers = DbSetupHelper.AddDefaultUsers(database.DbConnection);

                    var mapString = "" +
                        "                     " + Environment.NewLine +
                        "  S  S        W      " + Environment.NewLine +
                        "  +  +        +      " + Environment.NewLine +
                        "  +++++++M++++F++O   " + Environment.NewLine +
                        "  +                  " + Environment.NewLine +
                        "  +++++++M++         " + Environment.NewLine +
                        "  +        +         " + Environment.NewLine +
                        "  +++++++M++++F++O   " + Environment.NewLine +
                        "           +         " + Environment.NewLine +
                        "           W         ";

                    DbSetupHelper.AddDbGameObjectsByStringDefinition(database.DbConnection, mapString, defaultUsers[0].Id);


                    //Act on connectionfinder
                    var connectionFinder = new ConnectionFinder(database);

                    connectionFinder.RedetermineAllConnectionsForGameObject(2, 2, new DateTime(2015, 1, 1)); //Start pipe

                    var allGameObjects = DbSetupHelper.GetAllDbGameObjects(database.DbConnection);

                    //Assert the result
                    var solar1 = allGameObjects.Single(t => t.X == 2 && t.Y == 1);
                    var solar2 = allGameObjects.Single(t => t.X == 5 && t.Y == 1);

                    var windmoily1 = allGameObjects.Single(t => t.X == 14 && t.Y == 1);
                    var windmoily2 = allGameObjects.Single(t => t.X == 11 && t.Y == 9);

                    var ironmine1 = allGameObjects.Single(t => t.X == 9 && t.Y == 3);
                    var ironmine2 = allGameObjects.Single(t => t.X == 9 && t.Y == 5);
                    var ironmine3 = allGameObjects.Single(t => t.X == 9 && t.Y == 7);

                    var factory1 = allGameObjects.Single(t => t.X == 14 && t.Y == 3);
                    var factory2 = allGameObjects.Single(t => t.X == 14 && t.Y == 7);

                    var storage1 = allGameObjects.Single(t => t.X == 17 && t.Y == 3);
                    var storage2 = allGameObjects.Single(t => t.X == 17 && t.Y == 7);

                    Assert.AreEqual(GameObjectType.SolarPanel, solar1.GameObjectType);
                    Assert.AreEqual(GameObjectType.SolarPanel, solar2.GameObjectType);
                    Assert.AreEqual(GameObjectType.WindMoily, windmoily1.GameObjectType);
                    Assert.AreEqual(GameObjectType.WindMoily, windmoily2.GameObjectType);
                    Assert.AreEqual(GameObjectType.IronMine, ironmine1.GameObjectType);
                    Assert.AreEqual(GameObjectType.IronMine, ironmine2.GameObjectType);
                    Assert.AreEqual(GameObjectType.IronMine, ironmine3.GameObjectType);
                    Assert.AreEqual(GameObjectType.FactorySmall, factory1.GameObjectType);
                    Assert.AreEqual(GameObjectType.FactorySmall, factory2.GameObjectType);
                    Assert.AreEqual(GameObjectType.Storage, storage1.GameObjectType);
                    Assert.AreEqual(GameObjectType.Storage, storage2.GameObjectType);


                    int conn1idpre = 0;
                    int conn2idpre = 0;
                    int conn3idpre = 0;
                    int conn4idpre = 0;
                    int conn5idpre = 0;
                    int conn6idpre = 0;

                    var result = database.ObtainListOfConsumerProviderConnections(solar1.GlobalConnectionId);
                    Assert.AreEqual(1, result.Select(t => t.GlobalConnectionId).Distinct().Count());
                    Assert.AreEqual(6, result.Select(t => t.ConnectionId).Distinct().Count());
                    Assert.AreEqual(17, result.Count);

                    //The where is also important here else in this case the IronMine's will provide to themselves because they are both provider and consumer
                    //TODO: Move this select statement to the database once I know exactly what we need here
                    var resultTest = database.DbConnection.Select<ConsumerProviderConnection>(database.DbConnection.From<DbConsumerConnection>().Join<DbProviderConnection>((cons, prov) => cons.ConnectionId == prov.ConnectionId)).Where(t => t.SourceId != t.DestinationId).ToList();
                    Assert.AreEqual(1, resultTest.Select(t => t.GlobalConnectionId).Distinct().Count());
                    Assert.AreEqual(6, resultTest.Select(t => t.ConnectionId).Distinct().Count());
                    Assert.AreEqual(17, resultTest.Count);

                    //Obtain the connection ID's by just taking one of the connections inside and then obtaining the id
                    conn1idpre = result.Single(t => t.SourceId == solar1.Id && t.DestinationId == ironmine1.Id).ConnectionId; //Solars to Mines
                    conn2idpre = result.Single(t => t.SourceId == windmoily1.Id && t.DestinationId == factory1.Id).ConnectionId; //Windmoily1 to Factory1
                    conn3idpre = result.Single(t => t.SourceId == ironmine1.Id && t.DestinationId == factory1.Id).ConnectionId; //Mine1 to Factory1
                    conn4idpre = result.Single(t => t.SourceId == ironmine2.Id && t.DestinationId == factory2.Id).ConnectionId; //Mine2/3 and Windmoily2 to Factory2 + WindMoily provides to mines
                    conn5idpre = result.Single(t => t.SourceId == factory1.Id && t.DestinationId == storage1.Id).ConnectionId; //Factory1 to Storage1
                    conn6idpre = result.Single(t => t.SourceId == factory2.Id && t.DestinationId == storage2.Id).ConnectionId; //Factory2 to Storage2


                    //Add some in between connection
                    var simpleMapString = "                                   S++M++O  ";
                    DbSetupHelper.AddDbGameObjectsByStringDefinition(database.DbConnection, simpleMapString, defaultUsers[0].Id);
                    connectionFinder.RedetermineAllConnectionsForGameObject(36, 0, new DateTime(2015, 1, 1)); //Start of the simple thing



                    var lastGlobalConnection = database.DbConnection.Scalar<int>("SELECT MAX(GlobConId) from (select GlobalConnectionId as GlobConId from DbConsumerConnection UNION select GlobalConnectionId as GlobConId from DbProviderConnection) as s");





                    connectionFinder.RedetermineAllConnectionsForGameObject(2, 2, new DateTime(2015, 1, 1)); //Start pipe

                    allGameObjects = DbSetupHelper.GetAllDbGameObjects(database.DbConnection);

                    solar1 = allGameObjects.Single(t => t.X == 2 && t.Y == 1);
                    solar2 = allGameObjects.Single(t => t.X == 5 && t.Y == 1);

                    windmoily1 = allGameObjects.Single(t => t.X == 14 && t.Y == 1);
                    windmoily2 = allGameObjects.Single(t => t.X == 11 && t.Y == 9);

                    ironmine1 = allGameObjects.Single(t => t.X == 9 && t.Y == 3);
                    ironmine2 = allGameObjects.Single(t => t.X == 9 && t.Y == 5);
                    ironmine3 = allGameObjects.Single(t => t.X == 9 && t.Y == 7);

                    factory1 = allGameObjects.Single(t => t.X == 14 && t.Y == 3);
                    factory2 = allGameObjects.Single(t => t.X == 14 && t.Y == 7);

                    storage1 = allGameObjects.Single(t => t.X == 17 && t.Y == 3);
                    storage2 = allGameObjects.Single(t => t.X == 17 && t.Y == 7);

                    result = database.ObtainListOfConsumerProviderConnections(solar1.GlobalConnectionId);

                    Assert.AreEqual(1, result.Select(t => t.GlobalConnectionId).Distinct().Count());
                    Assert.AreEqual(6, result.Select(t => t.ConnectionId).Distinct().Count());
                    Assert.AreEqual(17, result.Count);


                    //The where is also important here else in this case the IronMine's will provide to themselves because they are both provider and consumer
                    //TODO: Move this select statement to the database once I know exactly what we need here

                    var allResults = database.DbConnection.Select<ConsumerProviderConnection>(database.DbConnection.From<DbConsumerConnection>().Join<DbProviderConnection>((cons, prov) => cons.ConnectionId == prov.ConnectionId)).Where(t => t.SourceId != t.DestinationId).ToList();
                    Assert.AreEqual(2, allResults.Select(t => t.GlobalConnectionId).Distinct().Count());

                    resultTest = database.DbConnection.Select<ConsumerProviderConnection>(database.DbConnection.From<DbConsumerConnection>().Join<DbProviderConnection>((cons, prov) => cons.ConnectionId == prov.ConnectionId)).Where(t => t.SourceId != t.DestinationId && t.GlobalConnectionId != lastGlobalConnection).ToList();

                    Assert.AreEqual(1, resultTest.Select(t => t.GlobalConnectionId).Distinct().Count());
                    Assert.AreEqual(6, resultTest.Select(t => t.ConnectionId).Distinct().Count());
                    Assert.AreEqual(17, resultTest.Count);

                    //Obtain the connection ID's by just taking one of the connections inside and then obtaining the id
                    var conn1id = result.Single(t => t.SourceId == solar1.Id && t.DestinationId == ironmine1.Id).ConnectionId; //Solars to Mines
                    var conn2id = result.Single(t => t.SourceId == windmoily1.Id && t.DestinationId == factory1.Id).ConnectionId; //Windmoily1 to Factory1
                    var conn3id = result.Single(t => t.SourceId == ironmine1.Id && t.DestinationId == factory1.Id).ConnectionId; //Mine1 to Factory1
                    var conn4id = result.Single(t => t.SourceId == ironmine2.Id && t.DestinationId == factory2.Id).ConnectionId; //Mine2/3 and Windmoily2 to Factory2 + WindMoily provides to mines
                    var conn5id = result.Single(t => t.SourceId == factory1.Id && t.DestinationId == storage1.Id).ConnectionId; //Factory1 to Storage1
                    var conn6id = result.Single(t => t.SourceId == factory2.Id && t.DestinationId == storage2.Id).ConnectionId; //Factory2 to Storage2

                    var oldConnList = new List<int>() { conn1idpre, conn2idpre, conn3idpre, conn4idpre, conn5idpre, conn6idpre };
                    var newConnList = new List<int>() { conn1id, conn2id, conn3id, conn4id, conn5id, conn6id };


                    //Ensure we got all the 6 different connection ID's
                    Assert.AreEqual(6, oldConnList.Distinct().Count());
                    Assert.AreEqual(6, newConnList.Distinct().Count());

                    foreach (var oldConnId in oldConnList)
                    {
                        var conConsumer = database.DbConnection.Select<DbConsumerConnection>(t => t.ConnectionId == oldConnId);
                        var conProvider = database.DbConnection.Select<DbProviderConnection>(t => t.ConnectionId == oldConnId);

                        Assert.IsTrue(conConsumer.Count == 0);
                        Assert.IsTrue(conProvider.Count == 0);

                        Assert.IsFalse(newConnList.Any(t => t == oldConnId));
                    }

                    foreach (var connId in newConnList)
                    {
                        var conConsumer = database.DbConnection.Select<DbConsumerConnection>(t => t.ConnectionId == connId);
                        var conProvider = database.DbConnection.Select<DbProviderConnection>(t => t.ConnectionId == connId);

                        Assert.IsTrue(conConsumer.Count > 0);
                        Assert.IsTrue(conProvider.Count > 0);
                    }



                    //Ensure that all the connections got the right number of ConsumerProviderConnections
                    Assert.AreEqual(6, result.Count(t => t.ConnectionId == conn1id));
                    Assert.AreEqual(1, result.Count(t => t.ConnectionId == conn2id));
                    Assert.AreEqual(1, result.Count(t => t.ConnectionId == conn3id));
                    Assert.AreEqual(7, result.Count(t => t.ConnectionId == conn4id));
                    Assert.AreEqual(1, result.Count(t => t.ConnectionId == conn5id));
                    Assert.AreEqual(1, result.Count(t => t.ConnectionId == conn6id));


                    //Verify if all ConsumerProvidersConnections are as expected
                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn1id && t.SourceId == solar1.Id && t.DestinationId == ironmine1.Id));
                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn1id && t.SourceId == solar1.Id && t.DestinationId == ironmine2.Id));
                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn1id && t.SourceId == solar1.Id && t.DestinationId == ironmine3.Id));
                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn1id && t.SourceId == solar2.Id && t.DestinationId == ironmine1.Id));
                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn1id && t.SourceId == solar2.Id && t.DestinationId == ironmine2.Id));
                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn1id && t.SourceId == solar2.Id && t.DestinationId == ironmine3.Id));

                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn2id && t.SourceId == windmoily1.Id && t.DestinationId == factory1.Id));

                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn3id && t.SourceId == ironmine1.Id && t.DestinationId == factory1.Id));

                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn4id && t.SourceId == ironmine2.Id && t.DestinationId == factory2.Id));
                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn4id && t.SourceId == ironmine3.Id && t.DestinationId == factory2.Id));
                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn4id && t.SourceId == windmoily2.Id && t.DestinationId == factory2.Id));
                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn4id && t.SourceId == windmoily2.Id && t.DestinationId == ironmine2.Id));
                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn4id && t.SourceId == windmoily2.Id && t.DestinationId == ironmine2.Id));
                    //Ironmines are providing to eachother but the calculation part will ensure this still keeps on working because they don't provide resources that they can consume from eachother
                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn4id && t.SourceId == ironmine2.Id && t.DestinationId == ironmine3.Id));
                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn4id && t.SourceId == ironmine3.Id && t.DestinationId == ironmine2.Id));

                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn5id && t.SourceId == factory1.Id && t.DestinationId == storage1.Id));

                    Assert.IsNotNull(result.SingleOrDefault(t => t.ConnectionId == conn6id && t.SourceId == factory2.Id && t.DestinationId == storage2.Id));

                    //Verify if all gameobjects were set to the right GlobalConnectionId
                    Assert.IsTrue(allGameObjects.All(t => t.GlobalConnectionId == result.First().GlobalConnectionId || t.GlobalConnectionId == lastGlobalConnection));
                    Assert.AreEqual(2, allGameObjects.Select(t => t.GlobalConnectionId).Distinct().Count());

                }
            }



            [TestMethod]
            public void WorksOnCombiningConnections()
            {
                var dbFactory = new TransportPipesDbFactory(DbSetupHelper.CreateInMemoryConnectionFactory());
                using (var database = dbFactory.CreateDatabaseWithOpenConnection())
                {
                    database.CreateDbIfNotExists();
                    var defaultUsers = DbSetupHelper.AddDefaultUsers(database.DbConnection);

                    var mapString = "" +
                        "S ++W       " + Environment.NewLine +
                        "+ +         " + Environment.NewLine +
                        "O O         ";
                    DbSetupHelper.AddDbGameObjectsByStringDefinition(database.DbConnection, mapString, defaultUsers[0].Id);


                    //Act on connectionfinder
                    var connectionFinder = new ConnectionFinder(database);

                    connectionFinder.RedetermineAllConnectionsForGameObject(0, 0, new DateTime(2015, 1, 1)); //Start solar

                    connectionFinder.RedetermineAllConnectionsForGameObject(4, 0, new DateTime(2015, 1, 2)); //Start windmoily

                    var allGameObjects = DbSetupHelper.GetAllDbGameObjects(database.DbConnection);

                    Assert.AreEqual(2, allGameObjects.Select(t => t.GlobalConnectionId).Distinct().Count());


                    //Now add pipe in between
                    DbSetupHelper.AddSimpleDbGameObject(database.DbConnection, GameObjectType.GreyPipe, 1, 0, defaultUsers[0].Id);

                    connectionFinder.RedetermineAllConnectionsForGameObject(1, 0, new DateTime(2015, 1, 3)); //Inbetween pipe

                    var allGameObjects2 = DbSetupHelper.GetAllDbGameObjects(database.DbConnection);

                    Assert.AreEqual(1, allGameObjects2.Select(t => t.GlobalConnectionId).Distinct().Count());

                    //The where is also important here else in this case the IronMine's will provide to themselves because they are both provider and consumer
                    //TODO: Move this select statement to the database once I know exactly what we need here
                    var allResults = database.DbConnection.Select<ConsumerProviderConnection>(database.DbConnection.From<DbConsumerConnection>().Join<DbProviderConnection>((cons, prov) => cons.ConnectionId == prov.ConnectionId)).Where(t => t.SourceId != t.DestinationId).ToList();

                    var expectedGlobalConnectionId = allGameObjects2.First().GlobalConnectionId;
                    Assert.AreEqual(0, allResults.Count(t => t.GlobalConnectionId != expectedGlobalConnectionId));

                    var allGlobalCons = database.DbConnection.Select<DbGlobalConnection>();
                    var theOnlyGlobalConnection = allGlobalCons.Single();

                    Assert.AreEqual(new DateTime(2015, 1, 3), theOnlyGlobalConnection.LastCalculated);

                    Assert.AreEqual(0, allGameObjects2.Count(t => t.DbGlobalConnectionId.Value != theOnlyGlobalConnection.Id));
                }
            }

            [TestMethod]
            public void WorksSplittingConnections()
            {
                var dbFactory = new TransportPipesDbFactory(DbSetupHelper.CreateInMemoryConnectionFactory());
                using (var database = dbFactory.CreateDatabaseWithOpenConnection())
                {
                    database.CreateDbIfNotExists();
                    var defaultUsers = DbSetupHelper.AddDefaultUsers(database.DbConnection);

                    var mapString = "" +
                        "S+++W       " + Environment.NewLine +
                        "+ +         " + Environment.NewLine +
                        "O O         ";
                    DbSetupHelper.AddDbGameObjectsByStringDefinition(database.DbConnection, mapString, defaultUsers[0].Id);


                    //Act on connectionfinder
                    var connectionFinder = new ConnectionFinder(database);

                    var startSolar = DbSetupHelper.GetDbGameObject(database.DbConnection, 0, 0);

                    connectionFinder.RedetermineAllConnectionsForGameObject(0, 0, new DateTime(2015, 1, 1)); //Start solar panel

                    var allGameObjects = DbSetupHelper.GetAllDbGameObjects(database.DbConnection);

                    Assert.AreEqual(1, allGameObjects.Select(t => t.GlobalConnectionId).Distinct().Count());

                    //Now remove pipe in between
                    var gameObjectToBeRemoved = database.GetGameObject(1, 0);
                    database.ClearCompleteGlobalConnections(new List<DbGameObject>() { gameObjectToBeRemoved });
                    DbSetupHelper.RemoveDbGameObject(database.DbConnection, 1, 0);

                    connectionFinder.RedetermineAllConnectionsForDeletedGameObject(1, 0, true, new DateTime(2015, 1, 2));

                    var allGameObjects2 = DbSetupHelper.GetAllDbGameObjects(database.DbConnection);

                    Assert.AreEqual(2, allGameObjects2.Select(t => t.GlobalConnectionId).Distinct().Count());


                    //The where is also important here else in this case the IronMine's will provide to themselves because they are both provider and consumer
                    //TODO: Move this select statement to the database once I know exactly what we need here
                    var allResults = database.DbConnection.Select<ConsumerProviderConnection>(database.DbConnection.From<DbConsumerConnection>().Join<DbProviderConnection>((cons, prov) => cons.ConnectionId == prov.ConnectionId)).Where(t => t.SourceId != t.DestinationId).ToList();

                    //Now ensure we only have the expected GlobalConnection Id's
                    var expectedGlobalConnectionIds = allGameObjects2.Select(t => t.GlobalConnectionId).Distinct().OrderBy(t => t).ToList();
                    var existingGlobalConnectionIds = allResults.Select(t => t.GlobalConnectionId).Distinct().OrderBy(t => t).ToList();

                    Assert.IsTrue(expectedGlobalConnectionIds.SequenceEqual(existingGlobalConnectionIds));

                    var allGlobalCons = database.DbConnection.Select<DbGlobalConnection>();
                    Assert.AreEqual(2, allGlobalCons.Count);
                    Assert.AreEqual(2, allGlobalCons.Count(t => t.LastCalculated == new DateTime(2015, 1, 2)));

                    Assert.AreEqual(0, allGameObjects2.Count(t => !allGlobalCons.Any(z => z.Id == t.DbGlobalConnectionId)));
                }
            }
        }
    }
}

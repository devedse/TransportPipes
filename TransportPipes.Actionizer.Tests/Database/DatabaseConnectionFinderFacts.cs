using TransportPipes.Actionizer.Calculators;
using TransportPipes.Common.Enums;
using TransportPipes.Database;
using TransportPipes.Database.Helpers;

namespace TransportPipes.Actionizer.Tests.Database.DatabaseConnectionFinderFacts;

[TestClass]
public class DatabaseTheFindGameObjectsInThisConnectionMethod
{
    [TestMethod]
    public void CorrectlyReturnsGameObjectsFromSimpleConnection()
    {
        var transportFactory = DbSetupHelper.CreateTestDbFactory();

        using (var database = transportFactory.CreateDatabaseWithOpenConnection())
        {

            var defaultUsers = DbSetupHelper.AddDefaultUsers(database.DbContext);

            var mapString = "" +
                "          " + Environment.NewLine +
                "  W       " + Environment.NewLine +
                "  +       " + Environment.NewLine +
                "  +       " + Environment.NewLine +
                "  +++O    " + Environment.NewLine +
                "   +      " + Environment.NewLine +
                "   +      " + Environment.NewLine +
                "   +      " + Environment.NewLine +
                "   O      " + Environment.NewLine +
                "          ";

            DbSetupHelper.AddDbGameObjectsByStringDefinition(database.DbContext, mapString, defaultUsers[0].Id);


            //Act on connectionfinder
            var connectionFinder = new ConnectionFinder(null!);

            var startPipe = DbSetupHelper.GetDbGameObject(database.DbContext, 2, 2);
            var allGameObjects = DbSetupHelper.GetAllDbGameObjects(database.DbContext);

            var realChunkCache = new ChunkCacheHelper(database);
            var allGameObjectsFound = connectionFinder.FindGameObjectsInThisConnection(realChunkCache, startPipe);


            //Assert the result
            Assert.AreEqual(allGameObjects.Count, allGameObjectsFound.Count());
            Assert.AreEqual(1, allGameObjectsFound.Count(t => t.GameObjectType == GameObjectType.WindMoily));
            Assert.AreEqual(2, allGameObjectsFound.Count(t => t.GameObjectType == GameObjectType.Storage));
            Assert.AreEqual(8, allGameObjectsFound.Count(t => t.GameObjectType == GameObjectType.GreyPipe));
        }
    }
}

[TestClass]
public class DatabaseTheObtainListOfConnectionsMethod
{
    [TestMethod]
    public void DetectsMultipleSimpleConnectionsWhenStartingWithPipe()
    {
        var transportFactory = DbSetupHelper.CreateTestDbFactory();

        using (var database = transportFactory.CreateDatabaseWithOpenConnection())
        {
            var defaultUsers = DbSetupHelper.AddDefaultUsers(database.DbContext);

            var mapString = "" +
                "          " + Environment.NewLine +
                "  W+++    " + Environment.NewLine +
                "  +  +++M " + Environment.NewLine +
                "  +    M  " + Environment.NewLine +
                "  +++O    " + Environment.NewLine +
                "   +      " + Environment.NewLine +
                "   +      " + Environment.NewLine +
                "   +      " + Environment.NewLine +
                "   O      " + Environment.NewLine +
                "          ";

            DbSetupHelper.AddDbGameObjectsByStringDefinition(database.DbContext, mapString, defaultUsers[0].Id);


            //Act on connectionfinder
            var connectionFinder = new ConnectionFinder(null!);

            var startPipe = DbSetupHelper.GetDbGameObject(database.DbContext, 2, 2);
            var allGameObjects = DbSetupHelper.GetAllDbGameObjects(database.DbContext);

            var realChunkCache = new ChunkCacheHelper(database);

            var allConnections = connectionFinder.ObtainListOfConnections(realChunkCache, startPipe).OrderBy(t => t.Count).ToList(); //Order by count to ensure the order for testing


            //Assert the result
            Assert.AreEqual(2, allConnections.Count());

            Assert.AreEqual(9, allConnections[0].Count);
            Assert.AreEqual(1, allConnections[0].Count(t => t.GameObjectType == GameObjectType.WindMoily));
            Assert.AreEqual(0, allConnections[0].Count(t => t.GameObjectType == GameObjectType.Storage));
            Assert.AreEqual(2, allConnections[0].Count(t => t.GameObjectType == GameObjectType.IronMine));

            Assert.AreEqual(11, allConnections[1].Count);
            Assert.AreEqual(1, allConnections[1].Count(t => t.GameObjectType == GameObjectType.WindMoily));
            Assert.AreEqual(2, allConnections[1].Count(t => t.GameObjectType == GameObjectType.Storage));
            Assert.AreEqual(0, allConnections[1].Count(t => t.GameObjectType == GameObjectType.IronMine));
        }
    }
}

[TestClass]
public class DatabaseTheDetermineProvidersAndConsumersMethod
{
    [TestMethod]
    public void DeterminesProvConsForWindmillMineStorageConnection()
    {
        var transportFactory = DbSetupHelper.CreateTestDbFactory();

        using (var database = transportFactory.CreateDatabaseWithOpenConnection())
        {
            var defaultUsers = DbSetupHelper.AddDefaultUsers(database.DbContext);

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

            DbSetupHelper.AddDbGameObjectsByStringDefinition(database.DbContext, mapString, defaultUsers[0].Id);


            //Act on connectionfinder
            var connectionFinder = new ConnectionFinder(null!);

            var startPipe = DbSetupHelper.GetDbGameObject(database.DbContext, 2, 2);
            var allGameObjects = DbSetupHelper.GetAllDbGameObjects(database.DbContext);

            var realChunkCache = new ChunkCacheHelper(database);

            var allConnections = connectionFinder.ObtainListOfConnections(realChunkCache, startPipe).OrderBy(t => t.Count).ToList(); //Order by count to ensure the order for testing

            var allProvidersConsumers = connectionFinder.DetermineProvidersAndConsumers(allConnections);

            //Assert the result
            Assert.AreEqual(2, allProvidersConsumers.Count);

            var firstConnection = allProvidersConsumers.Single(t => t.Providers.Any(z => z.GameObjectType == GameObjectType.WindMoily));
            var secondConnection = allProvidersConsumers.Single(t => t.Consumers.Any(z => z.GameObjectType == GameObjectType.Storage));

            Assert.AreEqual(1, firstConnection.Providers.Count);
            Assert.AreEqual(1, firstConnection.Consumers.Count);
            Assert.AreEqual(1, firstConnection.Providers.Count);
            Assert.AreEqual(1, firstConnection.Consumers.Count);

            Assert.AreEqual(GameObjectType.WindMoily, firstConnection.Providers[0].GameObjectType);
            Assert.AreEqual(GameObjectType.IronMine, firstConnection.Consumers[0].GameObjectType);
            Assert.AreEqual(GameObjectType.IronMine, secondConnection.Providers[0].GameObjectType);
            Assert.AreEqual(GameObjectType.Storage, secondConnection.Consumers[0].GameObjectType);
        }
    }
}

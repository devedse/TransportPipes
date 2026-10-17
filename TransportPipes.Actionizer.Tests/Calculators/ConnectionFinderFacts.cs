using TransportPipes.Actionizer.Calculators;
using TransportPipes.Actionizer.Tests.Mocks;
using TransportPipes.Common.Enums;

namespace TransportPipes.Actionizer.Tests.Calculators.ConnectionFinderFacts;

[TestClass]
public class TheFindGameObjectsInThisConnectionMethod
{
    [TestMethod]
    public void FindsAllGameObjectsInASimpleConnection()
    {
        //Arrange chunk cache
        var chunkCache = new ChunkCacheHelperMock();

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

        chunkCache.AddDbGameObjectsByStringDefinition(mapString);


        //Act on connectionfinder
        var connectionFinder = new ConnectionFinder(null!);

        var startPipe = chunkCache.GetDbGameObject(2, 2)!;

        var allGameObjectsFound = connectionFinder.FindGameObjectsInThisConnection(chunkCache, startPipe);


        //Assert the result
        Assert.AreEqual(chunkCache.MockedDbGameObjects.Count, allGameObjectsFound.Count());
    }

    [TestMethod]
    public void DoesNotFindDuplicateGameObjectsWhenTheyCanBeReachedFromTwoSides()
    {
        //Arrange chunk cache
        var chunkCache = new ChunkCacheHelperMock();

        var mapString = "" +
            "          " + Environment.NewLine +
            "  W       " + Environment.NewLine +
            "  +++++   " + Environment.NewLine +
            "  +   +   " + Environment.NewLine +
            "  +++O+   " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   O      " + Environment.NewLine +
            "          ";

        chunkCache.AddDbGameObjectsByStringDefinition(mapString);


        //Act on connectionfinder
        var connectionFinder = new ConnectionFinder(null!);

        var startPipe = chunkCache.GetDbGameObject(2, 2)!;

        var allGameObjectsFound = connectionFinder.FindGameObjectsInThisConnection(chunkCache, startPipe);


        //Assert the result
        Assert.AreEqual(chunkCache.MockedDbGameObjects.Count, allGameObjectsFound.Count());
    }

    [TestMethod]
    public void FindsAllGameObjectsInAMultipleLinesConnection()
    {
        //Arrange chunk cache
        var chunkCache = new ChunkCacheHelperMock();

        var mapString = "" +
            "          " + Environment.NewLine +
            "  W       " + Environment.NewLine +
            "  +++++   " + Environment.NewLine +
            "  + + +   " + Environment.NewLine +
            "  +++++   " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   +O     " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   O      " + Environment.NewLine +
            "          ";

        chunkCache.AddDbGameObjectsByStringDefinition(mapString);


        //Act on connectionfinder
        var connectionFinder = new ConnectionFinder(null!);

        var startPipe = chunkCache.GetDbGameObject(2, 2)!;

        var allGameObjectsFound = connectionFinder.FindGameObjectsInThisConnection(chunkCache, startPipe);


        //Assert the result
        Assert.AreEqual(chunkCache.MockedDbGameObjects.Count, allGameObjectsFound.Count());
    }

    [TestMethod]
    public void FindsAllGameObjectsInAComplexConnection()
    {
        //Arrange chunk cache
        var chunkCache = new ChunkCacheHelperMock();

        var mapString = "" +
            "    WW    " + Environment.NewLine +
            "  W+++O   " + Environment.NewLine +
            "  +++++   " + Environment.NewLine +
            "  + +O+   " + Environment.NewLine +
            "  +++++   " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   +O     " + Environment.NewLine +
            "   +++W   " + Environment.NewLine +
            "   O      " + Environment.NewLine +
            "          ";

        chunkCache.AddDbGameObjectsByStringDefinition(mapString);


        //Act on connectionfinder
        var connectionFinder = new ConnectionFinder(null!);

        var startPipe = chunkCache.GetDbGameObject(2, 2)!;

        var allGameObjectsFound = connectionFinder.FindGameObjectsInThisConnection(chunkCache, startPipe);


        //Assert the result
        Assert.AreEqual(allGameObjectsFound.Count(), chunkCache.MockedDbGameObjects.Count);
    }

    [TestMethod]
    public void DoesNotFindNeighbouringGameObjectsOfAnotherGameObject()
    {
        //Arrange chunk cache
        var chunkCache = new ChunkCacheHelperMock();

        var mapString = "" +
            "          " + Environment.NewLine +
            "  WW      " + Environment.NewLine +
            "  +       " + Environment.NewLine +
            "  +       " + Environment.NewLine +
            "  +++OO   " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   OO     " + Environment.NewLine +
            "          ";

        chunkCache.AddDbGameObjectsByStringDefinition(mapString);


        //Act on connectionfinder
        var connectionFinder = new ConnectionFinder(null!);

        var startPipe = chunkCache.GetDbGameObject(2, 2)!;

        var allGameObjectsFound = connectionFinder.FindGameObjectsInThisConnection(chunkCache, startPipe);


        //Assert the result
        Assert.AreEqual(chunkCache.MockedDbGameObjects.Count - 3, allGameObjectsFound.Count());
    }

    [TestMethod]
    public void DoesNotFindGameObjectsInANeighbouringConnection()
    {
        //Arrange chunk cache
        var chunkCache = new ChunkCacheHelperMock();

        var mapString = "" +
            "          " + Environment.NewLine +
            "  W+++    " + Environment.NewLine +
            "  +  +++M " + Environment.NewLine +
            "  +   M   " + Environment.NewLine +
            "  +++O    " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   O      " + Environment.NewLine +
            "          ";

        chunkCache.AddDbGameObjectsByStringDefinition(mapString);


        //Act on connectionfinder
        var connectionFinder = new ConnectionFinder(null!);

        var startPipe = chunkCache.GetDbGameObject(2, 2)!;

        var allGameObjectsFound = connectionFinder.FindGameObjectsInThisConnection(chunkCache, startPipe);


        //Assert the result
        Assert.AreEqual(11, allGameObjectsFound.Count());
        Assert.AreEqual(0, allGameObjectsFound.Count(t => t.GameObjectType == GameObjectType.IronMine));
        Assert.AreEqual(2, allGameObjectsFound.Count(t => t.GameObjectType == GameObjectType.Storage));
        Assert.AreEqual(1, allGameObjectsFound.Count(t => t.GameObjectType == GameObjectType.WindMoily));
    }

    [TestMethod]
    public void FindsAllGameObjectTypesWhenStartingWithPipe()
    {
        //Arrange chunk cache
        var chunkCache = new ChunkCacheHelperMock();

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

        chunkCache.AddDbGameObjectsByStringDefinition(mapString);


        //Act on connectionfinder
        var connectionFinder = new ConnectionFinder(null!);

        var startPipe = chunkCache.GetDbGameObject(2, 2)!;

        var allGameObjectsFound = connectionFinder.FindGameObjectsInThisConnection(chunkCache, startPipe);


        //Assert the result
        Assert.AreEqual(chunkCache.MockedDbGameObjects.Count, allGameObjectsFound.Count());
        Assert.AreEqual(8, allGameObjectsFound.Count(t => t.GameObjectType == GameObjectType.GreyPipe));
        Assert.AreEqual(1, allGameObjectsFound.Count(t => t.GameObjectType == GameObjectType.WindMoily));
        Assert.AreEqual(2, allGameObjectsFound.Count(t => t.GameObjectType == GameObjectType.Storage));
    }

    [TestMethod]
    public void FindsAllGameObjectTypesWhenStartingWithGameObject()
    {
        //Arrange chunk cache
        var chunkCache = new ChunkCacheHelperMock();

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

        chunkCache.AddDbGameObjectsByStringDefinition(mapString);


        //Act on connectionfinder
        var connectionFinder = new ConnectionFinder(null!);

        var standGameObject = chunkCache.GetDbGameObject(2, 1)!;

        var allGameObjectsFound = connectionFinder.FindGameObjectsInThisConnection(chunkCache, standGameObject);


        //Assert the result
        Assert.AreEqual(chunkCache.MockedDbGameObjects.Count, allGameObjectsFound.Count());
        Assert.AreEqual(8, allGameObjectsFound.Count(t => t.GameObjectType == GameObjectType.GreyPipe));
        Assert.AreEqual(1, allGameObjectsFound.Count(t => t.GameObjectType == GameObjectType.WindMoily));
        Assert.AreEqual(2, allGameObjectsFound.Count(t => t.GameObjectType == GameObjectType.Storage));
    }

    [TestMethod]
    public void FindEverythingWhenJustOneGameObject()
    {
        //Arrange chunk cache
        var chunkCache = new ChunkCacheHelperMock();

        var mapString = "" +
            "          " + Environment.NewLine +
            "  W       " + Environment.NewLine +
            "  +       " + Environment.NewLine +
            "  +       " + Environment.NewLine +
            "  +++     " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "          " + Environment.NewLine +
            "          ";

        chunkCache.AddDbGameObjectsByStringDefinition(mapString);


        //Act on connectionfinder
        var connectionFinder = new ConnectionFinder(null!);

        var standGameObject = chunkCache.GetDbGameObject(2, 1)!;

        var allGameObjectsFound = connectionFinder.FindGameObjectsInThisConnection(chunkCache, standGameObject);


        //Assert the result
        Assert.AreEqual(chunkCache.MockedDbGameObjects.Count, allGameObjectsFound.Count());
        Assert.AreEqual(8, allGameObjectsFound.Count(t => t.GameObjectType == GameObjectType.GreyPipe));
        Assert.AreEqual(1, allGameObjectsFound.Count(t => t.GameObjectType == GameObjectType.WindMoily));
    }

    [TestMethod]
    public void FindsOnlyPipesWhenThereAreJustPipes()
    {
        //Arrange chunk cache
        var chunkCache = new ChunkCacheHelperMock();

        var mapString = "" +
            "          " + Environment.NewLine +
            "  +       " + Environment.NewLine +
            "  +       " + Environment.NewLine +
            "  +       " + Environment.NewLine +
            "  +++     " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "          " + Environment.NewLine +
            "          ";

        chunkCache.AddDbGameObjectsByStringDefinition(mapString);


        //Act on connectionfinder
        var connectionFinder = new ConnectionFinder(null!);

        var startPipe = chunkCache.GetDbGameObject(2, 2)!;

        var allGameObjectsFound = connectionFinder.FindGameObjectsInThisConnection(chunkCache, startPipe);


        //Assert the result
        Assert.AreEqual(chunkCache.MockedDbGameObjects.Count, allGameObjectsFound.Count());
        Assert.AreEqual(9, allGameObjectsFound.Count(t => t.GameObjectType == GameObjectType.GreyPipe));
    }
}

[TestClass]
public class TheObtainListOfConnectionsMethod
{
    [TestMethod]
    public void DetectsMultipleSimpleConnectionsWhenStartingWithPipe()
    {
        //Arrange chunk cache
        var chunkCache = new ChunkCacheHelperMock();

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

        chunkCache.AddDbGameObjectsByStringDefinition(mapString);


        //Act on connectionfinder
        var connectionFinder = new ConnectionFinder(null!);

        var startPipe = chunkCache.GetDbGameObject(2, 2)!;

        var allConnections = connectionFinder.ObtainListOfConnections(chunkCache, startPipe).OrderBy(t => t.Count).ToList(); //Order by count to ensure the order for testing


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

    [TestMethod]
    public void DetectsMultipleSimpleConnectionsWhenStartingWithNormalGameObject()
    {
        //Arrange chunk cache
        var chunkCache = new ChunkCacheHelperMock();

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

        chunkCache.AddDbGameObjectsByStringDefinition(mapString);


        //Act on connectionfinder
        var connectionFinder = new ConnectionFinder(null!);

        var startGameObjectWindMoily = chunkCache.GetDbGameObject(2, 1)!;

        var allConnections = connectionFinder.ObtainListOfConnections(chunkCache, startGameObjectWindMoily).OrderBy(t => t.Count).ToList(); //Order by count to ensure the order for testing


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

    [TestMethod]
    public void DetectsMultipleComplexConnectionsWhenStartingWithPipe()
    {
        //Arrange chunk cache
        var chunkCache = new ChunkCacheHelperMock();

        var mapString = "" +
            "          " + Environment.NewLine +
            "  W+++    " + Environment.NewLine +
            "  +  +++M " + Environment.NewLine +
            "  +    M  " + Environment.NewLine +
            "  +++O++  " + Environment.NewLine +
            "   +   O  " + Environment.NewLine +
            "   + +++  " + Environment.NewLine +
            "   + +    " + Environment.NewLine +
            "   O++    " + Environment.NewLine +
            "          ";

        chunkCache.AddDbGameObjectsByStringDefinition(mapString);


        //Act on connectionfinder
        var connectionFinder = new ConnectionFinder(null!);

        var startPipe = chunkCache.GetDbGameObject(2, 2)!;

        var allConnections = connectionFinder.ObtainListOfConnections(chunkCache, startPipe).OrderBy(t => t.Count).ToList(); //Order by count to ensure the order for testing


        //Assert the result
        Assert.AreEqual(4, allConnections.Count());

        Assert.AreEqual(5, allConnections[0].Count);
        Assert.AreEqual(0, allConnections[0].Count(t => t.GameObjectType == GameObjectType.WindMoily));
        Assert.AreEqual(2, allConnections[0].Count(t => t.GameObjectType == GameObjectType.Storage));
        Assert.AreEqual(1, allConnections[0].Count(t => t.GameObjectType == GameObjectType.IronMine));

        Assert.AreEqual(8, allConnections[1].Count);
        Assert.AreEqual(0, allConnections[1].Count(t => t.GameObjectType == GameObjectType.WindMoily));
        Assert.AreEqual(2, allConnections[1].Count(t => t.GameObjectType == GameObjectType.Storage));
        Assert.AreEqual(0, allConnections[1].Count(t => t.GameObjectType == GameObjectType.IronMine));

        Assert.AreEqual(9, allConnections[2].Count);
        Assert.AreEqual(1, allConnections[2].Count(t => t.GameObjectType == GameObjectType.WindMoily));
        Assert.AreEqual(0, allConnections[2].Count(t => t.GameObjectType == GameObjectType.Storage));
        Assert.AreEqual(2, allConnections[2].Count(t => t.GameObjectType == GameObjectType.IronMine));

        Assert.AreEqual(11, allConnections[3].Count);
        Assert.AreEqual(1, allConnections[3].Count(t => t.GameObjectType == GameObjectType.WindMoily));
        Assert.AreEqual(2, allConnections[3].Count(t => t.GameObjectType == GameObjectType.Storage));
        Assert.AreEqual(0, allConnections[3].Count(t => t.GameObjectType == GameObjectType.IronMine));
    }

    [TestMethod]
    public void DetectsMultipleComplexConnectionsWhenStartingWithNormalGameObject()
    {
        //Arrange chunk cache
        var chunkCache = new ChunkCacheHelperMock();

        var mapString = "" +
            "          " + Environment.NewLine +
            "  W+++    " + Environment.NewLine +
            "  +  +++M " + Environment.NewLine +
            "  +    M  " + Environment.NewLine +
            "  +++O++  " + Environment.NewLine +
            "   +   O  " + Environment.NewLine +
            "   + +++  " + Environment.NewLine +
            "   + +    " + Environment.NewLine +
            "   O++    " + Environment.NewLine +
            "          ";

        chunkCache.AddDbGameObjectsByStringDefinition(mapString);


        //Act on connectionfinder
        var connectionFinder = new ConnectionFinder(null!);

        var startGameObjectWindMoily = chunkCache.GetDbGameObject(2, 1)!;

        var allConnections = connectionFinder.ObtainListOfConnections(chunkCache, startGameObjectWindMoily).OrderBy(t => t.Count).ToList(); //Order by count to ensure the order for testing


        //Assert the result
        Assert.AreEqual(4, allConnections.Count());

        Assert.AreEqual(5, allConnections[0].Count);
        Assert.AreEqual(0, allConnections[0].Count(t => t.GameObjectType == GameObjectType.WindMoily));
        Assert.AreEqual(2, allConnections[0].Count(t => t.GameObjectType == GameObjectType.Storage));
        Assert.AreEqual(1, allConnections[0].Count(t => t.GameObjectType == GameObjectType.IronMine));

        Assert.AreEqual(8, allConnections[1].Count);
        Assert.AreEqual(0, allConnections[1].Count(t => t.GameObjectType == GameObjectType.WindMoily));
        Assert.AreEqual(2, allConnections[1].Count(t => t.GameObjectType == GameObjectType.Storage));
        Assert.AreEqual(0, allConnections[1].Count(t => t.GameObjectType == GameObjectType.IronMine));

        Assert.AreEqual(9, allConnections[2].Count);
        Assert.AreEqual(1, allConnections[2].Count(t => t.GameObjectType == GameObjectType.WindMoily));
        Assert.AreEqual(0, allConnections[2].Count(t => t.GameObjectType == GameObjectType.Storage));
        Assert.AreEqual(2, allConnections[2].Count(t => t.GameObjectType == GameObjectType.IronMine));

        Assert.AreEqual(11, allConnections[3].Count);
        Assert.AreEqual(1, allConnections[3].Count(t => t.GameObjectType == GameObjectType.WindMoily));
        Assert.AreEqual(2, allConnections[3].Count(t => t.GameObjectType == GameObjectType.Storage));
        Assert.AreEqual(0, allConnections[3].Count(t => t.GameObjectType == GameObjectType.IronMine));
    }

    [TestMethod]
    public void DoesNotDetectNotConnectedConnections()
    {
        //Arrange chunk cache
        var chunkCache = new ChunkCacheHelperMock();

        var mapString = "" +
            "          " + Environment.NewLine +
            "  W+++    " + Environment.NewLine +
            "  +  +++M " + Environment.NewLine +
            "  +    M  " + Environment.NewLine +
            "  ++++OS  " + Environment.NewLine +
            "   +   +  " + Environment.NewLine +
            "   +   +  " + Environment.NewLine +
            "   +   +  " + Environment.NewLine +
            "   O   O  " + Environment.NewLine +
            "          ";

        chunkCache.AddDbGameObjectsByStringDefinition(mapString);


        //Act on connectionfinder
        var connectionFinder = new ConnectionFinder(null!);

        var startPipe = chunkCache.GetDbGameObject(2, 2)!;

        var allConnections = connectionFinder.ObtainListOfConnections(chunkCache, startPipe).OrderBy(t => t.Count).ToList(); //Order by count to ensure the order for testing


        //Assert the result
        Assert.AreEqual(2, allConnections.Count());

        Assert.AreEqual(9, allConnections[0].Count);
        Assert.AreEqual(1, allConnections[0].Count(t => t.GameObjectType == GameObjectType.WindMoily));
        Assert.AreEqual(0, allConnections[0].Count(t => t.GameObjectType == GameObjectType.Storage));
        Assert.AreEqual(2, allConnections[0].Count(t => t.GameObjectType == GameObjectType.IronMine));
        Assert.AreEqual(0, allConnections[0].Count(t => t.GameObjectType == GameObjectType.SolarPanel));

        Assert.AreEqual(12, allConnections[1].Count);
        Assert.AreEqual(1, allConnections[1].Count(t => t.GameObjectType == GameObjectType.WindMoily));
        Assert.AreEqual(2, allConnections[1].Count(t => t.GameObjectType == GameObjectType.Storage));
        Assert.AreEqual(0, allConnections[1].Count(t => t.GameObjectType == GameObjectType.IronMine));
        Assert.AreEqual(0, allConnections[1].Count(t => t.GameObjectType == GameObjectType.SolarPanel));
    }


    [TestMethod]
    public void WorksWithSomeRandomPipesOnTheRightWhenStartingWithNormalConnection()
    {
        //Arrange chunk cache
        var chunkCache = new ChunkCacheHelperMock();

        var mapString = "" +
            "          " + Environment.NewLine +
            "  W+++    " + Environment.NewLine +
            "  +  +++  " + Environment.NewLine +
            "  +       " + Environment.NewLine +
            "  ++      " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   O      " + Environment.NewLine +
            "          ";

        chunkCache.AddDbGameObjectsByStringDefinition(mapString);


        //Act on connectionfinder
        var connectionFinder = new ConnectionFinder(null!);

        var startPipe = chunkCache.GetDbGameObject(2, 2)!;

        var allConnections = connectionFinder.ObtainListOfConnections(chunkCache, startPipe).OrderBy(t => t.Count).ToList(); //Order by count to ensure the order for testing


        //Assert the result
        Assert.AreEqual(2, allConnections.Count());

        Assert.AreEqual(7, allConnections[0].Count);
        Assert.AreEqual(1, allConnections[0].Count(t => t.GameObjectType == GameObjectType.WindMoily));
        Assert.AreEqual(6, allConnections[0].Count(t => t.GameObjectType == GameObjectType.GreyPipe));
        Assert.AreEqual(0, allConnections[0].Count(t => t.GameObjectType == GameObjectType.Storage));


        Assert.AreEqual(9, allConnections[1].Count);
        Assert.AreEqual(1, allConnections[1].Count(t => t.GameObjectType == GameObjectType.WindMoily));
        Assert.AreEqual(1, allConnections[1].Count(t => t.GameObjectType == GameObjectType.Storage));
        Assert.AreEqual(7, allConnections[1].Count(t => t.GameObjectType == GameObjectType.GreyPipe));
    }

    [TestMethod]
    public void WorksWithSomeRandomPipesOnTheRightWhenStartingWithThosePipes()
    {
        //Arrange chunk cache
        var chunkCache = new ChunkCacheHelperMock();

        var mapString = "" +
            "          " + Environment.NewLine +
            "  W+++    " + Environment.NewLine +
            "  +  +++  " + Environment.NewLine +
            "  +       " + Environment.NewLine +
            "  ++      " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   O      " + Environment.NewLine +
            "          ";

        chunkCache.AddDbGameObjectsByStringDefinition(mapString);


        //Act on connectionfinder
        var connectionFinder = new ConnectionFinder(null!);

        var startPipe = chunkCache.GetDbGameObject(5, 2)!;

        var allConnections = connectionFinder.ObtainListOfConnections(chunkCache, startPipe).OrderBy(t => t.Count).ToList(); //Order by count to ensure the order for testing


        //Assert the result
        Assert.AreEqual(2, allConnections.Count());

        Assert.AreEqual(7, allConnections[0].Count);
        Assert.AreEqual(1, allConnections[0].Count(t => t.GameObjectType == GameObjectType.WindMoily));
        Assert.AreEqual(6, allConnections[0].Count(t => t.GameObjectType == GameObjectType.GreyPipe));
        Assert.AreEqual(0, allConnections[0].Count(t => t.GameObjectType == GameObjectType.Storage));


        Assert.AreEqual(9, allConnections[1].Count);
        Assert.AreEqual(1, allConnections[1].Count(t => t.GameObjectType == GameObjectType.WindMoily));
        Assert.AreEqual(1, allConnections[1].Count(t => t.GameObjectType == GameObjectType.Storage));
        Assert.AreEqual(7, allConnections[1].Count(t => t.GameObjectType == GameObjectType.GreyPipe));
    }
}

[TestClass]
public class TheDetermineProvidersAndConsumersMethod
{
    [TestMethod]
    public void DeterminesTheRightProvidersAndConsumersInASimpleConnection()
    {
        //Arrange chunk cache
        var chunkCache = new ChunkCacheHelperMock();

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

        chunkCache.AddDbGameObjectsByStringDefinition(mapString);


        //Act on connectionfinder
        var connectionFinder = new ConnectionFinder(null!);

        var startPipe = chunkCache.GetDbGameObject(2, 2)!;

        var allConnections = connectionFinder.ObtainListOfConnections(chunkCache, startPipe).OrderBy(t => t.Count).ToList(); //Order by count to ensure the order for testing

        var allProvidersConsumers = connectionFinder.DetermineProvidersAndConsumers(allConnections);

        //Assert the result
        Assert.AreEqual(1, allProvidersConsumers.Count);

        var firstConnection = allProvidersConsumers[0];

        Assert.AreEqual(1, firstConnection.Providers.Count);
        Assert.AreEqual(2, firstConnection.Consumers.Count);

        Assert.AreEqual(GameObjectType.WindMoily, firstConnection.Providers[0].GameObjectType);
        Assert.AreEqual(GameObjectType.Storage, firstConnection.Consumers[0].GameObjectType);
        Assert.AreEqual(GameObjectType.Storage, firstConnection.Consumers[1].GameObjectType);
    }

    [TestMethod]
    public void DeterminesProvConsForWindmillMineStorageConnection()
    {
        //Arrange chunk cache
        var chunkCache = new ChunkCacheHelperMock();

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

        chunkCache.AddDbGameObjectsByStringDefinition(mapString);


        //Act on connectionfinder
        var connectionFinder = new ConnectionFinder(null!);

        var startPipe = chunkCache.GetDbGameObject(2, 2)!;

        var allConnections = connectionFinder.ObtainListOfConnections(chunkCache, startPipe).OrderBy(t => t.Count).ToList(); //Order by count to ensure the order for testing

        var allProvidersConsumers = connectionFinder.DetermineProvidersAndConsumers(allConnections);

        //Assert the result
        Assert.AreEqual(2, allProvidersConsumers.Count);

        var firstConnection = allProvidersConsumers.Single(t => t.Providers.Any(z => z.GameObjectType == GameObjectType.WindMoily));
        var secondConnection= allProvidersConsumers.Single(t => t.Consumers.Any(z => z.GameObjectType == GameObjectType.Storage));

        Assert.AreEqual(1, firstConnection.Providers.Count);
        Assert.AreEqual(1, firstConnection.Consumers.Count);
        Assert.AreEqual(1, firstConnection.Providers.Count);
        Assert.AreEqual(1, firstConnection.Consumers.Count);

        Assert.AreEqual(GameObjectType.WindMoily, firstConnection.Providers[0].GameObjectType);
        Assert.AreEqual(GameObjectType.IronMine, firstConnection.Consumers[0].GameObjectType);
        Assert.AreEqual(GameObjectType.IronMine, secondConnection.Providers[0].GameObjectType);
        Assert.AreEqual(GameObjectType.Storage, secondConnection.Consumers[0].GameObjectType);
    }

    [TestMethod]
    public void FindsConsumersThatAreAlsoProviders()
    {
        //Arrange chunk cache
        var chunkCache = new ChunkCacheHelperMock();

        var mapString = "" +
            "          " + Environment.NewLine +
            "  W       " + Environment.NewLine +
            "  +       " + Environment.NewLine +
            "  +       " + Environment.NewLine +
            "  +++M    " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   O      " + Environment.NewLine +
            "          ";

        chunkCache.AddDbGameObjectsByStringDefinition(mapString);


        //Act on connectionfinder
        var connectionFinder = new ConnectionFinder(null!);

        var startPipe = chunkCache.GetDbGameObject(2, 2)!;

        var allConnections = connectionFinder.ObtainListOfConnections(chunkCache, startPipe).OrderBy(t => t.Count).ToList(); //Order by count to ensure the order for testing

        var allProvidersConsumers = connectionFinder.DetermineProvidersAndConsumers(allConnections);

        //Assert the result
        Assert.AreEqual(1, allProvidersConsumers.Count);

        var firstConnection = allProvidersConsumers[0];

        Assert.AreEqual(2, firstConnection.Providers.Count);
        Assert.AreEqual(2, firstConnection.Consumers.Count);

        Assert.IsNotNull(firstConnection.Providers.SingleOrDefault(t => t.GameObjectType == GameObjectType.WindMoily));
        Assert.IsNotNull(firstConnection.Providers.SingleOrDefault(t => t.GameObjectType == GameObjectType.IronMine));
        Assert.IsNotNull(firstConnection.Consumers.SingleOrDefault(t => t.GameObjectType == GameObjectType.IronMine));
        Assert.IsNotNull(firstConnection.Consumers.SingleOrDefault(t => t.GameObjectType == GameObjectType.Storage));
    }

    [TestMethod]
    public void ReturnsConnectionWithNonConsumerProviderBuildingsWhenNoConsumers()
    {
        //Arrange chunk cache
        var chunkCache = new ChunkCacheHelperMock();

        var mapString = "" +
            "          " + Environment.NewLine +
            "  W       " + Environment.NewLine +
            "  +       " + Environment.NewLine +
            "  +       " + Environment.NewLine +
            "  +++     " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "          " + Environment.NewLine +
            "          ";

        chunkCache.AddDbGameObjectsByStringDefinition(mapString);


        //Act on connectionfinder
        var connectionFinder = new ConnectionFinder(null!);

        var startPipe = chunkCache.GetDbGameObject(2, 2)!;

        var allConnections = connectionFinder.ObtainListOfConnections(chunkCache, startPipe).OrderBy(t => t.Count).ToList(); //Order by count to ensure the order for testing

        var allProvidersConsumers = connectionFinder.DetermineProvidersAndConsumers(allConnections);

        //Assert the result
        Assert.AreEqual(1, allProvidersConsumers.Count);
        Assert.AreEqual(0, allProvidersConsumers[0].Consumers.Count);
        Assert.AreEqual(0, allProvidersConsumers[0].Providers.Count);
        Assert.AreEqual(8, allProvidersConsumers[0].Pipes.Count);
        Assert.AreEqual(1, allProvidersConsumers[0].NonConsumerProviderBuildings.Count);
    }

    [TestMethod]
    public void ReturnsConnectionWithNonConsumerProviderBuildingsWhenNoValidConsumerProviderCanBeFound()
    {
        //Arrange chunk cache
        var chunkCache = new ChunkCacheHelperMock();

        var mapString = "" +
            "          " + Environment.NewLine +
            "  W       " + Environment.NewLine +
            "  +       " + Environment.NewLine +
            "  +       " + Environment.NewLine +
            "  +++     " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   +      " + Environment.NewLine +
            "   S       " + Environment.NewLine +
            "          ";

        chunkCache.AddDbGameObjectsByStringDefinition(mapString);


        //Act on connectionfinder
        var connectionFinder = new ConnectionFinder(null!);

        var startPipe = chunkCache.GetDbGameObject(2, 2)!;

        var allConnections = connectionFinder.ObtainListOfConnections(chunkCache, startPipe).OrderBy(t => t.Count).ToList(); //Order by count to ensure the order for testing

        var allProvidersConsumers = connectionFinder.DetermineProvidersAndConsumers(allConnections);

        //Assert the result
        Assert.AreEqual(1, allProvidersConsumers.Count);
        Assert.AreEqual(0, allProvidersConsumers[0].Consumers.Count);
        Assert.AreEqual(0, allProvidersConsumers[0].Providers.Count);
        Assert.AreEqual(8, allProvidersConsumers[0].Pipes.Count);
        Assert.AreEqual(2, allProvidersConsumers[0].NonConsumerProviderBuildings.Count);
    }
}

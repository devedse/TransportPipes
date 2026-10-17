using TransportPipes.Common.Enums;
using TransportPipes.Database.Entities;
using TransportPipes.Database.Entities.CombinedPoco;
using TransportPipes.Database.Repositories;

namespace TransportPipes.Actionizer.Tests.Repositories.ProvidersAndConsumersRepositoryFacts;

[TestClass]
public class TheGetConsumersOfThisGameObjectMethod
{
    [TestMethod]
    public void ReturnsTheRightConsumers()
    {
        //Arrange
        var gameObjects = new List<DbGameObject>();
        gameObjects.Add(new DbGameObject(1, 1, GameObjectType.IronMine, new DbUser()) { Id = 1 });
        gameObjects.Add(new DbGameObject(3, 1, GameObjectType.Storage, new DbUser()) { Id = 2 });

        var connections = new List<ConsumerProviderConnection>();
        connections.Add(new ConsumerProviderConnection() { SourceId = 1, DestinationId = 2, ConnectionId = 1, GlobalConnectionId = 1 });

        var pacr = new ProvidersAndConsumersRepository(gameObjects, connections);

        //Act/Assert
        Assert.AreEqual(2, pacr.GetConsumersOfThisGameObject(gameObjects[0]).Single().Id);
        Assert.AreEqual(0, pacr.GetConsumersOfThisGameObject(gameObjects[1]).Count);
    }

    [TestMethod]
    public void ReturnsTheRightConsumersForComplexConnection()
    {
        // S+M++
        // S+M F+O
        //   +++
        //     W

        //Arrange
        var gameObjects = new List<DbGameObject>();
        gameObjects.Add(new DbGameObject(1, 1, GameObjectType.SolarPanel, new DbUser()) { Id = 1 });
        gameObjects.Add(new DbGameObject(1, 2, GameObjectType.SolarPanel, new DbUser()) { Id = 2 });
        gameObjects.Add(new DbGameObject(3, 1, GameObjectType.IronMine, new DbUser()) { Id = 3 });
        gameObjects.Add(new DbGameObject(3, 2, GameObjectType.IronMine, new DbUser()) { Id = 4 });
        gameObjects.Add(new DbGameObject(5, 1, GameObjectType.FactorySmall, new DbUser()) { Id = 5 });
        gameObjects.Add(new DbGameObject(5, 3, GameObjectType.WindMoily, new DbUser()) { Id = 6 });
        gameObjects.Add(new DbGameObject(7, 1, GameObjectType.Storage, new DbUser()) { Id = 7 });

        var connections = new List<ConsumerProviderConnection>();
        connections.Add(new ConsumerProviderConnection() { SourceId = 1, DestinationId = 3, ConnectionId = 1, GlobalConnectionId = 1 });
        connections.Add(new ConsumerProviderConnection() { SourceId = 2, DestinationId = 3, ConnectionId = 1, GlobalConnectionId = 1 });
        connections.Add(new ConsumerProviderConnection() { SourceId = 1, DestinationId = 4, ConnectionId = 1, GlobalConnectionId = 1 });
        connections.Add(new ConsumerProviderConnection() { SourceId = 2, DestinationId = 4, ConnectionId = 1, GlobalConnectionId = 1 });
        connections.Add(new ConsumerProviderConnection() { SourceId = 3, DestinationId = 5, ConnectionId = 2, GlobalConnectionId = 1 });
        connections.Add(new ConsumerProviderConnection() { SourceId = 4, DestinationId = 5, ConnectionId = 3, GlobalConnectionId = 1 });
        connections.Add(new ConsumerProviderConnection() { SourceId = 6, DestinationId = 5, ConnectionId = 3, GlobalConnectionId = 1 });
        connections.Add(new ConsumerProviderConnection() { SourceId = 5, DestinationId = 7, ConnectionId = 4, GlobalConnectionId = 1 });

        var pacr = new ProvidersAndConsumersRepository(gameObjects, connections);

        //Act/Assert
        Assert.AreEqual(1, pacr.GetConsumersOfThisGameObject(gameObjects[0]).Count(t => t.Id == 3)); //Solar 1 provides to IronMine 1
        Assert.AreEqual(1, pacr.GetConsumersOfThisGameObject(gameObjects[0]).Count(t => t.Id == 4)); //Solar 1 provides to IronMine 2
        Assert.AreEqual(1, pacr.GetConsumersOfThisGameObject(gameObjects[1]).Count(t => t.Id == 3)); //Solar 2 provides to IronMine 1
        Assert.AreEqual(1, pacr.GetConsumersOfThisGameObject(gameObjects[1]).Count(t => t.Id == 4)); //Solar 2 provides to IronMine 2
        Assert.AreEqual(1, pacr.GetConsumersOfThisGameObject(gameObjects[2]).Count(t => t.Id == 5)); //IronMine 1 provides to FactorySmall
        Assert.AreEqual(1, pacr.GetConsumersOfThisGameObject(gameObjects[3]).Count(t => t.Id == 5)); //IronMine 1 provides to FactorySmall
        Assert.AreEqual(7, pacr.GetConsumersOfThisGameObject(gameObjects[4]).Single().Id); //FactorySmall provides to storage
        Assert.AreEqual(5, pacr.GetConsumersOfThisGameObject(gameObjects[5]).Single().Id); //WindMoily provides to FactorySmall
        Assert.AreEqual(0, pacr.GetConsumersOfThisGameObject(gameObjects[6]).Count); //Storage provides to nothing
    }
}

[TestClass]
public class TheGetProvidersToThisGameObjectMethod
{
    [TestMethod]
    public void TheReturnsTheRightProviders()
    {
        //Arrange
        var gameObjects = new List<DbGameObject>();
        gameObjects.Add(new DbGameObject(1, 1, GameObjectType.IronMine, new DbUser()) { Id = 1 });
        gameObjects.Add(new DbGameObject(3, 1, GameObjectType.Storage, new DbUser()) { Id = 2 });

        var connections = new List<ConsumerProviderConnection>();
        connections.Add(new ConsumerProviderConnection() { SourceId = 1, DestinationId = 2, ConnectionId = 1, GlobalConnectionId = 1 });

        var pacr = new ProvidersAndConsumersRepository(gameObjects, connections);

        //Act/Assert
        Assert.AreEqual(0, pacr.GetProvidersToThisGameObject(gameObjects[0]).Count);
        Assert.AreEqual(1, pacr.GetProvidersToThisGameObject(gameObjects[1]).Single().Id);
    }

    [TestMethod]
    public void ReturnsTheRightProvidersForComplexConnection()
    {
        // S+M++
        // S+M F+O
        //   +++
        //     W

        //Arrange
        var gameObjects = new List<DbGameObject>();
        gameObjects.Add(new DbGameObject(1, 1, GameObjectType.SolarPanel, new DbUser()) { Id = 1 });
        gameObjects.Add(new DbGameObject(1, 2, GameObjectType.SolarPanel, new DbUser()) { Id = 2 });
        gameObjects.Add(new DbGameObject(3, 1, GameObjectType.IronMine, new DbUser()) { Id = 3 });
        gameObjects.Add(new DbGameObject(3, 2, GameObjectType.IronMine, new DbUser()) { Id = 4 });
        gameObjects.Add(new DbGameObject(5, 1, GameObjectType.FactorySmall, new DbUser()) { Id = 5 });
        gameObjects.Add(new DbGameObject(5, 3, GameObjectType.WindMoily, new DbUser()) { Id = 6 });
        gameObjects.Add(new DbGameObject(7, 1, GameObjectType.Storage, new DbUser()) { Id = 7 });

        var connections = new List<ConsumerProviderConnection>();
        connections.Add(new ConsumerProviderConnection() { SourceId = 1, DestinationId = 3, ConnectionId = 1, GlobalConnectionId = 1 });
        connections.Add(new ConsumerProviderConnection() { SourceId = 2, DestinationId = 3, ConnectionId = 1, GlobalConnectionId = 1 });
        connections.Add(new ConsumerProviderConnection() { SourceId = 1, DestinationId = 4, ConnectionId = 1, GlobalConnectionId = 1 });
        connections.Add(new ConsumerProviderConnection() { SourceId = 2, DestinationId = 4, ConnectionId = 1, GlobalConnectionId = 1 });
        connections.Add(new ConsumerProviderConnection() { SourceId = 3, DestinationId = 5, ConnectionId = 2, GlobalConnectionId = 1 });
        connections.Add(new ConsumerProviderConnection() { SourceId = 4, DestinationId = 5, ConnectionId = 3, GlobalConnectionId = 1 });
        connections.Add(new ConsumerProviderConnection() { SourceId = 6, DestinationId = 5, ConnectionId = 3, GlobalConnectionId = 1 });
        connections.Add(new ConsumerProviderConnection() { SourceId = 5, DestinationId = 7, ConnectionId = 4, GlobalConnectionId = 1 });

        var pacr = new ProvidersAndConsumersRepository(gameObjects, connections);

        //Act/Assert
        Assert.AreEqual(0, pacr.GetProvidersToThisGameObject(gameObjects[0]).Count); //Solar 1 has no providers
        Assert.AreEqual(0, pacr.GetProvidersToThisGameObject(gameObjects[1]).Count); //Solar 2 has no providers
        Assert.AreEqual(1, pacr.GetProvidersToThisGameObject(gameObjects[2]).Count(t => t.Id == 1)); //Solar 1 provides to IronMine 1
        Assert.AreEqual(1, pacr.GetProvidersToThisGameObject(gameObjects[2]).Count(t => t.Id == 2)); //Solar 2 provides to IronMine 1
        Assert.AreEqual(1, pacr.GetProvidersToThisGameObject(gameObjects[3]).Count(t => t.Id == 1)); //Solar 1 provides to IronMine 2
        Assert.AreEqual(1, pacr.GetProvidersToThisGameObject(gameObjects[3]).Count(t => t.Id == 2)); //Solar 2 provides to IronMine 2
        Assert.AreEqual(1, pacr.GetProvidersToThisGameObject(gameObjects[4]).Count(t => t.Id == 3)); //IronMine 1 provides to FactorySmall
        Assert.AreEqual(1, pacr.GetProvidersToThisGameObject(gameObjects[4]).Count(t => t.Id == 4)); //IronMine 2 provides to FactorySmall
        Assert.AreEqual(1, pacr.GetProvidersToThisGameObject(gameObjects[4]).Count(t => t.Id == 6)); //WindMoily provides to FactorySmall
        Assert.AreEqual(0, pacr.GetProvidersToThisGameObject(gameObjects[5]).Count); //WindMoily has no providers
        Assert.AreEqual(5, pacr.GetProvidersToThisGameObject(gameObjects[6]).Single().Id); //FactorySmall provides to storage
    }
}

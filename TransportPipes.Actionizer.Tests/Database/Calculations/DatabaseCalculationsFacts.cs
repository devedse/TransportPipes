using TransportPipes.Actionizer.Tests.Database.Helpers;
using TransportPipes.Calculators.Calculators;
using TransportPipes.Common.Enums;
using TransportPipes.Common.GameLogicObjects.GameObjectInfo;
using TransportPipes.Common.Repositories;
using TransportPipes.Database.Entities;
using TransportPipes.Database.Repositories;
using TransportPipes.TestsCommon.Helpers;

namespace TransportPipes.Actionizer.Tests.Database.Calculations.DatabaseCalculationsFacts;

[TestClass]
public class DatabaseTheCalculationMethods
{
    [TestMethod]
    public void StorageContainsEnergyOfWindMoilyAfterCalculation()
    {
        //Arrange
        using (var dbTestCalculationHelper = new DbTestCalculationHelper())
        {

            dbTestCalculationHelper.AddMapStringForUser(1, "W+O");

            dbTestCalculationHelper.DetermineAllConnections();

            //Act
            var calculationPackage = dbTestCalculationHelper.CreateCalculationPackages().Single();
            var totalCalculation = dbTestCalculationHelper.CreateTotalCalculations(new List<CalculationPackage>() { calculationPackage }).Single();
            var allGameObjects = calculationPackage.ProvidersAndConsumersRepository.ConnectedGameObjects;

            //Assert
            var storage = allGameObjects.Single(t => t.X == 2 && t.Y == 0);

            var energyGeneratedByWindmill = GameResourcesRepository.DetermineResourceOfTypeProduced(GameObjectType.WindMoily, BackendResourceType.Energy);

            Assert.IsTrue(TestHelper.AlmostEqual(energyGeneratedByWindmill, totalCalculation
                .GetCalculationForGameObject(storage)
                .Provisions.SelectMany(t => t.ResourcesProvided)
                .Single().Value));

            Assert.IsTrue(TestHelper.AlmostEqual(energyGeneratedByWindmill, totalCalculation
                .GetCalculationForGameObject(storage)
                .Storage.Single().Value));
        }
    }

    [TestMethod]
    public void WindmillMineStorageTestWhereItOnlyProvides5Energy()
    {
        //Arrange
        using (var dbTestCalculationHelper = new DbTestCalculationHelper())
        {

            dbTestCalculationHelper.AddMapStringForUser(1, "" +
                " W+M+O ");

            dbTestCalculationHelper.DetermineAllConnections();

            //Act
            var calculationPackage = dbTestCalculationHelper.CreateCalculationPackages().Single();
            var totalCalculation = dbTestCalculationHelper.CreateTotalCalculations(new List<CalculationPackage>() { calculationPackage }).Single();
            var allGameObjects = calculationPackage.ProvidersAndConsumersRepository.ConnectedGameObjects;

            //Assert
            var storage = allGameObjects.Single(t => t.GameObjectType == GameObjectType.Storage);
            var ironmine = allGameObjects.Single(t => t.GameObjectType == GameObjectType.IronMine);

            var ironoreGeneratedByIronMine = GameResourcesRepository.DetermineResourceOfTypeProduced(GameObjectType.IronMine, BackendResourceType.IronOre);
            var energyConsumedByIronMine = GameResourcesRepository.DetermineResourceOfTypeConsumed(GameObjectType.IronMine, BackendResourceType.Energy);

            Assert.IsTrue(TestHelper.AlmostEqual(ironoreGeneratedByIronMine, totalCalculation
                .GetCalculationForGameObject(storage)
                .Provisions.SelectMany(t => t.ResourcesProvided)
                .Single().Value));

            Assert.IsTrue(TestHelper.AlmostEqual(ironoreGeneratedByIronMine, totalCalculation
                .GetCalculationForGameObject(storage)
                .Storage.Single().Value));

            Assert.IsTrue(TestHelper.AlmostEqual(energyConsumedByIronMine, totalCalculation
                .GetCalculationForGameObject(ironmine)
                .Provisions.SelectMany(t => t.ResourcesProvided)
                .Single().Value));
        }
    }

    [TestMethod]
    public void ReturnsRightNumbersWhenCalculatingSimpleConnection()
    {
        //Arrange
        using (var dbTestCalculationHelper = new DbTestCalculationHelper())
        {

            dbTestCalculationHelper.AddMapStringForUser(1, "" +
                "          " + Environment.NewLine +
                "  W       " + Environment.NewLine +
                "  +       " + Environment.NewLine +
                "  +       " + Environment.NewLine +
                "  +++O    " + Environment.NewLine +
                "   +      " + Environment.NewLine +
                "   +      " + Environment.NewLine +
                "   +      " + Environment.NewLine +
                "   O      " + Environment.NewLine +
                "          ");

            dbTestCalculationHelper.DetermineAllConnections();

            //Act
            var calculationPackage = dbTestCalculationHelper.CreateCalculationPackages().Single();
            var totalCalculation = dbTestCalculationHelper.CreateTotalCalculations(new List<CalculationPackage>() { calculationPackage }).Single();
            var allGameObjects = calculationPackage.ProvidersAndConsumersRepository.ConnectedGameObjects;

            //Assert
            var windMoily = allGameObjects.Single(t => t.GameObjectType == GameObjectType.WindMoily);
            var storage1 = allGameObjects.Single(t => t.X == 5 && t.Y == 4);
            var storage2 = allGameObjects.Single(t => t.X == 3 && t.Y == 8);

            var windMoilyCalcs = totalCalculation.GetCalculationForGameObject(windMoily);
            var storage1Calcs = totalCalculation.GetCalculationForGameObject(storage1);
            var storage2Calcs = totalCalculation.GetCalculationForGameObject(storage2);

            var windmillGeneratesEnergy = GameResourcesRepository.DetermineResourceOfTypeProduced(GameObjectType.WindMoily, BackendResourceType.Energy);

            Assert.IsTrue(storage1Calcs.Provisions.Single().ResourcesProvided.Single().ResourceType == BackendResourceType.Energy);
            Assert.IsTrue(TestHelper.AlmostEqual(storage1Calcs.Provisions.Single().ResourcesProvided.Single().Value, windmillGeneratesEnergy / 2));

            Assert.IsTrue(storage2Calcs.Provisions.Single().ResourcesProvided.Single().ResourceType == BackendResourceType.Energy);
            Assert.IsTrue(TestHelper.AlmostEqual(storage2Calcs.Provisions.Single().ResourcesProvided.Single().Value, windmillGeneratesEnergy / 2));
        }
    }

    [TestMethod]
    public void WindMillOnlyProvides5EnergyWhen5IsRequestedBy2WindMills()
    {
        //Arrange
        using (var dbTestCalculationHelper = new DbTestCalculationHelper())
        {

            dbTestCalculationHelper.AddMapStringForUser(1, "" +
               "            " + Environment.NewLine +
               " W+++       " + Environment.NewLine +
               "    M+++O   " + Environment.NewLine +
               " W+++       " + Environment.NewLine +
               "            ");

            dbTestCalculationHelper.DetermineAllConnections();

            //Act
            var calculationPackage = dbTestCalculationHelper.CreateCalculationPackages().Single();
            var totalCalculation = dbTestCalculationHelper.CreateTotalCalculations(new List<CalculationPackage>() { calculationPackage }).Single();
            var allGameObjects = calculationPackage.ProvidersAndConsumersRepository.ConnectedGameObjects;

            //Assert
            var storage = allGameObjects.Single(t => t.GameObjectType == GameObjectType.Storage);
            var ironmine = allGameObjects.Single(t => t.GameObjectType == GameObjectType.IronMine);

            var ironoreGeneratedByIronMine = GameResourcesRepository.DetermineResourceOfTypeProduced(GameObjectType.IronMine, BackendResourceType.IronOre);
            var energyConsumedByIronMine = GameResourcesRepository.DetermineResourceOfTypeConsumed(GameObjectType.IronMine, BackendResourceType.Energy);

            Assert.IsTrue(TestHelper.AlmostEqual(ironoreGeneratedByIronMine, totalCalculation
                .GetCalculationForGameObject(storage)
                .Provisions.SelectMany(t => t.ResourcesProvided)
                .Single().Value));

            Assert.IsTrue(TestHelper.AlmostEqual(ironoreGeneratedByIronMine, totalCalculation
                .GetCalculationForGameObject(storage)
                .Storage.Single().Value));

            var providedToIronMine = totalCalculation.GetCalculationForGameObject(ironmine).Provisions.SelectMany(t => t.ResourcesProvided).ToList();
            foreach (var resourceProvided in providedToIronMine)
            {
                Assert.IsTrue(TestHelper.AlmostEqual(energyConsumedByIronMine / 2, resourceProvided.Value));
            }
        }
    }

    [TestMethod]
    public void TransfersMoneyBetweenTwoPlayersInSimpleConnection()
    {
        //Arrange
        using (var dbTestCalculationHelper = new DbTestCalculationHelper())
        {

            dbTestCalculationHelper.AddMapStringForUser(1, "" +
                " +O");

            dbTestCalculationHelper.AddMapStringForUser(2, "" +
                "W");

            dbTestCalculationHelper.DetermineAllConnections();

            var moneyForPlayers = dbTestCalculationHelper.CreateMoneyForUsers();

            var energyGeneratedByWindmill = GameResourcesRepository.DetermineResourceOfTypeProduced(GameObjectType.WindMoily, BackendResourceType.Energy);

            Assert.AreEqual(2, moneyForPlayers.Count);
            Assert.IsTrue(TestHelper.AlmostEqual(energyGeneratedByWindmill * -2, moneyForPlayers[dbTestCalculationHelper.GetUserById(1)]));
            Assert.IsTrue(TestHelper.AlmostEqual(energyGeneratedByWindmill * 2, moneyForPlayers[dbTestCalculationHelper.GetUserById(2)]));
        }
    }

    [TestMethod]
    public void DoesNotReturnNanAsMoneyInThisConnection()
    {
        //Arrange
        using (var dbTestCalculationHelper = new DbTestCalculationHelper())
        {

            dbTestCalculationHelper.AddMapStringForUser(1, "" +
                " +M+F       " + Environment.NewLine +
                "    +       " + Environment.NewLine +
                "    M       " + Environment.NewLine +
                "    +       " + Environment.NewLine +
                "    W+M+F+T " + Environment.NewLine +
                "            ");

            dbTestCalculationHelper.AddMapStringForUser(2, "" +
                "W");

            dbTestCalculationHelper.DetermineAllConnections();

            var moneyForPlayers = dbTestCalculationHelper.CreateMoneyForUsers();

            Assert.AreEqual(2, moneyForPlayers.Count);
            Assert.IsFalse(double.IsNaN(moneyForPlayers[dbTestCalculationHelper.GetUserById(1)]));
            Assert.IsFalse(double.IsNaN(moneyForPlayers[dbTestCalculationHelper.GetUserById(2)]));
        }
    }

    [TestMethod]
    public void CheckIfForADistributionTheSameValuesAppearWhenWeStartCalculatingFromAnotherObject()
    {
        //Arrange
        using (var dbTestCalculationHelper = new DbTestCalculationHelper())
        {

            dbTestCalculationHelper.AddMapStringForUser(1, "" +
                "            " + Environment.NewLine +
                "   O+W      " + Environment.NewLine +
                "   +        " + Environment.NewLine +
                " O+++W      " + Environment.NewLine +
                "            ");

            dbTestCalculationHelper.DetermineAllConnections();

            var calculationPackage = dbTestCalculationHelper.CreateCalculationPackages().Single();
            var totalCalculation = dbTestCalculationHelper.CreateTotalCalculations(new List<CalculationPackage>() { calculationPackage }).Single();

            var allGameObjects = calculationPackage.ProvidersAndConsumersRepository.ConnectedGameObjects;

            var storageBottomLeft = allGameObjects.Single(t => t.X == 1 && t.Y == 3);
            var storageTopLeft = allGameObjects.Single(t => t.X == 3 && t.Y == 1);

            var windMillTopRight = allGameObjects.Single(t => t.X == 5 && t.Y == 1);
            var windMillBottomRight = allGameObjects.Single(t => t.X == 5 && t.Y == 3);

            var energyGeneratedByWindmill = GameResourcesRepository.DetermineResourceOfTypeProduced(GameObjectType.WindMoily, BackendResourceType.Energy);

            //Provisions
            Assert.IsTrue(TestHelper.AlmostEqual(energyGeneratedByWindmill / 2, totalCalculation
                .GetCalculationForGameObject(storageBottomLeft)
                .Provisions
                .SelectMany(t => t.ResourcesProvided)
                .Where(t => t.ResourceType == BackendResourceType.Energy)
                .Sum(t => t.Value)));
            Assert.IsTrue(TestHelper.AlmostEqual(energyGeneratedByWindmill + (energyGeneratedByWindmill / 2), totalCalculation
                .GetCalculationForGameObject(storageTopLeft)
                .Provisions
                .SelectMany(t => t.ResourcesProvided)
                .Where(t => t.ResourceType == BackendResourceType.Energy)
                .Sum(t => t.Value)));

            //Requests (It should requests the same as they produce in this case)
            Assert.IsTrue(TestHelper.AlmostEqual(energyGeneratedByWindmill, totalCalculation
                .GetCalculationForGameObject(windMillTopRight)
                .Requests
                .SelectMany(t => t.ResourcesRequested)
                .Where(t => t.ResourceType == BackendResourceType.Energy)
                .Sum(t => t.Value)));
            Assert.IsTrue(TestHelper.AlmostEqual(energyGeneratedByWindmill * 2, totalCalculation
                .GetCalculationForGameObject(windMillBottomRight)
                .Requests
                .SelectMany(t => t.ResourcesRequested)
                .Where(t => t.ResourceType == BackendResourceType.Energy)
                .Sum(t => t.Value)));
        }
    }

    [TestMethod]
    public void SolarAndWindMillDistribution()
    {
        //Arrange
        using (var dbTestCalculationHelper = new DbTestCalculationHelper())
        {

            dbTestCalculationHelper.AddMapStringForUser(1, "" +
                "             " + Environment.NewLine +
                " W++ ++M++   " + Environment.NewLine +
                "   + +   +   " + Environment.NewLine +
                "   ++++M+++O " + Environment.NewLine +
                "   + +   +   " + Environment.NewLine +
                " S++ ++M++   " + Environment.NewLine +
                "             ");

            dbTestCalculationHelper.DetermineAllConnections();

            var calculationPackage = dbTestCalculationHelper.CreateCalculationPackages().Single();
            var totalCalculation = dbTestCalculationHelper.CreateTotalCalculations(new List<CalculationPackage>() { calculationPackage }).Single();
            var allGameObjects = calculationPackage.ProvidersAndConsumersRepository.ConnectedGameObjects;

            var storage = allGameObjects.Single(t => t.X == 11 && t.Y == 3);

            var energyGeneratedByWindmill = GameResourcesRepository.DetermineResourceOfTypeProduced(GameObjectType.WindMoily, BackendResourceType.Energy);
            var energyGeneratedBySolarPanel = GameResourcesRepository.DetermineResourceOfTypeProduced(GameObjectType.SolarPanel, BackendResourceType.Energy);

            Assert.IsTrue(TestHelper.AlmostEqual(energyGeneratedByWindmill + energyGeneratedBySolarPanel, totalCalculation
                .GetCalculationForGameObject(storage)
                .Provisions.SelectMany(t => t.ResourcesProvided)
                .Where(t => t.ResourceType == BackendResourceType.IronOre)
                .Sum(t => t.Value)));

            var windmill = allGameObjects.Single(t => t.GameObjectType == GameObjectType.WindMoily);
            var solarpanel = allGameObjects.Single(t => t.GameObjectType == GameObjectType.SolarPanel);

            var requestsWindmill = totalCalculation.GetCalculationForGameObject(windmill).Requests.SelectMany(t => t.ResourcesRequested).Where(t => t.ResourceType == BackendResourceType.Energy);
            var requestsSolarPanel = totalCalculation.GetCalculationForGameObject(solarpanel).Requests.SelectMany(t => t.ResourcesRequested).Where(t => t.ResourceType == BackendResourceType.Energy);


            //Requests are exactly the same as production again here
            Assert.AreEqual(3, requestsWindmill.Count(t => TestHelper.AlmostEqual(t.Value, energyGeneratedByWindmill / 3)));
            Assert.AreEqual(3, requestsSolarPanel.Count(t => TestHelper.AlmostEqual(t.Value, energyGeneratedBySolarPanel / 3)));

            foreach (var mine in allGameObjects.Where(t => t.GameObjectType == GameObjectType.IronMine))
            {
                var provisionsToMine = totalCalculation.GetCalculationForGameObject(mine).Provisions.SelectMany(t => t.ResourcesProvided).Where(t => t.ResourceType == BackendResourceType.Energy);
                Assert.AreEqual(1, provisionsToMine.Count(t => TestHelper.AlmostEqual(t.Value, energyGeneratedByWindmill / 3)));
                Assert.AreEqual(1, provisionsToMine.Count(t => TestHelper.AlmostEqual(t.Value, energyGeneratedBySolarPanel / 3)));
            }
        }
    }

    [TestMethod]
    public void FactoryNotBeingAbleToWorkAtFullSpeed()
    {
        //Arrange
        using (var dbTestCalculationHelper = new DbTestCalculationHelper())
        {

            dbTestCalculationHelper.AddMapStringForUser(1, "" +
                "        " + Environment.NewLine +
                " W+F++O " + Environment.NewLine +
                " + +    " + Environment.NewLine +
                " M++    " + Environment.NewLine +
                "        ");

            dbTestCalculationHelper.DetermineAllConnections();

            //Act
            var calculationPackage = dbTestCalculationHelper.CreateCalculationPackages().Single();
            var totalCalculation = dbTestCalculationHelper.CreateTotalCalculations(new List<CalculationPackage>() { calculationPackage }).Single();
            var allGameObjects = calculationPackage.ProvidersAndConsumersRepository.ConnectedGameObjects;

            //Assert
            var ironProducedByFactory = GameResourcesRepository.DetermineResourceOfTypeProduced(GameObjectType.FactorySmall, BackendResourceType.Iron);

            var energyConsumedByMine = GameResourcesRepository.DetermineResourceOfTypeConsumed(GameObjectType.IronMine, BackendResourceType.Energy);
            var energyConsumedByFactory = GameResourcesRepository.DetermineResourceOfTypeConsumed(GameObjectType.FactorySmall, BackendResourceType.Energy);
            var totalEnergyConsumed = energyConsumedByFactory + energyConsumedByMine;

            var energyProducedByWindmill = GameResourcesRepository.DetermineResourceOfTypeProduced(GameObjectType.WindMoily, BackendResourceType.Energy);

            var percentageOfEnergyIsAvailable = energyProducedByWindmill / totalEnergyConsumed;

            var storageCalculation = totalCalculation.GetCalculationForGameObject(allGameObjects.Single(t => t.GameObjectType == GameObjectType.Storage));
            //Test total resources
            Assert.AreEqual(BackendResourceType.Iron, storageCalculation.Storage.Single().ResourceType);
            Assert.IsTrue(TestHelper.AlmostEqual(ironProducedByFactory * percentageOfEnergyIsAvailable, storageCalculation.Storage.Single().Value));

            //Test if provisioned also works (Should return the same value as above)
            var provisionToStorage = storageCalculation.Provisions.Single().ResourcesProvided.Single();
            Assert.AreEqual(1, storageCalculation.Provisions.Count);
            Assert.AreEqual(BackendResourceType.Iron, provisionToStorage.ResourceType);
            Assert.IsTrue(TestHelper.AlmostEqual(ironProducedByFactory * percentageOfEnergyIsAvailable, provisionToStorage.Value));

            var energyRequestedByFactory = GameResourcesRepository.DetermineResourceOfTypeConsumed(GameObjectType.FactorySmall, BackendResourceType.Energy);
            var ironoreRequestedByFactory = GameResourcesRepository.DetermineResourceOfTypeConsumed(GameObjectType.FactorySmall, BackendResourceType.IronOre);

            //Test if the Factory has also received the right resources
            var provisionsToFactory = totalCalculation.GetCalculationForGameObject(allGameObjects.Single(t => t.GameObjectType == GameObjectType.FactorySmall)).Provisions.SelectMany(t => t.ResourcesProvided).ToList();
            Assert.AreEqual(2, provisionsToFactory.Count);
            Assert.AreEqual(1, provisionsToFactory.Count(t => t.ResourceType == BackendResourceType.Energy && TestHelper.AlmostEqual(t.Value, energyRequestedByFactory * percentageOfEnergyIsAvailable)));
            Assert.AreEqual(1, provisionsToFactory.Count(t => t.ResourceType == BackendResourceType.IronOre && TestHelper.AlmostEqual(t.Value, ironoreRequestedByFactory * percentageOfEnergyIsAvailable)));
        }
    }

    [TestMethod]
    public void SolarPanelWith2MinesAndStorage()
    {
        //Arrange
        using (var dbTestCalculationHelper = new DbTestCalculationHelper())
        {

            dbTestCalculationHelper.AddMapStringForUser(1, "" +
                "           " + Environment.NewLine +
                " S+++M+++O " + Environment.NewLine +
                "   +   +   " + Environment.NewLine +
                "   ++M++   " + Environment.NewLine +
                "           ");

            dbTestCalculationHelper.DetermineAllConnections();

            //Act
            var calculationPackage = dbTestCalculationHelper.CreateCalculationPackages().Single();
            var totalCalculation = dbTestCalculationHelper.CreateTotalCalculations(new List<CalculationPackage>() { calculationPackage }).Single();
            var allGameObjects = calculationPackage.ProvidersAndConsumersRepository.ConnectedGameObjects;

            //Assert
            var energyProducedBySolarPanel = GameResourcesRepository.DetermineResourceOfTypeProduced(GameObjectType.SolarPanel, BackendResourceType.Energy);

            var allMines = allGameObjects.Where(t => t.GameObjectType == GameObjectType.IronMine).ToList();

            Assert.AreEqual(2, allMines.Count);

            foreach (var mine in allMines)
            {
                var mineCalculation = totalCalculation.GetCalculationForGameObject(mine);
                var singleProvision = mineCalculation.Provisions.Single().ResourcesProvided.Single();
                Assert.AreEqual(BackendResourceType.Energy, singleProvision.ResourceType);
                Assert.IsTrue(TestHelper.AlmostEqual(energyProducedBySolarPanel / 2, singleProvision.Value));
            }

            var storage = allGameObjects.Single(t => t.GameObjectType == GameObjectType.Storage);
            var storageCalculation = totalCalculation.GetCalculationForGameObject(storage);
            var storageProvisions = storageCalculation.Provisions.SelectMany(t => t.ResourcesProvided).ToList();

            var ironoreProducedByIronMine = GameResourcesRepository.DetermineResourceOfTypeProduced(GameObjectType.IronMine, BackendResourceType.IronOre);

            Assert.AreEqual(2, storageProvisions.Count);

            foreach (var prov in storageProvisions)
            {
                Assert.AreEqual(BackendResourceType.IronOre, prov.ResourceType);
                Assert.IsTrue(TestHelper.AlmostEqual(ironoreProducedByIronMine / 2, prov.Value));
            }
        }
    }

    [TestMethod]
    public void DoesCalculationsWhenEverythingIsConnectedInterjoined()
    {
        //Arrange
        using (var dbTestCalculationHelper = new DbTestCalculationHelper())
        {

            dbTestCalculationHelper.AddMapStringForUser(1, "" +
                "   W     " + Environment.NewLine +
                "   +     " + Environment.NewLine +
                " O+++M   " + Environment.NewLine +
                "   +     " + Environment.NewLine +
                "   F     " + Environment.NewLine +
                "         ");

            dbTestCalculationHelper.DetermineAllConnections();

            //Act
            var calculationPackage = dbTestCalculationHelper.CreateCalculationPackages().Single();
            var totalCalculation = dbTestCalculationHelper.CreateTotalCalculations(new List<CalculationPackage>() { calculationPackage }).Single();
            var allGameObjects = calculationPackage.ProvidersAndConsumersRepository.ConnectedGameObjects;

            //Assert
            var storage = allGameObjects.Single(t => t.GameObjectType == GameObjectType.Storage);

            Assert.AreEqual(0, totalCalculation.GetCalculationForGameObject(storage).Requests.SelectMany(t => t.ResourcesRequested).Count());
            Assert.AreEqual(1, totalCalculation.GetCalculationForGameObject(storage).Provisions.SelectMany(t => t.ResourcesProvided).Count());
        }
    }

    [TestMethod]
    public void DoesCallsWhenTwoNonConnectingThingsAreStillConnected()
    {
        //Arrange
        using (var dbTestCalculationHelper = new DbTestCalculationHelper())
        {
            dbTestCalculationHelper.AddMapStringForUser(1, "" +
                " W W     " + Environment.NewLine +
                " + +     " + Environment.NewLine +
                " M+M     " + Environment.NewLine +
                " + +     " + Environment.NewLine +
                " T T     " + Environment.NewLine +
                "         ");

            dbTestCalculationHelper.DetermineAllConnections();


            //Act
            var calculationPackage = dbTestCalculationHelper.CreateCalculationPackages().Single();
            var totalCalculation = dbTestCalculationHelper.CreateTotalCalculations(new List<CalculationPackage>() { calculationPackage }).Single();
            var allGameObjects = calculationPackage.ProvidersAndConsumersRepository.ConnectedGameObjects;

            //Assert
            var tradingBuilding1 = allGameObjects.Single(t => t.X == 1 && t.Y == 4);
            var tradingBuilding2 = allGameObjects.Single(t => t.X == 3 && t.Y == 4);

            var calcTradingBuilding1 = totalCalculation.GetCalculationForGameObject(tradingBuilding1);
            var calcTradingBuilding2 = totalCalculation.GetCalculationForGameObject(tradingBuilding2);


            Assert.AreEqual(1, calcTradingBuilding1.Provisions.Count);
            Assert.AreEqual(1, calcTradingBuilding2.Provisions.Count);

            var ironOreProvidedByMine = GameResourcesRepository.DetermineResourceOfTypeProduced(GameObjectType.IronMine, BackendResourceType.IronOre);

            Assert.AreEqual(ironOreProvidedByMine, calcTradingBuilding1.Provisions.Single().ResourcesProvided.Single(t => t.ResourceType == BackendResourceType.IronOre).Value);
            Assert.AreEqual(ironOreProvidedByMine, calcTradingBuilding2.Provisions.Single().ResourcesProvided.Single(t => t.ResourceType == BackendResourceType.IronOre).Value);


            var calculatingMoneyPerSecond = new Dictionary<DbUser, double>();
            MoneyForAllPlayersDeterminer.AddMoneyForPlayersToDictionary(new List<CalculationPackage>() { calculationPackage }, calculatingMoneyPerSecond, null);

            //Also verify if the gold we got for this user is what is expected
            var worth = BackendResourceWorthDeterminer.AllResourceTypeValues[BackendResourceType.IronOre].SellValueToMarket;
            var thisUser = dbTestCalculationHelper.DbUsers.Single(t => t.Id == 1);
            Assert.AreEqual(1, calculatingMoneyPerSecond.Count);
            Assert.IsTrue(TestHelper.AlmostEqual(ironOreProvidedByMine * worth * 2, calculatingMoneyPerSecond[thisUser]));
        }
    }

    [TestMethod]
    public void DeterminedGoldCorrectlyWithTradingToMarketAndOtherPlayers()
    {
        //Arrange
        using (var dbTestCalculationHelper = new DbTestCalculationHelper())
        {

            dbTestCalculationHelper.AddMapStringForUser(1, "" +
                " W+      " + Environment.NewLine +
                "         ");
            dbTestCalculationHelper.AddMapStringForUser(2, "" +
                "   M+T   " + Environment.NewLine +
                "         ");

            dbTestCalculationHelper.DetermineAllConnections();

            //Act
            var calculationPackage = dbTestCalculationHelper.CreateCalculationPackages().Single();
            var totalCalculation = dbTestCalculationHelper.CreateTotalCalculations(new List<CalculationPackage>() { calculationPackage }).Single();
            var allGameObjects = calculationPackage.ProvidersAndConsumersRepository.ConnectedGameObjects;

            //Assert
            var tradingBuilding = allGameObjects.Single(t => t.GameObjectType == GameObjectType.TradingBuilding);

            var calcTradingBuilding = totalCalculation.GetCalculationForGameObject(tradingBuilding);

            Assert.AreEqual(1, calcTradingBuilding.Provisions.Count);

            var energyProvidedByWindmoily = GameResourcesRepository.DetermineResourceOfTypeConsumed(GameObjectType.IronMine, BackendResourceType.Energy);
            var ironOreProvidedByMine = GameResourcesRepository.DetermineResourceOfTypeProduced(GameObjectType.IronMine, BackendResourceType.IronOre);

            Assert.AreEqual(ironOreProvidedByMine, calcTradingBuilding.Provisions.Single().ResourcesProvided.Single(t => t.ResourceType == BackendResourceType.IronOre).Value);

            var calculatingMoneyPerSecond = new Dictionary<DbUser, double>();
            MoneyForAllPlayersDeterminer.AddMoneyForPlayersToDictionary(new List<CalculationPackage>() { calculationPackage }, calculatingMoneyPerSecond, null);

            //Also verify if the gold we got for this user is what is expected
            var worthToMarketIronOre = BackendResourceWorthDeterminer.AllResourceTypeValues[BackendResourceType.IronOre].SellValueToMarket;
            var worthToPlayerEnergy = BackendResourceWorthDeterminer.AllResourceTypeValues[BackendResourceType.Energy].ValueForPlayers;

            var firstUser = dbTestCalculationHelper.DbUsers.Single(t => t.Id == 1);
            var secondUser = dbTestCalculationHelper.DbUsers.Single(t => t.Id == 2);
            Assert.AreEqual(2, calculatingMoneyPerSecond.Count);

            var worthIronToTradingBuilding = ironOreProvidedByMine * worthToMarketIronOre;
            var worthEnergyToPlayer2 = energyProvidedByWindmoily * worthToPlayerEnergy;

            Assert.IsTrue(TestHelper.AlmostEqual(worthEnergyToPlayer2, calculatingMoneyPerSecond[firstUser]));
            Assert.IsTrue(TestHelper.AlmostEqual(worthIronToTradingBuilding - worthEnergyToPlayer2, calculatingMoneyPerSecond[secondUser]));
        }
    }

    [TestMethod]
    public void TestSomething()
    {
        //Arrange
        using (var dbTestCalculationHelper = new DbTestCalculationHelper())
        {
            var db = dbTestCalculationHelper.TransportDb.DbContext;

            dbTestCalculationHelper.AddMapStringForUser(1, "" +
               " W+      " + Environment.NewLine +
               "         ");
            dbTestCalculationHelper.AddMapStringForUser(2, "" +
                "   M+T   " + Environment.NewLine +
                "         ");

            //dbTestCalculationHelper.DetermineAllConnections();
            var itemIds = new List<long>
            {
                1,2,3,4
            };

            //PostgreSQL checks foreign keys, so the global connection that is referenced has to exist
            var globalConnection = db.Insert(new DbGlobalConnection() { GlobalConnectionId = 5, LastCalculated = new DateTime(2015, 1, 1) });

            db.GameObjects
                .Where(t => itemIds.Contains(t.Id))
                .ExecuteUpdate(s => s
                    .SetProperty(t => t.GlobalConnectionId, 5)
                    .SetProperty(t => t.DbGlobalConnectionId, globalConnection.Id));

            var blah = db.Select<DbGameObject>();
            var sources = db.Select<DbGameObject>(t => t.DbGlobalConnectionId != 50 && t.DbGlobalConnectionId != 60);

            Assert.IsTrue(sources.Count > 0);
        }
    }
}

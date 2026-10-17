using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransportPipes.Backend.Calculations;
using TransportPipes.Backend.GameLogicObjects;
using TransportPipes.Backend.Database;
using System.Linq;
using TransportPipesCommon.Enums;
using TransportPipesCommon.Repositories;
using TransportPipesCommon.GameLogicObjects.GameObjectInfo;
using TransportPipesTestsCommon.Helpers;

namespace TransportPipesCalculationTests
{
    [TestClass]
    public class SimpleTests
    {
        /// <summary>
        /// W -- O
        /// </summary>
        [TestMethod]
        public void BasicWindmillAndStorageTest()
        {
            var dbWindMill1 = new DbGameObject(0, 0, GameObjectType.WindMoily, null);
            var windmill1 = new BackendGameObject(dbWindMill1);

            var dbStorage1 = new DbGameObject(0, 2, GameObjectType.Storage, null);
            var storage1 = new BackendGameObject(dbStorage1);


            dbWindMill1.Destinations = new List<DbConnection>() { new DbConnection()
            {
                Sources = new List<DbGameObject>() {  },
                Destinations = new List<DbGameObject>() {  }
            } };
            dbWindMill1.Sources = new List<DbConnection>() { new DbConnection()
            {
                Sources = new List<DbGameObject>() { dbWindMill1  },
                Destinations = new List<DbGameObject>() { dbStorage1 }
            } };


            dbStorage1.Destinations = new List<DbConnection>() { new DbConnection()
            {
                Sources = new List<DbGameObject>() { dbWindMill1  },
                Destinations = new List<DbGameObject>() { dbStorage1 }
            } };
            dbStorage1.Sources = new List<DbConnection>() { new DbConnection()
            {
               Sources = new List<DbGameObject>() {  },
                Destinations = new List<DbGameObject>() {  }
            } };



            var totalCalculation = new TotalCalculationObject();

            storage1.SendOutMyRequestsIfNeeded(TestHelpers.Ticks, totalCalculation);

            var storageCalculation = totalCalculation.GetCalculationForGameObject(storage1);

            var onlyResourceInStorage = storageCalculation.Storage.Single();

            var energyGeneratedByWindmill = GameResourcesRepository.DetermineResourceOfTypeProduced(GameObjectType.WindMoily, BackendResourceType.Energy);

            Assert.AreEqual(BackendResourceType.Energy, onlyResourceInStorage.ResourceType);
            Assert.IsTrue(TestHelper.AlmostEqual(energyGeneratedByWindmill * TestHelpers.Ticks, onlyResourceInStorage.Value));
        }

        /// <summary>
        /// W -- O
        /// </summary>
        [TestMethod]
        public void BasicWindmillAndStorageTest2()
        {
            var dbWindMill1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.WindMoily);
            var dbStorage1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.Storage);

            var windmill1 = TestHelpers.SetConnectionsMock(dbWindMill1, null, new[] { dbStorage1 });
            var storage1 = TestHelpers.SetConnectionsMock(dbStorage1, new[] { dbWindMill1 }, null);

            var storageCalculation = TestHelpers.ExecuteCalculationOnBackendGameObject(storage1);

            var onlyResourceInStorage = storageCalculation.GetCalculationForGameObject(storage1).Storage.Single();

            var energyGeneratedByWindmill = GameResourcesRepository.DetermineResourceOfTypeProduced(GameObjectType.WindMoily, BackendResourceType.Energy);

            Assert.AreEqual(BackendResourceType.Energy, onlyResourceInStorage.ResourceType);
            Assert.IsTrue(TestHelper.AlmostEqual(energyGeneratedByWindmill * TestHelpers.Ticks, onlyResourceInStorage.Value));
        }

        /// <summary>
        /// W -- M -- O
        /// </summary>
        [TestMethod]
        public void WindMillIronMineStorageTest()
        {
            var dbWindMill1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.WindMoily);
            var dbIronMine1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.IronMine);
            var dbStorage1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.Storage);

            var windmill1 = TestHelpers.SetConnectionsMock(dbWindMill1, null, new[] { dbIronMine1 });
            var ironmine1 = TestHelpers.SetConnectionsMock(dbIronMine1, new[] { dbWindMill1 }, new[] { dbStorage1 });
            var storage1 = TestHelpers.SetConnectionsMock(dbStorage1, new[] { dbIronMine1 }, null);

            var storageCalculation = TestHelpers.ExecuteCalculationOnBackendGameObject(storage1);

            var onlyResourceInStorage = storageCalculation.GetCalculationForGameObject(storage1).Storage.Single();

            var ironOreGeneratedByMine = GameResourcesRepository.DetermineResourceOfTypeProduced(GameObjectType.IronMine, BackendResourceType.IronOre);

            Assert.AreEqual(BackendResourceType.IronOre, onlyResourceInStorage.ResourceType);
            Assert.IsTrue(TestHelper.AlmostEqual(ironOreGeneratedByMine * TestHelpers.Ticks, onlyResourceInStorage.Value));
        }

        /// <summary>
        /// W -- M -- O
        /// Only provides 5 energy when a request is made for 5
        /// </summary>
        [TestMethod]
        public void WindMillOnlyProvides5EnergyWhen5IsRequested()
        {
            var dbWindMill1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.WindMoily);
            var dbIronMine1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.IronMine);
            var dbStorage1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.Storage);

            var windmill1 = TestHelpers.SetConnectionsMock(dbWindMill1, null, new[] { dbIronMine1 });
            var ironmine1 = TestHelpers.SetConnectionsMock(dbIronMine1, new[] { dbWindMill1 }, new[] { dbStorage1 });
            var storage1 = TestHelpers.SetConnectionsMock(dbStorage1, new[] { dbIronMine1 }, null);

            var storageCalculation = TestHelpers.ExecuteCalculationOnBackendGameObject(storage1);

            var ironOreProducedByMine = GameResourcesRepository.DetermineResourceOfTypeConsumed(GameObjectType.IronMine, BackendResourceType.Energy);

            var onlyResourceInStorage = storageCalculation.GetCalculationForGameObject(storage1).Storage.Single();
            Assert.AreEqual(BackendResourceType.IronOre, onlyResourceInStorage.ResourceType);
            Assert.IsTrue(TestHelper.AlmostEqual(ironOreProducedByMine * TestHelpers.Ticks, onlyResourceInStorage.Value));

            var energyConsumedByMine = GameResourcesRepository.DetermineResourceOfTypeConsumed(GameObjectType.IronMine, BackendResourceType.Energy);

            var resourcesProvidedToMine = storageCalculation.GetCalculationForGameObject(ironmine1).Provisions.SelectMany(t => t.ResourcesProvided).ToList();
            Assert.AreEqual(1, resourcesProvidedToMine.Count);
            Assert.AreEqual(BackendResourceType.Energy, resourcesProvidedToMine[0].ResourceType);
            Assert.IsTrue(TestHelper.AlmostEqual(energyConsumedByMine * TestHelpers.Ticks, resourcesProvidedToMine[0].Value));
        }

        /// <summary>
        /// W ---|
        ///      |
        ///      | -- M -- O
        ///      |
        /// W ---|
        /// Only provides 5 energy when a request is made for 5
        /// </summary>
        [TestMethod]
        public void WindMillOnlyProvides5EnergyWhen5IsRequestedBy2WindMills()
        {
            var dbWindMill1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.WindMoily);
            var dbWindMill2 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.WindMoily);
            var dbIronMine1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.IronMine);
            var dbStorage1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.Storage);

            var windmill1 = TestHelpers.SetConnectionsMock(dbWindMill1, null, new[] { dbIronMine1 });
            var windmill2 = TestHelpers.SetConnectionsMock(dbWindMill2, null, new[] { dbIronMine1 });
            var ironmine1 = TestHelpers.SetConnectionsMock(dbIronMine1, new[] { dbWindMill1, dbWindMill2 }, new[] { dbStorage1 });
            var storage1 = TestHelpers.SetConnectionsMock(dbStorage1, new[] { dbIronMine1 }, null);

            var storageCalculation = TestHelpers.ExecuteCalculationOnBackendGameObject(storage1);

            var ironOreProducedByMine = GameResourcesRepository.DetermineResourceOfTypeConsumed(GameObjectType.IronMine, BackendResourceType.Energy);

            var onlyResourceInStorage = storageCalculation.GetCalculationForGameObject(storage1).Storage.Single();
            Assert.AreEqual(BackendResourceType.IronOre, onlyResourceInStorage.ResourceType);
            Assert.IsTrue(TestHelper.AlmostEqual(ironOreProducedByMine * TestHelpers.Ticks, onlyResourceInStorage.Value));

            var energyConsumedByMine = GameResourcesRepository.DetermineResourceOfTypeConsumed(GameObjectType.IronMine, BackendResourceType.Energy);

            var resourcesProvidedToMine = storageCalculation.GetCalculationForGameObject(ironmine1).Provisions.SelectMany(t => t.ResourcesProvided).ToList();
            var summedResourcesProvidedToMine = TestHelpers.SumResources(resourcesProvidedToMine);
            Assert.AreEqual(1, summedResourcesProvidedToMine.Count);
            Assert.AreEqual(BackendResourceType.Energy, summedResourcesProvidedToMine[0].ResourceType);
            Assert.IsTrue(TestHelper.AlmostEqual(energyConsumedByMine * TestHelpers.Ticks, summedResourcesProvidedToMine[0].Value));
        }

        /// <summary>
        /// S --- M --- O
        ///    |      |
        ///    -- M -- 
        /// Mines will work at half speed
        /// </summary>
        [TestMethod]
        public void SolarPanelWith2MinesAndStorage()
        {
            var dbSolarPanel1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.SolarPanel);
            var dbIronMine1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.IronMine);
            var dbIronMine2 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.IronMine);
            var dbStorage1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.Storage);

            var solarpanel1 = TestHelpers.SetConnectionsMock(dbSolarPanel1, null, new[] { dbIronMine1, dbIronMine2 });
            var ironmine1 = TestHelpers.SetConnectionsMock(dbIronMine1, new[] { dbSolarPanel1 }, new[] { dbStorage1 });
            var ironmine2 = TestHelpers.SetConnectionsMock(dbIronMine2, new[] { dbSolarPanel1 }, new[] { dbStorage1 });
            var storage1 = TestHelpers.SetConnectionsMock(dbStorage1, new[] { dbIronMine1, dbIronMine2 }, null);

            var storageCalculation = TestHelpers.ExecuteCalculationOnBackendGameObject(storage1);

            var resourcesInStorage = storageCalculation.GetCalculationForGameObject(storage1).Storage.ToList();

            var ironoreProducedByMine = GameResourcesRepository.DetermineResourceOfTypeProduced(GameObjectType.IronMine, BackendResourceType.IronOre);

            //Test total resources
            var summedResources = TestHelpers.SumResources(resourcesInStorage);
            Assert.AreEqual(summedResources.Count, 1);
            Assert.AreEqual(BackendResourceType.IronOre, summedResources[0].ResourceType);
            Assert.IsTrue(TestHelper.AlmostEqual(ironoreProducedByMine * TestHelpers.Ticks, summedResources[0].Value));
        }

        /// <summary>
        /// W --- F --- O
        /// |     |
        /// M ---- 
        /// Factory uses 6 energy and mine 5 so they can't work at full speed
        /// </summary>
        [TestMethod]
        public void FactoryNotBeingAbleToWorkAtFullSpeed()
        {
            var dbWindMill1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.WindMoily);
            var dbIronMine1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.IronMine);
            var dbFactory1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.FactorySmall);
            var dbStorage1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.Storage);

            var windmill1 = TestHelpers.SetConnectionsMock(dbWindMill1, null, new[] { dbIronMine1, dbFactory1 });
            var ironmine1 = TestHelpers.SetConnectionsMock(dbIronMine1, new[] { dbWindMill1 }, new[] { dbFactory1 });
            var factory1 = TestHelpers.SetConnectionsMock(dbFactory1, new[] { dbWindMill1, dbIronMine1 }, new[] { dbStorage1 });
            var storage1 = TestHelpers.SetConnectionsMock(dbStorage1, new[] { dbFactory1 }, null);

            var storageCalculation = TestHelpers.ExecuteCalculationOnBackendGameObject(storage1);

            var resourcesInStorage = storageCalculation.GetCalculationForGameObject(storage1).Storage.ToList();

            var ironProducedByFactory = GameResourcesRepository.DetermineResourceOfTypeProduced(GameObjectType.FactorySmall, BackendResourceType.Iron);

            var energyConsumedByMine = GameResourcesRepository.DetermineResourceOfTypeConsumed(GameObjectType.IronMine, BackendResourceType.Energy);
            var energyConsumedByFactory = GameResourcesRepository.DetermineResourceOfTypeConsumed(GameObjectType.FactorySmall, BackendResourceType.Energy);
            var totalEnergyConsumed = energyConsumedByFactory + energyConsumedByMine;

            var energyProducedByWindmill = GameResourcesRepository.DetermineResourceOfTypeProduced(GameObjectType.WindMoily, BackendResourceType.Energy);

            var percentageOfEnergyIsAvailable = energyProducedByWindmill / totalEnergyConsumed;

            //Test total resources
            var summedResources = TestHelpers.SumResources(resourcesInStorage);
            Assert.AreEqual(summedResources.Count, 1);
            Assert.AreEqual(BackendResourceType.Iron, summedResources[0].ResourceType);
            Assert.IsTrue(TestHelper.AlmostEqual(ironProducedByFactory * percentageOfEnergyIsAvailable * TestHelpers.Ticks, summedResources[0].Value));

            //Test if provisioned also works (Should return the same value as above)
            var provisionsToStorage = storageCalculation.GetCalculationForGameObject(storage1).Provisions.SelectMany(t => t.ResourcesProvided).ToList();
            var summedProvisionsToStorage = TestHelpers.SumResources(provisionsToStorage);
            Assert.AreEqual(summedProvisionsToStorage.Count, 1);
            Assert.AreEqual(BackendResourceType.Iron, summedProvisionsToStorage[0].ResourceType);
            Assert.IsTrue(TestHelper.AlmostEqual(ironProducedByFactory * percentageOfEnergyIsAvailable * TestHelpers.Ticks, summedProvisionsToStorage[0].Value));

            var energyRequestedByFactory = GameResourcesRepository.DetermineResourceOfTypeConsumed(GameObjectType.FactorySmall, BackendResourceType.Energy);
            var ironoreRequestedByFactory = GameResourcesRepository.DetermineResourceOfTypeConsumed(GameObjectType.FactorySmall, BackendResourceType.IronOre);

            //Test if the Factory has also received the right resources
            var provisionsToFactory = storageCalculation.GetCalculationForGameObject(factory1).Provisions.SelectMany(t => t.ResourcesProvided).ToList();
            var summedProvisionsToFactory = TestHelpers.SumResources(provisionsToFactory);
            Assert.AreEqual(summedProvisionsToFactory.Count, 2);
            Assert.IsTrue(summedProvisionsToFactory.Any(t => t.ResourceType == BackendResourceType.Energy && TestHelper.AlmostEqual(t.Value, energyRequestedByFactory * percentageOfEnergyIsAvailable * TestHelpers.Ticks)));
            Assert.IsTrue(summedProvisionsToFactory.Any(t => t.ResourceType == BackendResourceType.IronOre && TestHelper.AlmostEqual(t.Value, ironoreRequestedByFactory * percentageOfEnergyIsAvailable * TestHelpers.Ticks)));
        }
    }
}

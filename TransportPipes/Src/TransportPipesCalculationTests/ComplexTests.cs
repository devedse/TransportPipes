using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransportPipes.Backend.Calculations;
using System.Linq;
using TransportPipes.Backend.GameLogicObjects.Resources;
using TransportPipesCommon.Enums;
using TransportPipesCommon.GameLogicObjects.GameObjectInfo;
using TransportPipesCommon.Repositories;
using TransportPipesTestsCommon.Helpers;

namespace TransportPipesCalculationTests
{
    [TestClass]
    public class ComplexTests
    {
        /// <summary>
        ///       O2 -- W2
        ///       |
        /// O1 -------- W1
        /// </summary>
        [TestMethod]
        public void DistributionWorksWithMultipleRequestersInSomewhatSeperateNetworks()
        {
            var dbWindMill1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.WindMoily);
            var dbWindMill2 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.WindMoily);
            var dbStorage1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.Storage);
            var dbStorage2 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.Storage);

            var windmill1 = TestHelpers.SetConnectionsMock(dbWindMill1, null, new[] { dbStorage1, dbStorage2 });
            var windmill2 = TestHelpers.SetConnectionsMock(dbWindMill2, null, new[] { dbStorage2 });
            var storage1 = TestHelpers.SetConnectionsMock(dbStorage1, new[] { dbWindMill1 }, null);
            var storage2 = TestHelpers.SetConnectionsMock(dbStorage2, new[] { dbWindMill1, dbWindMill2 }, null);


            var storageCalculation = TestHelpers.ExecuteCalculationOnBackendGameObject(storage1);

            double energyGeneratedByWindmill = GameResourcesRepository.DetermineResourceOfTypeProduced(GameObjectType.WindMoily, BackendResourceType.Energy);

            //Check if the first storage has received 1/2th of what W1 generates
            var firstCheck = storageCalculation.GetCalculationForGameObject(storage1).Provisions.Single();
            var resourcesInFirstCheck = firstCheck.ResourcesProvided.Single();
            Assert.AreEqual(firstCheck.Provisioner, windmill1);
            Assert.AreEqual(BackendResourceType.Energy, resourcesInFirstCheck.ResourceType);
            Assert.IsTrue(TestHelper.AlmostEqual((energyGeneratedByWindmill / 2) * TestHelpers.Ticks, resourcesInFirstCheck.Value));

            //Check if this storage has received the full load of W2 and 1/3 W1
            var secondCheck = storageCalculation.GetCalculationForGameObject(storage2).Provisions;
            Assert.AreEqual(2, secondCheck.Count);
            Assert.IsTrue(secondCheck.Any(t => t.Provisioner == windmill1 && t.ResourcesProvided.Single(z => z.ResourceType == BackendResourceType.Energy && TestHelper.AlmostEqual(z.Value, energyGeneratedByWindmill / 2 * TestHelpers.Ticks)) != null));
            Assert.IsTrue(secondCheck.Any(t => t.Provisioner == windmill2 && t.ResourcesProvided.Single(z => z.ResourceType == BackendResourceType.Energy && TestHelper.AlmostEqual(z.Value, energyGeneratedByWindmill * TestHelpers.Ticks)) != null));
        }

        /// <summary>
        ///       O2 -- W2
        ///       |
        /// O1 -------- W1
        /// Here we do the test 4 times once for every object in here
        /// If this test succeeds and the above fails it must be a bug in the code not being able to correctly get the same results when starting the calculation from a different place
        /// </summary>
        [TestMethod]
        public void CheckIfForADistributionTheSameValuesAppearWhenWeStartCalculatingFromAnotherObject()
        {
            var dbWindMill1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.WindMoily);
            var dbWindMill2 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.WindMoily);
            var dbStorage1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.Storage);
            var dbStorage2 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.Storage);

            var windmill1 = TestHelpers.SetConnectionsMock(dbWindMill1, null, new[] { dbStorage1, dbStorage2 });
            var windmill2 = TestHelpers.SetConnectionsMock(dbWindMill2, null, new[] { dbStorage2 });
            var storage1 = TestHelpers.SetConnectionsMock(dbStorage1, new[] { dbWindMill1 }, null);
            var storage2 = TestHelpers.SetConnectionsMock(dbStorage2, new[] { dbWindMill1, dbWindMill2 }, null);

            var calculations = new List<TotalCalculationObject>();
            calculations.Add(TestHelpers.ExecuteCalculationOnBackendGameObject(windmill1));
            calculations.Add(TestHelpers.ExecuteCalculationOnBackendGameObject(windmill2));
            calculations.Add(TestHelpers.ExecuteCalculationOnBackendGameObject(storage1));
            calculations.Add(TestHelpers.ExecuteCalculationOnBackendGameObject(storage2));

            double energyGeneratedByWindmill = GameResourcesRepository.DetermineResourceOfTypeProduced(GameObjectType.WindMoily, BackendResourceType.Energy);

            foreach (var calculationObject in calculations)
            {
                //Check if the first storage has received 2/3th of what W1 generates
                var firstCheck = calculationObject.GetCalculationForGameObject(storage1).Provisions.Single();
                var resourcesInFirstCheck = firstCheck.ResourcesProvided.Single();
                Assert.AreEqual(firstCheck.Provisioner, windmill1);
                Assert.AreEqual(BackendResourceType.Energy, resourcesInFirstCheck.ResourceType);
                Assert.IsTrue(TestHelper.AlmostEqual((energyGeneratedByWindmill / 2) * TestHelpers.Ticks, resourcesInFirstCheck.Value));

                //Check if this storage has received the full load of W2 and 1/3 W1
                var secondCheck = calculationObject.GetCalculationForGameObject(storage2).Provisions;
                Assert.AreEqual(2, secondCheck.Count);
                Assert.IsTrue(secondCheck.Any(t => t.Provisioner == windmill1 && t.ResourcesProvided.Single(z => z.ResourceType == BackendResourceType.Energy && TestHelper.AlmostEqual(z.Value, energyGeneratedByWindmill / 2 * TestHelpers.Ticks)) != null));
                Assert.IsTrue(secondCheck.Any(t => t.Provisioner == windmill2 && t.ResourcesProvided.Single(z => z.ResourceType == BackendResourceType.Energy && TestHelper.AlmostEqual(z.Value, energyGeneratedByWindmill * TestHelpers.Ticks)) != null));
            }
        }

        /// <summary>
        /// W1 -- M1
        ///       |
        ///       |----- F ---- O
        ///       |      |
        /// W2 -- M2     W3
        /// </summary>
        [TestMethod]
        public void WorksWithThisFinePieceOfConnections()
        {
            var dbWindMill1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.WindMoily);
            var dbWindMill2 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.WindMoily);
            var dbIronMine1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.IronMine);
            var dbIronMine2 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.IronMine);

            var dbFactory1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.FactorySmall);
            var dbWindMill3 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.WindMoily);

            var dbStorage1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.Storage);



            var windmill1 = TestHelpers.SetConnectionsMock(dbWindMill1, null, new[] { dbIronMine1 });
            var windmill2 = TestHelpers.SetConnectionsMock(dbWindMill2, null, new[] { dbIronMine2 });
            var ironmine1 = TestHelpers.SetConnectionsMock(dbIronMine1, new[] { dbWindMill1 }, new[] { dbFactory1 });
            var ironmine2 = TestHelpers.SetConnectionsMock(dbIronMine2, new[] { dbWindMill2 }, new[] { dbFactory1 });

            var factory1 = TestHelpers.SetConnectionsMock(dbFactory1, new[] { dbIronMine1, dbIronMine2, dbWindMill3 }, new[] { dbStorage1 });
            var windmill3 = TestHelpers.SetConnectionsMock(dbWindMill3, null, new[] { dbFactory1 });

            var storage1 = TestHelpers.SetConnectionsMock(dbStorage1, new[] { dbFactory1 }, null);

            double ironGeneratedByFactory = GameResourcesRepository.DetermineResourceOfTypeProduced(GameObjectType.FactorySmall, BackendResourceType.Iron);


            var totalCalculation = TestHelpers.ExecuteCalculationOnBackendGameObject(storage1);
            var storageCalculation = totalCalculation.GetCalculationForGameObject(storage1);

            var allProvisions = storageCalculation.Provisions.SelectMany(t => t.ResourcesProvided).ToList();
            Assert.AreEqual(1, allProvisions.Count);
            Assert.IsNotNull(allProvisions.Single(t => t.ResourceType == BackendResourceType.Iron && TestHelper.AlmostEqual(t.Value, ironGeneratedByFactory * TestHelpers.Ticks)));
        }



        /// <summary>
        /// S1  S2             W1
        ///  |   |              |
        ///  -------- M1 ----- F1 -- O1
        ///  |
        ///  | ------ M2 --|
        ///  | ------ M3 --|-- F2 -- O2
        ///                     |
        ///                    W2
        /// </summary>
        [TestMethod]
        public void DistributionWorksWithMultipleRequestersInSomewhatSeperateNetworksMoreComplexEdition()
        {
            var dbSolarPanel1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.SolarPanel);
            var dbSolarPanel2 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.SolarPanel);
            var dbIronMine1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.IronMine);
            var dbIronMine2 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.IronMine);
            var dbIronMine3 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.IronMine);

            var dbFactory1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.FactorySmall);
            var dbFactory2 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.FactorySmall);
            var dbWindMill1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.WindMoily);
            var dbWindMill2 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.WindMoily);
            var dbStorage1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.Storage);
            var dbStorage2 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.Storage);


            var solarpanel1 = TestHelpers.SetConnectionsMock(dbSolarPanel1, null, new[] { dbIronMine1, dbIronMine2, dbIronMine3 });
            var solanpanel2 = TestHelpers.SetConnectionsMock(dbSolarPanel2, null, new[] { dbIronMine1, dbIronMine2, dbIronMine3 });
            var ironmine1 = TestHelpers.SetConnectionsMock(dbIronMine1, new[] { dbSolarPanel1, dbSolarPanel2 }, new[] { dbFactory1 });
            var ironmine2 = TestHelpers.SetConnectionsMock(dbIronMine2, new[] { dbSolarPanel1, dbSolarPanel2 }, new[] { dbFactory2 });
            var ironmine3 = TestHelpers.SetConnectionsMock(dbIronMine3, new[] { dbSolarPanel1, dbSolarPanel2 }, new[] { dbFactory2 });

            var factory1 = TestHelpers.SetConnectionsMock(dbFactory1, new[] { dbIronMine1, dbWindMill1 }, new[] { dbStorage1 });
            var factory2 = TestHelpers.SetConnectionsMock(dbFactory2, new[] { dbIronMine2, dbIronMine3, dbWindMill2 }, new[] { dbStorage2 });
            var windmill1 = TestHelpers.SetConnectionsMock(dbWindMill1, null, new[] { dbFactory1 });
            var windmill2 = TestHelpers.SetConnectionsMock(dbWindMill2, null, new[] { dbFactory2 });
            var storage1 = TestHelpers.SetConnectionsMock(dbStorage1, new[] { dbFactory1 }, null);
            var storage2 = TestHelpers.SetConnectionsMock(dbStorage2, new[] { dbFactory2 }, null);


            var totalCalculation = TestHelpers.ExecuteCalculationOnBackendGameObject(storage1);
            var storage1Calculation = totalCalculation.GetCalculationForGameObject(storage1);
            var storage2Calculation = totalCalculation.GetCalculationForGameObject(storage2);

            double ironGeneratedByFactory = GameResourcesRepository.DetermineResourceOfTypeProduced(GameObjectType.FactorySmall, BackendResourceType.Iron);

            //Check storage 2 first because this one will work even with the distribution bug in place
            var storage2Provisions = storage2Calculation.Provisions.SelectMany(t => t.ResourcesProvided).ToList();
            var storage2SummedProvisions = TestHelpers.SumResources(storage2Provisions);
            Assert.IsNotNull(storage2SummedProvisions.Single(t => t.ResourceType == BackendResourceType.Iron && TestHelper.AlmostEqual(t.Value, ironGeneratedByFactory * TestHelpers.Ticks)));

            //Now check if this one has the same resources as the top one
            var storage1Provisions = storage1Calculation.Provisions.SelectMany(t => t.ResourcesProvided).ToList();
            var storage1SummedProvisions = TestHelpers.SumResources(storage1Provisions);
            Assert.IsNotNull(storage1SummedProvisions.Single(t => t.ResourceType == BackendResourceType.Iron && TestHelper.AlmostEqual(t.Value, ironGeneratedByFactory * TestHelpers.Ticks)));
        }


        /// <summary>
        /// W1 ---   ---- M1 ----|
        ///      |   |           |
        ///      |---|--- M2 ----|---- O1
        ///      |   |           |
        /// S1 ---   ---- M3 ----|
        ///
        /// </summary>
        [TestMethod]
        public void SolarAndWindMillDistribution()
        {
            var dbWindMill1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.WindMoily);
            var dbSolarPanel1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.SolarPanel);
            var dbIronMine1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.IronMine);
            var dbIronMine2 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.IronMine);
            var dbIronMine3 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.IronMine);

            var dbStorage1 = TestHelpers.GenerateDbGameObjectMock(GameObjectType.Storage);


            var solarpanel1 = TestHelpers.SetConnectionsMock(dbSolarPanel1, null, new[] { dbIronMine1, dbIronMine2, dbIronMine3 });
            var windmill1 = TestHelpers.SetConnectionsMock(dbWindMill1, null, new[] { dbIronMine1, dbIronMine2, dbIronMine3 });

            var ironmine1 = TestHelpers.SetConnectionsMock(dbIronMine1, new[] { dbSolarPanel1, dbWindMill1 }, new[] { dbStorage1 });
            var ironmine2 = TestHelpers.SetConnectionsMock(dbIronMine2, new[] { dbSolarPanel1, dbWindMill1 }, new[] { dbStorage1 });
            var ironmine3 = TestHelpers.SetConnectionsMock(dbIronMine3, new[] { dbSolarPanel1, dbWindMill1 }, new[] { dbStorage1 });

            var storage1 = TestHelpers.SetConnectionsMock(dbStorage1, new[] { dbIronMine1, dbIronMine2, dbIronMine3 }, null);


            var totalCalculation = TestHelpers.ExecuteCalculationOnBackendGameObject(storage1);
            var storage1Calculation = totalCalculation.GetCalculationForGameObject(storage1);

            double ironoreGeneratedByMine = GameResourcesRepository.DetermineResourceOfTypeProduced(GameObjectType.IronMine, BackendResourceType.IronOre);

            //Now check if this one has the same resources as the top one
            var storage1Provisions = storage1Calculation.Provisions.SelectMany(t => t.ResourcesProvided).ToList();
            var storage1SummedProvisions = TestHelpers.SumResources(storage1Provisions);
            Assert.IsNotNull(storage1SummedProvisions.Single(t => t.ResourceType == BackendResourceType.IronOre && TestHelper.AlmostEqual(t.Value, ironoreGeneratedByMine * 3 * TestHelpers.Ticks)));
        }
    }
}

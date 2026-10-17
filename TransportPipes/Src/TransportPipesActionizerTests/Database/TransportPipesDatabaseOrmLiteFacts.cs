using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransportPipesActionizerTests.Database.Helpers;
using TransportPipesDatabase.Poco;

namespace TransportPipesActionizerTests.Database
{
    namespace TransportPipesDatabaseOrmLiteFacts
    {
        [TestClass]
        public class TheFindAllCalculationsForTheseGlobalConnectionIdsMethod
        {
            [TestMethod]
            public void DoesTheCorrectJoin()
            {
                //Arrange
                using (var dbTestCalculationHelper = new DbTestCalculationHelper(realDb: false))
                {

                    dbTestCalculationHelper.AddMapStringForUser(1, "" +
                        "            " + Environment.NewLine +
                        "   O+W      " + Environment.NewLine +
                        "            " + Environment.NewLine +
                        "            " + Environment.NewLine +
                        "            ");

                    dbTestCalculationHelper.AddMapStringForUser(2, "" +
                        "            " + Environment.NewLine +
                        "            " + Environment.NewLine +
                        "   +        " + Environment.NewLine +
                        " O+++W      " + Environment.NewLine +
                        "            ");


                    var b = new StringBuilder();
                    b.AppendLine("" +
                        "            " + Environment.NewLine +
                        "            " + Environment.NewLine +
                        "            " + Environment.NewLine +
                        "            ");
                    for (int i = 0; i < 10; i++)
                    {
                        b.AppendLine("W+++++++++W");
                    }

                    dbTestCalculationHelper.AddMapStringForUser(3, b.ToString());

                    dbTestCalculationHelper.DetermineAllConnections();

                    var allGlobCons = dbTestCalculationHelper.GetAllGlobalConnectionIds();

                    var results = dbTestCalculationHelper.TransportDb.FindAllCalculationsForTheseGlobalConnectionIds(allGlobCons).Single();

                    Assert.AreEqual(24, results.ProvidersAndConsumersRepository.ConnectedGameObjects.Count);
                    Assert.AreEqual(3, results.ProvidersAndConsumersRepository.ConnectedGameObjects.Select(t => t.DbOwnerId).Distinct().Count());
                }
            }
        }

        [TestClass]
        public class TheChangeGoldOfPlayerMethod
        {
            [TestMethod]
            public void CorrectlyAddsGold()
            {
                //Arrange
                using (var dbTestCalculationHelper = new DbTestCalculationHelper(realDb: false))
                {
                    //Act
                    dbTestCalculationHelper.TransportDb.ChangeGoldOfPlayer(5, 500);
                    dbTestCalculationHelper.TransportDb.ChangeGoldOfPlayer(5, 250);

                    //Assert
                    var foundUser = dbTestCalculationHelper.TransportDb.DbConnection.Select<DbUser>(t => t.UserName == "user5").Single();
                    Assert.AreEqual(750, foundUser.Gold);
                }
            }
        }

        [TestClass]
        public class TheUpdateGoldForUsersMethod
        {
            [TestMethod]
            public void CorrectlyAddsGoldForMultipleUsers()
            {
                //Arrange
                using (var dbTestCalculationHelper = new DbTestCalculationHelper(realDb: false))
                {
                    var dict1 = new Dictionary<DbUser, double>();
                    var dict2 = new Dictionary<DbUser, double>();

                    var user2 = dbTestCalculationHelper.TransportDb.DbConnection.Select<DbUser>(t => t.UserName == "user2").Single();
                    var user4 = dbTestCalculationHelper.TransportDb.DbConnection.Select<DbUser>(t => t.UserName == "user4").Single();
                    var user6 = dbTestCalculationHelper.TransportDb.DbConnection.Select<DbUser>(t => t.UserName == "user6").Single();


                    dict1.Add(user2, 500);
                    dict1.Add(user4, -250);

                    dict2.Add(user4, 300);
                    dict2.Add(user6, -10);

                    //Act
                    dbTestCalculationHelper.TransportDb.UpdateGoldForUsers(dict1);
                    dbTestCalculationHelper.TransportDb.UpdateGoldForUsers(dict2);

                    //Assert
                    user2 = dbTestCalculationHelper.TransportDb.DbConnection.Select<DbUser>(t => t.UserName == "user2").Single();
                    user4 = dbTestCalculationHelper.TransportDb.DbConnection.Select<DbUser>(t => t.UserName == "user4").Single();
                    user6 = dbTestCalculationHelper.TransportDb.DbConnection.Select<DbUser>(t => t.UserName == "user6").Single();
                    var user8 = dbTestCalculationHelper.TransportDb.DbConnection.Select<DbUser>(t => t.UserName == "user8").Single();

                    Assert.AreEqual(500, user2.Gold);
                    Assert.AreEqual(50, user4.Gold);
                    Assert.AreEqual(-10, user6.Gold);
                    Assert.AreEqual(0, user8.Gold);
                }
            }
        }

        [TestClass]
        public class TheGetClaimedGoldForUserMethod
        {
            [TestMethod]
            public void CorrectlyDeterminesHowMuchGoldIsStillOpen()
            {
                //Arrange
                using (var dbTestCalculationHelper = new DbTestCalculationHelper(realDb: false))
                {
                    dbTestCalculationHelper.TransportDb.DbConnection.Insert(new DbAction() { ExecutedById = 5, GoldClaimed = 500 });
                    dbTestCalculationHelper.TransportDb.DbConnection.Insert(new DbAction() { ExecutedById = 5, GoldClaimed = 250 });
                    dbTestCalculationHelper.TransportDb.DbConnection.Insert(new DbAction() { ExecutedById = 5, GoldClaimed = 5 });

                    //Act
                    var result = dbTestCalculationHelper.TransportDb.GetClaimedGoldForUser(5);

                    //Assert
                    Assert.AreEqual(500 + 250 + 5, result);
                }
            }
        }
    }
}

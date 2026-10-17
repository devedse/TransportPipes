using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using TransportPipesActionizer.Calculators;
using TransportPipesCalculators.CalculationObjects;
using TransportPipesCalculators.Calculators;
using TransportPipesCommon.Enums;
using TransportPipesDatabase;
using TransportPipesDatabase.Poco;
using TransportPipesDatabase.Repositories;

namespace TransportPipesActionizerTests.Database
{
    public static class DbSetupHelper
    {
        public static OrmLiteConnectionFactory CreateInMemoryConnectionFactory()
        {
            //Additional information: You must set the singleton 'OrmLiteConfig.DialectProvider' to use the OrmLiteWriteExtensions

            OrmLiteConfig.DialectProvider = SqliteOrmLiteDialectProvider.Instance;

            var inMemoryConnectionFactory = new OrmLiteConnectionFactory(":memory:", SqliteOrmLiteDialectProvider.Instance, false);
            return inMemoryConnectionFactory;
        }

        public static List<DbUser> AddDefaultUsers(IDbConnection db)
        {
            var adminuser = new DbUser() { UserName = "admin", Password = "test", Email = "admin@test.com" };
            var otheruser = new DbUser() { UserName = "otheruser", Password = "othertest", Email = "other@test.com" };
            adminuser.Id = db.Insert(adminuser, selectIdentity: true); //Select Identity to obtain the ID of these users
            otheruser.Id = db.Insert(otheruser, selectIdentity: true); //Select Identity to obtain the ID of these users

            return new List<DbUser>() { adminuser, otheruser };
        }

        public static DbGameObject GetDbGameObject(IDbConnection db, int x, int y)
        {
            var foundGameObject = db.Select<DbGameObject>(t => t.X == x && t.Y == y).Single();
            return foundGameObject;
        }

        public static List<DbGameObject> GetAllDbGameObjects(IDbConnection db)
        {
            var foundGameObjects = db.Select<DbGameObject>();
            return foundGameObjects;
        }

        /// <summary>
        /// Enter a string for example:
        /// 00000000
        /// 00W00000
        /// 00+00000
        /// 00+00000
        /// 00+++O00
        /// 00000000
        /// 
        /// Allowed values:
        /// W: WindMoily
        /// S: SolarPanel
        /// +: GreyPipe
        /// O: Storage
        /// M: IronMine
        /// F: FactorySmall
        /// Everything else is empty (e.g. Space or 0)
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static TotalCalculationObject StartCalculatingThisMap(string str, int xOfGlobalConnection, int yOfGlobalConnection, out List<DbGameObject> allGameObjectsInThisGlobalConnection, int ticks = 1)
        {
            //Setup Database
            var transportFactory = new TransportPipesDbFactory(CreateInMemoryConnectionFactory());

            using (var transportDb = transportFactory.CreateDatabaseWithOpenConnection())
            {
                transportDb.CreateDbIfNotExists();
                var users = AddDefaultUsers(transportDb.DbConnection);

                //Add GameObjects
                AddDbGameObjectsByStringDefinition(transportDb.DbConnection, str, users.First().Id);

                //Determine Connections
                //var connectionFinder = new ConnectionFinder(transportDb);
                //TODO: Ensure that all GameObjects have their connections found
                //connectionFinder.RedetermineAllConnectionsForGameObject(xOfGlobalConnection, yOfGlobalConnection);
                DetermineAllConnections(transportDb.DbConnection, transportDb, new DateTime(2015, 1, 1));

                //Find GameObjects in this GlobalConnection
                DbGameObject theCurrentDbGameObject = transportDb.DbConnection.Single<DbGameObject>(t => t.X == xOfGlobalConnection && t.Y == yOfGlobalConnection);

                allGameObjectsInThisGlobalConnection = transportDb.DbConnection.Select<DbGameObject>(t => t.GlobalConnectionId == theCurrentDbGameObject.GlobalConnectionId);


                //Find ConsumerProviders in this GlobalConnection
                var consumerProviders = transportDb.ObtainListOfConsumerProviderConnections(theCurrentDbGameObject.GlobalConnectionId);

                //Initialize the 
                var pacRepository = new ProvidersAndConsumersRepository(allGameObjectsInThisGlobalConnection, consumerProviders);
                var resourceCalculator = new ResourceCalculator(pacRepository);

                //Execute the calculation
                var totalCalculation = new TotalCalculationObject();
                resourceCalculator.ExecuteCompleteCalculation(theCurrentDbGameObject, ticks, totalCalculation);

                //Return the calculation
                return totalCalculation;
            }
        }

        public static void DetermineAllConnections(IDbConnection db, ITransportDatabase transportDb, DateTime timeOfCreation)
        {
            var connectionFinder = new ConnectionFinder(transportDb);

            List<DbGameObject> todo = db.Select<DbGameObject>(t => t.GlobalConnectionId == 0);

            var todoPreviously = todo.Count;
            while (todo.Count > 0)
            {
                var curGameObject = todo.First();
                var foundConnections = connectionFinder.RedetermineAllConnectionsForGameObject(curGameObject.X, curGameObject.Y, timeOfCreation);

                var processedGameObjects = foundConnections.SelectMany(t => t.SelectMany(z => z.Select(g => g))).ToList();

                todo.RemoveAll(t => processedGameObjects.Any(z => t.X == z.X && t.Y == z.Y));

                if (todoPreviously == todo.Count)
                {
                    Trace.WriteLine("Not all GameObjects on the map are in a Connection");
                    break;
                }
                todoPreviously = todo.Count;
            }
        }

        /// <summary>
        /// Enter a string for example:
        /// 00000000
        /// 00W00000
        /// 00+00000
        /// 00+00000
        /// 00+++O00
        /// 00000000
        /// 
        /// Allowed values:
        /// W: WindMoily
        /// S: SolarPanel
        /// +: GreyPipe
        /// O: Storage
        /// M: IronMine
        /// F: FactorySmall
        /// Everything else is empty (e.g. Space or 0)
        /// </summary>
        /// <param name="connectionFactory"></param>
        /// <param name="str"></param>
        public static void AddDbGameObjectsByStringDefinition(IDbConnection db, string str, long ownerId)
        {
            var splittedString = str.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            for (int y = 0; y < splittedString.Length; y++)
            {
                var curSplit = splittedString[y];
                for (int x = 0; x < curSplit.Length; x++)
                {
                    var curCharacter = curSplit.Substring(x, 1).ToUpperInvariant()[0]; //Get uppercase character
                    GameObjectType gameObjectType = GameObjectType.None;
                    switch (curCharacter)
                    {
                        case 'W':
                            gameObjectType = GameObjectType.WindMoily;
                            break;
                        case 'S':
                            gameObjectType = GameObjectType.SolarPanel;
                            break;
                        case 'M':
                            gameObjectType = GameObjectType.IronMine;
                            break;
                        case '+':
                            gameObjectType = GameObjectType.GreyPipe;
                            break;
                        case 'O':
                            gameObjectType = GameObjectType.Storage;
                            break;
                        case 'F':
                            gameObjectType = GameObjectType.FactorySmall;
                            break;
                        case 'T':
                            gameObjectType = GameObjectType.TradingBuilding;
                            break;
                    }

                    if (gameObjectType != GameObjectType.None)
                    {
                        AddSimpleDbGameObject(db, gameObjectType, x, y, ownerId);
                    }
                }
            }
        }

        public static void AddDbGameObject(IDbConnection db, DbGameObject newDbGameObject)
        {
            db.Insert(newDbGameObject);
        }

        public static void AddSimpleDbGameObject(IDbConnection db, GameObjectType gameObjectType, int x, int y, long ownerId)
        {
            var newDbGameObject = new DbGameObject(x, y, gameObjectType, new DbUser() { Id = ownerId });
            AddDbGameObject(db, newDbGameObject);
        }

        public static void RemoveDbGameObject(IDbConnection db, int x, int y)
        {
            db.Delete<DbGameObject>(t => t.X == x && t.Y == y);
        }
    }
}

using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using TransportPipes.Actionizer.Calculators;
using TransportPipes.Calculators.CalculationObjects;
using TransportPipes.Calculators.Calculators;
using TransportPipes.Common.Enums;
using TransportPipes.Database;
using TransportPipes.Database.Entities;
using TransportPipes.Database.Entities.CombinedPoco;
using TransportPipes.Database.Repositories;
using TransportPipes.TestsCommon.Database;

namespace TransportPipes.Actionizer.Tests.Database;

public static class DbSetupHelper
{
    /// <summary>
    /// Creates a factory for a new, empty test database (PostgreSQL in a container, see <see cref="PostgresTestDatabase"/>)
    /// </summary>
    public static TransportPipesDbFactory CreateTestDbFactory()
    {
        return new TransportPipesDbFactory(PostgresTestDatabase.CreateDatabase());
    }

    public static List<DbUser> AddDefaultUsers(TransportPipesDbContext db)
    {
        var adminuser = new DbUser() { UserName = "admin", NormalizedUserName = "admin", PasswordHash = "test", Email = "admin@test.com" };
        var otheruser = new DbUser() { UserName = "otheruser", NormalizedUserName = "otheruser", PasswordHash = "othertest", Email = "other@test.com" };

        db.Insert(adminuser); //Inserting sets the Id of these users
        db.Insert(otheruser);

        return new List<DbUser>() { adminuser, otheruser };
    }

    public static DbGameObject GetDbGameObject(TransportPipesDbContext db, int x, int y)
    {
        var foundGameObject = db.Select<DbGameObject>(t => t.X == x && t.Y == y).Single();
        return foundGameObject;
    }

    public static List<DbGameObject> GetAllDbGameObjects(TransportPipesDbContext db)
    {
        var foundGameObjects = db.GameObjects.OrderBy(t => t.Id).ToList();
        return foundGameObjects;
    }

    /// <summary>
    /// All provider -> consumer pairs (a plain join on the ConnectionId, including pairs where the provider is the consumer)
    /// </summary>
    public static List<ConsumerProviderConnection> SelectAllConsumerProviderConnections(TransportPipesDbContext db)
    {
        var query = from cons in db.ConsumerConnections
                    join prov in db.ProviderConnections on cons.ConnectionId equals prov.ConnectionId
                    orderby cons.Id, prov.Id
                    select new ConsumerProviderConnection
                    {
                        ConnectionId = cons.ConnectionId,
                        SourceId = prov.SourceId,
                        DestinationId = cons.DestinationId,
                        GlobalConnectionId = cons.GlobalConnectionId
                    };
        return query.ToList();
    }

    public static int GetMaxConnectionId(TransportPipesDbContext db)
    {
        return Math.Max(
            db.ConsumerConnections.Max(t => (int?)t.ConnectionId) ?? 0,
            db.ProviderConnections.Max(t => (int?)t.ConnectionId) ?? 0);
    }

    public static int GetMaxGlobalConnectionIdOfConnections(TransportPipesDbContext db)
    {
        return Math.Max(
            db.ConsumerConnections.Max(t => (int?)t.GlobalConnectionId) ?? 0,
            db.ProviderConnections.Max(t => (int?)t.GlobalConnectionId) ?? 0);
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
    public static TotalCalculationObject StartCalculatingThisMap(string str, int xOfGlobalConnection, int yOfGlobalConnection, out List<DbGameObject> allGameObjectsInThisGlobalConnection, int ticks = 1)
    {
        //Setup Database
        var transportFactory = CreateTestDbFactory();
        try
        {
            using var transportDb = transportFactory.CreateDatabaseWithOpenConnection();

            var users = AddDefaultUsers(transportDb.DbContext);

            //Add GameObjects
            AddDbGameObjectsByStringDefinition(transportDb.DbContext, str, users.First().Id);

            //Determine Connections
            DetermineAllConnections(transportDb.DbContext, transportDb, new DateTime(2015, 1, 1));

            //Find GameObjects in this GlobalConnection
            DbGameObject theCurrentDbGameObject = transportDb.DbContext.Single<DbGameObject>(t => t.X == xOfGlobalConnection && t.Y == yOfGlobalConnection);
            allGameObjectsInThisGlobalConnection = transportDb.DbContext.GameObjects.Where(t => t.GlobalConnectionId == theCurrentDbGameObject.GlobalConnectionId).OrderBy(t => t.Id).ToList();

            //Find ConsumerProviders in this GlobalConnection
            var consumerProviders = transportDb.ObtainListOfConsumerProviderConnections(theCurrentDbGameObject.GlobalConnectionId);

            var pacRepository = new ProvidersAndConsumersRepository(allGameObjectsInThisGlobalConnection, consumerProviders);
            var resourceCalculator = new ResourceCalculator(pacRepository);

            //Execute the calculation
            var totalCalculation = new TotalCalculationObject();
            resourceCalculator.ExecuteCompleteCalculation(theCurrentDbGameObject, ticks, totalCalculation);

            //Return the calculation
            return totalCalculation;
        }
        finally
        {
            PostgresTestDatabase.DropDatabase(transportFactory.ConnectionString);
        }
    }

    public static void DetermineAllConnections(TransportPipesDbContext db, ITransportDatabase transportDb, DateTime timeOfCreation)
    {
        var connectionFinder = new ConnectionFinder(transportDb);

        List<DbGameObject> todo = db.GameObjects.Where(t => t.GlobalConnectionId == 0).OrderBy(t => t.Id).ToList();
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
    /// T: TradingBuilding
    /// Everything else is empty (e.g. Space or 0)
    /// </summary>
    public static void AddDbGameObjectsByStringDefinition(TransportPipesDbContext db, string str, long ownerId)
    {
        var splittedString = str.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

        var gameObjects = new List<DbGameObject>();
        for (int y = 0; y < splittedString.Length; y++)
        {
            var curSplit = splittedString[y];
            for (int x = 0; x < curSplit.Length; x++)
            {
                var curCharacter = char.ToUpperInvariant(curSplit[x]);
                GameObjectType gameObjectType = curCharacter switch
                {
                    'W' => GameObjectType.WindMoily,
                    'S' => GameObjectType.SolarPanel,
                    'M' => GameObjectType.IronMine,
                    '+' => GameObjectType.GreyPipe,
                    'O' => GameObjectType.Storage,
                    'F' => GameObjectType.FactorySmall,
                    'T' => GameObjectType.TradingBuilding,
                    _ => GameObjectType.None
                };

                if (gameObjectType != GameObjectType.None)
                {
                    gameObjects.Add(new DbGameObject(x, y, gameObjectType, ownerId));
                }
            }
        }

        //Insert them one by one so the Ids are in the same order as the map (like they always were)
        foreach (var gameObject in gameObjects)
        {
            AddDbGameObject(db, gameObject);
        }
    }

    public static void AddDbGameObject(TransportPipesDbContext db, DbGameObject newDbGameObject)
    {
        db.Insert(newDbGameObject);
    }

    public static void AddSimpleDbGameObject(TransportPipesDbContext db, GameObjectType gameObjectType, int x, int y, long ownerId)
    {
        var newDbGameObject = new DbGameObject(x, y, gameObjectType, ownerId);
        AddDbGameObject(db, newDbGameObject);
    }

    public static void RemoveDbGameObject(TransportPipesDbContext db, int x, int y)
    {
        db.GameObjects.Where(t => t.X == x && t.Y == y).ExecuteDelete();
    }
}

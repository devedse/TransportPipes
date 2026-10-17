using System.Data;
using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using TransportPipes.Calculators.CalculationObjects;
using TransportPipes.Calculators.Calculators;
using TransportPipes.Common;
using TransportPipes.Common.Enums;
using TransportPipes.Common.GameLogicObjects.GameObjectInfo;
using TransportPipes.Common.Generators;
using TransportPipes.Common.Helpers;
using TransportPipes.Common.Repositories;
using TransportPipes.Database;
using TransportPipes.Database.Entities;
using TransportPipes.Database.Entities.ActionDetails;
using TransportPipes.Database.Helpers;
using TransportPipes.Server.Dtos;

namespace TransportPipes.Server.Services;

/// <summary>
/// Everything the hub needs from the game and the database (this used to be the TransportPipesFrontEndDatabaseHelper)
/// </summary>
public class GameFrontEndService
{
    public const int MinimumUserNameLength = 4;

    private readonly TransportPipesDbFactory _transportPipesDbFactory;
    private readonly IPasswordHasher<DbUser> _passwordHasher;
    private readonly TransportPipesOptions _options;
    private readonly ILogger<GameFrontEndService> _logger;

    public GameFrontEndService(TransportPipesDbFactory transportPipesDbFactory, IPasswordHasher<DbUser> passwordHasher, IOptions<TransportPipesOptions> options, ILogger<GameFrontEndService> logger)
    {
        _transportPipesDbFactory = transportPipesDbFactory;
        _passwordHasher = passwordHasher;
        _options = options.Value;
        _logger = logger;
    }

    public DbUser? Login(string userName, string password)
    {
        using var transportDb = _transportPipesDbFactory.CreateDatabaseWithOpenConnection();

        var user = transportDb.GetUser(userName);
        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            return null;
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : user;
    }

    /// <returns>The created user, or the reason why the user could not be registered</returns>
    public (DbUser? User, string? Error) Register(string userName, string password, string email)
    {
        userName = userName.Trim();
        if (userName.Length < MinimumUserNameLength)
        {
            return (null, $"Error: username should be at least {MinimumUserNameLength} characters...");
        }

        if (string.IsNullOrEmpty(password))
        {
            return (null, "Error: please enter a password...");
        }

        using var transportDb = _transportPipesDbFactory.CreateDatabaseWithOpenConnection();

        if (transportDb.GetUser(userName) != null)
        {
            return (null, "Error: user with this name already exists...");
        }

        var newUser = new DbUser
        {
            UserName = userName,
            Email = email.Trim().ToLowerInvariant(),
            Gold = _options.StartingGold
        };
        newUser.PasswordHash = _passwordHasher.HashPassword(newUser, password);

        try
        {
            return (transportDb.AddUser(newUser), null);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            //Someone registered the same name at exactly the same moment
            return (null, "Error: user with this name already exists...");
        }
    }

    public IReadOnlyList<GameObjectInfo> GetGameObjectInfos()
    {
        var gameObjectInfos = GameResourcesRepository.PredeterminedGameObjectInfos.Values.Select(GameObjectInfo.From).ToList();

        //Pipes are free and built instantly, they have no entry in the game data but the build menu should show them
        var pipeInfo = GameResourcesRepository.DetermineGameObjectInfo(GameObjectType.GreyPipe);
        gameObjectInfos.Add(new GameObjectInfo((int)GameObjectType.GreyPipe, GameObjectType.GreyPipe.ToString(), pipeInfo.Price, [], [], pipeInfo.BuildTimeInSeconds));

        return gameObjectInfos;
    }

    /// <summary>
    /// Queues an action to build a GameObject
    /// </summary>
    /// <returns>null when the action was queued, otherwise a message for the user</returns>
    public string? SetDbGameObject(GameObjectType buildingType, DbUser currentTransportuser, int x, int y)
    {
        using var transportDb = _transportPipesDbFactory.CreateDatabaseWithOpenConnection();

        //RepeatableRead is a snapshot in PostgreSQL: the gold calculation should not be influenced by changes of the Actionizer.
        //The fix for building twice at exactly the same time with not enough gold is either claim that gold stuff and not
        //spend it or build some kind of retry mechanism.
        using var transaction = transportDb.OpenTransaction(IsolationLevel.RepeatableRead);
        try
        {
            var buildingInfo = GameResourcesRepository.DetermineGameObjectInfo(buildingType);
            var obtainedUser = transportDb.GetUser(currentTransportuser.UserName)
                ?? throw new InvalidOperationException($"User {currentTransportuser} does not exist anymore.");

            GetGoldForUserInternal(transportDb, obtainedUser, DateTime.UtcNow, out _, out var currentGold);

            if (currentGold > buildingInfo.Price)
            {
                var result = transportDb.AddDbAction(new DbAction()
                {
                    TransportAction = TransportAction.CreateBuilding,
                    DateToBeExecuted = DateTime.UtcNow.AddSeconds(buildingInfo.BuildTimeInSeconds),
                    ExecutedById = currentTransportuser.Id,
                    X = x,
                    Y = y,
                    ActionDetails = new ActionDetailsCreateBuilding()
                    {
                        GameObject = buildingType,
                        PriceOfBuilding = buildingInfo.Price
                    },
                    GoldClaimed = buildingInfo.Price
                });

                if (!result)
                {
                    transaction.Rollback();
                    return "Could not enqueue Action because there is already an action queued for this tile.";
                }

                transaction.Commit();
                return null;
            }

            return "You don't have enough money to build this...";
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            _logger.LogWarning(ex, "Serialization failure while building {BuildingType} at <{X},{Y}>", buildingType, x, y);
            return "Could not build this right now, please try again.";
        }
    }

    /// <returns>True if the action was queued, false if there already is an action queued for this tile</returns>
    public bool AddDbAction(DbAction dbAction)
    {
        //The database guarantees there is only one unexecuted action per tile
        using var transportDb = _transportPipesDbFactory.CreateDatabaseWithOpenConnection();
        return transportDb.AddDbAction(dbAction);
    }

    public List<DbClientUpdate> ObtainAndDeleteOpenClientUpdates()
    {
        using var transportDb = _transportPipesDbFactory.CreateDatabaseWithOpenConnection();
        return transportDb.ObtainAndDeleteOpenClientUpdates();
    }

    public ChunkData GetChunkData(int xChunk, int yChunk)
    {
        var terrain = TerrainGenerator.GenerateTerrain(xChunk, yChunk);
        GetGameObjectData(xChunk, yChunk, out var gameObjectsData, out var ownerData);
        return new ChunkData(xChunk, yChunk, ToJagged(terrain), ToJagged(gameObjectsData), ToJagged(ownerData));
    }

    public void GetGameObjectData(int xChunk, int yChunk, out string[,] gameObjectsData, out long[,] ownerData)
    {
        using var transportDb = _transportPipesDbFactory.CreateDatabaseWithOpenConnection();

        //Read the chunk, its neighbours (for the pipe shapes) and the actions from the same snapshot
        using var transaction = transportDb.OpenTransaction(IsolationLevel.RepeatableRead);

        var chunkCacheFinder = new ChunkCacheHelper(transportDb);
        var gameObjects = chunkCacheFinder.GetChunk(xChunk, yChunk);
        List<DbAction> actionsForChunk = transportDb.GetBuildActionsInChunk(xChunk, yChunk);

        gameObjectsData = new string[StaticConfigInfo.ChunkWidth, StaticConfigInfo.ChunkHeight];
        ownerData = new long[StaticConfigInfo.ChunkWidth, StaticConfigInfo.ChunkHeight];

        var chunkStart = GenericMathHelper.GetXYStartForChunkCoordinates(xChunk, yChunk);

        for (int y = 0; y < StaticConfigInfo.ChunkHeight; y++)
        {
            for (int x = 0; x < StaticConfigInfo.ChunkWidth; x++)
            {
                DbGameObject? gameObjectHere = gameObjects.Data[x, y];
                if (gameObjectHere == null)
                {
                    var actionHere = actionsForChunk.FirstOrDefault(t => t.X == chunkStart.X + x && t.Y == chunkStart.Y + y);
                    if (actionHere != null)
                    {
                        gameObjectsData[x, y] = "UnderConstruction";
                        ownerData[x, y] = actionHere.ExecutedById;
                    }
                    else
                    {
                        gameObjectsData[x, y] = "none";
                    }
                }
                else
                {
                    var objectName = gameObjectHere.GameObjectType.ToString();
                    if (gameObjectHere.GameObjectType == GameObjectType.GreyPipe)
                    {
                        objectName = DeterminePipeName(chunkCacheFinder, chunkStart.X + x, chunkStart.Y + y);
                    }

                    gameObjectsData[x, y] = objectName;
                    ownerData[x, y] = gameObjectHere.DbOwnerId;
                }
            }
        }
    }

    /// <summary>
    /// A pipe is drawn connected to every neighbouring GameObject: u(p), r(ight), d(own), l(eft)
    /// </summary>
    private static string DeterminePipeName(ChunkCacheHelper chunkCacheFinder, int x, int y)
    {
        var objectName = GameObjectType.GreyPipe.ToString();
        int countHori = 0;
        int countVerti = 0;

        if (chunkCacheFinder.GetDbGameObject(x, y - 1) != null)
        {
            objectName += "u";
            countVerti += 1;
        }
        if (chunkCacheFinder.GetDbGameObject(x + 1, y) != null)
        {
            objectName += "r";
            countHori += 1;
        }
        if (chunkCacheFinder.GetDbGameObject(x, y + 1) != null)
        {
            objectName += "d";
            countVerti += 1;
        }
        if (chunkCacheFinder.GetDbGameObject(x - 1, y) != null)
        {
            objectName += "l";
            countHori += 1;
        }

        if (objectName == "GreyPipe")
        {
            objectName = "GreyPipeurdl";
        }
        else if (countHori + countVerti < 2)
        {
            objectName = countHori > countVerti ? "GreyPiperl" : "GreyPipeud";
        }

        return objectName;
    }

    public GoldInfo GetGoldForUser(DbUser user)
    {
        using var transportDb = _transportPipesDbFactory.CreateDatabaseWithOpenConnection();

        //Using a snapshot since else the collection can get modified while querying
        using var transaction = transportDb.OpenTransaction(IsolationLevel.RepeatableRead);

        var obtainedUser = transportDb.GetUserById(user.Id)
            ?? throw new InvalidOperationException($"User {user} does not exist anymore.");

        GetGoldForUserInternal(transportDb, obtainedUser, DateTime.UtcNow, out var myGoldPerSecond, out var currentGold);
        return new GoldInfo(obtainedUser.UserName, currentGold, myGoldPerSecond);
    }

    private static void GetGoldForUserInternal(TransportDatabase transportDb, DbUser obtainedUser, DateTime now, out double myGoldPerSecond, out double currentGold)
    {
        var foundGlobalConnectionIds = transportDb.FindAllGlobalConnectionIdsThesePlayersAreInvolvedIn(new List<long>() { obtainedUser.Id });
        var calculationPackages = transportDb.FindAllCalculationsForTheseGlobalConnectionIds(foundGlobalConnectionIds);
        var goldClaimedForThisUser = transportDb.GetClaimedGoldForUser(obtainedUser.Id);

        var calculatingMoneyNow = new Dictionary<DbUser, double>();
        MoneyForAllPlayersDeterminer.AddMoneyForPlayersToDictionary(calculationPackages, calculatingMoneyNow, now);

        var calculatingMoneyPerSecond = new Dictionary<DbUser, double>();
        MoneyForAllPlayersDeterminer.AddMoneyForPlayersToDictionary(calculationPackages, calculatingMoneyPerSecond, null);

        calculatingMoneyNow.TryGetValue(obtainedUser, out var addedGoldSinceLastCalculationTime);
        calculatingMoneyPerSecond.TryGetValue(obtainedUser, out myGoldPerSecond);

        currentGold = obtainedUser.Gold + addedGoldSinceLastCalculationTime - goldClaimedForThisUser;
    }

    public string GetStatusOfGameObject(int x, int y)
    {
        using var transportDb = _transportPipesDbFactory.CreateDatabaseWithOpenConnection();

        //We need to use a snapshot here because we want to not be influenced by potentials changes of the backend during this calculation
        using var transaction = transportDb.OpenTransaction(IsolationLevel.RepeatableRead);

        var builder = new StringBuilder();
        var wTotal = Stopwatch.StartNew();
        var initialGameObject = transportDb.GetGameObject(x, y);

        if (initialGameObject != null)
        {
            var calculationPackage = transportDb.FindAllCalculationsForTheseGlobalConnectionIds(new List<int>() { initialGameObject.GlobalConnectionId }).SingleOrDefault();
            if (calculationPackage == null)
            {
                builder.AppendFormat("==== {0}: {1} ({2}) GlobConId: {3} ====", initialGameObject.Id, initialGameObject.GameObjectType, initialGameObject.DbOwnerId, initialGameObject.GlobalConnectionId);
                builder.AppendLine();
                builder.AppendLine("(No calculation found)");
            }
            else
            {
                var providersAndConsumersRepository = calculationPackage.ProvidersAndConsumersRepository;

                //Get the connected game object because else we can't use it when requesting destinations etc
                var connectedGameObject = providersAndConsumersRepository.GetConnectedGameObject(initialGameObject.X, initialGameObject.Y);

                if (connectedGameObject == null)
                {
                    builder.AppendFormat("==== {0}: {1} ({2}) GlobConId: {3} ===={4}", initialGameObject.Id, initialGameObject.GameObjectType, initialGameObject.DbOwnerId, initialGameObject.GlobalConnectionId, Environment.NewLine);
                    builder.AppendLine("(GameObject not in calculation, this happens when you obtain information about e.g. a pipe)");
                }
                else
                {
                    builder.AppendFormat("==== {0}: {1} ({2}) GlobConId: {3} ===={4}", connectedGameObject.Id, connectedGameObject.GameObjectType, connectedGameObject.DbOwnerId, connectedGameObject.GlobalConnectionId, Environment.NewLine);

                    var allDestinations = providersAndConsumersRepository.GetConsumersOfThisGameObject(connectedGameObject).Select(t => t.Id + ": " + t.GameObjectType.ToString());
                    var allSources = providersAndConsumersRepository.GetProvidersToThisGameObject(connectedGameObject).Select(t => t.Id + ": " + t.GameObjectType.ToString());

                    builder.AppendLine("This Game Object provides to:");
                    builder.AppendLine(string.Join(Environment.NewLine, allDestinations));
                    builder.AppendLine();
                    builder.AppendLine("This Game Object consumes from:");
                    builder.AppendLine(string.Join(Environment.NewLine, allSources));
                    builder.AppendLine();

                    var w = Stopwatch.StartNew();
                    var resourceCalculator = new ResourceCalculator(providersAndConsumersRepository);
                    var totalCalculation = new TotalCalculationObject();
                    resourceCalculator.ExecuteCompleteCalculation(connectedGameObject, 1, totalCalculation);
                    w.Stop();

                    var calculationsForThisGameObject = totalCalculation.GetCalculationForGameObject(connectedGameObject);
                    foreach (var provision in calculationsForThisGameObject.Provisions)
                    {
                        builder.AppendLine("Provision from: " + provision.Provisioner.GameObjectType.ToString() + " (" + provision.Provisioner.Id + "): " + provision.ToString());
                    }
                    foreach (var request in calculationsForThisGameObject.Requests)
                    {
                        builder.AppendLine("Request from: " + request.Requester.GameObjectType.ToString() + " (" + request.Requester.Id + "): " + request.ToString());
                    }

                    builder.AppendLine();
                    builder.AppendLine("Moneys Transfered for complete calculation:");
                    builder.AppendLine(string.Join(Environment.NewLine, totalCalculation.PlayersAndMoney.Select(t => t.Key.ToString() + ": " + t.Value.ToString() + " $")));
                    builder.AppendLine();

                    builder.AppendLine("Resources bought by this Game Object:");
                    builder.AppendLine(string.Join(Environment.NewLine, totalCalculation.GetBuysForGameObject(connectedGameObject)));
                    builder.AppendLine();

                    builder.AppendLine("Resources sold by this Game Object:");
                    builder.AppendLine(string.Join(Environment.NewLine, totalCalculation.GetSalesForGameObject(connectedGameObject)));
                    builder.AppendLine();

                    builder.AppendLine("Market has bought resources through this game object:");
                    builder.AppendLine(string.Join(Environment.NewLine, totalCalculation.GetBuysWithMarketForGameObject(connectedGameObject)));
                    builder.AppendLine();

                    builder.AppendLine("Resources sold to the market by this Game Object:");
                    builder.AppendLine(string.Join(Environment.NewLine, totalCalculation.GetSalesWithMarketForGameObject(connectedGameObject)));
                    builder.AppendLine();

                    builder.AppendLine("Time elapsed for resource calculations: " + w.Elapsed.TotalSeconds.ToString());
                    builder.AppendLine("Total time elapsed: " + wTotal.Elapsed.TotalSeconds.ToString());
                }
            }
        }

        //Let's not commit this transaction because we didn't change anything
        return builder.ToString();
    }

    /// <summary>
    /// System.Text.Json can't serialize multidimensional arrays, so [x, y] becomes [x][y]
    /// </summary>
    internal static T[][] ToJagged<T>(T[,] array)
    {
        var result = new T[array.GetLength(0)][];
        for (int x = 0; x < result.Length; x++)
        {
            result[x] = new T[array.GetLength(1)];
            for (int y = 0; y < result[x].Length; y++)
            {
                result[x][y] = array[x, y];
            }
        }
        return result;
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TransportPipesCalculators.CalculationObjects;
using TransportPipesCalculators.Calculators;
using TransportPipesCommon;
using TransportPipesCommon.Enums;
using TransportPipesCommon.Helpers;
using TransportPipesCommon.Repositories;
using TransportPipesDatabase;
using TransportPipesDatabase.Helpers;
using TransportPipesDatabase.Poco;
using TransportPipesDatabase.Poco.ActionDetails;

namespace TransportPipes.Backend.DatabaseNew
{
    public class TransportPipesFrontEndDatabaseHelper
    {
        private TransportPipesDbFactory _transportPipesDbFactory;

        public TransportPipesFrontEndDatabaseHelper(TransportPipesDbFactory transportPipesDbFactory)
        {
            _transportPipesDbFactory = transportPipesDbFactory;
        }

        public bool SetDbGameObject(GameObjectType buildingType, DbUser currentTransportuser, int x, int y, Action<string, int> logAction)
        {
            using (var transportDb = _transportPipesDbFactory.CreateDatabaseWithOpenConnection())
            {
                //TODO: This should actually be serializable but that one sometimes makes this transaction fail. I should really fix that the gold
                //is not deducted by the client but by the server instead.
                //Update: Snapshot is used here and is fine actually (we have to run GetGoldForUserInternal in snapshot), it can fail whenever the gold value is modified by another transaction. 
                //This could happend when a game object is build at exactly the same time.
                //The fix for this is either claim that gold stuff and not spend it or build some kind of retry mechanism
                
                //Maybe we need to do a simple server side lock around this
                //This will fix the stuff that could happen if this one is kicked of two times after eachother really fast (If that's even possible in SignalR???)
                //Well it theoretically is if you log in 2 times to 1 account.
                using (var transaction = transportDb.OpenTransaction(IsolationLevel.Snapshot))
                {
                    try
                    {
                        var buildingInfo = GameResourcesRepository.DetermineGameObjectInfo(buildingType);

                        var obtainedUser = transportDb.GetUser(currentTransportuser.UserName);

                        double currentGold, goldPerSecond;
                        GetGoldForUserInternal(transportDb, obtainedUser, out goldPerSecond, out currentGold);

                        if (currentGold > buildingInfo.Price)
                        {
                            var result = transportDb.AddDbAction(new DbAction()
                            {
                                TransportAction = TransportAction.CreateBuilding,
                                DateToBeExecuted = DateTime.Now.AddSeconds(buildingInfo.BuildTimeInSeconds),
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
                                logAction("Could not enqueue Action because there is already an action queued for this tile.", 3000);
                            }
                        }
                        else
                        {
                            logAction("You don't have enough money to build this...", 3000);
                        }

                        transaction.Commit();

                        return true;
                    }
                    catch (SqlException e)
                    {
                        Debug.WriteLine("Exception when setting game object: " + e);

                        return false;
                    }
                }
            }
        }

        public List<DbClientUpdate> ObtainAndDeleteOpenClientUpdates()
        {
            using (var transportDb = _transportPipesDbFactory.CreateDatabaseWithOpenConnection())
            {
                return transportDb.ObtainAndDeleteOpenClientUpdates();
            }
        }

        public bool AddDbAction(DbAction dbAction)
        {
            using (var transportDb = _transportPipesDbFactory.CreateDatabaseWithOpenConnection())
            {
                //We need to use the highest IsolationLevel because without it you can still get multiple Actions on a single tile
                using (var transaction = transportDb.OpenTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        var result = transportDb.AddDbAction(dbAction);

                        transaction.Commit();

                        return result;
                    }
                    catch (SqlException ex)
                    {
                        //This is normal when 2 clients add an action on the same tile at exactly the same moment. This can't happen so we have to rollback one of them.
                        Debug.WriteLine("Error while adding action, rolling back... Exception: " + ex);
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }

        public void GetGameObjectData(int xChunk, int yChunk, out string[,] gameObjectsData, out int[,] ownerData)
        {
            using (var transportDb = _transportPipesDbFactory.CreateDatabaseWithOpenConnection())
            {
                //TODO: Investigate why this transaction is even required. I somehow got a deadlock exception on some kind of system database :/
                //Basically on OrmLiteTransportDb.sys.query_notification_1502680451 index: cidx
                using (var transaction = transportDb.OpenTransaction(IsolationLevel.Snapshot))
                {
                    var chunkCacheFinder = new ChunkCacheHelper(transportDb);
                    var gameObjects = chunkCacheFinder.GetChunk(xChunk, yChunk);


                    List<DbAction> actionsForChunk = transportDb.GetBuildActionsInChunk(xChunk, yChunk);

                    gameObjectsData = new string[StaticConfigInfo.ChunkWidth, StaticConfigInfo.ChunkHeight];
                    ownerData = new int[StaticConfigInfo.ChunkWidth, StaticConfigInfo.ChunkHeight];
                    for (int y = 0; y < StaticConfigInfo.ChunkHeight; y++)
                    {
                        for (int x = 0; x < StaticConfigInfo.ChunkWidth; x++)
                        {
                            DbGameObject gameObjectHere = gameObjects.Data[x, y];
                            if (gameObjectHere == null)
                            {
                                //TODO: Update to dict or 2 way array
                                var posInChunk = GenericMathHelper.GetXYStartForChunkCoordinates(xChunk, yChunk);
                                var actionHere = actionsForChunk.FirstOrDefault(t => t.X == posInChunk.X + x && t.Y == posInChunk.Y + y);
                                if (actionHere != null)
                                {
                                    gameObjectsData[x, y] = "UnderConstruction";
                                    ownerData[x, y] = (int)actionHere.ExecutedById; //TODO Make this long instead of int
                                }
                                else
                                {
                                    gameObjectsData[x, y] = "none"; //This should be 0 later
                                }
                            }
                            else
                            {
                                var objectName = gameObjectHere.GameObjectType.ToString();
                                if (objectName == "GreyPipe")
                                {
                                    int xInChunk = xChunk * StaticConfigInfo.ChunkWidth + x;
                                    int yInChunk = yChunk * StaticConfigInfo.ChunkHeight + y;

                                    int countHori = 0;
                                    int countVerti = 0;
                                    if (chunkCacheFinder.GetDbGameObject(xInChunk, yInChunk - 1) != null)
                                    {
                                        objectName += "u";
                                        countVerti += 1;
                                    }
                                    if (chunkCacheFinder.GetDbGameObject(xInChunk + 1, yInChunk) != null)
                                    {
                                        objectName += "r";
                                        countHori += 1;
                                    }
                                    if (chunkCacheFinder.GetDbGameObject(xInChunk, yInChunk + 1) != null)
                                    {
                                        objectName += "d";
                                        countVerti += 1;
                                    }
                                    if (chunkCacheFinder.GetDbGameObject(xInChunk - 1, yInChunk) != null)
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
                                        if (countHori > countVerti)
                                        {
                                            objectName = "GreyPiperl";
                                        }
                                        else
                                        {
                                            objectName = "GreyPipeud";
                                        }
                                    }
                                }
                                gameObjectsData[x, y] = objectName;
                                ownerData[x, y] = (int)gameObjectHere.DbOwnerId; //TODO Make this long instead of int
                            }
                        }
                    }
                }
            }
        }


        public void GetGoldForUser(string userName, out double myGoldPerSecond, out double currentGold)
        {
            using (var transportDb = _transportPipesDbFactory.CreateDatabaseWithOpenConnection())
            {
                //Using SnapShot since else the collection can get modified while querying
                using (var transaction = transportDb.OpenTransaction(IsolationLevel.Snapshot))
                {
                    var obtainedUser = transportDb.GetUser(userName);
                    GetGoldForUserInternal(transportDb, obtainedUser, out myGoldPerSecond, out currentGold);
                }
            }
        }

        private void GetGoldForUserInternal(TransportPipesDatabaseOrmLite transportDb, DbUser obtainedUser, out double myGoldPerSecond, out double currentGold)
        {
            var foundGlobalConnectionIds = transportDb.FindAllGlobalConnectionIdsThesePlayersAreInvolvedIn(new List<long>() { obtainedUser.Id });
            var calculationPackages = transportDb.FindAllCalculationsForTheseGlobalConnectionIds(foundGlobalConnectionIds);
            var goldClaimedForThisUser = transportDb.GetClaimedGoldForUser(obtainedUser.Id);

            var calculatingMoneyNow = new Dictionary<DbUser, double>();
            MoneyForAllPlayersDeterminer.AddMoneyForPlayersToDictionary(calculationPackages, calculatingMoneyNow, DateTime.Now);
            var calculatingMoneyPerSecond = new Dictionary<DbUser, double>();
            MoneyForAllPlayersDeterminer.AddMoneyForPlayersToDictionary(calculationPackages, calculatingMoneyPerSecond, null);

            double addedGoldSinceLastCalculationTime = 0;
            var foundAddedGold = calculatingMoneyNow.TryGetValue(obtainedUser, out addedGoldSinceLastCalculationTime);

            myGoldPerSecond = 0;
            var foundMyGoldPerSecond = calculatingMoneyPerSecond.TryGetValue(obtainedUser, out myGoldPerSecond);

            currentGold = obtainedUser.Gold + addedGoldSinceLastCalculationTime - goldClaimedForThisUser;
        }


        public string GetStatusOfGameObject(int x, int y)
        {
            using (var transportDb = _transportPipesDbFactory.CreateDatabaseWithOpenConnection())
            {
                //We need to use Snapshot here because we want to not be influenced by potentials changes of the backend during this calculation
                using (var transaction = transportDb.OpenTransaction(IsolationLevel.Snapshot))
                {
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
                                builder.AppendLine("(GameObject not in calculation, this happens when you obtain informatino about e.g. a pipe)");
                            }
                            else
                            {
                                builder.AppendFormat("==== {0}: {1} ({2}) GlobConId: {3} ===={4}", connectedGameObject.Id, connectedGameObject.GameObjectType, connectedGameObject.DbOwnerId, connectedGameObject.GlobalConnectionId, Environment.NewLine);




                                var allDestinations = providersAndConsumersRepository.GetConsumersOfThisGameObject(connectedGameObject).Select(t => t.Id + ": " + t.GameObjectType.ToString());
                                var allSources = providersAndConsumersRepository.GetProvidersToThisGameObject(connectedGameObject).Select(t => t.Id + ": " + t.GameObjectType.ToString());

                                var destinationString = String.Join(Environment.NewLine, allDestinations);
                                builder.AppendLine("This Game Object provides to:");
                                builder.AppendLine(destinationString);
                                builder.AppendLine();

                                var sourceString = String.Join(Environment.NewLine, allSources);
                                builder.AppendLine("This Game Object consumes from:");
                                builder.AppendLine(sourceString);
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
                                var moneysTransferedString = String.Join(Environment.NewLine, totalCalculation.PlayersAndMoney.Select((t) => t.Key.ToString() + ": " + t.Value.ToString() + " $"));
                                builder.AppendLine(moneysTransferedString);
                                builder.AppendLine();

                                builder.AppendLine("Resources bought by this Game Object:");
                                var buysForGameObject = String.Join(Environment.NewLine, totalCalculation.GetBuysForGameObject(connectedGameObject));
                                builder.AppendLine(buysForGameObject);
                                builder.AppendLine();

                                builder.AppendLine("Resources sold by this Game Object:");
                                var salesForGameObject = String.Join(Environment.NewLine, totalCalculation.GetSalesForGameObject(connectedGameObject));
                                builder.AppendLine(salesForGameObject);
                                builder.AppendLine();

                                builder.AppendLine("Market has bought resources through this game object:");
                                var buysWithMarketForGameObject = String.Join(Environment.NewLine, totalCalculation.GetBuysWithMarketForGameObject(connectedGameObject));
                                builder.AppendLine(buysWithMarketForGameObject);
                                builder.AppendLine();

                                builder.AppendLine("Resources sold to the market by this Game Object:");
                                var salesWithMarketForGameObject = String.Join(Environment.NewLine, totalCalculation.GetSalesWithMarketForGameObject(connectedGameObject));
                                builder.AppendLine(salesWithMarketForGameObject);
                                builder.AppendLine();

                                builder.AppendLine("Time elapsed for resource calculations: " + w.Elapsed.TotalSeconds.ToString());
                                builder.AppendLine("Total time elapsed: " + wTotal.Elapsed.TotalSeconds.ToString());
                            }
                        }
                    }

                    //Let's not commit this transaction because we didn't change anything
                    return builder.ToString();
                }
            }
        }
    }
}

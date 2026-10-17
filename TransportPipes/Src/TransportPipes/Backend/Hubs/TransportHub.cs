using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TransportPipesCommon;
using TransportPipesDatabase;
using TransportPipesDatabase.Poco;
using TransportPipesCommon.Enums;
using TransportPipesDatabase.Poco.ActionDetails;
using TransportPipesCommon.Helpers;
using System.Diagnostics;
using TransportPipesCalculators.CalculationObjects;
using TransportPipesCalculators.Calculators;
using TransportPipesCommon.Generators;
using TransportPipesDatabase.Helpers;
using TransportPipesCommon.Repositories;
using TransportPipes.Backend.DatabaseNew;
using System.Data.SqlClient;
using TransportPipesDatabase.Poco.ClientUpdateDetails;

namespace TransportPipes.Backend.Hubs
{
    public class TransportHub : Hub
    {
        private static Dictionary<string, DbUser> _loggedInUsers = new Dictionary<string, DbUser>();

        private DbUser _currentTransportUser = null;
        private TransportPipesDbFactory _transportPipesDbFactory;
        private TransportPipesFrontEndDatabaseHelper _transportPipesFrontEndDatabaseHelper;

        private static bool SqlDependencySetup = false;
        private static object SqlDependencySetupLockject = new object();

        public TransportHub()
        {
            _transportPipesDbFactory = new TransportPipesDbFactory();
            _transportPipesFrontEndDatabaseHelper = new TransportPipesFrontEndDatabaseHelper(_transportPipesDbFactory);

            if (!SqlDependencySetup)
            {
                lock (SqlDependencySetupLockject)
                {
                    if (!SqlDependencySetup)
                    {
                        SqlDependencySetup = true;

                        var sqlDependencyCreator = new SqlDependencyCreatorDatabase(_transportPipesDbFactory);
                        sqlDependencyCreator.SetupSqlDependencyForRequiredClientUpdates(HandleUnhandledClientUpdates);
                    }
                }
            }
        }

        private void HandleUnhandledClientUpdates(object sender, SqlNotificationEventArgs eventArgs)
        {
            var dbClientUpdates = _transportPipesFrontEndDatabaseHelper.ObtainAndDeleteOpenClientUpdates();

            List<Point> chunksThatNeedToBeUpdated = new List<Point>();

            foreach (var clientUpdate in dbClientUpdates)
            {
                ClientUpdateDetailsIncomeChanged clientUpdateDetailsIncomeChanged;
                ClientUpdateDetailsTileNeedsRefresh clientUpdateDetailsTileNeedsRefresh;

                if ((clientUpdateDetailsTileNeedsRefresh = clientUpdate.ClientUpdateDetails as ClientUpdateDetailsTileNeedsRefresh) != null)
                {
                    var chunkXY = GenericMathHelper.GetChunkForPos(clientUpdateDetailsTileNeedsRefresh.X, clientUpdateDetailsTileNeedsRefresh.Y);
                    chunksThatNeedToBeUpdated.Add(chunkXY);

                    var posInChunk = GenericMathHelper.ConvertPosToPosInChunk(clientUpdateDetailsTileNeedsRefresh.X, clientUpdateDetailsTileNeedsRefresh.Y);

                    if (posInChunk.X == 0)
                    {
                        chunksThatNeedToBeUpdated.Add(new Point(chunkXY.X - 1, chunkXY.Y));
                    }
                    if (posInChunk.X == 7)
                    {
                        chunksThatNeedToBeUpdated.Add(new Point(chunkXY.X + 1, chunkXY.Y));
                    }
                    if (posInChunk.Y == 0)
                    {
                        chunksThatNeedToBeUpdated.Add(new Point(chunkXY.X, chunkXY.Y - 1));
                    }
                    if (posInChunk.Y == 7)
                    {
                        chunksThatNeedToBeUpdated.Add(new Point(chunkXY.X, chunkXY.Y + 1));
                    }
                }
                else if ((clientUpdateDetailsIncomeChanged = clientUpdate.ClientUpdateDetails as ClientUpdateDetailsIncomeChanged) != null)
                {
                    IHubContext context = GlobalHost.ConnectionManager.GetHubContext<TransportHub>();

                    if (clientUpdateDetailsIncomeChanged.DbUserIds != null)
                    {
                        foreach (var userIdToUpdate in clientUpdateDetailsIncomeChanged.DbUserIds)
                        {
                            var matchingKvp = _loggedInUsers.FirstOrDefault(t => t.Value.Id == userIdToUpdate);

                            //Ensure that the found item is not null (we have to check the key because the default of a KeyValuePair is not null
                            if (matchingKvp.Key != null)
                            {
                                var caller = context.Clients.Client(matchingKvp.Key);
                                CalculateIncomeForUser(caller, matchingKvp.Value);
                            }
                        }
                    }
                }
            }

            var toUpdate = chunksThatNeedToBeUpdated.Distinct();

            foreach (var chunkPos in toUpdate)
            {
                RequestChunkNow(chunkPos.X, chunkPos.Y, true);


                IHubContext context = GlobalHost.ConnectionManager.GetHubContext<TransportHub>();
                context.Clients.All.NotifyMessage2("Requested Chunk: X:" + chunkPos.X + " Y:" + chunkPos.Y, 3000);
            }
        }

        public void LogMeIn(string userName, string password)
        {
            using (var transportDatabase = _transportPipesDbFactory.CreateDatabaseWithOpenConnection())
            {
                var foundUserInDb = transportDatabase.GetUser(userName, password);

                if (foundUserInDb != null)
                {
                    var conId = this.Context.ConnectionId;
                    _loggedInUsers[conId] = foundUserInDb;
                    Clients.Caller.LoginResponse(true, "", foundUserInDb.UserName, foundUserInDb.Id);
                }
                else
                {
                    Clients.Caller.LoginResponse(false, "Error: invalid username or password...", "", 0);
                }
            }
        }


        public void RegisterMe(string userName, string password, string email)
        {
            using (var transportDatabase = _transportPipesDbFactory.CreateDatabaseWithOpenConnection())
            {
                if (userName.Length < 4)
                {
                    Clients.Caller.LoginResponse(false, "Error: username should be longer then 4 characters...", "", 0);
                }
                else if (transportDatabase.GetUser(userName) != null)
                {
                    Clients.Caller.LoginResponse(false, "Error: user with this name already exists...", "", 0);
                }
                else
                {
                    var createdUser = transportDatabase.RegisterUser(userName, password, email);
                    var conId = this.Context.ConnectionId;
                    _loggedInUsers[conId] = createdUser;
                    Clients.Caller.LoginResponse(true, "", createdUser.UserName, createdUser.Id);
                }
            }
        }

        public void ObtainGameObjectInfos()
        {
            var obtainedGameObjectInfos = GameResourcesRepository.PredeterminedGameObjectInfos.Values;
            Clients.Caller.UpdateGameObjectInfos(obtainedGameObjectInfos);
        }

        public void RequestChunk(int xChunk, int yChunk)
        {
            RequestChunkNow(xChunk, yChunk, false);
        }

        public void SetGameObject(int x, int y, int buildingType)
        {
            if (AmILoggedInAndSetUserIfTrue())
            {
                if (Enum.IsDefined(typeof(GameObjectType), buildingType))
                {
                    var buildingTypeEnum = (GameObjectType)buildingType;

                    _transportPipesFrontEndDatabaseHelper.SetDbGameObject(buildingTypeEnum, _currentTransportUser, x, y, (msg, duration) => Clients.Caller.NotifyMessage2(msg, duration));

                    CalculateMyIncome();

                    //Rerequest chunk because we have a new under construction action
                    var chunkToRequest = GenericMathHelper.GetChunkForPos(x, y);
                    RequestChunkNow(chunkToRequest.X, chunkToRequest.Y, true);
                }
            }
        }

        public void DeleteGameObject(int x, int y)
        {
            if (AmILoggedInAndSetUserIfTrue())
            {
                var result = _transportPipesFrontEndDatabaseHelper.AddDbAction(new DbAction()
                {
                    TransportAction = TransportAction.DeleteBuilding,
                    DateToBeExecuted = DateTime.Now,
                    ExecutedById = _currentTransportUser.Id,
                    X = x,
                    Y = y,
                    ActionDetails = new ActionDetailsDeleteBuilding()
                });


                if (!result)
                {
                    Clients.Caller.NotifyMessage2("Could not enqueue Action because there is already an action queued for this tile.", 3000);
                }
            }
        }


        public void GetStatusOfGameObject(int x, int y)
        {
            var stringResult = _transportPipesFrontEndDatabaseHelper.GetStatusOfGameObject(x, y);
            if (!string.IsNullOrWhiteSpace(stringResult))
            {
                Clients.Caller.NotifyMessage(stringResult);
            }
        }

        public void CalculateMyIncome()
        {
            if (AmILoggedInAndSetUserIfTrue())
            {
                CalculateIncomeForUser(Clients.Caller, _currentTransportUser);
            }
        }

        private void CalculateIncomeForUser(dynamic theSignalRCaller, DbUser transportUser)
        {
            double myGoldPerSecond, currentGold;
            _transportPipesFrontEndDatabaseHelper.GetGoldForUser(transportUser.UserName, out myGoldPerSecond, out currentGold);

            theSignalRCaller.UpdateGoldPerSecond(transportUser.UserName, currentGold, myGoldPerSecond);

            var builder = new StringBuilder();
            builder.AppendFormat("{0}, Gold: {1}, Gold per second: {2}{3}", transportUser.UserName, currentGold, myGoldPerSecond, Environment.NewLine);
            theSignalRCaller.NotifyMessage2(builder.ToString(), 5000);
        }



        private void RequestChunkNow(int xChunk, int yChunk, bool updateForAll)
        {
            var terrain = TerrainGenerator.GenerateTerrain(xChunk, yChunk);
            string[,] gameObjectsData;
            int[,] ownerData;
            _transportPipesFrontEndDatabaseHelper.GetGameObjectData(xChunk, yChunk, out gameObjectsData, out ownerData);

            if (updateForAll)
            {
                IHubContext context = GlobalHost.ConnectionManager.GetHubContext<TransportHub>();
                context.Clients.All.ServerHasSentBackChunkData(xChunk, yChunk, terrain, gameObjectsData, ownerData);
            }
            else
            {
                Clients.Caller.ServerHasSentBackChunkData(xChunk, yChunk, terrain, gameObjectsData, ownerData);
            }
        }

        private bool AmILoggedInAndSetUserIfTrue()
        {
            DbUser obtainedUser;
            if (_loggedInUsers.TryGetValue(Context.ConnectionId, out obtainedUser))
            {
                _currentTransportUser = obtainedUser;
                return true;
            }
            else
            {
                _currentTransportUser = null;
                return false;
            }
        }
    }
}

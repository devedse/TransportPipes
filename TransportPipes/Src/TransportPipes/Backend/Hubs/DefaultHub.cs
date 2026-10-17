using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Security;
using TransportPipes.Backend.Calculations;
using TransportPipes.Backend.Database;
using TransportPipes.Backend.Repositories;
using TransportPipes.Backend.UserManagement;
using TransportPipesCommon;
using TransportPipesCommon.Enums;
using TransportPipesCommon.Generators;
using TransportPipesCommon.Helpers;

namespace TransportPipes.Backend.Hubs
{
    public class DefaultHub : Hub
    {
        private UserRepository _userRepository;
        private ConnectedGameObjectsRepository _connectedGameObjectsRepository;
        private ResourcesAndGlobalConnectionsRepository _resourcesAndGlobalConnectionsRepository;

        private TransportUser _currentTransportUser = null;
        private String _connectionId;

        public DefaultHub()
        {
            _userRepository = StaticRepositoryProvider.UserRepository;
            _connectedGameObjectsRepository = StaticRepositoryProvider.ConnectedGameObjectsRepository;
            _resourcesAndGlobalConnectionsRepository = StaticRepositoryProvider.ResourcesAndGlobalConnectionsRepository;
        }

        public override System.Threading.Tasks.Task OnConnected()
        {
            Console.WriteLine("Connecting :)");
            return base.OnConnected();
        }

        private Boolean IsUserLoggedIn(SuperLocker superLocker)
        {
            _connectionId = this.Context.ConnectionId;

            superLocker.RequestReadLock();

            if (StaticUserManager.Users.ContainsKey(_connectionId))
            {
                _currentTransportUser = StaticUserManager.Users[_connectionId];
                return true;
            }
            return false;
        }

        public void LogMeIn(String userName, String password)
        {
            var superLock = new SuperLocker();
            try
            {
                //Write lock is required here because we wan't to make sure the user is not logging in on another thread while we start adding himhere
                superLock.RequestWriteLock();

                if (IsUserLoggedIn(superLock))
                {
                    //Quit if user is already logged in
                    return;
                }


                var foundUser = _userRepository.GetUser(userName, password, superLock);

                if (foundUser != null)
                {
                    this._currentTransportUser = new TransportUser(foundUser);
                    StaticUserManager.Users.Add(this.Context.ConnectionId, _currentTransportUser);
                    Clients.Caller.LoginResponse(true, "", foundUser.UserName, foundUser.ID);
                }
                else
                {
                    Clients.Caller.LoginResponse(false, "Error: invalid username or password...", "", 0);
                }

            }
            catch (Exception exceptionMessage)
            {
                Clients.Caller.NotifyMessage(exceptionMessage.ToString());
            }
            finally
            {
                Clients.Caller.NotifyMessage2(superLock.ReleaseLocks(), 3000);
            }
        }

        public void RegisterMe(String userName, String password, String email)
        {
            var superLock = new SuperLocker();
            try
            {
                //Write lock is required here because we wan't to make sure the user is not logging in on another thread while we start adding himhere
                superLock.RequestWriteLock();

                if (IsUserLoggedIn(superLock))
                {
                    //Quit if user is already logged in
                    return;
                }

                if (userName.Length < 4)
                {
                    Clients.Caller.LoginResponse(false, "Error: username should be longer then 4 characters...", "", 0);
                }
                else if (_userRepository.GetUser(userName, superLock) != null)
                {
                    Clients.Caller.LoginResponse(false, "Error: user with this name already exists...", "", 0);
                }
                else
                {
                    var newUser = _userRepository.RegisterUser(userName, password, email, superLock);
                    this._currentTransportUser = new TransportUser(newUser);
                    StaticUserManager.Users.Add(this.Context.ConnectionId, _currentTransportUser);
                    Clients.Caller.LoginResponse(true, "", newUser.UserName, newUser.ID);
                }


            }
            catch (Exception exceptionMessage)
            {
                Clients.Caller.NotifyMessage(exceptionMessage.ToString());
            }
            finally
            {
                Clients.Caller.NotifyMessage2(superLock.ReleaseLocks(), 3000);
            }
        }

        public void RequestChunk(int xChunk, int yChunk)
        {
            var superLock = new SuperLocker();
            try
            {
                //Call this function for some purposes I don't exactly remember (maybe creating the user or something)
                IsUserLoggedIn(superLock);



                RequestChunkNow(xChunk, yChunk, false, superLock);

            }
            catch (Exception exceptionMessage)
            {
                Clients.Caller.NotifyMessage(exceptionMessage.ToString());
            }
            finally
            {
                Clients.Caller.NotifyMessage2(superLock.ReleaseLocks(), 3000);
            }
        }

        private void RequestChunkNow(int xChunk, int yChunk, Boolean updateForAll, SuperLocker superLock)
        {
            int yMax = yChunk * StaticConfigInfo.ChunkHeight + StaticConfigInfo.ChunkHeight;
            int xMax = xChunk * StaticConfigInfo.ChunkWidth + StaticConfigInfo.ChunkWidth;


            var terrain = TerrainGenerator.GenerateTerrain(xChunk, yChunk);

            var gameObjects = _connectedGameObjectsRepository.GetChunk(xChunk, yChunk, superLock);

            String[,] gameObjectsData = new String[StaticConfigInfo.ChunkWidth, StaticConfigInfo.ChunkHeight];
            int[,] ownerData = new int[StaticConfigInfo.ChunkWidth, StaticConfigInfo.ChunkHeight];
            for (int y = 0; y < StaticConfigInfo.ChunkHeight; y++)
            {
                for (int x = 0; x < StaticConfigInfo.ChunkWidth; x++)
                {
                    var gameObjectHere = gameObjects.Data[x, y];
                    if (gameObjectHere == null)
                    {
                        gameObjectsData[x, y] = "none"; //This should be 0 later
                    }
                    else
                    {
                        var objectName = gameObjectHere.GameObjectType.ToString();
                        if (objectName == "GreyPipe")
                        {
                            objectName = "GreyPipeurdl";
                        }
                        gameObjectsData[x, y] = objectName;
                        ownerData[x, y] = gameObjectHere.DbGameObject.Owner.ID;
                    }
                }
            }

            //foreach (var gameObject in gameObjects)
            //{
            //    var posInChunk = GenericMathHelper.ConvertPosToPosInChunk(gameObject.X, gameObject.Y);

            //    gameObjectsData[posInChunk.X, posInChunk.Y] = gameObject.GameObjectType.ToString(); //This should be to int later
            //}

            if (updateForAll)
            {
                Clients.All.ServerHasSentBackChunkData(xChunk, yChunk, terrain, gameObjectsData, ownerData);
            }
            else
            {
                Clients.Caller.ServerHasSentBackChunkData(xChunk, yChunk, terrain, gameObjectsData, ownerData);
            }
        }

        public void SetGameObject(int x, int y, int buildingType)
        {
            var superLock = new SuperLocker();

            try
            {
                if (!IsUserLoggedIn(superLock))
                {
                    //Quit if user is not logged in
                    return;
                }

                if (Enum.IsDefined(typeof(GameObjectType), buildingType))
                {
                    var affectedUsers = _resourcesAndGlobalConnectionsRepository.GetAffectedUsersBasedOnAdd(x, y, superLock);
                    CalculateIncomeFor(superLock, DateTime.Now, affectedUsers); //Calculate income before we add this thing

                    _connectedGameObjectsRepository.SetGameObject(x, y, (GameObjectType)buildingType, _currentTransportUser.DbUser, superLock);

                    var chunkPos = GenericMathHelper.GetChunkForPos(x, y);
                    RequestChunkNow(chunkPos.X, chunkPos.Y, true, superLock);

                    //Affected users could now change, for example if we have a lonely WindMill and then we connect it it will get a GlobalConnection
                    //which it didn't have previously
                    var affectedUsersAfterAdd = _resourcesAndGlobalConnectionsRepository.GetAffectedUsersBasedOnAdd(x, y, superLock);
                    CalculateIncomeFor(superLock, DateTime.Now, affectedUsersAfterAdd); //Calculate income after we add this thing
                }
            }
            catch (Exception exceptionMessage)
            {
                Clients.Caller.NotifyMessage(exceptionMessage.ToString());
            }
            finally
            {
                Clients.Caller.NotifyMessage2(superLock.ReleaseLocks(), 3000);
            }
        }

        public void DeleteGameObject(int x, int y)
        {
            var superLock = new SuperLocker();

            try
            {
                if (!IsUserLoggedIn(superLock))
                {
                    //Quit if user is not logged in
                    return;
                }


                var affectedUsers = _resourcesAndGlobalConnectionsRepository.GetAffectedUsersBasedOnDeletion(x, y, superLock);
                CalculateIncomeFor(superLock, DateTime.Now, affectedUsers); //Calculate income before we delete this thing

                _connectedGameObjectsRepository.DeleteGameObject(x, y, superLock);
                var chunkPos = GenericMathHelper.GetChunkForPos(x, y);
                RequestChunkNow(chunkPos.X, chunkPos.Y, true, superLock);

                //We should use the same affected users because the object is deleted and will still impact the same global connection
                CalculateIncomeFor(superLock, DateTime.Now, affectedUsers); //Calculate income after we delete this thing
            }
            catch (Exception exceptionMessage)
            {
                Clients.Caller.NotifyMessage(exceptionMessage.ToString());
            }
            finally
            {
                Clients.Caller.NotifyMessage2(superLock.ReleaseLocks(), 3000);
            }
        }

        public void GetStatusOfGameObject(int x, int y)
        {
            var superLock = new SuperLocker();


            try
            {
                if (!IsUserLoggedIn(superLock))
                {
                    //Quit if user is not logged in
                    return;
                }

                var foundGameObject = _connectedGameObjectsRepository.GetGameObject(x, y, superLock);
                superLock.DowngradeToReadLock(); //Downgrade to read lock because we don't do any saving currently

                if (foundGameObject != null)
                {
                    var builder = new StringBuilder();

                    builder.AppendLine("==== " + foundGameObject.DbGameObject.ID + ": " + foundGameObject.GameObjectType.ToString() + " (" + foundGameObject.DbGameObject.Owner.ToString() + ")====");

                    var allDestinations = foundGameObject.AllDestinations.Select(t => t.DbGameObject.ID + ": " + t.GameObjectType.ToString());
                    var allSources = foundGameObject.AllSources.Select(t => t.DbGameObject.ID + ": " + t.GameObjectType.ToString());

                    var destinationString = String.Join(Environment.NewLine, allDestinations);
                    builder.AppendLine("This Game Object provides to:");
                    builder.AppendLine(destinationString);
                    builder.AppendLine();

                    var sourceString = String.Join(Environment.NewLine, allSources);
                    builder.AppendLine("This Game Object consumes from:");
                    builder.AppendLine(sourceString);
                    builder.AppendLine();



                    var totalCalculation = new TotalCalculationObject();

                    //Hacky code for testing
                    //var storagesConnectedToThisThing = StaticRepositoryProvider.TransportDb.GameObjects.Where(t => t.GlobalConnectionId == foundGameObject.DbGameObject.GlobalConnectionId && t.GameObjectType == GameObjectType.Storage).ToList();

                    //var requestEnergy = new BackendResourceInfo(BackendResourceType.Energy, 8000);
                    //var listOfResourcesRequested = new List<BackendResourceInfo>() { requestEnergy };
                    //var completeRequest = new RequestInfo(, listOfResourcesRequested, new List<BackendGameObject>() { storagesConnectedToThisThing });

                    //var firstStorage = storagesConnectedToThisThing.First();
                    //firstStorage.Request

                    var w = Stopwatch.StartNew();

                    foundGameObject.SendOutMyRequestsIfNeeded(1, totalCalculation);

                    w.Stop();

                    var calculationsForThisGameObject = totalCalculation.GetCalculationForGameObject(foundGameObject);

                    foreach (var provision in calculationsForThisGameObject.Provisions)
                    {
                        builder.AppendLine("Provision from: " + provision.Provisioner.GameObjectType.ToString() + " (" + provision.Provisioner.DbGameObject.ID + "): " + provision.ToString());
                    }

                    foreach (var request in calculationsForThisGameObject.Requests)
                    {
                        builder.AppendLine("Request from: " + request.Requester.GameObjectType.ToString() + " (" + request.Requester.DbGameObject.ID + "): " + request.ToString());
                    }

                    builder.AppendLine();

                    builder.AppendLine("Moneys Transfered for complete calculation:");
                    var moneysTransferedString = String.Join(Environment.NewLine, totalCalculation.PlayersAndMoney.Select((t) => t.Key.ToString() + ": " + t.Value.ToString() + " $"));
                    builder.AppendLine(moneysTransferedString);
                    builder.AppendLine();

                    builder.AppendLine("Resources bought by this Game Object:");
                    var buysForGameObject = String.Join(Environment.NewLine, totalCalculation.GetBuysForGameObject(foundGameObject));
                    builder.AppendLine(buysForGameObject);
                    builder.AppendLine();

                    builder.AppendLine("Resources sold by this Game Object:");
                    var salesForGameObject = String.Join(Environment.NewLine, totalCalculation.GetSalesForGameObject(foundGameObject));
                    builder.AppendLine(salesForGameObject);
                    builder.AppendLine();

                    builder.AppendLine("Market has bought resources through this game object:");
                    var buysWithMarketForGameObject = String.Join(Environment.NewLine, totalCalculation.GetBuysWithMarketForGameObject(foundGameObject));
                    builder.AppendLine(buysWithMarketForGameObject);
                    builder.AppendLine();

                    builder.AppendLine("Resources sold to the market by this Game Object:");
                    var salesWithMarketForGameObject = String.Join(Environment.NewLine, totalCalculation.GetSalesWithMarketForGameObject(foundGameObject));
                    builder.AppendLine(salesWithMarketForGameObject);
                    builder.AppendLine();

                    builder.AppendLine("Time elapsed for resource calculations: " + w.Elapsed.TotalSeconds.ToString());

                    Clients.Caller.NotifyMessage(builder.ToString());
                }
            }
            catch (Exception exceptionMessage)
            {
                Clients.Caller.NotifyMessage(exceptionMessage.ToString());
            }
            finally
            {
                Clients.Caller.NotifyMessage2(superLock.ReleaseLocks(), 3000);
            }

        }

        private void CalculateIncomeFor(SuperLocker superLock, DateTime currentDateTime, List<DbUser> users)
        {
            superLock.RequestWriteLock();

            var thatThingITypedRight = _resourcesAndGlobalConnectionsRepository.DetermineGlobalConnectionsGameObjectsForTheseUsers(users, superLock);

            var dictOfPlayersMoneyPerSecond = _resourcesAndGlobalConnectionsRepository.UpdateGoldGlobalConnectionsForGameObjectsAndDetermineMoneyPerSecond(currentDateTime, thatThingITypedRight, superLock);

            //No need to save database because we do this in UpdateGoldForGlobalConnectionOfThisGameObject

            var builder = new StringBuilder();

            if (users.Count == 1 && users.FirstOrDefault() == _currentTransportUser.DbUser)
            {
                //Since we have only 1 user that is the current transport user we can send him the update only
                //This is required because if we use the method in else it will send a calculation that's based
                //on all buildings from the users in the Users list, so there could be globalconnections missing
                //that are from other users, even though they have also been calculated partially.
                //
                //That means to fix this we need to somehow only send the update in the else to the players in
                //the users list
                double myGoldPerSecond = 0;
                var foundMyGoldPerSecond = dictOfPlayersMoneyPerSecond.TryGetValue(_currentTransportUser.DbUser, out myGoldPerSecond);

                Clients.Caller.UpdateGoldPerSecond(_currentTransportUser.DbUser.UserName, _currentTransportUser.DbUser.Gold, myGoldPerSecond);
                builder.AppendLine(_currentTransportUser.DbUser.UserName + ", Gold: " + _currentTransportUser.DbUser.Gold + ", Gold per second: " + myGoldPerSecond);

                Clients.Caller.NotifyMessage2(builder.ToString(), 5000);
            }
            else
            {
                foreach (var user in users)
                {
                    double myGoldPerSecond = 0;
                    var foundMyGoldPerSecond = dictOfPlayersMoneyPerSecond.TryGetValue(user, out myGoldPerSecond);

                    Clients.All.UpdateGoldPerSecond(user.UserName, user.Gold, myGoldPerSecond);
                    builder.AppendLine(user.UserName + ", Gold: " + user.Gold + ", Gold per second: " + myGoldPerSecond);
                }

                Clients.All.NotifyMessage2(builder.ToString(), 5000);
            }
        }

        public void CalculateMyIncome()
        {
            var superLock = new SuperLocker();

            try
            {
                if (!IsUserLoggedIn(superLock))
                {
                    //Quit if user is not logged in
                    return;
                }

                CalculateIncomeFor(superLock, DateTime.Now, new List<DbUser>() { _currentTransportUser.DbUser });

            }
            catch (Exception exceptionMessage)
            {
                Clients.Caller.NotifyMessage(exceptionMessage.ToString());
            }
            finally
            {
                Clients.Caller.NotifyMessage2(superLock.ReleaseLocks(), 3000);
            }
        }
    }
}
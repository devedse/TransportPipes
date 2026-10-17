using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TransportPipesCalculators.CalculationObjects;
using TransportPipesCalculators.Calculators;
using TransportPipesCalculators.Helpers;
using TransportPipesCommon.GameLogicObjects.GameObjectInfo;
using TransportPipesDatabase;
using TransportPipesDatabase.Poco;
using TransportPipesDatabase.Repositories;

namespace TransportPipesActionizerTests.Database.Helpers
{
    public class DbTestCalculationHelper : IDisposable
    {
        private TransportPipesDatabaseOrmLite _transportDb;
        public ITransportDatabase TransportDb { get { return _transportDb; } }

        public bool VerifyCalculationsUsingAllGameObjectsAsStartingPoint { get; set; }

        private List<DbUser> _dbUsers = new List<DbUser>();
        public IEnumerable<DbUser> DbUsers { get { return _dbUsers; } }

        public DbTestCalculationHelper(bool verifyCalculationsUsingAllGameObjectsAsStartingPoints = true, bool realDb = false)
        {
            if (realDb)
            {
                var transportPipesDbFactory = new TransportPipesDbFactory(@"Data Source=.;database=OrmLiteTransportDbTest;Integrated Security=True");
                _transportDb = transportPipesDbFactory.CreateDatabaseWithOpenConnection();
                _transportDb.DropDb();
            }
            else
            {
                var transportPipesDbFactory = new TransportPipesDbFactory(DbSetupHelper.CreateInMemoryConnectionFactory());
                _transportDb = transportPipesDbFactory.CreateDatabaseWithOpenConnection();
            }
            _transportDb.CreateDbIfNotExists();

            AddUsers(10);

            VerifyCalculationsUsingAllGameObjectsAsStartingPoint = verifyCalculationsUsingAllGameObjectsAsStartingPoints;
        }

        public DbUser GetUserById(long id)
        {
            return DbUsers.Single(t => t.Id == id);
        }

        private void AddUsers(int count)
        {
            int iStart = _dbUsers.Count + 1;

            for (int i = iStart; i < iStart + count; i++)
            {
                var newUser = new DbUser() { UserName = "user" + i, Password = "a", Email = "testuser@test.com" };
                newUser.Id = _transportDb.DbConnection.Insert(newUser, selectIdentity: true);
                _dbUsers.Add(newUser);
            }
        }

        public void AddMapStringForUser(long ownerId, string mapString)
        {
            DbSetupHelper.AddDbGameObjectsByStringDefinition(_transportDb.DbConnection, mapString, ownerId);
        }
        public void DetermineAllConnections(DateTime? timeOfCreation = null)
        {
            if (timeOfCreation == null)
            {
                timeOfCreation = new DateTime(2015, 1, 1, 0, 0, 0);
            }

            DbSetupHelper.DetermineAllConnections(_transportDb.DbConnection, _transportDb, timeOfCreation.Value);
        }

        public List<int> GetAllGlobalConnectionIds()
        {
            return _transportDb.DbConnection.Select<int>(_transportDb.DbConnection.From<DbGameObject>().SelectDistinct(t => t.GlobalConnectionId));
        }

        public List<CalculationPackage> CreateCalculationPackages()
        {
            var globalConnectionIds = GetAllGlobalConnectionIds();
            var foundCalculationPackages = _transportDb.FindAllCalculationsForTheseGlobalConnectionIds(globalConnectionIds);
            return foundCalculationPackages;
        }

        public List<TotalCalculationObject> CreateTotalCalculations()
        {
            var calculationPackages = CreateCalculationPackages();

            var totalCalculations = CreateTotalCalculations(calculationPackages);

            return totalCalculations;
        }

        public List<TotalCalculationObject> CreateTotalCalculations(List<CalculationPackage> calculationPackages)
        {
            var totalCalculations = new List<TotalCalculationObject>();

            foreach (var calculationPackage in calculationPackages)
            {
                var totalCalculation = MoneyForAllPlayersDeterminer.CreateCalculationForCalculationPackage(calculationPackage, 1);
                totalCalculations.Add(totalCalculation);

                if (VerifyCalculationsUsingAllGameObjectsAsStartingPoint)
                {
                    var allGameObjects = calculationPackage.ProvidersAndConsumersRepository.ConnectedGameObjects;
                    var allCalculationsToVerify = new List<TotalCalculationObject>();
                    foreach (var gameObject in allGameObjects)
                    {
                        Trace.WriteLine(string.Format("Creating additional calculation with '{0}' as StartGameObject...", gameObject));
                        var calculationToVerify = MoneyForAllPlayersDeterminer.CreateCalculationForCalculationPackage(calculationPackage, 1, gameObject);
                        allCalculationsToVerify.Add(calculationToVerify);
                    }
                    VerifyIfCalculationsAreEqual(totalCalculation, allCalculationsToVerify);
                }
            }

            return totalCalculations;
        }

        public Dictionary<DbUser, double> CreateMoneyForUsers(List<TotalCalculationObject> totalCalculations)
        {
            var dictWithMoneyForUsers = new Dictionary<DbUser, double>();

            foreach (var totalCalculation in totalCalculations)
            {
                foreach (var kvpMoney in totalCalculation.PlayersAndMoney)
                {
                    MoneyInDictionaryHelper.AddPlayerMoney(dictWithMoneyForUsers, kvpMoney.Key, kvpMoney.Value);
                }
            }

            return dictWithMoneyForUsers;
        }

        public Dictionary<DbUser, double> CreateMoneyForUsers()
        {
            var totalCalculations = CreateTotalCalculations();

            var dictWithMoneyForUsers = CreateMoneyForUsers(totalCalculations);

            return dictWithMoneyForUsers;
        }

        private void VerifyIfCalculationsAreEqual(TotalCalculationObject baseCalculation, List<TotalCalculationObject> otherCalculations)
        {
            Trace.WriteLine(string.Format("Verifying {0} calculations to be correct based on other start objects...", otherCalculations.Count));
            foreach (var otherCalculation in otherCalculations)
            {
                var result = VerifyIfCalculationsAreEqual(baseCalculation, otherCalculation);

                if (!result)
                {
                    throw new InvalidOperationException("BaseCalculation is not equal to OtherCalculation.");
                }
            }
        }

        private bool VerifyIfCalculationsAreEqual(TotalCalculationObject baseCalculation, TotalCalculationObject otherCalculation)
        {
            if (baseCalculation.PlayersAndMoney.Count != otherCalculation.PlayersAndMoney.Count)
            {
                return false;
            }

            foreach (var key in baseCalculation.PlayersAndMoney.Keys)
            {
                //We can reuse the DbUsers because first of all they should be equal and second of all we overridden the equals method
                if (baseCalculation.PlayersAndMoney[key] != otherCalculation.PlayersAndMoney[key])
                {
                    return false;
                }
            }

            if (!ListOfDbGameObjectsAreEqual(baseCalculation.GameObjects.ToList(), otherCalculation.GameObjects.ToList()))
            {
                return false;
            }

            foreach (var gameObject in baseCalculation.GameObjects)
            {
                var baseCalcGameObject = baseCalculation.GetCalculationForGameObject(gameObject);
                var otherCalcGameObject = otherCalculation.GetCalculationForGameObject(gameObject);

                if (baseCalcGameObject.DestinationsHandled != otherCalcGameObject.DestinationsHandled)
                {
                    return false;
                }

                //Doesn't matter
                //if (baseCalcGameObject.IHaveRequestedToSendOutRequestsForMyParents != otherCalcGameObject.IHaveRequestedToSendOutRequestsForMyParents)
                //{
                //    return false;
                //}

                if (baseCalcGameObject.IHaveSendedOutMyRequests != otherCalcGameObject.IHaveSendedOutMyRequests)
                {
                    return false;
                }

                if (baseCalcGameObject.SourcesHandled != otherCalcGameObject.SourcesHandled)
                {
                    return false;
                }

                if (!BackendResourceListsAreEqual(baseCalcGameObject.Storage, otherCalcGameObject.Storage))
                {
                    return false;
                }

                if (!ListOfRequestsAreEqual(baseCalcGameObject.Requests, otherCalcGameObject.Requests))
                {
                    return false;
                }

                if (!ListOfProvisionsAreEqual(baseCalcGameObject.Provisions, otherCalcGameObject.Provisions))
                {
                    return false;
                }

                if (!ListOfTransactionsAreEqual(baseCalculation.GetBuysForGameObject(gameObject), otherCalculation.GetBuysForGameObject(gameObject)))
                {
                    return false;
                }

                if (!ListOfTransactionsAreEqual(baseCalculation.GetSalesForGameObject(gameObject), otherCalculation.GetSalesForGameObject(gameObject)))
                {
                    return false;
                }

                if (!ListOfTransactionsAreEqual(baseCalculation.GetBuysWithMarketForGameObject(gameObject), otherCalculation.GetBuysWithMarketForGameObject(gameObject)))
                {
                    return false;
                }

                if (!ListOfTransactionsAreEqual(baseCalculation.GetSalesWithMarketForGameObject(gameObject), otherCalculation.GetSalesWithMarketForGameObject(gameObject)))
                {
                    return false;
                }
            }

            return true;
        }

        public bool ListOfRequestsAreEqual(List<RequestInfo> baseRequests, List<RequestInfo> otherRequests)
        {
            if (baseRequests.Count != otherRequests.Count)
            {
                return false;
            }

            //TODO: Actually unit test this
            var allAreEqual = baseRequests.All(t =>
                baseRequests.Count(z => z.Requester == t.Requester && BackendResourceListsAreEqual(z.ResourcesRequested, t.ResourcesRequested) && ListOfDbGameObjectsAreEqual(z.OtherProviders, t.OtherProviders)) ==
                otherRequests.Count(z => z.Requester == t.Requester && BackendResourceListsAreEqual(z.ResourcesRequested, t.ResourcesRequested) && ListOfDbGameObjectsAreEqual(z.OtherProviders, t.OtherProviders)));
            return allAreEqual;
        }

        public bool ListOfProvisionsAreEqual(List<ProvisionInfo> baseProvisions, List<ProvisionInfo> otherProvisions)
        {
            if (baseProvisions.Count != otherProvisions.Count)
            {
                return false;
            }

            //TODO: Actually unit test this
            var allAreEqual = baseProvisions.All(t =>
                baseProvisions.Count(z => z.Provisioner == t.Provisioner && BackendResourceListsAreEqual(z.ResourcesProvided, t.ResourcesProvided) && ListOfTransactionsAreEqual(z.Transactions, t.Transactions)) ==
                otherProvisions.Count(z => z.Provisioner == t.Provisioner && BackendResourceListsAreEqual(z.ResourcesProvided, t.ResourcesProvided) && ListOfTransactionsAreEqual(z.Transactions, t.Transactions)));
            return allAreEqual;
        }

        public bool ListOfDbGameObjectsAreEqual(List<DbGameObject> baseDbGameObjects, List<DbGameObject> otherDbGameObjects)
        {
            if (baseDbGameObjects.Count != otherDbGameObjects.Count)
            {
                return false;
            }

            //TODO: Actually unit test this
            var allAreEqual = baseDbGameObjects.All(t =>
                baseDbGameObjects.Count(z => z == t) ==
                otherDbGameObjects.Count(z => z == t));
            return allAreEqual;
        }

        public bool ListOfTransactionsAreEqual(List<TransactionInfo> baseTransactions, List<TransactionInfo> otherTransactions)
        {
            if (baseTransactions.Count != otherTransactions.Count)
            {
                return false;
            }

            //TODO Actually unit test this
            var allAreEqual = baseTransactions.All(t =>
                baseTransactions.Count(z => z.Buyer == t.Buyer && z.Seller == t.Seller && z.ResourceInfo.ResourceType == t.ResourceInfo.ResourceType && z.ResourceInfo.Value == t.ResourceInfo.Value && z.TransactionType == t.TransactionType && z.ValueOfMaterials == t.ValueOfMaterials) ==
                otherTransactions.Count(z => z.Buyer == t.Buyer && z.Seller == t.Seller && z.ResourceInfo.ResourceType == t.ResourceInfo.ResourceType && z.ResourceInfo.Value == t.ResourceInfo.Value && z.TransactionType == t.TransactionType && z.ValueOfMaterials == t.ValueOfMaterials));
            return allAreEqual;
        }

        public bool BackendResourceListsAreEqual(List<BackendResourceInfo> baseList, List<BackendResourceInfo> otherList)
        {
            if (baseList.Count != otherList.Count)
            {
                return false;
            }

            //TODO: Actually unit test this
            var allAreEqual = baseList.All(t =>
                baseList.Count(z => z.ResourceType == t.ResourceType && z.Value == t.Value) ==
                otherList.Count(z => z.ResourceType == t.ResourceType && z.Value == t.Value));
            return allAreEqual;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    _transportDb.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~DbTestCalculationHelper() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}

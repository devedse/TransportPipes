using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransportPipesDatabase.Poco;
using TransportPipesDatabase.Poco.CombinedPoco;
using TransportPipesDatabase.Poco.ExtraObjects;
using TransportPipesDatabase.Repositories;

namespace TransportPipesDatabase
{
    public interface ITransportDatabase : IDisposable
    {
        IDbConnection DbConnection { get; }

        /// <summary>
        /// Gets a chunk
        /// </summary>
        /// <param name="x">X of the Chunk</param>
        /// <param name="y">Y of the Chunk</param>
        /// <returns>The Chunk</returns>
        Chunk GetChunk(int x, int y);

        /// <summary>
        /// Clears the specific connection of every game object provided
        /// E.g. if you have:
        /// M --- F --- O
        /// And you provide M it will only clear the M --- F connection
        /// </summary>
        /// <param name="gameObjects">A list of GameObjects that have to be cleared</param>
        void ClearConnections(List<DbGameObject> gameObjects);

        /// <summary>
        /// Clears the complete connection of every game object provided
        /// E.g. if you have:
        /// M --- F --- O
        /// And you provide M it will clear the complete M --- F --- O connection
        /// </summary>
        /// <param name="gameObjects">A list of GameObjects that have to be cleared</param>
        void ClearCompleteGlobalConnections(List<DbGameObject> gameObjects);

        /// <summary>
        /// Adds a connection to the database
        /// </summary>
        /// <param name="connections">A list of all Connections that have to be added</param>
        /// <param name="timeOfCreation">The time these connections were created</param>
        void AddConnections(List<DbGameObjectsConnection> connections, DateTime timeOfCreation);

        /// <summary>
        /// Returns a list of all ConsumerProviderConnections that match this GlobalConnectionId
        /// </summary>
        /// <param name="globalConnectionId">The Id of the GlobalConnection</param>
        /// <returns>A list of all ConsumerProviderConnections</returns>
        List<ConsumerProviderConnection> ObtainListOfConsumerProviderConnections(int globalConnectionId);

        /// <summary>
        /// Obtains a list of all actions that have to be executed
        /// </summary>
        /// <param name="beforeThisTime">The time to obtain actions before off</param>
        /// <returns>A list of all actions that have to be executed</returns>
        List<DbAction> ObtainUnexecutedActions(DateTime beforeThisTime);

        /// <summary>
        /// Sets the provided actions to completed in the database
        /// </summary>
        void CompleteActions(List<DbAction> actionsToBeCompleted);

        /// <summary>
        /// Gets a GameObject
        /// </summary>
        /// <param name="x">The X Coordinate</param>
        /// <param name="y">The Y Coordinate</param>
        /// <returns>The found DbGameObject at this location</returns>
        DbGameObject GetGameObject(int x, int y);

        /// <summary>
        /// Adds a GameObject
        /// </summary>
        /// <param name="gameObject">The GameObject</param>
        void AddGameObject(DbGameObject gameObject);

        /// <summary>
        /// Deletes a building at a specific location
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        void DeleteBuilding(int x, int y);

        /// <summary>
        /// Obtains a all Calculation Packages for a list of Global Connections
        /// </summary>
        /// <param name="globalConnectionIds">A list of GlobalConnection Ids</param>
        /// <returns>A list of Calculation Packages that can be used to calculate all resource flows and costs</returns>
        List<CalculationPackage> FindAllCalculationsForTheseGlobalConnectionIds(List<int> globalConnectionIds);

        /// <summary>
        /// Changes the gold fo a user by a certain amount
        /// </summary>
        /// <param name="userId">id of the user</param>
        /// <param name="amount">Amount to change the gold with</param>
        void ChangeGoldOfPlayer(long userId, double amount);

        /// <summary>
        /// Adds this gold to all users in the Database
        /// </summary>
        /// <param name="moneyForPlayers">A Dictionary where the Key is the user and the value how much money should be added or deducted</param>
        void UpdateGoldForUsers(Dictionary<DbUser, double> moneyForPlayers);

        /// <summary>
        /// Sets the last executed date for the provided GlobalConnections to the provided date
        /// </summary>
        /// <param name="globalConnectionIds">A list of GlobalConnectionIds to set the executed calculation time for</param>
        /// <param name="executedCalculationDateTime">The executed calculation time</param>
        void SetLastCalculationTimeForGlobalConnections(List<int> globalConnectionIds, DateTime executedCalculationDateTime);

        /// <summary>
        /// Returns all gold that's claimed for this user and not processed yet. (E.g. actions that are being build)
        /// </summary>
        /// <param name="userId">Id of the user</param>
        /// <returns>Gold open</returns>
        double GetClaimedGoldForUser(long userId);

        /// <summary>
        /// Adds a new ClientUpdate which the client will execute
        /// </summary>
        /// <param name="clientUpdate">The ClientUpdate to add</param>
        void AddClientUpdate(DbClientUpdate clientUpdate);

        /// <summary>
        /// Opens a transaction. Put a using around this to do things with it.
        /// </summary>
        /// <param name="isolationLevel">The Isolation level of the transaction</param>
        /// <returns>The transaction</returns>
        IDbTransaction OpenTransaction(IsolationLevel isolationLevel);
    }
}

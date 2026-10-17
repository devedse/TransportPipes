//using System;
//using System.Collections.Generic;
//using System.Data.SqlClient;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using TransportPipesDatabase.Poco;
//using TransportPipesDatabase.Poco.ExtraObjects;
//using TransportPipesDatabase.Repositories;

//namespace TransportPipesDatabase
//{
//    public interface ITransportDatabaseFrontEnd
//    {
//        /// <summary>
//        /// Obtains a list client updates and deletes them
//        /// </summary>
//        /// <returns>The list of client updates</returns>
//        List<DbClientUpdate> ObtainAndDeleteOpenClientUpdates();

//        /// <summary>
//        /// Sets up an SqlDependency that listens to any new undisplayed events for clients
//        /// </summary>
//        /// <param name="listener">The Callback to kick off</param>
//        void SetupSqlDependencyForRequiredClientUpdates(Action<object, SqlNotificationEventArgs> listener);

//        /// <summary>
//        /// Gets a chunk
//        /// </summary>
//        /// <param name="x">X of the Chunk</param>
//        /// <param name="y">Y of the Chunk</param>
//        /// <returns>The Chunk</returns>
//        Chunk GetChunk(int x, int y);

//        /// <summary>
//        /// Adds a DbAction to the Database
//        /// </summary>
//        /// <param name="action">The Action</param>
//        /// <returns>Whether the action was added</returns>
//        bool AddDbAction(DbAction action);

//        /// <summary>
//        /// Registers a new user in the database
//        /// </summary>
//        /// <param name="userName">The UserName</param>
//        /// <param name="password">The Password</param>
//        /// <param name="email">The Email</param>
//        /// <returns></returns>
//        DbUser RegisterUser(string userName, string password, string email);

//        /// <summary>
//        /// Obtains a user from the database based on their username and password. If username or password doesn't match, returns null.
//        /// </summary>
//        /// <param name="userName">The UserName</param>
//        /// <param name="password">The Password</param>
//        /// <returns>The DbUser from the Database</returns>
//        DbUser GetUser(string userName, string password);

//        /// <summary>
//        /// Obtains a user from the database based on their username. If username doesn't match, returns null.
//        /// </summary>
//        /// <param name="userName">The UserName</param>
//        /// <returns>The DbUser from the Database</returns>
//        DbUser GetUser(string userName);

//        /// <summary>
//        /// Gets a GameObject
//        /// </summary>
//        /// <param name="x">The X Coordinate</param>
//        /// <param name="y">The Y Coordinate</param>
//        /// <returns>The found DbGameObject at this location</returns>
//        DbGameObject GetGameObject(int x, int y);

//        /// <summary>
//        /// Returns a list of all GlobalConnectionIds these players are involved in
//        /// </summary>
//        /// <param name="playerIds">A list of the Ids of the players to search for</param>
//        /// <returns>A list of GlobalConnectionIds</returns>
//        List<int> FindAllGlobalConnectionIdsThesePlayersAreInvolvedIn(List<long> playerIds);

//        /// <summary>
//        /// Obtains a all Calculation Packages for a list of Global Connections
//        /// </summary>
//        /// <param name="globalConnectionIds">A list of GlobalConnection Ids</param>
//        /// <returns>A list of Calculation Packages that can be used to calculate all resource flows and costs</returns>
//        List<CalculationPackage> FindAllCalculationsForTheseGlobalConnectionIds(List<int> globalConnectionIds);

//        /// <summary>
//        /// Changes the gold of a player by a specific amount
//        /// </summary>
//        /// <param name="userName">The username</param>
//        /// <param name="amount">The amount of money to increase (or decrease) the gold with</param>
//        void ChangeGoldOfPlayer(string userName, int amount);

//        /// <summary>
//        /// Returns the build actions happening in this chunk
//        /// </summary>
//        /// <param name="xChunk">Chunk X</param>
//        /// <param name="yChunk">Chunk Y</param>
//        /// <returns></returns>
//        List<DbAction> GetBuildActionsInChunk(int xChunk, int yChunk);
//    }
//}

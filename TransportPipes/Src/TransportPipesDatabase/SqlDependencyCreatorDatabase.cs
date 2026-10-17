using ServiceStack.OrmLite;
using System;
using System.Data.SqlClient;

namespace TransportPipesDatabase
{
    public class SqlDependencyCreatorDatabase
    {
        private TransportPipesDbFactory _dbFactory;

        public SqlDependencyCreatorDatabase(TransportPipesDbFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public void SetupSqlDependencyForRequiredClientUpdates(Action<object, SqlNotificationEventArgs> listener)
        {
            SqlDependency.Start(_dbFactory.ConnectionString);

            SetupSqlDependencyForRequiredClientUpdatesReset(listener);
        }


        public void SetupSqlDependencyForRequiredClientUpdatesReset(Action<object, SqlNotificationEventArgs> listener)
        {
            using (var transportDb = _dbFactory.CreateDatabaseWithOpenConnection())
            {
                //If a new item has been added with Executed kick of the SQL Dependency
                //var query = db.From<DbAction>().Where(t => !t.IsDisplayed);
                //var queryAsString = query.ToSelectStatement();

                var queryAsString = "SELECT Id FROM dbo.DbClientUpdate";

                var ormLiteConnection = (OrmLiteConnection)transportDb.DbConnection;
                var sqlConnection = ormLiteConnection.ToDbConnection() as SqlConnection;


                using (var command = new SqlCommand(queryAsString, sqlConnection))
                {
                    // Create a dependency and associate it with the SqlCommand.
                    var dependency = new SqlDependency(command);
                    // Maintain the refence in a class member.

                    // Subscribe to the SqlDependency event.
                    dependency.OnChange += (sender, e) => SqlDependencyForRequiredClientUpdates_OnChange(sender, e, listener);

                    // Execute the command.
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        // Process the DataReader.
                    }
                }
            }
        }

        private void SqlDependencyForRequiredClientUpdates_OnChange(object sender, SqlNotificationEventArgs e, Action<object, SqlNotificationEventArgs> listener)
        {
            if (e.Type == SqlNotificationType.Subscribe || e.Info == SqlNotificationInfo.Error)
            {
                Console.WriteLine("Kapot: " + e);
            }
            else
            {
                SetupSqlDependencyForRequiredClientUpdatesReset(listener);
                listener(sender, e);
            }
        }


    }
}

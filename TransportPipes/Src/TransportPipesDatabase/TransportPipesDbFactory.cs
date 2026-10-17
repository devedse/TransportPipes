using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransportPipesDatabase.Helpers;

namespace TransportPipesDatabase
{
    public class TransportPipesDbFactory
    {
        private readonly OrmLiteConnectionFactory _connectionFactory;

        public string ConnectionString { get { return _connectionFactory.ConnectionString; } }

        public TransportPipesDbFactory(string connectionString)
            : this(new OrmLiteConnectionFactory(connectionString, SqlServer2012Dialect.Provider))
        {
        }

        public TransportPipesDbFactory()
            : this(new OrmLiteConnectionFactory($"Data Source=.;database={DbConstants.DbName};Integrated Security=True", SqlServer2012Dialect.Provider))
        {
        }

        public TransportPipesDbFactory(OrmLiteConnectionFactory connectionFactory)
        {
            connectionFactory.DialectProvider.StringSerializer = new TypedJsvStringSerializer();
            _connectionFactory = connectionFactory;
        }

        public TransportPipesDatabaseOrmLite CreateDatabaseWithOpenConnection()
        {
            var dbConnection = _connectionFactory.OpenDbConnection();
            var db = new TransportPipesDatabaseOrmLite(dbConnection);
            return db;
        }
    }
}

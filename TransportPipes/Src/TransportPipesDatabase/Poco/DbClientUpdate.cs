using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransportPipesDatabase.Poco.ClientUpdateDetails;

namespace TransportPipesDatabase.Poco
{
    public class DbClientUpdate
    {
        [AutoIncrement]
        public long Id { get; set; }

        [Reference]
        public DbUser ExecutedBy { get; set; }

        [References(typeof(DbUser))]
        public long? ExecutedById { get; set; }

        public ClientUpdateDetailsBase ClientUpdateDetails { get; set; }
    }
}

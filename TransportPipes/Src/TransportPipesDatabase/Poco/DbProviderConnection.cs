using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportPipesDatabase.Poco
{
    public class DbProviderConnection
    {
        [AutoIncrement]
        public long Id { get; set; }

        [Index]
        public int ConnectionId { get; set; }
        public int GlobalConnectionId { get; set; }

        [Reference]
        public DbGameObject Source { get; set; }
        [References(typeof(DbGameObject))]
        public long SourceId { get; set; }
    }
}

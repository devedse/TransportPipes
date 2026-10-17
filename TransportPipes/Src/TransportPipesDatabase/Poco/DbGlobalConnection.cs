using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportPipesDatabase.Poco
{
    public class DbGlobalConnection
    {
        [AutoIncrement]
        public long Id { get; set; }

        public int GlobalConnectionId { get; set; }

        public DateTime? LastCalculated { get; set; }
    }
}

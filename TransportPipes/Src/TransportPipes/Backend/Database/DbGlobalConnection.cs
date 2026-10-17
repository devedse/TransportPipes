using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TransportPipes.Backend.Database
{
    public class DbGlobalConnection
    {
        public virtual int ID { get; set; }
        public virtual DateTime? LastCalculationUpdate { get; set; }
        public virtual ICollection<DbGameObject> DbGameObjects { get; set; }
    }
}
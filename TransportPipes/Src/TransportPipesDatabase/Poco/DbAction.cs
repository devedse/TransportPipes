using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransportPipesCommon.Enums;
using TransportPipesDatabase.Poco.ActionDetails;

namespace TransportPipesDatabase.Poco
{
    public class DbAction
    {
        [AutoIncrement]
        public long Id { get; set; }
        public TransportAction TransportAction { get; set; }

        [Index]
        public int X { get; set; }
        [Index]
        public int Y { get; set; }

        [Reference]
        public DbUser ExecutedBy { get; set; }

        [References(typeof(DbUser))]
        public long ExecutedById { get; set; }

        public ActionDetailsBase ActionDetails { get; set; }

        [Index]
        public bool Executed { get; set; }

        public DateTime DateToBeExecuted { get; set; }

        public double GoldClaimed { get; set; }
    }
}

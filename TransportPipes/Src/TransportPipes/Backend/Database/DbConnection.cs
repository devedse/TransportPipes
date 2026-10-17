using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace TransportPipes.Backend.Database
{
    public class DbConnection
    {
        public virtual int ID { get; set; }

        public virtual DbGlobalConnection GlobalConnection { get; set; }

        [InverseProperty("Sources")]
        public virtual ICollection<DbGameObject> Sources { get; set; }

        [InverseProperty("Destinations")]
        public virtual ICollection<DbGameObject> Destinations { get; set; }
    }
}
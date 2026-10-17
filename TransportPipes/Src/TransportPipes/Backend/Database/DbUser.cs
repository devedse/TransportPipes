using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TransportPipes.Backend.Database
{
    public class DbUser
    {
        public virtual int ID { get; set; }

        public virtual String UserName { get; set; }

        public virtual String Password { get; set; }

        public virtual String Email { get; set; }

        public double Gold { get; set; }

        public override string ToString()
        {
            return ID + ":" + UserName;
        }
    }
}
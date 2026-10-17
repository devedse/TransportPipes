using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TransportPipes.Backend.Database;

namespace TransportPipes.Backend.UserManagement
{
    public class TransportUser
    {
        public String Username { get; private set; }
        public DbUser DbUser { get; private set; }

        public TransportUser(DbUser dbUser)
        {
            this.DbUser = dbUser;
            this.Username = dbUser.UserName;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TransportPipes.Backend.UserManagement
{
    public class StaticUserManager
    {
        public static Dictionary<String, TransportUser> Users = new Dictionary<String, TransportUser>();
    }
}
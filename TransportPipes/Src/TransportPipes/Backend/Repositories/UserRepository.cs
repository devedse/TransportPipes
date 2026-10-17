using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TransportPipes.Backend.Database;

namespace TransportPipes.Backend.Repositories
{
    public class UserRepository
    {
        private TransportDb _transportDb;

        public UserRepository(TransportDb transportDb)
        {
            this._transportDb = transportDb;
        }

        public DbUser GetUser(String username, String password, SuperLocker superLock)
        {
            superLock.RequestWriteLock(); //All database actions need the write lock

            var foundUser = _transportDb.Users.FirstOrDefault(t => t.UserName.ToLower() == username.ToLower() && t.Password == password);
            return foundUser;
        }

        public DbUser GetUser(String username, SuperLocker superLock)
        {
            superLock.RequestWriteLock(); //All database actions need the write lock

            var foundUser = _transportDb.Users.FirstOrDefault(t => t.UserName.ToLower() == username.ToLower());
            return foundUser;
        }

        public DbUser RegisterUser(String username, String password, String email, SuperLocker superLock)
        {
            superLock.RequestWriteLock(); //All database actions need the write lock

            var newUser = new DbUser()
            {
                UserName = username,
                Password = password,
                Email = email
            };

            _transportDb.Users.Add(newUser);
            _transportDb.SaveChanges();

            return newUser;
        }


    }
}
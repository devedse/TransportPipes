using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace TransportPipes.Backend.Database
{
    public class TransportDb : DbContext
    {
        public DbSet<DbUser> Users { get; set; }
        public DbSet<DbGameObject> GameObjects { get; set; }
        public DbSet<DbConnection> Connections { get; set; }
        public DbSet<DbGlobalConnection> GlobalConnections { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
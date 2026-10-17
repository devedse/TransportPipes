using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransportPipesCommon.Enums;

namespace TransportPipesDatabase.Poco.CombinedPoco
{
    /// <summary>
    /// Combination between DbGameObject and its Owner, Currently not used since a LoadSelect was faster.
    /// </summary>
    public class DbGameObjectUserCombination
    {
        public long DbGameObjectId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public GameObjectType GameObjectType { get; set; }
        public int ChunkX { get; set; }
        public int ChunkY { get; set; }
        public int GlobalConnectionId { get; set; }
        public long? DbGlobalConnectionId { get; set; }
        public DbUser Owner { get; set; }
        public long DbOwnerId { get; set; }

        public long DbUserId { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public double Gold { get; set; }

        public DbGameObject ToDbGameObject()
        {
            return new DbGameObject
            {
                Id = DbGameObjectId,
                X = X,
                Y = Y,
                GameObjectType = GameObjectType,
                ChunkX = ChunkX,
                ChunkY = ChunkY,
                GlobalConnectionId = GlobalConnectionId,
                DbGlobalConnectionId = DbGlobalConnectionId,
                DbOwnerId = DbOwnerId,
                Owner = new DbUser
                {
                    Id = DbUserId,
                    UserName = UserName,
                    Password = Password,
                    Email = Email,
                    Gold = Gold
                }
            };
        }
    }
}

using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransportPipesCommon;
using TransportPipesCommon.Enums;

namespace TransportPipesDatabase.Poco
{
    [CompositeIndex("ChunkX", "ChunkY")]
    public class DbGameObject
    {
        [AutoIncrement]
        public long Id { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public GameObjectType GameObjectType { get; set; }
        public int ChunkX { get; set; }
        public int ChunkY { get; set; }
        [Index]
        public int GlobalConnectionId { get; set; }

        [Reference]
        public DbGlobalConnection DbGlobalConnection { get; set; }
        [References(typeof(DbGlobalConnection))]
        public long? DbGlobalConnectionId { get; set; }

        [Reference]
        public DbUser Owner { get; set; }

        [References(typeof(DbUser))]
        public long DbOwnerId { get; set; }

        public DbGameObject(int x, int y, GameObjectType gameObjectType, DbUser owner)
            : this(x, y, gameObjectType, owner.Id)
        {

        }

        public DbGameObject(int x, int y, GameObjectType gameObjectType, long ownerId)
        {
            this.X = x;
            this.Y = y;
            this.GameObjectType = gameObjectType;
            this.ChunkX = (int)Math.Floor(x / (float)StaticConfigInfo.ChunkWidth);
            this.ChunkY = (int)Math.Floor(y / (float)StaticConfigInfo.ChunkHeight);
            //this.Owner = owner;
            this.DbOwnerId = ownerId;
        }

        public DbGameObject()
        {

        }

        public override string ToString()
        {
            var ownerString = Owner != null ? Owner.ToString() : "Owner not loaded";
            return $"{Id} <{X},{Y}>: {GameObjectType} ({ownerString}<{DbOwnerId}>)";
        }
    }
}

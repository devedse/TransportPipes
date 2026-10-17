using TransportPipes.Common;
using TransportPipes.Common.Enums;

namespace TransportPipes.Database.Entities;

public class DbGameObject
{
    public long Id { get; set; }

    public int X { get; set; }
    public int Y { get; set; }

    public GameObjectType GameObjectType { get; set; }

    public int ChunkX { get; set; }
    public int ChunkY { get; set; }

    public int GlobalConnectionId { get; set; }

    public DbGlobalConnection? DbGlobalConnection { get; set; }
    public long? DbGlobalConnectionId { get; set; }

    public DbUser? Owner { get; set; }
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

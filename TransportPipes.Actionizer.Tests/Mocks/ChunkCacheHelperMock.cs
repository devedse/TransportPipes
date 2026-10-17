using TransportPipes.Common.Enums;
using TransportPipes.Database.Entities;
using TransportPipes.Database.Entities.ExtraObjects;
using TransportPipes.Database.Helpers;

namespace TransportPipes.Actionizer.Tests.Mocks;

public class ChunkCacheHelperMock : ChunkCacheHelperBase
{
    public List<DbGameObject> MockedDbGameObjects { get; set; }

    public ChunkCacheHelperMock()
    {
        MockedDbGameObjects = new List<DbGameObject>();
    }

    /// <summary>
    /// Enter a string for example:
    /// 00000000
    /// 00W00000
    /// 00+00000
    /// 00+00000
    /// 00+++O00
    /// 00000000
    ///
    /// Allowed values:
    /// W: WindMoily
    /// S: SolarPanel
    /// +: GreyPipe
    /// O: Storage
    /// M: IronMine
    /// F: FactorySmall
    /// Everything else is empty (e.g. Space or 0)
    /// </summary>
    /// <param name="str"></param>
    public void AddDbGameObjectsByStringDefinition(string str)
    {
        var splittedString = str.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

        for (int y = 0; y < splittedString.Length; y++)
        {
            var curSplit = splittedString[y];
            for (int x = 0; x < curSplit.Length; x++)
            {
                var curCharacter = curSplit.Substring(x, 1).ToUpperInvariant()[0]; //Get uppercase character
                GameObjectType gameObjectType = GameObjectType.None;
                switch (curCharacter)
                {
                    case 'W':
                        gameObjectType = GameObjectType.WindMoily;
                        break;
                    case 'S':
                        gameObjectType = GameObjectType.SolarPanel;
                        break;
                    case 'M':
                        gameObjectType = GameObjectType.IronMine;
                        break;
                    case '+':
                        gameObjectType = GameObjectType.GreyPipe;
                        break;
                    case 'O':
                        gameObjectType = GameObjectType.Storage;
                        break;
                    case 'F':
                        gameObjectType = GameObjectType.FactorySmall;
                        break;
                }

                if (gameObjectType != GameObjectType.None)
                {
                    AddMockedSimpleDbGameObject(gameObjectType, x, y);
                }
            }
        }
    }

    public void AddMockedDbGameObject(DbGameObject newDbGameObject)
    {
        MockedDbGameObjects.Add(newDbGameObject);
    }

    public void AddMockedSimpleDbGameObject(GameObjectType gameObjectType, int x, int y)
    {
        var newDbGameObject = new DbGameObject(x, y, gameObjectType, new DbUser());
        MockedDbGameObjects.Add(newDbGameObject);
    }

    public override DbGameObject? GetDbGameObject(int x, int y)
    {
        return MockedDbGameObjects.SingleOrDefault(t => t.X == x && t.Y == y);
    }

    public override Chunk GetChunk(int xChunk, int yChunk)
    {
        throw new NotImplementedException();
    }
}

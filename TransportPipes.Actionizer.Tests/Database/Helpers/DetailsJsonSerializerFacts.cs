using TransportPipes.Common.Enums;
using TransportPipes.Database.Entities.ActionDetails;
using TransportPipes.Database.Entities.ClientUpdateDetails;
using TransportPipes.Database.Helpers;

namespace TransportPipes.Actionizer.Tests.Database.Helpers.DetailsJsonSerializerFacts;

[TestClass]
public class SerializeAndDeserializeMethodsOfDetailsJsonSerializer
{
    [TestMethod]
    public void CorrectlySerializeAndDeserializeTypedObjects()
    {
        var listOfActionDetails = new List<ActionDetailsBase>();

        for (int i = 0; i < 10000; i++)
        {
            if (i % 2 == 0)
            {
                var act = new ActionDetailsCreateBuilding() { GameObject = i % 4 == 0 ? GameObjectType.TradingBuilding : GameObjectType.SolarPanel, PriceOfBuilding = i };
                listOfActionDetails.Add(act);
            }
            else
            {
                var act = new ActionDetailsResearch() { ResearchLevel = i * 20, NogWat = new DateTime(2015, 1, 1) };
                listOfActionDetails.Add(act);
            }
        }

        var results = listOfActionDetails.Select(DetailsJsonSerializer.Serialize).ToList();

        for (int i = 0; i < results.Count; i++)
        {
            var deserialized = DetailsJsonSerializer.DeserializeActionDetails(results[i]);

            if (i % 2 == 0)
            {
                var createBuilding = deserialized as ActionDetailsCreateBuilding;
                Assert.IsNotNull(createBuilding);
                Assert.AreEqual(i % 4 == 0 ? GameObjectType.TradingBuilding : GameObjectType.SolarPanel, createBuilding.GameObject);
                Assert.AreEqual(i, createBuilding.PriceOfBuilding);
            }
            else
            {
                var research = deserialized as ActionDetailsResearch;
                Assert.IsNotNull(research);
                Assert.AreEqual(i * 20, research.ResearchLevel);
                Assert.AreEqual(new DateTime(2015, 1, 1), research.NogWat);
            }
        }
    }

    [TestMethod]
    public void DeserializesWhenTheTypeDiscriminatorIsNotTheFirstProperty()
    {
        //PostgreSQL jsonb sorts the keys, so "$type" ends up after the shorter keys
        var deserialized = DetailsJsonSerializer.DeserializeClientUpdateDetails("{\"X\": 5, \"Y\": -3, \"$type\": \"TileNeedsRefresh\"}");

        var tileNeedsRefresh = deserialized as ClientUpdateDetailsTileNeedsRefresh;
        Assert.IsNotNull(tileNeedsRefresh);
        Assert.AreEqual(5, tileNeedsRefresh.X);
        Assert.AreEqual(-3, tileNeedsRefresh.Y);
    }

    [TestMethod]
    public void SerializesClientUpdateDetailsWithTheirType()
    {
        var json = DetailsJsonSerializer.Serialize(new ClientUpdateDetailsIncomeChanged() { DbUserIds = new List<long> { 1, 2, 3 } });

        var deserialized = DetailsJsonSerializer.DeserializeClientUpdateDetails(json) as ClientUpdateDetailsIncomeChanged;

        Assert.IsNotNull(deserialized);
        CollectionAssert.AreEqual(new List<long> { 1, 2, 3 }, deserialized.DbUserIds);
    }
}

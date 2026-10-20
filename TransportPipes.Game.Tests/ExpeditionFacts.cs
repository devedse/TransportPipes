using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

[TestClass]
public class ExpeditionFacts
{
    private static (TestGame Game, Company Company) WithOffice()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 20);
        game.World.LoadBuilding(new Building { Id = game.World.NextBuildingId, Type = "expeditionOffice", Owner = company.Id, X = game.Local(company, 28, 28).X, Y = game.Local(company, 28, 28).Y });
        return (game, company);
    }

    [TestMethod]
    public void AnExpeditionComesBackWithGoldAfterItsHours()
    {
        var (game, company) = WithOffice();
        StringAssert.Contains(game.World.StartExpedition(company.Id, 2).Error, "1, 4 or 12");
        var gold = company.Gold;

        Assert.IsTrue(game.World.StartExpedition(company.Id, 4).Success);
        Assert.AreEqual(gold - GameWorld.ExpeditionCost(company, 4), company.Gold);
        StringAssert.Contains(game.World.StartExpedition(company.Id, 1).Error, "out until");
        game.Pass(3 * 60);
        Assert.IsNotNull(company.Expedition, "still out");

        game.Pass(61);

        Assert.IsNull(company.Expedition);
        Assert.IsGreaterThan(gold, company.Gold, "always more back than it cost");
        StringAssert.StartsWith(company.ExpeditionLog[0], "Your 4 h expedition is back with");
    }

    [TestMethod]
    public void ExpeditionsNeedAnOffice()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 20);
        StringAssert.Contains(game.World.StartExpedition(company.Id, 1).Error, "Expedition Office");
    }

    [TestMethod]
    public void LongTripsFindRelicsAndAllRelicsGiveATitle()
    {
        var (game, company) = WithOffice();
        for (var i = 0; i < 120 && company.Relics.Count < game.World.Content.Relics.Count; i++)
        {
            Assert.IsTrue(game.World.StartExpedition(company.Id, 12).Success);
            game.Pass(12 * 60 + 1);
        }

        Assert.HasCount(game.World.Content.Relics.Count, company.Relics);
        Assert.IsTrue(company.Titles.Contains(GameWorld.RelicTitle));
        Assert.IsTrue(company.Crates.Count > 0 || game.World.Discoveries.Any(), "the other finds turn up too");
    }
}

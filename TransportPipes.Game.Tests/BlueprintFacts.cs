using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

[TestClass]
public class BlueprintFacts
{
    private static Company WithOffice(TestGame game, string name = "Test Co")
    {
        var company = game.NewCompany(name, level: GameWorld.BlueprintLevel);
        game.Map(company, game.Local(company, 28, 28), "B");
        return company;
    }

    [TestMethod]
    public void ABlueprintSavesARectangleAndRebuildsItAtTheNormalCost()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = WithOffice(game);
        game.Map(company, game.Local(company, 2, 2), "W+\n+S");

        Assert.IsTrue(game.World.SaveBlueprint(company.Id, "Power corner", game.Local(company, 2, 2), 2, 2).Success);
        var blueprint = game.World.Blueprints.Single();
        CollectionAssert.AreEquivalent(new[] { new BlueprintBuilding("windMoily", 0, 0), new BlueprintBuilding("solarPanel", 1, 1) }, blueprint.Buildings);
        Assert.HasCount(2, blueprint.Pipes);

        var spot = game.Spot(company, "windMoily");
        var gold = company.Gold;
        Assert.IsTrue(game.World.BuildBlueprint(company.Id, blueprint.Id, spot).Success);
        var moily = game.World.BuildingAt(spot)!;
        Assert.AreEqual("windMoily", moily.Type);
        Assert.IsNotNull(moily.ConstructionEndsAt, "built like any other building");
        Assert.IsLessThan(gold, company.Gold);
    }

    [TestMethod]
    public void SavingNeedsABlueprintOffice()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: GameWorld.BlueprintLevel);
        game.Map(company, game.Local(company, 2, 2), "W");

        StringAssert.Contains(game.World.SaveBlueprint(company.Id, "Moily", game.Local(company, 2, 2), 1, 1).Error, "Blueprint Office");
        game.Map(company, game.Local(company, 28, 28), "B");
        StringAssert.Contains(game.World.SaveBlueprint(company.Id, "Moily", game.Local(company, 2, 2), 40, 1).Error, "32");
        StringAssert.Contains(game.World.SaveBlueprint(company.Id, "Empty", game.Local(company, 5, 5), 1, 1).Error, "no buildings");
        Assert.IsTrue(game.World.SaveBlueprint(company.Id, "Moily", game.Local(company, 2, 2), 1, 1).Success);
    }

    [TestMethod]
    public void AComplexIsSavedAsItsMembers()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = WithOffice(game);
        var origin = game.Local(company, 2, 2);
        game.World.LoadBuilding(new Building { Id = game.World.NextBuildingId, Type = "windMoily", Owner = company.Id, X = origin.X, Y = origin.Y, Size = 2 });

        Assert.IsTrue(game.World.SaveBlueprint(company.Id, "Complex", origin, 2, 2).Success);

        Assert.HasCount(4, game.World.Blueprints.Single().Buildings);
    }

    [TestMethod]
    public void SharedBlueprintsReachTheCorporationOrEveryoneAndGetRatings()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var author = WithOffice(game, "Author");
        Assert.IsTrue(game.World.FoundCorporation(author.Id, "Pipe Guild", "PG", "#3399ff").Success);
        var member = game.NewCompany("Member", level: GameWorld.BlueprintLevel);
        Assert.IsTrue(game.World.ApplyToCorporation(member.Id, game.World.Corporations.Single().Id).Success);
        Assert.IsTrue(game.World.InviteToCorporation(author.Id, member.Id).Success);
        var stranger = game.NewCompany("Stranger", level: GameWorld.BlueprintLevel);
        game.Map(author, game.Local(author, 2, 2), "W");
        Assert.IsTrue(game.World.SaveBlueprint(author.Id, "Moily", game.Local(author, 2, 2), 1, 1).Success);
        var blueprint = game.World.Blueprints.Single();

        Assert.IsEmpty(game.World.BlueprintsFor(member));
        StringAssert.Contains(game.World.ShareBlueprint(member.Id, blueprint.Id, BlueprintSharing.Board).Error, "not your");
        Assert.IsTrue(game.World.ShareBlueprint(author.Id, blueprint.Id, BlueprintSharing.Corporation).Success);
        Assert.HasCount(1, game.World.BlueprintsFor(member));
        Assert.IsEmpty(game.World.BlueprintsFor(stranger));
        StringAssert.Contains(game.World.RateBlueprint(stranger.Id, blueprint.Id, 5).Error, "Unknown");

        Assert.IsTrue(game.World.ShareBlueprint(author.Id, blueprint.Id, BlueprintSharing.Board).Success);
        Assert.IsTrue(game.World.RateBlueprint(stranger.Id, blueprint.Id, 5).Success);
        Assert.IsTrue(game.World.RateBlueprint(member.Id, blueprint.Id, 2).Success);
        StringAssert.Contains(game.World.RateBlueprint(author.Id, blueprint.Id, 5).Error, "own");
        Assert.AreEqual(3.5, blueprint.Rating);

        Assert.IsTrue(game.World.DeleteBlueprint(author.Id, blueprint.Id).Success);
        Assert.IsEmpty(game.World.Blueprints);
    }
}

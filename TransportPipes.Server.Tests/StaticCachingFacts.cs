using Microsoft.AspNetCore.Http;
using TransportPipes.Server.Services;

namespace TransportPipes.Server.Tests;

[TestClass]
public class StaticCachingFacts
{
    private static string For(string path, string query = "") =>
        StaticCaching.For(new PathString(path), new DefaultHttpContext { Request = { QueryString = new QueryString(query) } }.Request.Query);

    [TestMethod]
    public void KeepsHashedFilesForGood()
    {
        Assert.AreEqual(StaticCaching.Forever, For("/assets/index-DhkaPCvr.js"));
        Assert.AreEqual(StaticCaching.Forever, For("/art/buildings/hq.webp", "?v=83d56bb9da"));
    }

    [TestMethod]
    public void RefreshesOtherPicturesInTheBackground()
    {
        Assert.AreEqual(StaticCaching.Pictures, For("/art/buildings/hq.webp"));
        Assert.AreEqual(StaticCaching.Pictures, For("/art/ui/logo.webp"));
        Assert.AreEqual(StaticCaching.Pictures, For("/images/CombinedGameObjects/combinedimage.png"));
    }

    [TestMethod]
    public void ChecksThePageAndTheManifestEveryTime()
    {
        Assert.AreEqual(StaticCaching.Check, For("/index.html"));
        Assert.AreEqual(StaticCaching.Check, For("/art/manifest.json"));
        Assert.AreEqual(StaticCaching.Check, For("/images/CombinedGameObjects/combinedjson.json"));
    }
}

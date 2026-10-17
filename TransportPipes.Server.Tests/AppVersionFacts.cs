using TransportPipes.Server.Dtos;
using TransportPipes.Server.Services;

namespace TransportPipes.Server.Tests;

[TestClass]
public class AppVersionFacts
{
    [TestMethod]
    public void SplitsTheVersionAndTheCommit()
    {
        Assert.AreEqual(new VersionInfo("1.0.5-beta", "0307205abc"), AppVersion.Parse("1.0.5-beta+0307205abc"));
    }

    [TestMethod]
    public void WorksWithoutACommit()
    {
        Assert.AreEqual(new VersionInfo("1.0.5", null), AppVersion.Parse("1.0.5"));
        Assert.AreEqual(new VersionInfo("1.0.5", null), AppVersion.Parse("1.0.5+"));
    }

    [TestMethod]
    public void HasAFallbackWhenThereIsNoVersion()
    {
        Assert.AreEqual(new VersionInfo("0.0.0", null), AppVersion.Parse(null));
    }
}

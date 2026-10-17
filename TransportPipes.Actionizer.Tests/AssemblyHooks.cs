using TransportPipes.TestsCommon.Database;

namespace TransportPipes.Actionizer.Tests;

[TestClass]
public static class AssemblyHooks
{
    [AssemblyCleanup]
    public static async Task AssemblyCleanup()
    {
        await PostgresTestDatabase.StopAsync();
    }
}

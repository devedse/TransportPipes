using TransportPipes.TestsCommon.Database;

namespace TransportPipes.Server.Tests;

[TestClass]
public static class AssemblyHooks
{
    [AssemblyCleanup]
    public static async Task AssemblyCleanup()
    {
        await PostgresTestDatabase.StopAsync();
    }
}

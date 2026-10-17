using System.Reflection;
using TransportPipes.Server.Dtos;

namespace TransportPipes.Server.Services;

/// <summary>
/// The version of the running server. The build sets it: /p:Version (1.0.&lt;build number&gt;[-beta]) and
/// /p:SourceRevisionId (the git commit), which ends up as "1.0.5-beta+&lt;commit&gt;" in the informational version.
/// </summary>
public static class AppVersion
{
    public static VersionInfo Current { get; } = Parse(typeof(AppVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    internal static VersionInfo Parse(string? informationalVersion)
    {
        if (string.IsNullOrWhiteSpace(informationalVersion))
        {
            return new VersionInfo("0.0.0", null);
        }

        var plusIndex = informationalVersion.IndexOf('+');
        if (plusIndex < 0)
        {
            return new VersionInfo(informationalVersion, null);
        }

        var commit = informationalVersion[(plusIndex + 1)..];
        return new VersionInfo(informationalVersion[..plusIndex], string.IsNullOrWhiteSpace(commit) ? null : commit);
    }
}

using System.Reflection;

namespace BetterAmongUs;

internal static class ModInfo
{
    internal static readonly ReleaseTypes ReleaseBuildType = ReleaseTypes.Dev;
    public static string CommitHash = GetAssemblyMetadata("CommitHash");
    public static string BuildDate = GetAssemblyMetadata("BuildDate");

    internal const string BETA_NUM = "0";
    internal const string HOTFIX_NUM = "0";
    internal const bool IS_HOTFIX = false;
    internal const string PLUGIN_NAME = "BetterAmongUs";
    internal const string PLUGIN_GUID = "com.d1gq.betteramongus";
    internal const string PLUGIN_VERSION = "1.3.2";
    internal const string GITHUB = "https://github.com/D1GQ/BetterAmongUs";
    internal const string DISCORD = "https://discord.gg/vjYrXpzNAn";

    private static string GetAssemblyMetadata(string key)
    {
        var attribute = Assembly.GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == key);

        return attribute?.Value ?? "unknown";
    }
}
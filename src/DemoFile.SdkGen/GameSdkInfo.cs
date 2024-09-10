using System.Collections.Immutable;

namespace DemoFile.SdkGen;

public class GameSdkInfo
{
    public GameSdkInfo(string gameName)
    {
        GameName = gameName;

        if (gameName == "Cs")
        {
            HardcodedChildClasses = new Dictionary<string, IReadOnlyList<string>>
            {
                {
                    "CCSGameModeRules", new[]
                    {
                        // Removed in v13987
                        "CCSGameModeRules_Scripted"
                    }
                }
            };
        }
    }

    public string DemoParserClass => $"{GameName}DemoParser";

    public string GameName { get; }

    public IReadOnlyDictionary<string, IReadOnlyList<string>> HardcodedChildClasses { get; } = ImmutableDictionary<string, IReadOnlyList<string>>.Empty;
}

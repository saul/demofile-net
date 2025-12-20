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
                },
                { "PhysicsRagdollPose_t", new[] { "PhysicsRagdollPose_t" } },
                { "CDestructiblePartsComponent", new[] { "CDestructiblePartsSystemComponent" } },
            };

            HardcodedEntities = new[]
            {
                "CBaseViewModel",
                "CCSGOViewModel",
                "CPredictedViewModel",
            }.ToImmutableArray();

            HardcodedClasses = new[]
            {
                "CCSObserver_ViewModelServices",
                "CCSPlayer_ViewModelServices",
                "CPlayer_ViewModelServices",
                "CSNetworkableLoadout_t"
            }.ToImmutableArray();
        }
    }

    public string DemoParserClass => $"{GameName}DemoParser";

    public string GameName { get; }

    public IReadOnlyDictionary<string, IReadOnlyList<string>> HardcodedChildClasses { get; } = ImmutableDictionary<string, IReadOnlyList<string>>.Empty;

    public ImmutableArray<string> HardcodedEntities { get; } = ImmutableArray<string>.Empty;

    public ImmutableArray<string> HardcodedClasses { get; } = ImmutableArray<string>.Empty;
}

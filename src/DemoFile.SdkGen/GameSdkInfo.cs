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

            NetworkAliases = new Dictionary<string, string>
            {
                {"CRenderComponent", "m_CRenderComponent"},
                {"m_bUsesIndexedBakedLighting", "m_bUsesBakedShadowing"},
                {"m_aPlayers", "m_aPlayerControllers"},
                {"m_aPawns", "m_aPlayers"},
                {"m_iRawValue32", "m_flValue"},
                {"CPropDataComponent", "m_CPropDataComponent"},
                {"CBodyComponent", "m_CBodyComponent"},
                {"CPathQueryComponent", "m_CPathQueryComponent"},
                {"CTouchExpansionComponent", "m_CTouchExpansionComponent"},
                {"CLightComponent", "m_CLightComponent"},
                {"CHitboxComponent", "m_CHitboxComponent"},
            };
        }
    }

    public string DemoParserClass => $"{GameName}DemoParser";

    public string GameName { get; }

    public IReadOnlyDictionary<string, IReadOnlyList<string>> HardcodedChildClasses { get; } = ImmutableDictionary<string, IReadOnlyList<string>>.Empty;

    public ImmutableArray<string> HardcodedEntities { get; } = ImmutableArray<string>.Empty;

    public ImmutableArray<string> HardcodedClasses { get; } = ImmutableArray<string>.Empty;

    /// Map of network field => schema field name
    public IReadOnlyDictionary<string, string> NetworkAliases { get; } = ImmutableDictionary<string, string>.Empty;
}

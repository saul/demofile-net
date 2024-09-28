using System.Collections.Immutable;

namespace DemoFile.Source1EventGen;

public class GameSdkInfo
{
    public GameSdkInfo(string gameName)
    {
        GameName = gameName;

        if (gameName == "Cs")
        {
            SyntheticEvents = new HashSet<string>
            {
                "round_start",
                "round_end"
            };
        }
        else if (gameName == "Dota")
        {
            HasPlayerId = true;
        }

        (PlayerControllerClass, PlayerPawnClass) = gameName switch
        {
            "Cs" => ("CCSPlayerController", "CCSPlayerPawn"),
            "Deadlock" => ("CCitadelPlayerController", "CCitadelPlayerPawn"),
            "Dota" => ("CDOTAPlayerController", "CDOTAPlayerPawn"),
            _ => throw new ArgumentOutOfRangeException(nameof(gameName), gameName, null)
        };
    }

    public bool HasPlayerId { get; }

    public string PlayerControllerClass { get; }

    public string PlayerPawnClass { get; }

    public string DemoParserClass => $"{GameName}DemoParser";

    public string GameName { get; }

    public IReadOnlySet<string> SyntheticEvents { get; } = ImmutableHashSet<string>.Empty;
}

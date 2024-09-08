namespace DemoFile.Source1EventGen;

public class GameSdkInfo
{
    public GameSdkInfo(string gameName)
    {
        GameName = gameName;
    }

    public string DemoParserClass => $"{GameName}DemoParser";

    public string GameName { get; }
}

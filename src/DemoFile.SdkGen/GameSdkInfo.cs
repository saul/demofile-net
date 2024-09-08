namespace DemoFile.SdkGen;

public class GameSdkInfo
{
    public GameSdkInfo(string gameName)
    {
        GameName = gameName;
    }

    public string DemoParserClass => $"{GameName}DemoParser";

    public string GameName { get; }
}

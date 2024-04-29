namespace DemoFile.SdkGen;

public static class BackCompat
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> HardcodedChildClasses = new Dictionary<string, IReadOnlyList<string>>
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

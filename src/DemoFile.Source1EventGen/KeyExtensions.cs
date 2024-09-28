using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace DemoFile.Source1EventGen;

public static partial class KeyExtensions
{
    public static bool TryGetPlayerId(
        this CMsgSource1LegacyGameEventList.Types.key_t key,
        GameSdkInfo gameSdkInfo,
        [NotNullWhen(true)] out string? remainingName)
    {
        remainingName = null;

        if (!gameSdkInfo.HasPlayerId)
            return false;

        var replacement = PlayerIdRegex().Replace(key.Name, "$1");
        if (ReferenceEquals(replacement, key.Name))
        {
            return false;
        }

        remainingName = replacement;
        return true;
    }

    public static bool TryGetEntityIndex(this CMsgSource1LegacyGameEventList.Types.key_t key,
        [NotNullWhen(true)] out string? remainingName)
    {
        remainingName = null;

        var replacement = EntityIndexRegex().Replace(key.Name, "$1");
        if (ReferenceEquals(replacement, key.Name))
        {
            return false;
        }

        remainingName = replacement;
        return true;
    }

    [GeneratedRegex("(player)(_?id)", RegexOptions.IgnoreCase)]
    private static partial Regex PlayerIdRegex();

    [GeneratedRegex("(ent(ity)?)_?index", RegexOptions.IgnoreCase)]
    private static partial Regex EntityIndexRegex();
}

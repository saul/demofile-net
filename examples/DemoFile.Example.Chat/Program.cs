using System.Buffers;
using System.Text.RegularExpressions;
using DemoFile;
using DemoFile.Sdk;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var path = args.SingleOrDefault() ?? throw new Exception("Expected a single argument: <path to .dem>");

        var demo = new DemoParser();
        demo.UserMessageEvents.UserMessageTextMsg += um =>
        {
            // 3 = HUD_PRINTTALK
            if (um.Dest == 3u)
            {
                var formatted = (um.Param[0][0] == '#'
                        ? ChatMessageStringLookup.GetValueOrDefault(um.Param[0][1..])
                        : null)
                    ?? string.Join(' ', um.Param);

                for (var i = 1; i < um.Param.Count; i++)
                {
                    formatted = formatted.Replace($"%s{i}", ParseSlotName(um.Param[i]));
                }

                WriteColourChat(formatted, teamColour: null);
            }
        };

        demo.UserMessageEvents.UserMessageSayText += um =>
        {
            WriteColourChat(um.Text, teamColour: null);
        };

        demo.UserMessageEvents.UserMessageSayText2 += um =>
        {
            var entity = um.Entityindex >= 0
                ? demo.GetEntityByIndex<CBaseEntity>(new CEntityIndex((uint) um.Entityindex))
                : null;

            var teamColour = entity?.CSTeamNum is { } teamNumber ? TeamConsoleColor(teamNumber) : null;

            var formatted = ChatMessageStringLookup.GetValueOrDefault(
                um.Messagename,
                $"{um.Messagename} {um.Param1} {um.Param2} {um.Param3} {um.Param4}");

            WriteColourChat(formatted
                .Replace("%s1", um.Param1)
                .Replace("%s2", um.Param2)
                .Replace("%s3", um.Param3)
                .Replace("%s4", um.Param4),
                teamColour);
        };

        await demo.ReadAllAsync(File.OpenRead(path));

        Console.WriteLine("\nFinished!");

        string ParseSlotName(string param)
        {
            return Regex.Replace(param, @"#SLOTNAME\[(\d+)\]", match =>
            {
                var slot = int.Parse(match.Groups[1].Value);
                var index = new CEntityIndex((uint) slot + 1);

                return demo.GetEntityByIndex<CBaseEntity>(index)?.CSTeamNum switch
                {
                    CSTeamNumber.Terrorist => "\u0007",
                    CSTeamNumber.CounterTerrorist => "\u000B",
                    _ => ""
                };
            });
        }
    }

    private static void WriteColourChat(string message, ConsoleColor? teamColour)
    {
        var originalColor = Console.ForegroundColor;

        foreach (var c in message)
        {
            switch (c)
            {
                case '\u0001': Console.ForegroundColor = originalColor; break; // Default
                case '\u0002': Console.ForegroundColor = ConsoleColor.DarkRed; break; // Dark Red
                case '\u0003': Console.ForegroundColor = teamColour ?? ConsoleColor.DarkMagenta; break; // Light purple
                case '\u0004': Console.ForegroundColor = ConsoleColor.Green; break; // Bright green
                case '\u0005': Console.ForegroundColor = ConsoleColor.DarkGreen; break; // Pale green
                case '\u0006': Console.ForegroundColor = ConsoleColor.Green; break; // Green
                case '\u0007': Console.ForegroundColor = ConsoleColor.Red; break; // Pale red
                case '\u0008': Console.ForegroundColor = ConsoleColor.Gray; break; // Grey
                case '\u0009': Console.ForegroundColor = ConsoleColor.Yellow; break; // Yellow
                case '\u000A': Console.ForegroundColor = ConsoleColor.DarkGray; break; // Silver
                case '\u000B': Console.ForegroundColor = ConsoleColor.Blue; break; // Blue
                case '\u000C': Console.ForegroundColor = ConsoleColor.DarkBlue; break; // Dark blue
                case '\u000D': Console.ForegroundColor = ConsoleColor.DarkMagenta; break; // Blue grey for SayText2, purple for SayText
                case '\u000E': Console.ForegroundColor = ConsoleColor.Magenta; break; // Magenta
                case '\u000F': Console.ForegroundColor = ConsoleColor.DarkRed; break; // Dull red
                case '\u0010': Console.ForegroundColor = ConsoleColor.DarkYellow; break; // Orange
                default: Console.Write(c); break;
            }
        }

        Console.WriteLine();
        Console.ForegroundColor = originalColor;
    }

    private static ConsoleColor? TeamConsoleColor(CSTeamNumber teamNumber) => teamNumber switch
    {
        CSTeamNumber.Terrorist => ConsoleColor.Red,
        CSTeamNumber.CounterTerrorist => ConsoleColor.Blue,
        _ => null
    };

    private static readonly Dictionary<string, string> ChatMessageStringLookup = new()
    {
        {"Cstrike_Chat_CT_Loc", "[CT] \u0003%s1\u0004﹫%s3\u0001: %s2"},
        {"Cstrike_Chat_T_Loc", "[T] \u0003%s1\u0004﹫%s3\u0001: %s2"},
        {"Cstrike_Chat_CT_Dead", "[CT] \u0003%s1 \b[DEAD]\u0001: %s2"},
        {"Cstrike_Chat_T_Dead", "[T] \u0003%s1 \b[DEAD]\u0001: %s2"},
        {"Cstrike_Chat_CT", "[CT] \u0003%s1: \u0001%s2"},
        {"Cstrike_Chat_T", "[T] \u0003%s1: \u0001%s2"},
        {"Cstrike_Chat_All", "\u0001[ALL] \u0003%s1\u0001: %s2"},
        {"Cstrike_Chat_AllDead", "\u0001[ALL] \u0003%s1 \b[DEAD]\u0001: %s2"},
        {"Cstrike_Chat_AllSpec", "\u0001[ALL] \u0001%s1 \b[SPEC]\u0001: %s2"},
        {"Cstrike_Chat_Spec", "\u0001%s1 \b[SPEC]\u0001: %s2"},
        {"Cstrike_Name_Change", "\b%s1 changed name to %s2"},
        {"CSGO_Coach_Join_CT", "\u0006* %s1 is now coaching the COUNTER-TERRORISTS."},
        {"CSGO_Coach_Join_T", "\u0006* %s1 is now coaching the TERRORISTS."},
        {"CSGO_No_Longer_Coach", "\u0006* %s1 is no longer coaching."},
        {"Cstrike_TitlesTXT_Game_teammate_attack", "\u000f%s1 attacked a teammate"},
        {"SFUI_Notice_Match_Will_Pause", "The match is set to pause during freeze time"},
        {"SFUI_Notice_Match_Will_Pause_Technical", "The match is set to pause during freeze time for a technical timeout"},
        {"SFUI_Notice_Match_Will_Resume", "Freeze time pause has been cancelled"}
    };
}
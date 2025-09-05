using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace DemoFile.FindLiveMatch;

class Program
{
    static async Task Main(string[] args)
    {
        if (args is not [var game])
            throw new Exception("Usage: <game>");

        if (Environment.GetEnvironmentVariable("GITHUB_OUTPUT") is not {Length: >0} githubOutput)
            throw new Exception("Missing required env variable 'GITHUB_OUTPUT'");

        string url;
        if (game.Equals("deadlock", StringComparison.OrdinalIgnoreCase))
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("https://deadlocktracker.gg/live");
            response.EnsureSuccessStatusCode();
            var htmlContent = await response.Content.ReadAsStringAsync();

            var document = new HtmlDocument();
            document.LoadHtml(htmlContent);

            var fs13Element = document.DocumentNode
                .SelectSingleNode("//*[contains(@class, 'fs13')]");

            var matchId = fs13Element?.InnerText?.Trim() ?? string.Empty;
            if (!Regex.IsMatch(matchId, @"^\d+$"))
            {
                throw new Exception($"Invalid match ID: '{matchId}'");
            }

            url = $"https://dist1-ord1.steamcontent.com/tv/{matchId}/sync";
        }
        else
        {
            throw new Exception($"Unknown game: {game}");
        }

        Console.WriteLine($"Match URL: {url}");
        await File.AppendAllLinesAsync(githubOutput, new[] {$"url={url}"});
    }
}

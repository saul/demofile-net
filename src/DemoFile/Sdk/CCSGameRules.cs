namespace DemoFile.Sdk;

public partial class CCSGameRules
{
    /// The current phase of gameplay
    public CSGamePhase CSGamePhase => (CSGamePhase) GamePhase;

    /// The result of each round in this match
    public IEnumerable<CSRoundResult> CSRoundResults => MatchStats_RoundResults
        .Where(x => x != 0)
        .Select(x => (CSRoundResult)(x - 1));
}

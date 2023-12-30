namespace DemoFile.Test.Unit;

[TestFixture]
public class GameTickTest
{
    private static readonly TestCaseData[] ComparisonCases =
    {
        new(default(GameTick), new GameTick(1)),
        new(new GameTick(0), new GameTick(1)),
        new(new GameTick(1), new GameTick(2))
    };

    [TestCaseSource(nameof(ComparisonCases))]
    public void Comparison_LessThanGreaterThan_True(GameTick left, GameTick right)
    {
        Assert.That(left < right);
        Assert.That(left <= right);

        Assert.That(left > right, Is.False);
        Assert.That(left >= right, Is.False);

        Assert.That(left, Is.LessThan(right));
        Assert.That(left.CompareTo(right), Is.EqualTo(-1));

        Assert.That(left != right);
        Assert.That(left == right, Is.False);

        Assert.That(right > left);
        Assert.That(right, Is.GreaterThan(left));
        Assert.That(right.CompareTo(left), Is.EqualTo(1));
    }

    private static readonly TestCaseData[] EqualityCases =
    {
        new(default(GameTick), new GameTick(0)),
        new(new GameTick(1), new GameTick(1)),
        new(new GameTick(2), new GameTick(2))
    };

    [TestCaseSource(nameof(EqualityCases))]
    public void Equality_True(GameTick left, GameTick right)
    {
        Assert.That(left == right);
        Assert.That(left != right, Is.False);
        Assert.That(left.CompareTo(right), Is.EqualTo(0));

        Assert.That(right == left);
        Assert.That(right != left, Is.False);
        Assert.That(right.CompareTo(left), Is.EqualTo(0));
    }
}